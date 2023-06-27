using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.BuilderMenu
{
    internal static class AddonsMenuEntry
    {
        /// <summary>
        /// Allows to open the URL to webinar.
        /// </summary>
        [MenuItem("Tools/VR Builder/Add-ons and Integrations", false, 128)]
        private static void OpenAddonsPage()
        {
            Application.OpenURL("https://www.mindport.co/vr-builder-add-ons-and-integrations");
        }
    }
}
