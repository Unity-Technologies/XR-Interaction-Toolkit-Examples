//----------------------------------------------
//            MeshBaker
// Copyright © 2011-2012 Ian Deane
//----------------------------------------------
using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using DigitalOpus.MB.Core;

using UnityEditor;

namespace DigitalOpus.MB.MBEditor
{

    public interface MB3_MeshBakerEditorWindowInterface
    {
        MonoBehaviour target
        {
            get;
            set;
        }
    }

    public class MB_EditorStyles
    {

        public GUIStyle multipleMaterialBackgroundStyle = new GUIStyle();
        public GUIStyle multipleMaterialBackgroundStyleDarker = new GUIStyle();
        public GUIStyle editorBoxBackgroundStyle = new GUIStyle();

        Texture2D multipleMaterialBackgroundColor;
        Texture2D multipleMaterialBackgroundColorDarker;
        Texture2D editorBoxBackgroundColor;

        public void Init()
        {
            bool isPro = EditorGUIUtility.isProSkin;
            Color backgroundColor = isPro
                ? new Color32(35, 35, 35, 255)
                : new Color32(174, 174, 174, 255);
            if (multipleMaterialBackgroundColor == null)
            {
                multipleMaterialBackgroundColor = MB3_MeshBakerEditorFunctions.MakeTex(8, 8, backgroundColor);
            }

            backgroundColor = isPro
                ? new Color32(50, 50, 50, 255)
                : new Color32(160, 160, 160, 255);
            if (multipleMaterialBackgroundColorDarker == null)
            {
                multipleMaterialBackgroundColorDarker = MB3_MeshBakerEditorFunctions.MakeTex(8, 8, backgroundColor);
            }

            backgroundColor = isPro
                ? new Color32(35, 35, 35, 255)
                : new Color32(174, 174, 174, 255);

            multipleMaterialBackgroundStyle.normal.background = multipleMaterialBackgroundColor;
            multipleMaterialBackgroundStyleDarker.normal.background = multipleMaterialBackgroundColorDarker;

            if (editorBoxBackgroundColor == null)
            {
                editorBoxBackgroundColor = MB3_MeshBakerEditorFunctions.MakeTex(8, 8, backgroundColor);
            }

            editorBoxBackgroundStyle.normal.background = editorBoxBackgroundColor;
            editorBoxBackgroundStyle.border = new RectOffset(0, 0, 0, 0);
            editorBoxBackgroundStyle.margin = new RectOffset(5, 5, 5, 5);
            editorBoxBackgroundStyle.padding = new RectOffset(10, 10, 10, 10);
        }

        public void DestroyTextures()
        {
            if (multipleMaterialBackgroundColor != null) GameObject.DestroyImmediate(multipleMaterialBackgroundColor);
            if (multipleMaterialBackgroundColorDarker != null) GameObject.DestroyImmediate(multipleMaterialBackgroundColorDarker);
            if (editorBoxBackgroundColor != null) GameObject.DestroyImmediate(editorBoxBackgroundColor);
        }
    }

