// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core
{
    /// <summary>
    /// A life cycle of an entity. It handles transition between four stages: Inactive, Activating, Active, and Deactivating.
    /// </summary>
    public interface ILifeCycle
    {
        /// <summary>
        /// An event which is fired when the current stage changes.
        /// </summary>
        event EventHandler<ActivationStateChangedEventArgs> StageChanged;

        /// <summary>
        /// The current stage.
        /// </summary>
        Stage Stage { get; }

        /// <summary>
        /// Enters Activating stage if was deactivating.
        /// </summary>
        void Activate();

        /// <summary>
        /// Enters Deactivating stage if was Active. If was Activating, will start deactivating as soon as it enters Active stage.
        /// </summary>
        void Deactivate();

        /// <summary>
        /// Marks all stages to fast-forward until it reaches Inactive stage.
        /// </summary>
        void MarkToFastForward();

        /// <summary>
        /// Marks the given <paramref name="stage"/> to fast-forward.
        /// </summary>
        void MarkToFastForwardStage(Stage stage);

        /// <summary>
        /// This method should be called every frame.
        /// </summary>
        void Update();
    }
}
