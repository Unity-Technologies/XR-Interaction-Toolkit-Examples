/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;

namespace Meta.Conduit
{
    internal interface IInstanceResolver
    {
        IEnumerable<object> GetObjectsOfType(Type type);
    }
}
