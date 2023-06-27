using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRBuilder.Core.SceneObjects;

namespace VRBuilder.Core.Configuration
{
    /// <summary>
    /// Default single-user implementation of <see cref="ISceneObjectManager"/>.
    /// </summary>
    public class DefaultSceneObjectManager : ISceneObjectManager
    {
        /// <inheritdoc/>
        public void SetSceneObjectActive(ISceneObject sceneObject, bool isActive)
        {
            sceneObject.GameObject.SetActive(isActive);
        }

        /// <inheritdoc/>
        public void SetComponentActive(ISceneObject sceneObject, string componentTypeName, bool isActive)
        {
            IEnumerable<Component> components = sceneObject.GameObject.GetComponents<Component>().Where(c => c.GetType().Name == componentTypeName);

            foreach (Component component in components)
            {
                Type componentType = component.GetType();

                if (componentType.GetProperty("enabled") != null)
                {
                    componentType.GetProperty("enabled").SetValue(component, isActive, null);
                }
            }
        }

        /// <inheritdoc/>
        public GameObject InstantiatePrefab(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return GameObject.Instantiate(prefab, position, rotation);
        }

        /// <inheritdoc/>
        public void RequestAuthority(ISceneObject sceneObject)
        {
        }
    }
}
