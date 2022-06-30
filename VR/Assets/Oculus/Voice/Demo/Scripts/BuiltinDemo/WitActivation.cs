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

namespace Oculus.Voice.Demo.BuiltInDemo
{
    public class WitActivation : MonoBehaviour
    {
        [SerializeField] private AppVoiceExperience voiceExperience;

        private void OnValidate()
        {
            if (!voiceExperience) voiceExperience = GetComponent<AppVoiceExperience>();
        }

        private void Start()
        {
            voiceExperience = GetComponent<AppVoiceExperience>();
        }

        private void Update()
        {
            // Make it possible to activate wit in the Unity Editor without the need to deploy to the headset.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                print("*** Pressed Space bar ***");
                ActivateWit();
            }
        }

        /// <summary>
        /// Activates Wit i.e. start listening to the user.
        /// </summary>
        public void ActivateWit()
        {
            voiceExperience.Activate();
        }
    }
}
