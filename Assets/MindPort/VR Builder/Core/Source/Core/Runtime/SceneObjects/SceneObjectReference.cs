// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Runtime.Serialization;
using VRBuilder.Core.Configuration;
using UnityEngine;

namespace VRBuilder.Core.SceneObjects
{
    /// <summary>
    /// Weak reference by a unique name to a process object in a scene.
    /// </summary>
    [DataContract(IsReference = true)]
    public sealed class SceneObjectReference : ObjectReference<ISceneObject>
    {
        public SceneObjectReference()
        {
        }

        public SceneObjectReference(string uniqueName) : base(uniqueName)
        {
        }

        protected override ISceneObject DetermineValue(ISceneObject cached)
        {
            if (string.IsNullOrEmpty(UniqueName))
            {
                return null;
            }

            ISceneObject value = cached;

            // If MonoBehaviour was destroyed, nullify the value.
            if (value != null && value.Equals(null))
            {
                value = null;
            }

            // If value exists, return it.
            if (value != null)
            {
                return value;
            }

            value = RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByName(UniqueName);
            return value;
        }
    }
}
