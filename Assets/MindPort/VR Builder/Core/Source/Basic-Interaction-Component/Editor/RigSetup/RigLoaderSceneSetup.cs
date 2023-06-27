using VRBuilder.BasicInteraction.RigSetup;
using VRBuilder.Core.Properties;
using UnityEngine;
using VRBuilder.Editor.Setup;

namespace VRBuilder.Editor.BasicInteraction.RigSetup
{
    /// <summary>
    /// Setups the rig loader, cleans up the scene and creates a dummy user. 
    /// </summary>
    public class RigLoaderSceneSetup : SceneSetup
    {
        /// <inheritdoc />
        public override int Priority { get; } = 10;

        /// <inheritdoc />
        public override string Key { get; } = "InteractionFrameworkSetup";

        /// <inheritdoc/>
        public override void Setup(ISceneSetupConfiguration configuration)
        {
            RemoveMainCamera();

            InteractionRigSetup setup = Object.FindObjectOfType<InteractionRigSetup>();
            if (setup == null)
            {
                SetupPrefab("INTERACTION_RIG_LOADER");
                setup = Object.FindObjectOfType<InteractionRigSetup>();
                setup.UpdateRigList();
            }

            UserSceneObject user = Object.FindObjectOfType<UserSceneObject>();
            if (user == null)
            {
                SetupPrefab("USER_DUMMY");
                setup.DummyUser = GameObject.Find("USER_DUMMY");
            }
        }

        /// <summary>
        /// Removes current MainCamera.
        /// </summary>
        private void RemoveMainCamera()
        {
            if (Camera.main != null && Camera.main.transform.parent == null && Camera.main.gameObject.name != "USER_DUMMY")
            {
                Object.DestroyImmediate(Camera.main.gameObject);
            }
        }
    }
}