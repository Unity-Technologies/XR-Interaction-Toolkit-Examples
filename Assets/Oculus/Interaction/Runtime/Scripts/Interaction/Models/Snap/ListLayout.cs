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
    public class ListLayout
    {
        public class ListElement
        {
            public int id;
            public float pos;
            public float halfSize;
            public ListElement prev;
            public ListElement next;

            public ListElement(int id, float size)
            {
                this.id = id;
                this.halfSize = size / 2;
                this.pos = 0;
                this.prev = null;
                this.next = null;
            }
        }

        private ListElement _root;
        private Dictionary<int, ListElement> _elements;
        public Action<int> WhenElementAdded;
        public Action<int, bool> WhenElementUpdated;
        public Action<int> WhenElementRemoved;
        private bool _sizeUpdate = false;
        private int _moveElement = -1;

        private float _size = 0;
        public float Size => _size;

        public ListLayout()
        {
            _root = null;
            _elements = new Dictionary<int, ListElement>();
            WhenElementAdded = delegate { };
            WhenElementUpdated = delegate { };
            WhenElementRemoved = delegate { };
        }

        public void AddElement(int id, float size, float target = float.MaxValue)
        {
            if (_elements.ContainsKey(id))
            {
                return;
            }

            ListElement element = new ListElement(id, size);
            _size += size;

            _elements[id] = element;
            WhenElementAdded(id);

            if (_root == null)
            {
                _elements[id] = element;
                _root = element;
                UpdatePositionsFromRoot();
                return;
            }

            ListElement current = _root;
            while (current.next != null)
            {
                current = current.next;
            }

            current.next = element;
            element.prev = current;
            UpdatePositionsFromRoot();

            MoveElement(id, target);
            UpdatePos(element, element.pos, true);
        }

        public void RemoveElement(int id)
        {
            if (!_elements.TryGetValue(id, out ListElement element))
            {
                return;
            }

            if (element.prev != null)
            {
                element.prev.next = element.next;
            }

            if (element.next != null)
            {
                element.next.prev = element.prev;
            }

            if (_root == element)
            {
                if (element.next != null)
                {
                    _root = element.next;
                }
                else
                {
                    _root = null;
                }
            }

            _size -= element.halfSize * 2;
            UpdatePositionsFromRoot();

            _elements.Remove(id);
            WhenElementRemoved(id);
        }

        private void UpdatePos(ListElement element, float pos, bool force = false)
        {
            if (pos != element.pos || force)
            {
                element.pos = pos;
                WhenElementUpdated(element.id, _sizeUpdate || _moveElement == element.id || force);
            }
        }

        private void UpdatePositionsFromRoot()
        {
            if (_root == null)
            {
                return;
            }

            UpdatePos(_root, _root.halfSize - _size / 2);
            UpdatePositionsRight(_root);
        }

        private void UpdatePositionsRight(ListElement current)
        {
            while (current.next != null)
            {
                UpdatePos(current.next, current.pos + current.halfSize + current.next.halfSize);
                current = current.next;
            }
        }

        private void SwapWithNext(ListElement element)
        {
            if (element.prev != null)
            {
                element.prev.next = element.next;
            }

            if (element.next.next != null)
            {
                element.next.next.prev = element;
            }

            element.next.prev = element.prev;
            element.prev = element.next;
            element.next = element.prev.next;
            element.prev.next = element;

            if (element == _root || element.prev == _root)
            {
                _root = element == _root ? element.prev : element;
                UpdatePositionsFromRoot();
            }
            else
            {
                UpdatePos(element.prev, element.prev.prev.pos +
                                        element.prev.prev.halfSize +
                                        element.prev.halfSize);
                UpdatePos(element,
                    element.prev.pos + element.prev.halfSize + element.halfSize);
            }
        }

        private void SwapWithPrev(ListElement element)
        {
            SwapWithNext(element.prev);
        }

        public void MoveElement(int id, float target)
        {
            _moveElement = id;
            if (!_elements.TryGetValue(id, out ListElement element))
            {
                _moveElement = -1;
                return;
            }

            if (target > element.pos)
            {
                while (element.next != null)
                {
                    float midPos = element.pos + (element.halfSize + element.next.halfSize) / 2;
                    if (target < midPos)
                    {
                        break;
                    }
                    SwapWithNext(element);
                }
            }
            else
            {
                while (element.prev != null)
                {
                    float midPos = element.pos - (element.halfSize + element.prev.halfSize) / 2;
                    if (target > midPos)
                    {
                        break;
                    }
                    SwapWithPrev(element);
                }
            }

            _moveElement = -1;
        }

        public void UpdateElementSize(int id, float size)
        {
            if (!_elements.TryGetValue(id, out ListElement element))
            {
                return;
            }

            _sizeUpdate = true;

            float deltaSize = size - element.halfSize * 2;
            _size += deltaSize;

            element.halfSize = size / 2;
            UpdatePositionsFromRoot();
            _sizeUpdate = false;
        }

        public float GetElementPosition(int id)
        {
            if (!_elements.TryGetValue(id, out ListElement element))
            {
                return 0;
            }

            return element.pos;
        }

        public float GetElementSize(int id)
        {
            if (!_elements.TryGetValue(id, out ListElement element))
            {
                return 0;
            }

            return element.halfSize * 2;
        }

        public float GetTargetPosition(int id, float target, float size)
        {
            if (_elements.ContainsKey(id))
            {
                return GetElementPosition(id);
            }

            if (_root == null)
            {
                return 0;
            }

            float tmpSize = _size + size;
            float start = -tmpSize / 2 + size / 2;
            float end = start;

            ListElement current = _root;
            while (current != null)
            {
                float nextJump = size / 2 + current.halfSize;
                float midPos = end + nextJump / 2;
                if (target < midPos)
                {
                    break;
                }

                end += nextJump;
                current = current.next;
            }

            return end;
        }
    }

    public class ListLayoutEase
    {
        private ListLayout _listLayout;
        private Dictionary<int, ListElementEase> _elementDict;
        private AnimationCurve _curve;
        private float _curveTime;
        private float _time = 0;

        private class ListElementEase
        {
            private AnimationCurve _curve;
            private float _curveTime = 0;
            private float _startTime = 0;
            private float _start = 0;
            private float _target = 0;
            public float position = 0;

            public ListElementEase(AnimationCurve curve, float easeTime, float position)
            {
                _curve = curve;
                _curveTime = easeTime;
                _start = _target = this.position = position;
            }

            public void SetTarget(float target, float time, bool skipEase)
            {
                _target = target;
                if (!skipEase)
                {
                    _start = this.position;
                    _startTime = time;
                }
                else
                {
                    _start = target;
                    this.position = target;
                }
            }

            public void UpdateTime(float time)
            {
                float t = time - this._startTime;
                float normalizedT = Mathf.Clamp01(t / _curveTime);
                float curveValue = _curve.Evaluate(normalizedT);
                position = (_target - _start) * curveValue + _start;
            }
        }

        public ListLayoutEase(ListLayout layout, float curveTime = 0.3f, AnimationCurve curve = null)
        {
            _curve = curve;
            _curveTime = curveTime;
            if (_curve == null)
            {
                _curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }

            _elementDict = new Dictionary<int, ListElementEase>();
            _listLayout = layout;
            _listLayout.WhenElementAdded += HandleElementAdded;
            _listLayout.WhenElementUpdated += HandleElementUpdated;
            _listLayout.WhenElementRemoved += HandleElementRemoved;
        }

        private void HandleElementAdded(int id)
        {
            float position = _listLayout.GetElementPosition(id);
            _elementDict.Add(id, new ListElementEase(_curve, _curveTime, position));
        }

        private void HandleElementUpdated(int id, bool sizeUpdate)
        {
            ListElementEase element = _elementDict[id];
            element.SetTarget(_listLayout.GetElementPosition(id), _time, sizeUpdate);
        }

        private void HandleElementRemoved(int id)
        {
            _elementDict.Remove(id);
        }

        public void UpdateTime(float time)
        {
            _time = time;
            foreach (ListElementEase element in _elementDict.Values)
            {
                element.UpdateTime(_time);
            }
        }

        public float GetPosition(int id)
        {
            return _elementDict[id].position;
        }
    }
}
