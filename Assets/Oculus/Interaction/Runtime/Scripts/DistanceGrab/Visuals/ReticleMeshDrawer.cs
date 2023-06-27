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

using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Serialization;

namespace Oculus.Interaction.DistanceReticles
{
    public class ReticleMeshDrawer : InteractorReticle<ReticleDataMesh>
    {
        [FormerlySerializedAs("_handGrabber")]
        [SerializeField, Interface(typeof(IHandGrabInteractor), typeof(IInteractorView))]
        private UnityEngine.Object _handGrabInteractor;
        private IHandGrabInteractor HandGrabInteractor { get; set; }

        [SerializeField]
        private MeshFilter _filter;

        [SerializeField]
        private MeshRenderer _renderer;

        [SerializeField]
        private PoseTravelData _travelData = PoseTravelData.FAST;
        public PoseTravelData TravelData
        {
            get
            {
                return _travelData;
            }
            set
            {
                _travelData = value;
            }
        }

        protected override IInteractorView Interactor { get; set; }
        protected override Component InteractableComponent => HandGrabInteractor.TargetInteractable as Component;

        private Tween _tween;

        protected virtual void Reset()
        {
            _filter = this.GetComponent<MeshFilter>();
            _renderer = this.GetComponent<MeshRenderer>();
        }

        protected virtual void Awake()
        {
            HandGrabInteractor = _handGrabInteractor as IHandGrabInteractor;
            Interactor = _handGrabInteractor as IInteractorView;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Interactor, nameof(_handGrabInteractor));
            this.AssertField(HandGrabInteractor, nameof(_handGrabInteractor));
            this.AssertField(_filter, nameof(_filter));
            this.AssertField(_renderer, nameof(_renderer));
            this.EndStart(ref _started);
        }

        protected override void Draw(ReticleDataMesh dataMesh)
        {
            _filter.sharedMesh = dataMesh.Filter.sharedMesh;
            _filter.transform.localScale = dataMesh.Filter.transform.lossyScale;
            _renderer.enabled = true;

            Pose target = DestinationPose(dataMesh, HandGrabInteractor.GetTargetGrabPose());
            _tween = _travelData.CreateTween(dataMesh.Target.GetPose(), target);
        }

        protected override void Hide()
        {
            _tween = null;
            _renderer.enabled = false;
        }

        protected override void Align(ReticleDataMesh data)
        {
            Pose target = DestinationPose(data, HandGrabInteractor.GetTargetGrabPose());
            _tween.UpdateTarget(target);


            _tween.Tick();
            _filter.transform.SetPose(_tween.Pose);
        }

        private Pose DestinationPose(ReticleDataMesh data, Pose worldSnapPose)
        {
            Pose targetOffset = PoseUtils.Delta(worldSnapPose, data.Target.GetPose());
            HandGrabInteractor.HandGrabApi.Hand.GetRootPose(out Pose pose);
            pose.Premultiply(HandGrabInteractor.WristToGrabPoseOffset);
            pose.Premultiply(targetOffset);

            return pose;
        }

        #region Inject
        public void InjectAllReticleMeshDrawer(IHandGrabInteractor handGrabInteractor,
            MeshFilter filter, MeshRenderer renderer)
        {
            InjectHandGrabInteractor(handGrabInteractor);
            InjectFilter(filter);
            InjectRenderer(renderer);
        }

        public void InjectHandGrabInteractor(IHandGrabInteractor handGrabInteractor)
        {
            _handGrabInteractor = handGrabInteractor as UnityEngine.Object;
            HandGrabInteractor = handGrabInteractor;
            Interactor = handGrabInteractor as IInteractorView;
        }

        public void InjectFilter(MeshFilter filter)
        {
            _filter = filter;
        }

        public void InjectRenderer(MeshRenderer renderer)
        {
            _renderer = renderer;
        }
        #endregion
    }
}
