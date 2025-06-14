// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUILayout;


namespace Meta.Utilities.Narrative
{
    [CustomEditor(typeof(TaskHandler))]
    public class TaskHandlerEditor : UnityEditor.Editor
    {
        private const float DELETE_BUTTON_WIDTH = 60f;
        private const float DIVIDER_VISIBLE_WIDTH = 0.8f, DIVIDER_TOTAL_WIDTH = 5f,
                            DIVIDER_OPACITY = .2f;

        private static readonly Color[] s_stateColors =
        {
            new(1f, .4f, .3f, 1f),
            new(.7f, 1f, .1f, 1f),
            new(0f, .7f, 1f, 1f),
        };

        private static readonly Color[] s_conditionColors =
        {
            new(1f, .4f, .3f, 1f),
            new(0f, .8f, .7f, 1f)
        };

        private static Type[] s_conditionTypes;
        private static GUIStyle s_richHeader;

        private TaskHandler m_handler;

        public override void OnInspectorGUI()
        {
            if (s_conditionTypes == null)
            {
                // Gather all non-abstract types deriving from TaskCondition 
                s_conditionTypes
                    = AppDomain.CurrentDomain.GetAssemblies().SelectMany(CollectTypes).ToArray();

                static IEnumerable<Type> CollectTypes(Assembly a) => a.GetTypes().Where
                    (t => t.IsSubclassOf(typeof(TaskCondition)) && !t.IsAbstract);
            }

            InitialiseEditorData();

            m_handler = target as TaskHandler;

            var oldRichTextSetting = EditorStyles.label.richText;
            EditorStyles.label.richText = true;

            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 96f;

            using (new EditorGUI.DisabledScope(true))
            {
                _ = PropertyField(serializedObject.FindProperty("m_Script"));
            }
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Start narrative from this sequence"))
                    TaskManager.StartNarrative(m_handler.Task.Sequence);
                using (new EditorGUI.DisabledScope(m_handler.Task != null && !m_handler.Task.Current))
                {
                    if (GUILayout.Button("Skip this task"))
                        m_handler.Skip();
                }
            }
            if (m_handler!.InitializeIfNecessary()) { EditorUtility.SetDirty(m_handler); }

            LayoutDivider();
            LayoutTaskInfo();

            EditorGUIUtility.labelWidth = oldLabelWidth;

            LayoutDivider();
            _ = PropertyField(serializedObject.FindProperty(nameof(TaskHandler.PlayerTransform)));
            _ = PropertyField(serializedObject.FindProperty(nameof(TaskHandler.PlayerGazeCamera)));

            LayoutDivider();
            LayoutCompletionConditions();

            LayoutDivider();
            LabelField("Events to trigger", EditorStyles.boldLabel);
            LayoutExpandableUnityEvent(serializedObject.FindProperty("m_onTaskStarted"));
            LayoutExpandableUnityEvent(serializedObject.FindProperty("m_onTaskCompleted"));
            LayoutExpandableUnityEvent(serializedObject.FindProperty("m_onReminder"));
            _ = PropertyField(serializedObject.FindProperty("m_reminderInterval"));

            _ = serializedObject.ApplyModifiedProperties();

            EditorStyles.label.richText = oldRichTextSetting;
        }

