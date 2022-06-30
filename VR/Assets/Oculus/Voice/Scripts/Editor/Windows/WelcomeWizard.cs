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
using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Windows
{
    public class WelcomeWizard : VoiceSDKWizardWindow
    {
        [SerializeField] private string serverToken;
        [SerializeField] private int witBuiltInIndex;

        protected override float ContentHeight
        {
            get
            {
                var height = 250f;
                if (witBuiltInIndex <= 0)
                {
                    height += 3 * EditorGUIUtility.singleLineHeight;
                }

                return height;
            }
        }

        private string[] builtinAppNames;
        public Action successAction;

        private void OnWizardCreate()
        {
            if (witBuiltInIndex == 0)
            {
                WitAuthUtility.ServerToken = serverToken;
            }

            if (WitAuthUtility.IsServerTokenValid())
            {
                if (witBuiltInIndex > 0)
                {
                    SettingsWindow.CreateConfiguration(WitAuthUtility.ServerToken,
                        builtinAppNames[witBuiltInIndex], successAction);
                }
                else
                {
                    SettingsWindow.CreateConfiguration(WitAuthUtility.ServerToken, null, successAction);
                }

                Close();
            }
            else
            {
                throw new ArgumentException(
                    "Server token is not valid. Please set a server token.");
            }
        }

        protected virtual void OnEnable()
        {
            WitAuthUtility.InitEditorTokens();
            WitAuthUtility.tokenValidator = new VoiceSDKTokenValidatorProvider();
            var names = AppBuiltIns.appNames;
            builtinAppNames = new string[names.Length + 1];
            builtinAppNames[0] = "Custom App";
            for (int i = 0; i < names.Length; i++)
            {
                builtinAppNames[i + 1] = names[i];
            }
        }

        protected override bool DrawWizardGUI()
        {
            base.DrawWizardGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Space(24);
            GUILayout.BeginVertical();
            GUILayout.Label("Building App Voice Experiences", WitStyles.LabelHeader, GUILayout.Height(64));
            GUILayout.Label(
                "Empowering developers to build engaging voice interactions.",GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
            GUILayout.EndVertical();
            GUILayout.Space(24);
            GUILayout.EndHorizontal();


            BaseWitWindow.BeginCenter(296);
            GUILayout.Label("Select language to use Built-In NLP", WitStyles.Label);
            int selected = EditorGUILayout.Popup("", witBuiltInIndex, builtinAppNames);
            if (selected != witBuiltInIndex)
            {
                witBuiltInIndex = selected;
                WitAuthUtility.ServerToken =
                    AppBuiltIns.builtInPrefix + AppBuiltIns.appNames[witBuiltInIndex];
            }

            BaseWitWindow.EndCenter();

            if (witBuiltInIndex <= 0)
            {
                GUILayout.Space(16);

                BaseWitWindow.BeginCenter(296);

                GUILayout.BeginHorizontal();
                var color = "blue";
                if (EditorGUIUtility.isProSkin)
                {
                    color = "#ccccff";
                }
                if (GUILayout.Button(
                    $"Paste your <color={color}>Wit.ai</color> Server Access Token here",
                    WitStyles.Label))
                {
                    Application.OpenURL("https://wit.ai/apps");
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button(WitStyles.PasteIcon, WitStyles.Label))
                {
                    serverToken = EditorGUIUtility.systemCopyBuffer;
                    WitAuthUtility.ServerToken = serverToken;

                }

                GUILayout.EndHorizontal();
                if (null == serverToken)
                {
                    serverToken = WitAuthUtility.ServerToken;
                }

                serverToken = EditorGUILayout.PasswordField(serverToken);
                BaseWitWindow.EndCenter();
            }

            return WitAuthUtility.IsServerTokenValid();
        }
    }

    public class VoiceSDKTokenValidatorProvider : WitAuthUtility.ITokenValidationProvider
    {
        public bool IsTokenValid(string appId, string token)
        {
            return IsServerTokenValid(token);
        }

        public bool IsServerTokenValid(string serverToken)
        {
            return null != serverToken && (serverToken.Length == 32 || serverToken.StartsWith(AppBuiltIns.builtInPrefix));
        }
    }
}
