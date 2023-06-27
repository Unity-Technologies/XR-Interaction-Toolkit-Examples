// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;

namespace VRBuilder.Core.Utils
{
    internal static class MathUtils
    {
        public static bool FindPathInGraph<T>(this T start, Func<T, IEnumerable<T>> getOutcomingConnections, T target, out IList<T> result)
        {
            result = null;

            List<T> passedNodes = new List<T>();

            return FindPathInGraphRecursive(start, getOutcomingConnections, target, ref passedNodes, out result);
        }

        private static bool FindPathInGraphRecursive<T>(T current, Func<T, IEnumerable<T>> getOutcomingConnections, T target, ref List<T> passedNodes, out IList<T> result)
        {
            if (passedNodes.Contains(current))
            {
                result = null;
                return false;
            }

            passedNodes.Add(current);

            if (Equals(current, target))
            {
                result = new List<T>();
                return true;
            }

            foreach (T next in getOutcomingConnections(current))
            {
                IList<T> iterationResult;
                if (FindPathInGraphRecursive(next, getOutcomingConnections, target, ref passedNodes, out iterationResult))
                {
                    iterationResult.Insert(0, next);
                    result = iterationResult;
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
