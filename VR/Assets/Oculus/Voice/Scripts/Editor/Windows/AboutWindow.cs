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
using Oculus.Voice.Utility;
using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Windows
{
    public class AboutWindow : VoiceSDKWizardWindow
    {
        protected override float ContentHeight => EditorGUIUtility.singleLineHeight * 4 + 16 + 100;

        [MenuItem("Oculus/Voice SDK/About", false, 200)]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<AboutWindow>("About Voice SDK", "Close");
        }

        protected override bool DrawWizardGUI()
        {
            base.DrawWizardGUI();

            GUILayout.Label("Voice SDK Version: " + VoiceSDKVersion.VERSION);
            GUILayout.Label("Wit.ai SDK Version: " + WitRequest.WIT_SDK_VERSION);
            GUILayout.Label("Wit.ai API Version: " + WitRequest.WIT_API_VERSION);

            GUILayout.Space(16);

            if (GUILayout.Button("Tutorials"))
            {
                Application.OpenURL("https://developer.oculus.com/experimental/voice-sdk/tutorial-overview/");
            }

            return false;
        }

        private void OnWizardCreate()
        {

        }
    }
}
