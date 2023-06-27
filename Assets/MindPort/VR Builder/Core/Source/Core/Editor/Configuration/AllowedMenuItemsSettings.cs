// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.Utils;
using VRBuilder.Editor.Serialization;
using VRBuilder.Editor.UI.StepInspector.Menu;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;

namespace VRBuilder.Editor.Configuration
{
    /// <summary>
    /// Configuration class for menu items.
    /// It manages whether a behavior or condition should be displayed in the Step Inspector or not.
    /// Can be serialized.
    /// </summary>
    [DataContract(IsReference = true)]
    public class AllowedMenuItemsSettings
    {
        private IList<MenuItem<IBehavior>> behaviorMenuItems;
        private IList<MenuItem<ICondition>> conditionMenuItems;

        [DataMember]
        public IDictionary<string, bool> SerializedBehaviorSelections;

        [DataMember]
        public IDictionary<string, bool> SerializedConditionSelections;

        [JsonConstructor]
        public AllowedMenuItemsSettings() : this(new Dictionary<string, bool>(), new Dictionary<string, bool>())
        {
        }

        public AllowedMenuItemsSettings(IDictionary<string, bool> behaviors, IDictionary<string, bool> serializedConditions)
        {
            SerializedBehaviorSelections = behaviors;
            SerializedConditionSelections = serializedConditions;

            UpdateWithAllBehaviorsAndConditionsInProject();
        }

