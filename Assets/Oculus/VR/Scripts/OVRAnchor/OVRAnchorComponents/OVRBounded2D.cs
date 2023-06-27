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
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Represents a plane described by its <see cref="Rect"/> and boundary points.
/// </summary>
/// <remarks>
/// This component can be accessed from an <see cref="OVRAnchor"/> that supports it by calling
/// <see cref="OVRAnchor.GetComponent{T}"/> from the anchor.
/// </remarks>
/// <seealso cref="BoundingBox"/>
/// <seealso cref="TryGetBoundaryPointsCount"/>
/// <seealso cref="TryGetBoundaryPoints"/>
public readonly partial struct OVRBounded2D : IOVRAnchorComponent<OVRBounded2D>, IEquatable<OVRBounded2D>
{
    /// <summary>
    /// Bounding Box
    /// </summary>
    /// <returns>
    /// <see cref="Rect"/> representing the 2D Bounding Box of the Anchor this component is attached to.
    /// </returns>
    /// <exception cref="InvalidOperationException">If it fails to retrieve the Bounding Box.</exception>
    public Rect BoundingBox => OVRPlugin.GetSpaceBoundingBox2D(Handle, out var rectf)
        ? ConvertRect(rectf)
        : throw new InvalidOperationException("Could not get BoundingBox");

    private Rect ConvertRect(OVRPlugin.Rectf openXrRect)
    {
        // OpenXR Rects describe a rectangle by its position (or offset) and its size (or extents)
        // https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrRect2Df.html
        // Unity Rects describe a rectangle by its position and its size (or extents)
        // https://docs.unity3d.com/ScriptReference/Rect.html
        var extents = openXrRect.Size.FromSizef();

        // OpenXR uses a right-handed coordinate system
        // Unity uses left-handed coordinate system
        // There is a design assumption that plane's normal should coincide with +z
        // We therefore need to rotate the plane 180° around +y axis
        // We therefore need to flip the x axis.
        var offset = openXrRect.Pos.FromFlippedXVector2f();
        // When flipping one axis, position doesn't point to a min corner any more
        offset.x -= extents.x;

        return new Rect(offset, extents);
    }

    /// <summary>
    /// Retrieves the number of boundary points contained in an Anchor with an enabled Bounded2D component.
    /// </summary>
    /// <param name="count">The number of boundary points contained in the Bounded2D component of the Anchor, as an <c>out</c> parameter.</param>
    /// <returns><c>true</c> if it successfully retrieves the count, <c>false</c> otherwise.</returns>
    /// <remarks>This is the first part of the two-calls idiom for retrieving boundary points. <see cref="TryGetBoundaryPoints"/> to actually get those points.</remarks>
    /// <seealso cref="TryGetBoundaryPoints"/>
    public bool TryGetBoundaryPointsCount(out int count) =>
        OVRPlugin.GetSpaceBoundary2DCount(Handle, out count);

    /// <summary>
    /// Retrieves the boundary points contained in an Anchor with an enabled Bounded2D component.
    /// </summary>
    /// <param name="positions">The array that will get populated with the boundary points contained in the Bounded2D component of the Anchor.</param>
    /// <returns><c>true</c> if it successfully populates the <paramref name="positions"/> array with the boundary points.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="positions"/> has not been created.</exception>
    /// <remarks>This is the second part of the two-calls idiom for retrieving boundary points.
    /// It is expected for the <paramref name="positions"/> to be created and with enough capacity to contain the boundary points.</remarks>
    /// <seealso cref="TryGetBoundaryPointsCount"/>
    public bool TryGetBoundaryPoints(NativeArray<Vector2> positions)
    {
        if (!positions.IsCreated) throw new ArgumentException("NativeArray is not created", nameof(positions));
        if (!OVRPlugin.GetSpaceBoundary2D(Handle, positions, out var count)) return false;

        var low = 0;
        var high = count - 1;
        for (; low <= high; low++, high--)
        {
            var swapTemporaryPositionHigh = positions[high];
            var swapTemporaryPositionLow = positions[low];
            positions[low] = new Vector2(-swapTemporaryPositionHigh.x, swapTemporaryPositionHigh.y);
            positions[high] = new Vector2(-swapTemporaryPositionLow.x, swapTemporaryPositionLow.y);
        }

        return true;
    }
}
