//            MeshBaker
// Copyright © 2011-2012 Ian Deane
//---------------------------------------------- 
using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DigitalOpus.MB.Core;

using UnityEditor;

namespace DigitalOpus.MB.MBEditor
{

    public class MB_TextureBakerEditorConfigureMultiMaterials
    {


        public static void DrawMultipleMaterialsMappings(MB3_TextureBaker momm, SerializedObject textureBaker, MB3_TextureBakerEditorInternal tbEditor)
        {
            EditorGUILayout.BeginVertical(tbEditor.editorStyles.multipleMaterialBackgroundStyle);
            EditorGUILayout.LabelField("Source Material To Combined Mapping", EditorStyles.boldLabel);

            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 300;
            EditorGUILayout.PropertyField(tbEditor.doMultiMaterialIfOBUVs, MB3_TextureBakerEditorInternal.gc_DoMultiMaterialSplitAtlasesIfOBUVs);
            EditorGUILayout.PropertyField(tbEditor.doMultiMaterialSplitAtlasesIfTooBig, MB3_TextureBakerEditorInternal.gc_DoMultiMaterialSplitAtlasesIfTooBig);
            EditorGUIUtility.labelWidth = oldLabelWidth;


            if (GUILayout.Button(MB3_TextureBakerEditorInternal.configAtlasMultiMatsFromObjsContent))
            {
                MB_TextureBakerEditorConfigureMultiMaterials.ConfigureMutiMaterialsFromObjsToCombine(momm, tbEditor.resultMaterials, textureBaker);
            }

            EditorGUILayout.BeginHorizontal();
            tbEditor.resultMaterialsFoldout = EditorGUILayout.Foldout(tbEditor.resultMaterialsFoldout, MB3_TextureBakerEditorInternal.combinedMaterialsGUIContent);

            if (GUILayout.Button(MB3_TextureBakerEditorInternal.insertContent, EditorStyles.miniButtonLeft, MB3_TextureBakerEditorInternal.buttonWidth))
            {
                if (tbEditor.resultMaterials.arraySize == 0)
                {
                    momm.resultMaterials = new MB_MultiMaterial[1];
                    momm.resultMaterials[0] = new MB_MultiMaterial();
                    momm.resultMaterials[0].considerMeshUVs = momm.fixOutOfBoundsUVs;
                }
                else
                {
                    int idx = tbEditor.resultMaterials.arraySize - 1;
                    tbEditor.resultMaterials.InsertArrayElementAtIndex(idx);
                    tbEditor.resultMaterials.GetArrayElementAtIndex(idx + 1).FindPropertyRelative("considerMeshUVs").boolValue = momm.fixOutOfBoundsUVs;
                }
            }
            if (GUILayout.Button(MB3_TextureBakerEditorInternal.deleteContent, EditorStyles.miniButtonRight, MB3_TextureBakerEditorInternal.buttonWidth))
            {
                tbEditor.resultMaterials.DeleteArrayElementAtIndex(tbEditor.resultMaterials.arraySize - 1);
            }
            EditorGUILayout.EndHorizontal();
            if (tbEditor.resultMaterialsFoldout)
            {
                for (int i = 0; i < tbEditor.resultMaterials.arraySize; i++)
                {
                    EditorGUILayout.Separator();
                    if (i % 2 == 1)
                    {
                        EditorGUILayout.BeginVertical(tbEditor.editorStyles.multipleMaterialBackgroundStyle);
                    }
                    else
                    {
                        EditorGUILayout.BeginVertical(tbEditor.editorStyles.multipleMaterialBackgroundStyleDarker);
                    }
                    string s = "";
                    if (i < momm.resultMaterials.Length && momm.resultMaterials[i] != null && momm.resultMaterials[i].combinedMaterial != null) s = momm.resultMaterials[i].combinedMaterial.shader.ToString();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("---------- submesh:" + i + " " + s, EditorStyles.boldLabel);
                    if (GUILayout.Button(MB3_TextureBakerEditorInternal.deleteContent, EditorStyles.miniButtonRight, MB3_TextureBakerEditorInternal.buttonWidth))
                    {
                        tbEditor.resultMaterials.DeleteArrayElementAtIndex(i);
                    }
                    EditorGUILayout.EndHorizontal();
                    if (i < tbEditor.resultMaterials.arraySize)
                    {
                        EditorGUILayout.Separator();
                        SerializedProperty resMat = tbEditor.resultMaterials.GetArrayElementAtIndex(i);
                        EditorGUILayout.PropertyField(resMat.FindPropertyRelative("combinedMaterial"));
                        EditorGUILayout.PropertyField(resMat.FindPropertyRelative("considerMeshUVs"));
                        SerializedProperty sourceMats = resMat.FindPropertyRelative("sourceMaterials");
                        EditorGUILayout.PropertyField(sourceMats, true);
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndVertical();
        }


        /* tried to see if the MultiMaterialConfig could be done using the GroupBy filters. Saddly it didn't work */
        public static void ConfigureMutiMaterialsFromObjsToCombine2(MB3_TextureBaker mom, SerializedProperty resultMaterials, SerializedObject textureBaker)
        {
            if (mom.GetObjectsToCombine().Count == 0)
            {
                Debug.LogError("You need to add some objects to combine before building the multi material list.");
                return;
            }
            if (resultMaterials.arraySize > 0)
            {
                Debug.LogError("You already have some source to combined material mappings configured. You must remove these before doing this operation.");
                return;
            }
            if (mom.textureBakeResults == null)
            {
                Debug.LogError("Texture Bake Result asset must be set before using this operation.");
                return;
            }

            //validate that the objects to be combined are valid
            for (int i = 0; i < mom.GetObjectsToCombine().Count; i++)
            {
                GameObject go = mom.GetObjectsToCombine()[i];
                if (go == null)
                {
                    Debug.LogError("Null object in list of objects to combine at position " + i);
                    return;
                }
                Renderer r = go.GetComponent<Renderer>();
                if (r == null || (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)))
                {
                    Debug.LogError("GameObject at position " + i + " in list of objects to combine did not have a renderer");
                    return;
                }
                if (r.sharedMaterial == null)
                {
                    Debug.LogError("GameObject at position " + i + " in list of objects to combine has a null material");
                    return;
                }
            }

            IGroupByFilter[] filters = new IGroupByFilter[3];
            filters[0] = new GroupByOutOfBoundsUVs();
            filters[1] = new GroupByShader();
            filters[2] = new MB3_GroupByStandardShaderType();

            List<GameObjectFilterInfo> gameObjects = new List<GameObjectFilterInfo>();
            HashSet<GameObject> objectsAlreadyIncludedInBakers = new HashSet<GameObject>();
            for (int i = 0; i < mom.GetObjectsToCombine().Count; i++)
            {
                GameObjectFilterInfo goaw = new GameObjectFilterInfo(mom.GetObjectsToCombine()[i], objectsAlreadyIncludedInBakers, filters);
                if (goaw.materials.Length > 0) //don't consider renderers with no materials
                {
                    gameObjects.Add(goaw);
                }
            }

            //analyse meshes
            Dictionary<int, MB_Utility.MeshAnalysisResult> meshAnalysisResultCache = new Dictionary<int, MB_Utility.MeshAnalysisResult>();
            int totalVerts = 0;
            for (int i = 0; i < gameObjects.Count; i++)
            {
                //string rpt = String.Format("Processing {0} [{1} of {2}]", gameObjects[i].go.name, i, gameObjects.Count);
                //EditorUtility.DisplayProgressBar("Analysing Scene", rpt + " A", .6f);
                Mesh mm = MB_Utility.GetMesh(gameObjects[i].go);
                int nVerts = 0;
                if (mm != null)
                {
                    nVerts += mm.vertexCount;
                    MB_Utility.MeshAnalysisResult mar;
                    if (!meshAnalysisResultCache.TryGetValue(mm.GetInstanceID(), out mar))
                    {

                        //EditorUtility.DisplayProgressBar("Analysing Scene", rpt + " Check Out Of Bounds UVs", .6f);
                        MB_Utility.hasOutOfBoundsUVs(mm, ref mar);
                        //Rect dummy = mar.uvRect;
                        MB_Utility.doSubmeshesShareVertsOrTris(mm, ref mar);
                        meshAnalysisResultCache.Add(mm.GetInstanceID(), mar);
                    }
                    if (mar.hasOutOfBoundsUVs)
                    {
                        int w = (int)mar.uvRect.width;
                        int h = (int)mar.uvRect.height;
                        gameObjects[i].outOfBoundsUVs = true;
                        gameObjects[i].warning += " [WARNING: has uvs outside the range (0,1) tex is tiled " + w + "x" + h + " times]";
                    }
                    if (mar.hasOverlappingSubmeshVerts)
                    {
                        gameObjects[i].submeshesOverlap = true;
                        gameObjects[i].warning += " [WARNING: Submeshes share verts or triangles. 'Multiple Combined Materials' feature may not work.]";
                    }
                }
                totalVerts += nVerts;
                //EditorUtility.DisplayProgressBar("Analysing Scene", rpt + " Validate OBuvs Multi Material", .6f);
                Renderer mr = gameObjects[i].go.GetComponent<Renderer>();
                if (!MB_Utility.AreAllSharedMaterialsDistinct(mr.sharedMaterials))
                {
                    gameObjects[i].warning += " [WARNING: Object uses same material on multiple submeshes. This may produce poor results when used with multiple materials or fix out of bounds uvs.]";
                }
            }

            List<GameObjectFilterInfo> objsNotAddedToBaker = new List<GameObjectFilterInfo>();

            Dictionary<GameObjectFilterInfo, List<List<GameObjectFilterInfo>>> gs2bakeGroupMap = MB3_MeshBakerEditorWindowAnalyseSceneTab.sortIntoBakeGroups3(gameObjects, objsNotAddedToBaker, filters, false, mom.maxAtlasSize);

            mom.resultMaterials = new MB_MultiMaterial[gs2bakeGroupMap.Keys.Count];
            string pth = AssetDatabase.GetAssetPath(mom.textureBakeResults);
            string baseName = Path.GetFileNameWithoutExtension(pth);
            string folderPath = pth.Substring(0, pth.Length - baseName.Length - 6);
            int k = 0;
            foreach (GameObjectFilterInfo m in gs2bakeGroupMap.Keys)
            {
                MB_MultiMaterial mm = mom.resultMaterials[k] = new MB_MultiMaterial();
                mm.sourceMaterials = new List<Material>();
                mm.sourceMaterials.Add(m.materials[0]);
                string matName = folderPath + baseName + "-mat" + k + ".mat";
                Material newMat = new Material(Shader.Find("Diffuse"));
                MB3_TextureBaker.ConfigureNewMaterialToMatchOld(newMat, m.materials[0]);
                AssetDatabase.CreateAsset(newMat, matName);
                mm.combinedMaterial = (Material)AssetDatabase.LoadAssetAtPath(matName, typeof(Material));
                k++;
            }
            MBVersionEditor.UpdateIfDirtyOrScript(textureBaker);
        }


        //posibilities
        //  using fixOutOfBoundsUVs or not 
        //  
        public static void ConfigureMutiMaterialsFromObjsToCombine(MB3_TextureBaker mom, SerializedProperty resultMaterials, SerializedObject textureBaker)
        {
            if (mom.GetObjectsToCombine().Count == 0)
            {
                Debug.LogError("You need to add some objects to combine before building the multi material list.");
                return;
            }
            if (resultMaterials.arraySize > 0)
            {
                Debug.LogError("You already have some source to combined material mappings configured. You must remove these before doing this operation.");
                return;
            }
            if (mom.textureBakeResults == null)
            {
                Debug.LogError("Texture Bake Result asset must be set before using this operation.");
                return;
            }

            Dictionary<MB3_TextureBakerEditorInternal.MultiMatSubmeshInfo, List<List<Material>>> shader2Material_map = new Dictionary<MB3_TextureBakerEditorInternal.MultiMatSubmeshInfo, List<List<Material>>>();
            Dictionary<Material, Mesh> obUVobject2mesh_map = new Dictionary<Material, Mesh>();

            //validate that the objects to be combined are valid
            for (int i = 0; i < mom.GetObjectsToCombine().Count; i++)
            {
                GameObject go = mom.GetObjectsToCombine()[i];
                if (go == null)
                {
                    Debug.LogError("Null object in list of objects to combine at position " + i);
                    return;
                }
                Renderer r = go.GetComponent<Renderer>();
                if (r == null || (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)))
                {
                    Debug.LogError("GameObject at position " + i + " in list of objects to combine did not have a renderer");
                    return;
                }
                if (r.sharedMaterial == null)
                {
                    Debug.LogError("GameObject at position " + i + " in list of objects to combine has a null material");
                    return;
                }
            }

            //first pass put any meshes with obUVs on their own submesh if not fixing OB uvs
            if (mom.doMultiMaterialSplitAtlasesIfOBUVs)
            {
                for (int i = 0; i < mom.GetObjectsToCombine().Count; i++)
                {
                    GameObject go = mom.GetObjectsToCombine()[i];
                    Mesh m = MB_Utility.GetMesh(go);
                    MB_Utility.MeshAnalysisResult dummyMar = new MB_Utility.MeshAnalysisResult();
                    Renderer r = go.GetComponent<Renderer>();
                    for (int j = 0; j < r.sharedMaterials.Length; j++)
                    {
                        if (MB_Utility.hasOutOfBoundsUVs(m, ref dummyMar, j))
                        {
                            if (!obUVobject2mesh_map.ContainsKey(r.sharedMaterials[j]))
                            {
                                Debug.LogWarning("Object " + go + " submesh " + j + " uses UVs outside the range 0,0..1,1 to generate tiling. This object has been mapped to its own submesh in the combined mesh. It can share a submesh with other objects that use different materials if you use the fix out of bounds UVs feature which will bake the tiling");
                                obUVobject2mesh_map.Add(r.sharedMaterials[j], m);
                            }
                        }
                    }
                }
            }

            //second pass  put other materials without OB uvs in a shader to material map
            for (int i = 0; i < mom.GetObjectsToCombine().Count; i++)
            {
                Renderer r = mom.GetObjectsToCombine()[i].GetComponent<Renderer>();
                for (int j = 0; j < r.sharedMaterials.Length; j++)
                {
                    if (!obUVobject2mesh_map.ContainsKey(r.sharedMaterials[j]))
                    { //if not already added
                        if (r.sharedMaterials[j] == null) continue;
                        List<List<Material>> binsOfMatsThatUseShader = null;
                        MB3_TextureBakerEditorInternal.MultiMatSubmeshInfo newKey = new MB3_TextureBakerEditorInternal.MultiMatSubmeshInfo(r.sharedMaterials[j].shader, r.sharedMaterials[j]);
                        if (!shader2Material_map.TryGetValue(newKey, out binsOfMatsThatUseShader))
                        {
                            binsOfMatsThatUseShader = new List<List<Material>>();
                            binsOfMatsThatUseShader.Add(new List<Material>());
                            shader2Material_map.Add(newKey, binsOfMatsThatUseShader);
                        }
                        if (!binsOfMatsThatUseShader[0].Contains(r.sharedMaterials[j])) binsOfMatsThatUseShader[0].Add(r.sharedMaterials[j]);
                    }
                }
            }

            int numResMats = shader2Material_map.Count;
            //third pass for each shader grouping check how big the atlas would be and group into bins that would fit in an atlas
            if (mom.doMultiMaterialSplitAtlasesIfTooBig)
            {
                if (mom.packingAlgorithm == MB2_PackingAlgorithmEnum.UnitysPackTextures)
                {
                    Debug.LogWarning("Unity texture packer does not support splitting atlases if too big. Atlases will not be split.");
                }
                else
                {
                    numResMats = 0;
                    foreach (MB3_TextureBakerEditorInternal.MultiMatSubmeshInfo sh in shader2Material_map.Keys)
                    {
                        List<List<Material>> binsOfMatsThatUseShader = shader2Material_map[sh];
                        List<Material> allMatsThatUserShader = binsOfMatsThatUseShader[0];//at this point everything is in the same list
                        binsOfMatsThatUseShader.RemoveAt(0);
                        MB3_TextureCombiner combiner = mom.CreateAndConfigureTextureCombiner();
                        combiner.saveAtlasesAsAssets = false;
                        if (allMatsThatUserShader.Count > 1) combiner.fixOutOfBoundsUVs = mom.fixOutOfBoundsUVs;
                        else combiner.fixOutOfBoundsUVs = false;

                        // Do the texture pack
                        List<AtlasPackingResult> packingResults = new List<AtlasPackingResult>();
                        Material tempMat = new Material(sh.shader);
                        MB_AtlasesAndRects atlasesAndRects = new MB_AtlasesAndRects();
                        combiner.CombineTexturesIntoAtlases(null, atlasesAndRects, tempMat, mom.GetObjectsToCombine(), allMatsThatUserShader, mom.texturePropNamesToIgnore, null, packingResults,
                            onlyPackRects:true, splitAtlasWhenPackingIfTooBig:true);
                        for (int i = 0; i < packingResults.Count; i++)
                        {

                            List<MB_MaterialAndUVRect> matsData = (List<MB_MaterialAndUVRect>)packingResults[i].data;
                            List<Material> mats = new List<Material>();
                            for (int j = 0; j < matsData.Count; j++)
                            {
                                Material mat = matsData[j].material;
                                if (!mats.Contains(mat))
                                {
                                    mats.Add(mat);
                                }
                            }
                            binsOfMatsThatUseShader.Add(mats);
                        }
                        numResMats += binsOfMatsThatUseShader.Count;
                    }
                }
            }

            //build the result materials
            if (shader2Material_map.Count == 0 && obUVobject2mesh_map.Count == 0) Debug.LogError("Found no materials in list of objects to combine");
            mom.resultMaterials = new MB_MultiMaterial[numResMats + obUVobject2mesh_map.Count];
            string pth = AssetDatabase.GetAssetPath(mom.textureBakeResults);
            string baseName = Path.GetFileNameWithoutExtension(pth);
            string folderPath = pth.Substring(0, pth.Length - baseName.Length - 6);
            int k = 0;
            foreach (MB3_TextureBakerEditorInternal.MultiMatSubmeshInfo sh in shader2Material_map.Keys)
            {
                foreach (List<Material> matsThatUse in shader2Material_map[sh])
                {
                    MB_MultiMaterial mm = mom.resultMaterials[k] = new MB_MultiMaterial();
                    mm.sourceMaterials = matsThatUse;
                    if (mm.sourceMaterials.Count == 1)
                    {
                        mm.considerMeshUVs = false;
                    }
                    else
                    {
                        mm.considerMeshUVs = mom.fixOutOfBoundsUVs;
                    }
                    string matName = folderPath + baseName + "-mat" + k + ".mat";
                    Material newMat = new Material(Shader.Find("Diffuse"));
                    if (matsThatUse.Count > 0 && matsThatUse[0] != null)
                    {
                        MB3_TextureBaker.ConfigureNewMaterialToMatchOld(newMat, matsThatUse[0]);
                    }
                    AssetDatabase.CreateAsset(newMat, matName);
                    mm.combinedMaterial = (Material)AssetDatabase.LoadAssetAtPath(matName, typeof(Material));
                    k++;
                }
            }
            foreach (Material m in obUVobject2mesh_map.Keys)
            {
                MB_MultiMaterial mm = mom.resultMaterials[k] = new MB_MultiMaterial();
                mm.sourceMaterials = new List<Material>();
                mm.sourceMaterials.Add(m);
                mm.considerMeshUVs = false;
                string matName = folderPath + baseName + "-mat" + k + ".mat";
                Material newMat = new Material(Shader.Find("Diffuse"));
                MB3_TextureBaker.ConfigureNewMaterialToMatchOld(newMat, m);
                AssetDatabase.CreateAsset(newMat, matName);
                mm.combinedMaterial = (Material)AssetDatabase.LoadAssetAtPath(matName, typeof(Material));
                k++;
            }
            MBVersionEditor.UpdateIfDirtyOrScript(textureBaker);
        }

    }
}
