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

using Oculus.Voice.Utility;
using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Windows
{
    public class VoiceSDKWizardWindow : ScriptableWizard
    {
        protected virtual float ContentHeight => 0;
        protected virtual float ContentWidth => 415;

        protected override bool DrawWizardGUI()
        {
            var header = VoiceSDKStyles.MainHeader;
            var headerHeight = header.height * (ContentWidth - 4) / header.width;
            maxSize = new Vector2(ContentWidth, ContentHeight + headerHeight);
            minSize = maxSize;
            GUILayout.Box(header, GUILayout.Width(ContentWidth - 8), GUILayout.Height(headerHeight));

            return false;
        }
    }
}
