/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Facebook.WitAi.Data.Entities;
using Facebook.WitAi.Data.Intents;
using Facebook.WitAi.Data.Traits;
using Facebook.WitAi.Utilities;
using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi.Data.Configuration
{
    public class WitConfigurationEditor : Editor
    {
        public IApplicationDetailProvider appDrawer = new WitApplicationDetailProvider();

        private WitConfiguration configuration;

        private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

        private int selectedToolPanel;

        private readonly string[] toolPanelNames = new[]
        {
            "Application",
            "Intents",
            "Entities",
            "Traits"
        };

        private readonly string[] toolPanelNamesOnlyAppInfo = new[]
        {
            "Application"
        };

        private readonly string[] toolPanelNamesWithoutAppInfo = new[]
        {
            "Intents",
            "Entities",
            "Traits"
        };

        private const int TOOL_PANEL_APP = 0;
        private const int TOOL_PANEL_INTENTS = 1;
        private const int TOOL_PANEL_ENTITIES = 2;
        private const int TOOL_PANEL_TRAITS = 3;

        private Editor applicationEditor;
        private Vector2 scroll;
        private bool appConfigurationFoldout;
        private bool initialized = false;
        public bool drawHeader = true;
        private string currentToken;

        private bool IsTokenValid => !string.IsNullOrEmpty(configuration.clientAccessToken) &&
                                     configuration.clientAccessToken.Length == 32;

        public void Initialize()
        {
        WitAuthUtility.InitEditorTokens();
        configuration = target as WitConfiguration;
        currentToken = WitAuthUtility.GetAppServerToken(configuration);
        if (WitAuthUtility.IsServerTokenValid(currentToken) && !string.IsNullOrEmpty(configuration?.clientAccessToken))
            {
                configuration?.UpdateData(() =>
                {
                    EditorForegroundRunner.Run(() => EditorUtility.SetDirty(configuration));
                });
            }
        }

        public override void OnInspectorGUI()
        {
            if (!initialized || configuration != target)
            {
                Initialize();
                initialized = true;
            }

            if (drawHeader)
            {
                string link = null;
                if (configuration && null != configuration.application &&
                    !string.IsNullOrEmpty(configuration.application.id))
                {
                    link = $"https://wit.ai/apps/{configuration.application.id}/settings";
                }

                BaseWitWindow.DrawHeader(headerLink: link);
            }

            GUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.BeginHorizontal();
            appConfigurationFoldout = EditorGUILayout.Foldout(appConfigurationFoldout,
                "Application Configuration");
            if (!string.IsNullOrEmpty(configuration?.application?.name))
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(configuration?.application?.name);
            }

            GUILayout.EndHorizontal();

            bool hasApplicationInfo = configuration && null != configuration.application;

            if (appConfigurationFoldout || !IsTokenValid)
            {
                GUILayout.BeginHorizontal();
                var token = EditorGUILayout.PasswordField("Server Access Token", currentToken);
                if (token != currentToken)
                {
                    currentToken = token;
                    ApplyToken(token);
                }

                if (configuration && GUILayout.Button("Refresh", GUILayout.Width(75)))
                {
                    ApplyToken(currentToken);
                }

                if (GUILayout.Button(WitStyles.PasteIcon, WitStyles.ImageIcon))
                {
                    currentToken = EditorGUIUtility.systemCopyBuffer;
                    ApplyToken(currentToken);
                }

                GUILayout.EndHorizontal();

                if (configuration)
                {
                    var clientToken =
                        EditorGUILayout.PasswordField("Client Access Token",
                            configuration.clientAccessToken);
                    if (clientToken != configuration.clientAccessToken)
                    {
                        configuration.clientAccessToken = clientToken;
                        EditorUtility.SetDirty(configuration);
                    }
                }
            }
            GUILayout.EndVertical();

            if (hasApplicationInfo)
            {
                if (configuration.application.id != null && !configuration.application.id.StartsWith("voice"))
                {
                    selectedToolPanel = GUILayout.Toolbar(selectedToolPanel, toolPanelNames);
                }
                else
                {
                    selectedToolPanel = GUILayout.Toolbar(selectedToolPanel, toolPanelNamesOnlyAppInfo);
                }
            }
            else
            {
                selectedToolPanel = GUILayout.Toolbar(selectedToolPanel, toolPanelNamesWithoutAppInfo);
            }

            scroll = GUILayout.BeginScrollView(scroll);
            switch (hasApplicationInfo ? selectedToolPanel : selectedToolPanel + 1)
            {
                case TOOL_PANEL_APP:
                    DrawApplication(configuration.application);
                    break;
                case TOOL_PANEL_INTENTS:
                    DrawIntents();
                    break;
                case TOOL_PANEL_ENTITIES:
                    DrawEntities();
                    break;
                case TOOL_PANEL_TRAITS:
                    DrawTraits();
                    break;
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("Open Wit.ai"))
            {
                if (!string.IsNullOrEmpty(configuration.application?.id))
                {
                    Application.OpenURL($"https://wit.ai/apps/{configuration.application.id}");
                }
                else
                {
                    Application.OpenURL("https://wit.ai");
                }
            }
        }

        private void ApplyToken(string token)
        {
            if (!string.IsNullOrEmpty(token) && WitAuthUtility.IsServerTokenValid(token))
            {
                RefreshAppData(WitAuthUtility.GetAppId(token),  token);
            }
        }

        private void RefreshAppData(string appId, string token = "")
        {
            var refreshToken = WitAuthUtility.GetAppServerToken(appId, token);
            if (string.IsNullOrEmpty(refreshToken) &&
                string.IsNullOrEmpty(appId) &&
                !string.IsNullOrEmpty(configuration?.application?.id))
            {
                refreshToken = WitAuthUtility.GetAppServerToken(configuration.application.id,
                    WitAuthUtility.ServerToken);
                appId = WitAuthUtility.GetAppId(refreshToken);
                if (string.IsNullOrEmpty(appId))
                {
                    UpdateTokenData(refreshToken, () =>
                    {
                        appId = WitAuthUtility.GetAppId(refreshToken);
                        if (appId == configuration.application.id)
                        {
                            configuration.FetchAppConfigFromServerToken(refreshToken, () =>
                            {
                                currentToken = refreshToken;
                                WitAuthUtility.SetAppServerToken(configuration.application.id, currentToken);
                                EditorUtility.SetDirty(configuration);
                                EditorForegroundRunner.Run(Repaint);
                                appConfigurationFoldout = false;
                            });
                        }
                    });
                    return;
                }

                if (appId == configuration.application.id)
                {
                    refreshToken = WitAuthUtility.ServerToken;
                }
            }

            if (currentToken != refreshToken)
            {
                currentToken = refreshToken;
            }

            configuration.FetchAppConfigFromServerToken(refreshToken, () =>
            {
                currentToken = refreshToken;
                EditorForegroundRunner.Run(Repaint);
                appConfigurationFoldout = false;
            });
        }

        public static void UpdateTokenData(string serverToken, Action updateComplete = null)
        {
            if (!WitAuthUtility.IsServerTokenValid(serverToken)) return;

            var listRequest = WitRequestFactory.ListAppsRequest(serverToken, 10000);
            listRequest.onResponse = (r) =>
            {
                if (r.StatusCode == 200)
                {
                    var applications = r.ResponseData.AsArray;
                    for (int i = 0; i < applications.Count; i++)
                    {
                        if (applications[i]["is_app_for_token"].AsBool)
                        {
                            var application = WitApplication.FromJson(applications[i]);
                            EditorForegroundRunner.Run(() =>
                            {
                                WitAuthUtility.SetAppServerToken(application.id, serverToken);
                                updateComplete?.Invoke();
                            });
                            break;
                        }
                    }
                }
                else
                {
                    Debug.LogError(r.StatusDescription);
                }
            };
            listRequest.Request();
        }

        private void DrawTraits()
        {
            var traits = configuration.traits;
            if (null != traits)
            {
                var n = traits.Length;
                if (n == 0)
                {
                    GUILayout.Label("No traits available.");
                }
                else
                {
                    BeginIndent();
                    for (int i = 0; i < n; i++)
                    {
                        var trait = traits[i];
                        if (null != trait && Foldout("t:", trait.name))
                        {
                            DrawTrait(trait);
                        }
                    }

                    EndIndent();
                }
            }
            else
            {
                GUILayout.Label("Traits have not been loaded yet.", EditorStyles.helpBox);
            }
        }

        private void DrawTrait(WitTrait trait)
        {
            InfoField("ID", trait.id);
            InfoField("Name", trait.name);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Values", GUILayout.Width(100));
            GUILayout.EndHorizontal();
            var values = trait.values;
            var n = values.Length;
            if (n == 0)
            {
                GUILayout.Label("No values available.");
            }
            else
            {
                BeginIndent();
                for (int i = 0; i < n; i++)
                {
                    var value = values[i];
                    if (null != value && Foldout("v:", value.value))
                    {
                        DrawTraitValue(value);
                    }
                }

                EndIndent();
            }
        }

        private void DrawTraitValue(WitTraitValue traitValue)
        {
            InfoField("ID", traitValue.id);
            InfoField("Value", traitValue.value);
        }

        private void DrawEntities()
        {
            var entities = configuration.entities;
            if (null != entities)
            {
                var n = entities.Length;
                if (n == 0)
                {
                    GUILayout.Label("No entities available.");
                }
                else
                {
                    BeginIndent();
                    for (int i = 0; i < n; i++)
                    {
                        var entity = entities[i];
                        if (null != entity && Foldout("e:", entity.name))
                        {
                            DrawEntity(entity);
                        }
                    }

                    EndIndent();
                }
            }
            else
            {
                GUILayout.Label("Entities have not been loaded yet.", EditorStyles.helpBox);
            }
        }

        private void DrawApplication(WitApplication application)
        {
            appDrawer.DrawApplication(application);
        }

        private void DrawEntity(WitEntity entity)
        {
            InfoField("ID", entity.id);
            if (null != entity.roles && entity.roles.Length > 0)
            {
                EditorGUILayout.Popup("Roles", 0, entity.roles);
            }

            if (null != entity.lookups && entity.lookups.Length > 0)
            {
                EditorGUILayout.Popup("Lookups", 0, entity.lookups);
            }
        }

        private void DrawIntents()
        {
            var intents = configuration.intents;
            if (null != intents)
            {
                var n = intents.Length;
                if (n == 0)
                {
                    GUILayout.Label("No intents available.");
                }
                else
                {
                    BeginIndent();
                    for (int i = 0; i < n; i++)
                    {
                        var intent = intents[i];
                        if (null != intent && Foldout("i:", intent.name))
                        {
                            DrawIntent(intent);
                        }
                    }

                    EndIndent();
                }
            }
            else
            {
                GUILayout.Label("Intents have not been loaded yet.", EditorStyles.helpBox);
            }
        }

        private void DrawIntent(WitIntent intent)
        {
            InfoField("ID", intent.id);
            var entities = intent.entities;
            if (entities.Length > 0)
            {
                var entityNames = entities.Select(e => e.name).ToArray();
                EditorGUILayout.Popup("Entities", 0, entityNames);
            }
        }

        #region UI Components

        private void BeginIndent()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginVertical();
        }

        private void EndIndent()
        {
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void InfoField(string name, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(100));
            GUILayout.Label(value, "TextField");
            GUILayout.EndHorizontal();
        }

        private bool Foldout(string keybase, string name)
        {
            string key = keybase + name;
            bool show = false;
            if (!foldouts.TryGetValue(key, out show))
            {
                foldouts[key] = false;
            }

            show = EditorGUILayout.Foldout(show, name, true);
            foldouts[key] = show;
            return show;
        }

        #endregion

        public static WitConfiguration CreateWitConfiguration(string serverToken, Action onCreationComplete)
        {
            var path = EditorUtility.SaveFilePanel("Create Wit Configuration", Application.dataPath,
                "WitConfiguration", "asset");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                WitConfiguration asset = ScriptableObject.CreateInstance<WitConfiguration>();

                asset.FetchAppConfigFromServerToken(serverToken, onCreationComplete);

                path = path.Substring(Application.dataPath.Length - 6);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                return asset;
            }

            return null;
        }
    }
}
