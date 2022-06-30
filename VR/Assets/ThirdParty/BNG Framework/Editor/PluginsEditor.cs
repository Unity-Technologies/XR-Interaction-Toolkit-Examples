using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;

[InitializeOnLoad]
public class PluginsEditor : EditorWindow {

    static ListRequest ListRequest;
    static AddRequest AddRequest;

    static bool LoadingData = false;

    static bool UsingOculusXR = false;
    static bool UsingOculusAndroid = false;
    static bool UsingOculusDesktop = false;
    static bool UsingOpenVRDesktop = false;
    static bool UsingOpenXR = false;
    static bool UsingXRManagement = false;

    static bool UsingURP = false;
    static bool UsingHDRP = false;

    static bool IsUnity2019 = false;
    static bool IsUnity2020 = false;
    static bool IsUnity2021 = false;

    static bool InstallingOpenXR = false;

    public static PluginsEditor Instance { get; private set; }
    public static bool IsOpen {
        get { return Instance != null; }
    }

    static bool DoCheckFirstRun = true;
    static bool FirstRun = true;

    static bool ShowedFirstRunWindow = false;

    static Texture logo;
    static GUIStyle rt;

    static PluginsEditor() {
        if(DoCheckFirstRun) {
            EditorApplication.update += CheckFirstRun;
        }
    }

    static void CheckFirstRun() {

        // Only call this once
        EditorApplication.update -= CheckFirstRun;

        // Open Window on first load
        FirstRun = !EditorPrefs.HasKey("FirstRun");
        if (FirstRun) {
            DoFirstRun();
        }
    }

    void OnEnable() { 
        Instance = this;

        logo = Resources.Load("v_64") as Texture;

#if UNITY_2019_4_OR_NEWER
        IsUnity2019 = true;
#endif

#if UNITY_2020_0_OR_NEWER
        IsUnity2019 = false;
        IsUnity2020 = true;
#endif
#if UNITY_2021_0_OR_NEWER
        IsUnity2020 = false;
        IsUnity2021 = true;
#endif
    }

    public static void DoFirstRun() {

        EditorPrefs.SetBool("FirstRun", true);        

        ShowedFirstRunWindow = true;

        PluginsEditor window = (PluginsEditor)GetWindow(typeof(PluginsEditor));
        window.Show();

        FirstRun = false;
    }

