/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Text;
using System.Collections.Generic;
using Meta.WitAi.Data.Info;
using Meta.WitAi.Requests;

namespace Meta.WitAi.Lib
{
    public static class WitAppInfoUtility
    {
        // List of app ids for currently updating configurations
        private static List<string> _updatingAppIds = new List<string>();
        // Getter for configuration update state
        public static bool IsUpdatingData(this IWitRequestConfiguration configuration)
        {
            string appId = configuration.GetApplicationId();
            return !string.IsNullOrEmpty(appId) && _updatingAppIds.Contains(appId);
        }
        // Setter for configuration update state
        private static void SetUpdatingData(IWitRequestConfiguration configuration, bool toRefreshing)
        {
            string appId = configuration.GetApplicationId();
            if (!string.IsNullOrEmpty(appId))
            {
                bool wasRefreshing = _updatingAppIds.Contains(appId);
                if (toRefreshing && !wasRefreshing)
                {
                    _updatingAppIds.Add(appId);
                }
                else if (!toRefreshing && wasRefreshing)
                {
                    _updatingAppIds.Remove(appId);
                }
            }
        }

        // Determine if is a server token
        public static VRequest CheckServerToken(string serverToken, Action<bool> onComplete)
        {
            WitServerRequestConfiguration config = new WitServerRequestConfiguration(serverToken);
            WitInfoVRequest request = GetRequest(config);
            request.RequestAppId((appId, error) =>
            {
                bool success = string.IsNullOrEmpty(error);
                onComplete?.Invoke(success);
            });
            return request;
        }

        // Returns a vrequest
        private static WitInfoVRequest GetRequest(IWitRequestConfiguration configuration, bool useServerToken = true) =>
            new WitInfoVRequest(configuration, useServerToken);

        /// <summary>
        /// Get application info using server access token
        /// </summary>
        /// <param name="serverAccessToken">Server access token</param>
        /// <param name="onUpdateComplete">On update completed callback</param>
        public static void GetAppInfo(string serverAccessToken,
            Action<string, WitAppInfo, string> onUpdateComplete)
        {
            WitServerRequestConfiguration config = new WitServerRequestConfiguration(serverAccessToken);
            Update(config, (info, error) => onUpdateComplete?.Invoke(config.GetClientAccessToken(), info, error));
        }

        /// <summary>
        /// Update configuration info using
        /// </summary>
        /// <param name="configInfo">Configuration info</param>
        /// <param name="onUpdateComplete"></param>
        public static void Update(IWitRequestConfiguration configuration,
            Action<WitAppInfo, string> onUpdateComplete)
        {
            // Get default application info
            WitAppInfo appInfo = configuration.GetApplicationInfo();
            StringBuilder warnings = new StringBuilder();

            // Needs server access token
            if (string.IsNullOrEmpty(configuration.GetServerAccessToken()))
            {
                // Update data without server access token
                if (!string.IsNullOrEmpty(appInfo.id) && !string.IsNullOrEmpty(configuration.GetClientAccessToken()))
                {
                    SetUpdatingData(configuration, true);
                    UpdateIntents(configuration, appInfo, warnings, false, onUpdateComplete);
                    return;
                }
                // Fail
                else
                {
                    warnings.AppendLine("No server access tokens provided.");
                    UpdateComplete(configuration, appInfo, warnings, onUpdateComplete);
                    return;
                }
            }

            // Needs app id
            if (string.IsNullOrEmpty(appInfo.id))
            {
                GetAppId(configuration, appInfo, warnings, onUpdateComplete);
            }
            // Update existing app info
            else
            {
                SetUpdatingData(configuration, true);
                UpdateAppInfo(configuration, appInfo, warnings, onUpdateComplete);
            }
        }

        // Update all configuration specific data
        private static void GetAppId(IWitRequestConfiguration configuration,
            WitAppInfo appInfo, StringBuilder warnings,
            Action<WitAppInfo, string> onUpdateComplete)
        {
            GetRequest(configuration).RequestAppId((appId, error) =>
            {
                if (!string.IsNullOrEmpty(error))
                {
                    warnings.AppendLine(error);
                    UpdateComplete(configuration, configuration.GetApplicationInfo(), warnings, onUpdateComplete);
                    return;
                }

                // Set app id
                appInfo.id = appId;
                SetUpdatingData(configuration, true);

                // Update app data
                UpdateAppInfo(configuration, appInfo, warnings, onUpdateComplete);
            });
        }

