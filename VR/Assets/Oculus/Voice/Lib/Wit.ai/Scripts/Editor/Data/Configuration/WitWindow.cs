/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi.Data.Configuration
{
    public class WitWindow : BaseWitWindow
    {
        public static void ShowWindow()
        {
            if (WitAuthUtility.IsServerTokenValid())
            {
                GetWindow<WitWindow>("Wit Settings");
            }
            else
            {
                WitWelcomeWizard.ShowWizard(ShowWindow);
            }
        }

        protected override string HeaderLink
        {
            get
            {
                if (null != witConfiguration && null != witConfiguration.application &&
                    !string.IsNullOrEmpty(witConfiguration.application.id))
                {
                    return $"https://wit.ai/apps/{witConfiguration.application.id}/settings";
                }

                return null;
            }
        }

        private Texture2D tex;
        private bool manualToken;
        protected Vector2 scroll;
        protected WitConfigurationEditor witEditor;
        protected string serverToken;
        protected bool welcomeSizeSet;

        protected override void OnDrawContent()
        {
            if (!WitAuthUtility.IsServerTokenValid())
            {
                DrawWelcome();
            }
            else
            {
                DrawWit();
            }
        }

        protected virtual void SetWitEditor()
        {
            if (witConfiguration)
            {
                witEditor = (WitConfigurationEditor) Editor.CreateEditor(witConfiguration);
                witEditor.drawHeader = false;
                witEditor.Initialize();
            }
        }

        protected override void OnEnable()
        {
            WitAuthUtility.InitEditorTokens();
            SetWitEditor();
            RefreshConfigList();
        }

        protected virtual void DrawWit()
        {
            // Recommended max size based on EditorWindow.maxSize doc for resizable window.
            if (welcomeSizeSet)
            {
                welcomeSizeSet = false;
                maxSize = new Vector2(4000, 4000);
            }

            titleContent = new GUIContent("Wit Configuration");

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            if (null == serverToken)
            {
                serverToken = WitAuthUtility.ServerToken;
            }
            serverToken = EditorGUILayout.PasswordField("Server Access Token", serverToken);
            if (GUILayout.Button(WitStyles.PasteIcon, WitStyles.ImageIcon))
            {
                serverToken = EditorGUIUtility.systemCopyBuffer;
                WitAuthUtility.ServerToken = serverToken;
                RefreshContent();
            }
            if (GUILayout.Button("Relink", GUILayout.Width(75)))
            {
                if (WitAuthUtility.IsServerTokenValid(serverToken))
                {
                    WitConfigurationEditor.UpdateTokenData(serverToken, RefreshContent);
                }

                WitAuthUtility.ServerToken = serverToken;
                RefreshContent();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            var configChanged = DrawWitConfigurationPopup();
            if (GUILayout.Button("Create", GUILayout.Width(75)))
            {
                CreateConfiguration();
            }
            GUILayout.EndHorizontal();

            if (witConfiguration && (configChanged || !witEditor))
            {
                WitConfiguration config = (WitConfiguration) witConfiguration;
                SetWitEditor();
            }

            if(witConfiguration && witEditor) witEditor.OnInspectorGUI();

            GUILayout.EndVertical();
        }

        protected virtual void CreateConfiguration()
        {
            var asset = WitConfigurationEditor.CreateWitConfiguration(serverToken, Repaint);
            if (asset)
            {
                RefreshConfigList();
                witConfigIndex = Array.IndexOf(witConfigs, asset);
                witConfiguration = asset;
                SetWitEditor();
            }
        }

        protected virtual void DrawWelcome()
        {
            titleContent = WitStyles.welcomeTitleContent;

            if (!welcomeSizeSet)
            {
                minSize = new Vector2(450, 686);
                maxSize = new Vector2(450, 686);
                welcomeSizeSet = true;
            }

            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label("Build Natural Language Experiences", WitStyles.LabelHeader);
            GUILayout.Label(
                "Enable people to interact with your products using voice and text.",
                WitStyles.LabelHeader2);
            GUILayout.Space(32);


            BeginCenter(296);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Paste your Server Access Token here", WitStyles.Label);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(WitStyles.PasteIcon, WitStyles.Label))
            {
                serverToken = EditorGUIUtility.systemCopyBuffer;
                WitAuthUtility.ServerToken = serverToken;
                if (WitAuthUtility.IsServerTokenValid())
                {
                    RefreshContent();
                }
            }
            GUILayout.EndHorizontal();
            if (null == serverToken)
            {
                serverToken = WitAuthUtility.ServerToken;
            }
            GUILayout.BeginHorizontal();
            serverToken = EditorGUILayout.PasswordField(serverToken, WitStyles.TextField);
            if (GUILayout.Button("Link", GUILayout.Width(75)))
            {
                WitAuthUtility.ServerToken = serverToken;
                if (WitAuthUtility.IsServerTokenValid())
                {
                    RefreshContent();
                }
            }
            GUILayout.EndHorizontal();
            EndCenter();

            BeginCenter();
            GUILayout.Label("or", WitStyles.Label);
            EndCenter();

            BeginCenter();

            if (GUILayout.Button(WitStyles.ContinueButton, WitStyles.Label, GUILayout.Height(50),
                GUILayout.Width(296)))
            {
                Application.OpenURL("https://wit.ai");
            }

            GUILayout.Label(
                "Please connect with Facebook login to continue using Wit.ai by clicking on the “Continue with Github Login” and following the instructions provided.",
                WitStyles.Label,
                GUILayout.Width(296));
            EndCenter();

            BeginCenter();
            GUILayout.Space(16);

            EndCenter();
            GUILayout.EndScrollView();
        }
    }
}
