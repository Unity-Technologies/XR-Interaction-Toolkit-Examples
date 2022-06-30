/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Data.Configuration;
using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi
{
    public abstract class BaseWitWindow : EditorWindow
    {
        protected static WitConfiguration[] witConfigs = Array.Empty<WitConfiguration>();
        protected static string[] witConfigNames = Array.Empty<string>();
        protected int witConfigIndex = -1;
        protected WitConfiguration witConfiguration;

        public static WitConfiguration[] WitConfigs => witConfigs;
        public static string[] WitConfigNames => witConfigNames;

        protected virtual string HeaderLink => null;

        protected virtual void OnEnable()
        {
            RefreshConfigList();
        }

        protected virtual void OnDisable()
        {

        }

        protected virtual void OnProjectChange()
        {
            RefreshConfigList();
        }

        protected void RefreshContent()
        {
            if (witConfiguration) witConfiguration.UpdateData();
        }

        protected static void RefreshConfigList()
        {
            string[] guids = AssetDatabase.FindAssets("t:WitConfiguration");
            witConfigs = new WitConfiguration[guids.Length];
            witConfigNames = new string[guids.Length];

            for (int i = 0; i < guids.Length; i++) //probably could get optimized
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                witConfigs[i] = AssetDatabase.LoadAssetAtPath<WitConfiguration>(path);
                witConfigNames[i] = witConfigs[i].name;
            }
        }

        protected virtual void OnGUI()
        {
            minSize = new Vector2(450, 300);
            DrawHeader();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            OnDrawContent();
            GUILayout.EndVertical();
        }

        protected abstract void OnDrawContent();

        protected void DrawHeader()
        {
            DrawHeader(HeaderLink);
        }

        public static void DrawHeader(string headerLink = null, Texture2D header = null)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(16);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (!header) header = WitStyles.MainHeader;
            var headerWidth = Mathf.Min(header.width, EditorGUIUtility.currentViewWidth - 64);
            var headerHeight =
                header.height * headerWidth / header.width;
            if (GUILayout.Button(header, "Label", GUILayout.Width(headerWidth), GUILayout.Height(headerHeight)))
            {
                Application.OpenURL(!string.IsNullOrEmpty(headerLink)
                    ? headerLink
                    : "https://wit.ai");
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(16);
            GUILayout.EndVertical();
        }

        protected bool DrawWitConfigurationPopup()
        {
            if (null == witConfigs) return false;

            bool changed = false;
            if (witConfigs.Length == 1)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Wit Configuration");
                EditorGUILayout.LabelField(witConfigNames[0], EditorStyles.popup);
                GUILayout.EndHorizontal();
            }
            else
            {
                var selectedConfig = EditorGUILayout.Popup("Wit Configuration", witConfigIndex, witConfigNames);
                if (selectedConfig != witConfigIndex)
                {
                    witConfigIndex = selectedConfig;
                    changed = true;
                }
            }

            if (changed || witConfigs.Length > 0 && !witConfiguration)
            {
                if (witConfigIndex < 0 || witConfigIndex >= witConfigs.Length)
                {
                    witConfigIndex = 0;
                }
                witConfiguration = witConfigs[witConfigIndex];
                RefreshContent();
            }

            return changed;
        }

        public static void BeginCenter(int width = -1)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (width > 0)
            {
                GUILayout.BeginVertical(GUILayout.Width(width));
            }
            else
            {
                GUILayout.BeginVertical();
            }
        }

        public static void EndCenter()
        {
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}
