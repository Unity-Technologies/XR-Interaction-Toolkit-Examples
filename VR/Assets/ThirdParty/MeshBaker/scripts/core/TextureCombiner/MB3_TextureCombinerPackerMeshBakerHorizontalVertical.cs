using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DigitalOpus.MB.Core
{
    /*
        TODO test
    */
    internal class MB3_TextureCombinerPackerMeshBakerHorizontalVertical : MB3_TextureCombinerPackerMeshBaker
    {
        private interface IPipeline
        {
            MB2_PackingAlgorithmEnum GetPackingAlg();
            void SortTexSetIntoBins(MB_TexSet texSet, List<MB_TexSet> horizontalVert, List<MB_TexSet> regular, int maxAtlasWidth, int maxAtlasHeight);
            MB_TextureTilingTreatment GetEdge2EdgeTreatment();
            void InitializeAtlasPadding(ref AtlasPadding padding, int paddingValue);
            void MergeAtlasPackingResultStackBonAInternal(AtlasPackingResult a, AtlasPackingResult b, out Rect AatlasToFinal, out Rect BatlasToFinal, bool stretchBToAtlasWidth, int maxWidthDim, int maxHeightDim, out int atlasX, out int atlasY);
            void GetExtraRoomForRegularAtlas(int usedHorizontalVertWidth, int usedHorizontalVertHeight, int maxAtlasWidth, int maxAtlasHeight, out int atlasRegularMaxWidth, out int atlasRegularMaxHeight);
        }

        private class VerticalPipeline : IPipeline
        {
            public MB2_PackingAlgorithmEnum GetPackingAlg()
            {
                return MB2_PackingAlgorithmEnum.MeshBakerTexturePacker_Vertical;
            }

            public void SortTexSetIntoBins(MB_TexSet texSet, List<MB_TexSet> horizontalVert, List<MB_TexSet> regular, int maxAtlasWidth, int maxAtlasHeight)
            {
                if (texSet.idealHeight >= maxAtlasHeight &&
                    texSet.ts[0].GetEncapsulatingSamplingRect().height >= 1f)
                {
                    horizontalVert.Add(texSet);
                }
                else
                {
                    regular.Add(texSet);
                }
            }

            public MB_TextureTilingTreatment GetEdge2EdgeTreatment()
            {
                return MB_TextureTilingTreatment.edgeToEdgeY;
            }

            public void InitializeAtlasPadding(ref AtlasPadding padding, int paddingValue)
            {
                padding.topBottom = 0;
                padding.leftRight = paddingValue;
            }

            public void MergeAtlasPackingResultStackBonAInternal(AtlasPackingResult a, AtlasPackingResult b, out Rect AatlasToFinal, out Rect BatlasToFinal, bool stretchBToAtlasWidth, int maxWidthDim, int maxHeightDim, out int atlasX, out int atlasY)
            {
                // first calc width scale and offset
                float finalW = a.usedW + b.usedW;
                float scaleXa, scaleXb;
                if (finalW > maxWidthDim)
                {
                    scaleXa = maxWidthDim / finalW; //0,1
                    float offsetBx = ((float)Mathf.FloorToInt(a.usedW * scaleXa)) / maxWidthDim;//0,1
                    scaleXa = offsetBx;
                    scaleXb = (1f - offsetBx);
                    AatlasToFinal = new Rect(0, 0, scaleXa, 1);
                    BatlasToFinal = new Rect(offsetBx, 0, scaleXb, 1);
                }
                else
                {
                    float offsetBx = a.usedW / finalW;
                    AatlasToFinal = new Rect(0, 0, offsetBx, 1);
                    BatlasToFinal = new Rect(offsetBx, 0, b.usedW / finalW, 1);
                }

                //next calc width scale and offset
                if (a.atlasX > b.atlasX)
                {
                    if (!stretchBToAtlasWidth)
                    {
                        // b rects will be placed in a larger atlas which will make them smaller
                        BatlasToFinal.width = ((float)b.atlasX) / a.atlasX;
                    }
                }
                else if (b.atlasX > a.atlasX)
                {
                    // a rects will be placed in a larger atlas which will make them smaller
                    AatlasToFinal.width = ((float)a.atlasX) / b.atlasX;
                }

                atlasX = a.usedW + b.usedW;
                atlasY = Mathf.Max(a.usedH, b.usedH);
            }

            public void GetExtraRoomForRegularAtlas(int usedHorizontalVertWidth, int usedHorizontalVertHeight, int maxAtlasWidth, int maxAtlasHeight, out int atlasRegularMaxWidth, out int atlasRegularMaxHeight)
            {
                atlasRegularMaxWidth = maxAtlasWidth - usedHorizontalVertWidth;
                atlasRegularMaxHeight = maxAtlasHeight;
            }
        }

        private class HorizontalPipeline : IPipeline
        {
            public MB2_PackingAlgorithmEnum GetPackingAlg()
            {
                return MB2_PackingAlgorithmEnum.MeshBakerTexturePacker_Horizontal;
            }

            public void SortTexSetIntoBins(MB_TexSet texSet, List<MB_TexSet> horizontalVert, List<MB_TexSet> regular, int maxAtlasWidth, int maxAtlasHeight)
            {
                if (texSet.idealWidth >= maxAtlasWidth &&
                    texSet.ts[0].GetEncapsulatingSamplingRect().width >= 1f)
                {
                    horizontalVert.Add(texSet);
                }
                else
                {
                    regular.Add(texSet);
                }
            }

            public MB_TextureTilingTreatment GetEdge2EdgeTreatment()
            {
                return MB_TextureTilingTreatment.edgeToEdgeX;
            }

            public void InitializeAtlasPadding(ref AtlasPadding padding, int paddingValue)
            {
                padding.topBottom = paddingValue;
                padding.leftRight = 0;
            }

            public void MergeAtlasPackingResultStackBonAInternal(AtlasPackingResult a, AtlasPackingResult b, out Rect AatlasToFinal, out Rect BatlasToFinal, bool stretchBToAtlasWidth, int maxWidthDim, int maxHeightDim, out int atlasX, out int atlasY)
            {
                float finalH = a.usedH + b.usedH;
                float scaleYa, scaleYb;
                if (finalH > maxHeightDim)
                {
                    scaleYa = maxHeightDim / finalH; //0,1
                    float offsetBy = ((float)Mathf.FloorToInt(a.usedH * scaleYa)) / maxHeightDim;//0,1
                    scaleYa = offsetBy;
                    scaleYb = (1f - offsetBy);
                    AatlasToFinal = new Rect(0, 0, 1, scaleYa);
                    BatlasToFinal = new Rect(0, offsetBy, 1, scaleYb);
                }
                else
                {
                    float offsetBy = a.usedH / finalH;
                    AatlasToFinal = new Rect(0, 0, 1, offsetBy);
                    BatlasToFinal = new Rect(0, offsetBy, 1, b.usedH / finalH);
                }

                //next calc width scale and offset
                if (a.atlasX > b.atlasX)
                {
                    if (!stretchBToAtlasWidth)
                    {
                        // b rects will be placed in a larger atlas which will make them smaller
                        BatlasToFinal.width = ((float)b.atlasX) / a.atlasX;
                    }
                }
                else if (b.atlasX > a.atlasX)
                {
                    // a rects will be placed in a larger atlas which will make them smaller
                    AatlasToFinal.width = ((float)a.atlasX) / b.atlasX;
                }

                atlasX = Mathf.Max(a.usedW, b.usedW);
                atlasY = a.usedH + b.usedH;
            }

            public void GetExtraRoomForRegularAtlas(int usedHorizontalVertWidth, int usedHorizontalVertHeight, int maxAtlasWidth, int maxAtlasHeight, out int atlasRegularMaxWidth, out int atlasRegularMaxHeight)
            {
                atlasRegularMaxWidth = maxAtlasWidth;
                atlasRegularMaxHeight = maxAtlasHeight - usedHorizontalVertHeight;
            }
        }

        public enum AtlasDirection
        {
            horizontal,
            vertical
        }

        private AtlasDirection _atlasDirection = AtlasDirection.horizontal;

        public MB3_TextureCombinerPackerMeshBakerHorizontalVertical(AtlasDirection ad)
        {
            _atlasDirection = ad;
        }

        public override AtlasPackingResult[] CalculateAtlasRectangles(MB3_TextureCombinerPipeline.TexturePipelineData data, bool doMultiAtlas, MB2_LogLevel LOG_LEVEL)
        {
            Debug.Assert(!data.OnlyOneTextureInAtlasReuseTextures());
            Debug.Assert(data._packingAlgorithm != MB2_PackingAlgorithmEnum.UnitysPackTextures, "Unity texture packer cannot be used");

            IPipeline pipeline;
            if (_atlasDirection == AtlasDirection.horizontal)
            {
                pipeline = new HorizontalPipeline();
            } else
            {
                pipeline = new VerticalPipeline();
            }

            //int maxAtlasWidth = data._maxAtlasWidth;
            //int maxAtlasHeight = data._maxAtlasHeight;
            if (_atlasDirection == AtlasDirection.horizontal)
            {
                if (!data._useMaxAtlasWidthOverride)
                {
                    // need to get the width of the atlas without mesh uvs considered
                    int maxWidth = 2;
                    for (int i = 0; i < data.distinctMaterialTextures.Count; i++)
                    {
                        MB_TexSet ts = data.distinctMaterialTextures[i];
                        int w;
                        if (data._fixOutOfBoundsUVs)
                        {
                            Vector2 rawHeightWidth = ts.GetMaxRawTextureHeightWidth();
                            w = (int)rawHeightWidth.x;
                        }
                        else
                        {
                            w = ts.idealWidth;
                        }
                        if (ts.idealWidth > maxWidth) maxWidth = w;
                    }
                    if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Calculated max atlas width: " + maxWidth);
                    data._maxAtlasWidth = maxWidth;
                }
            } else
            {
                if (!data._useMaxAtlasHeightOverride)
                {
                    int maxHeight = 2;
                    for (int i = 0; i < data.distinctMaterialTextures.Count; i++)
                    {
                        MB_TexSet ts = data.distinctMaterialTextures[i];
                        int h;
                        if (data._fixOutOfBoundsUVs)
                        {
                            Vector2 rawHeightWidth = ts.GetMaxRawTextureHeightWidth();
                            h = (int) rawHeightWidth.y;
                        } else
                        {
                            h = ts.idealHeight;
                        }
                        if (ts.idealHeight > maxHeight) maxHeight = h;
                    }
                    if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Calculated max atlas height: " + maxHeight);
                    data._maxAtlasHeight = maxHeight;
                }
            }

            //split the list of distinctMaterialTextures into two bins
            List<MB_TexSet> horizontalVerticalDistinctMaterialTextures = new List<MB_TexSet>();
            List<MB_TexSet> regularTextures = new List<MB_TexSet>();
            for (int i = 0; i < data.distinctMaterialTextures.Count; i++)
            {
                pipeline.SortTexSetIntoBins(data.distinctMaterialTextures[i], horizontalVerticalDistinctMaterialTextures, regularTextures, data._maxAtlasWidth, data._maxAtlasHeight);
            }

            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log(String.Format("Splitting list of distinctMaterialTextures numHorizontalVertical={0} numRegular={1} maxAtlasWidth={2} maxAtlasHeight={3}", horizontalVerticalDistinctMaterialTextures.Count, regularTextures.Count, data._maxAtlasWidth, data._maxAtlasHeight));

            //pack one bin with the horizontal vertical texture packer.
            MB2_TexturePacker tp;
            MB2_PackingAlgorithmEnum packingAlgorithm;
            AtlasPackingResult[] packerRectsHorizontalVertical;
            if (horizontalVerticalDistinctMaterialTextures.Count > 0)
            {
                packingAlgorithm = pipeline.GetPackingAlg();
                List<Vector2> imageSizesHorizontalVertical = new List<Vector2>();
                for (int i = 0; i < horizontalVerticalDistinctMaterialTextures.Count; i++)
                {
                    horizontalVerticalDistinctMaterialTextures[i].SetTilingTreatmentAndAdjustEncapsulatingSamplingRect(pipeline.GetEdge2EdgeTreatment());
                    imageSizesHorizontalVertical.Add(new Vector2(horizontalVerticalDistinctMaterialTextures[i].idealWidth, horizontalVerticalDistinctMaterialTextures[i].idealHeight));
                }

                tp = MB3_TextureCombinerPipeline.CreateTexturePacker(packingAlgorithm);
                tp.atlasMustBePowerOfTwo = false;
                List<AtlasPadding> paddingsHorizontalVertical = new List<AtlasPadding>();
                for (int i = 0; i < imageSizesHorizontalVertical.Count; i++)
                {
                    AtlasPadding padding = new AtlasPadding();
                    pipeline.InitializeAtlasPadding(ref padding, data._atlasPadding);
                    paddingsHorizontalVertical.Add(padding);
                }

                tp.LOG_LEVEL = MB2_LogLevel.trace;
                packerRectsHorizontalVertical = tp.GetRects(imageSizesHorizontalVertical, paddingsHorizontalVertical, data._maxAtlasWidth, data._maxAtlasHeight, false);
                if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(String.Format("Packed {0} textures with edgeToEdge tiling into an atlas of size {1} by {2} usedW {3} usedH {4}", horizontalVerticalDistinctMaterialTextures.Count, packerRectsHorizontalVertical[0].atlasX, packerRectsHorizontalVertical[0].atlasY, packerRectsHorizontalVertical[0].usedW, packerRectsHorizontalVertical[0].usedH));
            }
            else
            {
                packerRectsHorizontalVertical = new AtlasPackingResult[0];
            }

            //pack other bin with regular texture packer
            AtlasPackingResult[] packerRectsRegular;
            if (regularTextures.Count > 0)
            {
                packingAlgorithm = MB2_PackingAlgorithmEnum.MeshBakerTexturePacker;
                List<Vector2> imageSizesRegular = new List<Vector2>();
                for (int i = 0; i < regularTextures.Count; i++)
                {
                    imageSizesRegular.Add(new Vector2(regularTextures[i].idealWidth, regularTextures[i].idealHeight));
                }

                tp = MB3_TextureCombinerPipeline.CreateTexturePacker(MB2_PackingAlgorithmEnum.MeshBakerTexturePacker);
                tp.atlasMustBePowerOfTwo = false;
                List<AtlasPadding> paddingsRegular = new List<AtlasPadding>();
                for (int i = 0; i < imageSizesRegular.Count; i++)
                {
                    AtlasPadding padding = new AtlasPadding();
                    padding.topBottom = data._atlasPadding;
                    padding.leftRight = data._atlasPadding;
                    paddingsRegular.Add(padding);
                }
 
                int atlasRegularMaxWidth, atlasRegularMaxHeight;
                int usedHorizontalVertWidth = 0, usedHorizontalVertHeight = 0;
                if (packerRectsHorizontalVertical.Length > 0)
                {
                    usedHorizontalVertHeight = packerRectsHorizontalVertical[0].atlasY;
                    usedHorizontalVertWidth = packerRectsHorizontalVertical[0].atlasX;
                }
                pipeline.GetExtraRoomForRegularAtlas(usedHorizontalVertWidth, usedHorizontalVertHeight, data._maxAtlasWidth, data._maxAtlasHeight, out atlasRegularMaxWidth, out atlasRegularMaxHeight);
                packerRectsRegular = tp.GetRects(imageSizesRegular, paddingsRegular, atlasRegularMaxWidth, atlasRegularMaxHeight, false);
                if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log(String.Format("Packed {0} textures without edgeToEdge tiling into an atlas of size {1} by {2} usedW {3} usedH {4}", regularTextures.Count, packerRectsRegular[0].atlasX, packerRectsRegular[0].atlasY, packerRectsRegular[0].usedW, packerRectsRegular[0].usedH));
            }
            else
            {
                packerRectsRegular = new AtlasPackingResult[0];
            }
            

            AtlasPackingResult result = null;
            if (packerRectsHorizontalVertical.Length == 0 && packerRectsRegular.Length == 0)
            {
                Debug.Assert(false, "Should never have reached this.");
                return null;
            }
            else if (packerRectsHorizontalVertical.Length > 0 && packerRectsRegular.Length > 0)
            {
                result = MergeAtlasPackingResultStackBonA(packerRectsHorizontalVertical[0], packerRectsRegular[0], data._maxAtlasWidth, data._maxAtlasHeight, true, pipeline);
            }
            else if (packerRectsHorizontalVertical.Length > 0)
            {
                result = packerRectsHorizontalVertical[0];
            }
            else if (packerRectsRegular.Length > 0)
            {
                result = packerRectsRegular[0];
            }

            Debug.Assert(data.distinctMaterialTextures.Count == result.rects.Length);

            //We re-ordered the distinctMaterial textures so replace the list with the new reordered one
            horizontalVerticalDistinctMaterialTextures.AddRange(regularTextures);
            data.distinctMaterialTextures = horizontalVerticalDistinctMaterialTextures;
            AtlasPackingResult[] results;
            if (result != null) results = new AtlasPackingResult[] { result };
            else results = new AtlasPackingResult[0]; 
            return results;
        }

        public static AtlasPackingResult TestStackRectanglesHorizontal(AtlasPackingResult a,
            AtlasPackingResult b, int maxHeightDim, int maxWidthDim, bool stretchBToAtlasWidth)
        {
            return MergeAtlasPackingResultStackBonA(a, b, maxWidthDim, maxHeightDim, stretchBToAtlasWidth, new HorizontalPipeline());
        }

        public static AtlasPackingResult TestStackRectanglesVertical(AtlasPackingResult a,
            AtlasPackingResult b, int maxHeightDim, int maxWidthDim, bool stretchBToAtlasWidth)
        {
            return MergeAtlasPackingResultStackBonA(a, b, maxWidthDim, maxHeightDim, stretchBToAtlasWidth, new VerticalPipeline());
        }

        private static AtlasPackingResult MergeAtlasPackingResultStackBonA(AtlasPackingResult a,
            AtlasPackingResult b, int maxWidthDim, int maxHeightDim, bool stretchBToAtlasWidth, IPipeline pipeline)
        {
            Debug.Assert(a.usedW == a.atlasX);
            Debug.Assert(a.usedH == a.atlasY);
            Debug.Assert(b.usedW == b.atlasX);
            Debug.Assert(b.usedH == b.atlasY);
            Debug.Assert(a.usedW <= maxWidthDim, a.usedW + " " + maxWidthDim);
            Debug.Assert(a.usedH <= maxHeightDim, a.usedH + " " + maxHeightDim);
            Debug.Assert(b.usedH <= maxHeightDim);
            Debug.Assert(b.usedW <= maxWidthDim, b.usedW + " " + maxWidthDim);

            Rect AatlasToFinal;
            Rect BatlasToFinal;

            // first calc height scale and offset
            int atlasX;
            int atlasY;
            pipeline.MergeAtlasPackingResultStackBonAInternal(a, b, out AatlasToFinal, out BatlasToFinal, stretchBToAtlasWidth, maxWidthDim, maxHeightDim, out atlasX, out atlasY);

            Rect[] newRects = new Rect[a.rects.Length + b.rects.Length];
            AtlasPadding[] paddings = new AtlasPadding[a.rects.Length + b.rects.Length];
            int[] srcImgIdxs = new int[a.rects.Length + b.rects.Length];
            Array.Copy(a.padding, paddings, a.padding.Length);
            Array.Copy(b.padding, 0, paddings, a.padding.Length, b.padding.Length);
            Array.Copy(a.srcImgIdxs, srcImgIdxs, a.srcImgIdxs.Length);
            Array.Copy(b.srcImgIdxs, 0, srcImgIdxs, a.srcImgIdxs.Length, b.srcImgIdxs.Length);
            Array.Copy(a.rects, newRects, a.rects.Length);
            for (int i = 0; i < a.rects.Length; i++)
            {
                Rect r = a.rects[i];
                r.x = AatlasToFinal.x + r.x * AatlasToFinal.width;
                r.y = AatlasToFinal.y + r.y * AatlasToFinal.height;
                r.width *= AatlasToFinal.width;
                r.height *= AatlasToFinal.height;
                Debug.Assert(r.max.x <= 1f);
                Debug.Assert(r.max.y <= 1f);
                Debug.Assert(r.min.x >= 0f);
                Debug.Assert(r.min.y >= 0f);
                newRects[i] = r;
                srcImgIdxs[i] = a.srcImgIdxs[i];
            }

            for (int i = 0; i < b.rects.Length; i++)
            {
                Rect r = b.rects[i];
                r.x = BatlasToFinal.x + r.x * BatlasToFinal.width;
                r.y = BatlasToFinal.y + r.y * BatlasToFinal.height;
                r.width *= BatlasToFinal.width;
                r.height *= BatlasToFinal.height;
                Debug.Assert(r.max.x <= 1f);
                Debug.Assert(r.max.y <= 1f);
                Debug.Assert(r.min.x >= 0f);
                Debug.Assert(r.min.y >= 0f);
                newRects[a.rects.Length + i] = r;
                srcImgIdxs[a.rects.Length + i] = b.srcImgIdxs[i];
            }

            AtlasPackingResult res = new AtlasPackingResult(paddings);
            res.atlasX = atlasX;
            res.atlasY = atlasY;
            res.padding = paddings;
            res.rects = newRects;
            res.srcImgIdxs = srcImgIdxs;
            res.CalcUsedWidthAndHeight();
            return res;
        }
    }
}
