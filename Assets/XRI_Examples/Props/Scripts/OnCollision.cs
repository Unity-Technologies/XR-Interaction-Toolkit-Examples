using System;
using UnityEngine.Events;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Calls functionality when a physics collision occurs
    /// </summary>
    public class OnCollision : MonoBehaviour
    {
        [Serializable] public class CollisionEvent : UnityEvent<Collision> { }

        [SerializeField]
        [Tooltip("If set, this collision will only fire if the other gameobject has this tag.")]
        string m_RequiredTag = string.Empty;

        [SerializeField]
        [Tooltip("Events to fire when a matcing object collides with this one.")]
        CollisionEvent m_OnEnter = new CollisionEvent();

        [SerializeField]
        [Tooltip("Events to fire when a matching object stops colliding with this one.")]
        CollisionEvent m_OnExit = new CollisionEvent();

        /// <summary>
        /// If set, this collision will only fire if the other gameobject has this tag.
        /// </summary>
        public string requiredTag => m_RequiredTag;

        /// <summary>
        /// Events to fire when a matching object collides with this one.
        /// </summary>
        public CollisionEvent onEnter => m_OnEnter;

        /// <summary>
        /// Events to fire when a matching object stops colliding with this one.
        /// </summary>
        public CollisionEvent onExit => m_OnExit;

        void OnCollisionEnter(Collision collision)
        {
            if (CanInvoke(collision.gameObject))
                m_OnEnter?.Invoke(collision);
        }

        void OnCollisionExit(Collision collision)
        {
            if (CanInvoke(collision.gameObject))
                m_OnExit?.Invoke(collision);
        }

        bool CanInvoke(GameObject otherGameObject)
        {
            return string.IsNullOrEmpty(m_RequiredTag) || otherGameObject.CompareTag(m_RequiredTag);
        }
    }
}
