// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEngine;

namespace VRBuilder.Editor.UI.Graphics.Renderers
{
    /// <summary>
    /// Renderer for AddStepButton graphical elements.
    /// </summary>
    internal class AddStepButtonRenderer : MulticoloredGraphicalElementRenderer<AddStepButton>
    {
        /// <inheritdoc />
        public override Color NormalColor
        {
            get
            {
                return ColorPalette.ElementBackground;
            }
        }

        /// <inheritdoc />
        protected override Color PressedColor
        {
            get
            {
                return ColorPalette.Primary;
            }
        }

        /// <inheritdoc />
        protected override Color HoveredColor
        {
            get
            {
                return ColorPalette.Secondary;
            }
        }

        ///<inheritdoc />
        protected override Color TextColor
        {
            get
            {
                return ColorPalette.Text;
            }
        }

        public AddStepButtonRenderer(AddStepButton owner, WorkflowEditorColorPalette colorPalette) : base(owner, colorPalette)
        {
        }

        ///<inheritdoc />
        public override void Draw()
        {
            Rect rect = Owner.BoundingBox;
            rect.x = Mathf.Round(rect.x);
            rect.y = Mathf.Round(rect.y);
            rect.height = Mathf.Round(rect.height);
            rect.width = Mathf.Round(rect.width);

            EditorDrawingHelper.DrawRoundedRect(rect, CurrentColor, 4f);
            GUIStyle style = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState() { textColor = TextColor }
            };
            GUI.Label(rect, "+", style);
        }
    }
}
