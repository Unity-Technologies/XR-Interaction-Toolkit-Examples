/**************************************************************************************************
 * Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.
 *
 * Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
 * under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 **************************************************************************************************/

using System;
using Facebook.WitAi;
using Facebook.WitAi.Data.Configuration;
using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Windows
{
    public class SettingsWindow : WitWindow
    {
        [MenuItem("Oculus/Voice SDK/Settings", false, 100)]
        public static void ShowSettingsWindow()
        {
            if (WitAuthUtility.IsServerTokenValid())
            {
                GetWindow<SettingsWindow>("Welcome to Voice SDK");
            }
            else
            {
                var wizard =
                    ScriptableWizard.DisplayWizard<WelcomeWizard>("Welcome to Voice SDK", "Link");
                wizard.successAction = ShowSettingsWindow;
            }
        }

        protected override void OnEnable()
        {
            WitAuthUtility.InitEditorTokens();
            WitAuthUtility.tokenValidator = new VoiceSDKTokenValidatorProvider();
            SetWitEditor();
            RefreshConfigList();
        }

        protected override void SetWitEditor(){
            if (witConfiguration)
            {
                witEditor = (WitConfigurationEditor) Editor.CreateEditor(witConfiguration);
                witEditor.drawHeader = false;
                witEditor.appDrawer = new VoiceApplicationDetailProvider();
            }
        }

        public static WitConfiguration CreateConfiguration(string serverToken, string language = null, Action onCompleteAction = null)
        {
            var path = EditorUtility.SaveFilePanel("Create Wit Configuration", Application.dataPath,
                "WitConfiguration", "asset");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                WitConfiguration asset = ScriptableObject.CreateInstance<WitConfiguration>();
                if (null == language)
                {
                    asset.FetchAppConfigFromServerToken(serverToken, onCompleteAction);
                }
                else if (AppBuiltIns.apps.ContainsKey(language))
                {
                    var app = AppBuiltIns.apps[language];
                    asset.application = new WitApplication();
                    asset.application.name = app["name"];
                    asset.application.id = app["id"];
                    asset.application.lang = app["lang"];
                    asset.clientAccessToken = app["clientToken"];
                    onCompleteAction?.Invoke();
                }
                path = path.Substring(Application.dataPath.Length - 6);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                return asset;
            }

            return null;
        }

        protected override void OnDrawContent()
        {
            DrawWit();
        }

        protected override void DrawWelcome(){
            titleContent = WitStyles.welcomeTitleContent;

            if (!welcomeSizeSet)
            {
                minSize = new Vector2(450, 350);
                maxSize = new Vector2(450, 350);
                welcomeSizeSet = true;
            }

            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label("Build Natural Language Experiences", WitStyles.LabelHeader);
            GUILayout.Label(
                "Empower people to use your product with voice and text",
                WitStyles.LabelHeader2);
            GUILayout.Space(32);


            BeginCenter(296);
            GUILayout.Label("Select language to use Built-In NLP", WitStyles.Label);
            int witBuiltInIndex = -1;
            int selected = EditorGUILayout.Popup("", witBuiltInIndex, AppBuiltIns.appNames);
            if (selected != witBuiltInIndex)
            {
                witBuiltInIndex = selected;
                WitAuthUtility.ServerToken = AppBuiltIns.builtInPrefix+AppBuiltIns.appNames[witBuiltInIndex];
                CreateConfiguration(AppBuiltIns.appNames[witBuiltInIndex]);
                RefreshContent();
            }
            EndCenter();

            GUILayout.Space(16);

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
            GUILayout.EndScrollView();
        }
    }
}
