/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Meta.WitAi.Configuration;
using Meta.WitAi;
using Meta.WitAi.Data.Info;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.WitAi.Data.Configuration
{
    public class WitConfiguration : ScriptableObject, IWitRequestConfiguration
    {
        /// <summary>
        /// Access token used in builds to make requests for data from Wit.ai
        /// </summary>
        [Tooltip("Access token used in builds to make requests for data from Wit.ai")]
        [FormerlySerializedAs("clientAccessToken")]
        [SerializeField] private string _clientAccessToken;

        /// <summary>
        /// Application info
        /// </summary>
        [FormerlySerializedAs("application")]
        [SerializeField] private WitAppInfo _appInfo;

        /// <summary>
        /// Configuration id
        /// </summary>
        [FormerlySerializedAs("configId")]
        [HideInInspector] [SerializeField] private string _configurationId;

        [Tooltip("The number of milliseconds to wait before requests to Wit.ai will timeout")]
        [SerializeField] public int timeoutMS = 10000;

        /// <summary>
        /// Configuration parameters to set up a custom endpoint for testing purposes and request forwarding. The default values here will work for most.
        /// </summary>
        [Tooltip("Configuration parameters to set up a custom endpoint for testing purposes and request forwarding. The default values here will work for most.")]
        [SerializeField] public WitEndpointConfig endpointConfiguration = new WitEndpointConfig();

        /// <summary>
        /// True if this configuration should not show up in the demo list
        /// </summary>
        [SerializeField] public bool isDemoOnly;

        /// <summary>
        /// When set to true, will use Conduit to dispatch voice commands.
        /// </summary>
        [Tooltip("Conduit enables manifest-based dispatching to invoke callbacks with native types directly without requiring manual parsing.")]
        [SerializeField] public bool useConduit = true;

        /// <summary>
        /// The path to the Conduit manifest.
        /// </summary>
        [SerializeField] private string _manifestLocalPath;

        /// <summary>
        /// The assemblies that we want to exclude from Conduit.
        /// </summary>
        [SerializeField] public List<string> excludedAssemblies = new List<string>
        {
            "Oculus.Voice.Demo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Meta.WitAi.Samples, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"
        };

        [Tooltip("When true, Conduit will attempt to match incoming requests by type when no exact matches are found. This increases tolerance but reduces runtime performance.")]
        [SerializeField] public bool relaxedResolution;

        /// <summary>
        /// Safe access of local path
        /// </summary>
        public string ManifestLocalPath
        {
            get
            {
                #if UNITY_EDITOR
                if (string.IsNullOrEmpty(_manifestLocalPath))
                {
                    _manifestLocalPath = $"ConduitManifest-{Guid.NewGuid()}.json";
                    SaveConfiguration();
                }
                #endif
                return _manifestLocalPath;
            }
        }
        #if UNITY_EDITOR
        /// <summary>
        /// Returns manifest full editor path
        /// </summary>
        public string GetManifestEditorPath()
        {
            if (string.IsNullOrEmpty(_manifestLocalPath)) return string.Empty;

            string lookup = Path.GetFileNameWithoutExtension(_manifestLocalPath);
            string[] guids = UnityEditor.AssetDatabase.FindAssets(lookup);
            if (guids != null && guids.Length > 0)
            {
                return UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            }
            return string.Empty;
        }
        #endif

        /// <summary>
        /// Reset all data
        /// </summary>
        public void ResetData()
        {
            _configurationId = null;
            _appInfo = new WitAppInfo();
            endpointConfiguration = new WitEndpointConfig();
        }

        // Logger invalid warnings
        private const string INVALID_APP_ID_NO_CLIENT_TOKEN = "App Info Not Set - No Client Token";
        private const string INVALID_APP_ID_WITH_CLIENT_TOKEN =
            "App Info Not Set - Has Client Token";
        public string GetLoggerAppId()
        {
            // Get application id
            string applicationId = GetApplicationId();
            if (String.IsNullOrEmpty(applicationId))
            {
                // NOTE: If a dev only provides a client token we may not have the application id.
                string clientAccessToken = GetClientAccessToken();
                if (!string.IsNullOrEmpty(clientAccessToken))
                {
                    return INVALID_APP_ID_WITH_CLIENT_TOKEN;
                }
                return INVALID_APP_ID_NO_CLIENT_TOKEN;
            }
            return applicationId;
        }

        #region IWitRequestConfiguration
        /// <summary>
        /// Returns unique configuration guid
        /// </summary>
        public string GetConfigurationId()
        {
            #if UNITY_EDITOR
            // Ensure configuration id is generated
            if (string.IsNullOrEmpty(_configurationId))
            {
                _configurationId = Guid.NewGuid().ToString();
            }
            #endif
            // Return configuration id
            return _configurationId;
        }
        /// <summary>
        /// Returns unique application id
        /// </summary>
        public string GetApplicationId() => _appInfo.id;
        /// <summary>
        /// Returns application info
        /// </summary>
        public WitAppInfo GetApplicationInfo() => _appInfo;
        /// <summary>
        /// Return endpoint override
        /// </summary>
        public IWitRequestEndpointInfo GetEndpointInfo() => endpointConfiguration;
        /// <summary>
        /// Returns client access token
        /// </summary>
        public string GetClientAccessToken()
        {
            return _clientAccessToken;
        }
        #if UNITY_EDITOR
        /// <summary>
        /// Editor only setter
        /// </summary>
        public void SetClientAccessToken(string newToken)
        {
            _clientAccessToken = newToken;
            SaveConfiguration();
        }
        /// <summary>
        /// Returns server access token (Editor Only)
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public string GetServerAccessToken()
        {
            return WitAuthUtility.GetAppServerToken(GetApplicationId());
        }
        /// <summary>
        /// Set application info
        /// </summary>
        public void SetApplicationInfo(WitAppInfo newInfo)
        {
            _appInfo = newInfo;
            SaveConfiguration();
        }
        // Save this configuration asset
        private void SaveConfiguration()
        {
            EditorUtility.SetDirty(this);
            #if UNITY_2021_3_OR_NEWER
            AssetDatabase.SaveAssetIfDirty(this);
            #else
            AssetDatabase.SaveAssets();
            #endif
        }
        #endif
        #endregion
    }
}
