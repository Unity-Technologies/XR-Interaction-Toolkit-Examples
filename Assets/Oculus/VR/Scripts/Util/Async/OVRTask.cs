/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

internal static class OVRTask
{
    internal static OVRTask<TResult> FromGuid<TResult>(Guid id) => Create<TResult>(id);
    internal static OVRTask<TResult> FromRequest<TResult>(ulong id) => Create<TResult>(GetId(id));

    internal static OVRTask<TResult> FromResult<TResult>(TResult result)
    {
        var task = Create<TResult>(Guid.NewGuid());
        task.SetResult(result);
        return task;
    }

    internal static OVRTask<TResult> GetExisting<TResult>(Guid id) => Get<TResult>(id);
    internal static OVRTask<TResult> GetExisting<TResult>(ulong id) => Get<TResult>(GetId(id));

    internal static void SetResult<TResult>(Guid id, TResult result) =>
        GetExisting<TResult>(id).SetResult(result);

    internal static void SetResult<TResult>(ulong id, TResult result) =>
        GetExisting<TResult>(id).SetResult(result);

    private static OVRTask<TResult> Get<TResult>(Guid id)
    {
        return new OVRTask<TResult>(id);
    }

    private static OVRTask<TResult> Create<TResult>(Guid id)
    {
        var task = Get<TResult>(id);
        task.AddToPending();
        return task;
    }

    internal static unsafe Guid GetId(ulong value)
    {
        const ulong hashModifier1 = 0x319642b2d24d8ec3;
        const ulong hashModifier2 = 0x96de1b173f119089;
        var guid = default(Guid);
        *(ulong*)&guid = unchecked(value + hashModifier1);
        *((ulong*)&guid + 1) = hashModifier2;
        return guid;
    }
}

