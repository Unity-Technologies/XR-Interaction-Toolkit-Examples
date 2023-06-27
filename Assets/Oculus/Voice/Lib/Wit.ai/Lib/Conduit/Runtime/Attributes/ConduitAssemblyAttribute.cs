/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Meta.Conduit
{
    /// <summary>
    /// Marks an assembly as Conduit-enabled to allow quick filtering.
    /// This can show anywhere in the assembly, but typically would go in AssemblyInfo.cs if one exists.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ConduitAssemblyAttribute : Attribute
    {
    }
}
