#if UNITY_EDITOR_WIN && UNITY_ANDROID
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.IO;

using UnityEngine;
using UnityEditor;

public class OVRBundleTool : EditorWindow
{
	private static List<EditorSceneInfo> buildableScenes;
	private static Vector2 debugLogScroll = new Vector2(0, 0);
	private static bool invalidBuildableScene;

	private static string toolLog;
	private static bool useOptionalTransitionApkPackage;
	private static GUIStyle logBoxStyle;
	private static Vector2 logBoxSize;
	private static float logBoxSpacing = 30.0f;

	private bool forceRestart = false;
	private bool showBundleManagement = false;
	private bool showOther = false;

	// Needed to ensure that APK checking does happen during editor start up, but will still happen when the window is opened/updated
	private static bool panelInitialized = false;

	private enum ApkStatus
	{
		UNKNOWN,
		OK,
		NOT_INSTALLED,
		DEVICE_NOT_CONNECTED,
	};

	public enum SceneBundleStatus
	{
		[Description("Unknown")]
		UNKNOWN,
		[Description("Queued")]
		QUEUED,
		[Description("Building")]
		BUILDING,
		[Description("Done")]
		DONE,
		[Description("Transferring")]
		TRANSFERRING,
		[Description("Deployed")]
		DEPLOYED,
	};

	public class EditorSceneInfo
	{
		public string scenePath;
		public string sceneName;
		public SceneBundleStatus buildStatus;

		public EditorSceneInfo(string path, string name)
		{
			scenePath = path;
			sceneName = name;
			buildStatus = SceneBundleStatus.UNKNOWN;
		}
	}

	private enum GuiAction
	{
		None,
		OpenBuildSettingsWindow,
		BuildAndDeployScenes,
		BuildAndDeployApp,
		ClearDeviceBundles,
		ClearLocalBundles,
		LaunchApp,
		UninstallApk,
		ClearLog,
	}

	private GuiAction action = GuiAction.None;

	private static ApkStatus currentApkStatus;

	[MenuItem("Oculus/OVR Build/OVR Scene Quick Preview %l", false, 10)]
	static void Init()
	{
		currentApkStatus = ApkStatus.UNKNOWN;

		EditorWindow.GetWindow(typeof(OVRBundleTool));

		invalidBuildableScene = false;
		InitializePanel();

		OVRPlugin.SetDeveloperMode(OVRPlugin.Bool.True);
		OVRPlugin.SendEvent("oculus_bundle_tool", "show_window");
	}

	public void OnEnable()
	{
		InitializePanel();
	}

	public static void InitializePanel()
	{
		panelInitialized = true;
		GetScenesFromBuildSettings();
		EditorBuildSettings.sceneListChanged += GetScenesFromBuildSettings;
	}

