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
    /// This interactor group allows only the first interactor requesting it
    /// to be the one in Hover, and the rest will be disabled until it
    /// unhovers.
    /// </summary>
    public class FirstHoverInteractorGroup : InteractorGroup
    {
        private IInteractor _bestInteractor = null;

        private static readonly InteractorPredicate ShouldHoverPredicate =
            (interactor, index) => interactor.ShouldHover;

        private static readonly InteractorPredicate IsNormalAndShouldHoverPredicate =
            (interactor, index) => interactor.State == InteractorState.Normal && interactor.ShouldHover;

        private static readonly InteractorPredicate HasCandidatePredicate =
            (interactor, index) => interactor.HasCandidate;

        public override bool ShouldHover
        {
            get
            {
                if (State != InteractorState.Normal)
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

                return _bestInteractor != null && _bestInteractor.ShouldUnhover;
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

                return _bestInteractor != null && _bestInteractor.ShouldSelect;
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
            int interactorIndex = InteractorIndexWithBestCandidate(ShouldHoverPredicate);
            if (interactorIndex >= 0)
            {
                _bestInteractor = Interactors[interactorIndex];
                _bestInteractor.Hover();
                DisableAllExcept(_bestInteractor);
                State = _bestInteractor.State;
            }
        }

        public override void Unhover()
        {
            if (_bestInteractor != null)
            {
                _bestInteractor.Unhover();
                State = _bestInteractor.State;
                if (_bestInteractor.State == InteractorState.Normal)
                {
                    _bestInteractor = null;
                }
            }
        }

        public override void Select()
        {
            if (_bestInteractor != null)
            {
                _bestInteractor.Select();
                State = _bestInteractor.State;
            }
        }

        public override void Unselect()
        {
            if (_bestInteractor != null)
            {
                _bestInteractor.Unselect();
                State = _bestInteractor.State;
            }
        }

        public override void Enable()
        {
            if (State == InteractorState.Disabled
                || State == InteractorState.Normal)
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
                return _bestInteractor != null && _bestInteractor.HasInteractable;
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
        public void InjectAllInteractorGroupFirstHover(List<IInteractor> interactors)
        {
            base.InjectAllInteractorGroupBase(interactors);
        }
        #endregion
    }
}
