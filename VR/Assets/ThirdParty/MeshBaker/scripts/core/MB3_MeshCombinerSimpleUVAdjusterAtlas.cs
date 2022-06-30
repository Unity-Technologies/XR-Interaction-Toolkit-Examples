using UnityEngine;
using System.Collections;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using System.Text;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.Core
{
    public partial class MB3_MeshCombinerSingle : MB3_MeshCombiner
    {
        public class UVAdjuster_Atlas
        {
            MB2_TextureBakeResults textureBakeResults;
            MB2_LogLevel LOG_LEVEL;
            int[] numTimesMatAppearsInAtlas;
            MB_MaterialAndUVRect[] matsAndSrcUVRect;

            //Dictionary<Material, Material> runtimeMat_2_buildtimeMap;

            /// <summary>
            /// Mesh Baker maps materials on the source meshes to rectangles in the atlas.
            /// We we need to know is-mat-on-cube == mat-that-was-baked.
            /// matA == matB works great when using direct reference to assets in project folder.
            /// When using Addressables, the materials at runtime could be using the same material but it is
            /// a different instance. In this case we compare the material's name.
            /// </summary>
            bool compareNamesWhenComparingMaterials = false;

            public UVAdjuster_Atlas(MB2_TextureBakeResults tbr, MB2_LogLevel ll)
            {
                textureBakeResults = tbr;
                LOG_LEVEL = ll;
                matsAndSrcUVRect = tbr.materialsAndUVRects;

                {
                    compareNamesWhenComparingMaterials = false;
                    // This is an option if using addressables. 
                    // At runtime the materials can be different instances but are the same material.
                    // We replaced this hack by adding the primary key (path to material asset) in the textureBakeResult
                    // User can use this key to find the runtimeMaterial. However somebody could be using something other than 
                    // the path as the primary key. Could fall back to using this.
                    if (MBVersion.IsUsingAddressables() &&
                        Application.isPlaying)
                    {
                        compareNamesWhenComparingMaterials = true;
                    }
                    else
                    {
                        compareNamesWhenComparingMaterials = false;
                    }
                }

                //count the number of times a material appears in the atlas. used for fast lookup
                numTimesMatAppearsInAtlas = new int[matsAndSrcUVRect.Length];
                for (int i = 0; i < matsAndSrcUVRect.Length; i++)
                {
                    if (numTimesMatAppearsInAtlas[i] > 1)
                    {
                        continue;
                    }
                    int count = 1;
                    for (int j = i + 1; j < matsAndSrcUVRect.Length; j++)
                    {
                        if (matsAndSrcUVRect[i].material == matsAndSrcUVRect[j].material)
                        {
                            count++;
                        }
                    }
                    numTimesMatAppearsInAtlas[i] = count;
                    if (count > 1)
                    {
                        //allMatsAreUnique = false;
                        for (int j = i + 1; j < matsAndSrcUVRect.Length; j++)
                        {
                            if (matsAndSrcUVRect[i].material == matsAndSrcUVRect[j].material)
                            {
                                numTimesMatAppearsInAtlas[j] = count;
                            }
                        }
                    }
                }

                /*
                runtimeMat_2_buildtimeMap = new Dictionary<Material, Material>();
                for (int i = 0; i < matsAndSrcUVRect.Length; i++)
                {
                    if (matsAndSrcUVRect[i].runtimeMaterial != null)
                    {
                        runtimeMat_2_buildtimeMap.Add(matsAndSrcUVRect[i].runtimeMaterial, matsAndSrcUVRect[i].material);
                    } else
                    {
                        runtimeMat_2_buildtimeMap.Add(matsAndSrcUVRect[i].material, matsAndSrcUVRect[i].material);
                    }
                }
                */
            }

            public bool MapSharedMaterialsToAtlasRects(Material[] sharedMaterials,
                bool checkTargetSubmeshIdxsFromPreviousBake,
                Mesh m, MeshChannelsCache meshChannelsCache,
                Dictionary<int, MB_Utility.MeshAnalysisResult[]> meshAnalysisResultsCache,
                OrderedDictionary sourceMats2submeshIdx_map, GameObject go,  MB_DynamicGameObject dgoOut)
            {
                MB_TextureTilingTreatment[] tilingTreatment = new MB_TextureTilingTreatment[sharedMaterials.Length];
                Rect[] uvRectsInAtlas = new Rect[sharedMaterials.Length];
                Rect[] encapsulatingRect = new Rect[sharedMaterials.Length];
                Rect[] sourceMaterialTiling = new Rect[sharedMaterials.Length];
                int[] sliceIdx = new int[sharedMaterials.Length];
                String errorMsg = "";
                for (int srcSubmeshIdx = 0; srcSubmeshIdx<sharedMaterials.Length; srcSubmeshIdx++)
                {
                    System.Object subIdx = null; // sourceMats2submeshIdx_map[sharedMaterials[srcSubmeshIdx]];
                    foreach(DictionaryEntry de in sourceMats2submeshIdx_map)
                    {
                        //Material mm; // If using addressables we might be needing to map the runtime materails to mats used in build.
                        //if (!runtimeMat_2_buildtimeMap.TryGetValue(sharedMaterials[srcSubmeshIdx], out mm)) mm = sharedMaterials[srcSubmeshIdx];
                        if (IsSameMaterialInTextureBakeResult(sharedMaterials[srcSubmeshIdx], (Material)de.Key))
                        {
                            subIdx = (int)de.Value;
                        }
                    }

                    int resMatIdx;
                    if (subIdx == null)
                    {
                        Debug.LogError("Source object " + go.name + " used a material " + sharedMaterials[srcSubmeshIdx] + " that was not in the baked materials.");
                        return false;
                    }
                    else
                    {
                        resMatIdx = (int) subIdx;
                        if (checkTargetSubmeshIdxsFromPreviousBake)
                        {
                            /*
                                Possibilities:
                                Consider a mesh with three submeshes with materials A, B, C that map to
                                different submeshes in the combined mesh, AA,BB,CC. The user is updating the UVs on a 
                                MeshRenderer so that object 'one' now uses material C => CC instead of A => AA. This will mean that the
                                triangle buffers will need to be resized. This is not allowed in UpdateGameObjects.
                                Must map to the same submesh that the old one mapped to.
                            */

                            if (resMatIdx != dgoOut.targetSubmeshIdxs[srcSubmeshIdx])
                            {
                                Debug.LogError(String.Format("Update failed for object {0}. Material {1} is mapped to a different submesh in the combined mesh than the previous material. This is not supported. Try using AddDelete.", go.name, sharedMaterials[srcSubmeshIdx]));
                                return false;
                            }
                        }
                    }

                    if (!TryMapMaterialToUVRect(sharedMaterials[srcSubmeshIdx], m, srcSubmeshIdx, resMatIdx, meshChannelsCache, meshAnalysisResultsCache,
                                                            out tilingTreatment[srcSubmeshIdx],
                                                            out uvRectsInAtlas[srcSubmeshIdx],
                                                            out encapsulatingRect[srcSubmeshIdx],
                                                            out sourceMaterialTiling[srcSubmeshIdx],
                                                            out sliceIdx[srcSubmeshIdx],
                                                            ref errorMsg, LOG_LEVEL))
                    {
                        Debug.LogError(errorMsg);
                        return false;
                    }
                }

                dgoOut.uvRects = uvRectsInAtlas;
                dgoOut.encapsulatingRect = encapsulatingRect;
                dgoOut.sourceMaterialTiling = sourceMaterialTiling;
                dgoOut.textureArraySliceIdx = sliceIdx;
                return true;
            }

            /// <summary>
            /// When combining at runtime, the materials stored in the TextureBakeResult are direct Material references.
            /// If using Addressables, then the materials could have been packaged into an asset bundle. At runtime, these are now "different".
            /// </summary>
            public bool IsSameMaterialInTextureBakeResult(Material a, Material b)
            {
                if (a == b) return true;

                if (compareNamesWhenComparingMaterials)
                {
                    // This is an option if using addressables. 
                    // At runtime the materials can be different instances but are the same material.
                    // We replaced this hack by adding the primary key (path to material asset) in the textureBakeResult
                    // User can use this key to find the runtimeMaterial. However somebody could be using something other than 
                    // the path as the primary key. Could fall back to using this.
                    if (a != null && b != null && a.name.Equals(b.name)) return true;
                }

                return false;
            }

            public void _copyAndAdjustUVsFromMesh(MB2_TextureBakeResults tbr, MB_DynamicGameObject dgo, Mesh mesh, int uvChannel, int vertsIdx, Vector2[] uvsOut, float[] uvsSliceIdx, MeshChannelsCache meshChannelsCache)
            {
                Debug.Assert(dgo.sourceSharedMaterials != null && dgo.sourceSharedMaterials.Length == dgo.targetSubmeshIdxs.Length,
                    "sourceSharedMaterials array was a different size than the targetSubmeshIdxs. Was this old data that is being updated? " + dgo.sourceSharedMaterials.Length);
                Vector2[] nuvs = meshChannelsCache.GetUVChannel(uvChannel, mesh);

                int[] done = new int[nuvs.Length]; //use this to track uvs that have already been adjusted don't adjust twice
                for (int l = 0; l < done.Length; l++) done[l] = -1;
                bool triangleArraysOverlap = false;

                //Rect uvRectInSrc = new Rect (0f,0f,1f,1f);
                //need to address the UVs through the submesh indexes because
                //each submesh has a different UV index
                bool doTextureArray = tbr.resultType == MB2_TextureBakeResults.ResultType.textureArray;
                for (int srcSubmeshIdx = 0; srcSubmeshIdx < dgo.targetSubmeshIdxs.Length; srcSubmeshIdx++)
                {
                    int[] srcSubTris;
                    if (dgo._tmpSubmeshTris != null)
                    {
                        srcSubTris = dgo._tmpSubmeshTris[srcSubmeshIdx].data;
                    }
                    else
                    {
                        srcSubTris = mesh.GetTriangles(srcSubmeshIdx);
                    }

                    float slice = dgo.textureArraySliceIdx[srcSubmeshIdx];
                    int resultSubmeshIdx = dgo.targetSubmeshIdxs[srcSubmeshIdx];

                    if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(String.Format("Build UV transform for mesh {0} submesh {1} encapsulatingRect {2}",
                                dgo.name, srcSubmeshIdx, dgo.encapsulatingRect[srcSubmeshIdx]));

                    bool considerUVs = textureBakeResults.GetConsiderMeshUVs(resultSubmeshIdx, dgo.sourceSharedMaterials[srcSubmeshIdx]);
                    Rect rr = MB3_TextureCombinerMerging.BuildTransformMeshUV2AtlasRect(
                            considerUVs,
                            dgo.uvRects[srcSubmeshIdx],
                            dgo.obUVRects == null || dgo.obUVRects.Length == 0 ? new Rect(0, 0, 1, 1) : dgo.obUVRects[srcSubmeshIdx],
                            dgo.sourceMaterialTiling[srcSubmeshIdx],
                            dgo.encapsulatingRect[srcSubmeshIdx]);

                    for (int srcSubTriIdx = 0; srcSubTriIdx < srcSubTris.Length; srcSubTriIdx++)
                    {
                        int srcVertIdx = srcSubTris[srcSubTriIdx];
                        if (done[srcVertIdx] == -1)
                        {
                            done[srcVertIdx] = srcSubmeshIdx; //prevents a uv from being adjusted twice. Same vert can be on more than one submesh.
                            Vector2 nuv = nuvs[srcVertIdx]; //don't modify nuvs directly because it is cached and we might be re-using
                                                      //if (textureBakeResults.fixOutOfBoundsUVs) {
                                                      //uvRectInSrc can be larger than (out of bounds uvs) or smaller than 0..1
                                                      //this transforms the uvs so they fit inside the uvRectInSrc sample box 

                            // scale, shift to fit in atlas rect
                            nuv.x = rr.x + nuv.x * rr.width;
                            nuv.y = rr.y + nuv.y * rr.height;
                            int idx = vertsIdx + srcVertIdx;
                            uvsOut[idx] = nuv;
                            if (doTextureArray)
                            {
                                uvsSliceIdx[idx] = slice;
                            }
                        }
                        if (done[srcVertIdx] != srcSubmeshIdx)
                        {
                            triangleArraysOverlap = true;
                        }
                    }
                }
                if (triangleArraysOverlap)
                {
                    if (LOG_LEVEL >= MB2_LogLevel.warn)
                        Debug.LogWarning(dgo.name + "has submeshes which share verticies. Adjusted uvs may not map correctly in combined atlas.");
                }

                if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(string.Format("_copyAndAdjustUVsFromMesh copied {0} verts", nuvs.Length));
            }

            /// <summary>
            /// A material can appear more than once in an atlas if using fixOutOfBoundsUVs.
            /// in this case you need to use the UV rect of the mesh to find the correct rectangle.
            /// If the all properties on the mat use the same tiling then 
            /// encapsulatingRect can be larger and will include baked UV and material tiling
            /// If mat uses different tiling for different maps then encapsulatingRect is the uvs of
            /// source mesh used to bake atlas and sourceMaterialTilingOut is 0,0,1,1. This works because
            /// material tiling was baked into the atlas.
            /// </summary>
            public bool TryMapMaterialToUVRect(Material mat, Mesh m, int submeshIdx, int idxInResultMats,
                                                MB3_MeshCombinerSingle.MeshChannelsCache meshChannelCache,
                                                Dictionary<int, MB_Utility.MeshAnalysisResult[]> meshAnalysisCache,
                                                 out MB_TextureTilingTreatment tilingTreatment,
                                                 out Rect rectInAtlas,
                                                 out Rect encapsulatingRectOut,
                                                 out Rect sourceMaterialTilingOut,
                                                 out int sliceIdx,
                                                 ref String errorMsg,
                                                 MB2_LogLevel logLevel)
            {
                if (textureBakeResults.version < MB2_TextureBakeResults.VERSION)
                {
                    textureBakeResults.UpgradeToCurrentVersion(textureBakeResults);
                }

                //Material runtimeMat; // If using addressables we might be needing to map the runtime materails to mats used in build.
                //if (!runtimeMat_2_buildtimeMap.TryGetValue(mat, out runtimeMat)) runtimeMat = mat;

                tilingTreatment = MB_TextureTilingTreatment.unknown;
                if (textureBakeResults.materialsAndUVRects.Length == 0)
                {
                    errorMsg = "The 'Texture Bake Result' needs to be re-baked to be compatible with this version of Mesh Baker. Please re-bake using the MB3_TextureBaker.";
                    rectInAtlas = new Rect();
                    encapsulatingRectOut = new Rect();
                    sourceMaterialTilingOut = new Rect();
                    sliceIdx = -1;
                    return false;
                }

                if (mat == null)
                {
                    rectInAtlas = new Rect();
                    encapsulatingRectOut = new Rect();
                    sourceMaterialTilingOut = new Rect();
                    sliceIdx = -1;
                    errorMsg = String.Format("Mesh {0} Had no material on submesh {1} cannot map to a material in the atlas", m.name, submeshIdx);
                    return false;
                }
                if (submeshIdx >= m.subMeshCount)
                {
                    errorMsg = "Submesh index is greater than the number of submeshes";
                    rectInAtlas = new Rect();
                    encapsulatingRectOut = new Rect();
                    sourceMaterialTilingOut = new Rect();
                    sliceIdx = -1;
                    return false;
                }

                //find the first index of this material
                int idx = -1;
                for (int i = 0; i < matsAndSrcUVRect.Length; i++)
                {
                    if (IsSameMaterialInTextureBakeResult(mat, matsAndSrcUVRect[i].material))
                    {
                        idx = i;
                        break;
                    }
                }
                // if couldn't find material
                if (idx == -1)
                {
                    rectInAtlas = new Rect();
                    encapsulatingRectOut = new Rect();
                    sourceMaterialTilingOut = new Rect();
                    sliceIdx = -1;
                    errorMsg = String.Format("Material {0} could not be found in the Texture Bake Result", mat.name);
                    return false;
                }

                bool considerUVs = textureBakeResults.GetConsiderMeshUVs(idxInResultMats, mat);
                if (!considerUVs)
                {
                    if (numTimesMatAppearsInAtlas[idx] != 1)
                    {
                        Debug.LogError("There is a problem with this TextureBakeResults. FixOutOfBoundsUVs is false and a material appears more than once: " + matsAndSrcUVRect[idx].material + " appears: " + numTimesMatAppearsInAtlas[idx]);
                    }
                    MB_MaterialAndUVRect mr = matsAndSrcUVRect[idx];
                    rectInAtlas = mr.atlasRect;
                    tilingTreatment = mr.tilingTreatment;
                    encapsulatingRectOut = mr.GetEncapsulatingRect();
                    sourceMaterialTilingOut = mr.GetMaterialTilingRect();
                    sliceIdx = mr.textureArraySliceIdx;
                    return true;
                }
                else
                {
                    //todo what if no UVs
                    //Find UV rect in source mesh
                    MB_Utility.MeshAnalysisResult[] mar;
                    if (!meshAnalysisCache.TryGetValue(m.GetInstanceID(), out mar))
                    {
                        mar = new MB_Utility.MeshAnalysisResult[m.subMeshCount];
                        for (int j = 0; j < m.subMeshCount; j++)
                        {
                            Vector2[] uvss = meshChannelCache.GetUv0Raw(m);
                            MB_Utility.hasOutOfBoundsUVs(uvss, m, ref mar[j], j);
                        }
                        meshAnalysisCache.Add(m.GetInstanceID(), mar);
                    }

                    //this could be a mesh that was not used in the texture baking that has huge UV tiling too big for the rect that was baked
                    //find a record that has an atlas uvRect capable of containing this
                    bool found = false;
                    Rect encapsulatingRect = new Rect(0, 0, 0, 0);
                    Rect sourceMaterialTiling = new Rect(0, 0, 0, 0);
                    if (logLevel >= MB2_LogLevel.trace)
                    {
                        Debug.Log(String.Format("Trying to find a rectangle in atlas capable of holding tiled sampling rect for mesh {0} using material {1} meshUVrect={2}", m, mat, mar[submeshIdx].uvRect.ToString("f5")));
                    }
                    for (int i = idx; i < matsAndSrcUVRect.Length; i++)
                    {
                        MB_MaterialAndUVRect matAndUVrect = matsAndSrcUVRect[i];
                        if (IsSameMaterialInTextureBakeResult(mat, matAndUVrect.material))
                        {
                            if (matAndUVrect.allPropsUseSameTiling)
                            {
                                encapsulatingRect = matAndUVrect.allPropsUseSameTiling_samplingEncapsulatinRect;
                                sourceMaterialTiling = matAndUVrect.allPropsUseSameTiling_sourceMaterialTiling;
                            }
                            else
                            {
                                encapsulatingRect = matAndUVrect.propsUseDifferntTiling_srcUVsamplingRect;
                                sourceMaterialTiling = new Rect(0, 0, 1, 1);
                            }

                            if (MB2_TextureBakeResults.IsMeshAndMaterialRectEnclosedByAtlasRect(
                                    matAndUVrect.tilingTreatment,
                                    mar[submeshIdx].uvRect,
                                    sourceMaterialTiling,
                                    encapsulatingRect,
                                    logLevel))
                            {
                                if (logLevel >= MB2_LogLevel.trace)
                                {
                                    Debug.Log("Found rect in atlas capable of containing tiled sampling rect for mesh " + m + " at idx=" + i);
                                }
                                idx = i;
                                found = true;
                                break;
                            }
                        }
                    }
                    if (found)
                    {
                        MB_MaterialAndUVRect mr = matsAndSrcUVRect[idx];
                        rectInAtlas = mr.atlasRect;
                        tilingTreatment = mr.tilingTreatment;
                        encapsulatingRectOut = mr.GetEncapsulatingRect();
                        sourceMaterialTilingOut = mr.GetMaterialTilingRect();
                        sliceIdx = mr.textureArraySliceIdx;
                        return true;
                    }
                    else
                    {
                        rectInAtlas = new Rect();
                        encapsulatingRectOut = new Rect();
                        sourceMaterialTilingOut = new Rect();
                        sliceIdx = -1;
                        errorMsg = String.Format("Could not find a tiled rectangle in the atlas capable of containing the uv and material tiling on mesh {0} for material {1}. Was this mesh included when atlases were baked?", m.name, mat);
                        return false;
                    }
                }
            }
        }
    }
}