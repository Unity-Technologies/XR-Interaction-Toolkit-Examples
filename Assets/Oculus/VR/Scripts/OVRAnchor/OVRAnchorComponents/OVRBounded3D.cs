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
using UnityEngine;

/// <summary>
/// Represents a volume described by its <see cref="Bounds"/>.
/// </summary>
/// <remarks>
/// This component can be accessed from an <see cref="OVRAnchor"/> that supports it by calling
/// <see cref="OVRAnchor.GetComponent{T}"/> from the anchor.
/// </remarks>
/// <seealso cref="BoundingBox"/>
public readonly partial struct OVRBounded3D : IOVRAnchorComponent<OVRBounded3D>, IEquatable<OVRBounded3D>
{
    /// <summary>
    /// Bounding Box
    /// </summary>
    /// <returns>
    /// <see cref="Bounds"/> representing the 3D Bounding Box of the Anchor this component is attached to.
    /// </returns>
    /// <exception cref="InvalidOperationException">If it fails to retrieve the Bounding Box.</exception>
    public Bounds BoundingBox => OVRPlugin.GetSpaceBoundingBox3D(Handle, out var boundsf)
        ? ConvertBounds(boundsf)
        : throw new InvalidOperationException("Could not get BoundingBox");

    private Bounds ConvertBounds(OVRPlugin.Boundsf openXrBounds)
    {
        // OpenXR Bounds describe a volume by its position (or offset) and its size (or extents)
        // https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrRect3DfFB.html
        // Unity Bounds describe a volume by its center and its size (or extents)
        // https://docs.unity3d.com/ScriptReference/Bounds.html
        var extents = openXrBounds.Size.FromSize3f();

        // OpenXR uses a right-handed coordinate system
        // Unity uses left-handed coordinate system
        // We therefore need to flip the z axis to convert from OpenXR to Unity
        // And then, because of the z-axis positive normal, rotate 180 around +y
        // This ends up being equivalent to flipping x axis
        var offset = openXrBounds.Pos.FromFlippedXVector3f();
        // When flipping one axis, position doesn't point to a min corner any more
        offset.x -= extents.x;
        // And add half of the extents to find the center
        var halfExtents = extents * 0.5f;
        var center = offset + halfExtents;

        // Bounds constructor takes the center and the full size (not half extents)
        return new Bounds(center, extents);
    }
}
