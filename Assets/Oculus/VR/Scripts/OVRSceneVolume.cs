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

using UnityEngine;

/// <summary>
/// A <see cref="OVRSceneAnchor"/> that has a 3D bounds associated with it.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(OVRSceneAnchor))]
public class OVRSceneVolume : MonoBehaviour, IOVRSceneComponent
{
    /// <summary>
    /// The width (in the local X-direction), in meters.
    /// </summary>
    public float Width { get; private set; }

    /// <summary>
    /// The height (in the local Y-direction), in meters.
    /// </summary>
    public float Height { get; private set; }

    /// <summary>
    /// The depth (in the local Z-direction), in meters.
    /// </summary>
    public float Depth { get; private set; }

    /// <summary>
    /// The dimensions of the volume.
    /// </summary>
    /// <remarks>
    /// This property corresponds to a Vector whose components are
    /// (<see cref="Width"/>, <see cref="Height"/>, <see cref="Depth"/>).
    /// </remarks>
    public Vector3 Dimensions => new Vector3(Width, Height, Depth);

    /// <summary>
    /// The offset of the volume with respect to the anchor's pivot.
    /// </summary>
    /// <remarks>
    /// The offset is mostly zero, as most objects have the anchor's pivot
    /// aligned with the top face of the volume.
    ///
    /// The offset is not zero in cases where the anchor's pivot point is
    /// aligned with another element, such as the seated area for a couch,
    /// defined as a plane.
    ///
    /// The Offset is provided in the local coordinate space of the
    /// children. See <seealso cref="OVRSceneAnchor"/> to see the
    /// transformation of Unity and OpenXR coordinate systems.
    public Vector3 Offset { get; private set; }

    /// <summary>
    /// Whether the child transforms will be scaled according to the dimensions of this volume.
    /// </summary>
    /// <remarks>If set to True, all the child transforms will be scaled to the dimensions of this volume immediately.
    /// And, if it's set to False, dimensions of this volume will no longer affect the child transforms, and child
    /// transforms will retain their current scale. This can be controlled further by using a
    /// <seealso cref="OVRSceneObjectTransformType"/>.</remarks>
    public bool ScaleChildren
    {
        get => _scaleChildren;
        set
        {
            _scaleChildren = value;
            if (_scaleChildren && _sceneAnchor.Space.Valid)
            {
                SetChildScale();
            }
        }
    }

    /// <summary>
    /// Whether the child transforms will be offset according to the offset of this volume.
    /// </summary>
    /// <remarks>If set to True, all the child transforms will be offset to the offset of this volume immediately.
    /// And, if it's set to False, offsets of this volume will no longer affect the child transforms, and child
    /// transforms will retain their current offset. This can be controlled further by using a
    /// <seealso cref="OVRSceneObjectTransformType"/>.</remarks>
    public bool OffsetChildren
    {
        get => _offsetChildren;
        set
        {
            _offsetChildren = value;
            if (_offsetChildren && _sceneAnchor.Space.Valid)
            {
                SetChildOffset();
            }
        }
    }

    [Tooltip("When enabled, scales the child transforms according to the dimensions of this volume.")]
    [SerializeField]
    private bool _scaleChildren = true;

    [Tooltip("When enabled, offsets the child transforms according to the offset of this volume.")]
    [SerializeField]
    private bool _offsetChildren = false;

    private OVRSceneAnchor _sceneAnchor;

    private void Awake()
    {
        _sceneAnchor = GetComponent<OVRSceneAnchor>();
        if (_sceneAnchor.Space.Valid)
        {
            ((IOVRSceneComponent)this).Initialize();
        }
    }

    void IOVRSceneComponent.Initialize()
    {
        UpdateTransform();
    }

    private void SetChildScale()
    {
        // this will scale all children unless they specifically ask not to
        // be scaled, using a TransformType that is not Volume.
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.TryGetComponent<OVRSceneObjectTransformType>(out var transformType))
            {
                if (transformType.TransformType != OVRSceneObjectTransformType.Transformation.Volume)
                    continue;
            }

            child.localScale = Dimensions;
        }
    }

    private void SetChildOffset()
    {
        // this will offset all children unless they specifically ask not to
        // be offset, using a TransformType that is not Volume.
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.TryGetComponent<OVRSceneObjectTransformType>(out var transformType))
            {
                if (transformType.TransformType != OVRSceneObjectTransformType.Transformation.Volume)
                    continue;
            }

            child.localPosition = Offset;
        }
    }

    internal void UpdateTransform()
    {
        if (OVRPlugin.GetSpaceBoundingBox3D(_sceneAnchor.Space, out var bounds))
        {
            Width = bounds.Size.w;
            Height = bounds.Size.h;
            Depth = bounds.Size.d;

            // calculate the offset as the difference between the
            // volume pivot and anchor pivot, in Unity coordinate system
            var anchorPivot = transform.position;
            var minPoint = transform.TransformPoint(bounds.Pos.FromVector3f());
            var maxPoint = transform.TransformPoint(
                bounds.Pos.FromVector3f() + bounds.Size.FromSize3f());
            var volumePivot = Vector3.Lerp(minPoint, maxPoint, 0.5f);
            volumePivot.y = maxPoint.y;

            Offset = new Vector3(
                volumePivot.x - anchorPivot.x,
                volumePivot.z - anchorPivot.z,
                volumePivot.y - anchorPivot.y);

            OVRSceneManager.Development.Log(nameof(OVRSceneVolume),
                $"[{_sceneAnchor.Uuid}] Volume has dimensions {Dimensions} " +
                $"and offset {Offset}.");

            if (ScaleChildren)
                SetChildScale();
            if (OffsetChildren)
                SetChildOffset();
        }
        else
        {
            OVRSceneManager.Development.LogError(nameof(OVRSceneVolume),
                $"[{_sceneAnchor.Space}] Failed to retrieve volume's information.");
        }
    }
}
