// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Reflection;
using UnityEngine;


namespace Meta.Utilities.Narrative
{
    /// <summary>
    /// Serialized wrapper for accessing fields, properties and methods via reflection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class MemberLink<T>
    {
        public enum Kind { Field, Property, Method }

        [SerializeField] private Component m_targetObject;
        [SerializeField] private string m_memberName;

        [SerializeField, HideInInspector] private Kind m_kind;

        public Component TargetObject => m_targetObject;
        public string MemberName => m_memberName;

        public T GetValue()
        {
            if (!m_targetObject)
            {
                Debug.LogError("No target object for member link; cannot get value");
                return default;
            }

            if (string.IsNullOrWhiteSpace(m_memberName))
            {
                Debug.LogError("No member specified in member link; cannot get value");
                return default;
            }

            var type = m_targetObject.GetType();

            return m_kind switch
            {
                Kind.Field => GetFieldValue(type),
                Kind.Property => GetPropertyValue(type),
                Kind.Method => GetMethodValue(type),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private T GetMethodValue(Type type)
        {
            var method = type.GetRuntimeMethod(m_memberName, Type.EmptyTypes);

            if (method == null)
            {
                Debug.LogError($"Member '{m_memberName}' of type '{type.Name}' is null; "
                               + "cannot get value");

                return default;
            }

            if (typeof(T).IsAssignableFrom(method.ReturnType))
            {
                return (T)method.Invoke(m_targetObject, Array.Empty<object>());
            }

            Debug.LogError($"Method '{m_memberName}' of type '{type.Name}' "
                           + $"does not return a {typeof(T).Name}; cannot get value");

            return default;
        }

        private T GetPropertyValue(Type type)
        {
            var prop = type.GetRuntimeProperty(m_memberName);

            if (prop == null)
            {
                Debug.LogError($"Member '{m_memberName}' of type '{type.Name}' is null; "
                               + "cannot get value");

                return default;
            }

            if (typeof(T).IsAssignableFrom(prop.PropertyType))
            {
                return (T)prop.GetValue(m_targetObject);
            }

            Debug.LogError($"Property '{m_memberName}' of type '{type.Name}' "
                           + $"is not of type {typeof(T).Name}; cannot get value");

            return default;
        }

        private T GetFieldValue(Type type)
        {
            var field = type.GetRuntimeField(m_memberName);

            if (field == null)
            {
                Debug.LogError($"Member '{m_memberName}' of type '{type.Name}' is null; "
                               + "cannot get value");

                return default;
            }

            if (typeof(T).IsAssignableFrom(field.FieldType))
            {
                return (T)field.GetValue(m_targetObject);
            }

            Debug.LogError($"Field '{m_memberName}' of type '{type.Name}' "
                           + $"is not of type {typeof(T).Name}; cannot get value");

            return default;
        }
    }
}