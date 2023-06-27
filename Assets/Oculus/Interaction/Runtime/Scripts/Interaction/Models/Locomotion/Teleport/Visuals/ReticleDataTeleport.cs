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

namespace Oculus.Interaction.DistanceReticles
{
    public class ReticleDataTeleport : MonoBehaviour, IReticleData
    {
        [SerializeField, Optional]
        private Transform _snapPoint;

        [SerializeField, Optional]
        private MaterialPropertyBlockEditor _materialBlock;

        private static readonly int _highlightShaderID = Shader.PropertyToID("_Highlight");

        public enum TeleportReticleMode
        {
            Hidden,
            ValidTarget,
            InvalidTarget
        }

        [SerializeField]
        private TeleportReticleMode _reticleMode = TeleportReticleMode.ValidTarget;
        public TeleportReticleMode ReticleMode
        {
            get
            {
                return _reticleMode;
            }
            set
            {
                _reticleMode = value;
            }
        }

        public Vector3 ProcessHitPoint(Vector3 hitPoint)
        {
            if (_snapPoint != null)
            {
                return _snapPoint.position;
            }
            return hitPoint;
        }

        public void Highlight(bool highlight)
        {
            if(_materialBlock != null)
            {
                _materialBlock.MaterialPropertyBlock.SetFloat(_highlightShaderID, highlight ? 1f : 0f);
                _materialBlock.UpdateMaterialPropertyBlock();
            }
        }

        #region Inject
        public void InjectOptionalSnapPoint(Transform snapPoint)
        {
            _snapPoint = snapPoint;
        }
        #endregion
    }
}
