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
using System.Diagnostics;
#if DEVELOPMENT_BUILD
using System.Linq;
#endif
using Unity.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Represents a spatial anchor.
/// </summary>
/// <remarks>
/// This component can be used in two ways: to create a new spatial anchor or to bind to an existing spatial anchor.
///
/// To create a new spatial anchor, simply add this component to any GameObject. The transform of the GameObject is used
/// to create a new spatial anchor in the Oculus Runtime. Afterwards, the GameObject's transform will be updated
/// automatically. The creation operation is asynchronous, and, if it fails, this component will be destroyed.
///
/// To load previously saved anchors and bind them to an <see cref="OVRSpatialAnchor"/>, see
/// <see cref="LoadUnboundAnchors"/>.
/// </remarks>
[DisallowMultipleComponent]
public class OVRSpatialAnchor : MonoBehaviour
{
    private bool _startCalled;

    private ulong _requestId;

    private readonly SaveOptions _defaultSaveOptions = new SaveOptions
    {
        Storage = OVRSpace.StorageLocation.Local,
    };

    private readonly EraseOptions _defaultEraseOptions = new EraseOptions
    {
        Storage = OVRSpace.StorageLocation.Local,
    };

    /// <summary>
    /// Event that is dispatched when the localization process finishes.
    /// </summary>
    public event Action<OperationResult> OnLocalize;

    /// <summary>
    /// The space associated with the spatial anchor.
    /// </summary>
    /// <remarks>
    /// The <see cref="OVRSpace"/> represents the runtime instance of the spatial anchor and will change across
    /// different sessions.
    /// </remarks>
    public OVRSpace Space { get; private set; }

    /// <summary>
    /// The UUID associated with the spatial anchor.
    /// </summary>
    /// <remarks>
    /// UUIDs persist across sessions. If you load a persisted anchor, you can use the UUID to identify
    /// it.
    /// </remarks>
    public Guid Uuid { get; private set; }

    /// <summary>
    ///  Checks whether the spatial anchor is created.
    /// </summary>
    /// <remarks>
    /// Creation is asynchronous and may take several frames. If creation fails, the component is destroyed.
    /// </remarks>
    public bool Created => Space.Valid;

    /// <summary>
    /// Checks whether the spatial anchor is pending creation.
    /// </summary>
    public bool PendingCreation => _requestId != 0;

    /// <summary>
    /// Checks whether the spatial anchor is localized.
    /// </summary>
    /// <remarks>
    /// When you create a new spatial anchor, it may take a few frames before it is localized. Once localized,
    /// its transform updates automatically.
    /// </remarks>
    public bool Localized => Space.Valid &&
                             OVRPlugin.GetSpaceComponentStatus(Space, OVRPlugin.SpaceComponentType.Locatable,
                                 out var isEnabled, out _) && isEnabled;

    /// <summary>
    /// Initializes this component from an existing space handle and uuid, e.g., the result of a call to
    /// <see cref="OVRPlugin.QuerySpaces"/>.
    /// </summary>
    /// <remarks>
    /// This method associates the component with an existing spatial anchor, for example, the one that was saved in
    /// a previous session. Do not call this method to create a new spatial anchor.
    ///
    /// If you call this method, you must do so prior to the component's `Start` method. You cannot change the spatial
    /// anchor associated with this component after that.
    /// </remarks>
    /// <param name="space">The existing <see cref="OVRSpace"/> to associate with this spatial anchor.</param>
    /// <param name="uuid">The universally unique identifier to associate with this spatial anchor.</param>
    /// <exception cref="InvalidOperationException">Thrown if `Start` has already been called on this component.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="space"/> is not <see cref="OVRSpace.Valid"/>.</exception>
    public void InitializeFromExisting(OVRSpace space, Guid uuid)
    {
        if (_startCalled)
            throw new InvalidOperationException(
                $"Cannot call {nameof(InitializeFromExisting)} after {nameof(Start)}. This must be set once upon creation.");

        try
        {
            if (!space.Valid)
                throw new ArgumentException($"Invalid space {space}.", nameof(space));

            ThrowIfBound(uuid);
        }
        catch
        {
            Destroy(this);
            throw;
        }

        InitializeUnchecked(space, uuid);
    }

    /// <summary>
    /// Saves the <see cref="OVRSpatialAnchor"/> to local persistent storage.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous. Use <paramref name="onComplete"/> to be notified of completion.
    ///
    /// When saved, an <see cref="OVRSpatialAnchor"/> can be loaded by a different session. Use the
    /// <see cref="Uuid"/> to identify the same <see cref="OVRSpatialAnchor"/> at a future time.
    ///
    /// This operation fully succeeds or fails, which means, either all anchors are successfully saved,
    /// or the operation fails.
    /// </remarks>
    /// <param name="onComplete">
    /// Invoked when the save operation completes. May be null. Parameters are
    /// - <see cref="OVRSpatialAnchor"/>: The anchor being saved.
    /// - `bool`: A value indicating whether the save operation succeeded.
    /// </param>
    public void Save(Action<OVRSpatialAnchor, bool> onComplete = null)
    {
        Save(_defaultSaveOptions, onComplete);
    }

    private static NativeArray<ulong> ToNativeArray(ICollection<OVRSpatialAnchor> anchors)
    {
        var count = anchors.Count;
        var spaces = new NativeArray<ulong>(count, Allocator.Temp);
        var i = 0;
        foreach (var anchor in anchors.ToNonAlloc())
        {
            spaces[i++] = anchor ? anchor.Space : 0;
        }

        return spaces;
    }

    /// <summary>
    /// Saves the <see cref="OVRSpatialAnchor"/> with specified <see cref="SaveOptions"/>.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous. Use <paramref name="onComplete"/> to be notified of completion.
    /// When saved, the <see cref="OVRSpatialAnchor"/> can be loaded by a different session. Use the
    /// <see cref="Uuid"/> to identify the same <see cref="OVRSpatialAnchor"/> at a future time.
    ///
    /// This operation fully succeeds or fails; that is, either all anchors are successfully saved,
    /// or the operation fails.
    /// </remarks>
    /// <param name="saveOptions">Options how the anchor is saved. whether local or cloud.</param>
    /// <param name="onComplete">
    /// Invoked when the save operation completes. May be null. Parameters are
    /// - <see cref="OVRSpatialAnchor"/>: The anchor being saved.
    /// - `bool`: A value indicating whether the save operation succeeded.
    /// </param>
    public void Save(SaveOptions saveOptions, Action<OVRSpatialAnchor, bool> onComplete = null)
    {
        var task = SaveAsync(saveOptions);
        if (onComplete != null)
        {
            InvertedCapture<bool, OVRSpatialAnchor>.ContinueTaskWith(task, onComplete, this);
        }
    }

    /// <summary>
    /// Saves the <see cref="OVRSpatialAnchor"/> with specified <see cref="SaveOptions"/>.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous; use the returned <see cref="OVRTask{TResult}"/> to be notified of completion.
    /// When saved, the <see cref="OVRSpatialAnchor"/> can be loaded by a different session. Use the
    /// <see cref="Uuid"/> to identify the same <see cref="OVRSpatialAnchor"/> at a future time.
    ///
    /// This operation fully succeeds or fails; that is, either all anchors are successfully saved,
    /// or the operation fails.
    /// </remarks>
    /// <returns>
    /// An <see cref="OVRTask{TResult}"/> with a boolean type parameter indicating the success of the save operation.
    /// </returns>
    public OVRTask<bool> SaveAsync() => SaveAsync(_defaultSaveOptions);

