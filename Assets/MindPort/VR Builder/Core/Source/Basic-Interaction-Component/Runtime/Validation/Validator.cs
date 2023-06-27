using UnityEngine;

namespace VRBuilder.BasicInteraction.Validation
{
    /// <summary>
    /// Base validator used to implement concrete validators.
    /// </summary>
    public abstract class Validator : MonoBehaviour
    {
        /// <summary>
        /// When this returns true, the given object is allowed to be snapped.
        /// </summary>
        public abstract bool Validate(GameObject obj);

        private void OnEnable()
        {
            // Has to be implemented to allow disabling this script.
        }

        private void OnDisable()
        {
            // Has to be implemented to allow disabling this script.
        }
    }
}