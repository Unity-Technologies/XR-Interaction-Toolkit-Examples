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

namespace Oculus.Interaction.Samples
{
    public class ListSnapPoseDelegateRoundedBoxVisual : MonoBehaviour
    {
        [SerializeField]
        private ListSnapPoseDelegate _listSnapPoseDelegate;

        [SerializeField]
        private RoundedBoxProperties _properties;

        [SerializeField]
        private SnapInteractable _snapInteractable;

        [SerializeField]
        private float _minSize = 0f;

        [SerializeField]
        private ProgressCurve _curve;

        private float _targetWidth = 0;
        private float _startWidth = 0;

        protected virtual void LateUpdate()
        {
            float targetWidth = Mathf.Max(_listSnapPoseDelegate.Size, _minSize);
            if (targetWidth != _targetWidth)
            {
                _targetWidth = targetWidth;
                _curve.Start();
                _startWidth = _properties.Width;
            }

            _properties.Width = Mathf.Lerp(_startWidth, _targetWidth, _curve.Progress());
            _properties.BorderColor =
                _snapInteractable.Interactors.Count != _snapInteractable.SelectingInteractors.Count
                    ? new Color(1, 1, 1, 1)
                    : new Color(1, 1, 1, 0.5f);
        }
    }
}
