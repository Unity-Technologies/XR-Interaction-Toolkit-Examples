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

namespace Oculus.Interaction.UnityCanvas
{
    public enum OVRRenderingMode
    {
        [InspectorName("Alpha-Blended")]
        AlphaBlended = RenderingMode.AlphaBlended,
        [InspectorName("Alpha-Cutout")]
        AlphaCutout = RenderingMode.AlphaCutout,
        [InspectorName("Opaque")]
        Opaque = RenderingMode.Opaque,

        [InspectorName("OVR/Overlay")]
        Overlay = 100,
        [InspectorName("OVR/Underlay")]
        Underlay,
    }
}
