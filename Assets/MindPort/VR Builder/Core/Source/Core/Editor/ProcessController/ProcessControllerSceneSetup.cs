using VRBuilder.Unity;
using VRBuilder.UX;
using UnityEngine;
using VRBuilder.Editor.Setup;

namespace VRBuilder.Editor.UX
{
    /// <summary>
    /// Will be called on OnSceneSetup to add the process controller menu.
    /// </summary>
    public class ProcessControllerSceneSetup : SceneSetup
    {
        /// <inheritdoc />
        public override int Priority { get; } = 20;
        
        /// <inheritdoc />
        public override string Key { get; } = "ProcessControllerSetup";
        
        /// <inheritdoc />
        public override void Setup(ISceneSetupConfiguration configuration)
        {
            GameObject processController = SetupPrefab("PROCESS_CONTROLLER");
            if (processController != null)
            {
                ProcessControllerSetup processControllerSetup = processController.GetOrAddComponent<ProcessControllerSetup>();
                processControllerSetup.ResetToDefault();
                processControllerSetup.SetProcessControllerQualifiedName(configuration.DefaultProcessController);
            }
        }
    }
}
