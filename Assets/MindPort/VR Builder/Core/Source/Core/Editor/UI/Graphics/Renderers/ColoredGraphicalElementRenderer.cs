// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEngine;

namespace VRBuilder.Editor.UI.Graphics.Renderers
{
    /// <summary>
    /// Base class for graphical element renderers that use only one color.
    /// </summary>
    internal abstract class ColoredGraphicalElementRenderer<TOwner> : GraphicalElementRenderer<TOwner> where TOwner : GraphicalElement
    {
        /// <summary>
        /// Color palette which is used in current Workflow window. Use colors from it to keep your elements in the same style.
        /// </summary>
        protected WorkflowEditorColorPalette ColorPalette
        {
            get;
            private set;
        }

        /// <summary>
        /// Default color of the element.
        /// </summary>
        public abstract Color NormalColor
        {
            get;
        }

        /// <summary>
        /// Current color of the element.
        /// </summary>
        public Color CurrentColor
        {
            get;
            set;
        }

        protected ColoredGraphicalElementRenderer(TOwner owner, WorkflowEditorColorPalette colorPalette) : base(owner)
        {
            ColorPalette = colorPalette;
            CurrentColor = NormalColor;
        }
    }
}
