using System;
using UnityEngine;

namespace Oculus.Voice.Dictation.Configuration
{
    [Serializable]
    public class DictationConfiguration
    {
        [Tooltip("Re-open the mic after the final transcription. Useful for long form content/messaging.")]
        public bool multiPhrase;
        [Tooltip("Hint about the scenario that the user is dictating. Default to package name. In the future we might have messaging, search, general, etc")]
        public string scenario = "default";
        [Tooltip("Input types: text_default: Normal text, numeric: Numbers, email: Email addresses")]
        public string inputType = "text_default";
    }
}
