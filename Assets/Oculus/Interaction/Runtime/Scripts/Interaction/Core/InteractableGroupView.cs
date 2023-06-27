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
using System.Linq;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// This class implements higher level logic to forward the highest IInteractable
    /// state of any of the interactables in its list
    /// </summary>
    public class InteractableGroupView : MonoBehaviour, IInteractableView
    {
        [SerializeField, Interface(typeof(IInteractable))]
        private List<UnityEngine.Object> _interactables;

        private List<IInteractable> Interactables;

        [SerializeField, Optional]
        private UnityEngine.Object _data = null;
        public object Data { get; protected set; } = null;

        public int InteractorsCount
        {
            get
            {
                int count = 0;
                foreach (IInteractable interactable in Interactables)
                {
                    count += interactable.InteractorViews.Count();
                }

                return count;
            }
        }

        public int SelectingInteractorsCount
        {
            get
            {
                int count = 0;
                foreach (IInteractable interactable in Interactables)
                {
                    count += interactable.SelectingInteractorViews.Count();
                }

                return count;
            }
        }

        public IEnumerable<IInteractorView> InteractorViews =>
            Interactables.SelectMany(interactable => interactable.InteractorViews).ToList();

        public IEnumerable<IInteractorView> SelectingInteractorViews =>
            Interactables.SelectMany(interactable => interactable.SelectingInteractorViews).ToList();

        public event Action<IInteractorView> WhenInteractorViewAdded = delegate { };
        public event Action<IInteractorView> WhenInteractorViewRemoved = delegate { };
        public event Action<IInteractorView> WhenSelectingInteractorViewAdded = delegate { };
        public event Action<IInteractorView> WhenSelectingInteractorViewRemoved = delegate { };

        public int MaxInteractors
        {
            get
            {
                int max = 0;
                foreach (IInteractable interactable in Interactables)
                {
                    max = Mathf.Max(interactable.MaxInteractors, max);
                }

                return max;
            }
        }

        public int MaxSelectingInteractors
        {
            get
            {
                int max = 0;
                foreach (IInteractable interactable in Interactables)
                {
                    max = Mathf.Max(interactable.MaxSelectingInteractors, max);
                }

                return max;
            }
        }

        public event Action<InteractableStateChangeArgs> WhenStateChanged = delegate { };

        private InteractableState _state = InteractableState.Normal;
        public InteractableState State
        {
            get
            {
                return _state;
            }
            set
            {
                if (_state == value) return;
                InteractableState previousState = _state;
                _state = value;
                WhenStateChanged(new InteractableStateChangeArgs(
                    previousState,_state
                ));
            }
        }

        private void UpdateState()
        {
            if (SelectingInteractorsCount > 0)
            {
                State = InteractableState.Select;
                return;
            }
            if (InteractorsCount > 0)
            {
                State = InteractableState.Hover;
                return;
            }
            State = InteractableState.Normal;
        }

        protected virtual void Awake()
        {
            if (_interactables != null)
            {
                Interactables = _interactables.ConvertAll(mono => mono as IInteractable);
            }
        }

        protected bool _started = false;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertCollectionItems(Interactables, nameof(Interactables));

            if (Data == null)
            {
                _data = this;
                Data = _data;
            }

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                foreach (IInteractable interactable in Interactables)
                {
                    interactable.WhenStateChanged += HandleStateChange;
                    interactable.WhenInteractorViewAdded += HandleInteractorViewAdded;
                    interactable.WhenInteractorViewRemoved += HandleInteractorViewRemoved;
                    interactable.WhenSelectingInteractorViewAdded += HandleSelectingInteractorViewAdded;
                    interactable.WhenSelectingInteractorViewRemoved += HandleSelectingInteractorViewRemoved;
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                foreach (IInteractable interactable in Interactables)
                {
                    interactable.WhenStateChanged -= HandleStateChange;
                    interactable.WhenInteractorViewAdded -= HandleInteractorViewAdded;
                    interactable.WhenInteractorViewRemoved -= HandleInteractorViewRemoved;
                    interactable.WhenSelectingInteractorViewAdded -= HandleSelectingInteractorViewAdded;
                    interactable.WhenSelectingInteractorViewRemoved -= HandleSelectingInteractorViewRemoved;
                }
            }
        }

        private void HandleStateChange(InteractableStateChangeArgs args)
        {
            UpdateState();
        }

        private void HandleInteractorViewAdded(IInteractorView obj)
        {
            WhenInteractorViewAdded.Invoke(obj);
        }

        private void HandleInteractorViewRemoved(IInteractorView obj)
        {
            WhenInteractorViewRemoved.Invoke(obj);
        }

        private void HandleSelectingInteractorViewAdded(IInteractorView obj)
        {
            WhenSelectingInteractorViewAdded.Invoke(obj);
        }

        private void HandleSelectingInteractorViewRemoved(IInteractorView obj)
        {
            WhenSelectingInteractorViewRemoved.Invoke(obj);
        }

        #region Inject

        public void InjectAllInteractableGroupView(List<IInteractable> interactables)
        {
            InjectInteractables(interactables);
        }

        public void InjectInteractables(List<IInteractable> interactables)
        {
            Interactables = interactables;
            _interactables =
                Interactables.ConvertAll(interactable => interactable as UnityEngine.Object);
        }

        public void InjectOptionalData(object data)
        {
            _data = data as UnityEngine.Object;
            Data = data;
        }


        #endregion
    }
}
