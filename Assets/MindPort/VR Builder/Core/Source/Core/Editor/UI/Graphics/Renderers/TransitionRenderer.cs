// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEngine;

namespace VRBuilder.Editor.UI.Graphics.Renderers
{
    /// <summary>
    /// Renderer for transition between editor nodes.
    /// </summary>
    internal class TransitionRenderer : ColoredGraphicalElementRenderer<TransitionElement>
    {
        ///<inheritdoc />
        public override Color NormalColor
        {
            get
            {
                return ColorPalette.Transition;
            }
        }

        public TransitionRenderer(TransitionElement owner, WorkflowEditorColorPalette colorPalette) : base(owner, colorPalette)
        {
        }

        ///<inheritdoc />
        public override void Draw()
        {
            EditorDrawingHelper.DrawPolyline(Owner.PolylinePoints, CurrentColor);
            EditorDrawingHelper.DrawTriangle(Owner.PolylinePoints, CurrentColor);
        }
    }
}
