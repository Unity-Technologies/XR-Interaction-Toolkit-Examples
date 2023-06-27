// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;
using System.Reflection;

namespace VRBuilder.Editor.PackageManager
{
    /// <summary>
    /// Automatically retrieves all dependencies from the Unity's Package Manager at the startup.
    /// </summary>
    [InitializeOnLoad]
    public class DependencyManager
    {
        private static Type[] cachedTypes;

        public class DependenciesEnabledEventArgs : EventArgs
        {
            public readonly List<Dependency> DependenciesList;

            public DependenciesEnabledEventArgs(List<Dependency> dependenciesList)
            {
                DependenciesList = dependenciesList;
            }
        }

        /// <summary>
        /// Emitted when all found <see cref="Dependency"/> were installed.
        /// </summary>
        public static event EventHandler<DependenciesEnabledEventArgs> OnPostProcess;

        private static List<Dependency> dependenciesList;

        static DependencyManager()
        {            
            GatherDependencies();
        }

        private static void GatherDependencies()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            dependenciesList = new List<Dependency>();

            foreach (Type dependencyType in GetConcreteImplementationsOf<Dependency>())
            {
                try
                {
                    if (CreateInstanceOfType(dependencyType) is Dependency dependencyInstance && string.IsNullOrEmpty(dependencyInstance.Package) == false)
                    {
                        dependenciesList.Add(dependencyInstance);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogErrorFormat("{0} while retrieving Dependency object of type {1}.\n{2}", exception.GetType().Name, dependencyType.Name, exception.StackTrace);
                }
            }

            if (dependenciesList.Any())
            {
                dependenciesList = dependenciesList.OrderBy(setup => setup.Priority).ToList();
                ProcessDependencies();
            }
        }

        private static async void ProcessDependencies()
        {
            while (PackageOperationsManager.Packages == null || PackageOperationsManager.Packages.Any() == false || EditorApplication.isCompiling)
            {
                await Task.Delay(100);
            }

            foreach (Dependency dependency in dependenciesList)
            {

                if (PackageOperationsManager.IsPackageLoaded(dependency.Package, dependency.Version))
                {
                    if (string.IsNullOrEmpty(dependency.Version))
                    {
                        dependency.Version = PackageOperationsManager.GetInstalledPackageVersion(dependency.Package);
                    }

                    dependency.IsEnabled = true;
                }
                else
                {
                    int index = dependenciesList.FindIndex(item => item == dependency);
                    EditorUtility.DisplayProgressBar("Importing VR Builder dependencies", $"Fetching dependency {dependency.Package} ({index + 1}/{dependenciesList.Count})", (float)(index + 1) / dependenciesList.Count);

                    PackageOperationsManager.LoadPackage(dependency.Package, dependency.Version);
                    return;
                }
            }

            EditorUtility.ClearProgressBar();
            OnPostProcess?.Invoke(null, new DependenciesEnabledEventArgs(dependenciesList));
        }

        private static IEnumerable<Type> GetConcreteImplementationsOf(Type baseType)
        {
            return GetAllTypes()
                .Where(baseType.IsAssignableFrom)
                .Where(type => type.IsClass && type.IsAbstract == false);
        }

        private static IEnumerable<Type> GetConcreteImplementationsOf<T>()
        {
            return GetConcreteImplementationsOf(typeof(T));
        }

        private static IEnumerable<Type> GetAllTypes()
        {
            if (cachedTypes == null)
            {
                cachedTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        return e.Types.Where(type => type != null);
                    }
                }).ToArray();
            }

            return cachedTypes;
        }

        private static object CreateInstanceOfType(Type type)
        {
            return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[0], null);
        }
    }
}
