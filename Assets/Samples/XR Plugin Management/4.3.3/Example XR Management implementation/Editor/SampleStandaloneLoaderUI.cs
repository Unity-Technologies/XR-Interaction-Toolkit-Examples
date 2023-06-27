using System;

using UnityEditor;
using UnityEditor.XR.Management;

using UnityEngine;

namespace Samples
{
    /// <summary>
    /// Sample loader UI demonstrating how to provide your own loader selection UI for the
    /// loader selection list.
    /// </summary>
    [XRCustomLoaderUI("Samples.SampleLoader", BuildTargetGroup.Standalone)]
    public class SampleStandaloneLoaderUI : IXRCustomLoaderUI
    {
        static readonly string[] features = new string[]{
            "Feature One",
            "Feature Two",
            "Feature Three"
        };

        struct Content
        {
            public static readonly GUIContent k_LoaderName = new GUIContent("Sample Loader One Custom <SAMPLE ONLY YOU MUST REIMPLEMENT>");
            public static readonly GUIContent k_Download = new GUIContent("Download");
            public static readonly GUIContent k_WarningIcon = EditorGUIUtility.IconContent("console.warnicon.sml");
        }

        float renderLineHeight = 0;

        /// <inheritdoc />
        public bool IsLoaderEnabled { get; set; }

        /// <inheritdoc />
        public string[] IncompatibleLoaders => new string[] { "UnityEngine.XR.WindowsMR.WindowsMRLoader" };

        /// <inheritdoc />
        public float RequiredRenderHeight { get; private set; }

        /// <inheritdoc />
        public void SetRenderedLineHeight(float height)
        {
            renderLineHeight = height;
            RequiredRenderHeight = height;

            if (IsLoaderEnabled)
            {
                RequiredRenderHeight += features.Length * height;
            }
        }

        /// <inheritdoc />
        public BuildTargetGroup ActiveBuildTargetGroup { get; set; }

        /// <inheritdoc />
        public void OnGUI(Rect rect)
        {
            var size = EditorStyles.toggle.CalcSize(Content.k_LoaderName);
            var labelRect = new Rect(rect);
            labelRect.width = size.x;
            labelRect.height = renderLineHeight;
            IsLoaderEnabled = EditorGUI.ToggleLeft(labelRect, Content.k_LoaderName, IsLoaderEnabled);

            // The following shows how to make draw an icon with a tooltip
            size = EditorStyles.label.CalcSize(Content.k_WarningIcon);
            var imageRect = new Rect(rect);
            imageRect.xMin = labelRect.xMax + 1;
            imageRect.width = size.y;
            imageRect.height = renderLineHeight;
            var iconWithTooltip = new GUIContent("", Content.k_WarningIcon.image, "Warning: This is a sample to show how to draw a custom icon with a tooltip!");
            EditorGUI.LabelField(imageRect, iconWithTooltip);

            if (IsLoaderEnabled)
            {
                EditorGUI.indentLevel++;
                var featureRect = new Rect(rect);
                featureRect.yMin = labelRect.yMax + 1;
                featureRect.height = renderLineHeight;
                foreach (var feature in features)
                {
                    var buttonSize = EditorStyles.toggle.CalcSize(Content.k_Download);

                    var featureLabelRect = new Rect(featureRect);
                    featureLabelRect.width -= buttonSize.x;
                    EditorGUI.ToggleLeft(featureLabelRect, feature, false);

                    var buttonRect = new Rect(featureRect);
                    buttonRect.xMin = featureLabelRect.xMax + 1;
                    buttonRect.width = buttonSize.x;
                    if (GUI.Button(buttonRect, Content.k_Download))
                    {
                        Debug.Log($"{feature} download button pressed. Do something here!");
                    }

                    featureRect.yMin += renderLineHeight;
                    featureRect.height = renderLineHeight;
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}