        private static void LayoutDivider()
        {
            const float PADDING = .5f * (DIVIDER_TOTAL_WIDTH - DIVIDER_VISIBLE_WIDTH);

            var rect = GetControlRect(false, DIVIDER_TOTAL_WIDTH);

            rect.yMin += PADDING;
            rect.yMax -= PADDING;
            rect.xMin -= 20f;
            rect.xMax += 5f;

            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, DIVIDER_OPACITY));
        }

        private static void LayoutExpandableUnityEvent(SerializedProperty property)
        {
            if (property == null) { return; }

            if (!property.isExpanded)
            {
                var persistentCallsProp = property.FindPropertyRelative("m_PersistentCalls")?
                                                  .FindPropertyRelative("m_Calls");

                var label = $"{ObjectNames.NicifyVariableName(property.name)}   "
                            + $"(<b>{persistentCallsProp?.arraySize}</b> calls)";

                property.isExpanded = BeginFoldoutHeaderGroup(false, label, s_richHeader);
                EndFoldoutHeaderGroup();

                return;
            }

            var rect = GetControlRect(false, -2f);
            rect.height = EditorGUIUtility.singleLineHeight;

            property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(rect, true, "");
            EndFoldoutHeaderGroup();

            _ = PropertyField(property);
        }

        private void LayoutTaskInfo()
        {
            var task = m_handler.Task;
            var taskDef = m_handler.TaskDefinition;

            _ = PropertyField(serializedObject.FindProperty(nameof(TaskHandler.TaskID)));

            EditorGUI.indentLevel++;

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                var color = s_stateColors[(int)(task?.State ?? 0)];

                if (!Application.isPlaying)
                {
                    color = Color.Lerp(color, EditorStyles.label.normal.textColor, .5f);
                    color.a *= .5f;
                }

                var hex = ColorUtility.ToHtmlStringRGBA(color);

                var stateLabel = task != null
                    ? $"<color=#{hex}>{task.State}</color>"
                    : $"<color=#{hex}>[Edit mode]</color>";

                LabelField("State", stateLabel);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                var sequence = taskDef?.Sequence;

                _ = ObjectField("Sequence", sequence, typeof(TaskSequence), false);

                if (!sequence) { return; }

                EditorGUI.indentLevel--;

                const float PREV_NEXT_SPACING = 12f;

                var oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth -= 15f;

                var prevNextLabelLine = GetControlRect();

                prevNextLabelLine.xMin += 15f;

                prevNextLabelLine.width -= PREV_NEXT_SPACING;
                prevNextLabelLine.width *= .5f;
                prevNextLabelLine.width = Mathf.Floor(prevNextLabelLine.width);

                var currTaskIndex
                    = sequence.TaskDefinitions.FindIndex(def => def.ID == m_handler.TaskID);

                TaskHandler prevHandler = null;

                if (currTaskIndex > 0)
                {
                    var prevTaskDef = sequence.TaskDefinitions[currTaskIndex - 1];

                    prevHandler = TaskManager.HandlerForTask(prevTaskDef.ID);
                }

                _ = EditorGUI.ObjectField(prevNextLabelLine, "Prev handler", prevHandler,
                                      typeof(TaskHandler), true);

                prevNextLabelLine.x += prevNextLabelLine.width + PREV_NEXT_SPACING;

                TaskHandler nextHandler = null;

                if (currTaskIndex < sequence.TaskDefinitions.Count - 1)
                {
                    var nextTaskDef = sequence.TaskDefinitions[currTaskIndex + 1];

                    nextHandler = TaskManager.HandlerForTask(nextTaskDef.ID);
                }

                _ = EditorGUI.ObjectField(prevNextLabelLine, "Next handler", nextHandler,
                                      typeof(TaskHandler), true);

                EditorGUIUtility.labelWidth = oldLabelWidth;
            }

        }

        private void LayoutCompletionConditions()
        {
            _ = PropertyField(serializedObject.FindProperty("m_completeWhen"));

            LabelField("Completion conditions", EditorStyles.boldLabel);

            var listProp = serializedObject.FindProperty(nameof(TaskHandler.CompletionConditions));

            Space(2f);
            DrawConditionsBackingBox(listProp);

            EditorGUI.indentLevel++;

            for (var i = 0; i < listProp.arraySize; i++)
            {
                if (!LayoutCondition(listProp, i)) { break; }
            }

            Space(2f);
            LayoutAddConditionMenu();
            Space(5f);

            EditorGUI.indentLevel--;
        }

        private static void DrawConditionsBackingBox(SerializedProperty listProp)
        {
            var boxRect = GUILayoutUtility.GetLastRect();
            boxRect.y += boxRect.height - 2f;

            var height = 31f;

            boxRect.xMin -= 2f;
            boxRect.xMax += 2f;

            for (var i = 0; i < listProp.arraySize; i++)
            {
                height += EditorGUI.GetPropertyHeight(listProp.GetArrayElementAtIndex(i));
                height += EditorGUIUtility.standardVerticalSpacing;
            }

            if (listProp.arraySize == 0) { height -= EditorGUIUtility.standardVerticalSpacing; }

            boxRect.height = height;

            var guiColor = GUI.color;
            GUI.color *= new Color(.1f, .1f, .1f, 0.3333f);
            EditorGUI.HelpBox(boxRect, "", MessageType.None);
            GUI.color = guiColor;
        }

        private bool LayoutCondition(SerializedProperty conditionsListProp, int index)
        {
            var conditionProp = conditionsListProp.GetArrayElementAtIndex(index);

            if (conditionProp.managedReferenceValue is not TaskCondition condition)
            {
                if (conditionProp.managedReferenceValue == null)
                {
                    LabelField("Null condition reference!");
                }
                else
                {
                    LabelField("Broken condition reference of type "
                               + conditionProp.managedReferenceValue.GetType().Name);
                }

                return DeleteButton();
            }

            var indexProp = conditionProp.FindPropertyRelative("m_index");

            if (indexProp != null) { indexProp.intValue = index; }

            _ = PropertyField(conditionProp,
                          GetConditionLabel(index, condition, conditionProp.isExpanded),
                          conditionProp.isExpanded);

            return DeleteButton();


            bool DeleteButton()
            {
                var deleteRect = GUILayoutUtility.GetLastRect();
                deleteRect.height = EditorGUIUtility.singleLineHeight;
                deleteRect.xMin = deleteRect.xMax - DELETE_BUTTON_WIDTH;

                if (!GUI.Button(deleteRect, "Delete")) { return true; }

                conditionsListProp.DeleteArrayElementAtIndex(index);
                return false;
            }
        }

        private GUIContent GetConditionLabel(int index, TaskCondition condition, bool expanded)
        {
            var completionInsert = "";

            // show cross or tick to indicate whether condition is satisfied
            if (m_handler.Task?.State == TaskState.Current)
            {
                var complete = condition.IsComplete(m_handler);
                var colorHex = ColorUtility.ToHtmlStringRGBA(s_conditionColors[complete ? 1 : 0]);
                var symbol = complete ? "✔" : "<b>☓</b>";

                completionInsert = $"<color=#{colorHex}>{symbol}</color>";
            }

            var label = expanded
                ? $"<b>{TaskCondition.NiceTypeName(condition.GetType())}</b>"
                : condition.RichLabel;

            return new GUIContent($"#{index} {completionInsert}  {label}");
        }

        private void LayoutAddConditionMenu()
        {
            var position = GetControlRect();

            var ctrlRect = EditorGUI.PrefixLabel(position, new GUIContent("Add new condition"));

            if (EditorGUI.DropdownButton(ctrlRect, new GUIContent("Select type..."),
                                         FocusType.Keyboard))
            {
                GenerateAddConditionMenu().DropDown(ctrlRect);
            }
        }

        private GenericMenu GenerateAddConditionMenu()
        {
            var menu = new GenericMenu();
            var slowConditions = new List<Type>();

            foreach (var type in s_conditionTypes)
            {
                if (type.CustomAttributes.Any
                        (cad => cad.AttributeType == typeof(TaskCondition.SlowAttribute)))
                {
                    slowConditions.Add(type);
                    continue;
                }

                var typeName = TaskCondition.NiceTypeName(type);
                menu.AddItem(new GUIContent(typeName), false, () => OnItemSelect(type, typeName));
            }

            if (slowConditions.Count > 0) { menu.AddSeparator(""); }

            foreach (var type in slowConditions)
            {
                var typeName = TaskCondition.NiceTypeName(type);

                menu.AddItem(new GUIContent($"Performance caution/{typeName}"), false,
                             () => OnItemSelect(type, typeName));
            }

            return menu;


            void OnItemSelect(Type type, string typeName)
            {
                Undo.RecordObject(target, $"Add {typeName} Condition");

                m_handler.CompletionConditions.Add((TaskCondition)Activator.CreateInstance(type));

                serializedObject.Update();
            }
        }

        private static void InitialiseEditorData()
        {
            s_richHeader ??= new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontStyle = FontStyle.Normal,
                richText = true
            };
        }
    }
}
