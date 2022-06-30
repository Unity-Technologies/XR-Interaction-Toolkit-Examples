using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.MBEditor
{
    public class MB_ReplacePrefabsInSceneEditorWindow : EditorWindow
    {
        private MB_ReplacePrefabsSettings model;
        private SerializedObject soModel;
        private SerializedProperty prefabPairs;
        private SerializedProperty reverseSrcAndTarg;
        private SerializedProperty enforceSrcAndTargHaveSameStructure;
        Vector2 scrollViewPos;
        private string errorMessages;
        private string infoMessages;

        private GUIContent GUI_replacePefabsInScene = new GUIContent("Replace Prefabs In Scene", "This will find all instances of the source prefabs in the scene and replace them with instances of the target prefabs." +
                            "Will attempt to copy component settings from the source to the target (ignoring references to project assets, those are left alone)." +
                            "Prefabs should have identical hierarchy and components for the copy component settings to work.");
        private GUIContent GUI_switchSrcAndTarget = new GUIContent("Switch Source and Target Prefabs", "If this is checked then search the scene for target prefabs and replace them with source prefabs.");
        private GUIContent GUI_enforceSrcAndTargetHaveSameStructure = new GUIContent("Enforce Source And Target Have Same Structure", "If this is checked then prefab instances will only be replaced if they have the same hierarchy and components. \n\n" +
                            "If it is not checked then prefabs can be very different and ONLY THE TRANSFORM, LAYERS, TAGS & STATIC FLAGS ARE COPIED.\n\n" +
                            "WARNING THIS IS A DESTRUCTIVE OPERARTION! BACK UP YOUR SCENE FIRST.");

        [MenuItem("Window/Mesh Baker/Replace Prefabs In Scene")]
        public static void ShowWindow()
        {
            GetWindow<MB_ReplacePrefabsInSceneEditorWindow>(true, "Replace Prefabs In Scene", true).Show();
        }

        public static void ShowWindow(MB3_BatchPrefabBaker.MB3_PrefabBakerRow[] prefabPairs)
        {
            MB_ReplacePrefabsInSceneEditorWindow window = GetWindow<MB_ReplacePrefabsInSceneEditorWindow>(true, "Replace Prefabs In Scene", true);
            window.Show();
            MB_ReplacePrefabsSettings.PrefabPair[] pps = new MB_ReplacePrefabsSettings.PrefabPair[prefabPairs.Length];
            for (int i = 0; i < pps.Length; i++)
            {
                pps[i] = new MB_ReplacePrefabsSettings.PrefabPair()
                {
                    enabled = true,
                    srcPrefab = prefabPairs[i].sourcePrefab,
                    targPrefab = prefabPairs[i].resultPrefab,
                };
            }

            window.model.prefabsToSwitch = pps;
            window.soModel.Update();
        }

        private void InitModel()
        {
            soModel = new SerializedObject(model);
            prefabPairs = soModel.FindProperty("prefabsToSwitch");
            reverseSrcAndTarg = soModel.FindProperty("reverseSrcAndTarg");
            enforceSrcAndTargHaveSameStructure = soModel.FindProperty("enforceSrcAndTargHaveSameStructure");
            infoMessages = "";
            errorMessages = "";
        }

        private void OnEnable()
        {
            minSize = new Vector2(800f, 290f);
            if (model == null)
            {
                model = ScriptableObject.CreateInstance<MB_ReplacePrefabsSettings>();

                // Set the Hide flags so that this windows data not destroyed when entering playmode or a new scene.
                model.hideFlags = HideFlags.DontSave;
            }

            InitModel();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            MB_ReplacePrefabsSettings newModel = (MB_ReplacePrefabsSettings)EditorGUILayout.ObjectField("Settings", model, typeof(MB_ReplacePrefabsSettings), false);
            if (newModel != model)
            {
                if (newModel != null)
                {
                    model = newModel;
                }
                else
                {
                    if (model == null) model = ScriptableObject.CreateInstance<MB_ReplacePrefabsSettings>();

                    // Set the Hide flags so that this windows data not destroyed when entering playmode or a new scene.
                    model.hideFlags = HideFlags.DontSave;
                }

                InitModel();
            }

            if (model != null)
            {
                if (GUILayout.Button("Save Settings", GUILayout.Width(200)))
                {
                    string path = EditorUtility.SaveFilePanel("Save Settings", Application.dataPath, "ReplacePrefabSettings", "asset");
                    if (path != null)
                    {
                        model.hideFlags = HideFlags.None;
                        string relativepath = "Assets" + path.Substring(Application.dataPath.Length);
                        Debug.Log("Saved: " + relativepath);
                        AssetDatabase.CreateAsset(model, relativepath);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            soModel.Update();
            if (GUILayout.Button(GUI_replacePefabsInScene))
            {
                if (EditorUtility.DisplayDialog("Replace Prefabs In Scene",
                        "Are you sure you want to replace all source prefab instances with the target prefab instances in this scene? \n\n" +
                        "It is highly recommended that you back up your scene before doing this.", "OK", "Cancel"))
                {
                    ReplacePrefabsInScene();
                }
            }

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 300f;
            EditorGUILayout.PropertyField(reverseSrcAndTarg, GUI_switchSrcAndTarget);
            EditorGUILayout.PropertyField(enforceSrcAndTargHaveSameStructure, GUI_enforceSrcAndTargetHaveSameStructure);
            EditorGUIUtility.labelWidth = labelWidth;
            if (infoMessages != "" || errorMessages != "")
            {
                EditorGUILayout.HelpBox(errorMessages + "\n" + infoMessages, errorMessages == "" ? MessageType.Info : MessageType.Error);
            }

            scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos);
            EditorGUILayout.PropertyField(prefabPairs, true);
            EditorGUILayout.EndScrollView();
            soModel.ApplyModifiedProperties();
        }

        private void ReplacePrefabsInScene()
        {
            MB_ReplacePrefabsInScene rp = new MB_ReplacePrefabsInScene();
            rp.replaceEnforceStructure = model.enforceSrcAndTargHaveSameStructure;
            int numReplaced = 0;
            int numErrors = 0;
            errorMessages = "";
            infoMessages = "";
            EditorUtility.DisplayProgressBar("Replace Prefabs In Scene", "Replace Prefabs In Scene", 0);
            for (int i = 0; i < model.prefabsToSwitch.Length; i++)
            {
                MB_ReplacePrefabsSettings.PrefabPair pp = model.prefabsToSwitch[i];
                pp.objsWithErrors.Clear();
                if (pp.enabled)
                {
                    GameObject src, targ;
                    if (model.reverseSrcAndTarg)
                    {
                        src = pp.targPrefab;
                        targ = pp.srcPrefab;
                    }
                    else
                    {
                        src = pp.srcPrefab;
                        targ = pp.targPrefab;
                    }

                    numReplaced += rp.ReplacePrefabInstancesInScene(src, targ, pp.objsWithErrors);
                    numErrors += pp.objsWithErrors.Count;
                }

                EditorUtility.DisplayProgressBar("Replace Prefabs In Scene", "Replace In Scene: " + pp.srcPrefab, (float)i / (float)model.prefabsToSwitch.Length);
            }

            EditorUtility.ClearProgressBar();

            Debug.Log("Total prefab instances replaced: " + numReplaced);
            infoMessages = "Total prefab instances replaced: " + numReplaced;
            if (numErrors > 0)
            {
                errorMessages = "There were errors replacing some of the prefabs in the scene. See console for details.";
            }

            soModel.Update();
        }
    }
}
