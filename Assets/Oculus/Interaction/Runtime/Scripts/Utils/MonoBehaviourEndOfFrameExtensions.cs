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
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Oculus.Interaction
{
    /// <summary>
    /// This extension allows easily registering an unregistering callback at the EndOfFrame
    /// which happens after rendering but before the next fixed Update.
    /// Call RegisterEndOfFrameCallback to start receiving updates and
    /// UnregisterEndOfFrameCallback to stop.
    /// </summary>
    internal static class MonoBehaviourEndOfFrameExtensions
    {
        private static YieldInstruction _endOfFrame = new WaitForEndOfFrame();
        private static Dictionary<MonoBehaviour, Coroutine> _routines = new Dictionary<MonoBehaviour, Coroutine>();

        internal static void RegisterEndOfFrameCallback(this MonoBehaviour monoBehaviour, Action callback)
        {
            if (_routines.ContainsKey(monoBehaviour))
            {
                throw new ArgumentException("This MonoBehaviour is already registered for the EndOfFrameCallback");
            }

            Coroutine routine = monoBehaviour.StartCoroutine(EndOfFrameCoroutine(callback));
            _routines.Add(monoBehaviour, routine);
        }

        internal static void UnregisterEndOfFrameCallback(this MonoBehaviour monoBehaviour)
        {
            if (!_routines.ContainsKey(monoBehaviour))
            {
                throw new ArgumentException("This MonoBehaviour is not registered for the EndOfFrameCallback");
            }
            monoBehaviour.StopCoroutine(_routines[monoBehaviour]);
            _routines.Remove(monoBehaviour);
        }

        private static IEnumerator EndOfFrameCoroutine(Action callback)
        {
            while (true)
            {
                yield return _endOfFrame;
                callback.Invoke();
            }
        }
    }
}
