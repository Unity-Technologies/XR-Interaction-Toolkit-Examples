using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRBuilder.Core.Utils;
using VRBuilder.ProcessController;

namespace VRBuilder.UX
{
    /// <summary>
    /// Manages the setup of the process controller and lets the developer choose the <see cref="IProcessController"/>.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class ProcessControllerSetup : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField, SerializeReference]
        private string processControllerQualifiedName;

        [SerializeField, SerializeReference]
        private bool autoStartProcess = true;

        [SerializeField, SerializeReference]
        private bool useCustomPrefab;
        
        [SerializeField, SerializeReference]
        private GameObject customPrefab;
#pragma warning restore 0649
        
        /// <summary>
        /// Current used process controller.
        /// </summary>
        public IProcessController CurrentProcessController { get; protected set; }
        
        /// <summary>
        /// Enforced process controller will be use.
        /// </summary>
        protected static IProcessController enforcedProcessController = null;

        protected virtual void OnEnable()
        {
            Setup();
        }

        private void Setup()
        {
            CreateProcessController();

            if (CurrentProcessController != null)
            {
                AddComponents(CurrentProcessController.GetRequiredSetupComponents());
                CurrentProcessController.HandlePostSetup(gameObject);
            }
        }

        private void CreateProcessController()
        {
            if (enforcedProcessController != null)
            {
                CurrentProcessController = enforcedProcessController;
            }
            else if (useCustomPrefab && customPrefab != null)
            {
                Instantiate(customPrefab);
                return;
            }
            
            IProcessController defaultProcessController = GetProcessControllerFromType();
            
            if (CurrentProcessController == null)
            {
                CurrentProcessController = defaultProcessController;
                if (CurrentProcessController == null)
                {
                    Debug.LogError("ProcessControllerSetup was not configured properly.");
                    return;
                }
            }
            else
            {
                RemoveComponents(defaultProcessController.GetRequiredSetupComponents().Except(CurrentProcessController.GetRequiredSetupComponents()).ToList());
            }
            
            GameObject processControllerPrefab = CurrentProcessController.GetProcessControllerPrefab();
            if (processControllerPrefab != null)
            {
                GameObject processController = Instantiate(CurrentProcessController.GetProcessControllerPrefab());
                IConfigurableProcessController configurableController = processController.GetComponent<IConfigurableProcessController>();
                if(configurableController != null)
                {
                    configurableController.AutoStartProcess = autoStartProcess;
                }
            }
        }

        private IProcessController GetProcessControllerFromType()
        {
            Type processControllerType = RetrieveProcessControllerType();
            IProcessController processController = ReflectionUtils.CreateInstanceOfType(processControllerType) as IProcessController;

            return processController;
        }

        private Type RetrieveProcessControllerType()
        {
            if (string.IsNullOrEmpty(processControllerQualifiedName))
            {
                return RetrieveDefaultControllerType();
            }
            
            Type processControllerType = ReflectionUtils.GetTypeFromAssemblyQualifiedName(processControllerQualifiedName);
            return processControllerType != null ? processControllerType : RetrieveDefaultControllerType();
        }

        private Type RetrieveDefaultControllerType()
        {
            return ReflectionUtils.GetConcreteImplementationsOf<IProcessController>()
                .Select(c => (IProcessController) ReflectionUtils.CreateInstanceOfType(c)).OrderByDescending(controller => controller.Priority)
                .First()
                .GetType();
        }

        private void RemoveComponents(List<Type> components)
        {
            foreach (Type component in components)
            {
                DestroyImmediate(gameObject.GetComponent(component), true);
            }
        }

        private void AddComponents(List<Type> components)
        {
            if (components != null)
            {
                foreach (Type requiredComponent in components)
                {
                    if (gameObject.GetComponent(requiredComponent) == null)
                    {
                        gameObject.AddComponent(requiredComponent);
                    }
                }
            }
        }
        
        /// <summary>
        /// Enforces the given controller to be used, if possible.
        /// </summary>
        /// <param name="processController">Controller to be used.</param>
        /// <remarks>Scene has to be reloaded to use enforced ProcessController.</remarks>
        public static void SetEnforcedProcessController(IProcessController processController)
        {
            enforcedProcessController = processController;
        }

        public void SetProcessControllerQualifiedName(string processControllerQualifiedName)
        {
            this.processControllerQualifiedName = processControllerQualifiedName;
        }

        /// <summary>
        /// Resets the <cref name="processControllerQualifiedName"/> to the name of the process controller with the highest priority.
        /// </summary>
        /// <remarks>This may be used when instantiating a process controller prefab to make sure the default process controller is used.</remarks>
        public void ResetToDefault()
        {
            RemoveComponents(GetProcessControllerFromType().GetRequiredSetupComponents());
            Type processControllerType = RetrieveDefaultControllerType();
            processControllerQualifiedName = processControllerType.Name;
        }
    }
}
