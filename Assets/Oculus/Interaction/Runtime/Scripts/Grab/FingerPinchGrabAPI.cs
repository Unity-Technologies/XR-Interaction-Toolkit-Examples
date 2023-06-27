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

using Oculus.Interaction.Input;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Assertions.Must;

namespace Oculus.Interaction.GrabAPI
{
    [StructLayout(LayoutKind.Sequential)]
    public class HandPinchData
    {
        private const int NumHandJoints = 24;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NumHandJoints * 3, ArraySubType = UnmanagedType.R4)]
        private readonly float[] _jointPositions;

        public HandPinchData()
        {
            int floatCount = NumHandJoints * 3;
            _jointPositions = new float[floatCount];
        }

        public void SetJoints(IReadOnlyList<Pose> poses)
        {
            Assert.AreEqual(NumHandJoints, poses.Count);
            int floatIndex = 0;
            for (int jointIndex = 0; jointIndex < NumHandJoints; jointIndex++)
            {
                Vector3 position = poses[jointIndex].position;
                _jointPositions[floatIndex++] = position.x;
                _jointPositions[floatIndex++] = position.y;
                _jointPositions[floatIndex++] = position.z;
            }
        }

        public void SetJoints(IReadOnlyList<Vector3> positions)
        {
            Assert.AreEqual(NumHandJoints, positions.Count);
            int floatIndex = 0;
            for (int jointIndex = 0; jointIndex < NumHandJoints; jointIndex++)
            {
                Vector3 position = positions[jointIndex];
                _jointPositions[floatIndex++] = position.x;
                _jointPositions[floatIndex++] = position.y;
                _jointPositions[floatIndex++] = position.z;
            }
        }
    }

    /// <summary>
    /// This Finger API uses an advanced calculation for the pinch value of the fingers
    /// to detect if they are grabbing
    /// </summary>
    public class FingerPinchGrabAPI : IFingerAPI
    {
        enum ReturnValue { Success = 0, Failure = -1 };

        #region DLLImports

        [DllImport("InteractionSdk")]
        private static extern int isdk_FingerPinchGrabAPI_Create();

        [DllImport("InteractionSdk")]
        private static extern ReturnValue isdk_FingerPinchGrabAPI_UpdateHandData(int handle, [In] HandPinchData data);

        [DllImport("InteractionSdk")]
        private static extern ReturnValue isdk_FingerPinchGrabAPI_UpdateHandWristHMDData(int handle, [In] HandPinchData data, in Vector3 wristForward, in Vector3 hmdForward);

        [DllImport("InteractionSdk", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern bool isdk_FingerPinchGrabAPI_GetString(int handle, [MarshalAs(UnmanagedType.LPStr)] string name, out IntPtr val);

        [DllImport("InteractionSdk")]
        private static extern ReturnValue isdk_FingerPinchGrabAPI_GetFingerIsGrabbing(int handle, int index, out bool grabbing);

        [DllImport("InteractionSdk")]
        private static extern ReturnValue isdk_FingerPinchGrabAPI_GetFingerIsGrabbingChanged(int handle, int index, bool targetState, out bool grabbing);

        [DllImport("InteractionSdk")]
        private static extern ReturnValue isdk_FingerPinchGrabAPI_GetFingerGrabScore(int handle, HandFinger finger, out float outScore);

        [DllImport("InteractionSdk")]
        private static extern ReturnValue isdk_FingerPinchGrabAPI_GetCenterOffset(int handle, out Vector3 outCenter);

        [DllImport("InteractionSdk")]
        private static extern int isdk_Common_GetVersion(out IntPtr versionStringPtr);

        [DllImport("InteractionSdk")]
        private static extern ReturnValue isdk_FingerPinchGrabAPI_GetPinchGrabParam(int handle, PinchGrabParam paramId, out float outParam);

        [DllImport("InteractionSdk")]
        private static extern ReturnValue isdk_FingerPinchGrabAPI_SetPinchGrabParam(int handle, PinchGrabParam paramId, float param);

        [DllImport("InteractionSdk")]
        private static extern ReturnValue isdk_FingerPinchGrabAPI_IsPinchVisibilityGood(int handle, out bool outVal);
        #endregion

        private int _fingerPinchGrabApiHandle = -1;
        private HandPinchData _pinchData = new HandPinchData();

        private IHmd _hmd = null;

        public FingerPinchGrabAPI(IHmd hmd = null)
        {
            _hmd = hmd;
        }

        private int GetHandle()
        {
            if (_fingerPinchGrabApiHandle == -1)
            {
                _fingerPinchGrabApiHandle = isdk_FingerPinchGrabAPI_Create();
                Debug.Assert(_fingerPinchGrabApiHandle != -1, "FingerPinchGrabAPI: isdk_FingerPinchGrabAPI_Create failed");
            }

            return _fingerPinchGrabApiHandle;
        }

        public void SetPinchGrabParam(PinchGrabParam paramId, float paramVal)
        {
            ReturnValue rc = isdk_FingerPinchGrabAPI_SetPinchGrabParam(GetHandle(), paramId, paramVal);
            Debug.Assert(rc != ReturnValue.Failure, "FingerPinchGrabAPI: isdk_FingerPinchGrabAPI_SetPinchGrabParam failed");
        }

        public float GetPinchGrabParam(PinchGrabParam paramId)
        {
            ReturnValue rc = isdk_FingerPinchGrabAPI_GetPinchGrabParam(GetHandle(), paramId, out float paramVal);
            Debug.Assert(rc != ReturnValue.Failure, "FingerPinchGrabAPI: isdk_FingerPinchGrabAPI_GetPinchGrabParam failed");
            return paramVal;
        }

        public bool GetIsPinchVisibilityGood()
        {
            bool b;
            ReturnValue rc = isdk_FingerPinchGrabAPI_IsPinchVisibilityGood(GetHandle(), out b);
            Debug.Assert(rc != ReturnValue.Failure, "FingerPinchGrabAPI: isdk_FingerPinchGrabAPI_GetIsPinchVisibilityGood failed");
            return b;
        }

        public bool GetFingerIsGrabbing(HandFinger finger)
        {
            ReturnValue rc = isdk_FingerPinchGrabAPI_GetFingerIsGrabbing(GetHandle(), (int)finger, out bool grabbing);
            Debug.Assert(rc != ReturnValue.Failure, "FingerPinchGrabAPI: isdk_FingerPinchGrabAPI_GetFingerIsGrabbing failed");
            return grabbing;
        }

        public Vector3 GetWristOffsetLocal()
        {
            ReturnValue rc = isdk_FingerPinchGrabAPI_GetCenterOffset(GetHandle(), out Vector3 center);
            Debug.Assert(rc != ReturnValue.Failure, "FingerPinchGrabAPI: isdk_FingerPinchGrabAPI_GetCenterOffset failed");
            return center;
        }

        public bool GetFingerIsGrabbingChanged(HandFinger finger, bool targetPinchState)
        {
            ReturnValue rc = isdk_FingerPinchGrabAPI_GetFingerIsGrabbingChanged(GetHandle(), (int)finger, targetPinchState, out bool changed);
            Debug.Assert(rc != ReturnValue.Failure, "FingerPinchGrabAPI: isdk_FingerPinchGrabAPI_GetFingerIsGrabbingChanged failed");
            return changed;
        }

        public float GetFingerGrabScore(HandFinger finger)
        {
            ReturnValue rc = isdk_FingerPinchGrabAPI_GetFingerGrabScore(GetHandle(), finger, out float score);
            Debug.Assert(rc != ReturnValue.Failure, "FingerPinchGrabAPI: isdk_FingerPinchGrabAPI_GetFingerGrabScore failed");
            return score;
        }

        public void Update(IHand hand)
        {
            hand.GetJointPosesFromWrist(out ReadOnlyHandJointPoses poses);

            if (poses.Count > 0)
            {
                _pinchData.SetJoints(poses);
                Vector3 wristForward = Vector3.forward;
                Vector3 hmdForward = Vector3.forward;

                if (_hmd != null &&
                    hand.GetJointPose(HandJointId.HandWristRoot, out Pose wristPose) &&
                    _hmd.TryGetRootPose(out Pose centerEyePose))
                {
                    wristForward = -1.0f * wristPose.forward;
                    hmdForward = -1.0f * centerEyePose.forward;
                    if (hand.Handedness == Handedness.Right)
                    {
                        wristForward = -wristForward;
                    }
                }

                ReturnValue rc = isdk_FingerPinchGrabAPI_UpdateHandWristHMDData(GetHandle(), _pinchData, wristForward, hmdForward);
                Debug.Assert(rc != ReturnValue.Failure, "FingerPinchGrabAPI: isdk_FingerPinchGrabAPI_UpdateHandWristHMDData failed");
            }
        }
    }
}
