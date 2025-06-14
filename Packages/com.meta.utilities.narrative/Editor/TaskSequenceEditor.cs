// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEditor;


namespace Meta.Utilities.Narrative
{
    [CustomEditor(typeof(TaskSequence))]
    public class TaskSequenceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            TaskManagerEditor.LayoutTaskManagerInstanceField();

            if (TaskManager.Instance
                && !TaskManager.Instance.Sequences.Contains(target as TaskSequence))
            {
                EditorGUILayout.HelpBox("This sequence is not included in the "
                                        + "Task Manager asset's sequence list!", MessageType.Info);
            }

            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }
    }
}