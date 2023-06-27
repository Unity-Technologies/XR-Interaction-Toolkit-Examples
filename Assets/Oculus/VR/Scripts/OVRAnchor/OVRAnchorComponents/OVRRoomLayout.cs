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

/// <summary>
/// Represents a room described by its floor, ceiling and walls <see cref="OVRAnchor"/>s.
/// </summary>
/// <remarks>
/// This component can be accessed from an <see cref="OVRAnchor"/> that supports it by calling
/// <see cref="OVRAnchor.GetComponent{T}"/> from the anchor.
/// </remarks>
/// <seealso cref="FetchLayoutAnchorsAsync"/>
/// <seealso cref="TryGetRoomLayout"/>
public readonly partial struct OVRRoomLayout : IOVRAnchorComponent<OVRRoomLayout>, IEquatable<OVRRoomLayout>
{
    /// <summary>
    /// Asynchronous method that fetches anchors contained in the Room Layout.
    /// </summary>
    /// <param name="anchors">List that will get cleared and populated with the requested anchors.</param>
    /// <remarks>Dispose of the returned task if you don't use the results</remarks>
    /// <returns>A task that will eventually let you test if the fetch was successful or not.
    /// If the result is true, then the <see cref="anchors"/> parameter has been populated with the requested anchors.</returns>
    /// <exception cref="InvalidOperationException">If it fails to retrieve the Room Layout</exception>
    /// <exception cref="ArgumentNullException">If parameter anchors is null</exception>
    public OVRTask<bool> FetchLayoutAnchorsAsync(List<OVRAnchor> anchors)
    {
        if (!OVRPlugin.GetSpaceRoomLayout(Handle, out var roomLayout))
        {
            throw new InvalidOperationException("Could not get Room Layout");
        }

        using (new OVRObjectPool.ListScope<Guid>(out var list))
        {
            list.Add(roomLayout.floorUuid);
            list.Add(roomLayout.ceilingUuid);
            list.AddRange(roomLayout.wallUuids);
            return OVRAnchor.FetchAnchorsAsync(list, anchors);
        }
    }

    /// <summary>
    /// Tries to get the Ceiling, Floor and Walls unique identifiers. These can then be used to Fetch their anchors.
    /// </summary>
    /// <param name="ceiling">Out <see cref="Guid"/> representing the ceiling of the room.</param>
    /// <param name="floor">Out <see cref="Guid"/> representing the floor of the room.</param>
    /// <param name="walls">Out array of <see cref="Guid"/>s representing the walls of the room.</param>
    /// <returns>
    /// <see cref="bool"/> true if the request succeeds and false if it fails.
    /// </returns>
    public bool TryGetRoomLayout(out Guid ceiling, out Guid floor, out Guid[] walls)
    {
        ceiling = Guid.Empty;
        floor = Guid.Empty;
        walls = null;
        if (!OVRPlugin.GetSpaceRoomLayout(Handle, out var roomLayout))
        {
            return false;
        }

        ceiling = roomLayout.ceilingUuid;
        floor = roomLayout.floorUuid;
        walls = roomLayout.wallUuids;
        return true;
    }
}
