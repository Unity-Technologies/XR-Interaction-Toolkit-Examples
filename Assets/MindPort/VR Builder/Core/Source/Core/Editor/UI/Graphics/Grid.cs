// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Editor.UI.Graphics.Renderers;
using UnityEngine;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Base class for a grid which can be used as e.g. a background of the chapter view.
    /// </summary>
    internal abstract class Grid : GraphicalElement
    {
        public Grid(EditorGraphics editorGraphics, bool isReceivingEvents, GraphicalElement parent = null) : base(editorGraphics, isReceivingEvents, parent)
        {
        }

        /// <inheritdoc />
        public override GraphicalElementRenderer Renderer { get; }

        /// <inheritdoc />
        public override Rect BoundingBox { get; }

        /// <inheritdoc />
        public override int Layer { get; }
    }
}
