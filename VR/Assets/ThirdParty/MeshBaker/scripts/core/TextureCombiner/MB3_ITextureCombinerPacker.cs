using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DigitalOpus.MB.Core
{
    internal interface MB_ITextureCombinerPacker
    {
        bool Validate(MB3_TextureCombinerPipeline.TexturePipelineData data);

        IEnumerator ConvertTexturesToReadableFormats(ProgressUpdateDelegate progressInfo,
            MB3_TextureCombiner.CombineTexturesIntoAtlasesCoroutineResult result,
            MB3_TextureCombinerPipeline.TexturePipelineData data,
            MB3_TextureCombiner combiner,
            MB2_EditorMethodsInterface textureEditorMethods,
            MB2_LogLevel LOG_LEVEL);

        AtlasPackingResult[] CalculateAtlasRectangles(MB3_TextureCombinerPipeline.TexturePipelineData data, bool doMultiAtlas, MB2_LogLevel LOG_LEVEL);

        IEnumerator CreateAtlases(ProgressUpdateDelegate progressInfo,
            MB3_TextureCombinerPipeline.TexturePipelineData data, MB3_TextureCombiner combiner,
            AtlasPackingResult packedAtlasRects,
            Texture2D[] atlases, MB2_EditorMethodsInterface textureEditorMethods,
            MB2_LogLevel LOG_LEVEL);
    }

    internal abstract class MB3_TextureCombinerPackerRoot : MB_ITextureCombinerPacker
    {
        public abstract bool Validate(MB3_TextureCombinerPipeline.TexturePipelineData data);

        internal static void CreateTemporaryTexturesForAtlas(List<MB_TexSet> distinctMaterialTextures, MB3_TextureCombiner combiner, int propIdx, MB3_TextureCombinerPipeline.TexturePipelineData data)
        {
            for (int texSetIdx = 0; texSetIdx < data.distinctMaterialTextures.Count; texSetIdx++)
            {
                MB_TexSet txs = data.distinctMaterialTextures[texSetIdx];
                MeshBakerMaterialTexture matTex = txs.ts[propIdx];
                if (matTex.isNull)
                {
                    //create a small 16 x 16 texture to use in the atlas
                    Color col = data.nonTexturePropertyBlender.GetColorForTemporaryTexture(txs.matsAndGOs.mats[0].mat, data.texPropertyNames[propIdx]);
                    txs.CreateColoredTexToReplaceNull(data.texPropertyNames[propIdx].name, propIdx, data._fixOutOfBoundsUVs, combiner, col, MB3_TextureCombiner.ShouldTextureBeLinear(data.texPropertyNames[propIdx]));
                }
            }
        }

        internal static void SaveAtlasAndConfigureResultMaterial(MB3_TextureCombinerPipeline.TexturePipelineData data, MB2_EditorMethodsInterface textureEditorMethods, Texture2D atlas, ShaderTextureProperty property, int propIdx)
        {
            bool doAnySrcMatsHaveProperty = MB3_TextureCombinerPipeline._DoAnySrcMatsHaveProperty(propIdx, data.allTexturesAreNullAndSameColor);
            if (data._saveAtlasesAsAssets && textureEditorMethods != null)
            {
                textureEditorMethods.SaveAtlasToAssetDatabase(atlas, property, propIdx, doAnySrcMatsHaveProperty, data.resultMaterial);
            }
            else
            {
                if (doAnySrcMatsHaveProperty)
                {
                    data.resultMaterial.SetTexture(property.name, atlas);
                }
            }

            if (doAnySrcMatsHaveProperty)
            {
                data.resultMaterial.SetTextureOffset(property.name, Vector2.zero);
                data.resultMaterial.SetTextureScale(property.name, Vector2.one);
            }
        }

        public static AtlasPackingResult[] CalculateAtlasRectanglesStatic(MB3_TextureCombinerPipeline.TexturePipelineData data, bool doMultiAtlas, MB2_LogLevel LOG_LEVEL)
        {
            List<Vector2> imageSizes = new List<Vector2>();
            for (int i = 0; i < data.distinctMaterialTextures.Count; i++)
            {
                imageSizes.Add(new Vector2(data.distinctMaterialTextures[i].idealWidth, data.distinctMaterialTextures[i].idealHeight));
            }

            MB2_TexturePacker tp = MB3_TextureCombinerPipeline.CreateTexturePacker(data._packingAlgorithm);
            tp.atlasMustBePowerOfTwo = data._meshBakerTexturePackerForcePowerOfTwo;
            List<AtlasPadding> paddings = new List<AtlasPadding>();
            for (int i = 0; i < imageSizes.Count; i++)
            {
                AtlasPadding padding = new AtlasPadding();
                padding.topBottom = data._atlasPadding;
                padding.leftRight = data._atlasPadding;
                if (data._packingAlgorithm == MB2_PackingAlgorithmEnum.MeshBakerTexturePacker_Horizontal) padding.leftRight = 0;
                if (data._packingAlgorithm == MB2_PackingAlgorithmEnum.MeshBakerTexturePacker_Vertical) padding.topBottom = 0;
                paddings.Add(padding);
            }

            return tp.GetRects(imageSizes, paddings, data._maxAtlasWidth, data._maxAtlasHeight, doMultiAtlas);
        }

        public static void MakeProceduralTexturesReadable(ProgressUpdateDelegate progressInfo,
            MB3_TextureCombiner.CombineTexturesIntoAtlasesCoroutineResult result,
            MB3_TextureCombinerPipeline.TexturePipelineData data,
            MB3_TextureCombiner combiner,
            MB2_EditorMethodsInterface textureEditorMethods,
            MB2_LogLevel LOG_LEVEL)
        {
            //Debug.LogError("TODO this should be done as close to textures being used as possible due to memory issues.");
            //make procedural materials readable
            /*
            for (int i = 0; i < combiner._proceduralMaterials.Count; i++)
            {
                if (!combiner._proceduralMaterials[i].proceduralMat.isReadable)
                {
                    combiner._proceduralMaterials[i].originalIsReadableVal = combiner._proceduralMaterials[i].proceduralMat.isReadable;
                    combiner._proceduralMaterials[i].proceduralMat.isReadable = true;
                    //textureEditorMethods.AddProceduralMaterialFormat(_proceduralMaterials[i].proceduralMat);
                    combiner._proceduralMaterials[i].proceduralMat.RebuildTexturesImmediately();
                }
            }
            //convert procedural textures to RAW format
            
            for (int i = 0; i < distinctMaterialTextures.Count; i++)
            {
                for (int j = 0; j < texPropertyNames.Count; j++)
                {
                    if (distinctMaterialTextures[i].ts[j].IsProceduralTexture())
                    {
                        if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Converting procedural texture to Textur2D:" + distinctMaterialTextures[i].ts[j].GetTexName() + " property:" + texPropertyNames[i]);
                        Texture2D txx = distinctMaterialTextures[i].ts[j].ConvertProceduralToTexture2D(_temporaryTextures);
                        distinctMaterialTextures[i].ts[j].t = txx;
                    }
                }
            }
            */
        }

        public virtual IEnumerator ConvertTexturesToReadableFormats(ProgressUpdateDelegate progressInfo,
            MB3_TextureCombiner.CombineTexturesIntoAtlasesCoroutineResult result,
            MB3_TextureCombinerPipeline.TexturePipelineData data,
            MB3_TextureCombiner combiner,
            MB2_EditorMethodsInterface textureEditorMethods,
            MB2_LogLevel LOG_LEVEL)
        {
            Debug.Assert(!data.OnlyOneTextureInAtlasReuseTextures());
            //MakeProceduralTexturesReadable(progressInfo, result, data, combiner, textureEditorMethods, LOG_LEVEL);
            for (int i = 0; i < data.distinctMaterialTextures.Count; i++)
            {
                for (int j = 0; j < data.texPropertyNames.Count; j++)
                {
                    MeshBakerMaterialTexture ts = data.distinctMaterialTextures[i].ts[j];
                    if (!ts.isNull)
                    {
                        if (textureEditorMethods != null)
                        {
                            Texture tx = ts.GetTexture2D();
                            TextureFormat format = TextureFormat.RGBA32;
                            if (progressInfo != null) progressInfo(String.Format("Convert texture {0} to readable format ", tx), .5f);
                            textureEditorMethods.ConvertTextureFormat_DefaultPlatform((Texture2D)tx, format, data.texPropertyNames[j].isNormalMap);
                        }
                    }
                }
            }
            yield break;
        }

        public virtual AtlasPackingResult[] CalculateAtlasRectangles(MB3_TextureCombinerPipeline.TexturePipelineData data, bool doMultiAtlas, MB2_LogLevel LOG_LEVEL)
        {
            return CalculateAtlasRectanglesStatic(data, doMultiAtlas, LOG_LEVEL);
        }

        public abstract IEnumerator CreateAtlases(ProgressUpdateDelegate progressInfo,
            MB3_TextureCombinerPipeline.TexturePipelineData data, MB3_TextureCombiner combiner,
            AtlasPackingResult packedAtlasRects,
            Texture2D[] atlases, MB2_EditorMethodsInterface textureEditorMethods,
            MB2_LogLevel LOG_LEVEL);

    }
}
