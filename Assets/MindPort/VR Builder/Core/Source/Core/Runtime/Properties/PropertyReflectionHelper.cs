// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.Properties;
using VRBuilder.Core.RestrictiveEnvironment;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;
using VRBuilder.Unity;
using UnityEngine;
using VRBuilder.Core.Settings;

namespace VRBuilder.Core
{
    /// <summary>
    /// Helper class which provides methods to extract <see cref="LockablePropertyData"/> from different process entities.
    /// </summary>
    internal static class PropertyReflectionHelper
    {
        public static List<LockablePropertyData> ExtractLockablePropertiesFromStep(IStepData data)
        {

            List<LockablePropertyData> result = new List<LockablePropertyData>();

            if (data == null)
            {
                return result;
            }

            foreach (ITransition transition in data.Transitions.Data.Transitions)
            {
                foreach (ICondition condition in transition.Data.Conditions)
                {
                    result.AddRange(ExtractLockablePropertiesFromConditions(condition.Data));
                    result.AddRange(ExtractLockablePropertiesFromConditionTags(condition.Data));
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts all <see cref="ISceneObjectProperty"/> from type T from the given condition.
        /// </summary>
        /// <param name="data">Condition to be used for extraction</param>
        /// <param name="checkRequiredComponentsToo">if true the [RequiredComponents] will be checked and added too.</param>
        public static List<ISceneObjectProperty> ExtractPropertiesFromConditions(IConditionData data, bool checkRequiredComponentsToo = true)
        {
            List<ISceneObjectProperty> result = new List<ISceneObjectProperty>();

            List<MemberInfo> memberInfo = GetAllPropertyReferencesFromCondition(data);
            memberInfo.ForEach(info =>
            {
                UniqueNameReference reference = ReflectionUtils.GetValueFromPropertyOrField(data, info) as UniqueNameReference;

                if (reference == null || string.IsNullOrEmpty(reference.UniqueName))
                {
                    return;
                }

                if (RuntimeConfigurator.Configuration.SceneObjectRegistry.ContainsName(reference.UniqueName) == false)
                {
                    return;
                }

                IEnumerable<Type> refs = ExtractFittingPropertyType<ISceneObjectProperty>(reference.GetReferenceType());

                Type refType = refs.FirstOrDefault();
                if (refType != null)
                {
                    IEnumerable<Type> types = new[] {refType};
                    if (checkRequiredComponentsToo)
                    {
                        types = GetDependenciesFrom(refType);
                    }

                    foreach (Type type in types)
                    {
                        ISceneObjectProperty property = GetFittingPropertyFromReference<ISceneObjectProperty>(reference, type);
                        if (property != null)
                        {
                            result.Add(property);
                        }
                    }
                }
            });

            return result;
        }

        public static List<LockablePropertyData> ExtractLockablePropertiesFromConditionTags(IConditionData data, bool checkRequiredComponentsToo = true)
        {
            List<LockablePropertyData> result = new List<LockablePropertyData>();

            List<MemberInfo> memberInfo = GetAllPropertiesInTagsFromCondition(data);
            memberInfo.ForEach(info =>
            {
                SceneObjectTagBase reference = ReflectionUtils.GetValueFromPropertyOrField(data, info) as SceneObjectTagBase;

                if (reference == null || reference.Guid == null || reference.Guid == Guid.Empty)
                {
                    return;
                }

                if (SceneObjectTags.Instance.TagExists(reference.Guid) == false)
                {
                    return;
                }

                IEnumerable<ISceneObject> taggedObjects = RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByTag(reference.Guid);

                if (taggedObjects.Count() == 0)
                {
                    return;
                }

                IEnumerable<Type> refs = ExtractFittingPropertyType<LockableProperty>(reference.GetReferenceType());

                foreach (ISceneObject taggedObject in taggedObjects)
                {
                    Type refType = refs.Where(type => taggedObject.Properties.Select(property => property.GetType()).Contains(type)).FirstOrDefault();
                    if (refType != null)
                    {
                        IEnumerable<Type> types = new[] { refType };
                        if (checkRequiredComponentsToo)
                        {
                            types = GetDependenciesFrom<LockableProperty>(refType);
                        }

                        foreach (Type type in types)
                        {
                            LockableProperty property = taggedObject.Properties.FirstOrDefault(property => property.GetType() == type) as LockableProperty;                            
                            if (property != null)
                            {
                                result.Add(new LockablePropertyData(property));
                            }
                        }
                    }
                }
            });

            return result;
        }

        /// <summary>
        /// Extracts all <see cref="LockableProperties"/> from given condition.
        /// </summary>
        /// <param name="data">Condition to be used for extraction</param>
        /// <param name="checkRequiredComponentsToo">if true the [RequiredComponents] will be checked and added too.</param>
        public static List<LockablePropertyData> ExtractLockablePropertiesFromConditions(IConditionData data, bool checkRequiredComponentsToo = true)
        {
            List<LockablePropertyData> result = new List<LockablePropertyData>();

            List<MemberInfo> memberInfo = GetAllPropertyReferencesFromCondition(data);
            memberInfo.ForEach(info =>
            {
                UniqueNameReference reference = ReflectionUtils.GetValueFromPropertyOrField(data, info) as UniqueNameReference;

                if (reference == null || string.IsNullOrEmpty(reference.UniqueName))
                {
                    return;
                }

                if (RuntimeConfigurator.Configuration.SceneObjectRegistry.ContainsName(reference.UniqueName) == false)
                {
                    return;
                }

                IEnumerable<Type> refs = ExtractFittingPropertyType<LockableProperty>(reference.GetReferenceType());

                ISceneObject sceneObject = RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByName(reference.UniqueName);
                Type refType = refs.Where(type => sceneObject.Properties.Select(property => property.GetType()).Contains(type)).FirstOrDefault();
                if (refType != null)
                {
                    IEnumerable<Type> types = new[] {refType};
                    if (checkRequiredComponentsToo)
                    {
                        types = GetDependenciesFrom<LockableProperty>(refType);
                    }

                    foreach (Type type in types)
                    {
                        LockableProperty property = GetFittingPropertyFromReference<LockableProperty>(reference, type);
                        if (property != null)
                        {
                            result.Add(new LockablePropertyData(property));
                        }
                    }
                }
            });

            return result;
        }

        /// <summary>
        /// Returns all concrete runtime property types derived from T.
        /// </summary>
        public static IEnumerable<Type> ExtractFittingPropertyType<T>(Type referenceType) where T : ISceneObjectProperty
        {
            IEnumerable<Type> refs = ReflectionUtils.GetConcreteImplementationsOf(referenceType);
            refs = refs.Where(typeof(T).IsAssignableFrom);

            if (UnitTestChecker.IsUnitTesting == false)
            {
                refs = refs.Where(type => type.Assembly.GetReferencedAssemblies().All(name => name.Name != "nunit.framework"));
                if (Application.isEditor == false)
                {
                    refs = refs.Where(type => type.Assembly.GetReferencedAssemblies().All(name => name.Name != "UnityEditor"));
                }
            }

            return refs;
        }

        private static List<MemberInfo> GetAllPropertyReferencesFromCondition(IConditionData conditionData)
        {
            List<MemberInfo> memberInfo = conditionData.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(info =>
                    info.PropertyType.IsConstructedGenericType && info.PropertyType.GetGenericTypeDefinition() ==
                    typeof(ScenePropertyReference<>))
                .Cast<MemberInfo>()
                .ToList();

            memberInfo.AddRange(conditionData.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(info =>
                    info.FieldType.IsConstructedGenericType && info.FieldType.GetGenericTypeDefinition() ==
                    typeof(ScenePropertyReference<>)));

            return memberInfo;
        }

        private static List<MemberInfo> GetAllPropertiesInTagsFromCondition(IConditionData conditionData)
        {
            List<MemberInfo> memberInfo = conditionData.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(info =>
                    info.PropertyType.IsConstructedGenericType && info.PropertyType.GetGenericTypeDefinition() ==
                    typeof(SceneObjectTag<>))
                .Cast<MemberInfo>()
                .ToList();

            memberInfo.AddRange(conditionData.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(info =>
                    info.FieldType.IsConstructedGenericType && info.FieldType.GetGenericTypeDefinition() ==
                    typeof(SceneObjectTag<>)));

            return memberInfo;
        }

        private static T GetFittingPropertyFromReference<T>(UniqueNameReference reference, Type type) where T : class, ISceneObjectProperty
        {
            ISceneObject sceneObject = RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByName(reference.UniqueName);
            foreach (ISceneObjectProperty prop in sceneObject.Properties)
            {
                if (prop.GetType() == type)
                {
                    return prop as T;
                }
            }
            Debug.LogWarningFormat("Could not find fitting {0} type in SceneObject {1}", type.Name, reference.UniqueName);
            return null;
        }

        /// <summary>
        /// Get process scene properties which the given type dependence on.
        /// </summary>
        private static IEnumerable<Type> GetDependenciesFrom(Type processProperty)
        {
            return GetDependenciesFrom<ISceneObjectProperty>(processProperty);
        }

        /// <summary>
        /// Get process scene properties which the given type dependence on, which has to be a subclass of <T>
        /// </summary>
        private static IEnumerable<Type> GetDependenciesFrom<T>(Type processProperty) where T : ISceneObjectProperty
        {
            List<Type> dependencies = new List<Type>();
            IEnumerable<Type> requiredComponents = processProperty.GetCustomAttributes(typeof(RequireComponent), false)
                .Cast<RequireComponent>()
                .SelectMany(rq => new []{rq.m_Type0, rq.m_Type1, rq.m_Type2});

            foreach (Type requiredComponent in requiredComponents)
            {
                if (requiredComponent != null && requiredComponent.IsSubclassOf(typeof(T)))
                {
                    dependencies.AddRange(GetDependenciesFrom<T>(requiredComponent));
                }
            }

            dependencies.Add(processProperty);
            return new HashSet<Type>(dependencies);
        }
    }
}
