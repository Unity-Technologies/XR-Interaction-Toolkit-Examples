/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Data.Configuration;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Facebook.WitAi
{
    public class WitAuthUtility
    {
        public static ITokenValidationProvider tokenValidator = new DefaultTokenValidatorProvider();

        public static bool IsServerTokenValid()
        {
            return tokenValidator.IsServerTokenValid(ServerToken);
        }

        public static bool IsServerTokenValid(string token)
        {
            return tokenValidator.IsServerTokenValid(token);
        }

        private static string serverToken;

        public static string GetAppServerToken(WitConfiguration configuration,
            string defaultValue = "")
        {
            return GetAppServerToken(configuration?.application?.id, defaultValue);
        }

        public static string GetAppServerToken(string appId, string defaultValue = "")
        {
#if UNITY_EDITOR
            return EditorPrefs.GetString("Wit::AppIdToToken::" + appId, defaultValue);
#else
        return "";
#endif
        }

        public static string GetAppId(string serverToken, string defaultValue = "")
        {
#if UNITY_EDITOR
            return EditorPrefs.GetString("Wit::TokenToAppId::" + serverToken, defaultValue);
#else
        return "";
#endif
        }

        public static void SetAppServerToken(string appId, string token)
        {
#if UNITY_EDITOR
            EditorPrefs.SetString("Wit::AppIdToToken::" + appId, token);
            EditorPrefs.SetString("Wit::TokenToAppId::" + token, appId);
#endif
        }

        private static void SavePrefs()
        {

        }

        public static string ServerToken
        {
#if UNITY_EDITOR
            get
            {
                if (null == serverToken)
                {
                    try
                    {
                        serverToken = EditorPrefs.GetString("Wit::ServerToken", "");
                    }
                    catch (Exception e)
                    {
                        // This will happen if we don't prime the server token on the main thread and
                        // we access the server token editorpref value in a request.
                        Debug.LogError(e.Message);
                    }
                }

                return serverToken;
            }
            set
            {
                serverToken = value;
                EditorPrefs.SetString("Wit::ServerToken", serverToken);
            }
#else
        get => "";
#endif
        }

        public class DefaultTokenValidatorProvider : ITokenValidationProvider
        {
            public bool IsTokenValid(string appId, string token)
            {
                return IsServerTokenValid(token);
            }

            public bool IsServerTokenValid(string serverToken)
            {
                return null != serverToken && serverToken.Length == 32;
            }
        }

        public interface ITokenValidationProvider
        {
            bool IsTokenValid(string appId, string token);
            bool IsServerTokenValid(string serverToken);
        }

#if UNITY_EDITOR
        public static void InitEditorTokens()
        {
            if (null == serverToken)
            {
                serverToken = EditorPrefs.GetString("Wit::ServerToken", "");
            }
        }
#endif
    }
}
