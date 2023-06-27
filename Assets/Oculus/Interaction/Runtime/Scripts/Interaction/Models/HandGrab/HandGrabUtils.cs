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

using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    public static class HandGrabUtils
    {
        /// <summary>
        /// Serializable data-only version of the HandGrabInteractable so it can be stored when they
        /// are generated at Play-Mode (where Hand-tracking works).
        /// </summary>
        [System.Serializable]
        public struct HandGrabInteractableData
        {
            public List<HandGrabPoseData> poses;
            public GrabTypeFlags grabType;
            public HandAlignType handAlignment;

            public PoseMeasureParameters scoringModifier;
            public GrabbingRule pinchGrabRules;
            public GrabbingRule palmGrabRules;
        }

        /// <summary>
        /// Serializable data-only version of the HandGrabPose so it can be stored when they
        /// are generated at Play-Mode (where Hand-tracking works).
        /// </summary>
        [System.Serializable]
        public struct HandGrabPoseData
        {
            public Pose gripPose;
            public HandPose handPose;
            public float scale;
        }

        /// <summary>
        /// Creates a new HandGrabInteractable under the given object
        /// </summary>
        /// <param name="parent">The relative object for the interactable</param>
        /// <param name="name">Name for the GameObject holding this interactable</param>
        /// <returns>An non-populated HandGrabInteractable</returns>
        public static HandGrabInteractable CreateHandGrabInteractable(Transform parent, string name = null)
        {
            GameObject go = new GameObject(name ?? "HandGrabInteractable");
            go.transform.SetParent(parent, false);
            go.SetActive(false);
            HandGrabInteractable record = go.AddComponent<HandGrabInteractable>();
            record.InjectRigidbody(parent.GetComponentInParent<Rigidbody>());
            record.InjectOptionalPointableElement(parent.GetComponentInParent<Grabbable>());
            go.SetActive(true);
            return record;
        }

        public static HandGrabPose CreateHandGrabPose(Transform parent, Transform relativeTo)
        {
            GameObject go = new GameObject("HandGrabPose");
            go.transform.SetParent(parent, false);
            HandGrabPose record = go.AddComponent<HandGrabPose>();
            record.InjectAllHandGrabPose(relativeTo);
            return record;
        }

        /// <summary>
        /// Generates a new HandGrabPoseData that mirrors the provided one. Left hand becomes right hand and vice-versa.
        /// The mirror axis is defined by the surface of the snap point, if any, if none a best-guess is provided
        /// but note that it can then moved manually in the editor.
        /// </summary>
        /// <param name="originalPoint">The point to mirror</param>
        /// <param name="mirrorPoint">The target HandGrabPose to set as mirrored of the originalPoint</param>
        public static void MirrorHandGrabPose(HandGrabPose originalPoint, HandGrabPose mirrorPoint, Transform relativeTo)
        {
            HandPose handPose = originalPoint.HandPose;

            Handedness oppositeHandedness = handPose.Handedness == Handedness.Left ? Handedness.Right : Handedness.Left;

            HandGrabPoseData mirrorData = SaveHandGrabPoseData(originalPoint);
            mirrorData.handPose.Handedness = oppositeHandedness;

            if (originalPoint.SnapSurface != null)
            {
                mirrorData.gripPose = originalPoint.SnapSurface.MirrorPose(mirrorData.gripPose, relativeTo);
            }
            else
            {
                mirrorData.gripPose = mirrorData.gripPose.MirrorPoseRotation(Vector3.forward, Vector3.up);
                Vector3 translation = Vector3.Project(mirrorData.gripPose.position, Vector3.right);
                mirrorData.gripPose.position = mirrorData.gripPose.position - 2f * translation;
            }

            LoadHandGrabPoseData(mirrorPoint, mirrorData, relativeTo);
            if (originalPoint.SnapSurface != null)
            {
                Grab.GrabSurfaces.IGrabSurface mirroredSurface = originalPoint.SnapSurface.CreateMirroredSurface(mirrorPoint.gameObject);
                mirrorPoint.InjectOptionalSurface(mirroredSurface);
            }
        }

        private static HandGrabPoseData SaveHandGrabPoseData(HandGrabPose handGrabPose)
        {
            HandGrabPoseData data = new HandGrabPoseData()
            {
                handPose = new HandPose(handGrabPose.HandPose),
                scale = handGrabPose.RelativeScale,
                gripPose = handGrabPose.RelativePose
            };

            return data;
        }

        private static void LoadHandGrabPoseData(HandGrabPose handGrabPose, HandGrabPoseData data, Transform relativeTo)
        {
            handGrabPose.transform.localScale = Vector3.one * data.scale;
            handGrabPose.transform.SetPose(PoseUtils.GlobalPoseScaled(relativeTo, data.gripPose));
            if (data.handPose != null)
            {
                handGrabPose.InjectOptionalHandPose(new HandPose(data.handPose));
            }
        }

        #region dataSave
        /// <summary>
        /// Serializes the data of the HandGrabInteractable so it can be stored
        /// </summary>
        /// <returns>The struct data to recreate the interactable</returns>
        public static HandGrabInteractableData SaveData(HandGrabInteractable interactable)
        {
            List<HandGrabPoseData> poses = new List<HandGrabPoseData>();
            foreach (HandGrabPose pose in interactable.HandGrabPoses)
            {
                poses.Add(SaveHandGrabPoseData(pose));
            }

            return new HandGrabInteractableData()
            {
                poses = poses,
                scoringModifier = interactable.ScoreModifier,
                grabType = interactable.SupportedGrabTypes,
                handAlignment = interactable.HandAlignment,
                pinchGrabRules = interactable.PinchGrabRules,
                palmGrabRules = interactable.PalmGrabRules
            };
        }

        /// <summary>
        /// Populates the HandGrabInteractable with the serialized data version
        /// </summary>
        /// <param name="data">The serialized data for the HandGrabInteractable.</param>
        public static void LoadData(HandGrabInteractable interactable, HandGrabInteractableData data)
        {
            interactable.InjectSupportedGrabTypes(data.grabType);
            interactable.InjectPinchGrabRules(data.pinchGrabRules);
            interactable.InjectPalmGrabRules(data.palmGrabRules);
            interactable.InjectOptionalScoreModifier(data.scoringModifier);
            interactable.HandAlignment = data.handAlignment;

            if (data.poses == null)
            {
                return;
            }
            List<HandGrabPose> poses = new List<HandGrabPose>();
            foreach (HandGrabPoseData handGrabPoseData in data.poses)
            {
                poses.Add(LoadHandGrabPose(interactable, handGrabPoseData));
            }
            interactable.InjectOptionalHandGrabPoses(poses);
        }

        public static HandGrabPose LoadHandGrabPose(HandGrabInteractable interactable,
            HandGrabPoseData poseData)
        {
            HandGrabPose point = CreateHandGrabPose(interactable.transform, interactable.RelativeTo);
            LoadHandGrabPoseData(point, poseData, interactable.RelativeTo);
            interactable.HandGrabPoses.Add(point);
            return point;
        }
        #endregion
    }
}
