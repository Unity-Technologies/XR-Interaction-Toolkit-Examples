// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUIUtility;

namespace Meta.Utilities.Narrative
{
    [CustomPropertyDrawer(typeof(TaskSequence.TaskDefinition))]
    public class TaskDefinitionDrawer : PropertyDrawer
    {
        private readonly SerializedProperty[] m_props = new SerializedProperty[3];

        private SerializedProperty m_initialisedForProperty;
        private SerializedProperty m_idProp, m_startingPrereqsProp, m_completionPrereqsProp;
        private SerializedProperty m_sequenceProp;

        private int m_taskIndex = -1;
        private bool m_showPrerequisiteAlert;

        private void Initialise(SerializedProperty property)
        {
            if (m_initialisedForProperty == property) { return; }

            m_initialisedForProperty = property;

            m_idProp = property.FindPropertyRelative(nameof(TaskSequence.TaskDefinition.ID));

            m_startingPrereqsProp = property.FindPropertyRelative("m_startingPrerequisites");
            m_completionPrereqsProp = property.FindPropertyRelative("m_completionPrerequisites");

            m_sequenceProp
                = property.FindPropertyRelative(nameof(TaskSequence.TaskDefinition.Sequence));

            m_props[0] = m_idProp;
            m_props[1] = m_startingPrereqsProp;
            m_props[2] = m_completionPrereqsProp;

            var path = property.propertyPath;

            if (!path.EndsWith(']'))
            {
                m_taskIndex = -1;
            }
            else
            {
                var openIndex = path.LastIndexOf('[');
                m_taskIndex = int.Parse(path.Substring(openIndex + 1, path.Length - openIndex - 2));
            }

            m_showPrerequisiteAlert = m_startingPrereqsProp.arraySize == 0 && m_taskIndex > 0;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Initialise(property);

            if (!property.isExpanded) { return singleLineHeight + 2f; }

            return m_props.Sum(EditorGUI.GetPropertyHeight)   // main properties
                   + standardVerticalSpacing * m_props.Length // spacing
                   + (m_showPrerequisiteAlert ? singleLineHeight + standardVerticalSpacing : 0)
                   + singleLineHeight                         // handler reference
                   + 4f;                                      // padding
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialise(property);

            _ = EditorGUI.BeginProperty(position, label, property);

            position.xMin += 6f;
            position.height = singleLineHeight;
            position.y += 1f;

            var foldoutRect = position;

            if (property.isExpanded) { foldoutRect.width = 110f; }

            var foldoutLabel = property.isExpanded
                ? $"#{m_taskIndex}"
                : $"#{m_taskIndex}   {m_idProp.stringValue}";

            property.isExpanded
                = EditorGUI.BeginFoldoutHeaderGroup(foldoutRect, property.isExpanded,
                                                    new GUIContent(foldoutLabel));

            EditorGUI.EndFoldoutHeaderGroup();

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            // draw ID field
            position.height = EditorGUI.GetPropertyHeight(m_idProp);

            var idRect = position;
            idRect.xMin += 79f;

            DrawIDField(idRect);

            var oldLabelWidth = labelWidth;
            labelWidth = 96f;

            position.xMin += 15f;
            position.y += position.height + standardVerticalSpacing + 1f;

            // draw task handler reference
            position.height = singleLineHeight;

            using (new EditorGUI.DisabledScope(true))
            {
                _ = EditorGUI.ObjectField(position, "Task Handler",
                                      TaskManager.HandlerForTask(m_idProp.stringValue),
                                      typeof(TaskHandler), true);
            }

            position.y += position.height + standardVerticalSpacing;

            labelWidth = oldLabelWidth;

            if (m_showPrerequisiteAlert) { DrawStartingPrerequisiteAlert(ref position); }

            // draw prerequisite fields
            position.height = EditorGUI.GetPropertyHeight(m_startingPrereqsProp);
            _ = EditorGUI.PropertyField(position, m_startingPrereqsProp);
            position.y += position.height + standardVerticalSpacing;

            position.height = EditorGUI.GetPropertyHeight(m_completionPrereqsProp);
            _ = EditorGUI.PropertyField(position, m_completionPrereqsProp);
            position.y += position.height + standardVerticalSpacing;

            EditorGUI.EndProperty();
        }

        private void DrawIDField(Rect position)
        {
            var oldLabelWidth = labelWidth;
            labelWidth = 32f;

            var idLabel = new GUIContent("ID");

            _ = EditorGUI.BeginProperty(position, idLabel, m_idProp);

            var newID = EditorGUI.DelayedTextField(position, idLabel, m_idProp.stringValue);

            if (newID != m_idProp.stringValue)
            {
                if (!TaskManager.IDIsDefined(newID))
                {
                    if (!string.IsNullOrWhiteSpace(newID)
                        && TaskManager.HandlerForTask(m_idProp.stringValue) is { } taskHandler)
                    {
                        Undo.RecordObject(taskHandler, "Change Task ID on Handler");
                        taskHandler.TaskID = newID;
                    }

                    m_idProp.stringValue = newID;
                }

                if (!string.IsNullOrWhiteSpace(newID)) { TaskManager.RefreshIDSets(); }
            }

            EditorGUI.EndProperty();
            labelWidth = oldLabelWidth;
        }

        private void DrawStartingPrerequisiteAlert(ref Rect position)
        {
            position.height = singleLineHeight;

            var prereqAlertRect = position;
            prereqAlertRect.width = prereqAlertRect.width * .5f - 1f;
            EditorGUI.LabelField(prereqAlertRect, "No starting prerequisites!");
            prereqAlertRect.x += prereqAlertRect.width + 2f;

            var sequence = m_sequenceProp.objectReferenceValue as TaskSequence;
            EditorGUI.BeginDisabledGroup(!sequence);

            if (GUI.Button(prereqAlertRect, "Add preceding task ID"))
            {
                Undo.RecordObject(sequence, "Add preceding task as prerequisite");

                var id = sequence!.TaskDefinitions[m_taskIndex - 1].ID;
                m_startingPrereqsProp.InsertArrayElementAtIndex(0);
                var prereqElement = m_startingPrereqsProp.GetArrayElementAtIndex(0);
                prereqElement.FindPropertyRelative(nameof(TaskID.ID)).stringValue = id;

                m_startingPrereqsProp.isExpanded = true;
            }

            EditorGUI.EndDisabledGroup();

            position.y += position.height + standardVerticalSpacing;
        }
    }
}