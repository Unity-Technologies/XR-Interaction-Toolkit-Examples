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

namespace Oculus.Interaction
{
    /// <summary>
    /// A Snap Interactor Follower Visual uses the state of a Snap Interactor to ease a transform
    /// between normal, hover and select targets.
    /// </summary>
    public class SnapInteractorFollowVisual : MonoBehaviour
    {
        [SerializeField]
        private SnapInteractor _snapInteractor;

        [SerializeField]
        private float _hoverOffset;

        [SerializeField]
        private ProgressCurve _easeCurve =
            new ProgressCurve(AnimationCurve.EaseInOut(0, 0, 1, 1), 0.1f);

        [SerializeField, Optional]
        private Transform _transform;

        #region Properties

        public float HoverOffset
        {
            get
            {
                return _hoverOffset;
            }
            set
            {
                _hoverOffset = value;
            }
        }

        public ProgressCurve EaseCurve
        {
            get
            {
                return _easeCurve;
            }
            set
            {
                _easeCurve = value;
            }
        }

        #endregion

        protected bool _started = false;
        private Pose _from;
        private Pose _to;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_snapInteractor, nameof(_snapInteractor));
            if (_transform == null)
            {
                _transform = transform;
            }

            _from = _to = ComputeTargetPose();
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _snapInteractor.WhenStateChanged += HandleStateChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _snapInteractor.WhenStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(InteractorStateChangeArgs args)
        {
            _from = transform.GetPose();
            _to = ComputeTargetPose();
            _easeCurve.Start();
        }

        protected virtual Pose ComputeTargetPose()
        {
            if (_snapInteractor.HasInteractable &&
                _snapInteractor.Interactable.PoseForInteractor(_snapInteractor, out Pose result))
            {
                if (_snapInteractor.State == InteractorState.Hover)
                {
                    result.position += _hoverOffset * result.forward;
                }
                return result;
            }
            return _snapInteractor.transform.GetPose();
        }

        protected virtual void Update()
        {
            _to = ComputeTargetPose();

            float progress = _easeCurve.Progress();
            Pose target = _from;
            target.Lerp(_to, progress);

            _transform.position = target.position;
            _transform.rotation = target.rotation;
        }

        #region Inject

        public void InjectAllSnapInteractorFollowVisual(SnapInteractor snapInteractor)
        {
            _snapInteractor = snapInteractor;
        }

        public void InjectOptionalTransform(Transform transform)
        {
            _transform = transform;
        }

        #endregion
    }
}