	private void OnGUI()
	{
		this.titleContent.text = "OVR Scene Quick Preview";

		if (panelInitialized)
		{
			CheckForTransitionAPK();
			panelInitialized = false;
		}

		if (logBoxStyle == null)
		{
			logBoxStyle = new GUIStyle();
			logBoxStyle.margin.left = 5;
			logBoxStyle.wordWrap = true;
			logBoxStyle.normal.textColor = logBoxStyle.focused.textColor = EditorStyles.label.normal.textColor;
			logBoxStyle.richText = true;
		}

		GUILayout.Space(10.0f);

		GUILayout.Label("Scenes", EditorStyles.boldLabel);
		GUIContent buildSettingsBtnTxt = new GUIContent("Open Build Settings");
		if (buildableScenes == null || buildableScenes.Count == 0)
		{
			string sceneErrorMessage;
			if (invalidBuildableScene)
			{
				sceneErrorMessage = "Invalid scene selection. \nPlease remove OVRTransitionScene in the project's build settings.";
			}
			else
			{
				sceneErrorMessage = "No scenes detected. \nTo get started, add scenes in the project's build settings.";
			}
			GUILayout.Label(sceneErrorMessage);

			var buildSettingBtnRt = GUILayoutUtility.GetRect(buildSettingsBtnTxt, GUI.skin.button, GUILayout.Width(150));
			if (GUI.Button(buildSettingBtnRt, buildSettingsBtnTxt))
			{
				action = GuiAction.OpenBuildSettingsWindow;
			}
		}
		else
		{
			foreach (EditorSceneInfo scene in buildableScenes)
			{
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.LabelField(scene.sceneName, GUILayout.ExpandWidth(true));
					GUILayout.FlexibleSpace();

					if (scene.buildStatus != SceneBundleStatus.UNKNOWN)
					{
						string status = GetEnumDescription(scene.buildStatus);
						EditorGUILayout.LabelField(status, GUILayout.Width(70));
					}
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();
			{
				GUIContent sceneBtnTxt = new GUIContent("Build and Deploy Scene(s)");
				var sceneBtnRt = GUILayoutUtility.GetRect(sceneBtnTxt, GUI.skin.button, GUILayout.Width(200));
				if (GUI.Button(sceneBtnRt, sceneBtnTxt))
				{
					action = GuiAction.BuildAndDeployScenes;
				}

				GUIContent forceRestartLabel = new GUIContent("Force Restart [?]", "Relaunch the application after scene bundles are finished deploying.");
				forceRestart = GUILayout.Toggle(forceRestart, forceRestartLabel, GUILayout.ExpandWidth(true));
			}
			EditorGUILayout.EndHorizontal();
		}

		GUILayout.Space(10.0f);
		GUIContent transitionContent = new GUIContent("Transition APK [?]", "Build and deploy an APK that will transition into the scene you are working on. This enables fast iteration on a specific scene.");
		GUILayout.Label(transitionContent, EditorStyles.boldLabel);

		EditorGUILayout.BeginHorizontal();
		{
			GUIStyle statusStyle = EditorStyles.label;
			statusStyle.richText = true;
			GUILayout.Label("Status: ", statusStyle, GUILayout.ExpandWidth(false));

			string statusMesssage;
			switch (currentApkStatus)
			{
				case ApkStatus.OK:
					statusMesssage = "<color=green>APK installed. Ready to build and deploy scenes.</color>";
					break;
				case ApkStatus.NOT_INSTALLED:
					statusMesssage = "<color=red>APK not installed. Press build and deploy to install the transition APK.</color>";
					break;
				case ApkStatus.DEVICE_NOT_CONNECTED:
					statusMesssage = "<color=red>Device not connected via ADB. Please connect device and allow debugging.</color>";
					break;
				case ApkStatus.UNKNOWN:
				default:
					statusMesssage = "<color=red>Failed to get APK status!</color>";
					break;
			}
			GUILayout.Label(statusMesssage, statusStyle, GUILayout.ExpandWidth(true));
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		{
			GUIContent btnTxt = new GUIContent("Build and Deploy App");
			var rt = GUILayoutUtility.GetRect(btnTxt, GUI.skin.button, GUILayout.Width(200));
			if (GUI.Button(rt, btnTxt))
			{
				action = GuiAction.BuildAndDeployApp;
			}
		}
		EditorGUILayout.EndHorizontal();

		GUILayout.Space(10.0f);
		GUILayout.Label("Utilities", EditorStyles.boldLabel);

		showBundleManagement = EditorGUILayout.Foldout(showBundleManagement, "Bundle Management");
		if (showBundleManagement)
		{
			EditorGUILayout.BeginHorizontal();
			{
				GUIContent clearDeviceBundlesTxt = new GUIContent("Delete Device Bundles");
				var clearDeviceBundlesBtnRt = GUILayoutUtility.GetRect(clearDeviceBundlesTxt, GUI.skin.button, GUILayout.ExpandWidth(true));
				if (GUI.Button(clearDeviceBundlesBtnRt, clearDeviceBundlesTxt))
				{
					action = GuiAction.ClearDeviceBundles;
				}

				GUIContent clearLocalBundlesTxt = new GUIContent("Delete Local Bundles");
				var clearLocalBundlesBtnRt = GUILayoutUtility.GetRect(clearLocalBundlesTxt, GUI.skin.button, GUILayout.ExpandWidth(true));
				if (GUI.Button(clearLocalBundlesBtnRt, clearLocalBundlesTxt))
				{
					action = GuiAction.ClearLocalBundles;
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		showOther = EditorGUILayout.Foldout(showOther, "Other");
		if (showOther)
		{
			EditorGUILayout.BeginHorizontal();
			{
				GUIContent useOptionalTransitionPackageLabel = new GUIContent("Use optional APK package name [?]",
					"This allows both full build APK and transition APK to be installed on device. However, platform services like Entitlement check may fail.");

				EditorGUILayout.LabelField(useOptionalTransitionPackageLabel, GUILayout.ExpandWidth(false));
				bool newToggleValue = EditorGUILayout.Toggle(useOptionalTransitionApkPackage);

				if (newToggleValue != useOptionalTransitionApkPackage)
				{
					useOptionalTransitionApkPackage = newToggleValue;
					// Update transition APK status after changing package name option
					CheckForTransitionAPK();
				}

			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			{
				GUIContent launchBtnTxt = new GUIContent("Launch App");
				var launchBtnRt = GUILayoutUtility.GetRect(launchBtnTxt, GUI.skin.button, GUILayout.ExpandWidth(true));
				if (GUI.Button(launchBtnRt, launchBtnTxt))
				{
					action = GuiAction.LaunchApp;
				}

				var buildSettingBtnRt = GUILayoutUtility.GetRect(buildSettingsBtnTxt, GUI.skin.button, GUILayout.ExpandWidth(true));
				if (GUI.Button(buildSettingBtnRt, buildSettingsBtnTxt))
				{
					action = GuiAction.OpenBuildSettingsWindow;
				}

				GUIContent uninstallTxt = new GUIContent("Uninstall APK");
				var uninstallBtnRt = GUILayoutUtility.GetRect(uninstallTxt, GUI.skin.button, GUILayout.ExpandWidth(true));
				if (GUI.Button(uninstallBtnRt, uninstallTxt))
				{
					action = GuiAction.UninstallApk;
				}

				GUIContent clearLogTxt = new GUIContent("Clear Log");
				var clearLogBtnRt = GUILayoutUtility.GetRect(clearLogTxt, GUI.skin.button, GUILayout.ExpandWidth(true));
				if (GUI.Button(clearLogBtnRt, clearLogTxt))
				{
					action = GuiAction.ClearLog;
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		GUILayout.Space(10.0f);
		GUILayout.Label("Log", EditorStyles.boldLabel);

		if (!string.IsNullOrEmpty(toolLog))
		{
			debugLogScroll = EditorGUILayout.BeginScrollView(debugLogScroll, GUILayout.ExpandHeight(true));
			EditorGUILayout.SelectableLabel(toolLog, logBoxStyle, GUILayout.Height(logBoxSize.y + logBoxSpacing));
			EditorGUILayout.EndScrollView();
		}
	}

	private void Update()
	{
		switch (action)
		{
			case GuiAction.OpenBuildSettingsWindow:
				OpenBuildSettingsWindow();
				break;
			case GuiAction.BuildAndDeployScenes:
				// Check the latest transition apk status
				CheckForTransitionAPK();
				// Show a dialog to prompt for building and deploying transition APK
				if (currentApkStatus != ApkStatus.OK &&
					EditorUtility.DisplayDialog("Build and Deploy OVR Transition APK?",
							"OVR Transition APK status not ready, it is required to load your scene bundle for quick preview.",
							"Yes",
							"No"))
				{
					PrintLog("Building OVR Transition APK");
					OVRBundleManager.BuildDeployTransitionAPK(useOptionalTransitionApkPackage);
					CheckForTransitionAPK();
				}

				for (int i = 0; i < buildableScenes.Count; i++)
				{
					buildableScenes[i].buildStatus = SceneBundleStatus.QUEUED;
				}
				OVRBundleManager.BuildDeployScenes(buildableScenes, forceRestart);
				break;
			case GuiAction.BuildAndDeployApp:
				OVRBundleManager.BuildDeployTransitionAPK(useOptionalTransitionApkPackage);
				CheckForTransitionAPK();
				break;
			case GuiAction.ClearDeviceBundles:
				OVRBundleManager.DeleteRemoteAssetBundles();
				break;
			case GuiAction.ClearLocalBundles:
				OVRBundleManager.DeleteLocalAssetBundles();
				break;
			case GuiAction.LaunchApp:
				OVRBundleManager.LaunchApplication();
				break;
			case GuiAction.UninstallApk:
				OVRBundleManager.UninstallAPK();
				CheckForTransitionAPK();
				break;
			case GuiAction.ClearLog:
				PrintLog("", true);
				break;
			default:
				break;
		}

		action = GuiAction.None;
	}

	private static void OpenBuildSettingsWindow()
	{
		EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
	}

	public static void UpdateSceneBuildStatus(SceneBundleStatus status, int index = -1)
	{
		if (index >= 0 && index < buildableScenes.Count)
		{
			buildableScenes[index].buildStatus = status;
		}
		else
		{
			// Update status for all scenes
			for (int i = 0; i < buildableScenes.Count; i++)
			{
				buildableScenes[i].buildStatus = status;
			}
		}
	}

	private static void GetScenesFromBuildSettings()
	{
		invalidBuildableScene = false;
		buildableScenes = new List<EditorSceneInfo>();
		for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
		{
			EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
			if (scene.enabled)
			{
				if (Path.GetFileNameWithoutExtension(scene.path) != "OVRTransitionScene")
				{
					EditorSceneInfo sceneInfo = new EditorSceneInfo(scene.path, Path.GetFileNameWithoutExtension(scene.path));
					buildableScenes.Add(sceneInfo);
				}
				else
				{
					buildableScenes = null;
					invalidBuildableScene = true;
					return;
				}
			}
		}
	}

	private static void CheckForTransitionAPK()
	{
		OVRADBTool adbTool = new OVRADBTool(OVRConfig.Instance.GetAndroidSDKPath());
		if (adbTool.isReady)
		{
			string matchedPackageList, error;
			var transitionPackageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
			if (useOptionalTransitionApkPackage)
			{
				transitionPackageName += ".transition";
			}
			string[] packageCheckCommand = new string[] { "-d shell pm list package", transitionPackageName };
			if (adbTool.RunCommand(packageCheckCommand, null, out matchedPackageList, out error) == 0)
			{
				if (string.IsNullOrEmpty(matchedPackageList))
				{
					currentApkStatus = ApkStatus.NOT_INSTALLED;
				}
				else
				{
					// adb "list package" command returns all package names that contains the given query package name
					// Need to check if the transition package name is matched exactly
					if (matchedPackageList.Contains("package:" + transitionPackageName + "\r\n"))
					{
						if (useOptionalTransitionApkPackage)
						{
							// If optional package name is used, it is deterministic that the transition apk is installed
							currentApkStatus = ApkStatus.OK;
						}
						else
						{
							// get package info to check for TRANSITION_APK_VERSION_NAME
							string[] dumpPackageInfoCommand = new string[] { "-d shell dumpsys package", transitionPackageName };
							string packageInfo;
							if (adbTool.RunCommand(dumpPackageInfoCommand, null, out packageInfo, out error) == 0 &&
									!string.IsNullOrEmpty(packageInfo) &&
									packageInfo.Contains(OVRBundleManager.TRANSITION_APK_VERSION_NAME))
							{
								// Matched package name found, and the package info contains TRANSITION_APK_VERSION_NAME
								currentApkStatus = ApkStatus.OK;
							}
							else
							{
								currentApkStatus = ApkStatus.NOT_INSTALLED;
							}
						}
					}
					else
					{
						// No matached package name returned
						currentApkStatus = ApkStatus.NOT_INSTALLED;
					}
				}
			}
			else if (error.Contains("no devices found"))
			{
				currentApkStatus = ApkStatus.DEVICE_NOT_CONNECTED;
			}
			else
			{
				currentApkStatus = ApkStatus.UNKNOWN;
			}
		}
	}

	public static void PrintLog(string message, bool clear = false)
	{
		if (clear)
		{
			toolLog = message;
		}
		else
		{
			toolLog += message + "\n";
		}

		GUIContent logContent = new GUIContent(toolLog);
		logBoxSize = logBoxStyle.CalcSize(logContent);

		debugLogScroll.y = logBoxSize.y + logBoxSpacing;
	}

	public static void PrintError(string error = "")
	{
		if(!string.IsNullOrEmpty(error))
		{
			toolLog += "<color=red>Failed!\n</color>" + error + "\n";
		}
		else
		{
			toolLog += "<color=red>Failed! Check Unity log for more details.\n</color>";
		}
	}

	public static void PrintWarning(string warning)
	{
		toolLog += "<color=yellow>Warning!\n" + warning + "</color>\n";
	}

	public static void PrintSuccess()
	{
		toolLog += "<color=green>Success!</color>\n";
	}

	public static string GetEnumDescription(Enum eEnum)
	{
		Type enumType = eEnum.GetType();
		MemberInfo[] memberInfo = enumType.GetMember(eEnum.ToString());
		if (memberInfo != null && memberInfo.Length > 0)
		{
			var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
			if (attrs != null && attrs.Length > 0)
			{
				return ((DescriptionAttribute)attrs[0]).Description;
			}
		}
		return eEnum.ToString();
	}

	public static bool GetUseOptionalTransitionApkPackage()
	{
		return useOptionalTransitionApkPackage;
	}
}
#endif
