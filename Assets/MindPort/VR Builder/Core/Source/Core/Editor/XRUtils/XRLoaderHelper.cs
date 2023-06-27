// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEditor;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using VRBuilder.Editor.PackageManager;
using Debug = UnityEngine.Debug;

#if UNITY_XR_MANAGEMENT
using UnityEngine;
using System.Diagnostics;
using UnityEngine.XR.Management;
#endif

namespace VRBuilder.Editor.XRUtils
{
    /// <summary>
    /// Utility class that allows to load XR packages.
    /// </summary>
    public class XRLoaderHelper
    {
        internal const string IsXRLoaderInitialized = "IsXRLoaderInitialized";
        private const string OculusXRPackage = "com.unity.xr.oculus";
        private const string WindowsXRPackage = "com.unity.xr.windowsmr@4.5.0";
        private const string XRManagementPackage = "com.unity.xr.management";
        private const string OpenXRPackage = "com.unity.xr.openxr";

        public enum XRSDK
        {
            None,
            OpenVR,
            Oculus,
            WindowsMR,
            OpenXR
        }

        public enum XRConfiguration
        {
            None,
            XRManagement,
            OpenVRLegacy,
            OculusLegacy,
            OpenVRXR,
            OculusXR,
            WindowsMR,
            XRLegacy
        }

        /// <summary>
        /// Retrieves and loads the OpenXR package.
        /// </summary>
        public static void LoadOpenXR()
        {
            if (GetCurrentXRConfiguration().Any(loader => loader == XRConfiguration.OpenVRXR))
            {
                Debug.LogWarning("OpenXR is already loaded.");
                return;
            }

            EditorPrefs.DeleteKey(IsXRLoaderInitialized);

#if UNITY_2020_1_OR_NEWER
#if UNITY_XR_MANAGEMENT && OPEN_XR
#pragma warning disable CS4014
            TryToEnableLoader("OpenXRLoader");
#pragma warning restore CS4014
#elif !UNITY_XR_MANAGEMENT
            DisplayDialog("XR Plug-in Management");
            EditorPrefs.SetInt(nameof(XRSDK), (int)XRSDK.OpenXR);
            PackageOperationsManager.LoadPackage(XRManagementPackage);
#else
            DisplayDialog("OpenXR");
            PackageOperationsManager.LoadPackage(OpenXRPackage);
#endif
#endif
        }

        /// <summary>
        /// Retrieves and loads the OpenVR XR package.
        /// </summary>
        public static void LoadOpenVR()
        {
            if (GetCurrentXRConfiguration().Any(loader => loader == XRConfiguration.OpenVRXR || loader == XRConfiguration.OpenVRLegacy))
            {
                Debug.LogWarning("OpenVR/XR is already loaded.");
                return;
            }

            EditorPrefs.DeleteKey(IsXRLoaderInitialized);

#if UNITY_2020_1_OR_NEWER
#if UNITY_XR_MANAGEMENT && OPEN_XR
#pragma warning disable CS4014
            TryToEnableLoader("OpenXRLoader");
#pragma warning restore CS4014
#elif !UNITY_XR_MANAGEMENT
            DisplayDialog("XR Plug-in Management");
            EditorPrefs.SetInt(nameof(XRSDK), (int)XRSDK.OpenXR);
            PackageOperationsManager.LoadPackage(XRManagementPackage);
#else
            DisplayDialog("OpenXR");
            PackageOperationsManager.LoadPackage(OpenXRPackage);
#endif
#elif UNITY_2019_1_OR_NEWER
            if (EditorReflectionUtils.AssemblyExists("Unity.XR.Management") == false)
            {
                LoadOpenVRLegacy();
            }
            else
            {
                Debug.LogWarning("OpenVR could not be loaded. XR Plug-in Management must be disabled before start using OpenVR.");
            }
#endif
        }

        /// <summary>
        /// Retrieves and loads the Oculus XR package.
        /// </summary>
        public static void LoadOculus()
        {
            if (GetCurrentXRConfiguration().Any(loader => loader == XRConfiguration.OculusXR))
            {
                Debug.LogWarning("Oculus XR is already loaded.");
                return;
            }

            EditorPrefs.DeleteKey(IsXRLoaderInitialized);

#if UNITY_XR_MANAGEMENT && OCULUS_XR
#pragma warning disable CS4014
            TryToEnableLoader("OculusLoader");
#pragma warning restore CS4014
#elif !UNITY_XR_MANAGEMENT
            DisplayDialog("XR Plug-in Management");
            EditorPrefs.SetInt(nameof(XRSDK), (int)XRSDK.Oculus);
            PackageOperationsManager.LoadPackage(XRManagementPackage);
#else
            DisplayDialog("Oculus XR");
            PackageOperationsManager.LoadPackage(OculusXRPackage);
#endif
        }

