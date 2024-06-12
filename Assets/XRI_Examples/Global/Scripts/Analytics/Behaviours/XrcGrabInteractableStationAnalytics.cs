using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UnityEngine.XR.Content.Interaction.Analytics
{
    /// <summary>
    /// Class that connects the Grab Interactable station scene objects with their respective analytics events.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    class XrcGrabInteractableStationAnalytics : MonoBehaviour
    {
        const float k_FrequencyToSendWateringPlant = 4f;

        static readonly WateringPlant k_WateringPlantParameter = new WateringPlant();
        static readonly BreakPiggyBank k_BreakPiggyBankParameter = new BreakPiggyBank();

        [Header("Simple Object Substation")]
        [SerializeField]
        XRBaseInteractable[] m_InstantInteractables;

        [SerializeField]
        XRBaseInteractable[] m_KinematicInteractables;

        [SerializeField]
        XRBaseInteractable[] m_VelocityInteractables;

        [Header("Watering Can Substation")]
        [SerializeField]
        XRBaseInteractable m_WateringCanInteractable;

        [SerializeField]
        OnTrigger m_OnPlantGrowsTrigger;

        [Header("Piggy Bank Substation")]
        [SerializeField]
        XRBaseInteractable m_MalletInteractable;

        [SerializeField]
        GameObject m_PigBank;

        [Header("Ribbon Stick Substation")]
        [SerializeField]
        XRBaseInteractable m_RibbonStickInteractable;

        float m_TimeToSendWateringPlant;

        void Awake()
        {
            XrcAnalyticsUtils.Register(m_InstantInteractables, new GrabSimpleObjectInstant());
            XrcAnalyticsUtils.Register(m_KinematicInteractables, new GrabSimpleObjectKinematic());
            XrcAnalyticsUtils.Register(m_VelocityInteractables, new GrabSimpleObjectVelocity());

            XrcAnalyticsUtils.Register(m_WateringCanInteractable, new GrabWateringCan());
            if (m_OnPlantGrowsTrigger != null)
                m_OnPlantGrowsTrigger.onEnter.AddListener(OnWateringPlant);

            XrcAnalyticsUtils.Register(m_MalletInteractable, new GrabMallet());
            OnRestorePiggyBank(m_PigBank);

            XrcAnalyticsUtils.Register(m_RibbonStickInteractable, new GrabRibbonStick());
        }

        void OnWateringPlant(GameObject otherGameObject)
        {
            if (Time.unscaledTime < m_TimeToSendWateringPlant)
                return;

            m_TimeToSendWateringPlant = Time.unscaledTime + k_FrequencyToSendWateringPlant;
            XrcAnalytics.interactionEvent.Send(k_WateringPlantParameter);
        }

        void OnRestorePiggyBank(GameObject piggyBank)
        {
            if (piggyBank == null)
                return;

            var breakable = piggyBank.GetComponent<Breakable>();
            if (breakable != null)
                breakable.onBreak.AddListener(OnBreakPiggyBank);
        }

        void OnBreakPiggyBank(GameObject otherGameObject, GameObject brokenGameObject)
        {
            XrcAnalytics.interactionEvent.Send(k_BreakPiggyBankParameter);

            if (brokenGameObject == null)
                return;

            var unbreakable = brokenGameObject.GetComponent<Unbreakable>();
            if (unbreakable != null)
                unbreakable.onRestore.AddListener(OnRestorePiggyBank);
        }
    }
}
