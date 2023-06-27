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

#if UNITY_ANDROID && !UNITY_2020_2_OR_NEWER
#define MISSING_ANDROID_PERMISSION_CALLBACK
#endif

using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#if MISSING_ANDROID_PERMISSION_CALLBACK
using Meta.WitAi;
#endif

namespace Oculus.VoiceSDK.Utilities
{
    public static class MicPermissionsManager
    {
        public static bool HasMicPermission()
        {
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
            return true;
#endif
        }

        public static void RequestMicPermission(Action<string> permissionGrantedCallback = null)
        {
            #if UNITY_ANDROID
            if (HasMicPermission())
            {
                permissionGrantedCallback?.Invoke(Permission.Microphone);
                return;
            }
                #if MISSING_ANDROID_PERMISSION_CALLBACK
                Permission.RequestUserPermission(Permission.Microphone);
                CoroutineUtility.StartCoroutine(CheckPermissionGranted(permissionGrantedCallback));
                #else
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += s => permissionGrantedCallback?.Invoke(s);
                Permission.RequestUserPermission(Permission.Microphone, callbacks);
                #endif
            #else
            permissionGrantedCallback?.Invoke("android.permission.RECORD_AUDIO");

            // Do nothing for now, but eventually we may want to handle IOS/whatever permissions here, too.
            #endif
        }

        #if MISSING_ANDROID_PERMISSION_CALLBACK
        private const int PERMISSION_CHECK_FRAMES = 3;
        private static IEnumerator CheckPermissionGranted(Action<string> permissionGrantedCallback)
        {
            // Exit immediately
            if (permissionGrantedCallback == null)
            {
                yield break;
            }
            // Wait specified amount of frames
            for (int i = 0; i < PERMISSION_CHECK_FRAMES; i++)
            {
                yield return new WaitForEndOfFrame();
            }
            // Successful
            if (HasMicPermission())
            {
                permissionGrantedCallback?.Invoke(Permission.Microphone);
            }
        }
        #endif
    }
}
