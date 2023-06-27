// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Properties;
using VRBuilder.Core.RestrictiveEnvironment;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Settings;

namespace VRBuilder.Core
{
    /// <summary>
    /// Collection of <see cref="ISceneObject"/>s that can be locked and unlocked during a step.
    /// Additionally, checks if objects are automatically or manually unlocked.
    /// </summary>
    internal class LockableObjectsCollection
    {
        private List<LockablePropertyData> toUnlock;

        /// <summary>
        /// Returns the current tags to manually unlock.
        /// </summary>
        public IEnumerable<Guid> TagsToUnlock => data.TagsToUnlock.Keys;

        private Step.EntityData data;

        public List<ISceneObject> SceneObjects { get; set; } = new List<ISceneObject>();

        public LockableObjectsCollection(Step.EntityData entityData)
        {
            toUnlock = PropertyReflectionHelper.ExtractLockablePropertiesFromStep(entityData).ToList();
            data = entityData;

            CreateSceneObjects();
        }

        private void CreateSceneObjects()
        {
            CleanProperties();

            foreach (LockablePropertyReference propertyReference in data.ToUnlock)
            {
                AddSceneObject(propertyReference.Target.Value);
            }

            foreach (LockablePropertyData propertyData in toUnlock)
            {
                AddSceneObject(propertyData.Property.SceneObject);
            }
        }

        public void AddSceneObject(ISceneObject sceneObject)
        {
            if (SceneObjects.Contains(sceneObject) == false)
            {
                SceneObjects.Add(sceneObject);
                SortSceneObjectList();
            }
        }

        private void SortSceneObjectList()
        {
            SceneObjects.Sort((obj1, obj2) => obj1.GameObject.ToString().CompareTo(obj2.GameObject.ToString()));
        }

        public void RemoveSceneObject(ISceneObject sceneObject)
        {
            if (SceneObjects.Remove(sceneObject))
            {
                data.ToUnlock = data.ToUnlock.Where(property =>
                {
                    if (property.GetProperty() == null)
                    {
                        return false;
                    }

                    return property.GetProperty().SceneObject != sceneObject;
                }).ToList();
            }
        }

        public bool IsInManualUnlockList(LockableProperty property)
        {
            foreach (LockablePropertyReference lockableProperty in data.ToUnlock)
            {
                if (property == lockableProperty.GetProperty())
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsUsedInAutoUnlock(ISceneObject sceneObject)
        {
            return toUnlock.Any(propertyData => propertyData.Property.SceneObject == sceneObject);
        }

        public bool IsInAutoUnlockList(LockableProperty property)
        {
            foreach (LockablePropertyData lockableProperty in toUnlock)
            {
                if (property == lockableProperty.Property)
                {
                    return true;
                }
            }

            return false;
        }

        public void Remove(LockableProperty property)
        {
            data.ToUnlock = data.ToUnlock.Where(reference => reference.GetProperty() != property).ToList();
        }

        public void Add(LockableProperty property)
        {
            data.ToUnlock = data.ToUnlock.Union(new [] {new LockablePropertyReference(property), }).ToList();
        }

        public void AddTag(Guid tag)
        {
            if(data.TagsToUnlock.ContainsKey(tag))
            {
                return;
            }

            data.TagsToUnlock.Add(tag, new List<Type>());
        }

        public void RemoveTag(Guid tag)
        {
            data.TagsToUnlock.Remove(tag);
        }

        public void AddPropertyToTag(Guid tag, Type property)
        {
            if (data.TagsToUnlock.ContainsKey(tag) == false)
            {
                return;
            }

            data.TagsToUnlock[tag] = data.TagsToUnlock[tag].Union(new[] { property }).ToList();
        }

        public void RemovePropertyFromTag(Guid tag, Type property)
        {
            if (data.TagsToUnlock.ContainsKey(tag) == false)
            {
                return;
            }

            data.TagsToUnlock[tag] = data.TagsToUnlock[tag].Where(p => p != property).ToList();
        }

        public bool IsPropertyEnabledForTag(Guid tag, Type property)
        {
            return data.TagsToUnlock.ContainsKey(tag) && data.TagsToUnlock[tag].Contains(property);
        }

        private void CleanProperties()
        {
            data.ToUnlock = data.ToUnlock.Where(reference => reference.Target.IsEmpty() == false).ToList();
        }
    }
}