        // Update all application specific data
        private static void UpdateAppInfo(IWitRequestConfiguration configuration,
            WitAppInfo appInfo, StringBuilder warnings,
            Action<WitAppInfo, string> onUpdateComplete)
        {
            GetRequest(configuration).RequestAppInfo(appInfo.id, (info, error) =>
            {
                // Failed to update application info
                if (!string.IsNullOrEmpty(error))
                {
                    warnings.AppendLine($"Application info update failed ({error})");
                }
                // No app id
                else if (string.IsNullOrEmpty(appInfo.id))
                {
                    warnings.AppendLine($"Application info does not include app id");
                }
                // Success
                else
                {
                    appInfo = info;
                }

                // Invalid app id
                if (string.IsNullOrEmpty(appInfo.id))
                {
                    UpdateComplete(configuration, appInfo, warnings, onUpdateComplete);
                    return;
                }

                // Update client token
                UpdateClientToken(configuration, appInfo, warnings, onUpdateComplete);
            });
        }
        private static void UpdateExportInfo(IWitRequestConfiguration configuration,
            WitAppInfo appInfo, StringBuilder warnings,
            Action<WitAppInfo, string> onUpdateComplete)
        {
            GetRequest(configuration).RequestAppExportInfo(appInfo.id, (exportInfo, error) =>
            {
                if (!String.IsNullOrEmpty(error))
                {
                    warnings.AppendLine($"Could not determine export URI for {appInfo.id}.");
                    UpdateComplete(configuration, appInfo, warnings, onUpdateComplete);
                    return;
                }
                GetRequest(configuration).RequestAppExportZip(exportInfo.uri, (exportZip, downloadError) =>
                {
                    // Failed to update client token
                    if (!string.IsNullOrEmpty(downloadError))
                    {
                        warnings.AppendLine($"App export download failed ({downloadError})");
                    }
                    // Got download
                    else
                    {
                        var ep = new ExportParser(exportZip);
                        appInfo.composer = ep.ImportComposerInfo(); //TODO: split out to composer-only lib.
                    }
                    // Complete
                    UpdateComplete(configuration, appInfo, warnings, onUpdateComplete);
                });
            });
        }

        // Update client token
        private static void UpdateClientToken(IWitRequestConfiguration configuration,
            WitAppInfo appInfo, StringBuilder warnings,
            Action<WitAppInfo, string> onUpdateComplete)
        {
            GetRequest(configuration).RequestClientAppToken(appInfo.id, (token, error) =>
            {
                // Failed to update client token
                if (!string.IsNullOrEmpty(error))
                {
                    warnings.AppendLine($"Client token update failed ({error})");
                }
                // Got token
                else
                {
                    configuration.SetClientAccessToken(token);
                }

                // Update intents
                UpdateIntents(configuration, appInfo, warnings, true, onUpdateComplete);
            });
        }

        // Update intents
        private static void UpdateIntents(IWitRequestConfiguration configuration,
            WitAppInfo appInfo, StringBuilder warnings, bool useServerToken,
            Action<WitAppInfo, string> onUpdateComplete)
        {
            GetRequest(configuration, useServerToken).RequestIntentList((intents, error) =>
            {
                // Failed to update intent list
                if (!string.IsNullOrEmpty(error))
                {
                    warnings.AppendLine($"Intent list update failed ({error})");

                }
                // Successfully updated intent list
                else
                {
                    appInfo.intents = intents;
                }

                // Update each intent
                UpdateIntent(0, configuration, appInfo, warnings, useServerToken, onUpdateComplete);
            });
        }
        // Perform each
        private static void UpdateIntent(int index, IWitRequestConfiguration configuration,
            WitAppInfo appInfo, StringBuilder warnings, bool useServerToken,
            Action<WitAppInfo, string> onUpdateComplete)
        {
            // Done
            if (appInfo.intents == null || index >= appInfo.intents.Length)
            {
                UpdateEntities(configuration, appInfo, warnings, useServerToken, onUpdateComplete);
                return;
            }

            // Get original intent info
            WitIntentInfo intent = appInfo.intents[index];

            // Perform update
            GetRequest(configuration, useServerToken).RequestIntentInfo(intent.id, (result, error) =>
            {
                // Failed to update intent
                if (!string.IsNullOrEmpty(error))
                {
                    warnings.AppendLine($"Intent[{index}] update failed ({error})");
                }
                // Successfully updated intent
                else
                {
                    appInfo.intents[index] = result;
                }

                // Next
                UpdateIntent(index + 1, configuration, appInfo, warnings, useServerToken, onUpdateComplete);
            });
        }

