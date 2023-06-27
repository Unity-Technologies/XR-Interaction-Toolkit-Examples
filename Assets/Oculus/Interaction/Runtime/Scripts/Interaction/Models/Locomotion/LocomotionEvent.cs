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

namespace Oculus.Interaction.Locomotion
{
    public struct LocomotionEvent
    {
        public enum TranslationType
        {
            None,
            Velocity,
            Absolute,
            AbsoluteEyeLevel,
            Relative
        }

        public enum RotationType
        {
            None,
            Velocity,
            Absolute,
            Relative
        }

        public int Identifier { get; }
        public Pose Pose { get; }

        public TranslationType Translation { get; }
        public RotationType Rotation { get; }

        public LocomotionEvent(int identifier, Pose pose,
            TranslationType translationType, RotationType rotationType)
        {
            this.Identifier = identifier;
            this.Pose = pose;
            this.Translation = translationType;
            this.Rotation = rotationType;
        }

        public LocomotionEvent(int identifier,
            Vector3 position, TranslationType translationType) :
            this(identifier,
                new Pose(position, Quaternion.identity),
                translationType, RotationType.None)
        {
        }

        public LocomotionEvent(int identifier,
            Quaternion rotation, RotationType rotationType) :
            this(identifier,
                new Pose(Vector3.zero, rotation),
                TranslationType.None, rotationType)
        {
        }
    }
}
