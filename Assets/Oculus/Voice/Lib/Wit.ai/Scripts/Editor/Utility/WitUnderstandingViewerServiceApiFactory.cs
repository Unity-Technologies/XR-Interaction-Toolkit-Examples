/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Meta.WitAi.Windows
{
    public static class WitUnderstandingViewerServiceApiFactory
    {
        public delegate WitUnderstandingViewerServiceAPI Create(MonoBehaviour m);

        private static Dictionary<string, Create> factoryMethods = new Dictionary<string, Create>();

        public static void Register(string interfaceName, Create method)
        {
            factoryMethods.Add(interfaceName, method);
        }

        public static WitUnderstandingViewerServiceAPI CreateWrapper(MonoBehaviour service)
        {
            foreach (var interfaceType in service.GetType().GetInterfaces())
            {
                if (factoryMethods.ContainsKey(interfaceType.Name))
                {
                    return factoryMethods[interfaceType.Name](service);
                }
            }

            return null;
        }
    }
}
