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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    public abstract class InteractorGroup : MonoBehaviour, IInteractor
    {
        [SerializeField, Interface(typeof(IInteractor))]
        protected List<UnityEngine.Object> _interactors;
        protected List<IInteractor> Interactors;

        [SerializeField, Interface(typeof(IActiveState)), Optional]
        private UnityEngine.Object _activeState;
        private IActiveState ActiveState = null;

        [SerializeField, Interface(typeof(ICandidateComparer)), Optional]
        protected UnityEngine.Object _candidateComparer;
        protected ICandidateComparer CandidateComparer = null;

        [SerializeField]
        private int _maxIterationsPerFrame = 3;
        public int MaxIterationsPerFrame
        {
            get
            {
                return _maxIterationsPerFrame;
            }
            set
            {
                _maxIterationsPerFrame = value;
            }
        }

        public object Data => null;

        public bool IsRootDriver { get; set; } = true;

        #region Abstract
        public abstract bool ShouldHover { get; }
        public abstract bool ShouldUnhover { get; }
        public abstract bool ShouldSelect { get; }
        public abstract bool ShouldUnselect { get; }

        public abstract void Hover();
        public abstract void Unhover();
        public abstract void Select();
        public abstract void Unselect();

        public abstract bool HasCandidate { get; }
        public abstract bool HasInteractable { get; }
        public abstract bool HasSelectedInteractable { get; }

        public abstract object CandidateProperties { get; }
        #endregion

        public event Action<InteractorStateChangeArgs> WhenStateChanged = delegate { };
        public event Action WhenPreprocessed = delegate { };
        public event Action WhenProcessed = delegate { };
        public event Action WhenPostprocessed = delegate { };

        protected delegate bool InteractorPredicate(IInteractor interactor, int index);
        protected static readonly InteractorPredicate TruePredicate =
            (interactor, index) => true;

        private InteractorState _state = InteractorState.Normal;
        public InteractorState State
        {
            get
            {
                return _state;
            }
            protected set
            {
                if (_state != value)
                {
                    var changeArgs = new InteractorStateChangeArgs(_state, value);
                    _state = value;
                    WhenStateChanged.Invoke(changeArgs);
                }
            }
        }

        private UniqueIdentifier _identifier;
        public int Identifier => _identifier.ID;

        protected bool _started;

        protected virtual void Awake()
        {
            _identifier = UniqueIdentifier.Generate();
            ActiveState = _activeState as IActiveState;
            Interactors = _interactors?.ConvertAll(mono => mono as IInteractor);
            CandidateComparer = _candidateComparer as ICandidateComparer;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertCollectionItems(Interactors, nameof(Interactors));
            foreach (IInteractor interactor in Interactors)
            {
                interactor.IsRootDriver = false;
            }

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Enable();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Disable();
            }
        }

        protected virtual void OnDestroy()
        {
            UniqueIdentifier.Release(_identifier);
        }

        /// <summary>
        /// Compares two InteractorStates indicating which one is higher
        /// in the Active State chain
        /// </summary>
        /// <param name="a">First state to compare</param>
        /// <param name="b">Seconds state to compare</param>
        /// <returns>1 if b is higher than a, -1 if a is higher than b. 0 of they are equal</returns>
        protected static int CompareStates(InteractorState a, InteractorState b)
        {
            if (a == b)
            {
                return 0;
            }
            if ((a == InteractorState.Disabled && b != InteractorState.Disabled)
                || (a == InteractorState.Normal && (b == InteractorState.Hover || b == InteractorState.Select))
                || (a == InteractorState.Hover && b == InteractorState.Select))
            {
                return 1;
            }
            return -1;
        }

        protected int InteractorIndexWithBestCandidate(InteractorPredicate predicate)
        {
            int bestCandidateIndex = -1;
            for (int i = 0; i < Interactors.Count; i++)
            {
                IInteractor interactor = Interactors[i];
                if (!predicate(interactor, i))
                {
                    continue;
                }

                if (bestCandidateIndex == -1
                    || CompareCandidates(bestCandidateIndex, i) > 0)
                {
                    bestCandidateIndex = i;
                }
            }

            return bestCandidateIndex;
        }

        protected bool AnyInteractor(InteractorPredicate predicate)
        {
            for (int i = 0; i < Interactors.Count; i++)
            {
                if (predicate(Interactors[i], i))
                {
                    return true;
                }
            }
            return false;
        }

        protected int CompareCandidates(int indexA, int indexB)
        {
            IInteractor a = Interactors[indexA];
            IInteractor b = Interactors[indexB];

            if (!a.HasCandidate && !b.HasCandidate)
            {
                return indexA < indexB ? -1 : 1;
            }

            if (a.HasCandidate && b.HasCandidate)
            {
                if (CandidateComparer == null)
                {
                    return indexA < indexB ? -1 : 1;
                }

                int result = CandidateComparer.Compare(a.CandidateProperties, b.CandidateProperties);
                return result < 0 ? -1 : 1;
            }

            return a.HasCandidate ? -1 : 1;
        }


        public virtual void Preprocess()
        {
            if (!UpdateActiveState())
            {
                Disable();
            }
            else
            {
                foreach (IInteractor interactor in Interactors)
                {
                    interactor.Preprocess();
                }
            }
            WhenPreprocessed();
        }

        public virtual void Process()
        {
            foreach (IInteractor interactor in Interactors)
            {
                interactor.Process();
            }
            WhenProcessed();
        }

        public virtual void Postprocess()
        {
            foreach (IInteractor interactor in Interactors)
            {
                interactor.Postprocess();
            }
            WhenPostprocessed();
        }

        public virtual void ProcessCandidate()
        {
            foreach (IInteractor interactor in Interactors)
            {
                if (interactor.State == InteractorState.Hover
                    || interactor.State == InteractorState.Normal)
                {
                    interactor.ProcessCandidate();
                }
            }
        }

        public virtual void Enable()
        {
            if (!UpdateActiveState())
            {
                return;
            }

            foreach (IInteractor interactor in Interactors)
            {
                interactor.Enable();
            }

            if (State == InteractorState.Disabled)
            {
                State = InteractorState.Normal;
            }
        }

        public virtual void Disable()
        {
            foreach (IInteractor interactor in Interactors)
            {
                interactor.Disable();
            }
            State = InteractorState.Disabled;
        }

        protected void DisableAllExcept(IInteractor mainInteractor)
        {
            foreach (IInteractor interactor in Interactors)
            {
                if (interactor != mainInteractor)
                {
                    interactor.Disable();
                }
            }
        }

        protected bool UpdateActiveState()
        {
            if (ActiveState == null || ActiveState.Active)
            {
                return true;
            }
            return false;
        }

        protected virtual void Update()
        {
            if (!IsRootDriver)
            {
                return;
            }

            Drive();
        }

        public virtual void Drive()
        {
            Preprocess();

            if (!UpdateActiveState())
            {
                Disable();
                Postprocess();
                return;
            }

            Enable();

            for (int i = 0; i < MaxIterationsPerFrame; i++)
            {
                if (State == InteractorState.Normal || State == InteractorState.Hover)
                {
                    ProcessCandidate();
                }

                Process();

                if (State == InteractorState.Disabled)
                {
                    break;
                }

                if (State == InteractorState.Normal)
                {
                    if (ShouldHover)
                    {
                        Hover();
                        continue;
                    }
                    break;
                }

                if (State == InteractorState.Hover)
                {
                    if (ShouldSelect)
                    {
                        Select();
                        continue;
                    }
                    if (ShouldUnhover)
                    {
                        Unhover();
                        continue;
                    }
                    break;
                }

                if (State == InteractorState.Select)
                {
                    if (ShouldUnselect)
                    {
                        Unselect();
                        continue;
                    }
                    break;
                }
            }

            Postprocess();
        }

        #region Inject
        public void InjectAllInteractorGroupBase(List<IInteractor> interactors)
        {
            InjectInteractors(interactors);
        }

        public void InjectInteractors(List<IInteractor> interactors)
        {
            Interactors = interactors;
        }

        public void InjectOptionalActiveState(IActiveState activeState)
        {
            ActiveState = activeState;
            _activeState = activeState as UnityEngine.Object;
        }

        public void InjectOptionalCandidateComparer(ICandidateComparer candidateComparer)
        {
            CandidateComparer = candidateComparer;
            _candidateComparer = candidateComparer as UnityEngine.Object;
        }
        #endregion

    }
}
