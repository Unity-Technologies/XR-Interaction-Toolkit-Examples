/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.IO;

namespace Meta.Conduit
{
    internal interface IFileIo
    {
        bool Exists(string fileName);
        string ReadAllText(string fileName);
        void WriteAllText(string path, string contents);
        TextReader OpenText(string fileName);
    }
}