        /// <summary>
        /// Retrieves and loads the Windows MR package.
        /// </summary>
        public static void LoadWindowsMR()
        {
            if (GetCurrentXRConfiguration().Any(loader => loader == XRConfiguration.WindowsMR))
            {
                Debug.LogWarning("Windows MR is already loaded.");
                return;
            }

            EditorPrefs.DeleteKey(IsXRLoaderInitialized);

#if UNITY_XR_MANAGEMENT && WINDOWS_XR
#pragma warning disable CS4014
            TryToEnableLoader("WindowsMRLoader");
#pragma warning restore CS4014
#elif !UNITY_XR_MANAGEMENT
            DisplayDialog("XR Plug-in Management");
            EditorPrefs.SetInt(nameof(XRSDK), (int)XRSDK.WindowsMR);
            PackageOperationsManager.LoadPackage(XRManagementPackage);
#else
            DisplayDialog("Windows MR");
            PackageOperationsManager.LoadPackage(WindowsXRPackage);
#endif
        }

        /// <summary>
        /// Returns a list with all XR SDKs enabled in this project.
        /// </summary>
        public static IEnumerable<XRConfiguration> GetCurrentXRConfiguration()
        {
            List<XRConfiguration> enabledSDKs = new List<XRConfiguration>();

            if (EditorReflectionUtils.AssemblyExists("Unity.XR.Management"))
            {
#if UNITY_XR_MANAGEMENT
                if (XRGeneralSettings.Instance != null)
                {
                    foreach (XRLoader loader in XRGeneralSettings.Instance.Manager.activeLoaders)
                    {
                        if (loader.name == "Oculus Loader")
                        {
                            enabledSDKs.Add(XRConfiguration.OculusXR);
                        }

                        if (loader.name == "Windows MR Loader")
                        {
                            enabledSDKs.Add(XRConfiguration.WindowsMR);
                        }

                        if(loader.name == "Open XR Loader")
                        {
                            enabledSDKs.Add(XRConfiguration.OpenVRXR);
                        }
                    }

                    enabledSDKs.Add(XRConfiguration.XRManagement);
                }
#endif
            }
#if UNITY_2019_1_OR_NEWER && !UNITY_2020_1_OR_NEWER
#pragma warning disable CS0618
            else if (PlayerSettings.virtualRealitySupported)
            {
                foreach (string device in UnityEngine.XR.XRSettings.supportedDevices)
                {
                    if (device == "OpenVR")
                    {
                        enabledSDKs.Add(XRConfiguration.OpenVRLegacy);
                    }

                    if (device == "Oculus")
                    {
                        enabledSDKs.Add(XRConfiguration.OculusLegacy);
                    }
                }
                enabledSDKs.Add(XRConfiguration.XRLegacy);
            }
#pragma warning restore CS0618
#endif

            if (enabledSDKs.Any() == false)
            {
                enabledSDKs.Add(XRConfiguration.None);
            }



            return enabledSDKs;
        }

#if UNITY_XR_MANAGEMENT
        internal static async Task<bool> TryToEnableLoader(string loaderName)
        {
            EditorPrefs.SetBool(IsXRLoaderInitialized, true);

            SettingsService.OpenProjectSettings("Project/XR Plug-in Management");

            Stopwatch stopwatch = Stopwatch.StartNew();
            while (XRGeneralSettings.Instance == null)
            {
                await Task.Delay(500);
                if (stopwatch.ElapsedMilliseconds > 5000f)
                {
                    EditorUtility.DisplayDialog($"The {loaderName} could not be enabled!", $"The XR general settings file is missing. Enable {loaderName} manually here:\nEdit > Project Settings... > XR Plug-in Management.", "Continue");
                    return false;
                }
            }

            stopwatch.Stop();

            if (XRGeneralSettings.Instance.Manager.activeLoaders.Any(xrLoader => xrLoader.GetType().Name == loaderName))
            {
                return true;
            }            
            
            XRLoader loader = ScriptableObject.CreateInstance(loaderName) as XRLoader;
            return XRGeneralSettings.Instance.Manager.TryAddLoader(loader);
        }
#endif

        private static void DisplayDialog(string loader)
        {
            //Display dialog disabled.
            //EditorUtility.DisplayDialog($"Enabling {loader}", "Wait until the setup is done.", "Continue");
        }

#if UNITY_2019_1_OR_NEWER && !UNITY_2020_1_OR_NEWER
        private static async void LoadOpenVRLegacy()
        {
            DisplayDialog("OpenVR");
#pragma warning disable CS0618
            PlayerSettings.virtualRealitySupported = true;
            UnityEngine.XR.XRSettings.LoadDeviceByName("OpenVR");
            await Task.Delay(1000);
            UnityEngine.XR.XRSettings.enabled = true;
#pragma warning restore CS0618
        }
#endif
    }
}
