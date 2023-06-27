using UnityEditor;
using UnityEditor.XR.Interaction.Toolkit;

[CustomEditor(typeof(XRJoystick))]
public class XRJoystickEditor : XRBaseInteractableEditor
{
    private SerializedProperty rateOfChange = null;
    private SerializedProperty leverType = null;
    private SerializedProperty handle = null;

    private SerializedProperty onXValueChange = null;
    private SerializedProperty onYValueChange = null;

    protected override void OnEnable()
    {
        base.OnEnable();

        rateOfChange = serializedObject.FindProperty("rateOfChange");
        leverType = serializedObject.FindProperty("leverType");
        handle = serializedObject.FindProperty("handle");

        onXValueChange = serializedObject.FindProperty("OnXValueChange");
        onYValueChange = serializedObject.FindProperty("OnYValueChange");
    }

    protected override void DrawCoreConfiguration()
    {
        base.DrawCoreConfiguration();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Joystick Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(rateOfChange);
        EditorGUILayout.PropertyField(leverType);
        EditorGUILayout.PropertyField(handle);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Joystick Events", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(onXValueChange);
        EditorGUILayout.PropertyField(onYValueChange);
    }
}
