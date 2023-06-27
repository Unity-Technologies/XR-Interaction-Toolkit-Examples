using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRBuilder.UX
{
    /// <summary>
    /// Base process controller which instantiates a defined prefab.
    /// </summary>
    public abstract class BaseProcessController : IProcessController
    {
        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public abstract int Priority { get; }

        /// <summary>
        /// Name of the process controller prefab.
        /// </summary>
        protected abstract string PrefabName { get; }

        /// <inheritdoc />
        public virtual GameObject GetProcessControllerPrefab()
        {
            if (PrefabName == null)
            {
                Debug.LogError($"Could not find process controller prefab named {PrefabName}.");
                return null;
            }

            return Resources.Load<GameObject>($"Prefabs/{PrefabName}");
        }

        /// <inheritdoc />
        public virtual List<Type> GetRequiredSetupComponents() 
        {
            return new List<Type>();
        }

        /// <inheritdoc />
        public virtual void HandlePostSetup(GameObject processControllerObject)
        {
            // do nothing
        }
    }
}