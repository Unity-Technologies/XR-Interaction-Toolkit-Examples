// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEngine;

namespace VRBuilder.Editor.UI
{
    /// <summary>
    /// Allows to draws over the normal EditorGraphics.
    /// </summary>
    internal interface IEditorGraphicDrawer
    {
        /// <summary>
        /// Draw priority, lower numbers will be drawn first.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Your draw call.
        /// </summary>
        void Draw(Rect windowRect);
    }
}
