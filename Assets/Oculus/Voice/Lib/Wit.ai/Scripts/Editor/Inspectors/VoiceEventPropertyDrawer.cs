/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.IO;
using UnityEditor;

namespace Meta.WitAi.Events.Editor
{
    [CustomPropertyDrawer(typeof(VoiceEvents))]
    public class VoiceEventPropertyDrawer : EventPropertyDrawer<VoiceEvents>
    {
        /// <summary>
        /// Voice event diagram name
        /// </summary>
        public const string VOICE_EVENT_DIAGRAM_NAME = "VoiceEventsDiagram";
        /// <summary>
        /// Voice event tooltip
        /// </summary>
        public const string VOICE_EVENT_DIAGRAM_TOOLTIP = "Open " + VOICE_EVENT_DIAGRAM_NAME + ".pdf";

        /// <summary>
        /// Open voice event pdf
        /// </summary>
        public override string DocumentationUrl
        {
            get
            {
                if (_documentationUrl == null)
                {
                    string[] assetPaths = AssetDatabase.FindAssets(VOICE_EVENT_DIAGRAM_NAME);
                    if (assetPaths == null || assetPaths.Length == 0)
                    {
                        _documentationUrl = string.Empty;
                    }
                    else
                    {
                        string guid = assetPaths[0];
                        string localPath = AssetDatabase.GUIDToAssetPath(guid);
                        _documentationUrl = $"file://{Path.GetFullPath(localPath)}";
                    }
                }
                return _documentationUrl;
            }
        }
        private static string _documentationUrl = null;

        /// <summary>
        /// Voice pdf tooltip
        /// </summary>
        public override string DocumentationTooltip => VOICE_EVENT_DIAGRAM_TOOLTIP;
    }
}
