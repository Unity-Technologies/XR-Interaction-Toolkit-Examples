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
/// This class is used to affect the transformation of scene anchors.
/// </summary>
/// <remarks>
/// When a scene anchor is instantiated, a <seealso cref="OVRScenePlane"/> and/or
/// <seealso cref="OVRSceneVolume"/> is added if the underlying scene anchor
/// as a 2D and/or 3D component, respectively.
///
/// Both <seealso cref="OVRScenePlane"/> and <seealso cref="OVRSceneVolume"/>
/// provide the option to scale and offset all child objects, so that they
/// align with the dimensions of the scene anchor.
///
/// If this type exists on the scene anchor's child objects, then a scale/offset
/// will only be performed if the type aligns.
///
/// Note: if this type does not exist on the scene anchor's child objects,
/// then the <seealso cref="OVRSceneVolume"/> will take precedence for
/// scaling/offseting child objects.
/// </remarks>
public class OVRSceneObjectTransformType : MonoBehaviour
{
    [Serializable]
    public enum Transformation
    {
        Volume,
        Plane,
        None
    }

    [Tooltip("Choose the type of scene anchor (volume/plane) " +
             "that may modify this transform.")]
    public Transformation TransformType;
}
