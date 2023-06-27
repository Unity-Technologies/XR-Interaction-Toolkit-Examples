using UnityEditor;
using UnityEditor.XR.Interaction.Toolkit;

[CustomEditor(typeof(XRKnob))]
public class XRKnobEditor : XRBaseInteractableEditor
{
    private SerializedProperty knobTransform = null;
    private SerializedProperty minimum = null;
    private SerializedProperty maximum = null;
    private SerializedProperty defaultValue = null;

    private SerializedProperty onValueChange = null;

    protected override void OnEnable()
    {
        base.OnEnable();

        knobTransform = serializedObject.FindProperty("knobTransform");
        minimum = serializedObject.FindProperty("minimum");
        maximum = serializedObject.FindProperty("maximum");
        defaultValue = serializedObject.FindProperty("defaultValue");

        onValueChange = serializedObject.FindProperty("OnValueChange");
    }

    protected override void DrawCoreConfiguration()
    {
        base.DrawCoreConfiguration();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Knob Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(knobTransform);
        EditorGUILayout.PropertyField(minimum);
        EditorGUILayout.PropertyField(maximum);
        EditorGUILayout.PropertyField(defaultValue);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Knob Event", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(onValueChange);
    }
}
