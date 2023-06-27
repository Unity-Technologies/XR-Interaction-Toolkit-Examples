/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.WitAi.Windows
{
    /// <summary>
    /// This class provides a common API for accessing properties and methods of the various *Service classes
    /// used in the Understanding Viewer.  In order to add support for a new Service type, derive a child
    /// class from this one and provide implementations for the getter methods.
    /// </summary>
    public abstract class WitUnderstandingViewerServiceAPI
    {
        /// <summary>
        /// The base Component/Service class this object wraps.
        /// Child classes will provide a properly-cast reference to the component in their implementation.
        /// </summary>
        private MonoBehaviour _serviceComponent;

        /// <summary>
        /// The name of the service.  Gotten once and cached for future use.
        /// Child classes may override the base implementation if necessary.
        /// </summary>
        private String _serviceName;

        /// <summary>
        /// Flags to indicate whether or not the component supports voice and/or text activation.
        /// </summary>
        protected bool _hasVoiceActivation;
        protected bool _hasTextActivation;

        /// <summary>
        /// This flag dictates whether or not the service should send a network request as part of its
        /// (de)activation handling.
        /// </summary>
        protected bool _shouldSubmitUtterance;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="serviceComponent">The Service Component (VoiceService, DictationService, etc.
        /// this API will wrap.</param>
        public WitUnderstandingViewerServiceAPI(MonoBehaviour serviceComponent)
        {
            _serviceComponent = serviceComponent;
        }

        public MonoBehaviour ServiceComponent
        {
            get => _serviceComponent;
        }

        public bool HasVoiceActivation
        {
            get => _hasVoiceActivation;
        }

        public bool HasTextActivation
        {
            get => _hasTextActivation;
        }

        public bool ShouldSubmitUtterance
        {
            get => _shouldSubmitUtterance;
        }

        /// <summary>
        /// Most services have a common way of querying their service name.  For those that don't,
        /// override this method.  The name is cached after first query.
        /// </summary>
        public virtual string ServiceName {
            get
            {
                // Has the service name been cached to the local variable yet?
                if (_serviceName == null)
                {
                    if (_serviceComponent == null)
                        return "";

                    var configProvider = _serviceComponent.GetComponent<IWitRuntimeConfigProvider>();

                    if (configProvider != null)
                    {
                        // If no Witconfig is set for the component we get a NullPointerException here
                        if (configProvider.RuntimeConfiguration.witConfiguration == null)
                        {
                            VLog.E($"No Wit configuration found for {_serviceComponent.gameObject.name}");

                            return "";
                        }

                        _serviceName =
                            $"{configProvider.RuntimeConfiguration.witConfiguration.name} [{_serviceComponent.gameObject.name}]";
                    }
                    else
                    {
                        _serviceName = _serviceComponent.name;
                    }
                }

                return _serviceName;
            }
        }

        public abstract bool Active { get; }

        public abstract bool MicActive { get; }

        public abstract bool IsRequestActive { get; }

        // API methods - override these to provide functionality
        public abstract void Activate();

        public abstract void Activate(string text);

        public abstract void Deactivate();

        public abstract void DeactivateAndAbortRequest();

        // Event Callback Registration
        public abstract VoiceServiceRequestEvent OnSend { get; }
        public abstract WitTranscriptionEvent OnPartialTranscription { get; }
        public abstract WitTranscriptionEvent OnFullTranscription { get; }
        public abstract UnityEvent OnStoppedListening { get; }
        public abstract VoiceServiceRequestEvent OnComplete { get; }
    }
}
