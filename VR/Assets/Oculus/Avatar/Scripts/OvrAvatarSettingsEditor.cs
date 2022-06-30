#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using Oculus.Avatar;

[CustomEditor(typeof(OvrAvatarSettings))]
[InitializeOnLoadAttribute]
public class OvrAvatarSettingsEditor : Editor {
    GUIContent appIDLabel = new GUIContent("Oculus Rift App Id [?]", 
      "This AppID will be used for OvrAvatar registration.");

    GUIContent mobileAppIDLabel = new GUIContent("Oculus Go/Quest or Gear VR [?]", 
      "This AppID will be used when building to the Android target");

    [UnityEditor.MenuItem("Oculus/Avatars/Edit Settings")]
    public static void Edit()
    {
        var settings = OvrAvatarSettings.Instance;
        UnityEditor.Selection.activeObject = settings;
        CAPI.SendEvent("edit_settings");
    }

    static OvrAvatarSettingsEditor()
    {
#if UNITY_2017_2_OR_NEWER
        EditorApplication.playModeStateChanged += HandlePlayModeState;
#else
        EditorApplication.playmodeStateChanged += () =>
        {
            if (EditorApplication.isPlaying)
            {
                CAPI.SendEvent("load", CAPI.AvatarSDKVersion.ToString());
            }
        };
#endif
    }

#if UNITY_2017_2_OR_NEWER
    private static void HandlePlayModeState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            CAPI.SendEvent("load", CAPI.AvatarSDKVersion.ToString());
        }
    }
#endif

    private static string MakeTextBox(GUIContent label, string variable) {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label);
        GUI.changed = false;
        var result = EditorGUILayout.TextField(variable);
        if (GUI.changed)
        {
            EditorUtility.SetDirty(OvrAvatarSettings.Instance);
            GUI.changed = false;
        }
        EditorGUILayout.EndHorizontal();
        return result;
    }
    public override void OnInspectorGUI()
    {
        EditorGUILayout.BeginVertical();
        OvrAvatarSettings.AppID =
            OvrAvatarSettingsEditor.MakeTextBox(appIDLabel, OvrAvatarSettings.AppID);
        OvrAvatarSettings.MobileAppID =
            OvrAvatarSettingsEditor.MakeTextBox(mobileAppIDLabel, OvrAvatarSettings.MobileAppID);
        EditorGUILayout.EndVertical();
    }
}
#endif
