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
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data;
using Facebook.WitAi.Data.Configuration;
using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Data
{
    [Serializable]
    public class VoiceSDKDataCreation
    {

        [MenuItem("Assets/Create/Voice SDK/Add App Voice Experience to Scene")]
        public static void AddVoiceCommandServiceToScene()
        {
            var witGo = new GameObject();
            witGo.name = "App Voice Experience";
            var wit = witGo.AddComponent<AppVoiceExperience>();
            wit.RuntimeConfiguration = new WitRuntimeConfiguration
            {
                witConfiguration = WitDataCreation.FindDefaultWitConfig()
            };
        }

        [MenuItem("Assets/Create/Voice SDK/Values/String Value")]
        public static void WitStringValue()
        {
            WitDataCreation.CreateStringValue("");
        }

        [MenuItem("Assets/Create/Voice SDK/Values/Float Value")]
        public static void WitFloatValue()
        {
            WitDataCreation.CreateFloatValue("");
        }

        public static WitFloatValue CreateFloatValue(string path)
        {
            return WitDataCreation.CreateFloatValue(path);
        }

        [MenuItem("Assets/Create/Voice SDK/Values/Int Value")]
        public static void WitIntValue()
        {
            WitDataCreation.CreateStringValue("");
        }

        [MenuItem("Assets/Create/Voice SDK/Configuration")]
        public static void CreateWitConfiguration()
        {
            WitConfigurationEditor.CreateWitConfiguration(WitAuthUtility.ServerToken, null);
        }
    }
}
