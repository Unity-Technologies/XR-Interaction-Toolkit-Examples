/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Data.Info;

namespace Meta.WitAi
{
    /// <summary>
    /// Endpoint overrides
    /// </summary>
    public interface IWitRequestEndpointInfo
    {
        // Setup
        string UriScheme { get; }
        string Authority { get; }
        int Port { get; }
        string WitApiVersion { get; }

        // Voice Command Endpoints
        string Message { get; }
        string Speech { get; }
        // Dictation Endpoint
        string Dictation { get; }
        // TTS Endpoint
        string Synthesize { get; }
        // Composer Endpoints
        string Event { get; }
        string Converse { get; }
    }

    /// <summary>
    /// Configuration interface
    /// </summary>
    public interface IWitRequestConfiguration
    {
        string GetConfigurationId();
        string GetApplicationId();
        WitAppInfo GetApplicationInfo();
        IWitRequestEndpointInfo GetEndpointInfo();
        string GetClientAccessToken();
#if UNITY_EDITOR
        void SetClientAccessToken(string newToken);
        string GetServerAccessToken();
        void SetApplicationInfo(WitAppInfo appInfo);
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// A simple configuration for initial setup
    /// </summary>
    public class WitServerRequestConfiguration : IWitRequestConfiguration, IWitRequestEndpointInfo
    {
        private string _clientToken;
        private string _serverToken;

        public WitServerRequestConfiguration(string serverToken)
        {
            _serverToken = serverToken;
        }

        public string GetConfigurationId() => null;
        public string GetApplicationId() => null;
        public WitAppInfo GetApplicationInfo() => new WitAppInfo();

        public void SetApplicationInfo(WitAppInfo newInfo)
        {
        }

        public string GetClientAccessToken() => _clientToken;
        public void SetClientAccessToken(string newToken) => _clientToken = newToken;
        public string GetServerAccessToken() => _serverToken;

        // Endpoint info
        public IWitRequestEndpointInfo GetEndpointInfo() => this;
        public string UriScheme => WitConstants.URI_SCHEME;
        public string Authority => WitConstants.URI_AUTHORITY;
        public string WitApiVersion => WitConstants.API_VERSION;
        public int Port => WitConstants.URI_DEFAULT_PORT;
        public string Message => WitConstants.ENDPOINT_MESSAGE;
        public string Speech => WitConstants.ENDPOINT_SPEECH;
        public string Dictation => WitConstants.ENDPOINT_DICTATION;
        public string Synthesize => WitConstants.ENDPOINT_TTS;
        public string Event => WitConstants.ENDPOINT_COMPOSER_MESSAGE;
        public string Converse => WitConstants.ENDPOINT_COMPOSER_SPEECH;
    }
#endif
}
