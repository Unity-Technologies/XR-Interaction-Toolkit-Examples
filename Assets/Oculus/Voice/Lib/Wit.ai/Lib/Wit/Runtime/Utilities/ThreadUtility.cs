/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

#if !UNITY_WEBGL
#define THREADING_ENABLED
#endif

using System;
using UnityEngine;
using System.Collections;
#if THREADING_ENABLED
using System.Threading;
#endif

namespace Meta.WitAi
{
    public static class ThreadUtility
    {
        // Default timeout to off
        public const float THREAD_DEFAULT_TIMEOUT = -1f;

        // Perform in background & return on complete
        public static ThreadPerformer<T> PerformInBackground<T>(Func<T> workerAction, Action<T, string> onComplete, float timeout = THREAD_DEFAULT_TIMEOUT)
        {
            return new ThreadPerformer<T>(workerAction, onComplete, timeout);
        }

        // Performer
        public class ThreadPerformer<T>
        {
            /// <summary>
            /// Whether thread is running
            /// </summary>
            public bool IsRunning { get; private set; }

            // Complete callback items
            #if THREADING_ENABLED
            private Thread _thread;
            #endif
            private Func<T> _worker;
            private Action<T, string> _complete;
            private float _timeout;
            private T _result;
            private string _error;
            private CoroutineUtility.CoroutinePerformer _coroutine;

            /// <summary>
            /// Generate thread
            /// </summary>
            public ThreadPerformer(Func<T> worker, Action<T, string> onComplete, float timeout)
            {
                // Begin
                IsRunning = true;

                // Wait for thread completion
                _result = default(T);
                _error = string.Empty;
                _worker = worker;
                _complete = onComplete;
                _timeout = timeout;
                _coroutine = CoroutineUtility.StartCoroutine(WaitForCompletion(), true);

                // Start thread
                #if THREADING_ENABLED
                _thread = new Thread(Work);
                _thread.Start();
                #endif
            }

            // Work
            private void Work()
            {
                // Perform action
                try
                {
                    _result = _worker.Invoke();
                }
                // Catch exceptions
                catch (Exception e)
                {
                    _error = $"Background thread error thrown\n{e}";
                }

                // Complete
                IsRunning = false;
            }

            // Wait for completion
            private IEnumerator WaitForCompletion()
            {
                #if !THREADING_ENABLED
                yield return null;
                Work();
                #endif

                // Wait while running
                DateTime start = DateTime.Now;
                while (IsRunning && !IsTimedOut(start))
                {
                    yield return null;
                }

                // Timed out
                if (IsTimedOut(start))
                {
                    _error = "Timed out";
                }

                // Complete
                _complete?.Invoke(_result, _error);

                // Quit
                Quit();
            }
            // Check if timed out
            private bool IsTimedOut(DateTime start)
            {
                // Ignore if no timeout
                if (_timeout <= 0)
                {
                    return false;
                }
                // Timed out
                return (DateTime.Now - start).TotalSeconds >= _timeout;
            }

            // Quit running thread
            public void Quit()
            {
                if (_coroutine != null)
                {
                    GameObject.DestroyImmediate(_coroutine);
                    _coroutine = null;
                }
                #if THREADING_ENABLED
                if (_thread != null)
                {
                    if (IsRunning)
                    {
                        _thread.Join();
                    }
                    _thread = null;
                }
                #endif
                if (IsRunning)
                {
                    IsRunning = false;
                }
            }
        }
    }
}

