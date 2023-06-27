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
    public class CenterEyeOffset : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHmd))]
        private UnityEngine.Object _hmd;
        public IHmd Hmd { get; private set; }

        [SerializeField]
        private Vector3 _offset;

        [SerializeField]
        private Quaternion _rotation = Quaternion.identity;

        private Pose _cachedPose = Pose.identity;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Hmd = _hmd as IHmd;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hmd, nameof(Hmd));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hmd.WhenUpdated += HandleUpdated;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hmd.WhenUpdated -= HandleUpdated;
            }
        }

        private void HandleUpdated()
        {
            if (Hmd.TryGetRootPose(out Pose rootPose))
            {
                GetOffset(ref _cachedPose);
                _cachedPose.Postmultiply(rootPose);
                transform.SetPose(_cachedPose);
            }
        }

        public void GetOffset(ref Pose pose)
        {
            pose.position = _offset;
            pose.rotation = _rotation;
        }

        public void GetWorldPose(ref Pose pose)
        {
            pose.position = this.transform.position;
            pose.rotation = this.transform.rotation;
        }

        #region Inject

        public void InjectOffset(Vector3 offset)
        {
            _offset = offset;
        }

        public void InjectRotation(Quaternion rotation)
        {
            _rotation = rotation;
        }

        public void InjectAllCenterEyeOffset(IHmd hmd,
            Vector3 offset, Quaternion rotation)
        {
            InjectHmd(hmd);
            InjectOffset(offset);
            InjectRotation(rotation);
        }

        public void InjectHmd(IHmd hmd)
        {
            Hmd = hmd;
            _hmd = hmd as UnityEngine.Object;
        }

        #endregion
    }
}
