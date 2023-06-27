using System;
using System.Linq;
using UnityEngine;
using VRBuilder.Core.Utils;
using UnityEngine.SceneManagement;

namespace VRBuilder.BasicInteraction
{
    /// <summary>
    /// Base interaction simulator, only have one concrete simulator implementation in your project.
    /// If no concrete implementation is found a <see cref="BaseInteractionSimulatorDummy"/> will be used.
    /// </summary>
    public abstract class BaseInteractionSimulator
    {
        private static BaseInteractionSimulator instance;
        
        /// <summary>
        /// Current instance of the interaction simulator.
        /// </summary>
        public static BaseInteractionSimulator Instance
        {
            get
            {
                if (instance == null)
                {
                    Type type = ReflectionUtils
                        .GetConcreteImplementationsOf(typeof(BaseInteractionSimulator))
                        .FirstOrDefault(t => t != typeof(BaseInteractionSimulatorDummy));

                    if (type == null)
                    {
                        type = typeof(BaseInteractionSimulatorDummy);
                    }

                    instance = (BaseInteractionSimulator)ReflectionUtils.CreateInstanceOfType(type);
                    SceneManager.sceneUnloaded += OnSceneLoad;
                }

                return instance;
            }
        }

        private static void OnSceneLoad(Scene scene)
        {
            instance = null;
            SceneManager.sceneUnloaded -= OnSceneLoad;
        }

        /// <summary>
        /// Simulates touching the given object. Expected behavior is that the object stays touched until StopTouch is called.
        /// </summary>
        public abstract void Touch(IInteractableObject interactable);

        /// <summary>
        /// Simulates stop touching the given object.
        /// </summary>
        public abstract void StopTouch();

        /// <summary>
        /// Simulates grabbing the given object.
        /// </summary>
        public abstract void Grab(IInteractableObject interactable);

        /// <summary>
        /// Simulates releasing the given object.
        /// </summary>
        public abstract void Release();

        /// <summary>
        /// Simulates usage of the object and keeps using the given object until StopUse is called.
        /// </summary>
        public abstract void Use(IInteractableObject interactable);

        /// <summary>
        /// Simulates stop using the given object.
        /// </summary>
        public abstract void StopUse(IInteractableObject interactable);

        /// <summary>
        /// Simulates a hover over a SnapZone.
        /// </summary>
        public abstract void HoverSnapZone(ISnapZone snapZone, IInteractableObject interactable);
    
        /// <summary>
        /// Simulates a unhover over a SnapZone.
        /// </summary>
        public abstract void UnhoverSnapZone(ISnapZone snapZone, IInteractableObject interactable);
        
        /// <summary>
        /// Returns the base class used for teleportation in your VR framework.
        /// </summary>
        public abstract Type GetTeleportationBaseType();

        /// <summary>
        /// Executes a teleport action.
        /// </summary>
        /// <param name="rig">The rig object.</param>
        /// <param name="teleportationObject">The object with the teleportation logic or used to teleport into.</param>
        /// <param name="targetPosition">Desired position.</param>
        /// <param name="targetRotation">Desired rotation</param>
        public abstract void Teleport(GameObject rig, GameObject teleportationObject, Vector3 targetPosition, Quaternion targetRotation);

        /// <summary>
        /// True if the provided <paramref name="colliderToValidate"/> is an active collider of the <paramref name="teleportationObject"/>
        /// </summary>
        /// <param name="teleportationObject">The object with the teleportation logic or used to teleport into.</param>
        /// <param name="colliderToValidate">Collider to validate.</param>
        /// <returns></returns>
        public abstract bool IsColliderValid(GameObject teleportationObject, Collider colliderToValidate);
    }
}
