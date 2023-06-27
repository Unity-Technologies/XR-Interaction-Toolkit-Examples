// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Linq;
using System.Collections.Generic;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VRBuilder.Core.Properties
{
    /// <summary>
    /// Helper class that adds functionality to any <see cref="ISceneObject"/>.
    /// </summary>
    public static class SceneObjectExtensions
    {
        /// <summary>
        /// Ensures that this <see cref="ISceneObject"/>'s UniqueName is not duplicated.
        /// </summary>
        /// <param name="sceneObject"><see cref="ISceneObject"/> to whom the `UniqueName` will be validated.</param>
        /// <param name="baseName">Optional base for this <paramref name="sceneObject"/>'s `UniqueName`.</param>
        public static void SetSuitableName(this ISceneObject sceneObject, string baseName = "")
        {
            int counter = 1;
            string newName = baseName = string.IsNullOrEmpty(baseName) ? sceneObject.GameObject.name : baseName;

            RuntimeConfigurator.Configuration.SceneObjectRegistry.Unregister(sceneObject);
            while (RuntimeConfigurator.Configuration.SceneObjectRegistry.ContainsName(newName))
            {
                newName = string.Format("{0}_{1}", baseName, counter);
                counter++;
            }

            sceneObject.ChangeUniqueName(newName);
        }

        /// <summary>
        /// Adds a <see cref="ISceneObjectProperty"/> of type <typeparamref name="T"/> into this <see cref="ISceneObject"/>.
        /// </summary>
        /// <param name="sceneObject"><see cref="ISceneObject"/> to whom the type <typeparamref name="T"/> will be added.</param>
        /// <typeparam name="T">The type of <see cref="ISceneObjectProperty"/> to be added to <paramref name="sceneObject"/>.</typeparam>
        /// <returns>A reference to the <see cref="ISceneObjectProperty"/> added to <paramref name="sceneObject"/>.</returns>
        public static ISceneObjectProperty AddProcessProperty<T>(this ISceneObject sceneObject)
        {
            return AddProcessProperty(sceneObject, typeof(T));
        }

        /// <summary>
        /// Adds a type of <paramref name="processProperty"/> into this <see cref="ISceneObject"/>.
        /// </summary>
        /// <param name="sceneObject"><see cref="ISceneObject"/> to whom the <paramref name="processProperty"/> will be added.</param>
        /// <param name="processProperty">Typo of <see cref="ISceneObjectProperty"/> to be added to <paramref name="sceneObject"/>.</param>
        /// <returns>A reference to the <see cref="ISceneObjectProperty"/> added to <paramref name="sceneObject"/>.</returns>
        public static ISceneObjectProperty AddProcessProperty(this ISceneObject sceneObject, Type processProperty)
        {
            if (AreParametersNullOrInvalid(sceneObject, processProperty))
            {
                return null;
            }

            ISceneObjectProperty sceneObjectProperty = sceneObject.GameObject.GetComponent(processProperty) as ISceneObjectProperty;

            if (sceneObjectProperty != null)
            {
                return sceneObjectProperty;
            }

            if (processProperty.IsInterface || processProperty.IsAbstract)
            {
                // If it is an interface just take the first public found concrete implementation.
                Type propertyType = ReflectionUtils
                    .GetAllTypes()
                    .Where(processProperty.IsAssignableFrom)
                    .Where(type => type.Assembly.GetReferencedAssemblies().All(assemblyName =>  assemblyName.Name != "UnityEditor" && assemblyName.Name != "nunit.framework"))
                    .First(type => type.IsClass && type.IsPublic && type.IsAbstract == false);

                sceneObjectProperty = sceneObject.GameObject.AddComponent(propertyType) as ISceneObjectProperty;
            }
            else
            {
                sceneObjectProperty = sceneObject.GameObject.AddComponent(processProperty) as ISceneObjectProperty;
            }

            return sceneObjectProperty;
        }

        /// <summary>
        /// Checks if property extensions exist in the project and adds them to the game object if the current scene requires them.
        /// </summary>
        /// <param name="property">The property to check for.</param>
        public static void AddProcessPropertyExtensions(this ISceneObjectProperty property)
        {
            List<Type> propertyTypes = ReflectionUtils
                .GetAllTypes()
                .Where(type => type.IsAssignableFrom(property.GetType()))
                .Where(type => type.Assembly.GetReferencedAssemblies().All(assemblyName => assemblyName.Name != "UnityEditor" && assemblyName.Name != "nunit.framework"))
                .Where(type => type.IsPublic && type.IsPointer == false && type.IsByRef == false).ToList();


            List<Type> extensionTypes = new List<Type>();

            foreach (Type type in propertyTypes) 
            {
                if(typeof(ISceneObjectProperty).IsAssignableFrom(type))
                {
                    extensionTypes.Add(typeof(ISceneObjectPropertyExtension<>).MakeGenericType(type));
                }
            }

            List<Type> availableExtensions = ReflectionUtils
                .GetAllTypes()
                .Where(type => extensionTypes.Any(extensionType => extensionType.IsAssignableFrom(type)))
                .Where(type => type.Assembly.GetReferencedAssemblies().All(assemblyName => assemblyName.Name != "UnityEditor" && assemblyName.Name != "nunit.framework"))
                .Where(type => type.IsClass && type.IsPublic && type.IsAbstract == false).ToList();           

            foreach(Type concreteExtension in availableExtensions)
            {
                string assemblyName = concreteExtension.Assembly.FullName;

                if (RuntimeConfigurator.Configuration.SceneConfiguration.ExtensionAssembliesWhitelist.Contains(assemblyName))
                {
                    property.SceneObject.GameObject.AddComponent(concreteExtension);
                }
            }
        }

        /// <summary>
        /// Removes type of <paramref name="processProperty"/> from this <see cref="ISceneObject"/>.
        /// </summary>
        /// <param name="sceneObject"><see cref="ISceneObject"/> from whom the <paramref name="processProperty"/> will be removed.</param>
        /// <param name="processProperty"><see cref="ISceneObjectProperty"/> to be removed from <paramref name="sceneObject"/>.</param>
        /// <param name="removeDependencies">If true, this method also removes other components that are marked as `RequiredComponent` by <paramref name="processProperty"/>.</param>
        /// <param name="excludedFromBeingRemoved">The process properties in this list will not be removed if any is a dependency of <paramref name="processProperty"/>. Only relevant if <paramref name="removeDependencies"/> is true.</param>
        public static void RemoveProcessProperty(this ISceneObject sceneObject, Component processProperty, bool removeDependencies = false, IEnumerable<Component> excludedFromBeingRemoved = null)
        {
            Type processPropertyType = processProperty.GetType();
            RemoveProcessProperty(sceneObject, processPropertyType, removeDependencies, excludedFromBeingRemoved);
        }

        /// <summary>
        /// Removes type of <paramref name="processProperty"/> from this <see cref="ISceneObject"/>.
        /// </summary>
        /// <param name="sceneObject"><see cref="ISceneObject"/> from whom the <paramref name="processProperty"/> will be removed.</param>
        /// <param name="processProperty">Typo of <see cref="ISceneObjectProperty"/> to be removed from <paramref name="sceneObject"/>.</param>
        /// <param name="removeDependencies">If true, this method also removes other components that are marked as `RequiredComponent` by <paramref name="processProperty"/>.</param>
        /// <param name="excludedFromBeingRemoved">The process properties in this list will not be removed if any is a dependency of <paramref name="processProperty"/>. Only relevant if <paramref name="removeDependencies"/> is true.</param>
        public static void RemoveProcessProperty(this ISceneObject sceneObject, Type processProperty, bool removeDependencies = false, IEnumerable<Component> excludedFromBeingRemoved = null)
        {
            Component processComponent = sceneObject.GameObject.GetComponent(processProperty);

            if (AreParametersNullOrInvalid(sceneObject, processProperty) || processComponent == null)
            {
                return;
            }

            IEnumerable<Type> typesToIgnore = GetTypesFromComponents(excludedFromBeingRemoved);
            RemoveProperty(sceneObject, processProperty, removeDependencies, typesToIgnore);
        }

        private static void RemoveProperty(ISceneObject sceneObject, Type typeToRemove, bool removeDependencies, IEnumerable<Type> typesToIgnore)
        {
            IEnumerable<Component> processProperties = sceneObject.GameObject.GetComponents(typeof(Component)).Where(component => component.GetType() != typeToRemove);

            foreach (Component component in processProperties)
            {
                if (IsTypeDependencyOfComponent(typeToRemove, component))
                {
                    RemoveProperty(sceneObject, component.GetType(), removeDependencies, typesToIgnore);
                }
            }

            Component processComponent = sceneObject.GameObject.GetComponent(typeToRemove);

#if UNITY_EDITOR
            Object.DestroyImmediate(processComponent);
#else
            Object.Destroy(processComponent);
#endif

            if (removeDependencies)
            {
                HashSet<Type> dependencies = GetAllDependenciesFrom(typeToRemove);

                if (dependencies == null)
                {
                    return;
                }

                // Some Unity native components like Rigidbody require Transform but Transform can't be removed.
                dependencies.Remove(typeof(Transform));

                foreach (Type dependency in dependencies.Except(typesToIgnore))
                {
                    RemoveProperty(sceneObject, dependency, removeDependencies, typesToIgnore);
                }
            }
        }

        private static bool AreParametersNullOrInvalid(ISceneObject sceneObject, Type processProperty)
        {
            return sceneObject == null || sceneObject.GameObject == null || processProperty == null || typeof(ISceneObjectProperty).IsAssignableFrom(processProperty) == false;
        }

        private static bool IsTypeDependencyOfComponent(Type type, Component component)
        {
            Type propertyType = component.GetType();
            RequireComponent[] requireComponents = propertyType.GetCustomAttributes(typeof(RequireComponent), false) as RequireComponent[];

            if (requireComponents == null || requireComponents.Length == 0)
            {
                return false;
            }

            return requireComponents.Any(requireComponent => requireComponent.m_Type0 == type || requireComponent.m_Type1 == type || requireComponent.m_Type2 == type);
        }

        private static HashSet<Type> GetAllDependenciesFrom(Type processProperty)
        {
            RequireComponent[] requireComponents = processProperty.GetCustomAttributes(typeof(RequireComponent), false) as RequireComponent[];

            if (requireComponents == null || requireComponents.Length == 0)
            {
                return null;
            }

            HashSet<Type> dependencies = new HashSet<Type>();

            foreach (RequireComponent requireComponent in requireComponents)
            {
                AddTypeToList(requireComponent.m_Type0, ref dependencies);
                AddTypeToList(requireComponent.m_Type1, ref dependencies);
                AddTypeToList(requireComponent.m_Type2, ref dependencies);
            }

            return dependencies;
        }

        private static void AddTypeToList(Type type, ref HashSet<Type> dependencies)
        {
            if (type != null)
            {
                dependencies.Add(type);
            }
        }

        private static IEnumerable<Type> GetTypesFromComponents(IEnumerable<Component> components)
        {
            return components == null ? new Type[0] : components.Select(component => component.GetType());
        }
    }
}
