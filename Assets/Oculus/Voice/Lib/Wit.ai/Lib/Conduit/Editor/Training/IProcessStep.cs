/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Meta.Conduit
{
    /// <summary>
    /// A step in processing or query.
    /// </summary>
    /// <param name="success">True if the step succeeded. False otherwise (with error in data field)</param>
    /// <param name="data">The optional data returned in success or the error data on failure.</param>
    internal delegate void StepResult(bool success, string data);
}
