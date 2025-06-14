// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using static UnityEditor.EditorGUI;

namespace Meta.Utilities.Narrative
{
    [CustomPropertyDrawer(typeof(LabelWidthAttribute))]
    public class LabelWidthDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = (attribute as LabelWidthAttribute)!.Width;

            _ = PropertyField(position, property, label);

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }
    }

    [CustomPropertyDrawer(typeof(WaitForPlayableCondition), true)]
    public class WaitForPlayableConditionDrawer : TaskConditionDrawer
    {
        private bool m_showAddCallButton;

        private void Init(SerializedProperty property)
        {
            m_showAddCallButton = true;

            if (!property.isExpanded)
            {
                m_showAddCallButton = false;
                return;
            }

            var directorProp = property.FindPropertyRelative("Director");

            if (!directorProp.objectReferenceValue)
            {
                m_showAddCallButton = false;
                return;
            }

            var onTaskStartedProp = property.serializedObject.FindProperty("m_onTaskStarted");

            var persistentCallsProp = onTaskStartedProp
                                      .FindPropertyRelative("m_PersistentCalls")?
                                      .FindPropertyRelative("m_Calls");

            for (var i = 0; i < persistentCallsProp!.arraySize; i++)
            {
                var callProp = persistentCallsProp.GetArrayElementAtIndex(i);

                if (callProp.FindPropertyRelative("m_Target").objectReferenceValue
                    == directorProp.objectReferenceValue
                    && callProp.FindPropertyRelative("m_MethodName").stringValue == "Play")
                {
                    m_showAddCallButton = false;
                    return;
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Init(property);

            var height = base.GetPropertyHeight(property, label);

            if (m_showAddCallButton)
            {
                height += EditorGUIUtility.singleLineHeight
                          + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init(property);

            base.OnGUI(position, property, label);

            var btnRect = position;
            btnRect.yMin = btnRect.yMax - EditorGUIUtility.singleLineHeight - 1f;
            btnRect.y -= 6f;
            btnRect.xMin += 27f;
            btnRect.xMax -= 5f;

            if (!m_showAddCallButton
                || !GUI.Button(btnRect, "Add call to Play() in OnTaskStarted"))
            {
                return;
            }

            var group = Undo.GetCurrentGroup();

            Undo.RecordObject(property.serializedObject.targetObject,
                              "Add Play() to OnTaskStarted");

            var onTaskStartedProp = property.serializedObject.FindProperty("m_onTaskStarted");

            var persistentCallsProp = onTaskStartedProp
                                      .FindPropertyRelative("m_PersistentCalls")?
                                      .FindPropertyRelative("m_Calls");

            persistentCallsProp!.InsertArrayElementAtIndex(0);

            var callProp = persistentCallsProp.GetArrayElementAtIndex(0);

            callProp.FindPropertyRelative("m_Target").objectReferenceValue
                = property.FindPropertyRelative("Director").objectReferenceValue;

            callProp.FindPropertyRelative("m_MethodName").stringValue = "Play";

            callProp.FindPropertyRelative("m_Mode").enumValueIndex
                = (int)PersistentListenerMode.Void;

            callProp.FindPropertyRelative("m_CallState").enumValueIndex
                = (int)UnityEventCallState.RuntimeOnly;

            Undo.SetCurrentGroupName("Add Play() to OnTaskStarted");
            Undo.CollapseUndoOperations(group);
        }
    }


    [CustomPropertyDrawer(typeof(TaskCondition), true)]
    public class TaskConditionDrawer : PropertyDrawer
    {
        private const float BOX_PADDING = 4f, MARGIN = 2f, DELETE_WIDTH = 60f;

        private static GUIStyle s_richHeader;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUI.GetPropertyHeight(property, label, property.isExpanded)
               + (property.isExpanded ? BOX_PADDING * 2f + MARGIN : 1f) + 1f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            s_richHeader ??= new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontStyle = FontStyle.Normal,
                richText = true,
                clipping = TextClipping.Clip,
                padding = { right = 0 }
            };

            _ = BeginProperty(position, label, property);

            var foldoutRect = position;
            foldoutRect.height = EditorGUIUtility.singleLineHeight;
            foldoutRect.xMin += 15f * indentLevel;
            foldoutRect.width -= DELETE_WIDTH + 4f;

            property.isExpanded = BeginFoldoutHeaderGroup(foldoutRect, property.isExpanded,
                                                          label, s_richHeader);

            EndFoldoutHeaderGroup();

            if (!property.isExpanded)
            {
                EndProperty();
                return;
            }

            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth -= 12f;

            position.yMin = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;
            position.height -= MARGIN;

            var boxRect = position;
            boxRect.xMin += 15f * indentLevel - 3f;

            HelpBox(boxRect, "", MessageType.None);

            position.position += Vector2.one * BOX_PADDING;
            position.size -= Vector2.one * (BOX_PADDING * 2f);
            position.xMin += 8f;

            var rootPath = property.propertyPath;
            var iterationProp = property.Copy();
            var lineRect = position;
            var enteredChildren = false;

            while (iterationProp.NextVisible(!enteredChildren)
                   && iterationProp.propertyPath.StartsWith(rootPath))
            {
                enteredChildren = true;
                lineRect.height = EditorGUI.GetPropertyHeight(iterationProp);
                _ = PropertyField(lineRect, iterationProp);
                lineRect.y += lineRect.height + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
            EndProperty();
        }
    }
}