/// <summary>
/// Represents an awaitable task.
/// </summary>
/// <remarks>
/// This is a task-like object which supports the <c>await</c> pattern. Typically, you do not need to
/// create or use this object directly. Instead, you can either :
/// <para>- <c>await</c> a method which returns an object of type <see cref="OVRTask{TResult}"/>,
/// which will eventually return a <typeparamref name="TResult"/></para>
/// <para>- poll the <see cref="IsCompleted"/> property and then call <see cref="GetResult"/></para>
/// <para>- pass a delegate by calling <see cref="ContinueWith(Action{TResult})"/>. Note that an additional state <c>object</c> can get passed in and added as a parameter of the callback, see <see cref="ContinueWith{T}"/></para>
/// </remarks>
/// <typeparam name="TResult">The type of result being awaited.</typeparam>
public readonly struct OVRTask<TResult> : IEquatable<OVRTask<TResult>>, IDisposable
{
    #region static

    private static readonly HashSet<Guid> Pending = new HashSet<Guid>();
    private static readonly Dictionary<Guid, TResult> Results = new Dictionary<Guid, TResult>();
    private static readonly Dictionary<Guid, Action> Continuations = new Dictionary<Guid, Action>();

    private delegate void CallbackInvoker(Guid guid, TResult result);

    private delegate bool CallbackRemover(Guid guid);

    private static readonly Dictionary<Guid, CallbackInvoker>
        CallbackInvokers = new Dictionary<Guid, CallbackInvoker>();

    private static readonly Dictionary<Guid, CallbackRemover>
        CallbackRemovers = new Dictionary<Guid, CallbackRemover>();

    private static readonly HashSet<Action> CallbackClearers = new HashSet<Action>();

    private delegate bool InternalDataRemover(Guid guid);

    private static readonly Dictionary<Guid, InternalDataRemover> InternalDataRemovers =
        new Dictionary<Guid, InternalDataRemover>();

    private static readonly HashSet<Action> InternalDataClearers = new HashSet<Action>();

    private static readonly Dictionary<Guid, Action<Guid>> SubscriberRemovers =
        new Dictionary<Guid, Action<Guid>>();

    private static readonly HashSet<Action> SubscriberClearers = new HashSet<Action>();


    #endregion

    private readonly Guid _id;

    internal OVRTask(Guid id)
    {
        _id = id;
    }

    internal void AddToPending() => Pending.Add(_id);
    internal bool IsPending => Pending.Contains(_id);
    internal void SetInternalData<T>(T data) => InternalData<T>.Set(_id, data);
    internal bool TryGetInternalData<T>(out T data) => InternalData<T>.TryGet(_id, out data);

    internal void SetResult(TResult result)
    {
        // Means no one was awaiting this result.
        if (!Pending.Remove(_id)) return;

        if (InternalDataRemovers.TryGetValue(_id, out var internalDataRemover))
        {
            InternalDataRemovers.Remove(_id);
            internalDataRemover(_id);
        }

        if (SubscriberRemovers.TryGetValue(_id, out var subscriberRemover))
        {
            SubscriberRemovers.Remove(_id);
            subscriberRemover(_id);
        }

        if (CallbackInvokers.TryGetValue(_id, out var invoker))
        {
            CallbackInvokers.Remove(_id);
            invoker(_id, result);
        }
        else
        {
            // Add to the results so that GetResult can retrieve it later.
            Results.Add(_id, result);

            if (Continuations.TryGetValue(_id, out var continuation))
            {
                Continuations.Remove(_id);
                continuation();
            }
        }
    }

    private static class InternalData<T>
    {
        private static readonly Dictionary<Guid, T> Data = new Dictionary<Guid, T>();

        public static bool TryGet(Guid taskId, out T data)
        {
            return Data.TryGetValue(taskId, out data);
        }

        public static void Set(Guid taskId, T data)
        {
            Data[taskId] = data;
            InternalDataRemovers.Add(taskId, Remover);
            InternalDataClearers.Add(Clearer);
        }

        private static readonly InternalDataRemover Remover = Remove;
        private static readonly Action Clearer = Clear;
        private static bool Remove(Guid taskId) => Data.Remove(taskId);
        private static void Clear() => Data.Clear();
    }

    static class IncrementalResultSubscriber<T>
    {
        static readonly Dictionary<Guid, Action<T>> Subscribers = new Dictionary<Guid, Action<T>>();

        public static void Set(Guid taskId, Action<T> subscriber)
        {
            Subscribers[taskId] = subscriber;
            SubscriberRemovers[taskId] = Remover;
            SubscriberClearers.Add(Clearer);
        }

        public static void Notify(Guid taskId, T result)
        {
            if (Subscribers.TryGetValue(taskId, out var subscriber))
            {
                subscriber(result);
            }
        }

        static readonly Action<Guid> Remover = Remove;

        static void Remove(Guid id) => Subscribers.Remove(id);

        static readonly Action Clearer = Clear;

        static void Clear() => Subscribers.Clear();
    }

    /// <summary>
    /// Sets the delegate to be invoked when an incremental result is available before the task is complete.
    /// </summary>
    /// <remarks>
    /// Some tasks may provide incremental results before the task is complete. In this case, you can use
    /// <see cref="SetIncrementalResultCallback{TIncrementalResult}"/> to receive those results as they become available.
    ///
    /// For example, the task may provide a list of results over some period of time and may be able to provide
    /// partial results as they become available, before the task completes.
    /// </remarks>
    /// <param name="onIncrementalResultAvailable">Invoked whenever <see cref="NotifyIncrementalResult{TIncrementalResult}"/>
    /// is called.</param>
    /// <typeparam name="TIncrementalResult">The type of the incremental result. This is typically different than the
    /// <typeparamref name="TResult"/>.</typeparam>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="onIncrementalResultAvailable"/> is `null`.</exception>
    internal void SetIncrementalResultCallback<TIncrementalResult>(
        Action<TIncrementalResult> onIncrementalResultAvailable)
    {
        if (onIncrementalResultAvailable == null)
            throw new ArgumentNullException(nameof(onIncrementalResultAvailable));

        IncrementalResultSubscriber<TIncrementalResult>.Set(_id, onIncrementalResultAvailable);
    }

    /// <summary>
    /// Notifies a subscriber of an incremental result associated with an ongoing task.
    /// </summary>
    /// <remarks>
    /// Use this to provide partial results that may be available before the task fully completes.
    /// </remarks>
    /// <typeparam name="TIncrementalResult">The type of the result, usually different from <typeparamref name="TResult"/>.</typeparam>
    internal void NotifyIncrementalResult<TIncrementalResult>(TIncrementalResult incrementalResult)
        => IncrementalResultSubscriber<TIncrementalResult>.Notify(_id, incrementalResult);

    #region Polling Implementation

    /// <summary>
    /// Indicates whether the task has completed.
    /// </summary>
    /// <remarks>
    /// Choose only one pattern out of the three proposed way of awaiting for the task completion:
    /// Polling,<c>async/await</c> or <see cref="ContinueWith(Action{TResult})"/>
    /// as all three patterns will end up calling the <see cref="GetResult"/> which can only be called once.
    /// </remarks>
    /// <returns><c>True</c> if the task has completed. <see cref="GetResult"/> can be called.</returns>
    public bool IsCompleted => !IsPending;

    /// <summary>
    /// Gets the result of the Task.
    /// </summary>
    /// <remarks>
    /// This method should only be called once <see cref="IsCompleted"/> is true.
    /// Calling it multiple times leads to undefined behavior.
    /// Do not use in conjunction with any other methods (<c>await</c> or using <see cref="ContinueWith"/>).
    /// </remarks>
    /// <returns>Returns the result of type <typeparamref name="TResult"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the task doesn't have any available result. This could
    /// happen if the method is called before <see cref="IsCompleted"/> is true, after the task has been disposed of
    /// or if this method has already been called once.</exception>
    public TResult GetResult()
    {
        if (!Results.TryGetValue(_id, out var value))
        {
            throw new InvalidOperationException($"Task {_id} doesn't have any available result.");
        }

        Results.Remove(_id);
        return value;
    }

    #endregion

    #region Awaiter Contract Implementation

    /// <summary>
    /// Definition of an awaiter that satisfies the await contract.
    /// </summary>
    /// <remarks>
    /// This allows an <see cref="OVRTask{T}"/> to be awaited using the <c>await</c> keyword.
    /// Typically, you should not use this struct; instead, it is used by the compiler by
    /// automatically calling the <see cref="GetAwaiter"/> method when using the <c>await</c> keyword.
    /// </remarks>
    public readonly struct Awaiter : INotifyCompletion
    {
        private readonly OVRTask<TResult> _task;

        internal Awaiter(OVRTask<TResult> task)
        {
            _task = task;
        }

        public bool IsCompleted => _task.IsCompleted;
        public void OnCompleted(Action continuation) => _task.WithContinuation(continuation);
        public TResult GetResult() => _task.GetResult();
    }

    /// <summary>
    /// Gets an awaiter that satisfies the await contract.
    /// </summary>
    /// <remarks>
    /// This allows an <see cref="OVRTask{T}"/> to be awaited using the <c>await</c> keyword.
    /// Typically, you should not call this directly; instead, it is invoked by the compiler, e.g.,
    /// <example>
    /// <code><![CDATA[
    /// // Something that returns an OVRTask<T>
    /// var task = GetResultAsync();
    ///
    /// // compiler uses GetAwaiter here
    /// var result = await task;
    /// ]]></code>
    /// Or, more commonly:
    /// <code><![CDATA[
    /// var result = await GetResultAsync();
    /// ]]></code>
    /// </example>
    /// </remarks>
    /// <returns>Returns an Awaiter-like object that satisfies the await pattern.</returns>
    public Awaiter GetAwaiter() => new Awaiter(this);

    private void WithContinuation(Action continuation)
    {
        ValidateDelegateAndThrow(continuation, nameof(continuation));

        Continuations[_id] = continuation;
    }

    #endregion

    #region Delegate Implementation

    readonly struct Callback
    {
        private static readonly Dictionary<Guid, Callback> Callbacks = new Dictionary<Guid, Callback>();

        readonly Action<TResult> _delegate;

        static void Invoke(Guid taskId, TResult result)
        {
            if (Callbacks.TryGetValue(taskId, out var callback))
            {
                Callbacks.Remove(taskId);
                callback.Invoke(result);
            }
        }

        static bool Remove(Guid taskId) => Callbacks.Remove(taskId);

        static void Clear() => Callbacks.Clear();

        void Invoke(TResult result) => _delegate(result);

        Callback(Action<TResult> @delegate) => _delegate = @delegate;

        public static readonly CallbackInvoker Invoker = Invoke;

        public static readonly CallbackRemover Remover = Remove;

        public static readonly Action Clearer = Clear;

        public static void Add(Guid taskId, Action<TResult> @delegate)
        {
            Callbacks.Add(taskId, new Callback(@delegate));
            CallbackInvokers.Add(taskId, Invoker);
            CallbackRemovers.Add(taskId, Remover);
            CallbackClearers.Add(Clearer);
        }
    }

    readonly struct CallbackWithState<T>
    {
        private static readonly Dictionary<Guid, CallbackWithState<T>> Callbacks =
            new Dictionary<Guid, CallbackWithState<T>>();

        readonly T _data;

        readonly Action<TResult, T> _delegate;

        static void Invoke(Guid taskId, TResult result)
        {
            if (Callbacks.TryGetValue(taskId, out var callback))
            {
                Callbacks.Remove(taskId);
                callback.Invoke(result);
            }
        }

        CallbackWithState(T data, Action<TResult, T> @delegate)
        {
            _data = data;
            _delegate = @delegate;
        }

        private static readonly CallbackInvoker Invoker = Invoke;
        private static readonly CallbackRemover Remover = Remove;
        private static readonly Action Clearer = Clear;
        private static void Clear() => Callbacks.Clear();
        private static bool Remove(Guid taskId) => Callbacks.Remove(taskId);
        private void Invoke(TResult result) => _delegate(result, _data);

        public static void Add(Guid taskId, T data, Action<TResult, T> callback)
        {
            Callbacks.Add(taskId, new CallbackWithState<T>(data, callback));
            CallbackInvokers.Add(taskId, Invoker);
            CallbackRemovers.Add(taskId, Remover);
            CallbackClearers.Add(Clearer);
        }
    }

    /// <summary>
    /// Registers a delegate that will get called on completion of the task.
    /// </summary>
    /// <remarks>
    /// The delegate will be invoked with the <typeparamref name="TResult"/> result as parameter.
    /// Do not use in conjunction with any other methods (<c>await</c> or calling <see cref="GetResult"/>).
    /// </remarks>
    /// <param name="onCompleted">A delegate to be invoked when this task completes. If the task is already complete,
    /// <paramref name="onCompleted"/> is invoked immediately.</param>
    /// <seealso cref="ContinueWith{T}"/>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="onCompleted"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if there already is a delegate or a continuation registered to this task.</exception>
    public void ContinueWith(Action<TResult> onCompleted)
    {
        ValidateDelegateAndThrow(onCompleted, nameof(onCompleted));

        if (IsCompleted)
        {
            onCompleted.Invoke(GetResult());
        }
        else
        {
            Callback.Add(_id, onCompleted);
        }
    }

    /// <summary>
    /// Registers a delegate that will get called on completion of the task.
    /// </summary>
    /// <remarks>
    /// The delegate will be invoked with <paramref name="state"/> and the <typeparamref name="TResult"/> result as
    /// parameters.
    /// Do not use in conjunction with any other methods (<c>await</c> or calling <see cref="GetResult"/>).
    /// </remarks>
    /// <param name="onCompleted">A delegate to be invoked when this task completes. If the task is already complete,
    /// <paramref name="onCompleted"/> is invoked immediately.</param>
    /// <param name="state">An <c>object</c> to store and pass to <paramref name="onCompleted"/>.</param>
    /// <seealso cref="ContinueWith(Action{TResult})"/>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="onCompleted"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if there already is a delegate or a continuation registered to this task.</exception>
    public void ContinueWith<T>(Action<TResult, T> onCompleted, T state)
    {
        ValidateDelegateAndThrow(onCompleted, nameof(onCompleted));

        if (IsCompleted)
        {
            onCompleted.Invoke(GetResult(), state);
        }
        else
        {
            CallbackWithState<T>.Add(_id, state, onCompleted);
        }
    }

    void ValidateDelegateAndThrow(object @delegate, string paramName)
    {
        if (@delegate == null)
            throw new ArgumentNullException(paramName);

        if (Continuations.ContainsKey(_id))
            throw new InvalidOperationException($"Task {_id} is already being used by an await call.");

        if (CallbackInvokers.ContainsKey(_id))
            throw new InvalidOperationException($"Task {_id} is already being used with ContinueWith.");
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes of the task.
    /// </summary>
    /// <remarks>
    /// Invalidate this object but does not cancel the task.
    /// In the case where the result will not actually be consumed, it must be called to prevent a memory leak.
    /// You can not call <see cref="GetResult"/> nor use <c>await</c> on a disposed task.
    /// </remarks>
    public void Dispose()
    {
        Results.Remove(_id);
        Continuations.Remove(_id);
        Pending.Remove(_id);

        CallbackInvokers.Remove(_id);
        if (CallbackRemovers.TryGetValue(_id, out var remover))
        {
            CallbackRemovers.Remove(_id);
            remover(_id);
        }

        if (InternalDataRemovers.TryGetValue(_id, out var internalDataRemover))
        {
            InternalDataRemovers.Remove(_id);
            internalDataRemover(_id);
        }

        if (SubscriberRemovers.TryGetValue(_id, out var subscriberRemover))
        {
            SubscriberRemovers.Remove(_id);
            subscriberRemover(_id);
        }
    }

    #endregion

    #region IEquatable Implementation

    public bool Equals(OVRTask<TResult> other) => _id == other._id;
    public override bool Equals(object obj) => obj is OVRTask<TResult> other && Equals(other);
    public static bool operator ==(OVRTask<TResult> lhs, OVRTask<TResult> rhs) => lhs.Equals(rhs);
    public static bool operator !=(OVRTask<TResult> lhs, OVRTask<TResult> rhs) => !lhs.Equals(rhs);
    public override int GetHashCode() => _id.GetHashCode();
    public override string ToString() => _id.ToString();

    #endregion
}
