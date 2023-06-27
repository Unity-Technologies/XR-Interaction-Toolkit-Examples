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

namespace Oculus.Interaction.Surfaces
{
    /// <summary>
    /// Defines a portion of a cylinder surface
    /// </summary>
    [System.Serializable]
    public struct CylinderSegment
    {
        [SerializeField, Range(-180f, 180f)]
        private float _rotation;

        [SerializeField, Range(0f, 360f)]
        private float _arcDegrees;

        [SerializeField]
        private float _bottom;

        [SerializeField]
        private float _top;

        public float ArcDegrees => _arcDegrees;
        public float Rotation => _rotation;
        public float Bottom => _bottom;
        public float Top => _top;

        public bool IsInfiniteHeight => Bottom > Top;
        public bool IsInfiniteArc => ArcDegrees >= 360;

        public CylinderSegment(float rotation,
            float arcDegrees, float bottom, float top)
        {
            _rotation = rotation;
            _arcDegrees = arcDegrees;
            _bottom = bottom;
            _top = top;
        }

        public static CylinderSegment Default()
        {
            return new CylinderSegment()
            {
                _rotation = 0f,
                _arcDegrees = 360f,
                _bottom = -1,
                _top = 1
            };
        }

        public static CylinderSegment Infinite()
        {
            return new CylinderSegment()
            {
                _rotation = 0f,
                _arcDegrees = 360f,
                _bottom = 1,
                _top = -1
            };
        }
    }
}
