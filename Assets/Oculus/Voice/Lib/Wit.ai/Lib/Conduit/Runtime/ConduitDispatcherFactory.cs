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
    /// Creates and caches conduit dispatchers.
    /// </summary>
    internal class ConduitDispatcherFactory
    {
        /// <summary>
        /// Dispatcher instance
        /// </summary>
        private static IConduitDispatcher Instance;

        /// <summary>
        /// The instance resolver used to find instance objects at runtime.
        /// </summary>
        private readonly IInstanceResolver _instanceResolver;

        /// <summary>
        /// The parameter provider used to resolve parameters during dispatching.
        /// </summary>
        private readonly IParameterProvider _parameterProvider;

        public ConduitDispatcherFactory(IInstanceResolver instanceResolver)
        {
            this._instanceResolver = instanceResolver;
        }
        
        /// <summary>
        /// Returns a Conduit dispatcher instance. The same instance will be reused past the first request.  
        /// </summary>
        /// <returns>A Conduit dispatcher instance</returns>
        public IConduitDispatcher GetDispatcher()
        {
            return Instance = Instance ??
                              new ConduitDispatcher(new ManifestLoader(), this._instanceResolver);
        }
    }
}
