/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Configuration;
using Meta.WitAi.Dictation.Events;
using Meta.WitAi.Events;
using Meta.WitAi.Events.UnityEventListeners;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Requests;
using UnityEngine;

namespace Meta.WitAi.Dictation
{
    public abstract class DictationService : MonoBehaviour, IDictationService, IAudioEventProvider, ITranscriptionEventProvider
    {
        [Tooltip("Events that will fire before, during and after an activation")]
        [SerializeField] protected DictationEvents dictationEvents = new DictationEvents();

        ///<summary>
        /// Internal events used to report telemetry. These events are reserved for internal
        /// use only and should not be used for any other purpose.
        /// </summary>
        protected TelemetryEvents telemetryEvents = new TelemetryEvents();

        /// <summary>
        /// Returns true if this voice service is currently active and listening with the mic
        /// </summary>
        public abstract bool Active { get; }

        /// <summary>
        /// Returns true if the service is actively communicating with Wit.ai during an Activation. The mic may or may not still be active while this is true.
        /// </summary>
        public abstract bool IsRequestActive { get; }

        /// <summary>
        /// Gets/Sets a custom transcription provider. This can be used to replace any built in asr
        /// with an on device model or other provided source
        /// </summary>
        public abstract ITranscriptionProvider TranscriptionProvider { get; set; }

        /// <summary>
        /// Returns true if this voice service is currently reading data from the microphone
        /// </summary>
        public abstract bool MicActive { get; }

        public virtual DictationEvents DictationEvents
        {
            get => dictationEvents;
            set => dictationEvents = value;
        }

        public TelemetryEvents TelemetryEvents { get => telemetryEvents; set => telemetryEvents = value; }

        /// <summary>
        /// A subset of events around collection of audio data
        /// </summary>
        public IAudioInputEvents AudioEvents => DictationEvents;

        /// <summary>
        /// A subset of events around receiving transcriptions
        /// </summary>
        public ITranscriptionEvent TranscriptionEvents => DictationEvents;

        /// <summary>
        /// Returns true if the audio input should be read in an activation
        /// </summary>
        protected abstract bool ShouldSendMicData { get; }


        /// <summary>
        /// Activate the microphone and send data for NLU processing. Includes optional additional request parameters like dynamic entities and maximum results.
        /// </summary>
        public void Activate() => Activate(new WitRequestOptions(), new VoiceServiceRequestEvents());
        /// <summary>
        /// Activate the microphone and send data for NLU processing. Includes optional additional request parameters like dynamic entities and maximum results.
        /// </summary>
        /// <param name="requestOptions">Additional options such as custom request id</param>
        public void Activate(WitRequestOptions requestOptions) => Activate(requestOptions, new VoiceServiceRequestEvents());
        /// <summary>
        /// Activate the microphone and send data for NLU processing. Includes optional additional request parameters like dynamic entities and maximum results.
        /// </summary>
        /// <param name="requestOptions">Additional options such as custom request id</param>
        public VoiceServiceRequest Activate(VoiceServiceRequestEvents requestEvents) => Activate(new WitRequestOptions(), requestEvents);
        /// <summary>
        /// Activate the microphone and send data for NLU processing. Includes optional additional request parameters like dynamic entities and maximum results.
        /// </summary>
        /// <param name="requestOptions">Additional options such as custom request id</param>
        /// <param name="requestEvents">Events specific to the request's lifecycle</param>
        public abstract VoiceServiceRequest Activate(WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents);

        /// <summary>
        /// Activate the microphone and send data for NLU processing immediately without waiting for sound/speech from the user to begin.  Includes optional additional request parameters like dynamic entities and maximum results.
        /// </summary>
        public void ActivateImmediately() => ActivateImmediately(new WitRequestOptions(), new VoiceServiceRequestEvents());
        /// <summary>
        /// Activate the microphone and send data for NLU processing immediately without waiting for sound/speech from the user to begin.  Includes optional additional request parameters like dynamic entities and maximum results.
        /// </summary>
        /// <param name="requestOptions">Additional options such as custom request id</param>
        public void ActivateImmediately(WitRequestOptions requestOptions) => ActivateImmediately(requestOptions, new VoiceServiceRequestEvents());
        /// <summary>
        /// Activate the microphone and send data for NLU processing immediately without waiting for sound/speech from the user to begin.  Includes optional additional request parameters like dynamic entities and maximum results.
        /// </summary>
        /// <param name="requestOptions">Additional options such as custom request id</param>
        public VoiceServiceRequest ActivateImmediately(VoiceServiceRequestEvents requestEvents) => ActivateImmediately(new WitRequestOptions(), requestEvents);
        /// <summary>
        /// Activate the microphone and send data for NLU processing immediately without waiting for sound/speech from the user to begin.  Includes optional additional request parameters like dynamic entities and maximum results.
        /// </summary>
        /// <param name="requestOptions">Additional options such as custom request id</param>
        /// <param name="requestEvents">Events specific to the request's lifecycle</param>
        public abstract VoiceServiceRequest ActivateImmediately(WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents);

        /// <summary>
        /// Stop listening and submit any remaining buffered microphone data for processing.
        /// </summary>
        public abstract void Deactivate();

        /// <summary>
        /// Cancels the current transcription. No FullTranscription event will fire.
        /// </summary>
        public abstract void Cancel();

        protected virtual void Awake()
        {
            var audioEventListener = GetComponent<AudioEventListener>();
            if (!audioEventListener)
            {
                gameObject.AddComponent<AudioEventListener>();
            }

            var transcriptionEventListener = GetComponent<TranscriptionEventListener>();
            if (!transcriptionEventListener)
            {
                gameObject.AddComponent<TranscriptionEventListener>();
            }
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }
    }

    public interface IDictationService: ITelemetryEventsProvider
    {
        bool Active { get; }

        bool IsRequestActive { get; }

        bool MicActive { get; }

        ITranscriptionProvider TranscriptionProvider { get; set; }

        DictationEvents DictationEvents { get; set; }

        new TelemetryEvents TelemetryEvents { get; set; }

        VoiceServiceRequest Activate(WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents);

        VoiceServiceRequest ActivateImmediately(WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents);

        void Deactivate();

        void Cancel();
    }
}
