// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Editor.UndoRedo
{
    /// <summary>
    /// An interface for a method object,
    /// </summary>
    public interface IRevertableCommand
    {
        /// <summary>
        /// Perform some revertable action.
        /// </summary>
        void Do();

        /// <summary>
        /// Revert the changes done during by <see cref="Do"/> method.
        /// </summary>
        void Undo();
    }
}
