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
    public class TeleportArcVisual : MonoBehaviour
    {
        [SerializeField]
        private TeleportInteractor _interactor;

        [SerializeField]
        private LineRenderer _arcRenderer;

        private Vector3[] _positions;

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_interactor, nameof(_interactor));
            this.AssertField(_arcRenderer, nameof(_arcRenderer));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _interactor.WhenPostprocessed += HandleInteractorPostProcessed;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _interactor.WhenPostprocessed -= HandleInteractorPostProcessed;
            }
        }

        protected virtual void HandleInteractorPostProcessed()
        {
            int pointsCount = _interactor.TeleportArc.PointsCount;
            if (_positions == null
                || _positions.Length != pointsCount)
            {
                _positions = new Vector3[pointsCount];
                _arcRenderer.positionCount = pointsCount;
            }

            for (int i = 0; i < pointsCount; i++)
            {
                _positions[i] = _interactor.TeleportArc.PointAtIndex(i);
            }

            _arcRenderer.SetPositions(_positions);
        }

        #region Inject

        public void InjectAllTeleportArcVisual(TeleportInteractor interactor,
            LineRenderer arcRenderer)
        {
            InjectInteractor(interactor);
            InjectArcRenderer(arcRenderer);
        }

        public void InjectInteractor(TeleportInteractor interactor)
        {
            _interactor = interactor;
        }

        public void InjectArcRenderer(LineRenderer arcRenderer)
        {
            _arcRenderer = arcRenderer;
        }
        #endregion
    }
}
