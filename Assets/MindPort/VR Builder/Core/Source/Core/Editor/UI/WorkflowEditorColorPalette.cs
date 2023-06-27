// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI
{
    /// <summary>
    /// Class to provide convenient access to all colors used in the Workflow window.
    /// </summary>
    [DataContract]
    public class WorkflowEditorColorPalette
    {
        /// <summary>
        /// Returns background color of the editor based on current editor skin.
        /// </summary>
        public static Color EditorBackground
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color32(56, 56, 56, 255);
                }

                return new Color32(194, 194, 194, 255);
            }
        }

        /// <summary>
        /// Primary color for selected elements.
        /// </summary>
        [DataMember]
        public Color Primary { get; private set; }

        /// <summary>
        /// Secondary color (for highlights).
        /// </summary>
        [DataMember]
        public Color Secondary { get; private set; }

        /// <summary>
        /// Background color for graphical elements (step nodes, buttons...)
        /// </summary>
        [DataMember]
        public Color ElementBackground { get; private set; }

        /// <summary>
        /// Color of transition arrows between steps.
        /// </summary>
        [DataMember]
        public Color Transition { get; private set; }

        /// <summary>
        /// Text color easily readable at ElementBackground color.
        /// </summary>
        [DataMember]
        public Color Text { get; private set; }

        /// <summary>
        /// Load palette from a json file.
        /// </summary>
        /// <param name="paletteSubpath">Subpath to file. If file is not found, use default palette. If more than one possible candidate is found, use default palette. If there was a serialization error, use default palette.</param>
        public static WorkflowEditorColorPalette LoadPaletteFrom(string paletteSubpath)
        {
            string[] possiblePalettes = AssetDatabase.GetAllAssetPaths().Where(s => s.EndsWith(paletteSubpath)).ToArray();

            if (possiblePalettes.Length <= 0)
            {
                return null;
            }

            if (possiblePalettes.Length > 1)
            {
                Debug.LogError("More than one possible palette found.");
            }
            else
            {
                TextAsset paletteAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(possiblePalettes[0]);

                if (paletteAsset == null)
                {
                    return null;
                }

                string serialized = paletteAsset.text;
                try
                {
                    return JsonConvert.DeserializeObject<WorkflowEditorColorPalette>(serialized);
                }
                catch (JsonException e)
                {
                    Debug.LogError(e);
                    return null;
                }
            }

            return null;
        }

        public static WorkflowEditorColorPalette GetDefaultPalette()
        {
            return new WorkflowEditorColorPalette()
            {
                Primary = new Color32(124, 0, 255, 255),
                Secondary = new Color32(223, 0, 255, 255),
                ElementBackground = new Color32(102, 102, 102, 255),
                Transition =  Color.white,
                Text = Color.white
            };
        }
    }
}
