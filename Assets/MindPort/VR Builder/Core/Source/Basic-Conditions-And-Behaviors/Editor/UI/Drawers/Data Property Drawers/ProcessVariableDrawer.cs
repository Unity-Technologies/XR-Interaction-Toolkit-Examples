using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.ProcessUtils;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;
using VRBuilder.Editor.UI;
using VRBuilder.Editor.UI.Drawers;
using VRBuilder.Editor.UndoRedo;

namespace VRBuilder.Editor.Core.UI.Drawers
{
    /// <summary>
    /// Drawer for <see cref="ProcessVariable{T}"/>
    /// </summary>
    internal abstract class ProcessVariableDrawer<T> : UniqueNameReferenceDrawer where T : IEquatable<T>
    {
        /// <summary>
        /// Draws the field for the constant value depending on its type.
        /// </summary>
        protected abstract T DrawConstField(T value);

        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {            
            if (RuntimeConfigurator.Exists == false)
            {
                return rect;
            }

            isUndoOperation = false;
            ProcessVariable<T> processVariable = (ProcessVariable<T>)currentValue;
            UniqueNameReference uniqueNameReference = processVariable.PropertyReference;
            PropertyInfo valueProperty = uniqueNameReference.GetType().GetProperty("Value");
            Type valueType = ReflectionUtils.GetDeclaredTypeOfPropertyOrField(valueProperty);            

            if (valueProperty == null)
            {
                throw new ArgumentException("Only ObjectReference<> implementations should inherit from the UniqueNameReference type.");
            }

            Rect guiLineRect = rect;
            string oldUniqueName = uniqueNameReference.UniqueName;
            GameObject selectedSceneObject = GetGameObjectFromID(oldUniqueName);

            if (selectedSceneObject == null && string.IsNullOrEmpty(oldUniqueName) == false && missingUniqueNames.Contains(oldUniqueName) == false)
            {
                missingUniqueNames.Add(oldUniqueName);
                Debug.LogError($"The process object with the unique name '{oldUniqueName}' cannot be found!");
            }

            CheckForMisconfigurationIssues(selectedSceneObject, valueType, ref rect, ref guiLineRect);

            GUILayout.BeginArea(guiLineRect);
            GUILayout.BeginHorizontal();            

            EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUI.BeginDisabledGroup(processVariable.IsConst);            
            selectedSceneObject = EditorGUILayout.ObjectField("", selectedSceneObject, typeof(GameObject), true) as GameObject;
            EditorGUI.EndDisabledGroup();


            if(GUILayout.Toggle(!processVariable.IsConst, "Property reference", BuilderEditorStyles.RadioButton, GUILayout.Width(192)))
            {
                processVariable.IsConst = false;
                changeValueCallback(processVariable);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            guiLineRect = AddNewRectLine(ref rect);
            T oldConstValue = processVariable.ConstValue;
            T newConstValue = processVariable.ConstValue;

            GUILayout.BeginArea(guiLineRect);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUI.BeginDisabledGroup(processVariable.IsConst == false);
            newConstValue = DrawConstField(newConstValue);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Toggle(processVariable.IsConst, "Constant value", BuilderEditorStyles.RadioButton, GUILayout.Width(192)))
            {
                processVariable.IsConst = true;
                changeValueCallback(processVariable);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            string newUniqueName = GetIDFromSelectedObject(selectedSceneObject, valueType, oldUniqueName);

            if (oldUniqueName != newUniqueName)
            {
                RevertableChangesHandler.Do(
                    new ProcessCommand(
                        () =>
                        {
                            processVariable.PropertyReference.UniqueName = newUniqueName;
                            changeValueCallback(processVariable);
                        },
                        () =>
                        {
                            processVariable.PropertyReference.UniqueName = oldUniqueName;
                            changeValueCallback(processVariable);
                        }),
                    isUndoOperation ? undoGroupName : string.Empty);

                if (isUndoOperation)
                {
                    RevertableChangesHandler.CollapseUndoOperations(undoGroupName);
                }
            }

            if (newConstValue != null && newConstValue.Equals(processVariable.ConstValue) == false)
            {
                RevertableChangesHandler.Do(
                    new ProcessCommand(
                        () =>
                        {
                            processVariable.ConstValue = newConstValue;
                            changeValueCallback(processVariable);
                        },
                        () =>
                        {
                            processVariable.ConstValue = oldConstValue;
                            changeValueCallback(processVariable);
                        }),
                    isUndoOperation ? undoGroupName : string.Empty);

                if (isUndoOperation)
                {
                    RevertableChangesHandler.CollapseUndoOperations(undoGroupName);
                }
            }

            return rect;
        }
    }
}