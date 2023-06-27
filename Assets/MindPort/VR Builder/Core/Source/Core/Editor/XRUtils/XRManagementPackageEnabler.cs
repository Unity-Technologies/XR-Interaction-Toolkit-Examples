// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

#if UNITY_XR_MANAGEMENT
using UnityEditor;
using System;
using VRBuilder.Editor.PackageManager;

namespace VRBuilder.Editor.XRUtils
{
    /// <summary>
    /// Enables the XR Plug-in Management.
    /// </summary>
    /// <remarks>
    /// The purpose of this class is to ensure the XR SDKs are enabled after the XR Plugin Management is installed.
    /// </remarks>
    internal sealed class XRManagementPackageEnabler : Dependency, IDisposable
    {
        /// <inheritdoc/>
        public override string Package { get; } = "com.unity.xr.management";

        /// <inheritdoc/>
        public override int Priority { get; } = 1;

        public XRManagementPackageEnabler()
        {
            OnPackageEnabled += InitializeXRLoader;
        }

        public void Dispose()
        {
            OnPackageEnabled -= InitializeXRLoader;
        }

        private void InitializeXRLoader(object sender, EventArgs e)
        {
            XRLoaderHelper.XRSDK sdk = (XRLoaderHelper.XRSDK) EditorPrefs.GetInt(nameof(XRLoaderHelper.XRSDK));

            switch (sdk)
            {
                case XRLoaderHelper.XRSDK.OpenVR:
                    break;
                case XRLoaderHelper.XRSDK.Oculus:
                    XRLoaderHelper.LoadOculus();
                    break;
                case XRLoaderHelper.XRSDK.WindowsMR:
                    XRLoaderHelper.LoadWindowsMR();
                    break;
                case XRLoaderHelper.XRSDK.OpenXR:
                    XRLoaderHelper.LoadOpenXR();
                    break;
            }

            EditorPrefs.DeleteKey(nameof(XRLoaderHelper.XRSDK));
            OnPackageEnabled -= InitializeXRLoader;
        }
    }
}
#endif
