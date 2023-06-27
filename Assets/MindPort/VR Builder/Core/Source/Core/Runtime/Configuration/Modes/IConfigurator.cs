// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Core.Configuration.Modes
{
    /// <summary>
    /// An interface for entities' configurators.
    /// </summary>
    public interface IConfigurator
    {
        /// <paramref name="mode">The current mode.</param>
        /// <paramref name="stage">The current entity's stage.</param>
        void Configure(IMode mode, Stage stage);
    }
}
