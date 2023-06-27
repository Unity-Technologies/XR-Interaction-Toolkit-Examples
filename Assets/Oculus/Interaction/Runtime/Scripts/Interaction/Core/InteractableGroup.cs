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
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// This class implements higher level logic to share the number of max
    /// max interactors acting on this group of interactors at a time.
    /// </summary>
    public class InteractableGroup : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IInteractable))]
        private List<UnityEngine.Object> _interactables;
        private List<IInteractable> Interactables;
        private List<InteractableLimits> _limits;

        private struct InteractableLimits
        {
            public int MaxInteractors;
            public int MaxSelectingInteractors;
        }

        [SerializeField]
        private int _maxInteractors;

        [SerializeField]
        private int _maxSelectingInteractors;

        private int _interactors;
        private int _selectInteractors;

        [SerializeField, Optional]
        private UnityEngine.Object _data = null;
        public object Data { get; protected set; } = null;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Interactables = _interactables.ConvertAll(mono => mono as IInteractable);
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertCollectionItems(Interactables, nameof(Interactables));

            _limits = new List<InteractableLimits>();
            foreach (IInteractable interactable in Interactables)
            {
                _limits.Add(new InteractableLimits()
                {
                    MaxInteractors = interactable.MaxInteractors,
                    MaxSelectingInteractors = interactable.MaxSelectingInteractors
                });
            }

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
                    interactable.WhenInteractorViewAdded += HandleInteractorViewAdded;
                    interactable.WhenInteractorViewRemoved += HandleInteractorViewRemoved;
                    interactable.WhenSelectingInteractorViewAdded += HandleSelectingInteractorViewAdded;
                    interactable.WhenSelectingInteractorViewRemoved += HandleSelectingInteractorViewRemoved;
                }

                UpdateInteractorCount();
                UpdateSelectingInteractorCount();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                foreach (IInteractable interactable in Interactables)
                {
                    interactable.WhenInteractorViewAdded -= HandleInteractorViewAdded;
                    interactable.WhenInteractorViewRemoved -= HandleInteractorViewRemoved;
                    interactable.WhenSelectingInteractorViewAdded -= HandleSelectingInteractorViewAdded;
                    interactable.WhenSelectingInteractorViewRemoved -= HandleSelectingInteractorViewRemoved;
                }

                UpdateInteractorCount();
                UpdateSelectingInteractorCount();
            }
        }

        private void UpdateInteractorCount()
        {
            _interactors = 0;
            foreach (IInteractable interactable in Interactables)
            {
                _interactors += interactable.InteractorViews.Count();
            }

            UpdateMaxInteractors();

        }

        private void UpdateSelectingInteractorCount()
        {
            _selectInteractors = 0;
            foreach (IInteractable interactable in Interactables)
            {
                _selectInteractors += interactable.SelectingInteractorViews.Count();
            }

            UpdateMaxSelecting();
        }

        private void HandleInteractorViewAdded(IInteractorView interactorView) =>
            UpdateInteractorCount();
        private void HandleInteractorViewRemoved(IInteractorView interactorView) =>
            UpdateInteractorCount();
        private void HandleSelectingInteractorViewAdded(IInteractorView interactorView) =>
            UpdateInteractorCount();
        private void HandleSelectingInteractorViewRemoved(IInteractorView interactorView) =>
            UpdateInteractorCount();

        private void UpdateMaxInteractors()
        {
            if (_maxInteractors == -1) return;
            int remainingInteractors = Mathf.Max(0, _maxInteractors - _interactors);
            for (int i = 0; i < Interactables.Count; i++)
            {
                Interactables[i].MaxInteractors = (_limits[i].MaxInteractors == -1
                    ? remainingInteractors
                    : Mathf.Max(0, _limits[i].MaxInteractors - _interactors)) +
                                                  Interactables[i].InteractorViews.Count();
            }
        }

        private void UpdateMaxSelecting()
        {
            if (_maxSelectingInteractors == -1) return;
            int remainingActive = Mathf.Max(0, _maxSelectingInteractors - _selectInteractors);
            for (int i = 0; i < Interactables.Count; i++)
            {
                Interactables[i].MaxSelectingInteractors = (_limits[i].MaxSelectingInteractors == -1
                    ? remainingActive
                    : Mathf.Max(0, _limits[i].MaxSelectingInteractors - _selectInteractors)) +
                                                           Interactables[i].SelectingInteractorViews.Count();
            }
        }

        #region Inject

        public void InjectAllInteractableGroup(List<IInteractable> interactables)
        {
            InjectInteractables(interactables);
        }

        public void InjectInteractables(List<IInteractable> interactables)
        {
            Interactables = interactables;
            _interactables =
                interactables.ConvertAll(interactable => interactable as UnityEngine.Object);
        }

        public void InjectOptionalData(object data)
        {
            _data = data as UnityEngine.Object;
            Data = data;
        }


        #endregion
    }
}
