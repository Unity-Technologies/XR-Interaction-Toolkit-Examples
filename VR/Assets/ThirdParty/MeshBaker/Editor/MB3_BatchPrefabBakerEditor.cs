using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DigitalOpus.MB.Core;
using System.Text.RegularExpressions;

namespace DigitalOpus.MB.MBEditor
{
    [CustomEditor(typeof(MB3_BatchPrefabBaker))]
    public class MB3_BatchPrefabBakerEditor : Editor
    {
        SerializedObject prefabBaker = null;

        GUIContent GUIContentLogLevelContent = new GUIContent("Log Level");
        GUIContent GUIContentBatchBakePrefabReplacePrefab = new GUIContent("Batch Bake Prefabs (Replace Prefab)",
            "This will clone the source prefab, replace the meshes in the clone with baked versions, and replace the target prefab with the clone.\n\n" +
            "IF ANY CHANGES HAD BEEN MADE TO THE TARGET PREFAB, THOSE WILL BE LOST.");
        GUIContent GUIContentBatchBakePrefabOnlyMeshesAndMats = new GUIContent("Batch Bake Prefabs (Only Replace Meshes & Materials)",
            "This will attempt to match the meshes used by the target prefab to those used by the source prefab. For this to work well," +
            " the source and target prefabs should have the same hierarchy. The meshes and materials in the target prefab will be updated to baked versions." +
            " Modifications to the target prefab other than the meshes and materials will be preserved.\n\n" +
            "Check the console for errors after baking the prefabs.");

        SerializedProperty prefabRows, outputFolder, logLevel;

        Color buttonColor = new Color(.8f, .8f, 1f, 1f);

