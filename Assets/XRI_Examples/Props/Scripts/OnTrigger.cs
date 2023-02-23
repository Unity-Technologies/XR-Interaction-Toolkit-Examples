using System;
using UnityEngine.Events;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Calls functionality when a physics trigger occurs
    /// </summary>
    public class OnTrigger : MonoBehaviour
    {
        [Serializable] public class TriggerEvent : UnityEvent<GameObject> { }

        [SerializeField]
        [Tooltip("If set, this trigger will only fire if the other gameobject has this tag.")]
        string m_RequiredTag = string.Empty;

        [SerializeField]
        [Tooltip("Events to fire when a matcing object collides with this trigger.")]
        TriggerEvent m_OnEnter = new TriggerEvent();

        [SerializeField]
        [Tooltip("Events to fire when a matching object stops colliding with this trigger.")]
        TriggerEvent m_OnExit = new TriggerEvent();

        /// <summary>
        /// If set, this trigger will only fire if the other gameobject has this tag.
        /// </summary>
        public string requiredTag => m_RequiredTag;

        /// <summary>
        /// Events to fire when a matching object collides with this trigger.
        /// </summary>
        public TriggerEvent onEnter => m_OnEnter;

        /// <summary>
        /// Events to fire when a matching object stops colliding with this trigger.
        /// </summary>
        public TriggerEvent onExit => m_OnExit;

        void OnTriggerEnter(Collider other)
        {
            if (CanTrigger(other.gameObject))
                m_OnEnter?.Invoke(other.gameObject);
        }

        void OnTriggerExit(Collider other)
        {
            if (CanTrigger(other.gameObject))
                m_OnExit?.Invoke(other.gameObject);
        }

        void OnParticleCollision(GameObject other)
        {
            if (CanTrigger(other.gameObject))
                m_OnEnter?.Invoke(other);
        }

        bool CanTrigger(GameObject otherGameObject)
        {
            if (m_RequiredTag != string.Empty)
                return otherGameObject.CompareTag(m_RequiredTag);
            else
                return true;
        }
    }
}
