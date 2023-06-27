/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Meta.WitAi.Requests
{
    public class WitVRequest : VRequest
    {
        /// <summary>
        /// Uri customization delegate
        /// </summary>
        public static event Func<UriBuilder, UriBuilder> OnProvideCustomUri;
        /// <summary>
        /// Header customization delegate
        /// </summary>
        public static event Action<Dictionary<string, string>> OnProvideCustomHeaders;
        /// <summary>
        /// User agent customization delegate
        /// </summary>
        public static event Action<StringBuilder> OnProvideCustomUserAgent;

        /// <summary>
        /// The unique identifier used by Wit to track requests
        /// </summary>
        public string RequestId { get; private set; }

        /// <summary>
        /// The configuration used for voice requests
        /// </summary>
        public IWitRequestConfiguration Configuration { get; private set; }

        // Whether or not the configuration's server token should be used
        private bool _useServerToken;

        /// <summary>
        /// Constructor that takes in a configuration interface
        /// </summary>
        /// <param name="configuration">The configuration interface to be used</param>
        /// <param name="requestId">A unique identifier that can be used to track the request</param>
        /// <param name="useServerToken">Editor only option to use server token instead of client token</param>
        public WitVRequest(IWitRequestConfiguration configuration, string requestId, bool useServerToken = false)
        {
            Configuration = configuration;
            RequestId = requestId;
            if (string.IsNullOrEmpty(RequestId))
            {
                RequestId = Guid.NewGuid().ToString();
            }
            _useServerToken = useServerToken;
        }

        // Return uri
        public Uri GetUri(string path, Dictionary<string, string> queryParams = null)
        {
            return GetWitUri(Configuration, path, queryParams);
        }

        // Gets wit headers using static header generation
        protected override Dictionary<string, string> GetHeaders()
        {
            return GetWitHeaders(Configuration, RequestId, _useServerToken);
        }

        #region REQUESTS
        /// <summary>
        /// Perform a generic request
        /// </summary>
        /// <param name="unityRequest">The unity request</param>
        /// <param name="onProgress"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public override bool Request(UnityWebRequest unityRequest,
            RequestCompleteDelegate<UnityWebRequest> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            // Ensure configuration is set
            if (Configuration == null)
            {
                onComplete?.Invoke(unityRequest, "Cannot perform a request without a Wit configuration");
                return false;
            }

            // Perform base
            return base.Request(unityRequest, onComplete, onProgress);
        }

        /// <summary>
        /// Get request to a wit endpoint
        /// </summary>
        /// <param name="uriEndpoint">Endpoint name</param>
        /// <param name="uriParams">Endpoint url parameters</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The download progress</param>
        /// <returns>False if the request cannot be performed</returns>
        public bool RequestWitGet<TData>(string uriEndpoint,
            Dictionary<string, string> uriParams,
            RequestCompleteDelegate<TData> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            return RequestJsonGet(GetUri(uriEndpoint, uriParams), onComplete, onProgress);
        }

        /// <summary>
        /// Post text request to a wit endpoint
        /// </summary>
        /// <param name="uriEndpoint">Endpoint name</param>
        /// <param name="uriParams">Endpoint url parameters</param>
        /// <param name="postText">Text to be sent to endpoint</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The upload progress</param>
        /// <returns>False if the request cannot be performed</returns>
        public bool RequestWitPost<TData>(string uriEndpoint,
            Dictionary<string, string> uriParams, string postText,
            RequestCompleteDelegate<TData> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            return RequestJsonPost(GetUri(uriEndpoint, uriParams), postText, onComplete, onProgress);
        }

        /// <summary>
        /// Put text request to a wit endpoint
        /// </summary>
        /// <param name="uriEndpoint">Endpoint name</param>
        /// <param name="uriParams">Endpoint url parameters</param>
        /// <param name="putText">Text to be sent to endpoint</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The upload progress</param>
        /// <returns>False if the request cannot be performed</returns>
        public bool RequestWitPut<TData>(string uriEndpoint,
            Dictionary<string, string> uriParams, string putText,
            RequestCompleteDelegate<TData> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            return RequestJsonPut(GetUri(uriEndpoint, uriParams), putText, onComplete, onProgress);
        }
        #endregion

        #region STATIC
        /// <summary>
        /// Get custom wit uri using a specific path & query parameters
        /// </summary>
        public static Uri GetWitUri(IWitRequestConfiguration configuration, string path, Dictionary<string, string> queryParams = null)
        {
            // Uri builder
            UriBuilder uriBuilder = new UriBuilder();

            // Append endpoint data
            IWitRequestEndpointInfo endpoint = configuration.GetEndpointInfo();
            uriBuilder.Scheme = endpoint.UriScheme;
            uriBuilder.Host = endpoint.Authority;
            uriBuilder.Port = endpoint.Port;

            // Set path
            uriBuilder.Path = path;

            // Build query
            string apiVersion = endpoint.WitApiVersion;
            uriBuilder.Query = $"v={apiVersion}";
            if (queryParams != null)
            {
                foreach (string key in queryParams.Keys)
                {
                    uriBuilder.Query += $"&{key}={queryParams[key]}";
                }
            }

            // Return custom uri
            if (OnProvideCustomUri != null)
            {
                foreach (Func<UriBuilder, UriBuilder> del in OnProvideCustomUri.GetInvocationList())
                {
                    uriBuilder = del(uriBuilder);
                }
            }

            // Return uri
            return uriBuilder.Uri;
        }
        /// <summary>
        /// Obtain headers to be used with every wit service
        /// </summary>
        public static Dictionary<string, string> GetWitHeaders(IWitRequestConfiguration configuration, string requestId, bool useServerToken)
        {
            // Get headers
            Dictionary<string, string> headers = new Dictionary<string, string>();

            // Set request id
            headers[WitConstants.HEADER_REQUEST_ID] = string.IsNullOrEmpty(requestId) ? Guid.NewGuid().ToString() : requestId;
            // Set User-Agent
            headers[WitConstants.HEADER_USERAGENT] = GetUserAgentHeader(configuration);
            // Set authorization
            headers[WitConstants.HEADER_AUTH] = GetAuthorizationHeader(configuration, useServerToken);
            // Allow overrides
            if (OnProvideCustomHeaders != null)
            {
                // Allow overrides
                foreach (Action<Dictionary<string, string>> del in OnProvideCustomHeaders.GetInvocationList())
                {
                    del(headers);
                }
            }

            // Return results
            return headers;
        }
        /// <summary>
        /// Obtain authorization header using provided access token
        /// </summary>
        private static string GetAuthorizationHeader(IWitRequestConfiguration configuration, bool useServerToken)
        {
            // Default to client access token
            string token = configuration.GetClientAccessToken();
            // Use server token
            if (useServerToken)
            {
                #if UNITY_EDITOR
                token = configuration.GetServerAccessToken();
                #else
                token = string.Empty;
                #endif
            }
            // Trim token
            if (!string.IsNullOrEmpty(token))
            {
                token = token.Trim();
            }
            // Use invalid token
            else
            {
                token = "XXX";
            }
            // Return with bearer
            return $"Bearer {token}";
        }
        // Build and return user agent header
        private static string _operatingSystem;
        private static string _deviceModel;
        private static string _appIdentifier;
        private static string _unityVersion;
        private static string GetUserAgentHeader(IWitRequestConfiguration configuration)
        {
            // Generate user agent
            StringBuilder userAgent = new StringBuilder();

            // Append prefix if any exists
            userAgent.Append(WitConstants.HEADER_USERAGENT_PREFIX);

            // Append wit sdk version
            userAgent.Append($"wit-unity-{WitConstants.SDK_VERSION}");

            // Append operating system
            if (_operatingSystem == null) _operatingSystem = UnityEngine.SystemInfo.operatingSystem;
            userAgent.Append($",\"{_operatingSystem}\"");
            // Append device model
            if (_deviceModel == null) _deviceModel = UnityEngine.SystemInfo.deviceModel;
            userAgent.Append($",\"{_deviceModel}\"");

            // Append configuration log id
            string logId = configuration.GetConfigurationId();
            if (string.IsNullOrEmpty(logId))
            {
                logId = WitConstants.HEADER_USERAGENT_CONFID_MISSING;
            }
            userAgent.Append($",{logId}");

            // Append app identifier
            if (_appIdentifier == null) _appIdentifier = Application.identifier;
            userAgent.Append($",{_appIdentifier}");

            // Append editor identifier
            #if UNITY_EDITOR
            userAgent.Append(",Editor");
            #else
            userAgent.Append(",Runtime");
            #endif

            // Append unity version
            if (_unityVersion == null) _unityVersion = Application.unityVersion;
            userAgent.Append($",{_unityVersion}");

            // Set custom user agent
            if (OnProvideCustomUserAgent != null)
            {
                foreach (Action<StringBuilder> del in OnProvideCustomUserAgent.GetInvocationList())
                {
                    del(userAgent);
                }
            }

            // Return user agent
            return userAgent.ToString();
        }
        #endregion
    }
}
