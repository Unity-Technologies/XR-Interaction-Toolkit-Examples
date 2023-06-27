/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Meta.Voice.Hub.Interfaces
{
    public interface IPageInfo
    {
        string Name { get; }
        string Context { get; }
        int Priority { get; }
        string Prefix { get; }
    }
}
