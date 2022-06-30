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
using Facebook.WitAi.Events;
using Facebook.WitAi.Interfaces;
using Oculus.Voice.Core.Bindings.Android;
using Oculus.Voice.Interfaces;

namespace Oculus.Voice.Bindings.Android
{
    public class VoiceSDKImpl : BaseAndroidConnectionImpl<VoiceSDKBinding>,
        IPlatformVoiceService
    {
        public VoiceSDKImpl() : base(
            "com.oculus.voice.sdk.unity.UnityVoiceSDKServiceFragment")
        {
        }

        public bool PlatformSupportsWit => service.PlatformSupportsWit;

        public bool Active => service.Active;
        public bool IsRequestActive => service.IsRequestActive;
        public bool MicActive => service.MicActive;
        public void SetRuntimeConfiguration(WitRuntimeConfiguration configuration)
        {
            service.SetRuntimeConfiguration(configuration);
        }

        private VoiceSDKListenerBinding eventBinding;

        public VoiceEvents VoiceEvents
        {
            get => eventBinding.VoiceEvents;
            set
            {
                eventBinding = new VoiceSDKListenerBinding(value);
                service.SetListener(eventBinding);
            }
        }

        public ITranscriptionProvider TranscriptionProvider { get; set; }

        public void Activate(string text)
        {
            service.Activate(text);
        }

        public void Activate(string text, WitRequestOptions requestOptions)
        {
            service.Activate(text, requestOptions);
        }

        public void Activate()
        {
            service.Activate();
        }

        public void Activate(WitRequestOptions requestOptions)
        {
            service.Activate(requestOptions);
        }

        public void ActivateImmediately()
        {
            service.ActivateImmediately();
        }

        public void ActivateImmediately(WitRequestOptions requestOptions)
        {
            service.ActivateImmediately(requestOptions);
        }

        public void Deactivate()
        {
            service.Deactivate();
        }
    }
}
