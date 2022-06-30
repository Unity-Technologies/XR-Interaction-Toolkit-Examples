/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Concurrent;
using UnityEditor;

namespace Facebook.WitAi.Utilities
{
    public class EditorForegroundRunner
    {
        private static ConcurrentQueue<Action> foregroundQueue = new ConcurrentQueue<Action>();

        public static void Run(Action action)
        {
            if (null == action) return;

            foregroundQueue.Enqueue(action);
            EditorApplication.update += FlushQueue;
        }

        private static void FlushQueue()
        {
            EditorApplication.update -= FlushQueue;
            while (foregroundQueue.Count > 0)
            {
                if (foregroundQueue.TryDequeue(out var action))
                {
                    action.Invoke();
                }
            }
        }
    }
}
