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
using UnityEngine.Assertions;

namespace Oculus.Interaction.DistanceReticles
{
    public class DistantInteractionTubeVisual : DistantInteractionLineVisual
    {
        [SerializeField]
        private TubeRenderer _tubeRenderer;

        private TubePoint[] _tubePoints;

        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(_tubeRenderer);
        }

        protected override void RenderLine(Vector3[] linePoints)
        {
            InitializeArcPoints(linePoints);
            _tubeRenderer.RenderTube(_tubePoints, Space.World);
        }

        protected override void HideLine()
        {
            _tubeRenderer.Hide();
        }

        private void InitializeArcPoints(Vector3[] linePoints)
        {
            if (_tubePoints == null
                || _tubePoints.Length < linePoints.Length)
            {
                _tubePoints = new TubePoint[linePoints.Length];
            }

            float totalLength = 0f;
            for (int i = 1; i < linePoints.Length; i++)
            {
                totalLength += (linePoints[i] - linePoints[i - 1]).magnitude;
            }

            for (int i = 0; i < linePoints.Length; i++)
            {
                Vector3 difference = i == 0 ? linePoints[i + 1] - linePoints[i]
                    : linePoints[i] - linePoints[i - 1];
                _tubePoints[i].position = linePoints[i];
                _tubePoints[i].rotation = Quaternion.LookRotation(difference);
                _tubePoints[i].relativeLength = i == 0 ? 0f
                    : _tubePoints[i - 1].relativeLength + (difference.magnitude / totalLength);
            }
        }

        #region Inject

        public void InjectAllDistantInteractionPolylineVisual(IDistanceInteractor interactor)
        {
            InjectDistanceInteractor(interactor);
        }

        #endregion
    }
}
