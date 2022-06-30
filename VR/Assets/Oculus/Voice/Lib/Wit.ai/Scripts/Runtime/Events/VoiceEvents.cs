/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Facebook.WitAi.Events
{
    [Serializable]
    public class VoiceEvents
    {
        [Header("Activation Result Events")]
        [Tooltip("Called when a response from wit has been received")]
        public WitResponseEvent OnResponse = new WitResponseEvent();

        [Tooltip("Called when there was an error with a WitRequest")]
        public WitErrorEvent OnError = new WitErrorEvent();

        [Header("Mic Events")]
        [Tooltip("Called when the volume level of the mic input has changed")]
        public WitMicLevelChangedEvent OnMicLevelChanged = new WitMicLevelChangedEvent();

        /// <summary>
        /// Called when a request is created. This happens at the beginning of
        /// an activation before the microphone is activated (if in use).
        /// </summary>
        [Header("Activation/Deactivation Events")]
        [Tooltip(
            "Called when a request is created. This happens at the beginning of an activation before the microphone is activated (if in use)")]
        public WitRequestCreatedEvent OnRequestCreated = new WitRequestCreatedEvent();

        [Tooltip("Called when the microphone has been activated during a Wit voice command activation")]
        public UnityEvent OnStartListening = new UnityEvent();

        [Tooltip(
            "Called when the microphone has stopped recording during a Wit voice command activation")]
        public UnityEvent OnStoppedListening = new UnityEvent();

        [Tooltip(
            "Called when the microphone input volume has been below the volume threshold for the specified duration.")]
        public UnityEvent OnStoppedListeningDueToInactivity = new UnityEvent();

        [Tooltip(
            "The microphone has stopped recording because maximum recording time has been hit")]
        public UnityEvent OnStoppedListeningDueToTimeout = new UnityEvent();

        [Tooltip("The microphone was stopped from manual deactivation")]
        public UnityEvent OnStoppedListeningDueToDeactivation = new UnityEvent();

        [Tooltip("Fired when recording stops, the minimum volume threshold was hit, and data is being sent to the server.")]
        public UnityEvent OnMicDataSent = new UnityEvent();

        [Tooltip("Fired when the minimum wake threshold is hit after an activation")]
        public UnityEvent OnMinimumWakeThresholdHit = new UnityEvent();

        [Header("Transcription Events")]
        [Tooltip("Message fired when a partial transcription has been received.")]
        public WitTranscriptionEvent OnPartialTranscription = new WitTranscriptionEvent();

        [Tooltip("Message received when a complete transcription is received.")]
        public WitTranscriptionEvent OnFullTranscription = new WitTranscriptionEvent();
    }
 }
