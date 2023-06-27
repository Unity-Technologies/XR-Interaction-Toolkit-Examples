using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRBuilder.Core.Properties;
using VRBuilder.Core.SceneObjects;

namespace VRBuilder.Editor.BuilderMenu
{
    /// <summary>
    /// Adds all property extensions required by the current scene setup to the objects in the scene.    
    /// </summary>
    internal static class AddPropertyExtensionsMenuEntry
    {
        [MenuItem("Tools/VR Builder/Developer/Add Scene Property Extensions", false, 70)]
        private static void AddPropertyExtensions()
        {
            if(EditorUtility.DisplayDialog("Add Scene Property Extensions?", "This will add the extensions required by the current scene setup to all scene object properties in the scene.\n" +
                "Previously added extensions will not be removed.\n" +
                "Continue?", "Ok", "Cancel"))
            {
                IEnumerable<ProcessSceneObject> processSceneObjects = GameObject.FindObjectsOfType<ProcessSceneObject>(true);
                float processedObjects = 0;

                foreach (ProcessSceneObject processSceneObject in processSceneObjects)
                {
                    processedObjects++;
                    EditorUtility.DisplayProgressBar("Adding property extensions", $"Processing scene object: {processSceneObject.gameObject.name}", processedObjects / processSceneObjects.Count());

                    foreach (ISceneObjectProperty property in processSceneObject.Properties)
                    {
                        property.AddProcessPropertyExtensions();
                    }
                }

                EditorUtility.ClearProgressBar();
            }
        }
    }
}
