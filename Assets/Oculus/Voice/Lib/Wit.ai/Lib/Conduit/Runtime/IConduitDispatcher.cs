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
    /// The dispatcher is responsible for deciding which method to invoke when a request is received as well as parsing
    /// the parameters and passing them to the handling method.
    /// </summary>
    internal interface IConduitDispatcher
    {
        /// <summary>
        /// The Conduit manifest which captures the structure of the voice-enabled methods.
        /// </summary>
        Manifest Manifest { get; }
        
        /// <summary>
        /// Parses the manifest provided and registers its callbacks for dispatching.
        /// </summary>
        /// <param name="manifestFilePath">The path to the manifest file.</param>
        void Initialize(string manifestFilePath);

        /// <summary>
        /// Invokes the method matching the specified action ID.
        /// This should NOT be called before the dispatcher is initialized.
        /// </summary>
        /// <param name="parameterProvider">The parameter provider.</param>
        /// <param name="actionId">The action ID (which is also the intent name).</param>
        /// <param name="relaxed">When set to true, will allow matching parameters by type when the names mismatch.</param>
        /// <param name="confidence">The confidence level (between 0-1) of the intent that's invoking the action.</param>
        /// <param name="partial">Whether partial responses should be accepted or not</param>
        /// <returns>True if all invocations succeeded. False if at least one failed or no callbacks were found.</returns>
        bool InvokeAction(IParameterProvider parameterProvider, string actionId, bool relaxed, float confidence = 1f,
            bool partial = false);
        
        /// <summary>
        /// True if all the error handlers are called and received the action ID and exception.
        /// </summary>
        /// <param name="actionId">ID of action that failed to execute</param>
        /// <param name="exception">Exception containing the error message</param>
        /// <returns></returns>
        bool InvokeError( string actionId, Exception exception = null);
    }
}