        /// <summary>
        /// Returns all active behavior menu items.
        /// </summary>
        public IEnumerable<MenuItem<IBehavior>> GetBehaviorMenuOptions()
        {
            if (behaviorMenuItems == null)
            {
                behaviorMenuItems = SetupItemList<MenuItem<IBehavior>>(SerializedBehaviorSelections)
                    .OrderByAlphaNumericNaturalSort(item => item.DisplayedName)
                    .ToList();
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            return behaviorMenuItems.Where(item => SerializedBehaviorSelections[item.GetType().AssemblyQualifiedName]);
        }

        /// <summary>
        /// Returns all active condition menu items.
        /// </summary>
        public IEnumerable<MenuItem<ICondition>> GetConditionMenuOptions()
        {
            if (conditionMenuItems == null)
            {
                conditionMenuItems = SetupItemList<MenuItem<ICondition>>(SerializedConditionSelections)
                    .OrderByAlphaNumericNaturalSort(item => item.DisplayedName)
                    .ToList();
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            return conditionMenuItems.Where(item => SerializedConditionSelections[item.GetType().AssemblyQualifiedName]);
        }

        public async void RefreshMenuOptions()
        {
            conditionMenuItems = null;
            behaviorMenuItems = null;

            await Task.Run(() => GetBehaviorMenuOptions());
            await Task.Run(() => GetConditionMenuOptions());
        }

        /// <summary>
        /// Serializes the <paramref name="settings"/> object and saves it into a configuration file at a default path.
        /// </summary>
        /// <exception cref="NullReferenceException">Thrown when parameter is null.</exception>
        public static bool Save(AllowedMenuItemsSettings settings)
        {
            if (string.IsNullOrEmpty(EditorConfigurator.Instance.AllowedMenuItemsSettingsAssetPath))
            {
                Debug.LogFormat("The property \"AllowedMenuItemsSettingsAssetPath\" of the " +
                    "current editor configuration is not set. Thus, the AllowedMenuItemsSettings cannot be saved.");
                return false;
            }

            const string assets = "Assets/";
            string path = EditorConfigurator.Instance.AllowedMenuItemsSettingsAssetPath;

            if (path.StartsWith(assets) == false)
            {
                Debug.LogErrorFormat("The property \"AllowedMenuItemsSettingsAssetPath\" of the current editor configuration" +
                    " is invalid. It has to start with \"{0}\". Current value: \"{1}\"", assets, path);
                return false;
            }

            try
            {
                if (settings == null)
                {
                    throw new NullReferenceException("The allowed menu items settings file cannot be saved "
                        + "because the settings are null.");
                }

                string serialized = JsonEditorConfigurationSerializer.Serialize(settings);

                string fullPath = string.Format("{0}/{1}", Application.dataPath, path.Remove(0, assets.Length));
                string directoryPath = Path.GetDirectoryName(fullPath);

                if (string.IsNullOrEmpty(directoryPath))
                {
                    Debug.LogErrorFormat("No valid directory path found in path \"{0}\". The property \"AllowedMenuItemSettingsAssetPath\"" +
                        " of the current editor configuration is invalid. Current value: \"{1}\"", fullPath, path);
                    return false;
                }

                if (Directory.Exists(directoryPath) == false)
                {
                    Directory.CreateDirectory(directoryPath);
                }

                StreamWriter writer = new StreamWriter(fullPath, false);
                writer.Write(serialized);
                writer.Close();

                AssetDatabase.ImportAsset(path);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        /// <summary>
        /// Loads and returns the <see cref="AllowedMenuItemsSettings"/> object from the default configuration file location.
        /// If the <see cref="AllowedMenuItemsSettingsAssetPath"/> in the editor configuration is empty or null,
        /// it returns an empty <see cref="AllowedMenuItemsSettings"/> object.
        /// </summary>
        public static AllowedMenuItemsSettings Load()
        {            
            string path = EditorConfigurator.Instance.AllowedMenuItemsSettingsAssetPath;
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("The property \"AllowedMenuItemsSettingsAssetPath\" of the current editor " +
                    "configuration is not set. Therefore, it cannot be loaded. A new \"AllowedMenuItemsSettings\" " +
                    "object with all found conditions and behaviors was returned.");
                return new AllowedMenuItemsSettings();
            }

            TextAsset settings = (TextAsset)AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset));

            if (settings != null)
            {
                return JsonEditorConfigurationSerializer.Deserialize(settings.text);
            }

            return new AllowedMenuItemsSettings();
        }

        private IList<T> SetupItemList<T>(IDictionary<string, bool> userSelections)
        {
            if (userSelections == null)
            {
                return null;
            }

            IList<T> itemList = new List<T>();

            foreach (KeyValuePair<string, bool> keyValuePair in userSelections)
            {
                Type type = ReflectionUtils.GetTypeFromAssemblyQualifiedName(keyValuePair.Key);

                if (type == null)
                {
                    continue;
                }

                try
                {
                    T instance = (T)ReflectionUtils.CreateInstanceOfType(type);
                    itemList.Add(instance);
                }
                catch (Exception)
                {
                    // Type is abstract or has no parameterless constructor, ignore it.
                }
            }

            return itemList;
        }

        private void UpdateWithAllBehaviorsAndConditionsInProject()
        {
            IEnumerable<Type> implementedBehaviors = ReflectionUtils.GetConcreteImplementationsOf<MenuItem<IBehavior>>();

            foreach (Type type in implementedBehaviors)
            {
                if (type.AssemblyQualifiedName == null || SerializedBehaviorSelections.ContainsKey(type.AssemblyQualifiedName))
                {
                    continue;
                }
                SerializedBehaviorSelections.Add(type.AssemblyQualifiedName, ShouldBeEnabled(type));
            }

            IEnumerable<Type> implementedConditions = ReflectionUtils.GetConcreteImplementationsOf<MenuItem<ICondition>>();

            foreach (Type type in implementedConditions)
            {
                if (type.AssemblyQualifiedName == null || SerializedConditionSelections.ContainsKey(type.AssemblyQualifiedName))
                {
                    continue;
                }

                SerializedConditionSelections.Add(type.AssemblyQualifiedName, ShouldBeEnabled(type));
            }
        }

        private bool ShouldBeEnabled(Type type)
        {
            object instance = Activator.CreateInstance(type);
            if (instance is IInternalTypeProvider typeProvider)
            {
                return typeProvider.GetItemType().GetAttribute<ObsoleteAttribute>() == null;
            }

            return true;
        }
    }
}
