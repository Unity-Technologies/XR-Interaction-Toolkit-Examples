using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.BuilderMenu
{
    internal static class RoadmapMenuEntry
    {
        /// <summary>
        /// Redirects to the VR Builder asset store review page.
        /// </summary>
        [MenuItem("Tools/VR Builder/Roadmap", false, 128)]
        private static void OpenRoadmapPage()
        {
            Application.OpenURL("https://www.mindport.co/vr-builder/roadmap");
        }
    }
}
