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
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace Meta.XR
{
    public enum FoveationMethod
    {
        FFR = 0,
    }

#if UNITY_EDITOR
    [OpenXRFeature(UiName = "Meta XR Foveation",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
        Company = "Meta",
        Desc = "MetaXR Foveation Feature that includes FFR",
        DocumentationLink = "https://developer.oculus.com/",
        OpenxrExtensionStrings = extensionList,
        Version = "1.0.0",
        FeatureId = featureId)]
#endif
    public class MetaXRFoveationFeature : OpenXRFeature
    {
        public const string extensionList = "XR_FB_foveation " +
            "XR_FB_foveation_configuration " +
            "XR_FB_foveation_vulkan ";
        public const string featureId = "com.meta.openxr.feature.foveation";

        private static ulong _xrSession;
#if UNITY_OPENXR_1_5_3
        private static UInt32 _foveatedRenderingLevel = 0;
        private static UInt32 _useDynamicFoveation = 0;
#endif

        protected override void OnSessionCreate(ulong xrSession)
        {
            _xrSession = xrSession;
        }

        public static OVRManager.FoveatedRenderingLevel foveatedRenderingLevel
        {
            get
            {
#if UNITY_OPENXR_1_5_3
                UInt32 level;
                FBGetFoveationLevel(out level);
                return (OVRManager.FoveatedRenderingLevel)level;
#else
                return OVRManager.FoveatedRenderingLevel.Off;
#endif
            }
            set
            {
#if UNITY_OPENXR_1_5_3
                if (value == OVRManager.FoveatedRenderingLevel.HighTop)
                    _foveatedRenderingLevel = (UInt32)OVRManager.FoveatedRenderingLevel.High;
                else
                    _foveatedRenderingLevel = (UInt32)value;
                FBSetFoveationLevel(_xrSession, _foveatedRenderingLevel, 0.0f, _useDynamicFoveation);
#else
                return;
#endif
            }
        }

        public static bool useDynamicFoveatedRendering
        {
            get
            {
#if UNITY_OPENXR_1_5_3
                UInt32 dynamic;
                FBGetFoveationLevel(out dynamic);
                return dynamic != 0;
#else
                return false;
#endif
            }
            set
            {
#if UNITY_OPENXR_1_5_3
                if (value)
                    _useDynamicFoveation = 1;
                else
                    _useDynamicFoveation = 0;
                FBSetFoveationLevel(_xrSession, _foveatedRenderingLevel, 0.0f, _useDynamicFoveation);
#else
                return;
#endif
            }
        }

#region OpenXR Plugin DLL Imports
        [DllImport("UnityOpenXR", EntryPoint = "FBSetFoveationLevel")]
        private static extern void FBSetFoveationLevel(UInt64 session, UInt32 level, float verticalOffset, UInt32 dynamic);

        [DllImport("UnityOpenXR", EntryPoint = "FBGetFoveationLevel")]
        private static extern void FBGetFoveationLevel(out UInt32 level);

        [DllImport("UnityOpenXR", EntryPoint = "FBGetFoveationDynamic")]
        private static extern void FBGetFoveationDynamic(out UInt32 dynamic);
#endregion
    }
}
#endif