        [MenuItem("GameObject/Create Other/Mesh Baker/Batch Prefab Baker", false, 1000)]
        public static void CreateNewBatchPrefabBaker()
        {
            //if (MB3_MeshCombiner.EVAL_VERSION)
            //{
            //    Debug.LogError("The prefab baker is only available in the full version of MeshBaker.");
            //    return;
            //}

            MB3_TextureBaker[] mbs = (MB3_TextureBaker[])Editor.FindObjectsOfType(typeof(MB3_TextureBaker));
            // Generate unique name
            int largest = 0;
            {
                Regex regex = new Regex(@"\((\d+)\)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
                try
                {
                    for (int i = 0; i < mbs.Length; i++)
                    {
                        Match match = regex.Match(mbs[i].name);
                        if (match.Success)
                        {
                            int val = Convert.ToInt32(match.Groups[1].Value);
                            if (val >= largest)
                                largest = val + 1;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e == null) e = null; //Do nothing supress compiler warning
                }
            }

            GameObject nmb = new GameObject("BatchPrefabBaker (" + largest + ")");
            nmb.transform.position = Vector3.zero;

            MB3_BatchPrefabBaker bpb = nmb.AddComponent<MB3_BatchPrefabBaker>();
            nmb.AddComponent<MB3_TextureBaker>();
            nmb.AddComponent<MB3_MeshBaker>();
            bpb.prefabRows = new MB3_BatchPrefabBaker.MB3_PrefabBakerRow[0];
            bpb.outputPrefabFolder = "";
        }

        void OnEnable()
        {
            prefabBaker = new SerializedObject(target);
            prefabRows = prefabBaker.FindProperty("prefabRows");
            outputFolder = prefabBaker.FindProperty("outputPrefabFolder");
            logLevel = prefabBaker.FindProperty("LOG_LEVEL");
        }

        void OnDisable()
        {
            prefabBaker = null;
        }

        public override void OnInspectorGUI()
        {
            prefabBaker.Update();

            EditorGUILayout.HelpBox(
                                    "This tool speeds up the process of preparing prefabs" +
                                    " for static and dynamic batching. It creates duplicate prefab assets and meshes" +
                                    " that share a combined material. Source assets are not touched.\n\n" +
                                    "1) Create instances of source prefabs to this scene.\n" +
                                    "2) Add these instances to the TextureBaker on this GameObject and bake the textures used by the prefabs.\n" +
                                    "2) Using the BatchPrefabBaker component, click 'Populate Prefab Rows From Texture Baker' or manually set up Prefab Rows by dragging to the Prefab Rows list.\n" +
                                    "4) Choose a folder where the result prefabs will be stored and click 'Create Empty Result Prefabs'\n" +
                                    "5) click 'Batch Bake Prefabs'\n" +
                                    "6) Check the console for messages and errors\n" +
                                    "7) (Optional) If you want to compare the source objects to the result objects use the BatchPrefabBaker '...' menu command 'Create Instances For Prefab Rows'. This will create aligned instances of the prefabs in the scene so that it is easy to see any differences.\n", MessageType.Info);
            EditorGUILayout.PropertyField(logLevel, GUIContentLogLevelContent);

            EditorGUILayout.PropertyField(prefabRows, true);

            EditorGUILayout.LabelField("Output Folder", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(outputFolder.stringValue);

            if (GUILayout.Button("Browse For Output Folder"))
            {
                string path = EditorUtility.OpenFolderPanel("Browse For Output Folder", "", "");
                path = MB_BatchPrefabBakerEditorFunctions.ConvertFullPathToProjectRelativePath(path);
                outputFolder.stringValue = path;
            }

            if (GUILayout.Button("Create Empty Result Prefabs"))
            {
                 MB_BatchPrefabBakerEditorFunctions.CreateEmptyOutputPrefabs(outputFolder.stringValue, (MB3_BatchPrefabBaker) target);
            }

            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button(GUIContentBatchBakePrefabReplacePrefab))
            {
                MB3_BatchPrefabBaker pb = (MB3_BatchPrefabBaker)target;
                MB_BatchPrefabBakerEditorFunctions.BakePrefabs(pb, true);
            }
            if (GUILayout.Button(GUIContentBatchBakePrefabOnlyMeshesAndMats))
            {
                MB3_BatchPrefabBaker pb = (MB3_BatchPrefabBaker)target;
                MB_BatchPrefabBakerEditorFunctions.BakePrefabs(pb, false);
            }
            GUI.backgroundColor = oldColor;

            if (GUILayout.Button("Populate Prefab Rows From Texture Baker"))
            {
                PopulatePrefabRowsFromTextureBaker((MB3_BatchPrefabBaker)prefabBaker.targetObject);
            }

            if (GUILayout.Button("Open Replace Prefabs In Scene Window"))
            {
                MB3_BatchPrefabBaker pb = (MB3_BatchPrefabBaker)target;
                MB_ReplacePrefabsInSceneEditorWindow.ShowWindow(pb.prefabRows);
            }


            prefabBaker.ApplyModifiedProperties();
            prefabBaker.SetIsDifferentCacheDirty();
        }

        public void PopulatePrefabRowsFromTextureBaker(MB3_BatchPrefabBaker prefabBaker)
        {
            MB3_TextureBaker texBaker = prefabBaker.GetComponent<MB3_TextureBaker>();
            List<GameObject> newPrefabs = new List<GameObject>();
            List<GameObject> gos = texBaker.GetObjectsToCombine();
            for (int i = 0; i < gos.Count; i++)
            {
                GameObject go = (GameObject)MBVersionEditor.PrefabUtility_FindPrefabRoot(gos[i]);
                UnityEngine.Object obj = MBVersionEditor.PrefabUtility_GetCorrespondingObjectFromSource(go);

                if (obj != null && obj is GameObject)
                {
                    if (!newPrefabs.Contains((GameObject)obj)) newPrefabs.Add((GameObject)obj);
                }
                else
                {
                    Debug.LogWarning(String.Format("Object {0} did not have a prefab", gos[i]));
                }

            }

            // Remove prefabs that are already in the list of batch prefab baker's prefabs.
            {
                List<GameObject> tmpNewPrefabs = new List<GameObject>();
                for (int i = 0; i < newPrefabs.Count; i++)
                {
                    bool found = false;
                    for (int j = 0; j < prefabBaker.prefabRows.Length; j++)
                    {
                        if (prefabBaker.prefabRows[j].sourcePrefab == newPrefabs[i])
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        tmpNewPrefabs.Add(newPrefabs[i]);
                    }
                }

                newPrefabs = tmpNewPrefabs;
            }

            if (MB3_MeshCombiner.EVAL_VERSION)
            {
                int numPrefabsLimit = MB_BatchPrefabBakerEditorFunctions.EvalVersionPrefabLimit;
                int numNew = numPrefabsLimit - prefabBaker.prefabRows.Length;
                if (newPrefabs.Count + prefabBaker.prefabRows.Length > numPrefabsLimit)
                {
                    Debug.LogError("The free version of Mesh Baker is limited to batch baking " + numPrefabsLimit +
                        " prefabs. The full version has no limit on the number of prefabs that can be baked. " + (newPrefabs.Count - numNew) + " prefabs were not added.");

                }

                
                for (int i = newPrefabs.Count - 1; i >= numNew; i--)
                {
                    newPrefabs.RemoveAt(i);
                }
            }

            List<MB3_BatchPrefabBaker.MB3_PrefabBakerRow> newRows = new List<MB3_BatchPrefabBaker.MB3_PrefabBakerRow>();
            if (prefabBaker.prefabRows == null) prefabBaker.prefabRows = new MB3_BatchPrefabBaker.MB3_PrefabBakerRow[0];
            newRows.AddRange(prefabBaker.prefabRows);
            for (int i = 0; i < newPrefabs.Count; i++)
            {
                MB3_BatchPrefabBaker.MB3_PrefabBakerRow row = new MB3_BatchPrefabBaker.MB3_PrefabBakerRow();
                row.sourcePrefab = newPrefabs[i];
                newRows.Add(row);
            }


            Undo.RecordObject(prefabBaker, "Populate prefab rows");
            prefabBaker.prefabRows = newRows.ToArray();
        }
    }
}
