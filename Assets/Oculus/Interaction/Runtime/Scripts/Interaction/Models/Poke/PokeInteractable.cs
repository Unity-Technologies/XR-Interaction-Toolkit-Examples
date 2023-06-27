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
using Oculus.Interaction.Surfaces;
using System;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    public class PokeInteractable : PointerInteractable<PokeInteractor, PokeInteractable>
    {
        [Tooltip("Represents the pokeable surface area of this interactable.")]
        [SerializeField, Interface(typeof(ISurfacePatch))]
        private UnityEngine.Object _surfacePatch;
        public ISurfacePatch SurfacePatch { get; private set; }

        [SerializeField]
        [FormerlySerializedAs("_maxDistance")]
        [Tooltip("The distance required for a poke interactor to enter hovering, " +
                 "measured along the normal to the surface (in meters)")]
        private float _enterHoverNormal = 0.03f;
        [SerializeField]
        [Tooltip("The distance required for a poke interactor to enter hovering, " +
                 "measured along the tangent plane to the surface (in meters)")]
        private float _enterHoverTangent = 0;
        [SerializeField]
        [Tooltip("The distance required for a poke interactor to exit hovering, " +
                 "measured along the normal to the surface (in meters)")]
        private float _exitHoverNormal = 0.05f;
        [SerializeField]
        [Tooltip("The distance required for a poke interactor to exit hovering, " +
                 "measured along the tangent plane to the surface (in meters)")]
        private float _exitHoverTangent = 0f;
        [SerializeField]
        [FormerlySerializedAs("_releaseDistance")]
        [Tooltip("If greater than zero, " +
                 "the distance required for a selecting poke interactor to cancel selection, " +
                 "measured along the negative normal to the surface (in meters)")]
        private float _cancelSelectNormal = 0.3f;
        [SerializeField]
        [Tooltip("If greater than zero, " +
                 "the distance required for a selecting poke interactor to cancel selection, " +
                 "measured along the tangent plane to the surface (in meters)")]
        private float _cancelSelectTangent = 0.03f;

        [Serializable]
        public class MinThresholdsConfig
        {
            [Tooltip("If true, minimum thresholds will be applied.")]
            public bool Enabled;
            [Tooltip("The minimum distance required for a poke interactor to surpass before " +
                     "being able to hover, measured along the normal to the surface (in meters)")]
            public float MinNormal = 0.01f;
        }

        [SerializeField]
        [Tooltip("If enabled, a poke interactor must approach the surface from at least a " +
                 "minimum distance of the surface (in meters)")]
        private MinThresholdsConfig _minThresholds =
            new MinThresholdsConfig()
            {
                Enabled = false,
                MinNormal = 0.01f
            };

        [Serializable]
        public class DragThresholdsConfig
        {
            [Tooltip("If true, drag thresholds will be applied.")]
            public bool Enabled;
            [FormerlySerializedAs("ZThreshold")]
            [Tooltip("The distance a poke interactor must travel to be treated as a press, " +
                     "measured as a distance along the normal to the surface (in meters)")]
            public float DragNormal;
            [FormerlySerializedAs("SurfaceThreshold")]
            [Tooltip("The distance a poke interactor must travel to be treated as a drag, " +
                     "measured as a distance along the tangent plane to the surface (in meters)")]
            public float DragTangent;
            [Tooltip("The curve that a poke interactor will use to ease when transitioning " +
                     "between a press and drag state.")]
            public ProgressCurve DragEaseCurve;
        }

        [SerializeField]
        [FormerlySerializedAs("_dragThresholding")]
        [Tooltip("If enabled, drag thresholds will be applied in 3D space. " +
                 "Useful for disambiguating press vs drag and suppressing move pointer events " +
                 "when a poke interactor follows a pressing motion.")]
        private DragThresholdsConfig _dragThresholds =
            new DragThresholdsConfig()
            {
                Enabled = true,
                DragNormal = 0.01f,
                DragTangent = 0.01f,
                DragEaseCurve = new ProgressCurve(AnimationCurve.EaseInOut(0, 0, 1, 1), 0.05f)
            };

        [Serializable]
        public class PositionPinningConfig
        {
            [Tooltip("If true, position pinning will be applied.")]
            public bool Enabled;
            [Tooltip("The distance over which a poke interactor drag motion will be remapped " +
                     "measured along the tangent plane to the surface (in meters)")]
            public float MaxPinDistance;
            [Tooltip("The poke interactor position will be remapped along this curve from the " +
                "initial touch point to the current position on surface.")]
            public AnimationCurve PinningEaseCurve;
            [Tooltip("In cases where a resync is necessary between the pinned position and the " +
                "unpinned position, this time-based curve will be used.")]
            public ProgressCurve ResyncCurve;
        }

        [SerializeField]
        [Tooltip("If enabled, position pinning will be applied to surface motion during drag. " +
                 "Useful for adding a sense of friction to initial drag motion.")]
        private PositionPinningConfig _positionPinning =
            new PositionPinningConfig()
            {
                Enabled = false,
                MaxPinDistance = 0.075f,
                PinningEaseCurve = AnimationCurve.EaseInOut(0.2f, 0, 1, 1),
                ResyncCurve = new ProgressCurve(AnimationCurve.EaseInOut(0, 0, 1, 1), 0.2f)
            };

        [Serializable]
        public class RecoilAssistConfig
        {
            [Tooltip("If true, recoil assist will be applied.")]
            public bool Enabled;

            [Tooltip("If true, DynamicDecayCurve will be used to decay the max distance based on the normal velocity.")]
            public bool UseDynamicDecay;
            [Tooltip("A function of the normal movement ratio to determine the rate of decay.")]
            public AnimationCurve DynamicDecayCurve;

            [Tooltip("Expand recoil window when fast Z motion is detected.")]
            public bool UseVelocityExpansion;
            [Tooltip("When average velocity in interactable Z is greater than min speed, the recoil window will begin expanding.")]
            public float VelocityExpansionMinSpeed;
            [Tooltip("Full recoil window expansion reached at this speed.")]
            public float VelocityExpansionMaxSpeed;
            [Tooltip("Window will expand by this distance when average Z velocity reaches max speed.")]
            public float VelocityExpansionDistance;
            [Tooltip("Window will contract toward ExitDistance at this rate (in meters) per second when velocity lowers.")]
            public float VelocityExpansionDecayRate;

            [Tooltip("The distance over which a poke interactor must surpass to trigger " +
                     "an early unselect, measured along the normal to the surface (in meters)")]
            public float ExitDistance;
            [Tooltip("When in recoil, the distance which a poke interactor must surpass to trigger " +
                     "a subsequent select, measured along the negative normal to the surface (in meters)")]
            public float ReEnterDistance;
        }

        [SerializeField]
        [Tooltip("If enabled, recoil assist will affect unselection and reselection criteria. " +
                 "Useful for triggering unselect in response to a smaller motion in the negative " +
                 "direction from a surface.")]
        private RecoilAssistConfig _recoilAssist =
            new RecoilAssistConfig()
            {
                Enabled = false,
                UseDynamicDecay = false,
                DynamicDecayCurve = new AnimationCurve(
                    new Keyframe(0f, 50f), new Keyframe(0.9f, 0.5f, -47, -47)),
                UseVelocityExpansion = false,
                VelocityExpansionMinSpeed = 0.4f,
                VelocityExpansionMaxSpeed = 1.4f,
                VelocityExpansionDistance = 0.055f,
                VelocityExpansionDecayRate = 0.125f,
                ExitDistance = 0.02f,
                ReEnterDistance = 0.02f
            };

        [SerializeField, Optional]
        [Tooltip("(Meters, World) The threshold below which distances near this surface " +
                 "are treated as equal in depth for the purposes of ranking.")]
        private float _closeDistanceThreshold = 0.001f;

        [SerializeField, Optional]
        private int _tiebreakerScore = 0;

        #region Properties

        public float EnterHoverNormal
        {
            get
            {
                return _enterHoverNormal;
            }

            set
            {
                _enterHoverNormal = value;
            }
        }

        public float EnterHoverTangent
        {
            get
            {
                return _enterHoverTangent;
            }

            set
            {
                _enterHoverTangent = value;
            }
        }

        public float ExitHoverNormal
        {
            get
            {
                return _exitHoverNormal;
            }

            set
            {
                _exitHoverNormal = value;
            }
        }

        public float ExitHoverTangent
        {
            get
            {
                return _exitHoverTangent;
            }

            set
            {
                _exitHoverTangent = value;
            }
        }

        public float CancelSelectNormal
        {
            get
            {
                return _cancelSelectNormal;
            }

            set
            {
                _cancelSelectNormal = value;
            }
        }

        public float CancelSelectTangent
        {
            get
            {
                return _cancelSelectTangent;
            }

            set
            {
                _cancelSelectTangent = value;
            }
        }

        public float CloseDistanceThreshold
        {
            get
            {
                return _closeDistanceThreshold;
            }
            set
            {
                _closeDistanceThreshold = value;
            }
        }

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

        public MinThresholdsConfig MinThresholds
        {
            get
            {
                return _minThresholds;
            }

            set
            {
                _minThresholds = value;
            }
        }

        public DragThresholdsConfig DragThresholds
        {
            get
            {
                return _dragThresholds;
            }

            set
            {
                _dragThresholds = value;
            }
        }

        public PositionPinningConfig PositionPinning
        {
            get
            {
                return _positionPinning;
            }

            set
            {
                _positionPinning = value;
            }
        }

        public RecoilAssistConfig RecoilAssist
        {
            get
            {
                return _recoilAssist;
            }

            set
            {
                _recoilAssist = value;
            }
        }

        #endregion

        protected override void Awake()
        {
            base.Awake();
            SurfacePatch = _surfacePatch as ISurfacePatch;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(SurfacePatch, nameof(SurfacePatch));

            // _exitHover thresholds must be at a minimum the magnitude of the _enterHover thresholds
            _exitHoverNormal =
                Mathf.Max(_enterHoverNormal, _exitHoverNormal);

            _exitHoverTangent =
                Mathf.Max(_enterHoverTangent, _exitHoverTangent);

            // If non-zero, _cancelSelectTangent must be at a minimum the magnitude of _exitHoverTangent
            if (_cancelSelectTangent > 0)
            {
                _cancelSelectTangent =
                    Mathf.Max(_exitHoverTangent, _cancelSelectTangent);
            }

            if (_minThresholds.Enabled && _minThresholds.MinNormal > 0f)
            {
                _minThresholds.MinNormal =
                    Mathf.Min(_minThresholds.MinNormal,
                    _enterHoverNormal);
            }
            this.EndStart(ref _started);
        }

        public bool ClosestSurfacePatchHit(Vector3 point, out SurfaceHit hit)
        {
            return SurfacePatch.ClosestSurfacePoint(point, out hit);
        }

        public bool ClosestBackingSurfaceHit(Vector3 point, out SurfaceHit hit)
        {
            return SurfacePatch.BackingSurface.ClosestSurfacePoint(point, out hit);
        }

        #region Inject

        public void InjectAllPokeInteractable(ISurfacePatch surfacePatch)
        {
            InjectSurfacePatch(surfacePatch);
        }

        public void InjectSurfacePatch(ISurfacePatch surfacePatch)
        {
            _surfacePatch = surfacePatch as UnityEngine.Object;
            SurfacePatch = surfacePatch;
        }

        #endregion
    }
}
