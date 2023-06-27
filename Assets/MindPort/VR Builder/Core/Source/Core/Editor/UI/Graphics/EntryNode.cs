// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Editor.UI.Graphics.Renderers;
using UnityEngine;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Represents the beginning of a process in the process workflow.
    /// </summary>
    internal class EntryNode : EditorNode
    {
        private static readonly Vector2 size = new Vector2(24f, 24f);
        private readonly GraphicalElementRenderer renderer;

        private bool isDragging;

        public bool IsDragging
        {
            get
            {
                return isDragging;
            }
            set
            {
                if (value != isDragging)
                {
                    isDragging = value;
                }
            }
        }

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

        public EntryNode(EditorGraphics owner) : base(owner, true)
        {
            renderer = new EntryNodeRenderer(this, owner.ColorPalette);
        }
    }
}
