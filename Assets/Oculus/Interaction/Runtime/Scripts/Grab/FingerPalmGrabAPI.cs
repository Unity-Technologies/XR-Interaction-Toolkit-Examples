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
using Oculus.Interaction.PoseDetection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.GrabAPI
{
    /// <summary>
    /// This Finger API uses the curl value of the fingers to detect if they are grabbing
    /// </summary>
    public class FingerPalmGrabAPI : IFingerAPI
    {
        // Temporary structure used to pass data to and from native components
        [StructLayout(LayoutKind.Sequential)]
        public class HandData
        {
            private const int NumHandJoints = 24;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NumHandJoints * 7, ArraySubType = UnmanagedType.R4)]
            private float[] jointValues;
            private float _rootRotX;
            private float _rootRotY;
            private float _rootRotZ;
            private float _rootRotW;
            private float _rootPosX;
            private float _rootPosY;
            private float _rootPosZ;
            private int _handedness;

            public HandData()
            {
                jointValues = new float[NumHandJoints * 7];
            }

            public void SetData(IReadOnlyList<Pose> joints, Pose root, Handedness handedness)
            {
                Assert.AreEqual(NumHandJoints, joints.Count);
                int jointValueIndex = 0;
                for (int jointIndex = 0; jointIndex < NumHandJoints; jointIndex++)
                {
                    Pose joint = joints[jointIndex];
                    jointValues[jointValueIndex++] = joint.rotation.x;
                    jointValues[jointValueIndex++] = joint.rotation.y;
                    jointValues[jointValueIndex++] = joint.rotation.z;
                    jointValues[jointValueIndex++] = joint.rotation.w;
                    jointValues[jointValueIndex++] = joint.position.x;
                    jointValues[jointValueIndex++] = joint.position.y;
                    jointValues[jointValueIndex++] = joint.position.z;
                }
                this._rootRotX = root.rotation.x;
                this._rootRotY = root.rotation.y;
                this._rootRotZ = root.rotation.z;
                this._rootRotW = root.rotation.w;
                this._rootPosX = root.position.x;
                this._rootPosY = root.position.y;
                this._rootPosZ = root.position.z;
                this._handedness = (int)handedness;
            }
        }

        #region DLLImports
        enum ReturnValue { Success = 0, Failure = -1 };


        [DllImport("InteractionSdk")]
        private static extern int isdk_FingerPalmGrabAPI_Create();

        [DllImport("InteractionSdk")]
        private static extern ReturnValue isdk_FingerPalmGrabAPI_UpdateHandData(int handle, [In] HandData data);

        [DllImport("InteractionSdk")]
        private static extern ReturnValue isdk_FingerPalmGrabAPI_GetFingerIsGrabbing(int handle, HandFinger finger, out bool grabbing);

        [DllImport("InteractionSdk")]
        private static extern ReturnValue isdk_FingerPalmGrabAPI_GetFingerIsGrabbingChanged(int handle, HandFinger finger, bool targetGrabState, out bool changed);

        [DllImport("InteractionSdk")]
        private static extern ReturnValue isdk_FingerPalmGrabAPI_GetFingerGrabScore(int handle, HandFinger finger, out float score);

        [DllImport("InteractionSdk")]
        private static extern ReturnValue isdk_FingerPalmGrabAPI_GetCenterOffset(int handle, out Vector3 score);

        #endregion

        private int apiHandle_ = -1;
        private HandData handData_;

        public FingerPalmGrabAPI()
        {
            handData_ = new HandData();
        }

        private int GetHandle()
        {

            if (apiHandle_ == -1)
            {
                apiHandle_ = isdk_FingerPalmGrabAPI_Create();
                Debug.Assert(apiHandle_ != -1, "FingerPalmGrabAPI: isdk_FingerPalmGrabAPI_Create failed");
            }

            return apiHandle_;
        }

        public bool GetFingerIsGrabbing(HandFinger finger)
        {
            ReturnValue rv = isdk_FingerPalmGrabAPI_GetFingerIsGrabbing(GetHandle(), finger, out bool grabbing);
            Debug.Assert(rv != ReturnValue.Failure, "FingerPalmGrabAPI: isdk_FingerPalmGrabAPI_GetFingerIsGrabbing failed");
            return grabbing;
        }

        public bool GetFingerIsGrabbingChanged(HandFinger finger, bool targetGrabState)
        {
            ReturnValue rv = isdk_FingerPalmGrabAPI_GetFingerIsGrabbingChanged(GetHandle(), finger, targetGrabState, out bool grabbing);
            Debug.Assert(rv != ReturnValue.Failure, "FingerPalmGrabAPI: isdk_FingerPalmGrabAPI_GetFingerIsGrabbingChanged failed");
            return grabbing;
        }

        public float GetFingerGrabScore(HandFinger finger)
        {
            ReturnValue rv = isdk_FingerPalmGrabAPI_GetFingerGrabScore(GetHandle(), finger, out float score);
            Debug.Assert(rv != ReturnValue.Failure, "FingerPalmGrabAPI: isdk_FingerPalmGrabAPI_GetFingerGrabScore failed");
            return score;
        }

        public void Update(IHand hand)
        {
            if (!hand.GetRootPose(out Pose rootPose))
            {
                return;
            }

            if (!hand.GetJointPosesFromWrist(out ReadOnlyHandJointPoses poses))
            {
                return;
            }

            handData_.SetData(poses, rootPose, hand.Handedness);
            ReturnValue rv = isdk_FingerPalmGrabAPI_UpdateHandData(GetHandle(), handData_);
            Debug.Assert(rv != ReturnValue.Failure, "FingerPalmGrabAPI: isdk_FingerPalmGrabAPI_UpdateHandData failed");
        }

        public Vector3 GetWristOffsetLocal()
        {
            ReturnValue rv = isdk_FingerPalmGrabAPI_GetCenterOffset(GetHandle(), out Vector3 center);
            Debug.Assert(rv != ReturnValue.Failure, "FingerPalmGrabAPI: isdk_FingerPalmGrabAPI_GetCenterOffset failed");
            return center;
        }
    }
}
