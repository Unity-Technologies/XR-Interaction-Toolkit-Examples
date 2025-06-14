// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.Debug;


namespace Meta.Utilities.Narrative
{
    /// <summary>
    /// Serialized reflection wrapper for event connections used with the narrative task system
    /// </summary>
    [Serializable]
    public class EventLink : ISerializationCallbackReceiver
    {
        public enum Kind { Event, Delegate, UnityEvent }

        [SerializeField] private Component m_targetObject;
        [SerializeField] private string m_fieldName;

        [SerializeField, HideInInspector] private Kind m_kind;
        [SerializeField, HideInInspector] private bool m_isSubscribed;

        private Action m_callback;

        public Component TargetObject => m_targetObject;
        public string FieldName => m_fieldName;

        public void Subscribe(Action action)
        {
            if (m_isSubscribed) { Unsubscribe(); }

            m_callback = action;

            if (!m_targetObject)
            {
                LogError("No target object for event link; cannot subscribe");
                return;
            }

            if (string.IsNullOrWhiteSpace(m_fieldName))
            {
                LogError("No event member specified in event link; cannot subscribe");
                return;
            }

            if (m_callback == null)
            {
                LogError("Null callback action specified; cannot subscribe");
                return;
            }

            var type = m_targetObject.GetType();

            switch (m_kind)
            {
                case Kind.Event: { SubscribeToEvent(type); } break;
                case Kind.Delegate: { SubscribeToDelegate(type); } break;
                case Kind.UnityEvent: { SubscribeToUnityEvent(type); } break;
                default: { throw new ArgumentOutOfRangeException(); }
            }
        }

        private void SubscribeToEvent(Type type)
        {
            type.GetRuntimeEvent(m_fieldName).AddEventHandler(m_targetObject, m_callback);

            m_isSubscribed = true;

            if (TaskManager.DebugLogs)
            {
                Log($"EventLink subscribed to event '{type.Name}.{m_fieldName}' "
                    + $"on '{m_targetObject.name}'");
            }
        }

        private void SubscribeToDelegate(Type type)
        {
            var field = GetField(type);

            if (field == null)
            {
                LogError($"Member '{m_fieldName}' of type '{type.Name}' is null; cannot subscribe");
                return;
            }

            if (!typeof(Action).IsAssignableFrom(field.FieldType))
            {
                LogError($"Member '{m_fieldName}' of type '{type.Name}' is not a void delegate; "
                         + "cannot subscribe");
                return;
            }

            field.SetValue(m_targetObject,
                           (field.GetValue(m_targetObject) as Action) + EventHandler);

            m_isSubscribed = true;

            if (TaskManager.DebugLogs)
            {
                Log($"EventLink subscribed to delegate '{type.Name}.{field.Name}' "
                    + $"on '{m_targetObject.name}'");
            }
        }

        private void SubscribeToUnityEvent(Type type)
        {
            var field = GetField(type);

            if (!typeof(UnityEvent).IsAssignableFrom(field.FieldType))
            {
                LogError($"Member '{m_fieldName}' of type '{type.Name}' is not a UnityEvent; "
                         + "cannot subscribe");
                return;
            }

            (field.GetValue(m_targetObject) as UnityEvent)?.AddListener(EventHandler);

            if (TaskManager.DebugLogs)
            {
                Log($"EventLink subscribed to UnityEvent '{type.Name}.{m_fieldName}' "
                    + $"on '{m_targetObject.name}'");
            }

            m_isSubscribed = true;
        }

        public void Unsubscribe()
        {
            if (!m_targetObject)
            {
                if (TaskManager.DebugLogs)
                {
                    LogWarning("EventLink cannot unsubscribe - there is no target object set");
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(m_fieldName))
            {
                if (TaskManager.DebugLogs)
                {
                    LogWarning("EventLink cannot unsubscribe because there is no handler set");
                }

                return;
            }

            var type = m_targetObject.GetType();

            switch (m_kind)
            {
                case Kind.Event: { UnsubscribeFromEvent(type); } break;
                case Kind.Delegate: { UnsubscribeFromDelegate(type); } break;
                case Kind.UnityEvent: { UnsubscribeFromUnityEvent(type); } break;
                default: { throw new ArgumentOutOfRangeException(); }
            }
        }

        private void UnsubscribeFromEvent(Type type)
        {
            type.GetRuntimeEvent(m_fieldName).RemoveEventHandler(m_targetObject, m_callback);

            m_isSubscribed = false;

            if (TaskManager.DebugLogs)
            {
                Log($"EventLink unsubscribed from event '{type.Name}.{m_fieldName}' "
                    + $"on '{m_targetObject.name}'");
            }
        }


        private void UnsubscribeFromDelegate(Type type)
        {
            var field = GetField(type);

            if (field == null)
            {
                LogError($"Member '{m_fieldName}' of type '{type.Name}' is null; "
                         + "cannot unsubscribe");
                return;
            }

            if (!typeof(Action).IsAssignableFrom(field.FieldType))
            {
                LogError($"Member '{m_fieldName}' of type '{type.Name}' is not a void delegate; "
                         + "cannot unsubscribe");
                return;
            }

            field.SetValue(m_targetObject,
                           (field.GetValue(m_targetObject) as Action) - EventHandler);

            m_isSubscribed = false;

            if (TaskManager.DebugLogs)
            {
                Log($"EventLink unsubscribed from delegate '{type.Name}.{field.Name}' "
                    + $"on '{m_targetObject.name}'");
            }
        }

        private void UnsubscribeFromUnityEvent(Type type)
        {
            var field = GetField(type);

            if (field == null || !typeof(UnityEvent).IsAssignableFrom(field.FieldType))
            {
                LogError($"Member '{m_fieldName}' of type '{type.Name}' is not a UnityEvent; cannot unsubscribe {m_targetObject?.name}", m_targetObject);
                return;
            }

            (field.GetValue(m_targetObject) as UnityEvent)?.RemoveListener(EventHandler);

            m_isSubscribed = false;

            if (TaskManager.DebugLogs)
            {
                Log($"EventLink unsubscribed from UnityEvent '{type.Name}.{field.Name}' on '{m_targetObject.name}'");
            }
        }

        private FieldInfo GetField(Type type) => type.GetAllFields().FirstOrDefault(f => f.Name == m_fieldName);
        private void EventHandler() => m_callback?.Invoke();

        public void OnValidate()
        {
#if UNITY_EDITOR
            var type = m_targetObject?.GetType();
            if (type != null && m_kind is Kind.UnityEvent or Kind.Delegate)
            {
                var field = GetField(type);
                if (field == null)
                {
                    field = type.GetAllFields().FirstOrDefault(f => f.GetCustomAttributes<UnityEngine.Serialization.FormerlySerializedAsAttribute>().Any(a => a.oldName == m_fieldName));
                    if (field != null)
                    {
                        m_fieldName = field.Name;
                    }
                    else
                    {
                        LogError($"Couldn't find {type.FullName}.{m_fieldName}");
                    }
                }
            }
#endif
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() => OnValidate();
        void ISerializationCallbackReceiver.OnAfterDeserialize() => OnValidate();
    }
}