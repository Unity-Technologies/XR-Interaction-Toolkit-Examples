// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Editor.UI;
using UnityEditor;
using UnityEngine;
using VRBuilder.Editor;
#if CREATOR_PRO
using Innoactive.CreatorPro.Account;
#endif

namespace VRBuilder.Core
{
    /// <summary>
    /// Overlay for the workflow editor showing basic instructions on how to get started,
    /// e.g. how to create a first step.
    /// </summary>
    internal class WorkflowInstructionOverlay : IEditorGraphicDrawer
    {
        public int Priority { get; } = 100;

        private Texture2D backgroundTexture;

        private readonly float width = 420;
        private readonly float height = 40;

        public WorkflowInstructionOverlay()
        {
            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.3f));
            backgroundTexture.Apply();
        }

        /// <inheritdoc />
        public void Draw(Rect windowRect)
        {
#if CREATOR_PRO
            // Do not show when user is not logged in.
            if (UserAccount.IsAccountLoggedIn() == false)
            {
                return;
            }
#endif

            IChapter chapter = GlobalEditorHandler.GetCurrentChapter();
            if(chapter != null && (chapter.Data.FirstStep == null && chapter.Data.Steps.Count == 0))
            {
                GUIStyle style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = new Color(1, 1, 1, 0.70f);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 20;

                GUIStyle backgroundStyle = new GUIStyle(GUI.skin.box);
                backgroundStyle.normal.background = backgroundTexture;

                Rect positionalRect = new Rect(windowRect.x + (windowRect.width / 2) - (width / 2),  windowRect.y + (windowRect.height / 2) - (height / 2), width, height);

                GUI.Box(positionalRect, "", backgroundStyle);
                GUI.Label(positionalRect, "Right-click to create new step", style);
            }
        }
    }
}
