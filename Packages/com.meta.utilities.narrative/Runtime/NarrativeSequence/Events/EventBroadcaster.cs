// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Meta.Utilities.Narrative
{
    /// <summary>
    /// Proxy for broadcasting events for EventReciever, allowing events to transcend event references which are more difficult to maintain across separate prefabs
    /// </summary>

    public class EventBroadcaster : MonoBehaviour
    {
        [SerializeField] private string m_id;
        [SerializeField] private EventLink m_link;

        public static Dictionary<string, Action> Events = new();

        private void Awake()
        {
            m_link.Subscribe(Trigger);
        }

        private void OnDestroy()
        {
            m_link.Unsubscribe();
        }

        [ContextMenu("Trigger this event broadcast")]
        public void Trigger()
        {
            if (Events.TryGetValue(m_id, out var action))
                action?.Invoke();
        }
    }
}