    public class MB3_MeshBakerEditorInternal
    {
        //add option to exclude skinned mesh renderer and mesh renderer in filter
        //example scenes for multi material
        private static GUIContent
            gc_outputOptoinsGUIContent = new GUIContent("Output"),
            gc_logLevelContent = new GUIContent("Log Level"),
            gc_openToolsWindowLabelContent = new GUIContent("Open Tools For Adding Objects", "Use these tools to find out what can be combined, discover problems with meshes, and quickly add objects."),
            gc_parentSceneObject = new GUIContent("Parent Scene Object (Optional)", "Must be a scene object. If set, then combined meshes will be added as children of this GameObject in the hierarchy."),
            gc_resultPrefabLeaveInstanceInSceneAfterBake = new GUIContent("Leave Instance In Scene After Bake", "If checked, then an instance will be left in the scene after baking. Otherwise scene instance will be deleted after prefab is baked."),
            gc_objectsToCombineGUIContent = new GUIContent("Custom List Of Objects To Be Combined", "You can add objects here that were not on the list in the MB3_TextureBaker as long as they use a material that is in the Texture Bake Results"),
            gc_textureBakeResultsGUIContent = new GUIContent("Texture Bake Result", "When materials are combined a MB2_TextureBakeResult Asset is generated. Drag that Asset to this field to use the combined material."),
            gc_useTextureBakerObjsGUIContent = new GUIContent("Same As Texture Baker", "Build a combined mesh using the same list of objects that generated the Combined Material"),
            gc_combinedMeshPrefabGUIContent = new GUIContent("Combined Mesh Prefab", "Create a new prefab asset and drag an empty game object to it. Drag the prefab asset here."),
            gc_SortAlongAxis = new GUIContent("SortAlongAxis", "Transparent materials often require that triangles be rendered in a certain order. This will sort Game Objects along the specified axis. Triangles will be added to the combined mesh in this order."),
            gc_combinedMesh = new GUIContent("Mesh", "This is the Mesh used by this MeshBaker and assigned to the combined Renderer.\n\n" +
                                                     "If it is null, then a new Mesh will be created for the next bake.\n\n" +
                                                     "If it is a project folder asset, then that asset will be overwitten (changes may be reverted if the scene is not saved).\n\n" +
                                                     "If you are re-using the same MeshBaker to bake different combined meshes, set this to null before each new bake. If you don't, then each bake will overwrite the previous bake's mesh."),
            gc_Settings = new GUIContent("Use Shared Settings", "Different MeshBakers can share the same settings. If this field is None, then the settings below will be used. " +
                                                                "Assign one of the following:\n" +
                                                                "   - Mesh Baker Settings project asset \n" +
                                                                "   - Mesh Baker Grouper scene instance \n");



        private SerializedObject meshBaker;
        private SerializedProperty logLevel, combiner, outputOptions, textureBakeResults, useObjsToMeshFromTexBaker, objsToMesh, mesh, sortOrderAxis, parentSceneObject, resultPrefabLeaveInstanceInSceneAfterBake;

        private SerializedProperty settingsHolder;

        private MB_MeshBakerSettingsEditor meshBakerSettingsThis;
        private MB_MeshBakerSettingsEditor meshBakerSettingsExternal;

        bool showInstructions = false;
        bool showContainsReport = true;

        MB_EditorStyles editorStyles = new MB_EditorStyles();

        Color buttonColor = new Color(.8f, .8f, 1f, 1f);
        void _init(SerializedObject mb)
        {
            this.meshBaker = mb;
            objsToMesh = meshBaker.FindProperty("objsToMesh");
            combiner = meshBaker.FindProperty("_meshCombiner");
            parentSceneObject = meshBaker.FindProperty("parentSceneObject");
            resultPrefabLeaveInstanceInSceneAfterBake = meshBaker.FindProperty("resultPrefabLeaveInstanceInSceneAfterBake");
            logLevel = combiner.FindPropertyRelative("_LOG_LEVEL");
            outputOptions = combiner.FindPropertyRelative("_outputOption");
            useObjsToMeshFromTexBaker = meshBaker.FindProperty("useObjsToMeshFromTexBaker");
            textureBakeResults = combiner.FindPropertyRelative("_textureBakeResults");
            mesh = combiner.FindPropertyRelative("_mesh");
            sortOrderAxis = meshBaker.FindProperty("sortAxis");
            settingsHolder = combiner.FindPropertyRelative("_settingsHolder");
            meshBakerSettingsThis = new MB_MeshBakerSettingsEditor();
            meshBakerSettingsThis.OnEnable(combiner, meshBaker);
            editorStyles.Init();
        }

        public void OnEnable(SerializedObject meshBaker)
        {
            _init(meshBaker);
        }

        public void OnDisable()
        {
            editorStyles.DestroyTextures();
            if (meshBakerSettingsThis != null) meshBakerSettingsThis.OnDisable();
            if (meshBakerSettingsExternal != null) meshBakerSettingsExternal.OnDisable();
        }

        public void OnInspectorGUI(SerializedObject meshBaker, MB3_MeshBakerCommon target, UnityEngine.Object[] targets, System.Type editorWindowType)
        {
            DrawGUI(meshBaker, target, targets, editorWindowType);
        }

