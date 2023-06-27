/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;
using UnityEngine.Serialization;
using Meta.WitAi.Data.Configuration;

namespace Meta.WitAi.Configuration
{
    [Serializable]
    public class WitEndpointConfig : IWitRequestEndpointInfo
    {
        /// <summary>
        /// Customized uri scheme (Ex. https)
        /// </summary>
        [SerializeField] [FormerlySerializedAs("uriScheme")]
        private string _uriScheme;
        public string UriScheme => string.IsNullOrEmpty(_uriScheme) ? WitConstants.URI_SCHEME : _uriScheme;

        /// <summary>
        /// Customized host location (Ex. api.wit.ai)
        /// </summary>
        [SerializeField] [FormerlySerializedAs("authority")]
        private string _authority;
        public string Authority =>
            string.IsNullOrEmpty(_authority) ? WitConstants.URI_AUTHORITY : _authority;

        /// <summary>
        /// Customized host port (Ex. api.wit.ai)
        /// </summary>
        [SerializeField] [FormerlySerializedAs("port")]
        private int _port;
        public int Port => _port <= 0 ? WitConstants.URI_DEFAULT_PORT : _port;

        /// <summary>
        /// API version to be used for this endpoint. Defaults to sdk default version
        /// </summary>
        [SerializeField] [FormerlySerializedAs("witApiVersion")]
        private string _witApiVersion;
        public string WitApiVersion => string.IsNullOrEmpty(_witApiVersion)
            ? WitConstants.API_VERSION
            : _witApiVersion;

        /// <summary>
        /// Endpoint used for text based voice command.  Defaults to 'message'
        /// </summary>
        [SerializeField] [FormerlySerializedAs("message")]
        private string _message;
        public string Message =>
            string.IsNullOrEmpty(_message) ? WitConstants.ENDPOINT_MESSAGE : _message;

        /// <summary>
        /// Endpoint used for audio based voice command.  Defaults to 'speech'
        /// </summary>
        [SerializeField] [FormerlySerializedAs("speech")]
        private string _speech;
        public string Speech =>
            string.IsNullOrEmpty(_speech) ? WitConstants.ENDPOINT_SPEECH : _speech;

        /// <summary>
        /// Endpoint used for audio based transcription.  Defaults to 'dictation'
        /// </summary>
        [SerializeField] [FormerlySerializedAs("dictation")]
        private string _dictation;
        public string Dictation => string.IsNullOrEmpty(_dictation) ? WitConstants.ENDPOINT_DICTATION : _dictation;

        /// <summary>
        /// Endpoint used for Text-To-Speech.  Defaults to 'synthesize'
        /// </summary>
        [SerializeField]
        private string _synthesize;
        public string Synthesize => string.IsNullOrEmpty(_synthesize) ? WitConstants.ENDPOINT_TTS : _synthesize;

        /// <summary>
        /// Endpoint used for Composer text requests.  Defaults to 'event'
        /// </summary>
        [SerializeField]
        private string _event;
        public string Event => string.IsNullOrEmpty(_event) ? WitConstants.ENDPOINT_COMPOSER_MESSAGE : _event;

        /// <summary>
        /// Endpoint used for Composer audio requests.  Defaults to 'converse'
        /// </summary>
        [SerializeField]
        private string _converse;
        public string Converse => string.IsNullOrEmpty(_converse) ? WitConstants.ENDPOINT_COMPOSER_SPEECH : _converse;

        // Default endpoint data
        private static WitEndpointConfig defaultEndpointConfig = new WitEndpointConfig();

        /// <summary>
        /// Generates a configuration using a preset if possible
        /// </summary>
        public static WitEndpointConfig GetEndpointConfig(WitConfiguration witConfig)
        {
            return witConfig && null != witConfig.endpointConfiguration
                ? witConfig.endpointConfiguration
                : defaultEndpointConfig;
        }
    }
}
