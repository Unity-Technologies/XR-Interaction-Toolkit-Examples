using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DigitalOpus.MB.Core
{
    public class MB3_TextureCombinerMerging
    {
        public static bool DO_INTEGRITY_CHECKS = false;

        private bool _HasBeenInitialized = false;

        public static Rect BuildTransformMeshUV2AtlasRect(
                                            bool considerMeshUVs,
                                            Rect _atlasRect,
                                            Rect _obUVRect,
                                            Rect _sourceMaterialTiling,
                                            Rect _encapsulatingRect)
        {
            DRect atlasRect = new DRect(_atlasRect);
            DRect obUVRect;
            if (considerMeshUVs)
            {
                obUVRect = new DRect(_obUVRect); //this is the uvRect in src mesh
            }
            else
            {
                obUVRect = new DRect(0.0, 0.0, 1.0, 1.0);
            }

            DRect sourceMaterialTiling = new DRect(_sourceMaterialTiling);

            DRect encapsulatingRectMatAndUVTiling = new DRect(_encapsulatingRect);

            DRect encapsulatingRectMatAndUVTilingInverse = MB3_UVTransformUtility.InverseTransform(ref encapsulatingRectMatAndUVTiling);

            DRect toNormalizedUVs = MB3_UVTransformUtility.InverseTransform(ref obUVRect);

            DRect meshFullSamplingRect = MB3_UVTransformUtility.CombineTransforms(ref obUVRect, ref sourceMaterialTiling);

            DRect shiftToFitInEncapsulating = MB3_UVTransformUtility.GetShiftTransformToFitBinA(ref encapsulatingRectMatAndUVTiling, ref meshFullSamplingRect);
            meshFullSamplingRect = MB3_UVTransformUtility.CombineTransforms(ref meshFullSamplingRect, ref shiftToFitInEncapsulating);

            //transform between full sample rect and encapsulating rect
            DRect relativeTrans = MB3_UVTransformUtility.CombineTransforms(ref meshFullSamplingRect, ref encapsulatingRectMatAndUVTilingInverse);

            // [transform] = [toNormalizedUVs][relativeTrans][uvSubRectInAtlas]
            DRect trans = MB3_UVTransformUtility.CombineTransforms(ref toNormalizedUVs, ref relativeTrans);
            trans = MB3_UVTransformUtility.CombineTransforms(ref trans, ref atlasRect);
            Rect rr = trans.GetRect();
            return rr;
        }

        bool _considerNonTextureProperties = false;
        MB3_TextureCombinerNonTextureProperties resultMaterialTextureBlender;
        bool fixOutOfBoundsUVs = true;
        public MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info;
        private static bool LOG_LEVEL_TRACE_MERGE_MAT_SUBRECTS = false;

        public MB3_TextureCombinerMerging(bool considerNonTextureProps, MB3_TextureCombinerNonTextureProperties resultMaterialTexBlender, bool fixObUVs, MB2_LogLevel logLevel)
        {
            LOG_LEVEL = logLevel;
            _considerNonTextureProperties = considerNonTextureProps;
            resultMaterialTextureBlender = resultMaterialTexBlender;
            fixOutOfBoundsUVs = fixObUVs;
        }

        public void MergeOverlappingDistinctMaterialTexturesAndCalcMaterialSubrects(List<MB_TexSet> distinctMaterialTextures)
        {
            if (LOG_LEVEL >= MB2_LogLevel.debug)
            {
                Debug.Log("MergeOverlappingDistinctMaterialTexturesAndCalcMaterialSubrects num atlas rects" + distinctMaterialTextures.Count);
            }

            int numMerged = 0;

            // IMPORTANT: Note that the verts stored in the mesh are NOT Normalized UV Coords. They are normalized * [UVTrans]. To get normalized UV
            // coords we must multiply them by [invUVTrans]. Need to do this to the verts in the mesh before we do any transforms with them.
            // Also check that all textures use same tiling. This is a prerequisite for merging.
            // Mark MB3_TexSet that are mergable (allTexturesUseSameMatTiling)
            for (int i = 0; i < distinctMaterialTextures.Count; i++)
            {
                MB_TexSet tx = distinctMaterialTextures[i];
                int idxOfFirstNotNull = -1;
                bool allAreSame = true;
                DRect firstRect = new DRect();
                for (int propIdx = 0; propIdx < tx.ts.Length; propIdx++)
                {
                    if (idxOfFirstNotNull != -1)
                    {
                        if (!tx.ts[propIdx].isNull && firstRect != tx.ts[propIdx].matTilingRect)
                        {
                            allAreSame = false;
                        }
                    }
                    else if (!tx.ts[propIdx].isNull)
                    {
                        idxOfFirstNotNull = propIdx;
                        firstRect = tx.ts[propIdx].matTilingRect;
                    }
                }
                if (LOG_LEVEL >= MB2_LogLevel.debug || LOG_LEVEL_TRACE_MERGE_MAT_SUBRECTS == true)
                {
                    if (allAreSame)
                    {
                        Debug.LogFormat("TextureSet {0} allTexturesUseSameMatTiling = {1}", i, allAreSame);
                    }
                    else
                    {
                        Debug.Log(string.Format("Textures in material(s) do not all use the same material tiling. This set of textures will not be considered for merge: {0} ", tx.GetDescription()));
                    }
                }
                if (allAreSame)
                {
                    tx.SetAllTexturesUseSameMatTilingTrue();
                }
            }

            for (int i = 0; i < distinctMaterialTextures.Count; i++)
            {
                MB_TexSet tx = distinctMaterialTextures[i];
                for (int matIdx = 0; matIdx < tx.matsAndGOs.mats.Count; matIdx++)
                {
                    if (tx.matsAndGOs.gos.Count > 0) {
                        tx.matsAndGOs.mats[matIdx].objName = tx.matsAndGOs.gos[0].name;
                    } else if (tx.ts[0] != null) {
                        tx.matsAndGOs.mats[matIdx].objName = string.Format("[objWithTx:{0} atlasBlock:{1} matIdx{2}]",tx.ts[0].GetTexName(),i,matIdx);
                    } else {
                        tx.matsAndGOs.mats[matIdx].objName = string.Format("[objWithTx:{0} atlasBlock:{1} matIdx{2}]", "Unknown", i, matIdx);
                    }
                }

                tx.CalcInitialFullSamplingRects(fixOutOfBoundsUVs);
                tx.CalcMatAndUVSamplingRects();
            }

            _HasBeenInitialized = true;
            // need to calculate the srcSampleRect for the complete tiling in the atlas
            // for each material need to know what the subrect would be in the atlas if material UVRect was 0,0,1,1 and Merged uvRect was full tiling
            List<int> MarkedForDeletion = new List<int>();
            for (int i = 0; i < distinctMaterialTextures.Count; i++)
            {
                MB_TexSet tx2 = distinctMaterialTextures[i];
                for (int j = i + 1; j < distinctMaterialTextures.Count; j++)
                {
                    MB_TexSet tx1 = distinctMaterialTextures[j];
                    if (tx1.AllTexturesAreSameForMerge(tx2, _considerNonTextureProperties, resultMaterialTextureBlender))
                    {
                        double accumulatedAreaCombined = 0f;
                        double accumulatedAreaNotCombined = 0f;
                        DRect encapsulatingRectMerged = new DRect();
                        int idxOfFirstNotNull = -1;
                        for (int propIdx = 0; propIdx < tx2.ts.Length; propIdx++)
                        {
                            if (!tx2.ts[propIdx].isNull)
                            {
                                if (idxOfFirstNotNull == -1) idxOfFirstNotNull = propIdx;
                            }
                        }

                        DRect encapsulatingRect1 = new DRect();
                        DRect encapsulatingRect2 = new DRect();
                        if (idxOfFirstNotNull != -1)
                        {
                            // only in here if all properties use the same tiling so don't need to worry about which propIdx we are dealing with
                            //Get the rect that encapsulates all material and UV tiling for materials and meshes in tx1
                            encapsulatingRect1 = tx1.matsAndGOs.mats[0].samplingRectMatAndUVTiling;
                            for (int matIdx = 1; matIdx < tx1.matsAndGOs.mats.Count; matIdx++)
                            {
                                DRect tmpSsamplingRectMatAndUVTilingTx1 = tx1.matsAndGOs.mats[matIdx].samplingRectMatAndUVTiling;
                                encapsulatingRect1 = MB3_UVTransformUtility.GetEncapsulatingRectShifted(ref encapsulatingRect1, ref tmpSsamplingRectMatAndUVTilingTx1);
                            }

                            //same for tx2
                            encapsulatingRect2 = tx2.matsAndGOs.mats[0].samplingRectMatAndUVTiling;
                            for (int matIdx = 1; matIdx < tx2.matsAndGOs.mats.Count; matIdx++)
                            {
                                DRect tmpSsamplingRectMatAndUVTilingTx2 = tx2.matsAndGOs.mats[matIdx].samplingRectMatAndUVTiling;
                                encapsulatingRect2 = MB3_UVTransformUtility.GetEncapsulatingRectShifted(ref encapsulatingRect2, ref tmpSsamplingRectMatAndUVTilingTx2);
                            }

                            encapsulatingRectMerged = MB3_UVTransformUtility.GetEncapsulatingRectShifted(ref encapsulatingRect1, ref encapsulatingRect2);
                            accumulatedAreaCombined += encapsulatingRectMerged.width * encapsulatingRectMerged.height;
                            accumulatedAreaNotCombined += encapsulatingRect1.width * encapsulatingRect1.height + encapsulatingRect2.width * encapsulatingRect2.height;
                        }
                        else
                        {
                            encapsulatingRectMerged = new DRect(0f, 0f, 1f, 1f);
                        }

                        //the distinct material textures may overlap.
                        //if the area of these rectangles combined is less than the sum of these areas of these rectangles then merge these distinctMaterialTextures
                        if (accumulatedAreaCombined < accumulatedAreaNotCombined)
                        {
                            // merge tx2 into tx1
                            numMerged++;
                            StringBuilder sb = null;
                            if (LOG_LEVEL >= MB2_LogLevel.info)
                            {
                                sb = new StringBuilder();
                                sb.AppendFormat("About To Merge:\n   TextureSet1 {0}\n   TextureSet2 {1}\n", tx1.GetDescription(), tx2.GetDescription());
                                if (LOG_LEVEL >= MB2_LogLevel.trace)
                                {
                                    for (int matIdx = 0; matIdx < tx1.matsAndGOs.mats.Count; matIdx++)
                                    {
                                        sb.AppendFormat("tx1 Mat {0} matAndMeshUVRect {1} fullSamplingRect {2}\n",
                                            tx1.matsAndGOs.mats[matIdx].mat, tx1.matsAndGOs.mats[matIdx].samplingRectMatAndUVTiling, tx1.ts[0].GetEncapsulatingSamplingRect());
                                    }
                                    for (int matIdx = 0; matIdx < tx2.matsAndGOs.mats.Count; matIdx++)
                                    {
                                        sb.AppendFormat("tx2 Mat {0} matAndMeshUVRect {1} fullSamplingRect {2}\n",
                                            tx2.matsAndGOs.mats[matIdx].mat, tx2.matsAndGOs.mats[matIdx].samplingRectMatAndUVTiling, tx2.ts[0].GetEncapsulatingSamplingRect());
                                    }
                                }
                            }

                            //copy game objects over
                            for (int k = 0; k < tx2.matsAndGOs.gos.Count; k++)
                            {
                                if (!tx1.matsAndGOs.gos.Contains(tx2.matsAndGOs.gos[k]))
                                {
                                    tx1.matsAndGOs.gos.Add(tx2.matsAndGOs.gos[k]);
                                }
                            }

                            //copy materials over from tx2 to tx1
                            for (int matIdx = 0; matIdx < tx2.matsAndGOs.mats.Count; matIdx++)
                            {
                                tx1.matsAndGOs.mats.Add(tx2.matsAndGOs.mats[matIdx]);
                            }

                            tx1.SetEncapsulatingSamplingRectWhenMergingTexSets(encapsulatingRectMerged);
                            if (!MarkedForDeletion.Contains(i))
                            {
                                MarkedForDeletion.Add(i);
                            }

                            if (LOG_LEVEL >= MB2_LogLevel.debug)
                            {
                                if (LOG_LEVEL >= MB2_LogLevel.trace)
                                {
                                    sb.AppendFormat("=== After Merge TextureSet {0}\n", tx1.GetDescription());
                                    for (int matIdx = 0; matIdx < tx1.matsAndGOs.mats.Count; matIdx++)
                                    {
                                        sb.AppendFormat("tx1 Mat {0} matAndMeshUVRect {1} fullSamplingRect {2}\n",
                                            tx1.matsAndGOs.mats[matIdx].mat, tx1.matsAndGOs.mats[matIdx].samplingRectMatAndUVTiling, tx1.ts[0].GetEncapsulatingSamplingRect());
                                    }
                                    //Integrity check that sampling rects fit into enapsulating rects
                                    if (DO_INTEGRITY_CHECKS)
                                    {
                                        if (DO_INTEGRITY_CHECKS) { DoIntegrityCheckMergedEncapsulatingSamplingRects(distinctMaterialTextures); }
                                    }
                                }
                                Debug.Log(sb.ToString());
                            }
                            break;
                        }
                        else
                        {
                            if (LOG_LEVEL >= MB2_LogLevel.debug)
                            {
                                Debug.Log(string.Format("Considered merging {0} and {1} but there was not enough overlap. It is more efficient to bake these to separate rectangles.",
                                    tx1.GetDescription() + encapsulatingRect1,
                                    tx2.GetDescription() + encapsulatingRect2));
                            }
                        }
                    }
                }
            }

            //remove distinctMaterialTextures that were merged
            for (int j = MarkedForDeletion.Count - 1; j >= 0; j--)
            {
                distinctMaterialTextures.RemoveAt(MarkedForDeletion[j]);
            }
            MarkedForDeletion.Clear();
            if (LOG_LEVEL >= MB2_LogLevel.debug)
            {
                Debug.Log(string.Format("MergeOverlappingDistinctMaterialTexturesAndCalcMaterialSubrects complete merged {0} now have {1}", numMerged, distinctMaterialTextures.Count));
            }
            if (DO_INTEGRITY_CHECKS) { DoIntegrityCheckMergedEncapsulatingSamplingRects(distinctMaterialTextures); }
        }


        // This should only be called after regular merge so that rects have been correctly setup.
        public void MergeDistinctMaterialTexturesThatWouldExceedMaxAtlasSizeAndCalcMaterialSubrects(List<MB_TexSet> distinctMaterialTextures, int maxAtlasSize)
        {
            if (LOG_LEVEL >= MB2_LogLevel.debug)
            {
                Debug.Log("MergeDistinctMaterialTexturesThatWouldExceedMaxAtlasSizeAndCalcMaterialSubrects num atlas rects" + distinctMaterialTextures.Count);
            }

            Debug.Assert(_HasBeenInitialized, "MergeOverlappingDistinctMaterialTexturesAndCalcMaterialSubrects must be called before MergeDistinctMaterialTexturesThatWouldExceedMaxAtlasSizeAndCalcMaterialSubrects");

            int numMerged = 0;

            List<int> MarkedForDeletion = new List<int>();
            for (int i = 0; i < distinctMaterialTextures.Count; i++)
            {
                MB_TexSet tx2 = distinctMaterialTextures[i];
                for (int j = i + 1; j < distinctMaterialTextures.Count; j++)
                {
                    MB_TexSet tx1 = distinctMaterialTextures[j];
                    if (tx1.AllTexturesAreSameForMerge(tx2, _considerNonTextureProperties, resultMaterialTextureBlender))
                    {
                        //Check if the size of the rect in the atlas would be greater than max atlas size.

                        DRect encapsulatingRectMerged = new DRect();
                        int idxOfFirstNotNull = -1;
                        for (int propIdx = 0; propIdx < tx2.ts.Length; propIdx++)
                        {
                            if (!tx2.ts[propIdx].isNull)
                            {
                                if (idxOfFirstNotNull == -1) idxOfFirstNotNull = propIdx;
                            }
                        }

                        DRect encapsulatingRect1 = new DRect();
                        DRect encapsulatingRect2 = new DRect();
                        if (idxOfFirstNotNull != -1)
                        {
                            // only in here if all properties use the same tiling so don't need to worry about which propIdx we are dealing with
                            //Get the rect that encapsulates all material and UV tiling for materials and meshes in tx1
                            encapsulatingRect1 = tx1.matsAndGOs.mats[0].samplingRectMatAndUVTiling;
                            for (int matIdx = 1; matIdx < tx1.matsAndGOs.mats.Count; matIdx++)
                            {
                                DRect tmpSsamplingRectMatAndUVTilingTx1 = tx1.matsAndGOs.mats[matIdx].samplingRectMatAndUVTiling;
                                encapsulatingRect1 = MB3_UVTransformUtility.GetEncapsulatingRectShifted(ref encapsulatingRect1, ref tmpSsamplingRectMatAndUVTilingTx1);
                            }

                            //same for tx2
                            encapsulatingRect2 = tx2.matsAndGOs.mats[0].samplingRectMatAndUVTiling;
                            for (int matIdx = 1; matIdx < tx2.matsAndGOs.mats.Count; matIdx++)
                            {
                                DRect tmpSsamplingRectMatAndUVTilingTx2 = tx2.matsAndGOs.mats[matIdx].samplingRectMatAndUVTiling;
                                encapsulatingRect2 = MB3_UVTransformUtility.GetEncapsulatingRectShifted(ref encapsulatingRect2, ref tmpSsamplingRectMatAndUVTilingTx2);
                            }

                            encapsulatingRectMerged = MB3_UVTransformUtility.GetEncapsulatingRectShifted(ref encapsulatingRect1, ref encapsulatingRect2);
                        }
                        else
                        {
                            encapsulatingRectMerged = new DRect(0f, 0f, 1f, 1f);
                        }

                        Vector2 maxHeightWidth = tx1.GetMaxRawTextureHeightWidth();
                        if (encapsulatingRectMerged.width * maxHeightWidth.x > maxAtlasSize ||
                            encapsulatingRectMerged.height * maxHeightWidth.y > maxAtlasSize)
                        {
                            // merge tx2 into tx1
                            numMerged++;
                            StringBuilder sb = null;
                            if (LOG_LEVEL >= MB2_LogLevel.info)
                            {
                                sb = new StringBuilder();
                                sb.AppendFormat("About To Merge:\n   TextureSet1 {0}\n   TextureSet2 {1}\n", tx1.GetDescription(), tx2.GetDescription());
                                if (LOG_LEVEL >= MB2_LogLevel.trace)
                                {
                                    for (int matIdx = 0; matIdx < tx1.matsAndGOs.mats.Count; matIdx++)
                                    {
                                        sb.AppendFormat("tx1 Mat {0} matAndMeshUVRect {1} fullSamplingRect {2}\n",
                                            tx1.matsAndGOs.mats[matIdx].mat, tx1.matsAndGOs.mats[matIdx].samplingRectMatAndUVTiling, tx1.ts[0].GetEncapsulatingSamplingRect());
                                    }
                                    for (int matIdx = 0; matIdx < tx2.matsAndGOs.mats.Count; matIdx++)
                                    {
                                        sb.AppendFormat("tx2 Mat {0} matAndMeshUVRect {1} fullSamplingRect {2}\n",
                                            tx2.matsAndGOs.mats[matIdx].mat, tx2.matsAndGOs.mats[matIdx].samplingRectMatAndUVTiling, tx2.ts[0].GetEncapsulatingSamplingRect());
                                    }
                                }
                            }

                            //copy game objects over
                            for (int k = 0; k < tx2.matsAndGOs.gos.Count; k++)
                            {
                                if (!tx1.matsAndGOs.gos.Contains(tx2.matsAndGOs.gos[k]))
                                {
                                    tx1.matsAndGOs.gos.Add(tx2.matsAndGOs.gos[k]);
                                }
                            }

                            //copy materials over from tx2 to tx1
                            for (int matIdx = 0; matIdx < tx2.matsAndGOs.mats.Count; matIdx++)
                            {
                                tx1.matsAndGOs.mats.Add(tx2.matsAndGOs.mats[matIdx]);
                            }

                            tx1.SetEncapsulatingSamplingRectWhenMergingTexSets(encapsulatingRectMerged);
                            if (!MarkedForDeletion.Contains(i))
                            {
                                MarkedForDeletion.Add(i);
                            }

                            if (LOG_LEVEL >= MB2_LogLevel.debug)
                            {
                                if (LOG_LEVEL >= MB2_LogLevel.trace)
                                {
                                    sb.AppendFormat("=== After Merge TextureSet {0}\n", tx1.GetDescription());
                                    for (int matIdx = 0; matIdx < tx1.matsAndGOs.mats.Count; matIdx++)
                                    {
                                        sb.AppendFormat("tx1 Mat {0} matAndMeshUVRect {1} fullSamplingRect {2}\n",
                                            tx1.matsAndGOs.mats[matIdx].mat, tx1.matsAndGOs.mats[matIdx].samplingRectMatAndUVTiling, tx1.ts[0].GetEncapsulatingSamplingRect());
                                    }
                                    //Integrity check that sampling rects fit into enapsulating rects
                                    if (DO_INTEGRITY_CHECKS)
                                    {
                                        if (DO_INTEGRITY_CHECKS) { DoIntegrityCheckMergedEncapsulatingSamplingRects(distinctMaterialTextures); }
                                    }
                                }
                                Debug.Log(sb.ToString());
                            }
                            break;
                        }
                        else
                        {
                            if (LOG_LEVEL >= MB2_LogLevel.debug)
                            {
                                Debug.Log(string.Format("Considered merging {0} and {1} but there was not enough overlap. It is more efficient to bake these to separate rectangles.",
                                    tx1.GetDescription() + encapsulatingRect1,
                                    tx2.GetDescription() + encapsulatingRect2));
                            }
                        }
                    }
                }
            }

            //remove distinctMaterialTextures that were merged
            for (int j = MarkedForDeletion.Count - 1; j >= 0; j--)
            {
                distinctMaterialTextures.RemoveAt(MarkedForDeletion[j]);
            }
            MarkedForDeletion.Clear();
            if (LOG_LEVEL >= MB2_LogLevel.debug)
            {
                Debug.Log(string.Format("MergeDistinctMaterialTexturesThatWouldExceedMaxAtlasSizeAndCalcMaterialSubrects complete merged {0} now have {1}", numMerged, distinctMaterialTextures.Count));
            }
            if (DO_INTEGRITY_CHECKS) { DoIntegrityCheckMergedEncapsulatingSamplingRects(distinctMaterialTextures); }
        }

        public void DoIntegrityCheckMergedEncapsulatingSamplingRects(List<MB_TexSet> distinctMaterialTextures)
        {
            if (DO_INTEGRITY_CHECKS)
            {
                for (int i = 0; i < distinctMaterialTextures.Count; i++)
                {
                    MB_TexSet tx1 = distinctMaterialTextures[i];
                    if (!tx1.allTexturesUseSameMatTiling)
                    {
                        continue;
                    }
                    for (int matIdx = 0; matIdx < tx1.matsAndGOs.mats.Count; matIdx++)
                    {
                        MatAndTransformToMerged mat = tx1.matsAndGOs.mats[matIdx];
                        DRect uvR = mat.obUVRectIfTilingSame;
                        DRect matR = mat.materialTiling;
                        if (!MB2_TextureBakeResults.IsMeshAndMaterialRectEnclosedByAtlasRect(tx1.tilingTreatment, uvR.GetRect(), matR.GetRect(), tx1.ts[0].GetEncapsulatingSamplingRect().GetRect(),MB2_LogLevel.info))
                        {
                            Debug.LogErrorFormat("mesh " + tx1.matsAndGOs.mats[matIdx].objName + "\n" +
                                                " uv=" + uvR + "\n" +
                                                " mat=" + matR.GetRect().ToString("f5") + "\n" +
                                                " samplingRect=" + tx1.matsAndGOs.mats[matIdx].samplingRectMatAndUVTiling.GetRect().ToString("f4") + "\n" +
                                                " encapsulatingRect " + tx1.ts[0].GetEncapsulatingSamplingRect().GetRect().ToString("f4") + "\n");
                            Debug.LogErrorFormat(string.Format("Integrity check failed. " + tx1.matsAndGOs.mats[matIdx].objName + " Encapsulating sampling rect failed to contain potentialRect\n"));
                            MB2_TextureBakeResults.IsMeshAndMaterialRectEnclosedByAtlasRect(tx1.tilingTreatment, uvR.GetRect(), matR.GetRect(), tx1.ts[0].GetEncapsulatingSamplingRect().GetRect(), MB2_LogLevel.trace);
                            Debug.Assert(false);
                        }
                    }
                }
            }
        }
    }
}
