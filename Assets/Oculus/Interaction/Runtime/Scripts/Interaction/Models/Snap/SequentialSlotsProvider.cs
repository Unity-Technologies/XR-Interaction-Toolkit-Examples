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
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// This ISnapSlotsProvider uses a ordered list of individual Slots and will
    /// push the elements back or forth to make room for the new element.
    /// </summary>
    public class SequentialSlotsProvider : MonoBehaviour, ISnapPoseDelegate
    {
        [SerializeField]
        private List<Transform> _slots;

        private int[] _slotInteractors;

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertCollectionField(_slots, nameof(_slots));
            _slotInteractors = new int[_slots.Count];

            this.EndStart(ref _started);
        }

        public void TrackElement(int id, Pose pose)
        {
            int desiredIndex = FindBestSlotIndex(pose.position);
            if (TryOccupySlot(desiredIndex))
            {
                _slotInteractors[desiredIndex] = id;
            }
        }

        public void UntrackElement(int id)
        {
            if (TryFindIndexForInteractor(id, out int index))
            {
                _slotInteractors[index] = 0;
            }
        }

        public void SnapElement(int id, Pose pose)
        {
        }

        public void UnsnapElement(int id)
        {
        }

        public void MoveTrackedElement(int id, Pose pose)
        {
            int desiredIndex = FindBestSlotIndex(pose.position);
            if (TryFindIndexForInteractor(id, out int index))
            {
                if (desiredIndex != index)
                {
                    _slotInteractors[index] = 0;
                    if (TryOccupySlot(desiredIndex))
                    {
                        _slotInteractors[desiredIndex] = id;
                    }
                }
            }
            else if (TryOccupySlot(desiredIndex))
            {
                _slotInteractors[desiredIndex] = id;
            }
        }

        private bool TryFindIndexForInteractor(int id, out int index)
        {
            //FindIndex is not ideal, but this single line simplifies this sample SlotsProvider a lot.
            index = Array.FindIndex(_slotInteractors, i => i == id);
            return index >= 0;
        }

        public bool SnapPoseForElement(int id, Pose pose, out Pose result)
        {
            if (TryFindIndexForInteractor(id, out int index))
            {
                result = _slots[index].GetPose();
                return true;
            }
            result = Pose.identity;
            return false;
        }

        private bool TryOccupySlot(int index)
        {
            if (IsSlotFree(index))
            {
                return true;
            }

            int freeSlot = FindBestSlotIndex(_slots[index].position, true);
            if (freeSlot < 0)
            {
                return false;
            }

            PushSlots(index, freeSlot);
            return true;
        }

        private bool IsSlotFree(int index)
        {
            return _slotInteractors[index] == 0;
        }

        private int FindBestSlotIndex(in Vector3 target, bool freeOnly = false)
        {
            int bestIndex = -1;
            float minDistance = float.PositiveInfinity;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (freeOnly && !IsSlotFree(i))
                {
                    continue;
                }

                float distance = (target - _slots[i].position).sqrMagnitude;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestIndex = i;
                }

            }
            return bestIndex;
        }

        private void PushSlots(int index, int freeSlot)
        {
            bool forwardDirection = index > freeSlot;
            for (int i = freeSlot; i != index; i = Next(i))
            {
                int nextIndex = Next(i);
                SwapSlot(i, nextIndex);
            }

            int Next(int value)
            {
                return value + (forwardDirection ? 1 : -1);
            }
        }

        private void SwapSlot(int index, int freeSlot)
        {
            (_slotInteractors[index], _slotInteractors[freeSlot]) = (_slotInteractors[freeSlot], _slotInteractors[index]);
        }

        #region Inject
        public void InjectAllSequentialSlotsProvider(List<Transform> slots)
        {
            InjectSlots(slots);
        }

        public void InjectSlots(List<Transform> slots)
        {
            _slots = slots;
        }
        #endregion
    }
}
