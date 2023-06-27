// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Wizard
{
    /// <summary>
    /// Wizard base class which allows you to implement a new, awesome wizard!
    /// </summary>
    internal class WizardWindow : EditorWindow
    {
        public static readonly Color LineColor = new Color(0, 0, 0, 0.25f);

        /// <summary>
        /// Will be called when the wizard is closing.
        /// </summary>
        public event EventHandler<EventArgs> WizardClosing;

        [SerializeField]
        private int selectedPage = 0;

        [SerializeField]
        protected WizardSettings Settings = new WizardSettings()
        {
            DrawCloseButton = true,
            DrawPreviousButton = true,
            DrawSkipButton = true,

            BottomBarHeight = 40f,
            NavigationBarRatio = 0.25f,
            ButtonPadding = 8f,

            Size = new Vector2(800, 600),
        };

        protected Vector2 buttonSize;

        [SerializeField]
        protected List<WizardPage> pages = new List<WizardPage>();

        protected WizardNavigation navigation;

        public WizardWindow()
        {
            minSize = Settings.Size;
            maxSize = Settings.Size;

            buttonSize = new Vector2(Settings.BottomBarHeight * 2.5f, Settings.BottomBarHeight - 8);
        }

        public virtual void Setup(string title, List<WizardPage> pageList)
        {
            pages = pageList;
            Settings.Title = title;
            titleContent = new GUIContent(Settings.Title);
        }

        protected void OnEnable()
        {
            titleContent = new GUIContent(Settings.Title);
        }

        protected virtual WizardNavigation CreateNavigation()
        {
            List<IWizardNavigationEntry> entries = new List<IWizardNavigationEntry>();
            foreach (WizardPage page in pages)
            {
                entries.Add(new WizardNavigation.Entry(page.Name, entries.Count));
            }

            entries[selectedPage].Selected = true;
            return new WizardNavigation(entries);
        }

        private void OnGUI()
        {
            if (navigation == null)
            {
                navigation = CreateNavigation();
            }

            navigation.Draw(GetNavigationRect());
            GetActivePage().Draw(GetContentRect());
            DrawBottomBar(GetBottomBarRect());
        }

        protected void DrawBottomBar(Rect window)
        {
            EditorGUI.DrawRect(new Rect(0, window.y, window.width, 1), LineColor);

            Vector2 buttonPosition = new Vector2(window.width - (buttonSize.x + 4), window.y + 4);

            buttonPosition = DrawFinishButton(buttonPosition);
            buttonPosition = DrawNextButton(buttonPosition);
            buttonPosition = DrawRestartButton(buttonPosition);
            buttonPosition = DrawPreviousButton(buttonPosition);
            buttonPosition = DrawSkipButton(buttonPosition);
            buttonPosition = DrawCloseButton(buttonPosition);
        }

        private Vector2 DrawFinishButton(Vector2 position)
        {
            if (selectedPage == pages.Count - 1)
            {
                EditorGUI.BeginDisabledGroup(GetActivePage().CanProceed == false);
                if (GUI.Button(new Rect(position, buttonSize), "Finish"))
                {
                    FinishButtonPressed();
                }
                EditorGUI.EndDisabledGroup();
                return new Vector2(position.x - (buttonSize.x + Settings.ButtonPadding), position.y);
            }

            return position;
        }

        private Vector2 DrawRestartButton(Vector2 position)
        {
            if (GetActivePage().ShouldRestart)
            {
                EditorGUI.BeginDisabledGroup(GetActivePage().CanProceed == false);
                if (GUI.Button(new Rect(position, buttonSize), "Restart"))
                {
                    RestartButtonPressed();
                }
                EditorGUI.EndDisabledGroup();
                return new Vector2(position.x - (buttonSize.x + Settings.ButtonPadding), position.y);
            }

            return position;
        }

        private Vector2 DrawNextButton(Vector2 position)
        {
            if (selectedPage < pages.Count - 1 && GetActivePage().ShouldRestart == false)
            {
                EditorGUI.BeginDisabledGroup(GetActivePage().CanProceed == false);
                if (GUI.Button(new Rect(position, buttonSize), "Next"))
                {
                    NextButtonPressed();
                }
                EditorGUI.EndDisabledGroup();
                return new Vector2(position.x - (buttonSize.x + Settings.ButtonPadding), position.y);
            }

            return position;
        }

        private Vector2 DrawSkipButton(Vector2 position)
        {
            if (Settings.DrawSkipButton == false)
            {
                return position;
            }

            if (selectedPage < pages.Count - 1 && GetActivePage().AllowSkip)
            {
                position = new Vector2(position.x - Settings.ButtonPadding * 6, position.y);
                if (GUI.Button(new Rect(new Vector2(GetNavigationRect().width + 4, position.y), buttonSize), "Skip this Step"))
                {
                    SkipButtonPressed();
                }
            }

            return position;
        }

        private Vector2 DrawPreviousButton(Vector2 position)
        {
            if (Settings.DrawPreviousButton == false)
            {
                return position;
            }

            if (selectedPage > 0)
            {
                if (GUI.Button(new Rect(position, buttonSize), "Previous"))
                {
                    BackButtonPressed();
                }
                EditorGUI.EndDisabledGroup();
                return new Vector2(position.x - (buttonSize.x + Settings.ButtonPadding), position.y);
            }
            return position;
        }

        private Vector2 DrawCloseButton(Vector2 position)
        {
            if (Settings.DrawCloseButton == false)
            {
                return position;
            }

            if (selectedPage == 0)
            {
                if (GUI.Button(new Rect(position, buttonSize), "Close Wizard"))
                {
                    Close();
                }
                EditorGUI.EndDisabledGroup();
                return new Vector2(position.x - (buttonSize.x + Settings.ButtonPadding), position.y);
            }
            return position;
        }

        protected virtual void FinishButtonPressed()
        {
            GetActivePage().Apply();
            Close();
        }

        protected virtual void RestartButtonPressed()
        {
            GetActivePage().Apply();
            Close();

            BuilderProjectSettings settings = BuilderProjectSettings.Load();
            settings.IsFirstTimeStarted = true;
            settings.Save();
        }

        protected virtual void BackButtonPressed()
        {
            GetActivePage().Back();
            selectedPage--;
            navigation.SetSelected(selectedPage);
        }

        protected virtual void SkipButtonPressed()
        {
            GetActivePage().Skip();
            selectedPage++;
            navigation.SetSelected(selectedPage);
        }

        protected virtual void NextButtonPressed()
        {
            GetActivePage().Apply();
            selectedPage++;
            navigation.SetSelected(selectedPage);
        }

        protected virtual void OnDestroy()
        {
            if (selectedPage == pages.Count - 1)
            {
                bool cancelled = pages.GetRange(selectedPage + 1, pages.Count - selectedPage - 1).Any(page => page.Mandatory);
                pages.ForEach(page => page.Closing(!cancelled));
            }

            WizardClosing?.Invoke(this, EventArgs.Empty);
        }

        protected WizardPage GetActivePage()
        {
            return pages[selectedPage];
        }

        protected Rect GetNavigationRect()
        {
            return new Rect(0, 0, Settings.Size.x * Settings.NavigationBarRatio, Settings.Size.y);
        }

        protected Rect GetContentRect()
        {
            return new Rect(Settings.Size.x * Settings.NavigationBarRatio + BuilderEditorStyles.Indent, BuilderEditorStyles.Indent / 2, Settings.Size.x - (Settings.Size.x * Settings.NavigationBarRatio) - (2 * BuilderEditorStyles.Indent), Settings.Size.y - Settings.BottomBarHeight - BuilderEditorStyles.Indent);
        }

        protected Rect GetBottomBarRect()
        {
            return new Rect(Settings.Size.x * Settings.NavigationBarRatio, Settings.Size.y - Settings.BottomBarHeight, Settings.Size.x, Settings.BottomBarHeight);
        }

        [Serializable]
        protected struct WizardSettings
        {
            public bool DrawPreviousButton;
            public bool DrawSkipButton;
            public bool DrawCloseButton;

            public Vector2 Size;

            public float BottomBarHeight;
            public float ButtonPadding;

            public float NavigationBarRatio;

            public string Title;
        }
    }
}
