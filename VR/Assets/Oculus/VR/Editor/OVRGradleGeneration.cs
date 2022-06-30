/************************************************************************************

Copyright   :   Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus SDK License Version 3.4.1 (the "License");
you may not use the Oculus SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.4.1

Unless required by applicable law or agreed to in writing, the Oculus SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

//#define BUILDSESSION

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS
#define USING_XR_SDK
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#if UNITY_ANDROID
using UnityEditor.Android;
#endif

[InitializeOnLoad]
public class OVRGradleGeneration
	: IPreprocessBuildWithReport, IPostprocessBuildWithReport
#if UNITY_ANDROID
	, IPostGenerateGradleAndroidProject
#endif
{
	public OVRADBTool adbTool;
	public Process adbProcess;

	public int callbackOrder { get { return 3; } }
	static private System.DateTime buildStartTime;
	static private System.Guid buildGuid;

#if UNITY_ANDROID
	public const string prefName = "OVRAutoIncrementVersionCode_Enabled";
	private const string menuItemAutoIncVersion = "Oculus/Tools/Auto Increment Version Code";
	static bool autoIncrementVersion = false;
#endif

	static OVRGradleGeneration()
	{
		EditorApplication.delayCall += OnDelayCall;
	}

	static void OnDelayCall()
	{
#if UNITY_ANDROID
		autoIncrementVersion = PlayerPrefs.GetInt(prefName, 0) != 0;
		Menu.SetChecked(menuItemAutoIncVersion, autoIncrementVersion);
#endif
	}

#if UNITY_ANDROID
	[MenuItem(menuItemAutoIncVersion)]
	public static void ToggleUtilities()
	{
		autoIncrementVersion = !autoIncrementVersion;
		Menu.SetChecked(menuItemAutoIncVersion, autoIncrementVersion);

		int newValue = (autoIncrementVersion) ? 1 : 0;
		PlayerPrefs.SetInt(prefName, newValue);
		PlayerPrefs.Save();

		UnityEngine.Debug.Log("Auto Increment Version Code: " + autoIncrementVersion);
	}
#endif

	public void OnPreprocessBuild(BuildReport report)
	{
#if UNITY_ANDROID && !(USING_XR_SDK && UNITY_2019_3_OR_NEWER)
		// Generate error when Vulkan is selected as the perferred graphics API, which is not currently supported in Unity XR
		if (!PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android))
		{
			GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
			if (apis.Length >= 1 && apis[0] == GraphicsDeviceType.Vulkan)
			{
				throw new BuildFailedException("The Vulkan Graphics API does not support XR in your configuration. To use Vulkan, you must use Unity 2019.3 or newer, and the XR Plugin Management.");
			}
		}
#endif

#if UNITY_ANDROID
		bool useOpenXR = OVRPluginUpdater.IsOVRPluginOpenXRActivated();
#if USING_XR_SDK
		if (useOpenXR)
		{
			UnityEngine.Debug.LogWarning("Oculus Utilities Plugin with OpenXR is being used, which is under experimental status");

			if (PlayerSettings.colorSpace != ColorSpace.Linear)
			{
				throw new BuildFailedException("Oculus Utilities Plugin with OpenXR only supports linear lighting. Please set 'Rendering/Color Space' to 'Linear' in Player Settings");
			}
		}
#else
		if (useOpenXR)
		{
			throw new BuildFailedException("Oculus Utilities Plugin with OpenXR only supports XR Plug-in Managmenent with Oculus XR Plugin.");
		}
#endif
#endif

#if UNITY_ANDROID && USING_XR_SDK && !USING_COMPATIBLE_OCULUS_XR_PLUGIN_VERSION
		if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
			throw new BuildFailedException("Your project is using an Oculus XR Plugin version with known issues. Please navigate to the Package Manager and upgrade the Oculus XR Plugin to the latest verified version. When performing the upgrade" +
				", you must first \"Remove\" the Oculus XR Plugin package, and then \"Install\" the package at the verified version. Be sure to remove, then install, not just upgrade.");
#endif

		buildStartTime = System.DateTime.Now;
		buildGuid = System.Guid.NewGuid();

		if (OculusBuildApp.GetBuildTelemetryEnabled())
		{
			if (!report.summary.outputPath.Contains("OVRGradleTempExport"))
			{
				OVRPlugin.SetDeveloperMode(OVRPlugin.Bool.True);
				OVRPlugin.AddCustomMetadata("build_type", "standard");
			}

			OVRPlugin.AddCustomMetadata("build_guid", buildGuid.ToString());
			OVRPlugin.AddCustomMetadata("target_platform", report.summary.platform.ToString());
#if !UNITY_2019_3_OR_NEWER
			OVRPlugin.AddCustomMetadata("scripting_runtime_version", UnityEditor.PlayerSettings.scriptingRuntimeVersion.ToString());
#endif
			if (report.summary.platform == UnityEditor.BuildTarget.StandaloneWindows
				|| report.summary.platform == UnityEditor.BuildTarget.StandaloneWindows64)
			{
				OVRPlugin.AddCustomMetadata("target_oculus_platform", "rift");
			}
		}
#if BUILDSESSION
		StreamWriter writer = new StreamWriter("build_session", false);
		UnityEngine.Debug.LogFormat("Build Session: {0}", buildGuid.ToString());
		writer.WriteLine(buildGuid.ToString());
		writer.Close();
#endif
	}

	public void OnPostGenerateGradleAndroidProject(string path)
	{
		UnityEngine.Debug.Log("OVRGradleGeneration triggered.");

		var targetOculusPlatform = new List<string>();
		if (OVRDeviceSelector.isTargetDeviceQuestFamily)
		{
			targetOculusPlatform.Add("quest");
		}
		OVRPlugin.AddCustomMetadata("target_oculus_platform", String.Join("_", targetOculusPlatform.ToArray()));
		UnityEngine.Debug.LogFormat("QuestFamily = {0}: Quest = {1}, Quest2 = {2}",
			OVRDeviceSelector.isTargetDeviceQuestFamily,
			OVRDeviceSelector.isTargetDeviceQuest,
			OVRDeviceSelector.isTargetDeviceQuest2);

		OVRProjectConfig projectConfig = OVRProjectConfig.GetProjectConfig();
		if (projectConfig != null && projectConfig.systemSplashScreen != null)
		{
			if (PlayerSettings.virtualRealitySplashScreen != null)
			{
				UnityEngine.Debug.LogWarning("Virtual Reality Splash Screen (in Player Settings) is active. It would be displayed after the system splash screen, before the first game frame be rendered.");
			}
			string splashScreenAssetPath = AssetDatabase.GetAssetPath(projectConfig.systemSplashScreen);
			if (Path.GetExtension(splashScreenAssetPath).ToLower() != ".png")
			{
				throw new BuildFailedException("Invalid file format of System Splash Screen. It has to be a PNG file to be used by the Quest OS. The asset path: " + splashScreenAssetPath);
			}
			else
			{
				string sourcePath = splashScreenAssetPath;
				string targetFolder = Path.Combine(path, "src/main/assets");
				string targetPath = targetFolder + "/vr_splash.png";
				UnityEngine.Debug.LogFormat("Copy splash screen asset from {0} to {1}", sourcePath, targetPath);
				try
				{
					File.Copy(sourcePath, targetPath, true);
				}
				catch(Exception e)
				{
					throw new BuildFailedException(e.Message);
				}
			}
		}

		PatchAndroidManifest(path);
	}

	public void PatchAndroidManifest(string path)
	{
		string manifestFolder = Path.Combine(path, "src/main");
		string file = manifestFolder + "/AndroidManifest.xml";

		bool patchedSecurityConfig = false;
		// If Enable NSC Config, copy XML file into gradle project
		OVRProjectConfig projectConfig = OVRProjectConfig.GetProjectConfig();
		if (projectConfig != null)
		{
			if (projectConfig.enableNSCConfig)
			{
				// If no custom xml security path is specified, look for the default location in the integrations package.
				string securityConfigFile = projectConfig.securityXmlPath;
				if (string.IsNullOrEmpty(securityConfigFile))
				{
					securityConfigFile = GetOculusProjectNetworkSecConfigPath();
				}
				else
				{
					Uri configUri = new Uri(Path.GetFullPath(securityConfigFile));
					Uri projectUri = new Uri(Application.dataPath);
					Uri relativeUri = projectUri.MakeRelativeUri(configUri);
					securityConfigFile = relativeUri.ToString();
				}

				string xmlDirectory = Path.Combine(path, "src/main/res/xml");
				try
				{
					if (!Directory.Exists(xmlDirectory))
					{
						Directory.CreateDirectory(xmlDirectory);
					}
					File.Copy(securityConfigFile, Path.Combine(xmlDirectory, "network_sec_config.xml"), true);
					patchedSecurityConfig = true;
				}
				catch (Exception e)
				{
					UnityEngine.Debug.LogError(e.Message);
				}
			}
		}

		OVRManifestPreprocessor.PatchAndroidManifest(file, enableSecurity: patchedSecurityConfig);
	}

	private static string GetOculusProjectNetworkSecConfigPath()
	{
		var so = ScriptableObject.CreateInstance(typeof(OVRPluginUpdaterStub));
		var script = MonoScript.FromScriptableObject(so);
		string assetPath = AssetDatabase.GetAssetPath(script);
		string editorDir = Directory.GetParent(assetPath).FullName;
		string configAssetPath = Path.GetFullPath(Path.Combine(editorDir, "network_sec_config.xml"));
		Uri configUri = new Uri(configAssetPath);
		Uri projectUri = new Uri(Application.dataPath);
		Uri relativeUri = projectUri.MakeRelativeUri(configUri);

		return relativeUri.ToString();
	}

	public void OnPostprocessBuild(BuildReport report)
	{
#if UNITY_ANDROID
		if(autoIncrementVersion)
		{
			if((report.summary.options & BuildOptions.Development) == 0)
			{
				PlayerSettings.Android.bundleVersionCode++;
				UnityEngine.Debug.Log("Incrementing version code to " + PlayerSettings.Android.bundleVersionCode);
			}
		}

		bool isExporting = true;
		foreach (var step in report.steps)
		{
			if (step.name.Contains("Compile scripts")
				|| step.name.Contains("Building scenes")
				|| step.name.Contains("Writing asset files")
				|| step.name.Contains("Preparing APK resources")
				|| step.name.Contains("Creating Android manifest")
				|| step.name.Contains("Processing plugins")
				|| step.name.Contains("Exporting project")
				|| step.name.Contains("Building Gradle project"))
			{
				OculusBuildApp.SendBuildEvent("build_step_" + step.name.ToLower().Replace(' ', '_'),
					step.duration.TotalSeconds.ToString(), "ovrbuild");
#if BUILDSESSION
				UnityEngine.Debug.LogFormat("build_step_" + step.name.ToLower().Replace(' ', '_') + ": {0}", step.duration.TotalSeconds.ToString());
#endif
				if(step.name.Contains("Building Gradle project"))
				{
					isExporting = false;
				}
			}
		}
		OVRPlugin.AddCustomMetadata("build_step_count", report.steps.Length.ToString());
		if (report.summary.outputPath.Contains("apk")) // Exclude Gradle Project Output
		{
			var fileInfo = new System.IO.FileInfo(report.summary.outputPath);
			OVRPlugin.AddCustomMetadata("build_output_size", fileInfo.Length.ToString());
		}
#endif
		if (!report.summary.outputPath.Contains("OVRGradleTempExport"))
		{
			OculusBuildApp.SendBuildEvent("build_complete", (System.DateTime.Now - buildStartTime).TotalSeconds.ToString(), "ovrbuild");
#if BUILDSESSION
			UnityEngine.Debug.LogFormat("build_complete: {0}", (System.DateTime.Now - buildStartTime).TotalSeconds.ToString());
#endif
		}

#if UNITY_ANDROID
		if (!isExporting)
		{
			// Get the hosts path to Android SDK
			if (adbTool == null)
			{
				adbTool = new OVRADBTool(OVRConfig.Instance.GetAndroidSDKPath(false));
			}

			if (adbTool.isReady)
			{
				// Check to see if there are any ADB devices connected before continuing.
				List<string> devices = adbTool.GetDevices();
				if(devices.Count == 0)
				{
					return;
				}

				// Clear current logs on device
				Process adbClearProcess;
				adbClearProcess = adbTool.RunCommandAsync(new string[] { "logcat --clear" }, null);

				// Add a timeout if we cannot get a response from adb logcat --clear in time.
				Stopwatch timeout = new Stopwatch();
				timeout.Start();
				while (!adbClearProcess.WaitForExit(100))
				{
					if (timeout.ElapsedMilliseconds > 2000)
					{
						adbClearProcess.Kill();
						return;
					}
				}

				// Check if existing ADB process is still running, kill if needed
				if (adbProcess != null && !adbProcess.HasExited)
				{
					adbProcess.Kill();
				}

				// Begin thread to time upload and install
				var thread = new Thread(delegate ()
				{
					TimeDeploy();
				});
				thread.Start();
			}
		}
#endif
	}

#if UNITY_ANDROID
	public bool WaitForProcess;
	public bool TransferStarted;
	public DateTime UploadStart;
	public DateTime UploadEnd;
	public DateTime InstallEnd;

	public void TimeDeploy()
	{
		if (adbTool != null)
		{
			TransferStarted = false;
			DataReceivedEventHandler outputRecieved = new DataReceivedEventHandler(
				(s, e) =>
				{
					if (e.Data != null && e.Data.Length != 0 && !e.Data.Contains("\u001b"))
					{
						if (e.Data.Contains("free_cache"))
						{
							// Device recieved install command and is starting upload
							UploadStart = System.DateTime.Now;
							TransferStarted = true;
						}
						else if (e.Data.Contains("Running dexopt"))
						{
							// Upload has finished and Package Manager is starting install
							UploadEnd = System.DateTime.Now;
						}
						else if (e.Data.Contains("dex2oat took"))
						{
							// Package Manager finished install
							InstallEnd = System.DateTime.Now;
							WaitForProcess = false;
						}
						else if (e.Data.Contains("W PackageManager"))
						{
							// Warning from Package Manager is a failure in the install process
							WaitForProcess = false;
						}
					}
				}
			);

			WaitForProcess = true;
			adbProcess = adbTool.RunCommandAsync(new string[] { "logcat" }, outputRecieved);

			Stopwatch transferTimeout = new Stopwatch();
			transferTimeout.Start();
			while (adbProcess != null && !adbProcess.WaitForExit(100))
			{
				if (!WaitForProcess)
				{
					adbProcess.Kill();
					float UploadTime = (float)(UploadEnd - UploadStart).TotalMilliseconds / 1000f;
					float InstallTime = (float)(InstallEnd - UploadEnd).TotalMilliseconds / 1000f;

					if (UploadTime > 0f)
					{
						OculusBuildApp.SendBuildEvent("deploy_task", UploadTime.ToString(), "ovrbuild");
					}
					if (InstallTime > 0f)
					{
						OculusBuildApp.SendBuildEvent("install_task", InstallTime.ToString(), "ovrbuild");
					}
				}

				if (!TransferStarted && transferTimeout.ElapsedMilliseconds > 5000)
				{
					adbProcess.Kill();
				}
			}
		}
	}
#endif
}
