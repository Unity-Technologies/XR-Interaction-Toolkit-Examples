// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core.Configuration.Modes;

namespace VRBuilder.Core
{
    /// <summary>
    /// A configurator that does nothing.
    /// </summary>
    public class EmptyConfigurator : IConfigurator
    {
        /// <inheritdoc />
        public void Configure(IMode mode, Stage stage)
        {
        }
    }
}
