using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UnityEngine.XR.Content.Interaction.Analytics
{
    /// <summary>
    /// Class that connects the Active Interactable station scene objects with their respective analytics events.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    class XrcActiveInteractableStationAnalytics : MonoBehaviour
    {
        [Header("Active SimpleObject Substation")]
        [SerializeField]
        XRBaseInteractable[] m_SimpleActiveInteractables;

        [Header("Candle Substation")]
        [SerializeField]
        XRBaseInteractable m_LighterInteractable;

        [SerializeField]
        XRBaseInteractable[] m_CandleInteractables;

        [SerializeField]
        OnTrigger[] m_CandleTriggers;

        [Header("Launcher Substation")]
        [SerializeField]
        XRBaseInteractable m_LauncherInteractable;

        [SerializeField]
        OnTrigger m_EasyRingTrigger;

        [SerializeField]
        OnTrigger m_MediumRingTrigger;

        [SerializeField]
        OnTrigger m_HardRingTrigger;

        [Header("Megaphone Substation")]
        [SerializeField]
        XRBaseInteractable m_MegaphoneInteractable;

        void Awake()
        {
            XrcAnalyticsUtils.Register(m_SimpleActiveInteractables, new GrabActiveSimpleObject(), new SimpleObjectActivated());

            XrcAnalyticsUtils.Register(m_LighterInteractable, new GrabLighter(), new LighterActivated());
            XrcAnalyticsUtils.Register(m_CandleInteractables, new GrabCandle());
            XrcAnalyticsUtils.Register(m_CandleTriggers, new LightCandle());

            XrcAnalyticsUtils.Register(m_LauncherInteractable, new GrabLauncher(), new LauncherActivated());
            XrcAnalyticsUtils.Register(m_EasyRingTrigger, new LauncherEasyTargetHit());
            XrcAnalyticsUtils.Register(m_MediumRingTrigger, new LauncherMediumTargetHit());
            XrcAnalyticsUtils.Register(m_HardRingTrigger, new LauncherHardTargetHit());

            XrcAnalyticsUtils.Register(m_MegaphoneInteractable, new GrabMegaphone(), new MegaphoneActivated());
        }
    }
}
