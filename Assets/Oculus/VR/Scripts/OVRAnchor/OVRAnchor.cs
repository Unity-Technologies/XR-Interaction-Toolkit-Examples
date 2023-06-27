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
using Unity.Collections;
using UnityEngine;
using static OVRPlugin;

/// <summary>
/// Represents an anchor.
/// </summary>
/// <remarks>
/// Scenes anchors are uniquely identified with their <see cref="Uuid"/>.
/// <para>You may dispose of an anchor by calling their <see cref="Dispose"/> method.</para>
/// </remarks>
public readonly struct OVRAnchor : IEquatable<OVRAnchor>, IDisposable
{
    #region Static

    public static readonly OVRAnchor Null = new OVRAnchor(0, Guid.Empty);

    internal static OVRPlugin.SpaceQueryInfo GetQueryInfo(SpaceComponentType type,
        OVRSpace.StorageLocation location, int maxResults, double timeout) => new OVRSpaceQuery.Options
    {
        QueryType = OVRPlugin.SpaceQueryType.Action,
        ActionType = OVRPlugin.SpaceQueryActionType.Load,
        ComponentFilter = type,
        Location = location,
        Timeout = timeout,
        MaxResults = maxResults,
    }.ToQueryInfo();

    internal static OVRPlugin.SpaceQueryInfo GetQueryInfo(IEnumerable<Guid> uuids,
        OVRSpace.StorageLocation location, double timeout) => new OVRSpaceQuery.Options
    {
        QueryType = OVRPlugin.SpaceQueryType.Action,
        ActionType = OVRPlugin.SpaceQueryActionType.Load,
        UuidFilter = uuids,
        Location = location,
        Timeout = timeout,
        MaxResults = OVRSpaceQuery.Options.MaxUuidCount,
    }.ToQueryInfo();


    internal static OVRTask<bool> FetchAnchorsAsync(SpaceComponentType type, IList<OVRAnchor> anchors,
        OVRSpace.StorageLocation location = OVRSpace.StorageLocation.Local,
        int maxResults = OVRSpaceQuery.Options.MaxUuidCount, double timeout = 0.0)
        => FetchAnchors(anchors, GetQueryInfo(type, location, maxResults, timeout));

    /// <summary>
    /// Asynchronous method that fetches anchors with a specific component.
    /// </summary>
    /// <typeparam name="T">The type of component the fetched anchor must have.</typeparam>
    /// <param name="anchors">IList that will get cleared and populated with the requested anchors.</param>s
    /// <param name="location">Storage location to query</param>
    /// <param name="maxResults">The maximum number of results the query can return</param>
    /// <param name="timeout">Timeout in seconds for the query. Zero indicates the query does not timeout.</param>
    /// <remarks>Dispose of the returned <see cref="OVRTask{T}"/> if you don't use the results</remarks>
    /// <returns>An <see cref="OVRTask{T}"/> that will eventually let you test if the fetch was successful or not.
    /// If the result is true, then the <see cref="anchors"/> parameter has been populated with the requested anchors.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="anchors"/> is `null`.</exception>
    public static OVRTask<bool> FetchAnchorsAsync<T>(IList<OVRAnchor> anchors,
        OVRSpace.StorageLocation location = OVRSpace.StorageLocation.Local,
        int maxResults = OVRSpaceQuery.Options.MaxUuidCount, double timeout = 0.0)
        where T : struct, IOVRAnchorComponent<T>
    {
        if (anchors == null)
        {
            throw new ArgumentNullException(nameof(anchors));
        }

        return FetchAnchorsAsync(default(T).Type, anchors, location, maxResults, timeout);
    }

    /// <summary>
    /// Asynchronous method that fetches anchors with specifics uuids.
    /// </summary>
    /// <param name="uuids">Enumerable of uuids that anchors fetched must verify</param>
    /// <param name="anchors">IList that will get cleared and populated with the requested anchors.</param>s
    /// <param name="location">Storage location to query</param>
    /// <param name="timeout">Timeout in seconds for the query. Zero indicates the query does not timeout.</param>
    /// <remarks>Dispose of the returned <see cref="OVRTask{T}"/> if you don't use the results</remarks>
    /// <returns>An <see cref="OVRTask{T}"/> that will eventually let you test if the fetch was successful or not.
    /// If the result is true, then the <see cref="anchors"/> parameter has been populated with the requested anchors.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="uuids"/> is `null`.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="anchors"/> is `null`.</exception>
    public static OVRTask<bool> FetchAnchorsAsync(IEnumerable<Guid> uuids, IList<OVRAnchor> anchors,
        OVRSpace.StorageLocation location = OVRSpace.StorageLocation.Local, double timeout = 0.0)
    {
        if (uuids == null)
        {
            throw new ArgumentNullException(nameof(uuids));
        }

        if (anchors == null)
        {
            throw new ArgumentNullException(nameof(anchors));
        }

        return FetchAnchors(anchors, GetQueryInfo(uuids, location, timeout));
    }


    private static OVRTask<bool> FetchAnchors(IList<OVRAnchor> anchors, OVRPlugin.SpaceQueryInfo queryInfo)
    {
        if (anchors == null)
        {
            throw new ArgumentNullException(nameof(anchors));
        }

        anchors.Clear();

        if (!OVRPlugin.QuerySpaces(queryInfo, out var requestId))
        {
            return OVRTask.FromResult(false);
        }

        var task = OVRTask.FromRequest<bool>(requestId);
        task.SetInternalData(anchors);
        return task;
    }


    internal static void OnSpaceQueryCompleteData(OVRDeserialize.SpaceQueryCompleteData data)
    {
        var requestId = data.RequestId;
        var task = OVRTask.GetExisting<bool>(requestId);
        if (!task.IsPending)
        {
            return;
        }

        if (!task.TryGetInternalData<IList<OVRAnchor>>(out var anchors) || anchors == null)
        {
            task.SetResult(false);
            return;
        }

        if (!OVRPlugin.RetrieveSpaceQueryResults(requestId, out var rawResults, Allocator.Temp))
        {
            task.SetResult(false);
            return;
        }

        foreach (var result in rawResults)
        {
            var anchor = new OVRAnchor(result.space, result.uuid);
            anchors.Add(anchor);
        }

        rawResults.Dispose();

        task.SetResult(true);
    }

    /// <summary>
    /// Creates a new spatial anchor.
    /// </summary>
    /// <remarks>
    /// Spatial anchor creation is asynchronous. This method initiates a request to create a spatial anchor at
    /// <paramref name="trackingSpacePose"/>. The returned <see cref="OVRTask{TResult}"/> can be awaited or used to
    /// track the completion of the request.
    ///
    /// If spatial anchor creation fails, the resulting <see cref="OVRAnchor"/> will be <see cref="OVRAnchor.Null"/>.
    /// </remarks>
    /// <param name="trackingSpacePose">The pose, in tracking space, at which you wish to create the spatial anchor.</param>
    /// <returns>A task which can be used to track completion of the request.</returns>
    public static OVRTask<OVRAnchor> CreateSpatialAnchorAsync(Pose trackingSpacePose)
        => CreateSpatialAnchor(new SpatialAnchorCreateInfo
        {
            BaseTracking = GetTrackingOriginType(),
            PoseInSpace = new Posef
            {
                Orientation = trackingSpacePose.rotation.ToFlippedZQuatf(),
                Position = trackingSpacePose.position.ToFlippedZVector3f(),
            },
            Time = GetTimeInSeconds(),
        }, out var requestId)
            ? OVRTask.FromRequest<OVRAnchor>(requestId)
            : OVRTask.FromResult(Null);

    /// <summary>
    /// Creates a new spatial anchor.
    /// </summary>
    /// <remarks>
    /// Spatial anchor creation is asynchronous. This method initiates a request to create a spatial anchor at
    /// <paramref name="transform"/>. The returned <see cref="OVRTask{TResult}"/> can be awaited or used to
    /// track the completion of the request.
    ///
    /// If spatial anchor creation fails, the resulting <see cref="OVRAnchor"/> will be <see cref="OVRAnchor.Null"/>.
    /// </remarks>
    /// <param name="transform">The transform at which you wish to create the spatial anchor.</param>
    /// <param name="centerEyeCamera">The `Camera` associated with the Meta Quest's center eye.</param>
    /// <returns>A task which can be used to track completion of the request.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="transform"/> is `null`.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="centerEyeCamera"/> is `null`.</exception>
    public static OVRTask<OVRAnchor> CreateSpatialAnchorAsync(Transform transform, Camera centerEyeCamera)
    {
        if (transform == null)
            throw new ArgumentNullException(nameof(transform));

        if (centerEyeCamera == null)
            throw new ArgumentNullException(nameof(centerEyeCamera));

        var pose = transform.ToTrackingSpacePose(centerEyeCamera);
        return CreateSpatialAnchorAsync(new Pose
        {
            position = pose.position,
            rotation = pose.orientation,
        });
    }

    #endregion

    internal ulong Handle { get; }

    /// <summary>
    /// Unique Identifier representing the anchor.
    /// </summary>
    public Guid Uuid { get; }

    internal OVRAnchor(ulong handle, Guid uuid)
    {
        Handle = handle;
        Uuid = uuid;
    }

    /// <summary>
    /// Gets the anchor's component of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <returns>The requested component.</returns>
    /// <remarks>Make sure the anchor supports the specified type of component using <see cref="SupportsComponent{T}"/></remarks>
    /// <exception cref="InvalidOperationException">Thrown if the anchor doesn't support the specified type of component.</exception>
    /// <seealso cref="TryGetComponent{T}"/>
    /// <seealso cref="SupportsComponent{T}"/>
    public T GetComponent<T>() where T : struct, IOVRAnchorComponent<T>
    {
        if (!TryGetComponent<T>(out var component))
        {
            throw new InvalidOperationException($"Anchor {Uuid} does not have component {typeof(T).Name}");
        }

        return component;
    }

    /// <summary>
    /// Tries to get the anchor's component of a specific type.
    /// </summary>
    /// <param name="component">The requested component, as an <c>out</c> parameter.</param>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <returns>Whether or not the request succeeded. It may fail if the anchor doesn't support this type of component.</returns>
    /// <seealso cref="GetComponent{T}"/>
    public bool TryGetComponent<T>(out T component) where T : struct, IOVRAnchorComponent<T>
    {
        component = default(T);
        var result = OVRPlugin.GetSpaceComponentStatusInternal(Handle, component.Type, out _, out _);
        if (!result.IsSuccess())
        {
            return false;
        }

        component = component.FromAnchor(this);
        return true;
    }

    /// <summary>
    /// Tests whether or not the anchor supports a specific type of components.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <returns>Wether or not the specified type of component is supported.</returns>
    public bool SupportsComponent<T>() where T : struct, IOVRAnchorComponent<T>
    {
        var component = default(T);
        var result = OVRPlugin.GetSpaceComponentStatusInternal(Handle, component.Type, out _, out _);
        return result.IsSuccess();
    }

    public bool Equals(OVRAnchor other) => Handle.Equals(other.Handle) && Uuid.Equals(other.Uuid);
    public override bool Equals(object obj) => obj is OVRAnchor other && Equals(other);
    public static bool operator ==(OVRAnchor lhs, OVRAnchor rhs) => lhs.Equals(rhs);
    public static bool operator !=(OVRAnchor lhs, OVRAnchor rhs) => !lhs.Equals(rhs);
    public override int GetHashCode() => unchecked(Handle.GetHashCode() * 486187739 + Uuid.GetHashCode());
    public override string ToString() => Uuid.ToString();

    /// <summary>
    /// Disposes of an anchor.
    /// </summary>
    /// <remarks>
    /// Calling this method will destroy the anchor so that it won't be managed by internal systems until
    /// the next time it is fetched again.
    /// </remarks>
    public void Dispose() => OVRPlugin.DestroySpaceUser(Handle);
}
