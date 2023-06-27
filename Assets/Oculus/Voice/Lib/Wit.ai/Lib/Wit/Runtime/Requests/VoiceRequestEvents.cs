/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Voice
{
    [Serializable]
    public class VoiceRequestEvents<TUnityEvent>
        : IVoiceRequestEvents<TUnityEvent>
        where TUnityEvent : UnityEventBase
    {
        /// <summary>
        /// Called whenever a request state changes
        /// </summary>
        public TUnityEvent OnStateChange => _onStateChange;
        [Header("State Events")] [Tooltip("Called whenever a request state changes.")]
        [SerializeField] private TUnityEvent _onStateChange = Activator.CreateInstance<TUnityEvent>();

        /// <summary>
        /// Called on initial request generation
        /// </summary>
        public TUnityEvent OnInit => _onInit;
        [Tooltip("Called on initial request generation.")]
        [SerializeField] private TUnityEvent _onInit = Activator.CreateInstance<TUnityEvent>();
        /// <summary>
        /// Called following the start of data transmission
        /// </summary>
        public TUnityEvent OnSend => _onSend;
        [Tooltip("Called following the start of data transmission.")]
        [SerializeField] private TUnityEvent _onSend = Activator.CreateInstance<TUnityEvent>();
        /// <summary>
        /// Called following the cancellation of a request
        /// </summary>
        public TUnityEvent OnCancel => _onCancel;
        [Tooltip("Called following the cancellation of a request.")]
        [SerializeField] private TUnityEvent _onCancel = Activator.CreateInstance<TUnityEvent>();
        /// <summary>
        /// Called following an error response from a request
        /// </summary>
        public TUnityEvent OnFailed => _onFailed;
        [Tooltip("Called following an error response from a request.")]
        [SerializeField] private TUnityEvent _onFailed = Activator.CreateInstance<TUnityEvent>();
        /// <summary>
        /// Called following a successful request & data parse with results provided
        /// </summary>
        public TUnityEvent OnSuccess => _onSuccess;
        [Tooltip("Called following a successful request & data parse with results provided.")]
        [SerializeField] private TUnityEvent _onSuccess = Activator.CreateInstance<TUnityEvent>();
        /// <summary>
        /// Called following cancellation, failure or success to finalize request.
        /// </summary>
        public TUnityEvent OnComplete => _onComplete;
        [Tooltip("Called following cancellation, failure or success to finalize request.")]
        [SerializeField] private TUnityEvent _onComplete = Activator.CreateInstance<TUnityEvent>();

        /// <summary>
        /// Called on download progress update
        /// </summary>
        public TUnityEvent OnDownloadProgressChange => _onDownloadProgressChange;
        [Header("Progress Events")] [Tooltip("Called on download progress update.")]
        [SerializeField] private TUnityEvent _onDownloadProgressChange = Activator.CreateInstance<TUnityEvent>();
        /// <summary>
        /// Called on upload progress update
        /// </summary>
        public TUnityEvent OnUploadProgressChange => _onUploadProgressChange;
        [Tooltip("Called on upload progress update.")]
        [SerializeField] private TUnityEvent _onUploadProgressChange = Activator.CreateInstance<TUnityEvent>();
    }
}
