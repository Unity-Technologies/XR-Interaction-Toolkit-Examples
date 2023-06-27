using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.BuilderMenu
{
    internal static class ReviewMenuEntry
    {
        /// <summary>
        /// Redirects to the VR Builder asset store review page.
        /// </summary>
        [MenuItem("Tools/VR Builder/Leave a Review", false, 128)]
        private static void OpenReviewPage()
        {
            Application.OpenURL("https://assetstore.unity.com/packages/tools/visual-scripting/vr-builder-open-source-toolkit-for-vr-creation-201913#reviews");
        }
    }
}
