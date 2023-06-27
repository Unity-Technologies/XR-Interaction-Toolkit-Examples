// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections;

namespace VRBuilder.Core
{
    /// <summary>
    /// A process for an <see cref="IEntity"/>'s <see cref="Stage"/>.
    /// </summary>
    public interface IStageProcess
    {
        /// <summary>
        /// This method is invoked immediately when entity enters the stage.
        /// The invocation is guaranteed.
        /// Use it for initialization.
        /// </summary>
        void Start();

        /// <summary>
        /// This method will be iterated over while the entity is in this stage, one iteration step per frame, starting from the second frame.
        /// </summary>
        /// <returns></returns>
        IEnumerator Update();

        /// <summary>
        /// This method is invoked immediately after the <see cref="Update"/> was iterated over completely, or after the <see cref="FastForward"/> was called.
        /// The invocation is guaranteed.
        /// Use it for deinitialization.
        /// </summary>
        void End();

        /// <summary>
        /// This method is called when the process was not completed yet.
        /// It must "fake" normal execution of the process and handle the cases when the <see cref="Update"/> is not iterated over completely.
        /// </summary>
        void FastForward();
    }
}
