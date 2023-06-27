// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEngine;

namespace VRBuilder.Editor.UI.Graphics.Renderers
{
    /// <summary>
    /// Base class for rendering nodes of the Workflow window (entry node, exit node, and step node).
    /// </summary>
    internal abstract class EditorNodeRenderer<TOwner> : ColoredGraphicalElementRenderer<TOwner> where TOwner : EditorNode
    {
        ///<inheritdoc />
        public override Color NormalColor
        {
            get
            {
                return ColorPalette.ElementBackground;
            }
        }

        public EditorNodeRenderer(TOwner owner, WorkflowEditorColorPalette colorPalette) : base(owner, colorPalette)
        {
        }
    }
}
