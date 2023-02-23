namespace UnityEngine.XR.Content.Interaction.Analytics
{
    /// <summary>
    /// Class that manages scene analytics events for <c>XRContent</c>.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    class XrcSceneAnalytics : MonoBehaviour
    {
        void Awake()
        {
            XrcAnalytics.interactionEvent.Send(new SceneStarted());
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            gameObject.hideFlags = UnityEditor.Unsupported.IsDeveloperMode() ? HideFlags.None : HideFlags.HideInHierarchy | HideFlags.NotEditable;
        }

#endif
    }
}
