using UnityEditor;
using UnityEditor.XR.Interaction.Toolkit;

[CustomEditor(typeof(XRSlider))]
public class XRSliderEditor : XRBaseInteractableEditor
{
    private SerializedProperty handle = null;
    private SerializedProperty start = null;
    private SerializedProperty end = null;
    private SerializedProperty defaultValue = null;

    private SerializedProperty onValueChange = null;

    protected override void OnEnable()
    {
        base.OnEnable();

        handle = serializedObject.FindProperty("handle");
        start = serializedObject.FindProperty("start");
        end = serializedObject.FindProperty("end");
        defaultValue = serializedObject.FindProperty("defaultValue");

        onValueChange = serializedObject.FindProperty("OnValueChange");
    }

    protected override void DrawCoreConfiguration()
    {
        base.DrawCoreConfiguration();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Slider Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(handle);
        EditorGUILayout.PropertyField(start);
        EditorGUILayout.PropertyField(end);
        EditorGUILayout.PropertyField(defaultValue);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Slider Event", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(onValueChange);
    }
}
