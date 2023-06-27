// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

#if UNITY_XR_MANAGEMENT && OPEN_XR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace VRBuilder.Editor.XRUtils
{
    /// <summary>
    /// Enables the Open XR Plugin.
    /// </summary>
    internal sealed class OpenXRPackageEnabler : XRProvider
    {
        /// <inheritdoc/>
        public override string Package { get; } = "com.unity.xr.openxr";

        /// <inheritdoc/>
        public override int Priority { get; } = 2;

        protected override string XRLoaderName { get; } = "OpenXRLoader";

        protected override void InitializeXRLoader(object sender, EventArgs e)
        {
            BuilderProjectSettings settings = BuilderProjectSettings.Load();

            BuildTargetGroup buildTargetGroup = BuildTargetGroup.Standalone;
            FeatureHelpers.RefreshFeatures(buildTargetGroup);

            foreach (string controllerProfileType in settings.OpenXRControllerProfiles)
            {
                OpenXRFeature feature = OpenXRSettings.Instance.GetFeatures<OpenXRInteractionFeature>().FirstOrDefault(f => f.GetType().Name == controllerProfileType);

                if (feature != null)
                {
                    feature.enabled = true;
                }
            }

            AssetDatabase.SaveAssets();

            base.InitializeXRLoader(sender, e);
        }
    }
}
#endif
