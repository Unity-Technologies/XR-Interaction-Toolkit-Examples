// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using GetComponentFunc = System.Func<UnityEngine.Component, object>;

namespace Meta.Utilities.Editor
{
    internal class AutoSetPostprocessor : AssetPostprocessor
    {
        private static IEnumerable<Scene> GetActiveScenes()
        {
            for (var i = 0; i != SceneManager.sceneCount; i += 1)
                yield return SceneManager.GetSceneAt(i);
        }

        private static void OnPostprocessAllAssets(string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var assets = importedAssets.Select(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>);
            foreach (var gameObject in assets.OfType<GameObject>().OrderBy(GetPrefabDepth))
            {
                CheckPrefab(gameObject);
            }

            foreach (var (path, asset) in importedAssets.Zip(assets))
            {
                if (asset is SceneAsset sceneAsset)
                {
                    CheckScene(path);
                }
            }
        }

        private static int GetPrefabDepth(GameObject obj)
        {
            return 1 + obj.GetComponentsInChildren<Transform>(true).
                Select(PrefabUtility.GetCorrespondingObjectFromSource).
                Where(source => source != null).
                Concat(new[] { null as Transform }).
                Max(source => source == null ? 0 : GetPrefabDepth(source.gameObject));
        }

        private static void CheckPrefab(GameObject gameObject)
        {
            if (CheckObjects(new[] { gameObject }))
            {
                _ = PrefabUtility.SavePrefabAsset(gameObject);
            }
        }

        private static void CheckScene(string path)
        {
            var scene = GetActiveScenes().FirstOrDefault(s => s.path == path);
            var wasValid = scene.IsValid();
            var wasLoaded = scene.isLoaded;

            if (!wasLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            var objects = scene.GetRootGameObjects();
            var anyChanges = CheckObjects(objects);
            if (anyChanges)
            {
                _ = EditorSceneManager.MarkSceneDirty(scene);
                _ = EditorSceneManager.SaveScene(scene);
            }

            if (!wasLoaded)
            {
                _ = EditorSceneManager.CloseScene(scene, !wasValid);
            }
        }

        private static bool CheckObjects(GameObject[] objects)
        {
            var anyModified = false;
            var componentsByType = objects.
                SelectMany(o => o.GetComponentsInChildren<Component>(true)).
                WhereNonNull().
                GroupBy(c => c.GetType());

            foreach (var (type, components) in componentsByType)
            {
                if (HasAutoSet(type, components))
                {
                    var serializedObject = new SerializedObject(components.ToArray());
                    foreach (var property in serializedObject.GetSerializedProperties())
                    {
                        anyModified = DoAutoSet(property, false) || anyModified;
                    }
                }
            }
            return anyModified;
        }

        private static Dictionary<Type, bool> s_componentTypeHasAutoSet = new();
        private static bool HasAutoSet(Type type, IEnumerable<Component> components)
        {
            if (s_componentTypeHasAutoSet.TryGetValue(type, out var hasAutoSet))
                return hasAutoSet;

            var serializedObject = new SerializedObject(components.ToArray());
            var autoSet = serializedObject.GetSerializedProperties().
                Select(GetGetComponentFunc).
                Any(f => f != null);
            s_componentTypeHasAutoSet[type] = autoSet;
            return autoSet;
        }

        internal static GetComponentFunc GetGetComponentFunc(SerializedProperty property)
        {
            var componentType = property.serializedObject.targetObject.GetType();
            var field = componentType.GetField(property);
            var attr = GetAutoSetAttribute(field);
            if (attr != null)
            {
                var includeInactive = attr.GetNamedArgument(nameof(AutoSetFromAttribute.IncludeInactive)) is true;
                var typeArg = attr.ConstructorArguments.
                    FirstOrDefault(arg => arg.ArgumentType == typeof(Type)).
                    Value;
                var type = typeArg is Type t ? t : field.FieldType;

                if (type.IsArray)
                {
                    var elementType = type.GetElementType();
                    if (attr.AttributeType == typeof(AutoSetAttribute))
                        return (target) => target.GetComponents(elementType);
                    if (attr.AttributeType == typeof(AutoSetFromChildrenAttribute))
                        return (target) => target.GetComponentsInChildren(elementType, includeInactive);
                    if (attr.AttributeType == typeof(AutoSetFromParentAttribute))
                        return (target) => target.GetComponentsInParent(elementType, includeInactive);
                }
                else
                {
                    if (attr.AttributeType == typeof(AutoSetAttribute))
                        return (target) => target.GetComponent(type);
                    if (attr.AttributeType == typeof(AutoSetFromChildrenAttribute))
                        return (target) => target.GetComponentInChildren(type, includeInactive);
                    if (attr.AttributeType == typeof(AutoSetFromParentAttribute))
                        return (target) => target.GetComponentInParent(type, includeInactive);
                }
            }
            return null;
        }

        private static CustomAttributeData GetAutoSetAttribute(FieldInfo field) =>
            field?.CustomAttributes?.FirstOrDefault(attr =>
                typeof(AutoSetAttribute).IsAssignableFrom(attr.AttributeType));

        internal static bool DoAutoSet(SerializedProperty property, bool alwaysSet)
        {
            var getComponent = GetGetComponentFunc(property);
            if (getComponent == null)
                return false;

            var modifiedComponents = property.serializedObject.targetObjects.
                OfType<Component>().
                Where(target => Apply(property, target, getComponent, alwaysSet)).
                ToArray();
            if (modifiedComponents.Length != 0)
            {
                property.serializedObject.SetIsDifferentCacheDirty();
                property.serializedObject.Update();
                _ = property.serializedObject.ApplyModifiedPropertiesWithoutUndo();

                foreach (var component in modifiedComponents)
                {
                    Debug.Log($"AutoSet {property.name} to {property.objectReferenceValue} for {component}.", component);
                }
            }
            return modifiedComponents.Length != 0;
        }

        private static bool Apply(SerializedProperty sourceProperty, Component target, GetComponentFunc getComponent, bool alwaysSet)
        {
            // create a new SerializedObject for this target, since there may be multiple
            using var obj = new SerializedObject(target);
            var property = obj.FindProperty(sourceProperty.propertyPath);
            if (!property.isArray)
            {
                if (alwaysSet ||
                    property.objectReferenceValue is not Component value ||
                    value == null)
                {
                    property.objectReferenceValue = getComponent(target) as Component;
                    return obj.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else
            {
                if (alwaysSet ||
                    property.arraySize == 0 ||
                    property.GetArrayElementAtIndex(0).objectReferenceValue is not Component value ||
                    value == null)
                {
                    var array = getComponent(target) as Array;
                    property.arraySize = array.Length;
                    foreach (var (i, component) in array.Cast<Component>().Enumerate())
                        property.GetArrayElementAtIndex(i).objectReferenceValue = component;
                    return obj.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            return false;
        }
    }
}
