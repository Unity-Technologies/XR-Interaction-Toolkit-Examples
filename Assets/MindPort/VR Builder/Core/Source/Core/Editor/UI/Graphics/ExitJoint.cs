// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Editor.UI.Graphics.Renderers;
using UnityEngine;

namespace VRBuilder.Editor.UI.Graphics
{
    internal class ExitJoint : GraphicalElement
    {
        public Vector2 DragDelta { get; set; }

        private readonly Vector2 size = new Vector2(16f, 16f);

        private readonly GraphicalElementRenderer renderer;
        private Rect boundingBox;
        private int layer;

        public ExitJoint(EditorGraphics editorGraphics, GraphicalElement parent = null) : base(editorGraphics, true, parent)
        {
            renderer = new ExitJointRenderer(this, editorGraphics.ColorPalette);
        }

        public override GraphicalElementRenderer Renderer
        {
            get
            {
                return renderer;
            }
        }

        public override Rect BoundingBox
        {
            get
            {
                return new Rect(Position - size / 2f, size);
            }
        }

        public override int Layer
        {
            get
            {
                return 0;
            }
        }
    }
}
