// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Editor.UI.Graphics.Renderers;
using UnityEngine;
using UnityEditor;

namespace VRBuilder.Editor.UI.Graphics
{
    internal class EntryNodeRenderer : MulticoloredGraphicalElementRenderer<EntryNode>
    {
        private static int LabelWidth = 30;
        private static int LabelHeight = 50;

        /// <inheritdoc />
        public override Color NormalColor
        {
            get
            {
                if (Owner.IsDragging)
                {
                    return SelectedColor;
                }
                return ColorPalette.ElementBackground;
            }
        }

        protected override Color PressedColor
        {
            get
            {
                return SelectedColor;
            }
        }

        protected override Color HoveredColor
        {
            get
            {
                return ColorPalette.Secondary;
            }
        }

        protected override Color TextColor
        {
            get
            {
                return ColorPalette.Text;
            }
        }

        protected virtual Color SelectedColor
        {
            get
            {
                return ColorPalette.Primary;
            }
        }

        public EntryNodeRenderer(EntryNode owner, WorkflowEditorColorPalette colorPalette) : base(owner, colorPalette)
        {
        }

        /// <inheritdoc />
        public override void Draw()
        {
            EditorDrawingHelper.DrawCircle(Owner.BoundingBox.center, Owner.BoundingBox.size.x / 2f, CurrentColor);
            Rect StartLabelRect = new Rect(Owner.BoundingBox.center.x - LabelWidth / 2f, Owner.BoundingBox.center.y, LabelWidth, LabelHeight);
            EditorGUI.LabelField(StartLabelRect, "Start", EditorStyles.label);
        }
    }
}
