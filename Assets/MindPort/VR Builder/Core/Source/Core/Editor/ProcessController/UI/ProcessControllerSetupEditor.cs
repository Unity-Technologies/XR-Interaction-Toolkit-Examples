using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VRBuilder.Core.Utils;
using VRBuilder.UX;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UX
{
    /// <summary>
    /// Custom editor for <see cref="IProcessController"/>s.
    /// Takes care of adding required components.
    /// </summary>
    [CustomEditor(typeof(ProcessControllerSetup))]
    internal class ProcessControllerSetupEditor : UnityEditor.Editor
    {
        private SerializedProperty processControllerProperty;
        private SerializedProperty autoStartProperty;
        private SerializedProperty useCustomPrefabProperty;
        private SerializedProperty customPrefabProperty;

        private ProcessControllerSetup setupObject;
        
        private IProcessController[] availableProcessControllers;
        private string[] availableProcessControllerNames;
        private GameObject customPrefab = null;
        private int selectedIndex = 0;
        private bool useCustomPrefab;

        private List<Type> currentRequiredComponents = new List<Type>();
        
        private void OnEnable()
        {
            processControllerProperty = serializedObject.FindProperty("processControllerQualifiedName");
            autoStartProperty = serializedObject.FindProperty("autoStartProcess");
            useCustomPrefabProperty = serializedObject.FindProperty("useCustomPrefab");
            customPrefabProperty = serializedObject.FindProperty("customPrefab");

            customPrefab = (GameObject) customPrefabProperty.objectReferenceValue;
            setupObject = (ProcessControllerSetup) serializedObject.targetObject;

            availableProcessControllers = ReflectionUtils.GetConcreteImplementationsOf<IProcessController>()
                .Select(c => (IProcessController) ReflectionUtils.CreateInstanceOfType(c)).OrderByDescending(controller => controller.Priority).ToArray();

            availableProcessControllerNames = availableProcessControllers.Select(controller => controller.Name).ToArray();

            selectedIndex = availableProcessControllers.Select(controller => controller.GetType().AssemblyQualifiedName).ToList().IndexOf(processControllerProperty.stringValue);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }
            
            currentRequiredComponents = availableProcessControllers[selectedIndex].GetRequiredSetupComponents();
            currentRequiredComponents.AddRange(currentRequiredComponents
                .SelectMany(type => type.GetCustomAttributes(typeof(RequireComponent)).Cast<RequireComponent>())
                .SelectMany(component => new List<Type>() {component.m_Type0, component.m_Type1, component.m_Type2})
                .Where(type => type != null)
                .Distinct()
                .Except(currentRequiredComponents)
                .ToList());
        }

        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((ProcessControllerSetup)target), typeof(ProcessControllerSetup), false);
            GUI.enabled = useCustomPrefab == false && Application.isPlaying == false;
            bool prevUseCustomPrefab = useCustomPrefab;
            int prevIndex = selectedIndex;
            
            selectedIndex = EditorGUILayout.Popup("Process Controller", selectedIndex, availableProcessControllerNames);

            autoStartProperty.boolValue = EditorGUILayout.Toggle("Auto start process", autoStartProperty.boolValue);
            
            GUI.enabled = !Application.isPlaying;

            useCustomPrefab = EditorGUILayout.Toggle("Use custom prefab", useCustomPrefabProperty.boolValue);

            if (Application.isPlaying)
            {
                if (useCustomPrefab)
                {
                    customPrefab = EditorGUILayout.ObjectField("Custom prefab", customPrefab, typeof(GameObject), false) as GameObject;
                }
                serializedObject.ApplyModifiedProperties();
                
                return;
            }
            
            if (useCustomPrefab)
            {
                customPrefab = EditorGUILayout.ObjectField("Custom prefab", customPrefab, typeof(GameObject), false) as GameObject;
                if (useCustomPrefab != prevUseCustomPrefab)
                {
                    RemoveComponents(currentRequiredComponents);
                    currentRequiredComponents = new List<Type>();
                }
                customPrefabProperty.objectReferenceValue = customPrefab;
            }
            else if (prevIndex != selectedIndex || HasComponents(currentRequiredComponents) == false || useCustomPrefab != prevUseCustomPrefab)
            {
                RemoveComponents(currentRequiredComponents);
                currentRequiredComponents = availableProcessControllers[selectedIndex].GetRequiredSetupComponents();
                AddComponents(currentRequiredComponents);
                availableProcessControllers[selectedIndex].HandlePostSetup(setupObject.gameObject);
            }

            useCustomPrefabProperty.boolValue = useCustomPrefab;
            processControllerProperty.stringValue = availableProcessControllers[selectedIndex].GetType().AssemblyQualifiedName;
            
            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveComponents(List<Type> components)
        {
            foreach (Type component in currentRequiredComponents)
            {
                DestroyImmediate(setupObject.GetComponent(component), true);
            }
        }

        private void AddComponents(List<Type> components)
        {
            if (components != null)
            {
                foreach (Type requiredComponent in components)
                {
                    setupObject.gameObject.AddComponent(requiredComponent);
                }
            }
        }

        private bool HasComponents(List<Type> components)
        {
            return components.Except(setupObject.gameObject.GetComponents<Component>().ToList().Select(c => c.GetType())).Any() == false;
        }
    }
}