    /// <summary>
    /// Saves the <see cref="OVRSpatialAnchor"/> with specified <see cref="SaveOptions"/>.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous; use the returned <see cref="OVRTask{TResult}"/> to be notified of completion.
    /// When saved, the <see cref="OVRSpatialAnchor"/> can be loaded by a different session. Use the
    /// <see cref="Uuid"/> to identify the same <see cref="OVRSpatialAnchor"/> at a future time.
    ///
    /// This operation fully succeeds or fails; that is, either all anchors are successfully saved,
    /// or the operation fails.
    /// </remarks>
    /// <param name="saveOptions">Options for how the anchor will be saved.</param>
    /// <returns>
    /// An <see cref="OVRTask{TResult}"/> with a boolean type parameter indicating the success of the save operation.
    /// </returns>
    public OVRTask<bool> SaveAsync(SaveOptions saveOptions)
    {
        var requestId = Guid.NewGuid();
        SaveRequests[saveOptions.Storage].Add(this);
        AsyncRequestTaskIds[this] = requestId;
        return OVRTask.FromGuid<bool>(requestId);
    }

    /// <summary>
    /// Saves a collection of <see cref="OVRSpatialAnchor"/> to specified storage.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous. Use <paramref name="onComplete"/> to be notified of completion.
    /// When saved, an <see cref="OVRSpatialAnchor"/> can be loaded by a different session. Use the
    /// <see cref="Uuid"/> to identify the same <see cref="OVRSpatialAnchor"/> at a future time.
    /// </remarks>
    /// <param name="anchors">Collection of anchors</param>
    /// <param name="saveOptions">Options how the anchors are saved whether local or cloud.</param>
    /// <param name="onComplete">
    /// Invoked when the save operation completes. May be null. <paramref name="onComplete"/> receives two parameters:
    /// - `ICollection&lt;OVRSpatialAnchor&gt;`: The same collection as in <paramref name="anchors"/> parameter
    /// - `OperationResult`: An error code indicating whether the save operation succeeded or not.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="anchors"/> is `null`.</exception>
    public static void Save(ICollection<OVRSpatialAnchor> anchors, SaveOptions saveOptions,
        Action<ICollection<OVRSpatialAnchor>, OperationResult> onComplete = null)
    {
        if (anchors == null)
            throw new ArgumentNullException(nameof(anchors));

        using var spaces = ToNativeArray(anchors);
        var saveResult =
            OVRPlugin.SaveSpaceList(spaces, saveOptions.Storage.ToSpaceStorageLocation(), out var requestId);

        if (saveResult.IsSuccess())
        {
            Development.LogRequest(requestId, $"Saving spatial anchors...");

            MultiAnchorCompletionDelegates[requestId] = new MultiAnchorDelegatePair
            {
                Anchors = CopyAnchorListIntoListFromPool(anchors),
                Delegate = onComplete
            };

            if (onComplete != null)
            {
                OVRTelemetry.Client.MarkerStart(OVRTelemetryConstants.Scene.MarkerId.SpatialAnchorSave,
                    requestId.GetHashCode());
            }
        }
        else
        {
            Development.LogError(
                $"{nameof(OVRPlugin)}.{nameof(OVRPlugin.SaveSpaceList)} failed with error {saveResult}.");
            onComplete?.Invoke(anchors, (OperationResult)saveResult);
        }
    }

    private static List<OVRSpatialAnchor> CopyAnchorListIntoListFromPool(
        IEnumerable<OVRSpatialAnchor> anchorList)
    {
        var poolList = OVRObjectPool.List<OVRSpatialAnchor>();
        poolList.AddRange(anchorList);
        return poolList;
    }

    /// <summary>
    /// Shares the anchor to an <see cref="OVRSpaceUser"/>.
    /// The specified user will be able to download, track, and share specified anchors.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous. Use <paramref name="onComplete"/> to be notified of completion.
    /// </remarks>
    /// <param name="user">An Oculus user to share the anchor with.</param>
    /// <param name="onComplete">
    /// Invoked when the share operation completes. May be null. Delegate parameter is
    /// - `OperationResult`: An error code that indicates whether the share operation succeeded or not.
    /// </param>
    public void Share(OVRSpaceUser user, Action<OperationResult> onComplete = null)
    {
        var task = ShareAsync(user);
        if (onComplete != null)
        {
            task.ContinueWith(onComplete);
        }
    }

    /// <summary>
    /// Shares the anchor to an <see cref="OVRSpaceUser"/>.
    /// The specified user will be able to download, track, and share specified anchors.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous; use the returned <see cref="OVRTask{TResult}"/> to be notified of completion.
    /// </remarks>
    /// <param name="user">An Oculus user to share the anchor with.</param>
    /// <returns>
    /// An <see cref="OVRTask{TResult}"/> with a <see cref="OperationResult"/> type parameter indicating the success of the share operation.
    /// </returns>
    public OVRTask<OperationResult> ShareAsync(OVRSpaceUser user)
    {
        var userList = OVRObjectPool.List<OVRSpaceUser>();
        userList.Add(user);
        return ShareAsyncInternal(userList);
    }

    /// <summary>
    /// Shares the anchor with two <see cref="OVRSpaceUser"/>.
    /// Specified users will be able to download, track, and share specified anchors.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous. Use <paramref name="onComplete"/> to be notified of completion.
    /// </remarks>
    /// <param name="user1">An Oculus user to share the anchor with.</param>
    /// <param name="user2">An Oculus user to share the anchor with.</param>
    /// <param name="onComplete">
    /// Invoked when the share operation completes. May be null. Delegate parameter is
    /// - `OperationResult`: An error code that indicates whether the share operation succeeded or not.
    /// </param>
    public void Share(OVRSpaceUser user1, OVRSpaceUser user2, Action<OperationResult> onComplete = null)
    {
        var task = ShareAsync(user1, user2);
        if (onComplete != null)
        {
            task.ContinueWith(onComplete);
        }
    }

    /// <summary>
    /// Shares the anchor to an <see cref="OVRSpaceUser"/>.
    /// The specified user will be able to download, track, and share specified anchors.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous; use the returned <see cref="OVRTask{TResult}"/> to be notified of completion.
    /// </remarks>
    /// <param name="user1">An Oculus user to share the anchor with.</param>
    /// <param name="user2">An Oculus user to share the anchor with.</param>
    /// <returns>
    /// An <see cref="OVRTask{TResult}"/> with a <see cref="OperationResult"/> type parameter indicating the success of the share operation.
    /// </returns>
    public OVRTask<OperationResult> ShareAsync(OVRSpaceUser user1, OVRSpaceUser user2)
    {
        var userList = OVRObjectPool.List<OVRSpaceUser>();
        userList.Add(user1);
        userList.Add(user2);
        return ShareAsyncInternal(userList);
    }

    /// <summary>
    /// Shares the anchor with three <see cref="OVRSpaceUser"/>.
    /// Specified users will be able to download, track, and share specified anchors.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous. Use <paramref name="onComplete"/> to be notified of completion.
    /// </remarks>
    /// <param name="user1">An Oculus user to share the anchor with.</param>
    /// <param name="user2">An Oculus user to share the anchor with.</param>
    /// <param name="user3">An Oculus user to share the anchor with.</param>
    /// <param name="onComplete">
    /// Invoked when the share operation completes. May be null. Delegate parameter is
    /// - `OperationResult`: An error code that indicates whether the share operation succeeded or not.
    /// </param>
    public void Share(OVRSpaceUser user1, OVRSpaceUser user2, OVRSpaceUser user3,
        Action<OperationResult> onComplete = null)
    {
        var task = ShareAsync(user1, user2, user3);
        if (onComplete != null)
        {
            task.ContinueWith(onComplete);
        }
    }

    /// <summary>
    /// Shares the anchor to an <see cref="OVRSpaceUser"/>.
    /// The specified user will be able to download, track, and share specified anchors.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous; use the returned <see cref="OVRTask{TResult}"/> to be notified of completion.
    /// </remarks>
    /// <param name="user1">An Oculus user to share the anchor with.</param>
    /// <param name="user2">An Oculus user to share the anchor with.</param>
    /// <param name="user3">An Oculus user to share the anchor with.</param>
    /// <returns>
    /// An <see cref="OVRTask{TResult}"/> with a <see cref="OperationResult"/> type parameter indicating the success of the share operation.
    /// </returns>
    public OVRTask<OperationResult> ShareAsync(OVRSpaceUser user1, OVRSpaceUser user2, OVRSpaceUser user3)
    {
        var userList = OVRObjectPool.List<OVRSpaceUser>();
        userList.Add(user1);
        userList.Add(user2);
        userList.Add(user3);
        return ShareAsyncInternal(userList);
    }

