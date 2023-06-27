/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using System.Reflection;
using Meta.WitAi;

namespace Meta.WitAi.Windows
{
    public class WitEndpointConfigDrawer : WitPropertyDrawer
    {
        // All WitEndpointConfig parameters
        private const string FIELD_URISCHEME = "_uriScheme";
        private const string FIELD_AUTHORITY = "_authority";
        private const string FIELD_PORT = "_port";
        private const string FIELD_API = "_witApiVersion";
        private const string FIELD_SPEECH = "_speech";
        private const string FIELD_MESSAGE = "_message";
        private const string FIELD_DICTATION = "_dictation";
        private const string FIELD_SYNTHESIZE = "_synthesize";

        // Allow edit with lock
        protected override WitPropertyEditType EditType => WitPropertyEditType.LockEdit;
        // Get default fields
        protected override string GetDefaultFieldValue(SerializedProperty property, FieldInfo subfield)
        {
            // Iterate options
            switch (subfield.Name)
            {
                case FIELD_URISCHEME:
                    return WitConstants.URI_SCHEME;
                case FIELD_AUTHORITY:
                    return WitConstants.URI_AUTHORITY;
                case FIELD_PORT:
                    return "0";
                case FIELD_API:
                    return WitConstants.API_VERSION;
                case FIELD_SPEECH:
                    return WitConstants.ENDPOINT_SPEECH;
                case FIELD_MESSAGE:
                    return WitConstants.ENDPOINT_MESSAGE;
                case FIELD_DICTATION:
                    return WitConstants.ENDPOINT_DICTATION;
                case FIELD_SYNTHESIZE:
                    return WitConstants.ENDPOINT_TTS;
                case "_event":
                    return WitConstants.ENDPOINT_COMPOSER_MESSAGE;
                case "_converse":
                    return WitConstants.ENDPOINT_COMPOSER_SPEECH;
            }

            // Return base
            return base.GetDefaultFieldValue(property, subfield);
        }
        // Use name value for title if possible
        protected override string GetLocalizedText(SerializedProperty property, string key)
        {
            // Iterate options
            switch (key)
            {
                case LocalizedTitleKey:
                    return WitTexts.Texts.ConfigurationEndpointTitleLabel;
                case FIELD_URISCHEME:
                    return WitTexts.Texts.ConfigurationEndpointUriLabel;
                case FIELD_AUTHORITY:
                    return WitTexts.Texts.ConfigurationEndpointAuthLabel;
                case FIELD_PORT:
                    return WitTexts.Texts.ConfigurationEndpointPortLabel;
                case FIELD_API:
                    return WitTexts.Texts.ConfigurationEndpointApiLabel;
                case FIELD_SPEECH:
                    return WitTexts.Texts.ConfigurationEndpointSpeechLabel;
                case FIELD_MESSAGE:
                    return WitTexts.Texts.ConfigurationEndpointMessageLabel;
                case FIELD_DICTATION:
                    return WitTexts.Texts.ConfigurationEndpointDictationLabel;
                case FIELD_SYNTHESIZE:
                    return WitTexts.Texts.ConfigurationEndpointSynthesizeLabel;
                case "_event":
                    return WitTexts.Texts.ConfigurationEndpointComposerEventLabel;
                case "_converse":
                    return WitTexts.Texts.ConfigurationEndpointComposerConverseLabel;
            }
            // Default to base
            return base.GetLocalizedText(property, key);
        }
    }
}
