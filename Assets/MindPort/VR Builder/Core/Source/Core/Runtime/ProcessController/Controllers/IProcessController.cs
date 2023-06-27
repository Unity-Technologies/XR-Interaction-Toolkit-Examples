using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRBuilder.UX
{
    /// <summary>
    /// Controller for managing the process.
    /// Can for example instantiate a controller menu and a spectator camera.
    /// </summary>
    public interface IProcessController
    {
        /// <summary>
        /// Prettified name.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Priority of the controller.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets a process controller game object.
        /// </summary>
        GameObject GetProcessControllerPrefab();

        /// <summary>
        /// List of component types which are required on the setup object.
        /// </summary>
        /// <returns>List of component types.</returns>
        List<Type> GetRequiredSetupComponents();

        /// <summary>
        /// Handles post-setup logic.
        /// Should be called after all components are added and object is initialized.
        /// </summary>
        /// <param name="processControllerObject">Actual process controller object</param>
        void HandlePostSetup(GameObject processControllerObject);
    }
}