        public void DrawGUI(SerializedObject meshBaker, MB3_MeshBakerCommon target, UnityEngine.Object[] targets, System.Type editorWindowType)
        {
            if (meshBaker == null)
            {
                return;
            }

            meshBaker.Update();

            showInstructions = EditorGUILayout.Foldout(showInstructions, "Instructions:");
            if (showInstructions)
            {
                EditorGUILayout.HelpBox("1. Bake combined material(s).\n\n" +
                                        "2. If necessary set the 'Texture Bake Results' field.\n\n" +
                                        "3. Add scene objects or prefabs to combine or check 'Same As Texture Baker'. For best results these should use the same shader as result material.\n\n" +
                                        "4. Select options and 'Bake'.\n\n" +
                                        "6. Look at warnings/errors in console. Decide if action needs to be taken.\n\n" +
                                        "7. (optional) Disable renderers in source objects.", UnityEditor.MessageType.None);

                EditorGUILayout.Separator();
            }

            MB3_MeshBakerCommon momm = (MB3_MeshBakerCommon)target;
            EditorGUILayout.PropertyField(logLevel, gc_logLevelContent);
            EditorGUILayout.PropertyField(textureBakeResults, gc_textureBakeResultsGUIContent);
            bool doingTextureArray = false;
            if (textureBakeResults.objectReferenceValue != null)
            {
                doingTextureArray = ((MB2_TextureBakeResults)textureBakeResults.objectReferenceValue).resultType == MB2_TextureBakeResults.ResultType.textureArray;
                showContainsReport = EditorGUILayout.Foldout(showContainsReport, "Shaders & Materials Contained");
                if (showContainsReport)
                {
                    EditorGUILayout.HelpBox(((MB2_TextureBakeResults)textureBakeResults.objectReferenceValue).GetDescription(), MessageType.Info);
                }
            }

            EditorGUILayout.BeginVertical(editorStyles.editorBoxBackgroundStyle);
            EditorGUILayout.LabelField("Objects To Be Combined", EditorStyles.boldLabel);
            if (momm.GetTextureBaker() != null)
            {
                EditorGUILayout.PropertyField(useObjsToMeshFromTexBaker, gc_useTextureBakerObjsGUIContent);
            }
            else
            {
                useObjsToMeshFromTexBaker.boolValue = false;
                momm.useObjsToMeshFromTexBaker = false;
                GUI.enabled = false;
                EditorGUILayout.PropertyField(useObjsToMeshFromTexBaker, gc_useTextureBakerObjsGUIContent);
                GUI.enabled = true;
            }

            if (!momm.useObjsToMeshFromTexBaker)
            {
                if (GUILayout.Button(gc_openToolsWindowLabelContent))
                {
                    MB3_MeshBakerEditorWindow mmWin = (MB3_MeshBakerEditorWindow) EditorWindow.GetWindow(editorWindowType);
                    mmWin.SetTarget((MB3_MeshBakerRoot)momm);
                }

                object[] objs = MB3_EditorMethods.DropZone("Drag & Drop Renderers or Parents\n" + "HERE\n" +
                "to add objects to be combined", 300, 50);
                MB3_EditorMethods.AddDroppedObjects(objs, momm);

                EditorGUILayout.PropertyField(objsToMesh, gc_objectsToCombineGUIContent, true);
                EditorGUILayout.Separator();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select Objects In Scene"))
                {
                    List<MB3_MeshBakerCommon> selectedBakers = _getBakersFromTargets(targets);
                    List<GameObject> obsToCombine = new List<GameObject>();

                    foreach(MB3_MeshBakerCommon baker in selectedBakers) obsToCombine.AddRange(baker.GetObjectsToCombine());
                    Selection.objects = obsToCombine.ToArray();
                    if (momm.GetObjectsToCombine().Count > 0)
                    {
                        SceneView.lastActiveSceneView.pivot = momm.GetObjectsToCombine()[0].transform.position;
                    }
                }
                if (GUILayout.Button(gc_SortAlongAxis))
                {
                    MB3_MeshBakerRoot.ZSortObjects sorter = new MB3_MeshBakerRoot.ZSortObjects();
                    sorter.sortAxis = sortOrderAxis.vector3Value;
                    sorter.SortByDistanceAlongAxis(momm.GetObjectsToCombine());
                }
                EditorGUILayout.PropertyField(sortOrderAxis, GUIContent.none);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.PropertyField(objsToMesh, gc_objectsToCombineGUIContent, true);
                GUI.enabled = true;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(outputOptions, gc_outputOptoinsGUIContent);
            if (momm.meshCombiner.outputOption == MB2_OutputOptions.bakeIntoSceneObject)
            {
                Transform pgo = (Transform)EditorGUILayout.ObjectField(gc_parentSceneObject, parentSceneObject.objectReferenceValue, typeof(Transform), true);
                if (pgo != null && MB_Utility.IsSceneInstance(pgo.gameObject))
                {
                    parentSceneObject.objectReferenceValue = pgo;
                }
                else
                {
                    parentSceneObject.objectReferenceValue = null;
                }

                //todo switch to renderer
                momm.meshCombiner.resultSceneObject = (GameObject)EditorGUILayout.ObjectField("Combined Mesh Object", momm.meshCombiner.resultSceneObject, typeof(GameObject), true);
                if (momm is MB3_MeshBaker)
                {
                    string l = "Mesh";
                    Mesh m = (Mesh)mesh.objectReferenceValue;
                    if (m != null)
                    {
                        l += " (" + m.GetInstanceID() + ")";
                    }
                    Mesh nm = (Mesh)EditorGUILayout.ObjectField(gc_combinedMesh, m, typeof(Mesh), true);
                    if (nm != m)
                    {
                        Undo.RecordObject(momm, "Assign Mesh");
                        ((MB3_MeshCombinerSingle)momm.meshCombiner).SetMesh(nm);
                        mesh.objectReferenceValue = nm;
                    }
                }
            }
            else if (momm.meshCombiner.outputOption == MB2_OutputOptions.bakeIntoPrefab)
            {
                if (momm.meshCombiner.settings.renderType == MB_RenderType.skinnedMeshRenderer)
                {
                    EditorGUILayout.HelpBox("The workflow for baking Skinned Meshes into prefabs has changed as of version 29.1. " +
                        "It is no longer necessary to manually copy bones to the target prefab after baking. This should happen automatically.", MessageType.Info);
                }

                Transform pgo = (Transform)EditorGUILayout.ObjectField(gc_parentSceneObject, parentSceneObject.objectReferenceValue, typeof(Transform), true);
                if (pgo != null && MB_Utility.IsSceneInstance(pgo.gameObject)) 
                {
                    parentSceneObject.objectReferenceValue = pgo;
                }
                else
                {
                    parentSceneObject.objectReferenceValue = null;
                }

                EditorGUILayout.BeginHorizontal();
                momm.resultPrefab = (GameObject)EditorGUILayout.ObjectField(gc_combinedMeshPrefabGUIContent, momm.resultPrefab, typeof(GameObject), true);
                if (momm.resultPrefab != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(momm.resultPrefab);
                    if (assetPath == null || assetPath.Length == 0)
                    {
                        Debug.LogError("The " + gc_combinedMeshPrefabGUIContent.text + " must be a prefab asset, not a scene GameObject");
                        momm.resultPrefab = null;
                    } else
                    {
                        MB_PrefabType pt = MBVersionEditor.PrefabUtility_GetPrefabType(momm.resultPrefab);
                        if (pt != MB_PrefabType.prefabAsset)
                        {
                            Debug.LogError("The " + gc_combinedMeshPrefabGUIContent.text + " must be a prefab asset, the prefab type was '" + pt + "'");
                            momm.resultPrefab = null;
                        }
                    }
                } else
                {
                    if (GUILayout.Button("Create Empty Prefab"))
                    {
                        if (!Application.isPlaying)
                        {
                            string path = EditorUtility.SaveFilePanelInProject("Create Empty Prefab", "MyPrefab", "prefab", "Create a prefab containing an empty GameObject");
                            string pathNoFolder = Path.GetDirectoryName(path);
                            string fileNameNoExt = Path.GetFileNameWithoutExtension(path);
                            List<MB3_MeshBakerCommon> selectedBakers = _getBakersFromTargets(targets);
                            if (selectedBakers.Count > 1) Debug.Log("About to create prefabs for " + selectedBakers.Count);
                            int idx = 0;
                            foreach (MB3_MeshBakerCommon baker in selectedBakers)
                            {
                                createEmptyPrefab(baker, pathNoFolder, fileNameNoExt, idx);
                                idx++;
                            }
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(resultPrefabLeaveInstanceInSceneAfterBake, gc_resultPrefabLeaveInstanceInSceneAfterBake);
                if (momm is MB3_MeshBaker)
                {
                    string l = "Mesh";
                    Mesh m = (Mesh)mesh.objectReferenceValue;
                    if (m != null)
                    {
                        l += " (" + m.GetInstanceID() + ")";
                    }
                    Mesh nm = (Mesh)EditorGUILayout.ObjectField(gc_combinedMesh, m, typeof(Mesh), true);
                    if (nm != m)
                    {
                        Undo.RecordObject(momm, "Assign Mesh");
                        ((MB3_MeshCombinerSingle)momm.meshCombiner).SetMesh(nm);
                        mesh.objectReferenceValue = nm;
                    }
                }
            }
            else if (momm.meshCombiner.outputOption == MB2_OutputOptions.bakeMeshAssetsInPlace)
            {
                EditorGUILayout.HelpBox("Try the BatchPrefabBaker component! It makes preparing a batch of prefabs for static/ dynamic batching much easier.", MessageType.Info);
                if (GUILayout.Button("Choose Folder For Bake In Place Meshes"))
                {
                    string newFolder = EditorUtility.SaveFolderPanel("Folder For Bake In Place Meshes", Application.dataPath, "");
                    if (!newFolder.Contains(Application.dataPath)) Debug.LogWarning("The chosen folder must be in your assets folder.");
                    string folder = "Assets" + newFolder.Replace(Application.dataPath, "");
                    List<MB3_MeshBakerCommon> selectedBakers = _getBakersFromTargets(targets);
                    Undo.RecordObjects(targets, "Undo Set Folder");
                    foreach (MB3_MeshBakerCommon baker in selectedBakers)
                    {
                        baker.bakeAssetsInPlaceFolderPath = folder;
                        EditorUtility.SetDirty(baker);
                    }
                }

                EditorGUILayout.LabelField("Folder For Meshes: " + momm.bakeAssetsInPlaceFolderPath);
            }

            if (momm is MB3_MultiMeshBaker)
            {
                MB3_MultiMeshCombiner mmc = (MB3_MultiMeshCombiner)momm.meshCombiner;
                mmc.maxVertsInMesh = EditorGUILayout.IntField("Max Verts In Mesh", mmc.maxVertsInMesh);
            }

            //-----------------------------------
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            bool settingsEnabled = true;

            //------------- Mesh Baker Settings is a bit tricky because it is an interface.



            EditorGUILayout.Space();
            UnityEngine.Object obj = settingsHolder.objectReferenceValue;

            // Don't use a PropertyField because we may not be able to use the assigned object. It may not implement requried interface.
            obj = EditorGUILayout.ObjectField(gc_Settings, obj, typeof(UnityEngine.Object), true);

            if (obj == null)
            {
                settingsEnabled = true;
                settingsHolder.objectReferenceValue = null;
                if (meshBakerSettingsExternal != null)
                {
                    meshBakerSettingsExternal.OnDisable();
                    meshBakerSettingsExternal = null;
                }
            }

            else if (obj is GameObject)
            {
                // Check to see if there is a component on this game object that implements MB_IMeshBakerSettingsHolder
                MB_IMeshBakerSettingsHolder itf = (MB_IMeshBakerSettingsHolder)((GameObject)obj).GetComponent(typeof(MB_IMeshBakerSettingsHolder));
                if (itf != null)
                {
                    settingsEnabled = false;
                    Component settingsHolderComponent = (Component)itf;
                    if (settingsHolder.objectReferenceValue != settingsHolderComponent)
                    {
                        settingsHolder.objectReferenceValue = settingsHolderComponent;
                        meshBakerSettingsExternal = new MB_MeshBakerSettingsEditor();
                        UnityEngine.Object targetObj;
                        string propertyName;
                        itf.GetMeshBakerSettingsAsSerializedProperty(out propertyName, out targetObj);
                        SerializedProperty meshBakerSettings = new SerializedObject(targetObj).FindProperty(propertyName);
                        meshBakerSettingsExternal.OnEnable(meshBakerSettings);
                    }
                }
                else
                {
                    Debug.LogError("You must drag a MeshBakerGrouper or a MeshCombinerSetting asset to this field.");
                    settingsEnabled = true;
                    settingsHolder.objectReferenceValue = null;
                    if (meshBakerSettingsExternal != null)
                    {
                        meshBakerSettingsExternal.OnDisable();
                        meshBakerSettingsExternal = null;
                    }
                }
            }
            else if (obj is MB_IMeshBakerSettingsHolder)
            {
                settingsEnabled = false;
                if (settingsHolder.objectReferenceValue != obj)
                {
                    settingsHolder.objectReferenceValue = obj;
                    meshBakerSettingsExternal = new MB_MeshBakerSettingsEditor();
                    UnityEngine.Object targetObj;
                    string propertyName;
                    ((MB_IMeshBakerSettingsHolder)obj).GetMeshBakerSettingsAsSerializedProperty(out propertyName, out targetObj);
                    SerializedProperty meshBakerSettings = new SerializedObject(targetObj).FindProperty(propertyName);
                    meshBakerSettingsExternal.OnEnable(meshBakerSettings);
                }
            }
            else
            {
                Debug.LogError("You must drag a MeshBakerGrouper or a MeshCombinerSetting asset to this field.");
            }
            EditorGUILayout.Space();

            if (settingsHolder.objectReferenceValue == null)
            {
                // Use the meshCombiner settings
                meshBakerSettingsThis.DrawGUI(momm.meshCombiner, settingsEnabled, doingTextureArray);
            }
            else
            {
                if (meshBakerSettingsExternal == null)
                {
                    meshBakerSettingsExternal = new MB_MeshBakerSettingsEditor();
                    UnityEngine.Object targetObj;
                    string propertyName;
                    ((MB_IMeshBakerSettingsHolder)obj).GetMeshBakerSettingsAsSerializedProperty(out propertyName, out targetObj);
                    SerializedProperty meshBakerSettings = new SerializedObject(targetObj).FindProperty(propertyName);
                    meshBakerSettingsExternal.OnEnable(meshBakerSettings);
                }
                meshBakerSettingsExternal.DrawGUI(((MB_IMeshBakerSettingsHolder)settingsHolder.objectReferenceValue).GetMeshBakerSettings(), settingsEnabled, doingTextureArray);
            }

            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button("Bake"))
            {
                List<MB3_MeshBakerCommon> selectedBakers = _getBakersFromTargets(targets);
                if (selectedBakers.Count > 1) Debug.Log("About to bake " + selectedBakers.Count);
                foreach(MB3_MeshBakerCommon baker in selectedBakers)
                {
                    // Why are we caching and recreating the SerializedObject? Because "bakeIntoPrefab" corrupts the serialized object
                    // and the meshBaker SerializedObject throws an NRE the next time it gets used.
                    MB3_MeshBakerCommon mbr  = (MB3_MeshBakerCommon) meshBaker.targetObject;
                    bake(baker);
                    meshBaker = new SerializedObject(mbr);
                }
            }
            GUI.backgroundColor = oldColor;

            string enableRenderersLabel;
            bool disableRendererInSource = false;
            if (momm.GetObjectsToCombine().Count > 0)
            {
                Renderer r = MB_Utility.GetRenderer(momm.GetObjectsToCombine()[0]);
                if (r != null && r.enabled) disableRendererInSource = true;
            }
            if (disableRendererInSource)
            {
                enableRenderersLabel = "Disable Renderers On Source Objects";
            }
            else
            {
                enableRenderersLabel = "Enable Renderers On Source Objects";
            }
            if (GUILayout.Button(enableRenderersLabel))
            {
                List<MB3_MeshBakerCommon> selectedBakers = _getBakersFromTargets(targets);
                foreach (MB3_MeshBakerCommon baker in selectedBakers)
                {
                    baker.EnableDisableSourceObjectRenderers(!disableRendererInSource);
                }
            }

            meshBaker.ApplyModifiedProperties();
            meshBaker.SetIsDifferentCacheDirty();
        }

        public static void updateProgressBar(string msg, float progress)
        {
            EditorUtility.DisplayProgressBar("Combining Meshes", msg, progress);
        }

        public static bool bake(MB3_MeshBakerCommon mom)
        {
            SerializedObject so = null;
            return bake(mom, ref so);
        }

        private List<MB3_MeshBakerCommon> _getBakersFromTargets(UnityEngine.Object[] targs)
        {
            List<MB3_MeshBakerCommon> outList = new List<MB3_MeshBakerCommon>(targs.Length);
            for (int i = 0; i < targs.Length; i++)
            {
                outList.Add((MB3_MeshBakerCommon) targs[i]);
            }

            return outList;
        }

        private static void createEmptyPrefab(MB3_MeshBakerCommon mom, string folder, string prefabNameNoExtension, int idx)
        {
            if (prefabNameNoExtension != null && prefabNameNoExtension.Length > 0)
            {
                string prefabName = prefabNameNoExtension + idx;
                GameObject go = new GameObject(prefabName);
                string fullName = folder + "/" + prefabName + ".prefab";
                fullName = AssetDatabase.GenerateUniqueAssetPath(fullName);
                Debug.Log(fullName);
                MBVersionEditor.PrefabUtility_CreatePrefab(fullName, go);
                GameObject.DestroyImmediate(go);
                SerializedObject so = new SerializedObject(mom);
                so.FindProperty("resultPrefab").objectReferenceValue =  (GameObject)AssetDatabase.LoadAssetAtPath(fullName, typeof(GameObject));
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Bakes a combined mesh.
        /// </summary>
        /// <param name="mom"></param>
        /// <param name="so">This is needed to work around a Unity bug where UnpackPrefabInstance corrupts 
        /// a SerializedObject. Only needed for bake into prefab.</param>
        public static bool bake(MB3_MeshBakerCommon mom, ref SerializedObject so)
        {
            bool createdDummyTextureBakeResults = false;
            bool success = false;
            try
            {
                if (mom.meshCombiner.outputOption == MB2_OutputOptions.bakeIntoSceneObject ||
                    mom.meshCombiner.outputOption == MB2_OutputOptions.bakeIntoPrefab)
                {
                    success = MB3_MeshBakerEditorFunctions.BakeIntoCombined(mom, out createdDummyTextureBakeResults, ref so);
                }
                else
                {
                    //bake meshes in place
                    if (mom is MB3_MeshBaker)
                    {
                        if (MB3_MeshCombiner.EVAL_VERSION)
                        {
                            Debug.LogError("Bake Mesh Assets In Place is disabled in the evaluation version.");
                        }
                        else
                        {
                            MB2_ValidationLevel vl = Application.isPlaying ? MB2_ValidationLevel.quick : MB2_ValidationLevel.robust;
                            if (!MB3_MeshBakerRoot.DoCombinedValidate(mom, MB_ObjsToCombineTypes.prefabOnly, new MB3_EditorMethods(), vl)) return false;

                            List<GameObject> objsToMesh = mom.GetObjectsToCombine();
                            success = MB3_BakeInPlace.BakeMeshesInPlace((MB3_MeshCombinerSingle)((MB3_MeshBaker)mom).meshCombiner, objsToMesh, mom.bakeAssetsInPlaceFolderPath, mom.clearBuffersAfterBake, updateProgressBar);
                        }
                    }
                    else
                    {
                        Debug.LogError("Multi Mesh Baker components cannot be used for Bake Mesh Assets In Place. Use an ordinary MeshBaker object instead.");
                    }
                }
                mom.meshCombiner.CheckIntegrity();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message + "\n" + ex.StackTrace.ToString());
            }
            finally
            {
                if (createdDummyTextureBakeResults && mom.textureBakeResults != null)
                {
                    MB_Utility.Destroy(mom.textureBakeResults);
                    mom.textureBakeResults = null;
                }
                EditorUtility.ClearProgressBar();
            }
            return success;
        }
    }
}
