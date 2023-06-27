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

#if USING_XR_SDK_OPENXR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.XR.OpenXR.Features;

namespace Meta.XR
{
    [OpenXRFeatureSet(
        FeatureIds = new string[] {
            MetaXRFeature.featureId,
            MetaXRFoveationFeature.featureId,
            },
        UiName = "Meta XR",
        Description = "Feature set for using Meta XR Features",
        FeatureSetId = featureSetId,
        SupportedBuildTargets = new BuildTargetGroup[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone },
        RequiredFeatureIds = new string[]
        {
            MetaXRFeature.featureId,
        },
        DefaultFeatureIds = new string[]
        {
            MetaXRFoveationFeature.featureId,
        }
    )]
    sealed class MetaXRFeatureSet
    {
        public const string featureSetId = "com.meta.openxr.featureset.metaxr";
    }
}
#endif
#endif
