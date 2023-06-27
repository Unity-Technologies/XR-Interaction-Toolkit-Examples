/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Text;
using Meta.WitAi;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Voice
{
    /// <summary>
    /// Abstract class for all voice requests
    /// </summary>
    /// <typeparam name="TRequest">The type of request to be returned in event callbacks</typeparam>
    /// <typeparam name="TOptions">The type containing all specific options to be passed to the end service.</typeparam>
    /// <typeparam name="TEvents">The type containing all events of TSession to be called throughout the lifecycle of the request.</typeparam>
    /// <typeparam name="TResults">The type containing all data that can be returned from the end service.</typeparam>
    public abstract class VoiceRequest<TUnityEvent, TOptions, TEvents, TResults>
        : IVoiceRequest<TUnityEvent, TOptions, TEvents, TResults>
        where TUnityEvent : UnityEventBase
        where TOptions : IVoiceRequestOptions
        where TEvents : VoiceRequestEvents<TUnityEvent>
        where TResults : IVoiceRequestResults
    {
        /// <summary>
        /// The states of a voice request
        /// </summary>
        public VoiceRequestState State { get; private set; } = (VoiceRequestState) (-1);
        /// <summary>
        /// Active if not currently canceled, failed or successful
        /// </summary>
        public bool IsActive => State == VoiceRequestState.Initialized || State == VoiceRequestState.Transmitting;
        /// <summary>
        /// Download progress of the current request transmission
        /// if available
        /// </summary>
        public float DownloadProgress { get; private set; } = 0f;
        /// <summary>
        /// Upload progress of the current request transmission
        /// if available
        /// </summary>
        public float UploadProgress { get; private set; } = 0f;

        /// <summary>
        /// Options sent as the request parameters
        /// </summary>
        public TOptions Options { get; private set; }
        /// <summary>
        /// Events specific to this voice request
        /// </summary>
        public TEvents Events { get; private set; }
        /// <summary>
        /// Results returned from the request
        /// </summary>
        public TResults Results { get; protected set; }

        /// <summary>
        /// Whether request can currently transmit data
        /// </summary>
        public bool CanSend => string.IsNullOrEmpty(GetSendError());

        #region INITIALIZATION
        /// <summary>
        /// Constructor class for voice requests
        /// </summary>
        /// <param name="newOptions">The request parameters to be used</param>
        /// <param name="newEvents">The request events to be called throughout it's lifecycle</param>
        public VoiceRequest(TOptions newOptions, TEvents newEvents)
        {
            // Apply options if they exist, otherwise generate
            Options = newOptions != null ? newOptions : Activator.CreateInstance<TOptions>();
            // Apply events if they exist, otherwise generate
            Events = newEvents != null ? newEvents : Activator.CreateInstance<TEvents>();

            // Initialized
            SetState(VoiceRequestState.Initialized);
        }
        /// <summary>
        /// Call after initialization
        /// </summary>
        protected virtual void OnInit()
        {
            RaiseEvent(Events?.OnInit);
            SetUploadProgress(0f);
            SetDownloadProgress(0f);
        }
        /// <summary>
        /// Apply the voice request state
        /// </summary>
        protected virtual void SetState(VoiceRequestState newState)
        {
            // Ignore same state
            if (State == newState)
            {
                return;
            }

            // Set state & update event
            State = newState;
            RaiseEvent(Events?.OnStateChange);

            // Perform state specific event
            switch (State)
            {
                case VoiceRequestState.Initialized:
                    OnInit();
                    break;
                case VoiceRequestState.Transmitting:
                    OnSend();
                    HandleSend();
                    break;
                case VoiceRequestState.Canceled:
                    HandleCancel();
                    OnCancel();
                    OnComplete();
                    break;
                case VoiceRequestState.Failed:
                    OnFailed();
                    OnComplete();
                    break;
                case VoiceRequestState.Successful:
                    OnSuccess();
                    OnComplete();
                    break;
            }
        }
        /// <summary>
        /// Set current request download progress
        /// </summary>
        /// <param name="newProgress">New progress value</param>
        protected void SetDownloadProgress(float newProgress)
        {
            // Ignore same progress
            if (DownloadProgress.Equals(newProgress))
            {
                return;
            }

            // Set progress & update event
            DownloadProgress = newProgress;
            RaiseEvent(Events?.OnDownloadProgressChange);
        }
        /// <summary>
        /// Set current request upload progress
        /// </summary>
        /// <param name="newProgress">New progress value</param>
        protected void SetUploadProgress(float newProgress)
        {
            // Ignore same progress
            if (UploadProgress.Equals(newProgress))
            {
                return;
            }

            // Set progress & update event
            UploadProgress = newProgress;
            RaiseEvent(Events?.OnUploadProgressChange);
        }
        /// <summary>
        /// Raises a voice request event
        /// </summary>
        /// <param name="requestEvent">Event to be performed</param>
        protected abstract void RaiseEvent(TUnityEvent requestEvent);

        /// <summary>
        /// Internal method for
        /// </summary>
        protected void Log(string log, bool warning = false)
        {
            // Start log
            StringBuilder requestLog = new StringBuilder();
            // Append type of request
            requestLog.Append($"{GetType().Name} ");
            // Append sent log
            requestLog.AppendLine(log);
            // Append any request specific data
            AppendLogData(requestLog, warning);

            // Log warning
            if (warning)
            {
                VLog.W(requestLog);
            }
            // Log debug
            else
            {
                VLog.D(requestLog);
            }
        }
        protected void LogW(string log) => Log(log, true);

        /// <summary>
        /// Append request specific data to log
        /// </summary>
        /// <param name="log">Building log</param>
        /// <param name="warning">True if this is a warning log</param>
        protected virtual void AppendLogData(StringBuilder log, bool warning)
        {
            #if UNITY_EDITOR
            // Append request id
            log.AppendLine($"Request Id: {Options?.RequestId}");
            #endif
            // Append request state
            log.AppendLine($"Request State: {State}");
        }
        #endregion INITIALIZATION

        #region TRANSMISSION
        /// <summary>
        /// Internal way to determine send error
        /// </summary>
        protected virtual string GetSendError()
        {
            // Cannot send if not initialized
            if (State != VoiceRequestState.Initialized)
            {
                return $"Cannot send request in '{State}' state.";
            }
            // Cannot send without valid request id
            if (string.IsNullOrEmpty(Options?.RequestId))
            {
                return $"Cannot send request without a request id.";
            }
            // Send allowed
            return string.Empty;
        }
        /// <summary>
        /// Public request to transmit data
        /// </summary>
        public virtual void Send()
        {
            // Warn & ignore
            if (State != VoiceRequestState.Initialized)
            {
                LogW($"Request Send Ignored\nReason: Invalid state");
                return;
            }

            // Fail if cannot send
            string sendError = GetSendError();
            if (!string.IsNullOrEmpty(sendError))
            {
                HandleFailure(sendError);
                return;
            }

            // Set to transmitting state
            SetState(VoiceRequestState.Transmitting);
        }

        /// <summary>
        /// Call after transmission begins
        /// </summary>
        protected virtual void OnSend()
        {
            // Call send event
            Log($"Request Transmitting");
            RaiseEvent(Events?.OnSend);
        }

        /// <summary>
        /// Child class send implementation
        /// Call HandleFailure, HandleCancel from this class
        /// </summary>
        protected abstract void HandleSend();
        #endregion TRANSMISSION

        #region RESULTS
        /// <summary>
        /// Returns an empty result object with a specific message
        /// </summary>
        /// <param name="newMessage">The message to be set on the results</param>
        protected abstract TResults GetResultsWithMessage(string newMessage);
        /// <summary>
        /// Method for handling failure with only an error string
        /// </summary>
        /// <param name="error">The error to be returned</param>
        protected virtual void HandleFailure(string error) => HandleFailure(GetResultsWithMessage(error));
        /// <summary>
        /// Method for handling failure with a full result object
        /// </summary>
        /// <param name="error">The error to be returned</param>
        protected virtual void HandleFailure(TResults results)
        {
            // Ignore if not in correct state
            if (State != VoiceRequestState.Initialized && State != VoiceRequestState.Transmitting)
            {
                LogW($"Request Failure Ignored\nReason: Request is already complete");
                return;
            }

            // Apply results with error
            Results = results;

            // Set failure state
            SetState(VoiceRequestState.Failed);
        }
        /// <summary>
        /// Call after failure state set
        /// </summary>
        protected virtual void OnFailed()
        {
            LogW($"Request Failed\nError: {Results?.Message}");
            RaiseEvent(Events?.OnFailed);
        }

        /// <summary>
        /// Method for handling success with a full result object
        /// </summary>
        /// <param name="error">The error to be returned</param>
        protected virtual void HandleSuccess(TResults results)
        {
            // Ignore if not in correct state
            if (State != VoiceRequestState.Initialized && State != VoiceRequestState.Transmitting)
            {
                LogW($"Request Success Ignored\nReason: Request is already complete");
                return;
            }

            // Generate results if needed
            if (results == null)
            {
                results = Activator.CreateInstance<TResults>();
            }
            // Apply results
            Results = results;

            // Set success state
            SetState(VoiceRequestState.Successful);
        }
        /// <summary>
        /// Call after success state set
        /// </summary>
        protected virtual void OnSuccess()
        {
            Log($"Request Success\nResults: {Results != null}");
            RaiseEvent(Events?.OnSuccess);
        }

        /// <summary>
        /// Cancel the request immediately
        /// </summary>
        public virtual void Cancel(string reason = WitConstants.CANCEL_MESSAGE_DEFAULT)
        {
            // Ignore if cannot cancel
            if (State != VoiceRequestState.Initialized && State != VoiceRequestState.Transmitting)
            {
                LogW($"Request Cancel Ignored\nReason: Request is already complete");
                return;
            }

            // Set cancellation reason
            Results = GetResultsWithMessage(reason);

            // Set cancellation state
            SetState(VoiceRequestState.Canceled);
        }

        /// <summary>
        /// Handle cancelation in subclass
        /// </summary>
        protected abstract void HandleCancel();

        /// <summary>
        /// Call after cancellation state set
        /// </summary>
        protected virtual void OnCancel()
        {
            // Log & callbacks
            Log($"Request Cancelled\nReason: {Results?.Message}");
            RaiseEvent(Events?.OnCancel);
        }

        /// <summary>
        /// Call after failure, success or cancellation
        /// </summary>
        protected virtual void OnComplete()
        {
            RaiseEvent(Events?.OnComplete);
        }
        #endregion RESULTS
    }
}
