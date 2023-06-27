using System.Threading.Tasks;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace VRBuilder.Editor.TextToSpeech
{
    /// <summary>
    /// Generates TTS files for all processes before a build.
    /// </summary>
    public class TextToSpeechBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        /// <summary>
        /// Generates TTS files for all processes before a build.
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            TextToSpeechEditorUtils.GenerateTextToSpeechForAllProcesses();
        }
    }
}