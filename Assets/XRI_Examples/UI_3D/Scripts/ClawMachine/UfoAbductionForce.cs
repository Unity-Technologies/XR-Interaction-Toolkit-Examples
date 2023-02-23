using System.Collections.Generic;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// This class applies an abduction force in all rigidbodies inside the trigger collider.
    /// A list is used to store all rigidbodies inside the trigger; the force is applied in
    /// the FixedUpdate. The force magnitude is calculated using the pressure value of the <c>XRPushButton</c>
    /// <seealso cref="XRPushButton"/>
    /// </summary>
    public class UfoAbductionForce : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The minimum magnitude of the abduction force")]
        float m_MinForceMagnitude;

        [SerializeField]
        [Tooltip("The maximum magnitude of the abduction force")]
        float m_MaxForceMagnitude;

        [SerializeField]
        [Tooltip("All rigidbodies inside this trigger will receive the abduction force.")]
        Collider m_Trigger;

        float m_ButtonValue;
        readonly List<Rigidbody> m_Rigidbodies = new List<Rigidbody>();

        void Awake()
        {
            m_Trigger.enabled = false;
        }

        void OnEnable()
        {
            m_Trigger.enabled = true;
        }

        void OnDisable()
        {
            m_Trigger.enabled = false;
            m_Rigidbodies.Clear();
        }

        void FixedUpdate()
        {
            var deltaForce = m_MaxForceMagnitude - m_MinForceMagnitude;
            var force = transform.up * (m_MinForceMagnitude + deltaForce * m_ButtonValue);
            foreach (var rigidbody in m_Rigidbodies)
                rigidbody.AddForce(force, ForceMode.Acceleration);
        }

        void OnTriggerEnter(Collider other)
        {
            var otherRigidbody = other.GetComponent<Rigidbody>();
            if (otherRigidbody != null)
                m_Rigidbodies.Add(otherRigidbody);
        }

        void OnTriggerExit(Collider other)
        {
            var otherRigidbody = other.GetComponent<Rigidbody>();
            if (otherRigidbody != null)
                m_Rigidbodies.Remove(otherRigidbody);
        }

        /// <summary>
        /// Gets the current button value. Called by the <c>XRPushButton.OnValueChange</c> event.
        /// </summary>
        /// <param name="value">The current button value</param>
        public void OnButtonValueChange(float value)
        {
            m_ButtonValue = value;
        }
    }
}