    /// <summary>
    /// Shares the anchor with four <see cref="OVRSpaceUser"/>.
    /// Specified users will be able to download, track, and share specified anchors.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous. Use <paramref name="onComplete"/> to be notified of completion.
    /// </remarks>
    /// <param name="user1">An Oculus user to share the anchor with.</param>
    /// <param name="user2">An Oculus user to share the anchor with.</param>
    /// <param name="user3">An Oculus user to share the anchor with.</param>
    /// <param name="user4">An Oculus user to share the anchor with.</param>
    /// <param name="onComplete">
    /// Invoked when the share operation completes. May be null. Delegate parameter is
    /// - `OperationResult`: An error code that indicates whether the share operation succeeded or not.
    /// </param>
    public void Share(OVRSpaceUser user1, OVRSpaceUser user2, OVRSpaceUser user3, OVRSpaceUser user4,
        Action<OperationResult> onComplete = null)
    {
        var task = ShareAsync(user1, user2, user3, user4);
        if (onComplete != null)
        {
            task.ContinueWith(onComplete);
        }
    }

    /// <summary>
    /// Shares the anchor to an <see cref="OVRSpaceUser"/>.
    /// The specified user will be able to download, track, and share specified anchors.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous; use the returned <see cref="OVRTask{TResult}"/> to be notified of completion.
    /// </remarks>
    /// <param name="user1">An Oculus user to share the anchor with.</param>
    /// <param name="user2">An Oculus user to share the anchor with.</param>
    /// <param name="user3">An Oculus user to share the anchor with.</param>
    /// <param name="user4">An Oculus user to share the anchor with.</param>
    /// <returns>
    /// An <see cref="OVRTask{TResult}"/> with a <see cref="OperationResult"/> type parameter indicating the success of the share operation.
    /// </returns>
    public OVRTask<OperationResult> ShareAsync(OVRSpaceUser user1, OVRSpaceUser user2, OVRSpaceUser user3,
        OVRSpaceUser user4)
    {
        var userList = OVRObjectPool.List<OVRSpaceUser>();
        userList.Add(user1);
        userList.Add(user2);
        userList.Add(user3);
        userList.Add(user4);
        return ShareAsyncInternal(userList);
    }

    /// <summary>
    /// Shares the anchor to a collection of <see cref="OVRSpaceUser"/>.
    /// Specified users will be able to download, track, and share specified anchors.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous. Use <paramref name="onComplete"/> to be notified of completion.
    /// </remarks>
    /// <param name="users">A collection of Oculus users to share the anchor with.</param>
    /// <param name="onComplete">
    /// Invoked when the share operation completes. May be null. Delegate parameter is
    /// - `OperationResult`: An error code that indicates whether the share operation succeeded or not.
    /// </param>
    public void Share(IEnumerable<OVRSpaceUser> users, Action<OperationResult> onComplete = null)
    {
        var task = ShareAsync(users);
        if (onComplete != null)
        {
            task.ContinueWith(onComplete);
        }
    }

    /// <summary>
    /// Shares the anchor to an <see cref="OVRSpaceUser"/>.
    /// The specified user will be able to download, track, and share specified anchors.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous; use the returned <see cref="OVRTask{TResult}"/> to be notified of completion.
    /// </remarks>
    /// <param name="users">A collection of Oculus users to share the anchor with.</param>
    /// <returns>
    /// An <see cref="OVRTask{TResult}"/> with a <see cref="OperationResult"/> type parameter indicating the success of the share operation.
    /// </returns>
    public OVRTask<OperationResult> ShareAsync(IEnumerable<OVRSpaceUser> users)
    {
        var userList = OVRObjectPool.List<OVRSpaceUser>();
        userList.AddRange(users);
        return ShareAsyncInternal(userList);
    }

    /// <summary>
    /// Shares a collection of <see cref="OVRSpatialAnchor"/> to specified users.
    /// Specified users will be able to download, track, and share specified anchors.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous. Use <paramref name="onComplete"/> to be notified of completion.
    ///
    /// This operation fully succeeds or fails, which means, either all anchors are successfully shared
    /// or the operation fails.
    /// </remarks>
    /// <param name="anchors">The collection of anchors to share.</param>
    /// <param name="users">An array of Oculus users to share these anchors with.</param>
    /// <param name="onComplete">
    /// Invoked when the share operation completes. May be null. Delegate parameter is
    /// - `ICollection&lt;OVRSpatialAnchor&gt;`: The collection of anchors being shared.
    /// - `OperationResult`: An error code that indicates whether the share operation succeeded or not.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="anchors"/> is `null`.</exception>
    public static void Share(ICollection<OVRSpatialAnchor> anchors, ICollection<OVRSpaceUser> users,
        Action<ICollection<OVRSpatialAnchor>, OperationResult> onComplete = null)
    {
        if (anchors == null)
            throw new ArgumentNullException(nameof(anchors));

        using var spaces = ToNativeArray(anchors);

        var handles = new NativeArray<ulong>(users.Count, Allocator.Temp);
        using var disposer = handles;
        int i = 0;
        foreach (var user in users)
        {
            handles[i++] = user._handle;
        }

        var shareResult = OVRPlugin.ShareSpaces(spaces, handles, out var requestId);
        if (shareResult.IsSuccess())
        {
            Development.LogRequest(requestId, $"Sharing {(uint)spaces.Length} spatial anchors...");

            MultiAnchorCompletionDelegates[requestId] = new MultiAnchorDelegatePair
            {
                Anchors = CopyAnchorListIntoListFromPool(anchors),
                Delegate = onComplete
            };
        }
        else
        {
            Development.LogError(
                $"{nameof(OVRPlugin)}.{nameof(OVRPlugin.ShareSpaces)}  failed with error {shareResult}.");
            onComplete?.Invoke(anchors, (OperationResult)shareResult);
        }
    }

    private OVRTask<OperationResult> ShareAsyncInternal(List<OVRSpaceUser> users)
    {
        var shareRequestAnchors = GetListToStoreTheShareRequest(users);
        shareRequestAnchors.Add(this);
        var requestId = Guid.NewGuid();
        AsyncRequestTaskIds[this] = requestId;
        return OVRTask.FromGuid<OperationResult>(requestId);
    }

    private List<OVRSpatialAnchor> GetListToStoreTheShareRequest(List<OVRSpaceUser> users)
    {
        users.Sort((x, y) => x.Id.CompareTo(y.Id));
        foreach (var (shareRequestUsers, shareRequestAnchors) in ShareRequests)
        {
            if (!AreSortedUserListsEqual(users, shareRequestUsers))
            {
                continue;
            }

            // reuse the current request
            return shareRequestAnchors;
        }

        // add a new request
        var anchorList = OVRObjectPool.List<OVRSpatialAnchor>();
        ShareRequests.Add((users, anchorList));
        return anchorList;
    }

