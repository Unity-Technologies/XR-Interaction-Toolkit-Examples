/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Lib;
using Meta.WitAi.Requests;

namespace Meta.WitAi.Windows
{
    public class WitWelcomeWizard : WitScriptableWizard
    {
        protected string serverToken;
        public Action<WitConfiguration> successAction;

        protected override Texture2D HeaderIcon => WitTexts.HeaderIcon;
        protected override GUIContent Title => WitTexts.SetupTitleContent;
        protected override string ButtonLabel => WitTexts.Texts.SetupSubmitButtonLabel;
        protected override string ContentSubheaderLabel => WitTexts.Texts.SetupSubheaderLabel;

        // Whether currently using a client token
        private bool _isCheckingToken = false;
        private bool _usingClientToken = false;
        private VRequest _request;

        protected override void OnEnable()
        {
            base.OnEnable();
            _isCheckingToken = false;
            _usingClientToken = false;
            serverToken = string.Empty;
            WitAuthUtility.ServerToken = serverToken;
        }
        protected virtual void OnDisable()
        {
            StopTokenCheck();
        }
        protected override bool DrawWizardGUI()
        {
            // Layout base
            base.DrawWizardGUI();
            // True if valid server token
            return WitConfigurationUtility.IsServerTokenValid(serverToken);
        }
        protected override void LayoutFields()
        {
            // Token label
            string serverTokenLabelText = WitTexts.Texts.SetupServerTokenLabel;
            serverTokenLabelText = serverTokenLabelText.Replace(WitStyles.WitLinkKey, WitStyles.WitLinkColor);
            if (GUILayout.Button(serverTokenLabelText, WitStyles.Label))
            {
                Application.OpenURL(WitTexts.GetAppURL("", WitTexts.WitAppEndpointType.Settings));
            }
            // Token field
            bool updated = false;
            WitEditorUI.LayoutPasswordField(null, ref serverToken, ref updated);

            // Verifying
            if (_isCheckingToken)
            {
                WitEditorUI.LayoutLabel(WitTexts.Texts.SetupServerTokenVerifyLabel);
            }
            // Client token warning
            else if (_usingClientToken)
            {
                WitEditorUI.LayoutErrorLabel(WitTexts.Texts.SetupClientTokenWarningLabel);
            }

            // Determine if new token is a server token
            if (updated && WitConfigurationUtility.IsServerTokenValid(serverToken))
            {
                CheckToken();
            }
        }
        private void CheckToken()
        {
            // Cancel previous check
            StopTokenCheck();

            // Begin checking
            _isCheckingToken = true;

            // Perform request
            _request = WitConfigurationUtility.CheckServerToken(serverToken, (success) =>
            {
                _usingClientToken = !success;
                _isCheckingToken = false;
            });
        }
        private void StopTokenCheck()
        {
            // Ignore if not checking
            if (!_isCheckingToken)
            {
                return;
            }

            // Kill request
            if (_request != null)
            {
                _request.Cancel();
                _request = null;
            }

            // Done checking
            _isCheckingToken = false;
        }
        protected override void OnWizardCreate()
        {
            ValidateAndClose();
        }
        protected virtual void ValidateAndClose()
        {
            // Verify
            if (_isCheckingToken)
            {
                VLog.E(WitTexts.Texts.SetupServerTokenVerifyWarning);
                return;
            }
            // Check client token
            if (_usingClientToken)
            {
                if (!WitConfigurationUtility.IsClientTokenValid(serverToken))
                {
                    VLog.E(WitTexts.Texts.SetupSubmitFailLabel);
                    return;
                }
            }
            // Check server token
            else
            {
                WitAuthUtility.ServerToken = serverToken;
                if (!WitAuthUtility.IsServerTokenValid())
                {
                    VLog.E(WitTexts.Texts.SetupSubmitFailLabel);
                    return;
                }
            }
            // Create configuration
            int index = CreateConfiguration(serverToken);
            if (index != -1)
            {
                // Complete
                Close();
                WitConfiguration c = WitConfigurationUtility.WitConfigs[index];
                if (successAction == null)
                {
                    WitWindowUtility.OpenConfigurationWindow(c);
                }
                else
                {
                    successAction(c);
                }
            }
        }
        protected virtual int CreateConfiguration(string newToken)
        {
            // Generate asset with client token
            if (_usingClientToken)
            {
                WitConfiguration configuration = ScriptableObject.CreateInstance<WitConfiguration>();
                configuration.SetClientAccessToken(newToken);
                return WitConfigurationUtility.SaveConfiguration(string.Empty, configuration);
            }
            // Generate asset with server token
            return WitConfigurationUtility.CreateConfiguration(newToken);
        }
    }
}
