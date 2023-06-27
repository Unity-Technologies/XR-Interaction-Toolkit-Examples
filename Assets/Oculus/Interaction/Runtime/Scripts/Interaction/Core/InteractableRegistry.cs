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

using System.Collections;
using System.Collections.Generic;

namespace Oculus.Interaction
{
    /// <summary>
    /// A registry that houses a set of concrete Interactables.
    /// </summary>
    public class InteractableRegistry<TInteractor, TInteractable>
                                     where TInteractor : Interactor<TInteractor, TInteractable>
                                     where TInteractable : Interactable<TInteractor, TInteractable>
    {
        /// <summary>
        /// Allocation-free collection that can be iterated over
        /// to provide a pruned list of Interactables.
        /// </summary>
        public struct InteractableSet : IEnumerable<TInteractable>
        {
            private readonly IReadOnlyList<TInteractable> _data;
            private readonly ISet<TInteractable> _onlyInclude;
            private readonly TInteractor _testAgainst;

            /// <summary>
            /// A pruned set of interactables from the
            /// <see cref="InteractableRegistry{TInteractor, TInteractable}"/>
            /// </summary>
            /// <param name="onlyInclude">Include only these interactables from
            /// the base <see cref="_interactables"/> collection.
            /// Provide a null value to skip this filtering.</param>
            /// <param name="testAgainst">Filter against an interactor.
            /// Provide a null value to skip this filtering.</param>
            public InteractableSet(
                ISet<TInteractable> onlyInclude,
                TInteractor testAgainst)
            {
                _data = _interactables;
                _onlyInclude = onlyInclude;
                _testAgainst = testAgainst;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

            IEnumerator<TInteractable> IEnumerable<TInteractable>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private bool Include(TInteractable interactable)
            {
                // Skip interactables not contained in a provided subset
                if (_onlyInclude != null)
                {
                    if (!_onlyInclude.Contains(interactable))
                    {
                        return false;
                    }
                }

                // Skip interactables that fail test against interactable
                if (_testAgainst != null)
                {
                    if (!_testAgainst.CanSelect(interactable))
                    {
                        return false;
                    }
                    if (!interactable.CanBeSelectedBy(_testAgainst))
                    {
                        return false;
                    }
                }

                return true;
            }

            public struct Enumerator : IEnumerator<TInteractable>
            {
                private readonly InteractableSet _set;
                private int _position;

                private IReadOnlyList<TInteractable> Data => _set._data;

                public Enumerator(in InteractableSet set)
                {
                    _set = set;
                    _position = -1;
                }

                public TInteractable Current
                {
                    get
                    {
                        if (Data == null || _position < 0)
                        {
                            throw new System.InvalidOperationException();
                        }
                        return Data[_position];
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (Data == null)
                    {
                        return false;
                    }

                    do
                    {
                        _position++;
                    } while (_position < Data.Count &&
                        !_set.Include(Data[_position]));

                    return _position < Data.Count;
                }

                public void Reset()
                {
                    _position = -1;
                }

                public void Dispose()
                {
                }
            }
        }

        private static List<TInteractable> _interactables;

        public InteractableRegistry()
        {
            _interactables = new List<TInteractable>();
        }

        public virtual void Register(TInteractable interactable)
        {
            _interactables.Add(interactable);
        }

        public virtual void Unregister(TInteractable interactable)
        {
            _interactables.Remove(interactable);
        }

        /// <summary>
        /// Returns a filtered collection of interactables
        /// </summary>
        /// <param name="interactor">Only interactables that can be selected by
        /// this interactor will be returned</param>
        /// <param name="onlyInclude">Only interactables included in this
        /// subset will be included in the returned collection</param>
        protected InteractableSet List(TInteractor interactor,
                                       HashSet<TInteractable> onlyInclude)
        {
            return new InteractableSet(onlyInclude, interactor);
        }

        /// <summary>
        /// Returns a filtered collection of interactables
        /// </summary>
        /// <param name="interactor">Only interactables that can be selected by
        /// this interactor will be returned</param>
        public virtual InteractableSet List(TInteractor interactor)
        {
            return new InteractableSet(null, interactor);
        }

        /// <summary>
        /// Returns all interactables in this registry
        /// </summary>
        public virtual InteractableSet List()
        {
            return new InteractableSet(null, null);
        }
    }
}
