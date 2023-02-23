using UnityEngine.Events;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Calls events for when the velocity of this objects breaks the begin and end threshold.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class OnVelocity : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The speed that will trigger the begin event.")]
        float m_BeginThreshold = 1.25f;

        [SerializeField]
        [Tooltip("The speed that will trigger the end event.")]
        float m_EndThreshold = 0.25f;

        [SerializeField]
        [Tooltip("Event that triggers when speed meets the begin threshold.")]
        UnityEvent m_OnBegin = new UnityEvent();

        [SerializeField]
        [Tooltip("Event that triggers when the speed dips below the end threshold.")]
        UnityEvent m_OnEnd = new UnityEvent();

        /// <summary>
        /// Event that triggers when speed meets the begin threshold.
        /// </summary>
        public UnityEvent onBegin => m_OnBegin;

        /// <summary>
        /// Event that triggers when the speed dips below the end threshold.
        /// </summary>
        public UnityEvent onEnd => m_OnEnd;

        Rigidbody m_RigidBody;
        bool m_HasBegun;

        void Awake()
        {
            m_RigidBody = GetComponent<Rigidbody>();
        }

        void Update()
        {
            CheckVelocity();
        }

        void CheckVelocity()
        {
            var speed = m_RigidBody.velocity.magnitude;
            m_HasBegun = HasVelocityBegun(speed);

            if (HasVelocityEnded(speed))
                m_HasBegun = false;
        }

        bool HasVelocityBegun(float speed)
        {
            if (m_HasBegun)
                return true;

            var beginCheck = speed > m_BeginThreshold;

            if (beginCheck)
                m_OnBegin.Invoke();

            return beginCheck;
        }

        bool HasVelocityEnded(float speed)
        {
            if (!m_HasBegun)
                return false;

            var endCheck = speed < m_EndThreshold;

            if (endCheck)
                m_OnEnd.Invoke();

            return endCheck;
        }
    }
}
