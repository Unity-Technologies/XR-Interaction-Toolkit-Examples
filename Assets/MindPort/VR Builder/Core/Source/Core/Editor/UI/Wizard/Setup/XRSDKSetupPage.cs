// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using VRBuilder.Editor.XRUtils;

namespace VRBuilder.Editor.UI.Wizard
{
    /// <summary>
    /// Wizard page which retrieves and loads XR SDKs.
    /// </summary>
    internal class XRSDKSetupPage : WizardPage
    {
        private enum XRLoader
        {
            Oculus,
            OpenXR_OculusTouch,
            OpenXR_ValveIndex,
            OpenXR_HtcVive,
            WindowsMR,
            OpenXR,
            None,
        }

        private readonly List<XRLoader> options = new List<XRLoader>(Enum.GetValues(typeof(XRLoader)).Cast<XRLoader>());

        private readonly List<string> nameplates = new List<string>()
        {
            "Meta Quest/Oculus Rift",
            "Pico Neo 3",
            "Valve Index",
            "HTC Vive",
            "WMR Devices",
            "Other (Default OpenXR)",
            "None",
        };

        private readonly List<XRLoader> disabledOptions = new List<XRLoader>();

        [SerializeField]
        private XRLoader selectedLoader = XRLoader.None;

        [SerializeField]
        private bool wasApplied = false;

        public XRSDKSetupPage() : base("XR Hardware")
        {
#if !UNITY_2020_1_OR_NEWER
            disabledOptions.Add(XRLoader.OpenXR);
            disabledOptions.Add(XRLoader.OpenXR_HtcVive);
            disabledOptions.Add(XRLoader.OpenXR_OculusTouch);
            disabledOptions.Add(XRLoader.OpenXR_ValveIndex);
#endif            
        }

        /// <inheritdoc/>
        public override void Draw(Rect window)
        {
            wasApplied = false;

            GUILayout.BeginArea(window);
            {
                GUILayout.Label("VR Hardware Setup", BuilderEditorStyles.Title);
                GUILayout.Label("Select your VR hardware from the list below.", BuilderEditorStyles.Header);
                GUILayout.Label("VR Builder will automatically configure the project to work with your hardware in tethered mode.", BuilderEditorStyles.Paragraph);
                GUILayout.Space(16);

                selectedLoader = BuilderGUILayout.DrawToggleGroup(selectedLoader, options, nameplates, disabledOptions);

                if (selectedLoader == XRLoader.OpenXR)
                {
                    GUILayout.Space(16);
                    GUILayout.Label("You will need to enable a suitable controller profile before being able to use your hardware. Please review the OpenXR Project Settings page after setup.", BuilderEditorStyles.Paragraph);
                }

                if(selectedLoader == XRLoader.None)
                {
                    GUILayout.Space(16);
                    GUILayout.Label("Are you using a different headset? Let us know what it is and if you would like us to provide automated setup for it! You can join our community from the Tools > VR Builder menu.", BuilderEditorStyles.Paragraph);
                }
            }
            GUILayout.EndArea();
        }

        public override void Apply()
        {
            wasApplied = true;
        }

        /// <inheritdoc/>
        public override void Skip()
        {
            ResetSettings();
        }

        /// <inheritdoc/>
        public override void Closing(bool isCompleted)
        {
            if (isCompleted && wasApplied)
            {
                switch (selectedLoader)
                {
                    case XRLoader.Oculus:
                        XRLoaderHelper.LoadOculus();
                        break;
                    case XRLoader.OpenXR:
                        XRLoaderHelper.LoadOpenXR();
                        break;
                    case XRLoader.WindowsMR:
#if UNITY_2020_1_OR_NEWER
                        AddOpenXRControllerProfile("MicrosoftMotionControllerProfile");
                        XRLoaderHelper.LoadOpenXR();
#else
                        XRLoaderHelper.LoadWindowsMR();
#endif
                        break;
                    case XRLoader.OpenXR_OculusTouch:
                        AddOpenXRControllerProfile("OculusTouchControllerProfile");
                        XRLoaderHelper.LoadOpenXR();
                        break;
                    case XRLoader.OpenXR_ValveIndex:
                        AddOpenXRControllerProfile("ValveIndexControllerProfile");
                        XRLoaderHelper.LoadOpenXR();
                        break;
                    case XRLoader.OpenXR_HtcVive:
                        AddOpenXRControllerProfile("HTCViveControllerProfile");
                        XRLoaderHelper.LoadOpenXR();
                        break;
                }
            }
        }

        private void AddOpenXRControllerProfile(string profileType)
        {
            BuilderProjectSettings settings = BuilderProjectSettings.Load();
            settings.OpenXRControllerProfiles.Add(profileType);
            settings.Save();
        }

        private void ResetSettings()
        {
            CanProceed = false;
            selectedLoader = XRLoader.None;
        }
    }
}
