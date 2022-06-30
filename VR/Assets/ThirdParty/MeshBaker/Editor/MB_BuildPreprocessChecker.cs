using UnityEditor;
using UnityEngine;
using DigitalOpus.MB.Core;

#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DigitalOpus.MB.MBEditor
{
    class MB_BuildPreprocessChecker : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            string msg = MB3_TextureBakerEditorInternal.ValidatePlayerSettingsDefineSymbols();
            if (msg != null) Debug.LogError(msg);
        }
    }
}
#endif


