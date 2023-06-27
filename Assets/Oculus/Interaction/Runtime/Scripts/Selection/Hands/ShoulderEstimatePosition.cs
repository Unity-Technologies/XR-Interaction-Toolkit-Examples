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

using Oculus.Interaction.Input;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Updates its transform to the estimated shoulder position and rotation.
    /// Estimated pose is based on an offset from the head, taking in count
    /// just the rotation Yaw. Hand is required to know not just the handedness
    /// but also alter the scale of the offset.
    /// </summary>
    public class ShoulderEstimatePosition : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHmd))]
        private UnityEngine.Object _hmd;
        private IHmd Hmd { get; set; }

        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        private IHand Hand { get; set; }

        private static readonly Vector3 ShoulderOffset = new Vector3(0.13f, -0.25f, -0.13f);

        protected bool _started;

        protected virtual void Awake()
        {
            Hmd = _hmd as IHmd;
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hmd, nameof(_hmd));
            this.AssertField(Hand, nameof(_hand));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hmd.WhenUpdated += HandleHmdUpdated;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hmd.WhenUpdated -= HandleHmdUpdated;
            }
        }

        protected virtual void HandleHmdUpdated()
        {
            Hmd.TryGetRootPose(out Pose headPose);
            Quaternion shoulderYaw = Quaternion.Euler(0f, headPose.rotation.eulerAngles.y, 0f);
            Vector3 offset = ShoulderOffset * Hand.Scale;
            if (Hand.Handedness == Handedness.Left)
            {
                offset.x = -offset.x;
            }
            Vector3 projectionOrigin = headPose.position + shoulderYaw * offset;

            this.transform.SetPositionAndRotation(projectionOrigin, shoulderYaw);
        }

        #region Inject

        public void InjectAllShoulderPosition(IHmd hmd,
            IHand hand)
        {
            InjectHmd(hmd);
            InjectHand(hand);
        }

        public void InjectHmd(IHmd hmd)
        {
            _hmd = hmd as UnityEngine.Object;
            Hmd = hmd;
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        #endregion
    }
}
