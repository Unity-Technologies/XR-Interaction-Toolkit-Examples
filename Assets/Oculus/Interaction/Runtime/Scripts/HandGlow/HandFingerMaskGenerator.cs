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

using Oculus.Interaction;
using Oculus.Interaction.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Oculus.Interaction
{
    /// <summary>
    /// HandFingerMaskGenerator creates a finger mask from the hand model currently used (check diff that added this script)
    /// It projects the hand model into a 2d space using the object space positions of the hand model
    /// and line positions using the hand visuals Joint Poses.
    /// </summary>
    public class HandFingerMaskGenerator
    {
        private static readonly int[] _fingerLinesID =
        {
            Shader.PropertyToID("_ThumbLine"), Shader.PropertyToID("_IndexLine"),
            Shader.PropertyToID("_MiddleLine"), Shader.PropertyToID("_RingLine"),
            Shader.PropertyToID("_PinkyLine")
        };

        private static readonly int[] _palmFingerLinesID =
        {
            Shader.PropertyToID("_PalmThumbLine"), Shader.PropertyToID("_PalmIndexLine"),
            Shader.PropertyToID("_PalmMiddleLine"), Shader.PropertyToID("_PalmRingLine"),
            Shader.PropertyToID("_PalmPinkyLine")
        };

        private static float HandednessMultiplier(Handedness hand) =>
            hand != Handedness.Right ? -1.0f : 1.0f;

        private static List<Vector2> GenerateModelUV(Handedness hand, Mesh sharedHandMesh,
            out Vector2 minPosition,
            out Vector2 maxPosition)
        {
            List<Vector3> mVertices = new List<Vector3>();
            sharedHandMesh.GetVertices(mVertices);
            minPosition = new Vector2(mVertices[0].x, mVertices[0].z);
            maxPosition = new Vector2(mVertices[0].x, mVertices[0].z);
            for (int i = 0; i < mVertices.Count; i++)
            {
                var vertex = mVertices[i] * HandednessMultiplier(hand);
                var vertex2d = new Vector2(vertex.x, vertex.z);
                minPosition = Vector2.Min(minPosition, vertex2d);
                maxPosition = Vector2.Max(maxPosition, vertex2d);
                mVertices[i] = vertex;
            }

            List<Vector2> mUVs = new List<Vector2>();
            Vector2 regionSize = maxPosition - minPosition;
            float maxLength = Mathf.Max(regionSize.x, regionSize.y);
            foreach (var vertex in mVertices)
            {
                var vertex2d = new Vector2(vertex.x, vertex.z);
                var vertexUV = (vertex2d - minPosition) / maxLength;
                mUVs.Add(vertexUV);
            }

            return mUVs;
        }

        private static Vector2 getPositionOnRegion(HandVisual handVisual, HandJointId jointId,
            Vector2 minRegion,
            float sideLength)
        {
            var lineStartPose = handVisual.GetJointPose(jointId, Space.World);
            var lineStartLocalPosition =
                handVisual.transform.InverseTransformPoint(lineStartPose.position);
            Vector2 point = new Vector2(lineStartLocalPosition.x, lineStartLocalPosition.z);
            point *= HandednessMultiplier(handVisual.Hand.Handedness);
            return (point - minRegion) / sideLength;
        }

        private static Vector4 GenerateLineData(HandVisual handVisual, HandJointId jointIdStart,
            HandJointId jointIdEnd,
            Vector2 minRegion, float sideLength, float lineScale)
        {
            Vector2 startPosition =
                getPositionOnRegion(handVisual, jointIdStart, minRegion, sideLength);
            Vector2 endPosition =
                getPositionOnRegion(handVisual, jointIdEnd, minRegion, sideLength);
            endPosition = Vector2.LerpUnclamped(startPosition, endPosition, lineScale);
            return new Vector4(startPosition.x, startPosition.y, endPosition.x, endPosition.y);
        }

        private static Vector4[] GenerateFingerLines(HandVisual handVisual, Vector2 minPosition,
            float maxLength, float[] lineScale)
        {
            Vector4 thumbLine = GenerateLineData(handVisual, HandJointId.HandThumbTip,
                HandJointId.HandThumb1, minPosition, maxLength, lineScale[0]);
            Vector4 indexLine = GenerateLineData(handVisual, HandJointId.HandIndexTip,
                HandJointId.HandIndex1, minPosition, maxLength, lineScale[1]);
            Vector4 middleLine = GenerateLineData(handVisual, HandJointId.HandMiddleTip,
                HandJointId.HandMiddle1, minPosition, maxLength, lineScale[2]);
            Vector4 ringLine = GenerateLineData(handVisual, HandJointId.HandRingTip,
                HandJointId.HandRing1, minPosition, maxLength, lineScale[3]);
            Vector4 pinkyLine = GenerateLineData(handVisual, HandJointId.HandPinkyTip,
                HandJointId.HandPinky1, minPosition, maxLength, lineScale[4]);

            return new Vector4[5] { thumbLine, indexLine, middleLine, ringLine, pinkyLine };
        }

        private static void SetGlowModelUV(SkinnedMeshRenderer handRenderer, HandVisual handVisual,
            out Vector2 minPosition, out Vector2 maxPosition)
        {
            Mesh sharedHandMesh = handRenderer.sharedMesh;

            var mUVs = GenerateModelUV(handVisual.Hand.Handedness, sharedHandMesh,
                out minPosition,
                out maxPosition);

            sharedHandMesh.SetUVs(1, mUVs);
            sharedHandMesh.UploadMeshData(false);
        }

        private static void SetFingerMaskUniforms(HandVisual handVisual, MaterialPropertyBlock materialPropertyBlock, Vector2 minPosition, Vector2 maxPosition)
        {
            Vector2 regionSize = maxPosition - minPosition;
            float maxLength = Mathf.Max(regionSize.x, regionSize.y);

            //The following numbers generate good looking lines for the current hand model
            var fingerLineScales = new float[5] { 0.9f, 0.91f, 0.9f, 0.87f, 0.87f };
            var fingerLines = GenerateFingerLines(handVisual, minPosition, maxLength, fingerLineScales);
            //The thumb line is going to be perpendicularly aligned to the wrist direction
            fingerLines[0].z = Mathf.Lerp(fingerLines[0].z, fingerLines[0].x, 0.3f);
            fingerLines[0].x = fingerLines[0].z;

            var palmFingerLineScales = new float[5] { 1.2f, 1.25f, 1.25f, 1.25f, 1.25f };
            var palmFingerLines =
                GenerateFingerLines(handVisual, minPosition, maxLength, palmFingerLineScales);
            //The thumb line is going to be perpendicularly aligned to the wrist direction
            float thumbOffset = Mathf.Abs(palmFingerLines[0].x - palmFingerLines[0].z) * 0.1f;
            palmFingerLines[0].z += thumbOffset;

            for (int i = 0; i < 5; i++)
            {
                materialPropertyBlock.SetVector(_fingerLinesID[i], fingerLines[i]);
                materialPropertyBlock.SetVector(_palmFingerLinesID[i], palmFingerLines[i]);
            }
        }

        public static void GenerateFingerMask(SkinnedMeshRenderer handRenderer, HandVisual handVisual, MaterialPropertyBlock materialPropertyBlock)
        {
            SetGlowModelUV(handRenderer, handVisual, out Vector2 minPosition,
                out Vector2 maxPosition);
            SetFingerMaskUniforms(handVisual, materialPropertyBlock, minPosition, maxPosition);
        }
    }
}
