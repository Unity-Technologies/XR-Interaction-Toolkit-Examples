// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

#if UNITY_XR_MANAGEMENT && (OCULUS_XR || WINDOWS_XR || OPEN_XR)
using System;
using UnityEditor;
using VRBuilder.Editor.PackageManager;
using UnityEngine;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;

namespace VRBuilder.Editor.XRUtils
{
    internal abstract class XRProvider : Dependency, IDisposable
    {
        protected virtual string XRLoaderName { get; } = "";

        protected XRProvider()
        {
            if (EditorPrefs.GetBool(XRLoaderHelper.IsXRLoaderInitialized) == false)
            {
                OnPackageEnabled += InitializeXRLoader;
            }
        }

        public void Dispose()
        {
            OnPackageEnabled -= InitializeXRLoader;
        }

        protected virtual async void InitializeXRLoader(object sender, EventArgs e)
        {
            OnPackageEnabled -= InitializeXRLoader;
            bool wasLoaderEnabled = await XRLoaderHelper.TryToEnableLoader(XRLoaderName);

            SettingsService.NotifySettingsProviderChanged();

            XRGeneralSettings buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            XRManagerSettings pluginsSettings = buildTargetSettings.AssignedSettings;
            bool wasLoaderAssigned = XRPackageMetadataStore.AssignLoader(pluginsSettings, XRLoaderName, BuildTargetGroup.Standalone);

            if (wasLoaderEnabled == false || wasLoaderAssigned == false)
            {
                Debug.LogWarning($"{XRLoaderName} could not be loaded. Enable it manually here:\nEdit > Project Settings... > XR Plug-in Management.");
            }           
        }
    }
}
#endif
