// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Editor.UI.Graphics.Renderers;
using UnityEngine;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Represents "Add new step" button. It is rendered at the middle of its parent Transition and inserts a new step on click into the process workflow.
    /// </summary>
    internal class AddStepButton : GraphicalElement
    {
        private static readonly Vector2 size = new Vector2(20f, 20f);

        private readonly GraphicalElementRenderer renderer;

        /// <inheritdoc />
        public override GraphicalElementRenderer Renderer
        {
            get
            {
                return renderer;
            }
        }

        /// <inheritdoc />
        public override Rect BoundingBox
        {
            get
            {
                return new Rect(Position - size / 2f, size);
            }
        }

        /// <inheritdoc />
        public override int Layer
        {
            get
            {
                return 100;
            }
        }

        public AddStepButton(EditorGraphics editorGraphics, GraphicalElement parent = null) : base(editorGraphics, true, parent)
        {
            renderer = new AddStepButtonRenderer(this, editorGraphics.ColorPalette);
        }
    }
}
