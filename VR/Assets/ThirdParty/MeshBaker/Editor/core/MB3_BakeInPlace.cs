using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.Core
{

    public class MB3_BakeInPlace
    {

        public static bool BakeMeshesInPlace(MB3_MeshCombinerSingle mom, List<GameObject> objsToMesh, string saveFolder, bool clearBuffersAfterBake, ProgressUpdateDelegate updateProgressBar)
        {
            if (MB3_MeshCombiner.EVAL_VERSION) return false;
            if (saveFolder.Length < 6)
            {
                Debug.LogError("Please select a folder for meshes.");
                return false;
            }
            if (!Directory.Exists(Application.dataPath + saveFolder.Substring(6)))
            {
                Debug.Log((Application.dataPath + saveFolder.Substring(6)));
                Debug.Log(Path.GetFullPath(Application.dataPath + saveFolder.Substring(6)));
                Debug.LogError("The selected Folder For Meshes does not exist or is not inside the projects Assets folder. Please 'Choose Folder For Bake In Place Meshes' that is inside the project's assets folder.");
                return false;
            }

            MB3_EditorMethods editorMethods = new MB3_EditorMethods();
            mom.DestroyMeshEditor(editorMethods);

            MB_RenderType originalRenderType = mom.settings.renderType;
            bool success = false;
            string[] objNames = GenerateNames(objsToMesh);
            for (int i = 0; i < objsToMesh.Count; i++)
            {
                if (objsToMesh[i] == null)
                {
                    Debug.LogError("The " + i + "th object on the list of objects to combine is 'None'. Use Command-Delete on Mac OS X; Delete or Shift-Delete on Windows to remove this one element.");
                    return false;
                }

                Mesh m = new Mesh();
                success = BakeOneMesh(mom, m, objsToMesh[i]);
                if (success)
                {
                    string newMeshFilePath = saveFolder + "/" + objNames[i];
                    Debug.Log("Creating mesh asset at " + newMeshFilePath + " for mesh " + m + " numVerts " + m.vertexCount);
                    AssetDatabase.CreateAsset(mom.GetMesh(), newMeshFilePath);
                }
                if (updateProgressBar != null) updateProgressBar("Created mesh saving mesh on " + objsToMesh[i].name + " to asset " + objNames[i], .6f);
            }
            mom.settings.renderType = originalRenderType;
            MB_Utility.Destroy(mom.resultSceneObject);
            if (clearBuffersAfterBake) { mom.ClearBuffers(); }
            return success;
        }

        static public bool BakeOneMesh(MB3_MeshCombinerSingle mom, Mesh targMesh, GameObject objToBake)
        {
            if (objToBake == null)
            {
                Debug.LogError("An object on the list of objects to combine is 'None'. Use Command-Delete on Mac OS X; Delete or Shift-Delete on Windows to remove this one element.");
                return false;
            }
            if (targMesh == null)
            {

                Debug.LogError("No mesh was provided.");
                return false;
            }

            mom.SetMesh(targMesh);
            mom.ClearMesh();
            GameObject[] objs = new GameObject[] { objToBake };
            Renderer r = MB_Utility.GetRenderer(objToBake);
            if (r is SkinnedMeshRenderer)
            {
                mom.settings.renderType = MB_RenderType.skinnedMeshRenderer;
            }
            else if (r is MeshRenderer)
            {
                mom.settings.renderType = MB_RenderType.meshRenderer;
            }
            else
            {
                Debug.LogError("Unsupported Renderer type on object. Must be SkinnedMesh or MeshFilter.");
                return false;
            }
            if (mom.AddDeleteGameObjects(objs, null, false))
            {
                mom.Apply(MB3_MeshBakerEditorFunctions.UnwrapUV2);
                Mesh mf = MB_Utility.GetMesh(objToBake);
                if (mf == null)
                {
                    Debug.LogError("Failed to create mesh for " + objToBake.name);
                    return false;
                }
            }

            return true;
        }

        public static string[] GenerateNames(List<GameObject> objsToMesh)
        {
            string[] ns = new string[objsToMesh.Count];
            for (int i = 0; i < objsToMesh.Count; i++)
            {
                string newNameBase = objsToMesh[i].name;
                string newName = newNameBase + ".asset";
                int j = 1;
                while (ArrayUtility.Contains<string>(ns, objsToMesh[i].name))
                {
                    newName = newNameBase + "-" + j + ".asset";
                    j++;
                }
                ns[i] = newName;
            }
            return ns;
        }
    }
}