namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// This component is designed to easily toggle a specific component on or off when an object
    /// enters the specified <see cref="triggerVolume"/>.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ToggleComponentZone : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Main Trigger Volume to detect the Activation Object within. Must be on same physics layer as the Activation Object.")]
        Collider m_TriggerVolume;

        /// <summary>
        /// Main Trigger Volume to detect the Activation Object within.
        /// Must be on same physics layer as the Activation Object.
        /// </summary>
        public Collider triggerVolume
        {
            get => m_TriggerVolume;
            set => m_TriggerVolume = value;
        }

        [SerializeField]
        [Tooltip("Collider that will trigger the component to turn on or off when entering the Trigger Volume. Must have a Rigidbody component and be on the same physics layer as the Trigger Volume.")]
        Collider m_ActivationObject;

        /// <summary>
        /// Collider that will trigger the component to turn on or off when entering the Trigger Volume.
        /// Must have a Rigidbody component and be on the same physics layer as the Trigger Volume.
        /// </summary>
        public Collider activationObject
        {
            get => m_ActivationObject;
            set => m_ActivationObject = value;
        }

        [SerializeField]
        [Tooltip("Component to set the enabled state for. Will set the value to the Enable On Entry value upon entry and revert to original value on exit.")]
        Behaviour m_ComponentToToggle;

        /// <summary>
        /// Component to set the enabled state for. Will set the value to the
        /// Enable On Entry value upon entry and revert to original value on exit.
        /// </summary>
        public Behaviour componentToToggle
        {
            get => m_ComponentToToggle;
            set => m_ComponentToToggle = value;
        }

        [SerializeField]
        [Tooltip("Sets whether to enable or disable the Component To Toggle upon entry into the Trigger Volume.")]
        bool m_EnableOnEntry = true;

        /// <summary>
        /// Sets whether to enable or disable the Component To Toggle upon entry into the Trigger Volume.
        /// </summary>
        public bool enableOnEntry
        {
            get => m_EnableOnEntry;
            set => m_EnableOnEntry = value;
        }

        bool m_InitialStateOnEntry;

        void Start()
        {
            if (m_TriggerVolume == null && !TryGetComponent(out m_TriggerVolume))
            {
                enabled = false;
                return;
            }

            if (!m_TriggerVolume.isTrigger)
                m_TriggerVolume.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other != null && other == m_ActivationObject)
            {
                m_InitialStateOnEntry = m_ComponentToToggle.enabled;
                m_ComponentToToggle.enabled = m_EnableOnEntry;
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other != null && other == m_ActivationObject)
            {
                m_ComponentToToggle.enabled = m_InitialStateOnEntry;
            }
        }
    }
}
