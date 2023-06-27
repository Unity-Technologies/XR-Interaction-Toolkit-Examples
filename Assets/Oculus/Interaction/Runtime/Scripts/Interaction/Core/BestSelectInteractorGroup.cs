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

using System.Collections.Generic;

namespace Oculus.Interaction
{
    /// <summary>
    /// This interactor group allows many Interactors to be in Hover state
    /// but upon selection, the one with the highest priority will be the
    /// one Selecting and the rest will be disabled until it unselects.
    /// </summary>
    public class BestSelectInteractorGroup : InteractorGroup
    {
        private IInteractor _bestInteractor = null;

        private static readonly InteractorPredicate IsNormalAndShouldHoverPredicate =
            (interactor, index) => interactor.State == InteractorState.Normal && interactor.ShouldHover;

        private static readonly InteractorPredicate IsHoverAndShoulUnhoverPredicate =
            (interactor, index) => interactor.State == InteractorState.Hover && interactor.ShouldUnhover;

        private static readonly InteractorPredicate IsHoverAndShoulSelectPredicate =
            (interactor, index) => interactor.State == InteractorState.Hover && interactor.ShouldSelect;

        private static readonly InteractorPredicate HasCandidatePredicate =
            (interactor, index) => interactor.HasCandidate;

        private static readonly InteractorPredicate HasInteractablePredicate =
            (interactor, index) => interactor.HasInteractable;

        public override bool ShouldHover
        {
            get
            {
                if (State != InteractorState.Normal
                    && State != InteractorState.Hover)
                {
                    return false;
                }
                return AnyInteractor(IsNormalAndShouldHoverPredicate);
            }
        }

        public override bool ShouldUnhover
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }
                return AnyInteractor(IsHoverAndShoulUnhoverPredicate);
            }
        }

        public override bool ShouldSelect
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                return AnyInteractor(IsHoverAndShoulSelectPredicate);
            }
        }

        public override bool ShouldUnselect
        {
            get
            {
                if (State != InteractorState.Select)
                {
                    return false;
                }

                return _bestInteractor != null && _bestInteractor.ShouldUnselect;
            }
        }

        public override void Hover()
        {
            bool anyHovered = false;
            foreach (IInteractor interactor in Interactors)
            {
                if (interactor.State == InteractorState.Normal
                    && interactor.ShouldHover)
                {
                    interactor.Hover();
                    anyHovered = true;
                }
            }
            if (anyHovered)
            {
                State = InteractorState.Hover;
            }
        }

        public override void Unhover()
        {
            bool allUnhovered = State == InteractorState.Hover;
            foreach (IInteractor interactor in Interactors)
            {
                if (interactor.State == InteractorState.Hover)
                {
                    if (interactor.ShouldUnhover)
                    {
                        interactor.Unhover();
                    }

                    if (interactor.State == InteractorState.Hover)
                    {
                        allUnhovered = false;
                    }
                }
            }

            if (allUnhovered)
            {
                State = InteractorState.Normal;
            }
        }

        public override void Select()
        {
            int interactorIndex = InteractorIndexWithBestCandidate((interactor, index) =>
               interactor.State == InteractorState.Hover
               && interactor.ShouldSelect);
            if (interactorIndex < 0)
            {
                return;
            }

            _bestInteractor = Interactors[interactorIndex];
            _bestInteractor.Select();
            DisableAllExcept(_bestInteractor);
            State = _bestInteractor.State;
        }

        public override void Unselect()
        {
            if (_bestInteractor != null)
            {
                _bestInteractor.Unselect();
                State = _bestInteractor.State;
                if (_bestInteractor.State == InteractorState.Hover)
                {
                    _bestInteractor = null;
                }
            }
        }

        public override void Enable()
        {
            if (State == InteractorState.Disabled
                || State == InteractorState.Normal
                || State == InteractorState.Hover)
            {
                base.Enable();
                return;
            }

            if (!UpdateActiveState())
            {
                return;
            }

            if (_bestInteractor != null)
            {
                _bestInteractor.Enable();
                State = _bestInteractor.State;
            }
        }

        public override void Disable()
        {
            base.Disable();
            _bestInteractor = null;
        }

        public override void Process()
        {
            if (State == InteractorState.Hover
                && ShouldHover)
            {
                Hover();
            }
            base.Process();
        }

        public override bool HasCandidate
        {
            get
            {
                if (_bestInteractor != null)
                {
                    return _bestInteractor.HasCandidate;
                }
                return AnyInteractor(HasCandidatePredicate);
            }
        }
        public override bool HasInteractable
        {
            get
            {
                if (_bestInteractor != null)
                {
                    return _bestInteractor.HasInteractable;
                }
                return AnyInteractor(HasInteractablePredicate);
            }
        }
        public override bool HasSelectedInteractable
        {
            get
            {
                return _bestInteractor != null && _bestInteractor.HasSelectedInteractable;
            }
        }
        public override object CandidateProperties
        {
            get
            {
                if (_bestInteractor != null)
                {
                    return _bestInteractor.CandidateProperties;
                }
                int interactorIndex = InteractorIndexWithBestCandidate(TruePredicate);
                return interactorIndex >= 0 ? Interactors[interactorIndex].CandidateProperties : null;
            }
        }


        #region Inject
        public void InjectAllInteractorGroupBestSelect(List<IInteractor> interactors)
        {
            base.InjectAllInteractorGroupBase(interactors);
        }
        #endregion
    }
}
