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

namespace Oculus.Interaction.Input.Visuals
{
    public class OVRControllerVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IController))]
        private UnityEngine.Object _controller;

        public IController Controller;

        [SerializeField]
        private OVRControllerHelper _ovrControllerHelper;

        public bool ForceOffVisibility { get; set; }

        private bool _started = false;

        protected virtual void Awake()
        {
            Controller = _controller as IController;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Controller, nameof(Controller));
            this.AssertField(_ovrControllerHelper, nameof(_ovrControllerHelper));
            switch (Controller.Handedness)
            {
                case Handedness.Left:
                    _ovrControllerHelper.m_controller = OVRInput.Controller.LTouch;
                    break;
                case Handedness.Right:
                    _ovrControllerHelper.m_controller = OVRInput.Controller.RTouch;
                    break;
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Controller.WhenUpdated += HandleUpdated;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started && _controller != null)
            {
                Controller.WhenUpdated -= HandleUpdated;
            }
        }

        private void HandleUpdated()
        {
            if (!Controller.IsConnected ||
                ForceOffVisibility ||
                !Controller.TryGetPose(out Pose rootPose))
            {
                _ovrControllerHelper.gameObject.SetActive(false);
                return;
            }

            _ovrControllerHelper.gameObject.SetActive(true);
            transform.position = rootPose.position;
            transform.rotation = rootPose.rotation;
            float parentScale = transform.parent != null ? transform.parent.lossyScale.x : 1f;
            transform.localScale = Controller.Scale / parentScale * Vector3.one;
        }

        #region Inject

        public void InjectAllOVRControllerVisual(IController controller, OVRControllerHelper ovrControllerHelper)
        {
            InjectController(controller);
            InjectAllOVRControllerHelper(ovrControllerHelper);
        }

        public void InjectController(IController controller)
        {
            _controller = controller as UnityEngine.Object;
            Controller = controller;
        }

        public void InjectAllOVRControllerHelper(OVRControllerHelper ovrControllerHelper)
        {
            _ovrControllerHelper = ovrControllerHelper;
        }

        #endregion
    }
}
