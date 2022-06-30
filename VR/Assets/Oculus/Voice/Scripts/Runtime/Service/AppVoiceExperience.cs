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
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Interfaces;
#if UNITY_ANDROID
using Oculus.Voice.Bindings.Android;
#endif
using Oculus.Voice.Interfaces;
using UnityEngine;

namespace Oculus.Voice
{
    [HelpURL("https://developer.oculus.com/experimental/voice-sdk/tutorial-overview/")]
    public class AppVoiceExperience : VoiceService, IWitRuntimeConfigProvider
    {
        [SerializeField] private WitRuntimeConfiguration witRuntimeConfiguration;

        public WitRuntimeConfiguration RuntimeConfiguration
        {
            get => witRuntimeConfiguration;
            set => witRuntimeConfiguration = value;
        }

        private IPlatformVoiceService platformService;
        private IVoiceService voiceServiceImpl;

        #region Voice Service Properties
        public override bool Active => null != voiceServiceImpl && voiceServiceImpl.Active;
        public override bool IsRequestActive => null != voiceServiceImpl && voiceServiceImpl.IsRequestActive;
        public override ITranscriptionProvider TranscriptionProvider
        {
            get => voiceServiceImpl.TranscriptionProvider;
            set => voiceServiceImpl.TranscriptionProvider = value;

        }
        public override bool MicActive => null != voiceServiceImpl && voiceServiceImpl.MicActive;
        public override bool ShouldSendMicData => witRuntimeConfiguration.sendAudioToWit ||
                                                  null == TranscriptionProvider;
        #endregion

        public bool HasPlatformIntegrations => false;

        #region Voice Service Methods

        public override void Activate()
        {
            voiceServiceImpl.Activate();
        }

        public override void Activate(WitRequestOptions options)
        {
            voiceServiceImpl.Activate(options);
        }

        public override void ActivateImmediately()
        {
            voiceServiceImpl.ActivateImmediately();
        }

        public override void ActivateImmediately(WitRequestOptions options)
        {
            voiceServiceImpl.ActivateImmediately(options);
        }

        public override void Deactivate()
        {
            voiceServiceImpl.Deactivate();
        }

        public override void Activate(string text)
        {
            voiceServiceImpl.Activate(text);
        }

        public override void Activate(string text, WitRequestOptions requestOptions)
        {
            voiceServiceImpl.Activate(text, requestOptions);
        }

        #endregion

        void Start()
        {
            InitVoiceSDK();
        }

        private void InitVoiceSDK()
        {

#if UNITY_ANDROID && !UNITY_EDITOR
            if (HasPlatformIntegrations)
            {
                IPlatformVoiceService platformImpl = new VoiceSDKImpl();
                if (platformImpl.PlatformSupportsWit)
                {
                    voiceServiceImpl = platformImpl;
                }
                else
                {
                    RevertToWitUnity();
                }
            }
            else
            {
                RevertToWitUnity();
            }
#else
            RevertToWitUnity();
#endif

            if (voiceServiceImpl is Wit wit)
            {
                wit.RuntimeConfiguration = witRuntimeConfiguration;
            }

            voiceServiceImpl.VoiceEvents = VoiceEvents;
        }

        private void RevertToWitUnity()
        {
            voiceServiceImpl = GetComponent<Wit>();
            if (null == voiceServiceImpl)
            {
                voiceServiceImpl = gameObject.AddComponent<Wit>();
            }
        }

        private void OnEnable()
        {
            if(null == voiceServiceImpl) InitVoiceSDK();

            #if UNITY_ANDROID && !UNITY_EDITOR
            platformService?.SetRuntimeConfiguration(witRuntimeConfiguration);
            #endif
        }
    }
}
