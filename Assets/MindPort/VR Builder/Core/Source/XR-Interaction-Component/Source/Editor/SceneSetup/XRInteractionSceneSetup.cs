using UnityEditor;
using UnityEngine;
using VRBuilder.Editor.BasicInteraction;
using VRBuilder.Editor.PackageManager.XRInteraction;
using VRBuilder.Editor.Setup;

namespace VRBuilder.Editor.XRInteraction
{
    /// <summary>
    /// Scene setup for XR-Interaction.
    /// </summary>
    public class XRInteractionSceneSetup : InteractionFrameworkSceneSetup
    {
        private const string Title = "Obsolete XR Rig detected";
        
        /// <inheritdoc />
        public override string Key { get; } = "XRInteractionSetup";
        
        /// <inheritdoc />
        public override void Setup(ISceneSetupConfiguration configuration)
        {
            DeleteStaticObject("[XR_Setup]");
            
            XRSimulatorImporter simulatorImporter = new XRSimulatorImporter();

            if (string.IsNullOrEmpty(simulatorImporter.SimulatorRigPath) || AssetDatabase.GetMainAssetTypeAtPath(simulatorImporter.SimulatorRigPath) == null)
            {
                simulatorImporter.ImportSimulatorRig();
            }
        }

        private void DeleteStaticObject(string objectName)
        {
            GameObject objectToDelete = GameObject.Find(objectName);
            
            if (objectToDelete != null)
            {
                string message = $"VR Builder changed the XR Rig loading to a new dynamic system, you have a static {objectName} in the current scene, do you want to delete it?";
                
                if (EditorUtility.DisplayDialog(Title, message, "Delete", "Skip"))
                {
                    EditorUtility.SetDirty(objectToDelete);
                    Object.DestroyImmediate(objectToDelete);
                }
            }
        }
    }
}
