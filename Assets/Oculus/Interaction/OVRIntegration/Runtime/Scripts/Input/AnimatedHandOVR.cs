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

namespace Oculus.Interaction.Input
{
    public class AnimatedHandOVR : MonoBehaviour
    {
        public enum AllowThumbUp
        {
            Always,
            GripRequired,
            TriggerAndGripRequired,
        }
        public const string ANIM_LAYER_NAME_POINT = "Point Layer";
        public const string ANIM_LAYER_NAME_THUMB = "Thumb Layer";
        public const string ANIM_PARAM_NAME_FLEX = "Flex";
        public const string ANIM_PARAM_NAME_PINCH = "Pinch";
        public const string ANIM_PARAM_NAME_INDEX_SLIDE = "IndexSlide";

        [SerializeField]
        private OVRInput.Controller _controller = OVRInput.Controller.None;
        [SerializeField]
        private Animator _animator = null;
        [SerializeField]
        private AllowThumbUp _allowThumbUp = AllowThumbUp.TriggerAndGripRequired;

        [Header("Animation Speed")]
        [SerializeField]
        private float _animFlexhGain = 35;
        [SerializeField]
        private float _animPinchGain = 35;
        [SerializeField]
        private float _animPointAndThumbsUpGain = 20;

        private int _animLayerIndexThumb = -1;
        private int _animLayerIndexPoint = -1;
        private int _animParamIndexFlex = Animator.StringToHash(ANIM_PARAM_NAME_FLEX);
        private int _animParamPinch = Animator.StringToHash(ANIM_PARAM_NAME_PINCH);
        private int _animParamIndexSlide = Animator.StringToHash(ANIM_PARAM_NAME_INDEX_SLIDE);

        private bool _isGivingThumbsUp = false;
        private float _pointBlend = 0.0f;
        private float _slideBlend = 0.0f;

        private float _thumbsUpBlend = 0.0f;
        private float _pointTarget = 0.0f;
        private float _slideTarget = 0.0f;

        private float _animFlex = 0;
        private float _animPinch = 0;

        private const float TRIGGER_MAX = 0.95f;

        protected virtual void Start()
        {
            _animLayerIndexPoint = _animator.GetLayerIndex(ANIM_LAYER_NAME_POINT);
            _animLayerIndexThumb = _animator.GetLayerIndex(ANIM_LAYER_NAME_THUMB);
        }

        protected virtual void Update()
        {
            UpdateCapTouchStates();

            _pointBlend = Mathf.Lerp(_pointBlend, _pointTarget, _animPointAndThumbsUpGain * Time.deltaTime);
            _slideBlend = Mathf.Lerp(_slideBlend, _slideTarget, _animPointAndThumbsUpGain * Time.deltaTime);
            _thumbsUpBlend = Mathf.Lerp(_thumbsUpBlend, _isGivingThumbsUp ? 1 : 0, _animPointAndThumbsUpGain * Time.deltaTime);

            UpdateAnimStates();
        }

        private void UpdateCapTouchStates()
        {
            float indexCurl = OVRControllerUtility.GetIndexCurl(_controller);
            float indexSlide = OVRControllerUtility.GetIndexSlide(_controller);
            _pointTarget = 1 - indexCurl;
            _slideTarget = indexSlide;

            bool triggerThumbsUp = _allowThumbUp == AllowThumbUp.Always ||
                (_allowThumbUp == AllowThumbUp.GripRequired
                    && OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, _controller) >= TRIGGER_MAX) ||
                (_allowThumbUp == AllowThumbUp.TriggerAndGripRequired
                    && OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, _controller) >= TRIGGER_MAX
                    && OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, _controller) >= TRIGGER_MAX);

            _isGivingThumbsUp = !OVRInput.Get(OVRInput.NearTouch.PrimaryThumbButtons, _controller)
                && !OVRInput.Get(OVRInput.Button.One, _controller)
                && !OVRInput.Get(OVRInput.Button.Two, _controller)
                && !OVRInput.Get(OVRInput.Button.Three, _controller)
                && !OVRInput.Get(OVRInput.Button.Four, _controller)
                && !OVRInput.Get(OVRInput.Button.PrimaryThumbstick, _controller)
                && OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, _controller).magnitude == 0
                && triggerThumbsUp;
        }

        private void UpdateAnimStates()
        {
            // Flex
            // blend between open hand and fully closed fist
            float flex = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, _controller);
            _animFlex = Mathf.Lerp(_animFlex, flex, _animFlexhGain * Time.deltaTime);
            _animator.SetFloat(_animParamIndexFlex, _animFlex);

            // Pinch
            float pinchAmount = OVRControllerUtility.GetPinchAmount(_controller);
            _animPinch = Mathf.Lerp(_animPinch, pinchAmount, _animPinchGain * Time.deltaTime);
            _animator.SetFloat(_animParamPinch, _animPinch);

            // Point
            _animator.SetLayerWeight(_animLayerIndexPoint, _pointBlend);
            _animator.SetFloat(_animParamIndexSlide, _slideBlend);

            // Thumbs up
            _animator.SetLayerWeight(_animLayerIndexThumb, _thumbsUpBlend);
        }


        #region Inject

        public void InjectAllAnimatedHandOVR(OVRInput.Controller controller, Animator animator)
        {
            InjectController(controller);
            InjectAnimator(animator);
        }

        public void InjectController(OVRInput.Controller controller)
        {
            _controller = controller;
        }

        public void InjectAnimator(Animator animator)
        {
            _animator = animator;
        }

        #endregion
    }
}
