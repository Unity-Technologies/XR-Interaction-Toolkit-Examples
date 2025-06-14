// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Utilities.Narrative
{
    /// <summary>
    /// Proxy for receiving events from EventBroadcaster, allowing events to transcend event references which are more difficult to maintain across separate prefabs
    /// </summary>
    public class EventReceiver : MonoBehaviour
    {
        [SerializeField, Dropdown(typeof(EventBroadcaster), "m_id")] private string m_id;
        [SerializeField] private UnityEvent m_event;

        private void OnEnable()
        {
            if (!EventBroadcaster.Events.ContainsKey(m_id))
            {
                EventBroadcaster.Events.Add(m_id, null);
            }
            EventBroadcaster.Events[m_id] += m_event.Invoke;
        }
        private void OnDisable()
        {
            if (EventBroadcaster.Events.TryGetValue(m_id, out var action))
            {
                action -= m_event.Invoke;
            }
        }

        [ContextMenu("Debug: Force this event receiver to fire its event")]
        public void ForceEventReceiverToFire()
        {
            m_event.Invoke();
        }
    }
}
