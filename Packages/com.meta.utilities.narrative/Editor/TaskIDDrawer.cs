// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEditor;
using UnityEngine;

using static UnityEditor.EditorGUI;
using static UnityEditor.EditorGUIUtility;


namespace Meta.Utilities.Narrative
{
    [CustomPropertyDrawer(typeof(TaskID))]
    public class TaskIDDrawer : PropertyDrawer
    {
        private static bool s_contextMenuCallbackRegistered;

        private SerializedProperty m_initialisedForProperty;
        private SerializedProperty m_idProp;

        private int m_idIndex = -1;

        private void Initialise(SerializedProperty property)
        {
            if (!s_contextMenuCallbackRegistered)
            {

                EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
                s_contextMenuCallbackRegistered = true;
            }

            if (m_initialisedForProperty == property) { return; }

            m_initialisedForProperty = property;
            m_idProp = property.FindPropertyRelative(nameof(TaskID.ID));

            var path = property.propertyPath;

            if (!path.EndsWith(']')) { return; }

            var openIndex = path.LastIndexOf('[');
            m_idIndex = int.Parse(path.Substring(openIndex + 1, path.Length - openIndex - 2));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialise(property);

            var oldLabelWidth = labelWidth;

            if (m_idIndex >= 0)
            {
                label = new GUIContent($"#{m_idIndex}");
                labelWidth = 32f;
            }

            _ = BeginProperty(position, label, property);

            position = PrefixLabel(position, GUIUtility.GetControlID(FocusType.Keyboard), label);

            position.height = EditorGUI.GetPropertyHeight(m_idProp);

            if (TaskManager.Instance)
            {
                if (DrawTaskIDPopupChanged(position, m_idProp.stringValue, out var newVal))
                {
                    m_idProp.stringValue = newVal;
                }
            }
            else
            {
                m_idProp.stringValue = TextField(position, m_idProp.stringValue);
            }

            if (m_idIndex >= 0) { labelWidth = oldLabelWidth; }

            EndProperty();
        }

        private static bool DrawTaskIDPopupChanged(Rect position, string currID, out string newID)
        {
            var currIndex = TaskManager.GetPopupIndexForID(currID);
            newID = currID;

            if (currIndex < 0)
            {
                if (!DrawUndefinedIDControl()) { return false; }

                newID = null;
                return true;
            }

            var newIndex = Popup(position, currIndex, TaskManager.TaskIDDisplayPopupOptions);

            if (newIndex == currIndex) { return false; }

            newID = TaskManager.GetIDFromPopupIndex(newIndex);
            return true;

            bool DrawUndefinedIDControl()
            {
                var oldSetting = EditorStyles.label.richText;
                EditorStyles.label.richText = true;

                const float BUTTON_WIDTH = 104f;

                var labelRect = position;
                labelRect.width -= BUTTON_WIDTH + 2f;

                var oldLabelWidth = labelWidth;
                labelWidth = 108;

                var colorHex = ColorUtility.ToHtmlStringRGBA(new Color(1f, 0.5f, 0.2f));
                LabelField(labelRect, $"<color=#{colorHex}>Undefined ID:</color> {currID}");

                EditorStyles.label.richText = oldSetting;

                var buttonRect = position;
                buttonRect.xMin = buttonRect.xMax - BUTTON_WIDTH;

                labelWidth = oldLabelWidth;

                return GUI.Button(buttonRect, "Select from list");
            }
        }

        private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty prop)
        {
            if (prop.type != "TaskID") { return; }

            menu.AddItem(new GUIContent("Copy ID"), false,
                         () => systemCopyBuffer
                             = prop.FindPropertyRelative(nameof(TaskID.ID))?.stringValue);
        }
    }
}