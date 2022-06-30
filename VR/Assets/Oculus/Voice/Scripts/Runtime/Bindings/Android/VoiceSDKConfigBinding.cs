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

using Facebook.WitAi.Configuration;
using UnityEngine;

namespace Oculus.Voice.Bindings.Android
{
    public class VoiceSDKConfigBinding
    {
        private WitRuntimeConfiguration configuration;

        public VoiceSDKConfigBinding(WitRuntimeConfiguration config)
        {
            configuration = config;
        }

        public AndroidJavaObject ToJavaObject()
        {
            AndroidJavaObject witConfig =
                new AndroidJavaObject("com.oculus.voice.sdk.api.WitConfiguration");
            witConfig.Set("clientAccessToken", configuration.witConfiguration.clientAccessToken);

            AndroidJavaObject witRuntimeConfig = new AndroidJavaObject("com.oculus.voice.sdk.api.WitRuntimeConfiguration");
            witRuntimeConfig.Set("witConfiguration", witConfig);

            witRuntimeConfig.Set("minKeepAliveVolume", configuration.minKeepAliveVolume);
            witRuntimeConfig.Set("minKeepAliveTimeInSeconds",
                configuration.minKeepAliveTimeInSeconds);
            witRuntimeConfig.Set("minTranscriptionKeepAliveTimeInSeconds",
                configuration.minTranscriptionKeepAliveTimeInSeconds);
            witRuntimeConfig.Set("maxRecordingTime",
                configuration.maxRecordingTime);
            witRuntimeConfig.Set("soundWakeThreshold",
                configuration.soundWakeThreshold);
            witRuntimeConfig.Set("sampleLengthInMs",
                configuration.sampleLengthInMs);
            witRuntimeConfig.Set("micBufferLengthInSeconds",
                configuration.micBufferLengthInSeconds);
            witRuntimeConfig.Set("sendAudioToWit",
                configuration.sendAudioToWit);

            return witRuntimeConfig;
        }
    }
}
