/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Meta.WitAi;
using Meta.WitAi.Configuration;
using Oculus.Voice.Dictation.Configuration;
using UnityEngine;

namespace Oculus.Voice.Dictation.Bindings.Android
{
    public class DictationConfigurationBinding
    {
        private readonly WitDictationRuntimeConfiguration _runtimeConfiguration;
        private readonly DictationConfiguration _dictationConfiguration;
        private readonly int MAX_PLATFORM_SUPPORTED_RECORDING_TIME_SECONDS = 300;

        public DictationConfigurationBinding(WitDictationRuntimeConfiguration runtimeConfiguration)
        {
            if (null == runtimeConfiguration)
            {
                // No config defined, use the default configuration.
                VLog.W("No dictation config has been defined. Using the default configuration.");
                _dictationConfiguration = new DictationConfiguration();
            }
            else
            {
                _dictationConfiguration = runtimeConfiguration.dictationConfiguration;
                _runtimeConfiguration = runtimeConfiguration;
            }
        }

        public AndroidJavaObject ToJavaObject()
        {
            AndroidJavaObject jo = new AndroidJavaObject("com.oculus.assistant.api.voicesdk.dictation.PlatformDictationConfiguration");
            jo.Set("multiPhrase", _dictationConfiguration.multiPhrase);
            jo.Set("scenario", _dictationConfiguration.scenario);
            jo.Set("inputType", _dictationConfiguration.inputType);
            if (_runtimeConfiguration != null)
            {
                int maxRecordingTime = (int) _runtimeConfiguration.maxRecordingTime;
                if (maxRecordingTime < 0)
                {
                    maxRecordingTime = MAX_PLATFORM_SUPPORTED_RECORDING_TIME_SECONDS;
                }
                jo.Set("interactionTimeoutSeconds", maxRecordingTime);
            }

            return jo;
        }
    }
}
