/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;

namespace Meta.WitAi.Data.Info
{
    [Serializable]
    public struct WitComposerInfo
    {
        /// <summary>
        /// List of canvases in the app
        /// </summary>
        [Tooltip("Represents the canvas of the given name.")]
        public ComposerGraph[] canvases;
    }

    [Serializable]
    public struct ComposerGraph
    {
        [HideInInspector]
        public string canvasName;
        [Tooltip("The Context Map is a JSON object passed between the the server and the client. \n" +
            "These are the JSON paths and values present in the Context Map on the server")]
        public ContextMapPaths contextMap;
    }

    /// <summary>
    /// Path names and values of variables referenced in a context map.
    /// </summary>
    [Serializable]
    public struct ContextMapPaths
    {
        [Tooltip("The path names and values which are written by the Composer graph for the client to read. Composer does not read these values.")]
        public ComposerGraphValues[] server;

        [Tooltip("The path names which the Composer graph references but does not modify. The values of these must be supplied by the client.")]
        public string[] client;

        [Tooltip("The paths which the Composer graph both modifies and references. The client read or modify these.")]
        public ComposerGraphValues[] shared;
    }

    [Serializable]
    public struct ComposerGraphValues
    {
        [Tooltip("The path name referenced in Composer")]
        public string path;
        [Tooltip("The values assigned to this path in Composer")]
        public string[] values;
    }
}