        // Update entities
        private static void UpdateEntities(IWitRequestConfiguration configuration,
            WitAppInfo appInfo, StringBuilder warnings, bool useServerToken,
            Action<WitAppInfo, string> onUpdateComplete)
        {
            GetRequest(configuration, useServerToken).RequestEntityList((entities, error) =>
            {
                // Failed to update entity list
                if (!string.IsNullOrEmpty(error))
                {
                    warnings.AppendLine($"Entity list update failed ({error})");

                }
                // Successfully updated entity list
                else
                {
                    appInfo.entities = entities;
                }

                // Update each
                UpdateEntity(0, configuration, appInfo, warnings, useServerToken, onUpdateComplete);
            });
        }
        // Perform each
        private static void UpdateEntity(int index, IWitRequestConfiguration configuration,
            WitAppInfo appInfo, StringBuilder warnings, bool useServerToken,
            Action<WitAppInfo, string> onUpdateComplete)
        {
            // Done
            if (appInfo.entities == null || index >= appInfo.entities.Length)
            {
                UpdateTraits(configuration, appInfo, warnings, useServerToken, onUpdateComplete);
                return;
            }

            // Get original entity info
            WitEntityInfo entity = appInfo.entities[index];

            // Perform update
            GetRequest(configuration, useServerToken).RequestEntityInfo(entity.id, (result, error) =>
            {
                // Failed to update entity
                if (!string.IsNullOrEmpty(error))
                {
                    warnings.AppendLine($"Entity[{index}] update failed ({error})");
                }
                // Successfully updated intent
                else
                {
                    appInfo.entities[index] = result;
                }

                // Next
                UpdateEntity(index + 1, configuration, appInfo, warnings, useServerToken, onUpdateComplete);
            });
        }

        // Update traits
        private static void UpdateTraits(IWitRequestConfiguration configuration,
            WitAppInfo appInfo, StringBuilder warnings, bool useServerToken,
            Action<WitAppInfo, string> onUpdateComplete)
        {
            GetRequest(configuration, useServerToken).RequestTraitList((traits, error) =>
            {
                // Failed to update trait list
                if (!string.IsNullOrEmpty(error))
                {
                    warnings.AppendLine($"Trait list update failed ({error})");
                }
                // Successfully updated trait list
                else
                {
                    appInfo.traits = traits;
                }

                // Update each trait
                UpdateTrait(0, configuration, appInfo, warnings, useServerToken, onUpdateComplete);
            });
        }
        // Perform each
        private static void UpdateTrait(int index, IWitRequestConfiguration configuration,
            WitAppInfo appInfo, StringBuilder warnings, bool useServerToken,
            Action<WitAppInfo, string> onUpdateComplete)
        {
            // Done
            if (appInfo.traits == null || index >= appInfo.traits.Length)
            {
                UpdateVoices(configuration, appInfo, warnings, useServerToken, onUpdateComplete);
                return;
            }

            // Get original trait info
            WitTraitInfo trait = appInfo.traits[index];

            // Perform update
            GetRequest(configuration, useServerToken).RequestTraitInfo(trait.id, (result, error) =>
            {
                // Failed to update trait
                if (!string.IsNullOrEmpty(error))
                {
                    warnings.AppendLine($"Trait[{index}] update failed ({error})");
                }
                // Successfully updated trait
                else
                {
                    appInfo.traits[index] = result;
                }

                // Next
                UpdateTrait(index + 1, configuration, appInfo, warnings, useServerToken, onUpdateComplete);
            });
        }

        // Update tts voices
        private static void UpdateVoices(IWitRequestConfiguration configuration,
            WitAppInfo appInfo, StringBuilder warnings, bool useServerToken,
            Action<WitAppInfo, string> onUpdateComplete)
        {
            GetRequest(configuration, useServerToken).RequestVoiceList((voicesByLocale, error) =>
                {
                    // Failed
                    if (!string.IsNullOrEmpty(error))
                    {
                        warnings.AppendLine($"Voice list update failed ({error})");
                    }
                    // Success
                    else
                    {
                        List<WitVoiceInfo> voiceList = new List<WitVoiceInfo>();
                        foreach (var voices in voicesByLocale.Values)
                        {
                            voiceList.AddRange(voices);
                        }

                        appInfo.voices = voiceList.ToArray();
                    }

                    UpdateExportInfo(configuration, appInfo, warnings, onUpdateComplete);

                });
        }

        // Completion
        private static void UpdateComplete(IWitRequestConfiguration configuration,
            WitAppInfo appInfo, StringBuilder warnings,
            Action<WitAppInfo, string> onUpdateComplete)
        {
            // Get app name
            string appNameLog = string.IsNullOrEmpty(appInfo.name) ? string.Empty : $"\nWit App: {appInfo.name}";

            // No longer updating
            SetUpdatingData(configuration, false);

            // Success
            if (warnings.Length == 0)
            {
                VLog.D($"App Info Update Success{appNameLog}");
            }
            // Warnings
            else
            {
                VLog.W($"App Info Update Warnings{appNameLog}\n{warnings}");
            }

            // Callback
            onUpdateComplete?.Invoke(appInfo, warnings.ToString());
        }
    }
}
