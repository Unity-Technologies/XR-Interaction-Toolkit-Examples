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
using System.Text.RegularExpressions;
using DigitalOpus.MB.Core;

using UnityEditor;

namespace DigitalOpus.MB.MBEditor
{

    public class MB_TextureBakerConfigureTextureArrays
    {
        private class Slice
        {
            public List<MB_MaterialAndUVRect> atlasRects;
            public AtlasPackingResult packingResult;
            public int numAtlasRects;
        }

        private static GUIContent gc_TextureArrayOutputFormats = new GUIContent(
            "Texture Array Output Formats",
            "Texture Arrays do not have a 'TextureImporter' that lets you change the TextureArray format." +
            "You can provide a list of formats to be generated here. You will probably have one set of formats per platform.");
        public static void DrawTextureArrayConfiguration(MB3_TextureBaker momm, SerializedObject textureBaker, MB3_TextureBakerEditorInternal editorInternal)
        {
            EditorGUILayout.BeginVertical(editorInternal.editorStyles.multipleMaterialBackgroundStyle);
            EditorGUILayout.LabelField("Texture Array Slice Configuration", EditorStyles.boldLabel);

            float oldLabelWidth = EditorGUIUtility.labelWidth;

            if (GUILayout.Button(MB3_TextureBakerEditorInternal.configAtlasTextureSlicesFromObjsContent))
            {
                ConfigureTextureArraysFromObjsToCombine(momm, editorInternal.resultMaterialsTexArray, textureBaker);
            }

            if (GUILayout.Button("Report texture sizes"))
            {
                Debug.Log(ReportTextureSizesAndFormats(momm));
            }

            if (editorInternal.textureArrayOutputFormats.arraySize == 0)
            {
                EditorGUILayout.HelpBox("You need at least one output format.", MessageType.Error);
            }

            EditorGUILayout.PropertyField(editorInternal.textureArrayOutputFormats, gc_TextureArrayOutputFormats, true);
            EditorGUILayout.BeginHorizontal();
            editorInternal.resultMaterialsFoldout = EditorGUILayout.Foldout(editorInternal.resultMaterialsFoldout,  MB3_TextureBakerEditorInternal.textureArrayCombinedMaterialFoldoutGUIContent);
            if (GUILayout.Button(MB3_TextureBakerEditorInternal.insertContent, EditorStyles.miniButtonLeft, MB3_TextureBakerEditorInternal.buttonWidth))
            {
                if (editorInternal.resultMaterialsTexArray.arraySize == 0)
                {
                    momm.resultMaterialsTexArray = new MB_MultiMaterialTexArray[1];
                }
                else
                {
                    int idx = editorInternal.resultMaterialsTexArray.arraySize - 1;
                    editorInternal.resultMaterialsTexArray.InsertArrayElementAtIndex(idx);
                }
            }

            if (GUILayout.Button(MB3_TextureBakerEditorInternal.deleteContent, EditorStyles.miniButtonRight, MB3_TextureBakerEditorInternal.buttonWidth))
            {
                editorInternal.resultMaterialsTexArray.DeleteArrayElementAtIndex(editorInternal.resultMaterialsTexArray.arraySize - 1);
            }

            EditorGUILayout.EndHorizontal();

            
            if (editorInternal.resultMaterialsFoldout)
            {
                for (int i = 0; i < editorInternal.resultMaterialsTexArray.arraySize; i++)
                {
                    
                    EditorGUILayout.Separator();
                    if (i % 2 == 1)
                    {
                        EditorGUILayout.BeginVertical(editorInternal.editorStyles.multipleMaterialBackgroundStyle);
                    }
                    else
                    {
                        EditorGUILayout.BeginVertical(editorInternal.editorStyles.multipleMaterialBackgroundStyleDarker);
                    }
                    
                    string s = "";
                    if (i < momm.resultMaterialsTexArray.Length && momm.resultMaterialsTexArray[i] != null && momm.resultMaterialsTexArray[i].combinedMaterial != null) s = momm.resultMaterialsTexArray[i].combinedMaterial.shader.ToString();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("---------- submesh:" + i + " " + s, EditorStyles.boldLabel);
                    if (GUILayout.Button(MB3_TextureBakerEditorInternal.deleteContent, EditorStyles.miniButtonRight, MB3_TextureBakerEditorInternal.buttonWidth))
                    {
                        editorInternal.resultMaterialsTexArray.DeleteArrayElementAtIndex(i);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    if (i < editorInternal.resultMaterialsTexArray.arraySize)
                    {
                        EditorGUILayout.Separator();
                        SerializedProperty resMat = editorInternal.resultMaterialsTexArray.GetArrayElementAtIndex(i);
                        EditorGUILayout.PropertyField(resMat.FindPropertyRelative("combinedMaterial"));
                        SerializedProperty slices = resMat.FindPropertyRelative("slices");
                        EditorGUILayout.PropertyField(slices, true);
                    }
                    
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndVertical();
            
        }

        public static string ReportTextureSizesAndFormats(MB3_TextureBaker mom)
        {
            if (mom.resultType != MB2_TextureBakeResults.ResultType.textureArray)
            {
                Debug.LogError("Result Type must be Texture Array.");
                return "";
            }

            for (int resMatIdx = 0; resMatIdx < mom.resultMaterialsTexArray.Length; resMatIdx++)
            {
                MB_MultiMaterialTexArray resMatTexArray = mom.resultMaterialsTexArray[resMatIdx];
                if (resMatTexArray.combinedMaterial == null)
                {
                    Debug.LogError("Result Material " + resMatIdx + " is null");
                    return "";
                }
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            // Visit each result material
            for (int resMatIdx = 0; resMatIdx < mom.resultMaterialsTexArray.Length; resMatIdx++)
            {
                MB_MultiMaterialTexArray resMatTexArray = mom.resultMaterialsTexArray[resMatIdx];

                // Do an atlas pack in order to collect all the textures needed by the result material
                // And group these by material texture property.

                MB3_TextureCombiner combiner = mom.CreateAndConfigureTextureCombiner();
                combiner.saveAtlasesAsAssets = false;
                combiner.fixOutOfBoundsUVs = false;
                List<AtlasPackingResult> packingResults = new List<AtlasPackingResult>();
                Material tempMat = new Material(resMatTexArray.combinedMaterial.shader);

                List<Material> allSourceMaterials = new List<Material>();
                for (int sliceIdx = 0; sliceIdx < resMatTexArray.slices.Count; sliceIdx++)
                {
                    List<Material> srcMats = new List<Material>();
                    resMatTexArray.slices[sliceIdx].GetAllUsedMaterials(srcMats);
                    for (int srcMatIdx = 0; srcMatIdx < srcMats.Count; srcMatIdx++)
                    {
                        if (srcMats[srcMatIdx] != null && !allSourceMaterials.Contains(srcMats[srcMatIdx]))
                        {
                            allSourceMaterials.Add(srcMats[srcMatIdx]);
                        }
                    }
                }

                MB_AtlasesAndRects atlasesAndRects = new MB_AtlasesAndRects();
                combiner.CombineTexturesIntoAtlases(null, atlasesAndRects, tempMat, mom.GetObjectsToCombine(), allSourceMaterials, mom.texturePropNamesToIgnore, null, packingResults,
                        onlyPackRects:true, splitAtlasWhenPackingIfTooBig:false);

                // Now vist the packing results and collect all the textures
                Debug.Assert(packingResults.Count == 1);
                for (int texPropIdx = 0; texPropIdx < atlasesAndRects.texPropertyNames.Length; texPropIdx++)
                {
                    string propertyName = atlasesAndRects.texPropertyNames[texPropIdx];
                    sb.AppendLine(String.Format("Prop: {0}", propertyName));

                    List<MB_MaterialAndUVRect> matsData = (List<MB_MaterialAndUVRect>)packingResults[0].data;
                    HashSet<Material> visitedMats = new HashSet<Material>();
                    for (int matAndGoIdx = 0; matAndGoIdx < matsData.Count; matAndGoIdx++)
                    {
                        Material mat = matsData[matAndGoIdx].material;
                        if (visitedMats.Contains(mat)) continue;
                        visitedMats.Add(mat);
                        if (mat.HasProperty(propertyName))
                        {
                            Texture tex = mat.GetTexture(propertyName);
                            if (tex != null)
                            {
                                string texFormatString = "UnknownFormat";
                                string texWrapMode = "UnknownClampMode";
                                if (tex is Texture2D)
                                {
                                    texFormatString = ((Texture2D)tex).format.ToString();
                                    texWrapMode = ((Texture2D)tex).wrapMode.ToString();
                                }

                                sb.AppendLine(String.Format("    {0} x {1}  format:{2}  wrapMode:{3}   {4}", tex.width.ToString().PadLeft(6,' '), tex.height.ToString().PadRight(6,' '), texFormatString.PadRight(20,' '), texWrapMode.PadRight(12,' '), tex.name));
                            }
                        }
                    }
                }
            }

            return sb.ToString();
        }

        public static void ConfigureTextureArraysFromObjsToCombine(MB3_TextureBaker mom, SerializedProperty resultMaterialsTexArrays, SerializedObject textureBaker)
        {
            if (mom.GetObjectsToCombine().Count == 0)
            {
                Debug.LogError("You need to add some objects to combine before building the texture array result materials.");
                return;
            }
            if (resultMaterialsTexArrays.arraySize > 0)
            {
                Debug.LogError("You already have some texture array result materials configured. You must remove these before doing this operation.");
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

                if (MB_Utility.GetMesh(go) == null)
                {
                    Debug.LogError("Could not get mesh for object in list of objects to combine at position " + i);
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

            //Will sort into "result material"
            //  slices
            Dictionary<MB3_TextureBakerEditorInternal.MultiMatSubmeshInfo, List<Slice>> shader2ResultMat_map = new Dictionary<MB3_TextureBakerEditorInternal.MultiMatSubmeshInfo, List<Slice>>();

            // first pass split by shader and analyse meshes.
            List<GameObject> objsToCombine = mom.GetObjectsToCombine();
            for (int meshIdx = 0; meshIdx < objsToCombine.Count; meshIdx++)
            {
                GameObject srcGo = objsToCombine[meshIdx];
                Mesh mesh = MB_Utility.GetMesh(srcGo);
                Renderer r = MB_Utility.GetRenderer(srcGo);

                if (mom.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("1st Pass 'Split By Shader' Processing Mesh: "+ mesh +" Num submeshes: " + r.sharedMaterials.Length);
                for (int submeshIdx = 0; submeshIdx < r.sharedMaterials.Length; submeshIdx++)
                {
                    if (r.sharedMaterials[submeshIdx] == null) continue;

                    MB3_TextureBakerEditorInternal.MultiMatSubmeshInfo newKey = new MB3_TextureBakerEditorInternal.MultiMatSubmeshInfo(r.sharedMaterials[submeshIdx].shader, r.sharedMaterials[submeshIdx]);
                    // Initially we fill the list of srcMaterials with garbage MB_MaterialAndUVRects. Will get proper ones when we atlas pack.
                    MB_MaterialAndUVRect submeshMaterial = new MB_MaterialAndUVRect(
                            r.sharedMaterials[submeshIdx],
                            new Rect(0, 0, 0, 0), // garbage value
                            false, //garbage value
                            new Rect(0, 0, 0, 0), // garbage value
                            new Rect(0, 0, 0, 0), // garbage value
                            new Rect(0, 0, 1, 1), // garbage value
                            MB_TextureTilingTreatment.unknown, // garbage value
                            r.name);
                    submeshMaterial.objectsThatUse = new List<GameObject>();
                    submeshMaterial.objectsThatUse.Add(r.gameObject);
                    if (!shader2ResultMat_map.ContainsKey(newKey))
                    {
                        // this is a new shader create a new result material
                        Slice srcMaterials = new Slice
                        {
                            atlasRects = new List<MB_MaterialAndUVRect>(),
                            numAtlasRects = 1,

                        };
                        srcMaterials.atlasRects.Add(submeshMaterial);
                        List<Slice> binsOfMatsThatUseShader = new List<Slice>();
                        binsOfMatsThatUseShader.Add(srcMaterials);
                        if (mom.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("  Adding Source Material: " + submeshMaterial.material);
                        shader2ResultMat_map.Add(newKey, binsOfMatsThatUseShader);
                    }
                    else
                    {
                        // there is a result material that uses this shader. Add this source material
                        Slice srcMaterials = shader2ResultMat_map[newKey][0]; // There should only be one list of source materials
                        if (srcMaterials.atlasRects.Find(x => x.material == submeshMaterial.material) == null)
                        {
                            if (mom.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("  Adding Source Material: " + submeshMaterial.material);
                            srcMaterials.atlasRects.Add(submeshMaterial);
                        }
                    }
                }
            }

            int resMatCount = 0;
            foreach (MB3_TextureBakerEditorInternal.MultiMatSubmeshInfo resultMat in shader2ResultMat_map.Keys)
            {
                // at this point there there should be only one slice with all the source materials
                resMatCount++;

                // For each result material, all source materials are in the first slice.
                // We will now split these using a texture packer. Each "atlas" generated by the packer will be a slice.
                {
                    // All source materials should be in the first slice at this point.
                    List<Slice> slices = shader2ResultMat_map[resultMat];
                    List<Slice> newSlices = new List<Slice>();
                    Slice firstSlice = slices[0];
                    List<Material> allMatsThatUserShader = new List<Material>();
                    List<GameObject> objsThatUseFirstSlice = new List<GameObject>();
                    for (int i = 0; i < firstSlice.atlasRects.Count; i++)
                    {
                        allMatsThatUserShader.Add(firstSlice.atlasRects[i].material);
                        if (!objsThatUseFirstSlice.Contains(firstSlice.atlasRects[i].objectsThatUse[0]))
                        {
                            objsThatUseFirstSlice.Add(firstSlice.atlasRects[i].objectsThatUse[0]);
                        }
                    }

                    MB3_TextureCombiner combiner = mom.CreateAndConfigureTextureCombiner();
                    combiner.packingAlgorithm = MB2_PackingAlgorithmEnum.MeshBakerTexturePacker;
                    combiner.saveAtlasesAsAssets = false;
                    combiner.fixOutOfBoundsUVs = true;
                    combiner.doMergeDistinctMaterialTexturesThatWouldExceedAtlasSize = true;
                    List<AtlasPackingResult> packingResults = new List<AtlasPackingResult>();
                    Material tempMat = new Material(resultMat.shader);

                    if (mom.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("======== 2nd pass. Use atlas packer to split the first slice into multiple if it exceeds atlas size. ");
                    combiner.CombineTexturesIntoAtlases(null, null, tempMat, mom.GetObjectsToCombine(), allMatsThatUserShader, mom.texturePropNamesToIgnore, null,  packingResults,
                        onlyPackRects:true, splitAtlasWhenPackingIfTooBig:true);
                    if (mom.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("======== Completed packing with texture packer. numPackingResults: " + packingResults.Count);
                    newSlices.Clear();
                        
                    // The texture packing just split the atlas into multiple atlases. Each atlas will become a "slice".
                    for (int newSliceIdx = 0; newSliceIdx < packingResults.Count; newSliceIdx++)
                    {
                        List<MB_MaterialAndUVRect> sourceMats = new List<MB_MaterialAndUVRect>();
                        List<MB_MaterialAndUVRect> packedMatRects = (List<MB_MaterialAndUVRect>) packingResults[newSliceIdx].data;
                        HashSet<Rect> distinctAtlasRects = new HashSet<Rect>();
                        for (int packedMatRectIdx = 0; packedMatRectIdx < packedMatRects.Count; packedMatRectIdx++)
                        {
                            MB_MaterialAndUVRect muvr = packedMatRects[packedMatRectIdx];
                            distinctAtlasRects.Add(muvr.atlasRect);
                            //{
                            //    Rect encapsulatingRect = muvr.GetEncapsulatingRect();
                            //    Vector2 sizeInAtlas_px = new Vector2(
                            //        packingResults[newSliceIdx].atlasX * encapsulatingRect.width,
                            //        packingResults[newSliceIdx].atlasY * encapsulatingRect.height);
                            //}
                            sourceMats.Add(muvr);
                        }

                        Slice slice = new Slice()
                        {
                            atlasRects = sourceMats,
                            packingResult = packingResults[newSliceIdx],
                            numAtlasRects = distinctAtlasRects.Count,
                        };

                        newSlices.Add(slice);
                    }

                    // Replace first slice with split version.
                    if (mom.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("First slice exceeded atlas size splitting it into " + newSlices.Count + " slices");
                    slices.RemoveAt(0);
                    for (int i = 0; i < newSlices.Count; i++)
                    {
                        slices.Insert(i, newSlices[i]);
                    }
                }
            }

            // build the texture array result materials
            if (shader2ResultMat_map.Count == 0) Debug.LogError("Found no materials in list of objects to combine");
            mom.resultMaterialsTexArray = new MB_MultiMaterialTexArray[shader2ResultMat_map.Count];
            int k = 0;
            foreach (MB3_TextureBakerEditorInternal.MultiMatSubmeshInfo resMatKey in shader2ResultMat_map.Keys)
            {
                List<Slice> srcSlices = shader2ResultMat_map[resMatKey];
                MB_MultiMaterialTexArray mm = mom.resultMaterialsTexArray[k] = new MB_MultiMaterialTexArray();
                for (int sliceIdx = 0; sliceIdx < srcSlices.Count; sliceIdx++)
                {
                    Slice slice = srcSlices[sliceIdx];
                    MB_TexArraySlice resSlice = new MB_TexArraySlice();
                    for (int srcMatIdx = 0; srcMatIdx < slice.atlasRects.Count; srcMatIdx++)
                    {
                        MB_MaterialAndUVRect matAndUVRect = slice.atlasRects[srcMatIdx];
                        List<GameObject> objsThatUse = matAndUVRect.objectsThatUse;
                        for (int objsThatUseIdx = 0; objsThatUseIdx < objsThatUse.Count; objsThatUseIdx++)
                        {
                            GameObject obj = objsThatUse[objsThatUseIdx];
                            if (!resSlice.ContainsMaterialAndMesh(slice.atlasRects[srcMatIdx].material, MB_Utility.GetMesh(obj)))
                            {
                                resSlice.sourceMaterials.Add(
                                    new MB_TexArraySliceRendererMatPair()
                                    {
                                        renderer = obj,
                                        sourceMaterial = slice.atlasRects[srcMatIdx].material
                                    }
                                );
                            }
                        }
                    }

                    {
                        // Should we use considerUVs
                        bool doConsiderUVs = false;
                        //     If there is more than one atlas rectangle in a slice then use considerUVs
                        if (slice.numAtlasRects > 1)
                        {
                            doConsiderUVs = true;
                        }
                        else
                        {
                            //     There is only one source material, could be: 
                            //          - lots of tiling (don't want consider UVs)
                            //          - We are extracting a small part of a large atlas (want considerUVs)
                            if (slice.packingResult.atlasX >= mom.maxAtlasSize ||
                                slice.packingResult.atlasY >= mom.maxAtlasSize)
                            {
                                doConsiderUVs = false; // lots of tiling
                            }
                            else
                            {
                                doConsiderUVs = true; // extracting a small part of an atlas
                            }
                        }

                        resSlice.considerMeshUVs = doConsiderUVs;
                    }

                    mm.slices.Add(resSlice);
                }

                // Enforce integrity. If a material appears in more than one slice then all those slices must be considerUVs=true
                {
                    // collect all distinct materials
                    HashSet<Material> distinctMats = new HashSet<Material>();
                    Dictionary<Material, int> mat2sliceCount = new Dictionary<Material, int>();
                    for (int sliceIdx = 0; sliceIdx < mm.slices.Count; sliceIdx++)
                    {
                        for (int sliceMatIdx = 0; sliceMatIdx < mm.slices[sliceIdx].sourceMaterials.Count; sliceMatIdx++)
                        {
                            Material mat = mm.slices[sliceIdx].sourceMaterials[sliceMatIdx].sourceMaterial;
                            distinctMats.Add(mat);
                            mat2sliceCount[mat] = 0;
                        }
                    }

                    // Count the number of slices that use each material.
                    foreach (Material mat in distinctMats)
                    {
                        for (int sliceIdx = 0; sliceIdx < mm.slices.Count; sliceIdx++)
                        {
                            if (mm.slices[sliceIdx].ContainsMaterial(mat))
                            {
                                mat2sliceCount[mat] = mat2sliceCount[mat] + 1;
                            }
                        }
                    }

                    // Check that considerUVs is true for any materials that appear more than once
                    foreach (Material mat in distinctMats)
                    {
                        if (mat2sliceCount[mat] > 1)
                        {
                            for (int sliceIdx = 0; sliceIdx < mm.slices.Count; sliceIdx++)
                            {
                                if (mm.slices[sliceIdx].ContainsMaterial(mat))
                                {
                                    if (mom.LOG_LEVEL >= MB2_LogLevel.debug &&
                                        mm.slices[sliceIdx].considerMeshUVs) Debug.Log("There was a material " + mat + " that was used by more than one slice and considerUVs was false. sliceIdx:" + sliceIdx);
                                    mm.slices[sliceIdx].considerMeshUVs = true;
                                }
                            }
                        }
                    }
                }

                // Cleanup. remove "Renderer"s from source materials that do not use considerUVs and delete extra
                {
                    // put any slices with consider UVs first
                    List<MB_TexArraySlice> newSlices = new List<MB_TexArraySlice>();
                    for (int sliceIdx = 0; sliceIdx < mm.slices.Count; sliceIdx++)
                    {
                        if (mm.slices[sliceIdx].considerMeshUVs == true)
                        {
                            newSlices.Add(mm.slices[sliceIdx]);
                        }
                    }

                    // for any slices without considerUVs, remove "renderer" and truncate
                    for (int sliceIdx = 0; sliceIdx < mm.slices.Count; sliceIdx++)
                    {
                        MB_TexArraySlice slice = mm.slices[sliceIdx];
                        if (slice.considerMeshUVs == false)
                        {
                            newSlices.Add(slice);
                            HashSet<Material> distinctMats = slice.GetDistinctMaterials();
                            slice.sourceMaterials.Clear();
                            foreach (Material mat in distinctMats)
                            {
                                slice.sourceMaterials.Add(new MB_TexArraySliceRendererMatPair() {sourceMaterial = mat });
                            }
                        }
                    }

                    mm.slices = newSlices;
                }

                string pth = AssetDatabase.GetAssetPath(mom.textureBakeResults);
                string baseName = Path.GetFileNameWithoutExtension(pth);
                string folderPath = pth.Substring(0, pth.Length - baseName.Length - 6);
                string matName = folderPath + baseName + "-mat" + k + ".mat";
                Material existingAsset = AssetDatabase.LoadAssetAtPath<Material>(matName);
                if (!existingAsset)
                {
                    Material newMat = new Material(Shader.Find("Standard"));
                    // Don't try to configure the material we need the user to pick a shader that has TextureArrays
                    AssetDatabase.CreateAsset(newMat, matName);
                }

                mm.combinedMaterial = (Material)AssetDatabase.LoadAssetAtPath(matName, typeof(Material));
                k++;
            }


            MBVersionEditor.UpdateIfDirtyOrScript(textureBaker);
            textureBaker.Update();
        }
    }
}