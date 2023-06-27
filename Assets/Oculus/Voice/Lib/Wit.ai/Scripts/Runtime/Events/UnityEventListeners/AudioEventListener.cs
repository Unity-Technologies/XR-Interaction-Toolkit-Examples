using Meta.WitAi.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.WitAi.Events.UnityEventListeners
{
    [RequireComponent(typeof(IAudioEventProvider))]
    public class AudioEventListener : MonoBehaviour, IAudioInputEvents
    {
        [SerializeField] private WitMicLevelChangedEvent onMicAudioLevelChanged = new WitMicLevelChangedEvent();
        [SerializeField] private UnityEvent onMicStartedListening = new UnityEvent();
        [SerializeField] private UnityEvent onMicStoppedListening = new UnityEvent();

        public WitMicLevelChangedEvent OnMicAudioLevelChanged => onMicAudioLevelChanged;
        public UnityEvent OnMicStartedListening => onMicStartedListening;
        public UnityEvent OnMicStoppedListening => onMicStoppedListening;

        private IAudioInputEvents _events;

        private IAudioInputEvents AudioInputEvents
        {
            get
            {
                if (null == _events)
                {
                    var eventProvider = GetComponent<IAudioEventProvider>();
                    if (null != eventProvider)
                    {
                        _events = eventProvider.AudioEvents;
                    }
                }

                return _events;
            }
        }

        private void OnEnable()
        {
            var events = AudioInputEvents;
            if (null != events)
            {
                events.OnMicAudioLevelChanged.AddListener(onMicAudioLevelChanged.Invoke);
                events.OnMicStartedListening.AddListener(onMicStartedListening.Invoke);
                events.OnMicStoppedListening.AddListener(onMicStoppedListening.Invoke);
            }
        }

        private void OnDisable()
        {
            var events = AudioInputEvents;
            if (null != events)
            {
                events.OnMicAudioLevelChanged.RemoveListener(onMicAudioLevelChanged.Invoke);
                events.OnMicStartedListening.RemoveListener(onMicStartedListening.Invoke);
                events.OnMicStoppedListening.RemoveListener(onMicStoppedListening.Invoke);
            }
        }
    }
}
