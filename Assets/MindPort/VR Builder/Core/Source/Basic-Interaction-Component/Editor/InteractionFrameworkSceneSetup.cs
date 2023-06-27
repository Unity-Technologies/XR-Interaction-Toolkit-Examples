using UnityEngine;

namespace VRBuilder.Editor.BasicInteraction
{
    /// <summary>
    /// This base class is supposed to be implemented by classes which will be called to setup the scene,
    /// specifically interaction frameworks.
    /// </summary>
    public abstract class InteractionFrameworkSceneSetup : SceneSetup
    {
        /// <inheritdoc />
        public override int Priority { get; } = 10;

        /// <inheritdoc />
        public override string Key { get; } = "InteractionFrameworkSetup";
        
        /// <summary>
        /// Removes current MainCamera.
        /// </summary>
        protected void RemoveMainCamera()
        {
            if (Camera.main != null && Camera.main.transform.parent == null)
            {
                Object.DestroyImmediate(Camera.main.gameObject);
            }
        }
    }
}
