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

using Oculus.Interaction.Surfaces;
using UnityEngine;

namespace Oculus.Interaction
{
    public class RayInteractable : PointerInteractable<RayInteractor, RayInteractable>
    {
        [SerializeField, Interface(typeof(ISurface))]
        private UnityEngine.Object _surface;
        public ISurface Surface { get; private set; }

        [SerializeField, Optional, Interface(typeof(ISurface))]
        private UnityEngine.Object _selectSurface = null;
        private ISurface SelectSurface;

        [SerializeField, Optional, Interface(typeof(IMovementProvider))]
        private UnityEngine.Object _movementProvider;
        private IMovementProvider MovementProvider { get; set; }

        [SerializeField, Optional]
        private int _tiebreakerScore = 0;

        #region Properties
        public int TiebreakerScore
        {
            get
            {
                return _tiebreakerScore;
            }
            set
            {
                _tiebreakerScore = value;
            }
        }
        #endregion

        protected override void Awake()
        {
            base.Awake();
            Surface = _surface as ISurface;
            SelectSurface = _selectSurface as ISurface;
            MovementProvider = _movementProvider as IMovementProvider;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Surface, nameof(Surface));
            if (_selectSurface != null)
            {
                this.AssertField(SelectSurface, nameof(SelectSurface));
            }
            else
            {
                SelectSurface = Surface;
                _selectSurface = SelectSurface as MonoBehaviour;
            }
            this.EndStart(ref _started);
        }

        public bool Raycast(Ray ray, out SurfaceHit hit, in float maxDistance, bool selectSurface)
        {
            hit = new SurfaceHit();
            ISurface surface = selectSurface ? SelectSurface : Surface;
            return surface.Raycast(ray, out hit, maxDistance);
        }

        public IMovement GenerateMovement(in Pose to, in Pose source)
        {
            if (MovementProvider == null)
            {
                return null;
            }
            IMovement movement = MovementProvider.CreateMovement();
            movement.StopAndSetPose(source);
            movement.MoveTo(to);
            return movement;
        }

        #region Inject

        public void InjectAllRayInteractable(ISurface surface)
        {
            InjectSurface(surface);
        }

        public void InjectSurface(ISurface surface)
        {
            Surface = surface;
            _surface = surface as UnityEngine.Object;
        }

        public void InjectOptionalSelectSurface(ISurface surface)
        {
            SelectSurface = surface;
            _selectSurface = surface as UnityEngine.Object;
        }

        public void InjectOptionalMovementProvider(IMovementProvider provider)
        {
            _movementProvider = provider as UnityEngine.Object;
            MovementProvider = provider;
        }

        #endregion
    }
}
