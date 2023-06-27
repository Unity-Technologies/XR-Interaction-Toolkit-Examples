// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core.Configuration.Modes;

namespace VRBuilder.Core
{
    /// <summary>
    /// A base class for entities' configurators which have access to their entities' data.
    /// </summary>
    public abstract class Configurator<TData> : IConfigurator where TData : IData
    {
        /// <summary>
        /// The data to configure.
        /// </summary>
        protected TData Data { get; }

        protected Configurator(TData data)
        {
            Data = data;
        }

        /// <inheritdoc />
        public abstract void Configure(IMode mode, Stage stage);
    }
}