    [MenuItem("VRIF/VRIF XR Plugins Helper")]
    public static void ShowWindow() {

        const int width = 600;
        const int height = 440;

        var x = (Screen.currentResolution.width - width) / 2;
        var y = (Screen.currentResolution.height - height) / 2;

        GetWindow<PluginsEditor>("Plugins Helper").position = new Rect(x, y, width, height);

        RefreshWindow(); 
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    static void OnScriptsReloaded() {
       if(IsOpen) {
            RefreshWindow();
        }
    }

    void OnGUI() {

        // Sanity check on rich text style
        if(rt == null) {
            rt = new GUIStyle(EditorStyles.label);
            rt.richText = true;
        }

        // Logo / Info
        GUILayout.BeginHorizontal();

        if (logo) {
            GUILayout.Label(logo);
        }

        GUILayout.Label("\n<b>Welcome to the VR Interaction Framework!</b> \nBelow is a list of XR-related packages and their current installation status. \n",  rt);

        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();


        if (LoadingData) {
            EditorGUILayout.LabelField("<i>Loading plugin info...</i>", rt);
            return;
        }

        // First Time Check
        if(ShowedFirstRunWindow) {
            EditorGUILayout.HelpBox("This appears to be your fist time installing VRIF - awesome! Be sure to check out the Wiki link listed below for installation instructions and documentation.", MessageType.Info);
            EditorGUILayout.Separator();
        }

        CheckNoPluginsInstalled();

        GUILayout.Label("Installed XR Plugin Packages : ", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Oculus XR Plugin : " + GetLabel(UsingOculusXR), rt);

        AddOpenXRContent();

        // XR Management
        EditorGUILayout.LabelField("XR Management : " + GetLabel(UsingXRManagement), rt);

        EditorGUILayout.Separator();

        GUILayout.Label("Installed Legacy Packages : ", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Oculus Android Package : " + GetLabel(UsingOculusAndroid), rt);
        EditorGUILayout.LabelField("Oculus Desktop Package : " + GetLabel(UsingOculusDesktop), rt);
        EditorGUILayout.LabelField("OpenVR Desktop Package : " + GetLabel(UsingOpenVRDesktop), rt);

        EditorGUILayout.Separator();

        // Warning about not having Render Pipeline set
        if ((UsingURP || UsingHDRP) && UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset == null) {
            GUILayout.Label("Additional Info : ", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("WARNING! No Render Pipeline has been set, but you have a Render Pipeline plugin installed. Go to Project Settings -> Graphics and verify your settings.", MessageType.Warning);

            EditorGUILayout.LabelField("URP : " + GetLabel(UsingURP), rt);
            EditorGUILayout.LabelField("HDRP : " + GetLabel(UsingHDRP), rt);
        }
        
        EditorGUILayout.Separator();

        // Useful Links
        GUILayout.Label("Useful Links : ", EditorStyles.boldLabel);

        AddLink("VRIF Wiki", "https://wiki.beardedninjagames.com");
        AddLink("VRIF Asset", "http://u3d.as/1JpA");
        AddLink("VRIF Discord", "https://discord.gg/BFauBCj");

        EditorGUILayout.Separator();

        AddLink("Pico SDK", "https://developer.pico-interactive.com/sdk");
        AddLink("Oculus Integration Asset", "https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022");
        AddLink("SteamVR SDK", "https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647");
    }

    static void AddLink(string label, string url) {
        if (GUILayout.Button(label, EditorStyles.linkLabel)) {
            Application.OpenURL(url);
        }
    }

    static string OpenXRMessage;

    public static void DrawUILine(Color color, int thickness = 1, int padding = 10) {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding / 2;
        r.x -= 2;
        r.width += 6;
        EditorGUI.DrawRect(r, color);
    }

    static void CheckNoPluginsInstalled() {
        // Not using any plugins
        bool noPluginsInstalled = !UsingOculusXR && !UsingOculusAndroid && !UsingOculusDesktop && !UsingOpenVRDesktop && !UsingOpenXR;
        if (noPluginsInstalled) {
            EditorGUILayout.HelpBox("WARNING! No XR plugin packages have been detected. You need at least one XR Plugin installed for your device to function properly. You can disregard this message if you are using a plugin not listed below.", MessageType.Warning);
            EditorGUILayout.Separator();
        }
    }

    static void AddOpenXRContent() {

        GUIStyle rt = new GUIStyle(EditorStyles.label);
        rt.richText = true;

        // 2019 Open XR
        if (IsUnity2019) {

            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("OpenXR :  " + GetLabel(UsingOpenXR) + "  <size=11>(<i>Unity 2020+</i>)</size>", rt);

            if (InstallingOpenXR) {
                EditorGUILayout.LabelField(" <i>Installing OpenXR...</i>", rt);
            }
            // OpenXR is built-in to Unity in 2021. Removing this for now as the user can just install from the package manager.
            //else if (UsingOpenXR == false && GUILayout.Button("Install OpenXR Plugin", EditorStyles.miniButton)) {
            //    InstallOpenXR();
            //}

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(OpenXRMessage)) {
                EditorGUILayout.HelpBox(OpenXRMessage, MessageType.Warning);
            }
        }
        else {
            if (UsingOpenXR) {
                EditorGUILayout.LabelField("OpenXR :  " + GetLabel(UsingOpenXR), rt);
            }
            else {
                // Add install button
                if (IsUnity2020) {
                    EditorGUILayout.LabelField("OpenXR :  " + GetLabel(UsingOpenXR), rt);

                    if (GUILayout.Button("Install OpenXR Plugin")) {
                        InstallOpenXR();
                    }
                }
                else if (IsUnity2021) {
                    // 2021 is handled internally
                    EditorGUILayout.LabelField("OpenXR :  (install from within XR-Management)", rt);
                }
            }
        }
    }
    
    static void RefreshWindow() {
        if (!LoadingData) {
            LoadingData = true;

            // Get Currently installed packages
            ListRequest = Client.List();
            EditorApplication.update += ListProgress;
        }
    }

    static string GetLabel(bool active) {
        if(active) {
            return "<color=green><b>True</b></color>";
        }

        return "<color=gray><b>False</b></color>"; 
    }

    static void ListProgress() {
        if (ListRequest.IsCompleted) {
            if (ListRequest.Status == StatusCode.Success) {
                foreach (var package in ListRequest.Result) {
                    if(package.name == "com.unity.xr.oculus") {
                        UsingOculusXR = true;
                    }
                    else if (package.name == "com.unity.xr.openxr") {
                        UsingOpenXR = true;
                    }
                    else if (package.name == "com.unity.xr.oculus.android") {
                        UsingOculusAndroid = true;
                    }
                    else if (package.name == "com.unity.xr.oculus.standalone") {
                        UsingOculusDesktop = true;
                    }
                    else if (package.name == "com.unity.xr.openvr.standalone") {
                        UsingOpenVRDesktop = true;
                    }
                    else if (package.name == "com.unity.xr.management") {
                        UsingXRManagement = true;
                    }
                    else if(package.name.Contains("render-pipelines.universal")) {
                        UsingURP = true;
                    }
                    else if (package.name.Contains("render-pipelines.high-definition")) {
                        UsingHDRP = true;
                    }
                }
            }
            else if (ListRequest.Status >= StatusCode.Failure) {
                Debug.Log(ListRequest.Error.message);
            }

            LoadingData = false;

            EditorApplication.update -= ListProgress;
        }
    }

    static void AddProgress() {
        if (AddRequest.IsCompleted) {

            if (AddRequest.Status == StatusCode.Success) {
                OpenXRMessage = "Successfully Installed";
            }
            else if (AddRequest.Status >= StatusCode.Failure) {
                OpenXRMessage = AddRequest.Error.message;
            }

            InstallingOpenXR = false;

            EditorApplication.update -= AddProgress;
        }
    }

    static void InstallOpenXR() {
        InstallingOpenXR = true;

        AddRequest = Client.Add("com.unity.xr.openxr");
        EditorApplication.update += AddProgress;
    }
}
