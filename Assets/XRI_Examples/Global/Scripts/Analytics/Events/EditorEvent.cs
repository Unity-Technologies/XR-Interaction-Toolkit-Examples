using UnityEngine.Analytics;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if DEBUG_XRC_EDITOR_ANALYTICS
using UnityEngine;
#endif

namespace UnityEngine.XR.Content.Interaction.Analytics
{
    /// <summary>
    /// Base class for <c>XRContent</c> editor events.
    /// </summary>
    abstract class EditorEvent
    {
        protected const int k_DefaultMaxEventsPerHour = 1000;
        protected const int k_DefaultMaxElementCount = 1000;

        /// <summary>
        /// The event name determines which database table it goes into in the CDP backend.
        /// All events which we want grouped into a table must share the same event name.
        /// </summary>
        readonly string m_EventName;

        readonly int m_MaxEventsPerHour;
        readonly int m_MaxElementCount;

        internal EditorEvent(string eventName, int maxPerHour = k_DefaultMaxEventsPerHour, int maxElementCount = k_DefaultMaxElementCount)
        {
            m_EventName = eventName;
            m_MaxEventsPerHour = maxPerHour;
            m_MaxElementCount = maxElementCount;
        }

        /// <summary>
        /// Call this method in the child classes to send an event.
        /// </summary>
        /// <param name="parameter">The parameter object within the event.</param>
        /// <returns>Returns whenever the event was successfully sent.</returns>
        protected bool Send(object parameter)
        {
#if ENABLE_CLOUD_SERVICES_ANALYTICS
            // Analytics events will always refuse to send if analytics are disabled or the editor is for sure quitting
            if (XrcAnalytics.disabled || XrcAnalytics.quitting)
                return false;

#if UNITY_EDITOR
            var result = EditorAnalytics.SendEventWithLimit(m_EventName, parameter);
#else
            var result = AnalyticsResult.AnalyticsDisabled;
#endif

#if DEBUG_XRC_EDITOR_ANALYTICS
            Debug.Log($"Event {m_EventName} : {parameter} sent with status {result}");
#endif

            return result == AnalyticsResult.Ok;
#else // ENABLE_CLOUD_SERVICES_ANALYTICS
            return false;
#endif
        }

        internal bool Register()
        {
#if UNITY_EDITOR && ENABLE_CLOUD_SERVICES_ANALYTICS
            return EditorAnalytics.RegisterEventWithLimit(m_EventName, m_MaxEventsPerHour, m_MaxElementCount, XrcAnalytics.k_VendorKey) == AnalyticsResult.Ok;
#else
            return false;
#endif
        }
    }
}
