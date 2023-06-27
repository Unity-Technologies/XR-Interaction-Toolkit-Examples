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
using System;
using Oculus.Interaction.Input;

namespace Oculus.Interaction.Body.Input
{
    public class BodyDataAsset : ICopyFrom<BodyDataAsset>
    {
        public ISkeletonMapping SkeletonMapping { get; set; }
        public Pose Root { get; set; } = Pose.identity;
        public float RootScale { get; set; } = 1f;
        public bool IsDataValid { get; set; } = false;
        public bool IsDataHighConfidence { get; set; } = false;
        public Pose[] JointPoses { get; set; } = new Pose[Constants.NUM_BODY_JOINTS];
        public int SkeletonChangedCount { get; set; } = 0;

        public void CopyFrom(BodyDataAsset source)
        {
            SkeletonMapping = source.SkeletonMapping;
            Root = source.Root;
            RootScale = source.RootScale;
            IsDataValid = source.IsDataValid;
            IsDataHighConfidence = source.IsDataHighConfidence;
            SkeletonChangedCount = source.SkeletonChangedCount;

            for (int i = 0; i < source.JointPoses.Length; ++i)
            {
                JointPoses[i] = source.JointPoses[i];
            }
        }
    }
}
