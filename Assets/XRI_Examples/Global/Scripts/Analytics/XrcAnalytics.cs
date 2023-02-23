#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.XR.Content.Interaction.Analytics
{
    /// <summary>
    /// The entry point class to send XRContent analytics data.
    /// Stores all events usd by XRContent.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    static class XrcAnalytics
    {
        internal const string k_VendorKey = "unity.xrcontent.interaction";

        internal static bool quitting { get; private set; }
        internal static bool disabled { get; }

        internal static InteractionEvent interactionEvent { get; } = new InteractionEvent();

        static XrcAnalytics()
        {
            // if the user has analytics disabled, respect that and make sure that no code actually tries to send events
            if (!interactionEvent.Register())
            {
                disabled = true;
                return;
            }

#if UNITY_EDITOR
            EditorApplication.quitting += SetQuitting;
#endif
        }

        static void SetQuitting()
        {
            // we set the Quitting variable so that we don't record window close events when the editor quits
            quitting = true;
        }
    }
}
