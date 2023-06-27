using System.Linq;
using VRBuilder.Core.SceneObjects;
using UnityEngine;

namespace VRBuilder.BasicInteraction.Validation
{
    /// <summary>
    /// Checks if the process object attached to the given GameObject is listed as accepted trainin scene object.
    /// </summary>
    public class IsProcessSceneObjectValidation : Validator
    {
        [SerializeField]
        [Tooltip("All listed process objects are valid to be snapped other will be rejected.")]
        private ProcessSceneObject[] acceptedProcessSceneObjects = {};

        /// <summary>
        /// Adds a new ProcessSceneObject to the list.
        /// </summary>
        public void AddProcessSceneObject(ProcessSceneObject target)
        {
            if (acceptedProcessSceneObjects.Contains(target) == false)
            {
                acceptedProcessSceneObjects = acceptedProcessSceneObjects.Append(target).ToArray();
            }
        }

        /// <summary>
        /// Removes an existing process object from the list.
        /// </summary>
        public void RemoveProcessSceneObject(ProcessSceneObject target)
        {
            if (acceptedProcessSceneObjects.Contains(target))
            {
                acceptedProcessSceneObjects = acceptedProcessSceneObjects.Where((obj => obj != target)).ToArray();
            }
        }
        
        /// <inheritdoc />
        public override bool Validate(GameObject obj)
        {
            ProcessSceneObject processSceneObject = obj.GetComponent<ProcessSceneObject>();

            if (processSceneObject == null)
            {
                return false;
            }
            
            if (acceptedProcessSceneObjects.Length == 0)
            {
                return true;
            }
            
            return acceptedProcessSceneObjects.Contains(processSceneObject);
        }
    }
}