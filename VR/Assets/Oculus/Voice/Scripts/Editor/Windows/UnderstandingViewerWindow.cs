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

using Facebook.WitAi;
using Facebook.WitAi.Utilities;
using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Windows
{
    public class UnderstandingViewerWindow : WitUnderstandingViewer
    {
        [MenuItem("Oculus/Voice SDK/Understanding Viewer", false, 100)]
        static void Init()
        {
            if (witConfigs.Length == 0)
            {
                RefreshConfigList();
            }
            if (witConfigs.Length > 0)
            {
                UnderstandingViewerWindow window = GetWindow(typeof(UnderstandingViewerWindow)) as
                    UnderstandingViewerWindow;
                window.titleContent = new GUIContent("Understanding Viewer");
                window.autoRepaintOnSceneChange = true;
                window.Show();
            }
            else
            {
                var wizard = ScriptableWizard.DisplayWizard<WelcomeWizard>("Welcome to Voice SDK", "Link");
                wizard.successAction = Init;
            }
        }

        protected override string HeaderLink
        {
            get
            {
                if (null != witConfiguration && null != witConfiguration.application &&
                    !string.IsNullOrEmpty(witConfiguration.application.id))
                {
                    return $"https://wit.ai/apps/{witConfiguration.application.id}/understanding";
                }

                return null;
            }
        }
    }
}
