// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using UnityEngine;

namespace Meta.Utilities.Narrative
{
    /// <summary>
    /// Attribute for searching all objects in scene of a type for a string field to make a ui dropdown
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class Dropdown : PropertyAttribute
    {
        public Type SourceType { get; private set; }
        public string FieldName { get; private set; }

        public Dropdown(Type sourceType, string fieldName)
        {
            SourceType = sourceType;
            FieldName = fieldName;
        }
    }
}