    private static bool AreSortedUserListsEqual(IReadOnlyList<OVRSpaceUser> sortedList1,
        IReadOnlyList<OVRSpaceUser> sortedList2)
    {
        if (sortedList1.Count != sortedList2.Count)
        {
            return false;
        }

        for (var i = 0; i < sortedList1.Count; i++)
        {
            if (sortedList1[i].Id != sortedList2[i].Id)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Erases the <see cref="OVRSpatialAnchor"/> from persistent storage.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous. Use <paramref name="onComplete"/> to be notified of completion.
    /// Erasing an <see cref="OVRSpatialAnchor"/> does not destroy the anchor.
    /// </remarks>
    /// <param name="onComplete">
    /// Invoked when the erase operation completes. May be null. Parameters are
    /// - <see cref="OVRSpatialAnchor"/>: The anchor being erased.
    /// - `bool`: A value indicating whether the erase operation succeeded.
    /// </param>
    public void Erase(Action<OVRSpatialAnchor, bool> onComplete = null)
    {
        Erase(_defaultEraseOptions, onComplete);
    }

    /// <summary>
    /// Erases the <see cref="OVRSpatialAnchor"/> from specified storage.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous. Use <paramref name="onComplete"/> to be notified of completion.
    /// Erasing an <see cref="OVRSpatialAnchor"/> does not destroy the anchor.
    /// </remarks>
    /// <param name="eraseOptions">Options how the anchor should be erased.</param>
    /// <param name="onComplete">
    /// Invoked when the erase operation completes. May be null. Parameters are
    /// - <see cref="OVRSpatialAnchor"/>: The anchor being erased.
    /// - `bool`: A value indicating whether the erase operation succeeded.
    /// </param>
    public void Erase(EraseOptions eraseOptions, Action<OVRSpatialAnchor, bool> onComplete = null)
    {
        var task = EraseAsync(eraseOptions);

        if (onComplete != null)
        {
            InvertedCapture<bool, OVRSpatialAnchor>.ContinueTaskWith(task, onComplete, this);
        }
    }

    /// <summary>
    /// Erases the <see cref="OVRSpatialAnchor"/> from specified storage.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous; use the returned <see cref="OVRTask{TResult}"/> to be notified of completion.
    /// Erasing an <see cref="OVRSpatialAnchor"/> does not destroy the anchor.
    /// </remarks>
    /// <returns>
    /// An <see cref="OVRTask{TResult}"/> with a boolean type parameter indicating the success of the erase operation.
    /// </returns>
    public OVRTask<bool> EraseAsync() => EraseAsync(_defaultEraseOptions);

    /// <summary>
    /// Erases the <see cref="OVRSpatialAnchor"/> from specified storage.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous; use the returned <see cref="OVRTask{TResult}"/> to be notified of completion.
    /// Erasing an <see cref="OVRSpatialAnchor"/> does not destroy the anchor.
    /// </remarks>
    /// <param name="eraseOptions">Options for how the anchor should be erased.</param>
    /// <returns>
    /// An <see cref="OVRTask{TResult}"/> with a boolean type parameter indicating the success of the erase operation.
    /// </returns>
    public OVRTask<bool> EraseAsync(EraseOptions eraseOptions) =>
        OVRPlugin.EraseSpace(Space, eraseOptions.Storage.ToSpaceStorageLocation(), out var requestId)
            ? OVRTask.FromRequest<bool>(requestId)
            : OVRTask.FromResult(false);


    private static void ThrowIfBound(Guid uuid)
    {
        if (SpatialAnchors.ContainsKey(uuid))
            throw new InvalidOperationException(
                $"Spatial anchor with uuid {uuid} is already bound to an {nameof(OVRSpatialAnchor)}.");
    }

    // Initializes this component without checking preconditions
    private void InitializeUnchecked(OVRSpace space, Guid uuid)
    {
        SpatialAnchors.Add(uuid, this);
        _requestId = 0;
        Space = space;
        Uuid = uuid;
        OVRPlugin.SetSpaceComponentStatus(Space, OVRPlugin.SpaceComponentType.Locatable, true, 0, out _);
        OVRPlugin.SetSpaceComponentStatus(Space, OVRPlugin.SpaceComponentType.Storable, true, 0, out _);
        OVRPlugin.SetSpaceComponentStatus(Space, OVRPlugin.SpaceComponentType.Sharable, true, 0, out _);

        // Try to update the pose as soon as we can.
        UpdateTransform();
    }

    private void Start()
    {
        _startCalled = true;

        if (Space.Valid)
        {
            Development.Log($"[{Uuid}] Created spatial anchor from existing an existing space.");
        }
        else
        {
            CreateSpatialAnchor();
        }
    }

    private void Update()
    {
        if (Space.Valid)
        {
            UpdateTransform();
        }
    }

    private void LateUpdate()
    {
        SaveBatchAnchors();
        ShareBatchAnchors();
    }

    private static void SaveBatchAnchors()
    {
        foreach (var pair in SaveRequests)
        {
            if (pair.Value.Count == 0)
            {
                continue;
            }

            Save(pair.Value, new SaveOptions { Storage = pair.Key });
            pair.Value.Clear();
        }
    }

    private static void ShareBatchAnchors()
    {
        foreach (var (userList, anchorList) in ShareRequests)
        {
            if (userList.Count > 0 && anchorList.Count > 0)
            {
                Share(anchorList, userList);
            }

            OVRObjectPool.Return(userList);
            OVRObjectPool.Return(anchorList);
        }

        ShareRequests.Clear();
    }

    private void OnDestroy()
    {
        if (Space.Valid)
        {
            OVRPlugin.DestroySpace(Space);
        }

        SpatialAnchors.Remove(Uuid);
    }

    private OVRPose GetTrackingSpacePose()
    {
        var mainCamera = Camera.main;
        if (mainCamera)
        {
            return transform.ToTrackingSpacePose(mainCamera);
        }

        Development.LogWarning($"No main camera found. Using world-space pose.");
        return transform.ToOVRPose(isLocal: false);
    }

    private void CreateSpatialAnchor()
    {
        var created = OVRPlugin.CreateSpatialAnchor(new OVRPlugin.SpatialAnchorCreateInfo
        {
            BaseTracking = OVRPlugin.GetTrackingOriginType(),
            PoseInSpace = GetTrackingSpacePose().ToPosef(),
            Time = OVRPlugin.GetTimeInSeconds(),
        }, out _requestId);

        OVRTelemetry.Client.MarkerStart(OVRTelemetryConstants.Scene.MarkerId.SpatialAnchorCreate,
            _requestId.GetHashCode());

        if (created)
        {
            Development.LogRequest(_requestId, $"Creating spatial anchor...");
            CreationRequests[_requestId] = this;
        }
        else
        {
            OVRTelemetry.Client.MarkerEnd(OVRTelemetryConstants.Scene.MarkerId.SpatialAnchorCreate,
                OVRPlugin.Qpl.ResultType.Fail, _requestId.GetHashCode());
            Development.LogError(
                $"{nameof(OVRPlugin)}.{nameof(OVRPlugin.CreateSpatialAnchor)} failed. Destroying {nameof(OVRSpatialAnchor)} component.");
            Destroy(this);
        }
    }

    internal static bool TryGetPose(OVRSpace space, out OVRPose pose)
    {
        if (!OVRPlugin.TryLocateSpace(space, OVRPlugin.GetTrackingOriginType(), out var posef))
        {
            pose = OVRPose.identity;
            return false;
        }

        pose = posef.ToOVRPose();
        var mainCamera = Camera.main;
        if (mainCamera)
        {
            pose = pose.ToWorldSpacePose(mainCamera);
        }

        return true;
    }

    private void UpdateTransform()
    {
        if (TryGetPose(Space, out var pose))
        {
            transform.SetPositionAndRotation(pose.position, pose.orientation);
        }
    }

    private static bool TryExtractValue<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key, out TValue value) =>
        dict.TryGetValue(key, out value) && dict.Remove(key);

    private struct MultiAnchorDelegatePair
    {
        public List<OVRSpatialAnchor> Anchors;
        public Action<ICollection<OVRSpatialAnchor>, OperationResult> Delegate;
    }

    internal static readonly Dictionary<Guid, OVRSpatialAnchor> SpatialAnchors =
        new Dictionary<Guid, OVRSpatialAnchor>();

    private static readonly Dictionary<ulong, OVRSpatialAnchor> CreationRequests =
        new Dictionary<ulong, OVRSpatialAnchor>();

    private static readonly Dictionary<OVRSpace.StorageLocation, List<OVRSpatialAnchor>> SaveRequests =
        new Dictionary<OVRSpace.StorageLocation, List<OVRSpatialAnchor>>
        {
            { OVRSpace.StorageLocation.Cloud, new List<OVRSpatialAnchor>() },
            { OVRSpace.StorageLocation.Local, new List<OVRSpatialAnchor>() },
        };

    private static readonly Dictionary<OVRSpatialAnchor, Guid> AsyncRequestTaskIds =
        new Dictionary<OVRSpatialAnchor, Guid>();

    private static readonly List<(List<OVRSpaceUser>, List<OVRSpatialAnchor>)> ShareRequests =
        new List<(List<OVRSpaceUser>, List<OVRSpatialAnchor>)>();

    private static readonly Dictionary<ulong, MultiAnchorDelegatePair> MultiAnchorCompletionDelegates =
        new Dictionary<ulong, MultiAnchorDelegatePair>();

    private static readonly List<UnboundAnchor> UnboundAnchorBuffer = new List<UnboundAnchor>();

    private static readonly OVRPlugin.SpaceComponentType[] ComponentTypeBuffer = new OVRPlugin.SpaceComponentType[32];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void InitializeOnLoad()
    {
        CreationRequests.Clear();
        MultiAnchorCompletionDelegates.Clear();
        UnboundAnchorBuffer.Clear();
        SpatialAnchors.Clear();
    }

    static OVRSpatialAnchor()
    {
        OVRManager.SpatialAnchorCreateComplete += OnSpatialAnchorCreateComplete;
        OVRManager.SpaceSaveComplete += OnSpaceSaveComplete;
        OVRManager.SpaceListSaveComplete += OnSpaceListSaveComplete;
        OVRManager.ShareSpacesComplete += OnShareSpacesComplete;
        OVRManager.SpaceEraseComplete += OnSpaceEraseComplete;
        OVRManager.SpaceQueryComplete += OnSpaceQueryComplete;
        OVRManager.SpaceSetComponentStatusComplete += OnSpaceSetComponentStatusComplete;
    }

    private static void InvokeMultiAnchorDelegate(ulong requestId, OperationResult result,
        MultiAnchorActionType actionType)
    {
        if (!TryExtractValue(MultiAnchorCompletionDelegates, requestId, out var value))
        {
            return;
        }

        if (actionType == MultiAnchorActionType.Save)
        {
            OVRTelemetry.Client.MarkerEnd(OVRTelemetryConstants.Scene.MarkerId.SpatialAnchorSave,
                result == OperationResult.Success ? OVRPlugin.Qpl.ResultType.Success : OVRPlugin.Qpl.ResultType.Fail,
                requestId.GetHashCode());
        }

        value.Delegate?.Invoke(value.Anchors, result);

        try
        {
            foreach (var anchor in value.Anchors)
            {
                switch (actionType)
                {
                    case MultiAnchorActionType.Save:
                    {
                        if (result != OperationResult.Success)
                        {
                            Development.LogError(
                                $"[{anchor.Uuid}] {nameof(OVRPlugin)}.{nameof(OVRPlugin.SaveSpaceList)} failed with result: {result}.");
                        }

                        if (AsyncRequestTaskIds.TryGetValue(anchor, out var taskId))
                        {
                            AsyncRequestTaskIds.Remove(anchor);
                            OVRTask.GetExisting<bool>(taskId).SetResult(result == OperationResult.Success);
                        }

                        break;
                    }
                    case MultiAnchorActionType.Share:
                    {
                        if (result != OperationResult.Success)
                        {
                            Development.LogError(
                                $"[{anchor.Uuid}] {nameof(OVRPlugin)}.{nameof(OVRPlugin.ShareSpaces)} failed with result: {result}.");
                        }

                        if (AsyncRequestTaskIds.TryGetValue(anchor, out var taskId))
                        {
                            AsyncRequestTaskIds.Remove(anchor);
                            OVRTask.GetExisting<OperationResult>(taskId).SetResult(result);
                        }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null);
                }
            }
        }
        finally
        {
            OVRObjectPool.Return(value.Anchors);
        }
    }

    private static void OnSpatialAnchorCreateComplete(ulong requestId, bool success, OVRSpace space, Guid uuid)
    {
        Development.LogRequestResult(requestId, success,
            $"[{uuid}] Spatial anchor created.",
            $"Failed to create spatial anchor. Destroying {nameof(OVRSpatialAnchor)} component.");

        if (!TryExtractValue(CreationRequests, requestId, out var anchor)) return;

        OVRTelemetry.Client.MarkerEnd(OVRTelemetryConstants.Scene.MarkerId.SpatialAnchorCreate,
            success ? OVRPlugin.Qpl.ResultType.Success : OVRPlugin.Qpl.ResultType.Fail,
            requestId.GetHashCode());

        if (success && anchor)
        {
            // All good; complete setup of OVRSpatialAnchor component.
            anchor.InitializeUnchecked(space, uuid);
            return;
        }

        if (success && !anchor)
        {
            // Creation succeeded, but the OVRSpatialAnchor component was destroyed before the callback completed.
            OVRPlugin.DestroySpace(space);
        }
        else if (!success && anchor)
        {
            // The OVRSpatialAnchor component exists but creation failed.
            Destroy(anchor);
        }
        // else if creation failed and the OVRSpatialAnchor component was destroyed, nothing to do.
    }

    private static void OnSpaceSaveComplete(ulong requestId, OVRSpace space, bool result, Guid uuid)
    {
        Development.LogRequestResult(requestId, result,
            $"[{uuid}] Saved.",
            $"[{uuid}] Save failed.");
    }

    private static void OnSpaceEraseComplete(ulong requestId, bool result, Guid uuid,
        OVRPlugin.SpaceStorageLocation location)
    {
        Development.LogRequestResult(requestId, result,
            $"[{uuid}] Erased.",
            $"[{uuid}] Erase failed.");
    }

    /// <summary>
    /// Options for loading unbound spatial anchors used by <see cref="OVRSpatialAnchor.LoadUnboundAnchors"/>.
    /// </summary>
    /// <example>
    /// This example shows how to create LoadOptions for loading anchors when given a set of UUIDs.
    /// <code>
    /// OVRSpatialAnchor.LoadOptions options = new OVRSpatialAnchor.LoadOptions
    /// {
    ///     StorageLocation = OVRSpace.StorageLocation.Local,
    ///     Timeout = 0,
    ///     Uuids = savedAnchorUuids
    /// };
    /// </code>
    /// </example>
    public struct LoadOptions
    {
        /// <summary>
        /// The maximum number of uuids that may be present in the <see cref="Uuids"/> collection.
        /// </summary>
        public const int MaxSupported = OVRSpaceQuery.Options.MaxUuidCount;

        /// <summary>
        /// The storage location from which to query spatial anchors.
        /// </summary>
        public OVRSpace.StorageLocation StorageLocation { get; set; }

        /// <summary>
        /// (Obsolete) The maximum number of anchors to query.
        /// </summary>
        /// <remarks>
        /// In prior SDK versions, it was mandatory to set this property to receive any
        /// results. However, this property is now obsolete. If <see cref="MaxAnchorCount"/> is zero,
        /// i.e., the default initialized value, it will automatically be set to the count of
        /// <see cref="Uuids"/>.
        ///
        /// If non-zero, the number of anchors in the result will be limited to
        /// <see cref="MaxAnchorCount"/>, preserving the previous behavior.
        /// </remarks>
        [Obsolete(
            "This property is no longer required. MaxAnchorCount will be automatically set to the number of uuids to load.")]
        public int MaxAnchorCount { get; set; }

        /// <summary>
        /// The timeout, in seconds, for the query operation.
        /// </summary>
        /// <remarks>
        /// A value of zero indicates no timeout.
        /// </remarks>
        public double Timeout { get; set; }

        /// <summary>
        /// The set of spatial anchors to query, identified by their UUIDs.
        /// </summary>
        /// <remarks>
        /// The UUIDs are copied by the <see cref="OVRSpatialAnchor.LoadUnboundAnchors"/> method and no longer
        /// referenced internally afterwards.
        ///
        /// You must supply a list of UUIDs. <see cref="OVRSpatialAnchor.LoadUnboundAnchors"/> will throw if this
        /// property is null.
        /// </remarks>
        /// <exception cref="System.ArgumentException">Thrown if <see cref="Uuids"/> contains more
        ///     than <see cref="MaxSupported"/> elements.</exception>
        public IReadOnlyList<Guid> Uuids
        {
            get => _uuids;
            set
            {
                if (value?.Count > OVRSpaceQuery.Options.MaxUuidCount)
                    throw new ArgumentException(
                        $"There must not be more than {MaxSupported} UUIDs (new value contains {value.Count} UUIDs).",
                        nameof(value));

                _uuids = value;
            }
        }

        private IReadOnlyList<Guid> _uuids;


        internal OVRSpaceQuery.Options ToQueryOptions() => new OVRSpaceQuery.Options
        {
            Location = StorageLocation,
#pragma warning disable CS0618
            MaxResults = MaxAnchorCount == 0 ? Uuids?.Count ?? 0 : MaxAnchorCount,
#pragma warning restore CS0618
            Timeout = Timeout,
            UuidFilter = Uuids,
            QueryType = OVRPlugin.SpaceQueryType.Action,
            ActionType = OVRPlugin.SpaceQueryActionType.Load,
        };
    }

    /// <summary>
    /// A spatial anchor that has not been bound to an <see cref="OVRSpatialAnchor"/>.
    /// </summary>
    /// <remarks>
    /// Use this object to bind an unbound spatial anchor to an <see cref="OVRSpatialAnchor"/>.
    /// </remarks>
    public readonly struct UnboundAnchor
    {
        internal readonly OVRSpace _space;

        /// <summary>
        /// The universally unique identifier associated with this anchor.
        /// </summary>
        public Guid Uuid { get; }

        /// <summary>
        /// Whether the anchor has been localized.
        /// </summary>
        /// <remarks>
        /// Prior to localization, the anchor's <see cref="Pose"/> cannot be determined.
        /// </remarks>
        /// <seealso cref="Localized"/>
        /// <seealso cref="Localizing"/>
        public bool Localized => OVRPlugin.GetSpaceComponentStatus(_space, OVRPlugin.SpaceComponentType.Locatable,
            out var enabled, out _) && enabled;

        /// <summary>
        /// Whether the anchor is in the process of being localized.
        /// </summary>
        /// <seealso cref="Localized"/>
        /// <seealso cref="Localize"/>
        public bool Localizing => OVRPlugin.GetSpaceComponentStatus(_space, OVRPlugin.SpaceComponentType.Locatable,
            out var enabled, out var pending) && !enabled && pending;

        /// <summary>
        /// The world space pose of the spatial anchor.
        /// </summary>
        public Pose Pose
        {
            get
            {
                if (!TryGetPose(_space, out var pose))
                    throw new InvalidOperationException(
                        $"[{Uuid}] Anchor must be localized before obtaining its pose.");

                return new Pose(pose.position, pose.orientation);
            }
        }

        /// <summary>
        /// Localizes an anchor.
        /// </summary>
        /// <remarks>
        /// The delegate supplied to <see cref="OVRSpatialAnchor.LoadUnboundAnchors"/> receives an array of unbound
        /// spatial anchors. You can choose whether to localize each one and be notified when localization completes.
        ///
        /// The <paramref name="onComplete"/> delegate receives two arguments:
        /// - `bool`: Whether localization was successful
        /// - <see cref="UnboundAnchor"/>: The anchor to bind
        ///
        /// Upon successful localization, your delegate should instantiate an <see cref="OVRSpatialAnchor"/>, then bind
        /// the <see cref="UnboundAnchor"/> to the <see cref="OVRSpatialAnchor"/> by calling
        /// <see cref="UnboundAnchor.BindTo"/>. Once an <see cref="UnboundAnchor"/> is bound to an
        /// <see cref="OVRSpatialAnchor"/>, it cannot be used again; that is, it cannot be bound to multiple
        /// <see cref="OVRSpatialAnchor"/> components.
        /// </remarks>
        /// <param name="onComplete">A delegate invoked when localization completes (which may fail). The delegate
        /// receives two arguments:
        /// - <see cref="UnboundAnchor"/>: The anchor to bind
        /// - `bool`: Whether localization was successful
        /// </param>
        /// <param name="timeout">The timeout, in seconds, to attempt localization, or zero to indicate no timeout.</param>
        /// <exception cref="InvalidOperationException">Thrown if
        /// - The anchor does not support localization, e.g., because it is invalid.
        /// - The anchor has already been localized.
        /// - The anchor is being localized, e.g., because <see cref="Localize"/> was previously called.
        /// </exception>
        public void Localize(Action<UnboundAnchor, bool> onComplete = null, double timeout = 0)
        {
            var task = LocalizeAsync(timeout);

            if (onComplete != null)
            {
                InvertedCapture<bool, UnboundAnchor>.ContinueTaskWith(task, onComplete, this);
            }
        }

        private void ValidateLocalization()
        {
            if (!OVRPlugin.GetSpaceComponentStatus(_space, OVRPlugin.SpaceComponentType.Locatable, out var enabled,
                    out var changePending))
                throw new InvalidOperationException($"[{Uuid}] {nameof(UnboundAnchor)} does not support localization.");

            if (enabled)
                throw new InvalidOperationException($"[{Uuid}] Anchor has already been localized.");

            if (changePending)
                throw new InvalidOperationException($"[{Uuid}] Anchor is currently being localized.");
        }

        /// <summary>
        /// Localizes an anchor.
        /// </summary>
        /// <remarks>
        /// The delegate supplied to <see cref="OVRSpatialAnchor.LoadUnboundAnchors"/> receives an array of unbound
        /// spatial anchors. You can choose whether to localize each one and be notified when localization completes.
        ///
        /// Upon successful localization, your delegate should instantiate an <see cref="OVRSpatialAnchor"/>, then bind
        /// the <see cref="UnboundAnchor"/> to the <see cref="OVRSpatialAnchor"/> by calling
        /// <see cref="UnboundAnchor.BindTo"/>. Once an <see cref="UnboundAnchor"/> is bound to an
        /// <see cref="OVRSpatialAnchor"/>, it cannot be used again; that is, it cannot be bound to multiple
        /// <see cref="OVRSpatialAnchor"/> components.
        /// </remarks>
        /// <param name="timeout">The timeout, in seconds, to attempt localization, or zero to indicate no timeout.</param>
        /// <returns>
        /// An <see cref="OVRTask{TResult}"/> with a boolean type parameter indicating the success of the localization.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if
        /// - The anchor does not support localization, e.g., because it is invalid.
        /// - The anchor has already been localized.
        /// - The anchor is being localized, e.g., because <see cref="Localize"/> was previously called.
        /// </exception>
        public OVRTask<bool> LocalizeAsync(double timeout = 0)
        {
            ValidateLocalization();

            var setStatus = OVRPlugin.SetSpaceComponentStatus(_space, OVRPlugin.SpaceComponentType.Locatable, true,
                timeout, out var requestId);

            if (!setStatus)
            {
                Development.LogError($"[{Uuid}] {nameof(OVRPlugin.SetSpaceComponentStatus)} failed.");
                return OVRTask.FromResult(false);
            }

            Development.LogRequest(requestId,
                $"[{Uuid}] {nameof(OVRPlugin.SetSpaceComponentStatus)} enable {nameof(OVRPlugin.SpaceComponentType.Locatable)}.");

            AddStorableAndShareableComponents();

            return OVRTask.FromRequest<bool>(requestId);
        }

        private void AddStorableAndShareableComponents()
        {
            OVRPlugin.SetSpaceComponentStatus(_space, OVRPlugin.SpaceComponentType.Storable, true, 0, out _);
            OVRPlugin.SetSpaceComponentStatus(_space, OVRPlugin.SpaceComponentType.Sharable, true, 0, out _);
        }

        /// <summary>
        /// Binds an unbound anchor to an <see cref="OVRSpatialAnchor"/> component.
        /// </summary>
        /// <remarks>
        /// Use this to bind an unbound anchor to an <see cref="OVRSpatialAnchor"/>. After <see cref="BindTo"/> is used
        /// to bind an <see cref="UnboundAnchor"/> to an <see cref="OVRSpatialAnchor"/>, the
        /// <see cref="UnboundAnchor"/> is no longer valid; that is, it cannot be bound to another
        /// <see cref="OVRSpatialAnchor"/>.
        /// </remarks>
        /// <param name="spatialAnchor">The component to which this unbound anchor should be bound.</param>
        /// <exception cref="InvalidOperationException">Thrown if this <see cref="UnboundAnchor"/> does not refer to a valid anchor.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="spatialAnchor"/> is `null`.</exception>
        /// <exception cref="ArgumentException">Thrown if an anchor is already bound to <paramref name="spatialAnchor"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="spatialAnchor"/> is pending creation (see <see cref="OVRSpatialAnchor.PendingCreation"/>).</exception>
        /// <exception cref="InvalidOperationException">Thrown if this <see cref="UnboundAnchor"/> is already bound to an <see cref="OVRSpatialAnchor"/>.</exception>
        public void BindTo(OVRSpatialAnchor spatialAnchor)
        {
            if (!_space.Valid)
                throw new InvalidOperationException($"{nameof(UnboundAnchor)} does not refer to a valid anchor.");

            if (spatialAnchor == null)
                throw new ArgumentNullException(nameof(spatialAnchor));

            if (spatialAnchor.Created)
                throw new ArgumentException(
                    $"Cannot bind {Uuid} to {nameof(spatialAnchor)} because {nameof(spatialAnchor)} is already bound to {spatialAnchor.Uuid}.",
                    nameof(spatialAnchor));

            if (spatialAnchor.PendingCreation)
                throw new ArgumentException(
                    $"Cannot bind {Uuid} to {nameof(spatialAnchor)} because {nameof(spatialAnchor)} is being used to create a new spatial anchor.",
                    nameof(spatialAnchor));

            ThrowIfBound(Uuid);

            spatialAnchor.InitializeUnchecked(_space, Uuid);
        }

        internal UnboundAnchor(OVRSpace space, Guid uuid)
        {
            _space = space;
            Uuid = uuid;
        }
    }

    /// <summary>
    /// Performs a query for anchors with the specified <paramref name="options"/>.
    /// </summary>
    /// <remarks>
    /// Use this method to find anchors that were previously persisted with
    /// <see cref="Save(Action{OVRSpatialAnchor, bool}"/>. The query is asynchronous; when the query completes,
    /// <paramref name="onComplete"/> is invoked with an array of <see cref="UnboundAnchor"/>s for which tracking
    /// may be requested.
    /// </remarks>
    /// <param name="options">Options that affect the query.</param>
    /// <param name="onComplete">A delegate invoked when the query completes. The delegate accepts one argument:
    /// - `UnboundAnchor[]`: An array of unbound anchors.
    ///
    /// If the operation fails, <paramref name="onComplete"/> is invoked with `null`.</param>
    /// <returns>Returns `true` if the operation could be initiated; otherwise `false`.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="onComplete"/> is `null`.</exception>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="LoadOptions.Uuids"/> of <paramref name="options"/> is `null`.</exception>
    public static bool LoadUnboundAnchors(LoadOptions options, Action<UnboundAnchor[]> onComplete)
    {
        var task = LoadUnboundAnchorsAsync(options);
        task.ContinueWith(onComplete);
        return task.IsPending;
    }

    /// <summary>
    /// Performs a query for anchors with the specified <paramref name="options"/>.
    /// </summary>
    /// <remarks>
    /// Use this method to find anchors that were previously persisted with
    /// <see cref="Save(Action{OVRSpatialAnchor, bool}"/>. The query is asynchronous; when the query completes,
    /// the returned <see cref="OVRTask{TResult}"/> will contain an array of <see cref="UnboundAnchor"/>s for which tracking
    /// may be requested.
    /// </remarks>
    /// <param name="options">Options that affect the query.</param>
    /// <returns>
    /// An <see cref="OVRTask{TResult}"/> with a <see cref="T:UnboundAnchor[]"/> type parameter containing the loaded unbound anchors.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="LoadOptions.Uuids"/> of <paramref name="options"/> is `null`.</exception>
    public static OVRTask<UnboundAnchor[]> LoadUnboundAnchorsAsync(LoadOptions options)
    {
        if (options.Uuids == null)
        {
            throw new InvalidOperationException($"{nameof(LoadOptions)}.{nameof(LoadOptions.Uuids)} must not be null.");
        }

        if (!options.ToQueryOptions().TryQuerySpaces(out var requestId))
        {
            Development.LogError($"{nameof(OVRPlugin.QuerySpaces)} failed.");
            return OVRTask.FromResult<UnboundAnchor[]>(null);
        }

        Development.LogRequest(requestId, $"{nameof(OVRPlugin.QuerySpaces)}: Query created.");
        return OVRTask.FromRequest<UnboundAnchor[]>(requestId);
    }


    private static void OnSpaceQueryComplete(ulong requestId, bool queryResult)
    {
        Development.LogRequestResult(requestId, queryResult,
            $"{nameof(OVRPlugin.QuerySpaces)}: Query succeeded.",
            $"{nameof(OVRPlugin.QuerySpaces)}: Query failed.");

        var hasPendingTask = OVRTask.GetExisting<UnboundAnchor[]>(requestId).IsPending;

        if (!hasPendingTask)
        {
            return;
        }

        OVRTelemetry.Client.MarkerEnd(OVRTelemetryConstants.Scene.MarkerId.SpatialAnchorQuery,
            queryResult ? OVRPlugin.Qpl.ResultType.Success : OVRPlugin.Qpl.ResultType.Fail,
            requestId.GetHashCode());

        if (!queryResult)
        {
            OVRTask.GetExisting<UnboundAnchor[]>(requestId).SetResult(null);
            return;
        }

        if (OVRPlugin.RetrieveSpaceQueryResults(requestId, out var results, Allocator.Temp))
        {
            Development.Log(
                $"{nameof(OVRPlugin.RetrieveSpaceQueryResults)}({requestId}): Retrieved {results.Length} results.");
        }
        else
        {
            Development.LogError(
                $"{nameof(OVRPlugin.RetrieveSpaceQueryResults)}({requestId}): Failed to retrieve results.");
            OVRTask.GetExisting<UnboundAnchor[]>(requestId).SetResult(null);
            return;
        }

        using var disposer = results;

        UnboundAnchorBuffer.Clear();
        foreach (var result in results)
        {
            PopulateUnbound(result.uuid, result.space);
        }

        Development.Log(
            $"Invoking callback with {UnboundAnchorBuffer.Count} unbound anchor{(UnboundAnchorBuffer.Count == 1 ? "" : "s")}.");

        var unboundAnchors = UnboundAnchorBuffer.Count == 0
            ? Array.Empty<UnboundAnchor>()
            : UnboundAnchorBuffer.ToArray();
        OVRTask.GetExisting<UnboundAnchor[]>(requestId).SetResult(unboundAnchors);
    }

    private static void PopulateUnbound(Guid uuid, UInt64 space)
    {
        if (SpatialAnchors.ContainsKey(uuid))
        {
            Development.Log($"[{uuid}] Anchor is already bound to an {nameof(OVRSpatialAnchor)}. Ignoring.");
            return;
        }

        // See if it supports localization
        if (!OVRPlugin.EnumerateSpaceSupportedComponents(space, out var numSupportedComponents,
                ComponentTypeBuffer))
        {
            Development.LogWarning($"[{uuid}] Unable to enumerate supported component types. Ignoring.");
            return;
        }

        var supportsLocatable = false;
        for (var i = 0; i < numSupportedComponents; i++)
        {
            supportsLocatable |= ComponentTypeBuffer[i] == OVRPlugin.SpaceComponentType.Locatable;
        }

#if DEVELOPMENT_BUILD
        var supportedComponentTypesMsg =
            $"[{uuid}] Supports {numSupportedComponents} component type(s): {(numSupportedComponents == 0 ? "(none)" : string.Join(", ", ComponentTypeBuffer.Take((int)numSupportedComponents).Select(c => c.ToString())))}";
#endif

        if (!supportsLocatable)
        {
#if DEVELOPMENT_BUILD
            Development.Log($"{supportedComponentTypesMsg} -- ignoring because it does not support localization.");
#endif
            return;
        }

        if (!OVRPlugin.GetSpaceComponentStatus(space, OVRPlugin.SpaceComponentType.Locatable, out var enabled,
                out var changePending))
        {
#if DEVELOPMENT_BUILD
            Development.Log($"{supportedComponentTypesMsg} -- ignoring because failed to get localization.");
#endif
            return;
        }
#if DEVELOPMENT_BUILD
        Development.Log($"{supportedComponentTypesMsg}.");
#endif

        Debug.Log($"{uuid}: locatable enabled? {enabled} changePending? {changePending}");

        UnboundAnchorBuffer.Add(new UnboundAnchor(space, uuid));
    }


    private static void OnSpaceSetComponentStatusComplete(ulong requestId, bool result, OVRSpace space, Guid uuid,
        OVRPlugin.SpaceComponentType componentType, bool enabled)
    {
        Development.LogRequestResult(requestId, result,
            $"[{uuid}] {componentType} {(enabled ? "enabled" : "disabled")}.",
            $"[{uuid}] Failed to set {componentType} status.");

        if (componentType == OVRPlugin.SpaceComponentType.Locatable && SpatialAnchors.TryGetValue(uuid, out var anchor))
        {
            anchor.OnLocalize?.Invoke(enabled ? OperationResult.Success : OperationResult.Failure);
        }
    }

    private enum MultiAnchorActionType
    {
        Save,
        Share
    }

    private static void OnSpaceListSaveComplete(ulong requestId, OperationResult result)
    {
        Development.LogRequestResult(requestId, result >= 0,
            $"Spaces saved.",
            $"Spaces save failed with error {result}.");

        InvokeMultiAnchorDelegate(requestId, result, MultiAnchorActionType.Save);
    }

    private static void OnShareSpacesComplete(ulong requestId, OperationResult result)
    {
        Development.LogRequestResult(requestId, result >= 0,
            $"Spaces shared.",
            $"Spaces share failed with error {result}.");

        InvokeMultiAnchorDelegate(requestId, result, MultiAnchorActionType.Share);
    }

    private static class Development
    {
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void Log(string message) => Debug.Log($"[{nameof(OVRSpatialAnchor)}] {message}");

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void LogWarning(string message) => Debug.LogWarning($"[{nameof(OVRSpatialAnchor)}] {message}");

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void LogError(string message) => Debug.LogError($"[{nameof(OVRSpatialAnchor)}] {message}");

#if DEVELOPMENT_BUILD
        private static readonly HashSet<ulong> _requests = new HashSet<ulong>();
#endif // DEVELOPMENT_BUILD

        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogRequest(ulong requestId, string message)
        {
#if DEVELOPMENT_BUILD
            _requests.Add(requestId);
#endif // DEVELOPMENT_BUILD
            Log($"({requestId}) {message}");
        }

        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogRequestResult(ulong requestId, bool result, string successMessage, string failureMessage)
        {
#if DEVELOPMENT_BUILD
            // Not a request we're tracking
            if (!_requests.Remove(requestId)) return;
#endif // DEVELOPMENT_BUILD
            if (result)
            {
                Log($"({requestId}) {successMessage}");
            }
            else
            {
                LogError($"({requestId}) {failureMessage}");
            }
        }
    }

    /// <summary>
    /// Represents options for saving <see cref="OVRSpatialAnchor"/>.
    /// </summary>
    public struct SaveOptions
    {
        /// <summary>
        /// Location where <see cref="OVRSpatialAnchor"/> will be saved.
        /// </summary>
        public OVRSpace.StorageLocation Storage;
    }

    /// <summary>
    /// Represents options for erasing <see cref="OVRSpatialAnchor"/>.
    /// </summary>
    public struct EraseOptions
    {
        /// <summary>
        /// Location from where <see cref="OVRSpatialAnchor"/> will be erased.
        /// </summary>
        public OVRSpace.StorageLocation Storage;
    }

    public enum OperationResult
    {
        /// <summary>Operation succeeded.</summary>
        Success = 0,

        /// <summary>Operation failed.</summary>
        Failure = -1000,

        /// <summary>Saving anchors to cloud storage is not permitted by the user.</summary>
        Failure_SpaceCloudStorageDisabled = -2000,

        /// <summary>
        /// The user was able to download the anchors, but the device was unable to localize
        /// itself in the spatial data received from the sharing device.
        /// </summary>
        Failure_SpaceMappingInsufficient = -2001,

        /// <summary>
        /// The user was able to download the anchors, but the device was unable to localize them.
        /// </summary>
        Failure_SpaceLocalizationFailed = -2002,

        /// <summary>Network operation timed out.</summary>
        Failure_SpaceNetworkTimeout = -2003,

        /// <summary>Network operation failed.</summary>
        Failure_SpaceNetworkRequestFailed = -2004,
    }

    /// <summary>
    /// This struct helped inverting callback signature
    /// when using OVRTasks. OVRTasks expect <c>Action{TResult, TCapture}</c> signature
    /// but public API requires <c>Action{TCapture, TResult}</c> signature.
    /// </summary>
    private readonly struct InvertedCapture<TResult, TCapture>
    {
        private static readonly Action<TResult, InvertedCapture<TResult, TCapture>> Delegate = Invoke;

        private readonly TCapture _capture;
        private readonly Action<TCapture, TResult> _callback;

        private InvertedCapture(Action<TCapture, TResult> callback, TCapture capture)
        {
            _callback = callback;
            _capture = capture;
        }

        private static void Invoke(TResult result, InvertedCapture<TResult, TCapture> invertedCapture)
        {
            invertedCapture._callback?.Invoke(invertedCapture._capture, result);
        }

        public static void ContinueTaskWith(OVRTask<TResult> task, Action<TCapture, TResult> onCompleted,
            TCapture state)
        {
            task.ContinueWith(Delegate, new InvertedCapture<TResult, TCapture>(onCompleted, state));
        }
    }

}

/// <summary>
/// Represents a user for purposes of sharing scene anchors
/// </summary>
public struct OVRSpaceUser : IDisposable
{
    internal ulong _handle;

    /// <summary>
    /// Checks if the user is valid
    /// </summary>
    public bool Valid => _handle != 0 && Id != 0;

    /// <summary>
    /// Creates a space user handle for given Facebook user ID
    /// </summary>
    /// <param name="spaceUserId">The Facebook user ID obtained from the other party over the network</param>
    public OVRSpaceUser(ulong spaceUserId)
    {
        OVRPlugin.CreateSpaceUser(spaceUserId, out _handle);
    }

    /// <summary>
    /// The user ID associated with this <see cref="OVRSpaceUser"/>.
    /// </summary>
    public ulong Id => OVRPlugin.GetSpaceUserId(_handle, out var userId) ? userId : 0;

    /// <summary>
    /// Disposes of the <see cref="OVRSpaceUser"/>.
    /// </summary>
    /// <remarks>
    /// This method does not destroy the user account. It disposes the handle used to reference it.
    /// </remarks>
    public void Dispose()
    {
        OVRPlugin.DestroySpaceUser(_handle);
        _handle = 0;
    }
}
