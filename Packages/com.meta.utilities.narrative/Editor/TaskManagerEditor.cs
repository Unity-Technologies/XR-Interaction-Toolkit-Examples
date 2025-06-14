// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEditor;


namespace Meta.Utilities.Narrative
{
    [CustomEditor(typeof(TaskManager))]
    public class TaskManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            LayoutTaskManagerInstanceField();
        }

        public static void LayoutTaskManagerInstanceField()
        {
            EditorGUI.BeginDisabledGroup(true);

            _ = EditorGUILayout.ObjectField("Manager Instance", TaskManager.Instance,
                                        typeof(TaskManager), false);

            EditorGUI.EndDisabledGroup();
        }
    }
}