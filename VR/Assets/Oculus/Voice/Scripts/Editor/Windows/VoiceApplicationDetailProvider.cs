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

using UnityEngine;
using Facebook.WitAi.Data.Configuration;

namespace Oculus.Voice.Windows
{
    public class VoiceApplicationDetailProvider : IApplicationDetailProvider
    {
        public void DrawApplication(WitApplication application)
        {
            if (string.IsNullOrEmpty(application.name))
            {
                GUILayout.Label("Loading...");
            }
            else
            {
                if (application.id.StartsWith("voice"))
                {
                    InfoField("Name", application.name);
                    InfoField("Language", application.lang);
                }
                else
                {
                    InfoField("Name", application.name);
                    InfoField("ID", application.id);
                    InfoField("Language", application.lang);
                    InfoField("Created", application.createdAt);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Private", GUILayout.Width(100));
                    GUILayout.Toggle(application.isPrivate, "");
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void InfoField(string name, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(100));
            GUILayout.Label(value, "TextField");
            GUILayout.EndHorizontal();
        }
    }
}
