using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DigitalOpus.MB.Core
{
    internal class MB3_TextureCombinerPackerMeshBakerFast : MB_ITextureCombinerPacker
    {
        public bool Validate(MB3_TextureCombinerPipeline.TexturePipelineData data)
        {
            return true;
        }

        public IEnumerator ConvertTexturesToReadableFormats(ProgressUpdateDelegate progressInfo,
            MB3_TextureCombiner.CombineTexturesIntoAtlasesCoroutineResult result,
            MB3_TextureCombinerPipeline.TexturePipelineData data,
            MB3_TextureCombiner combiner,
            MB2_EditorMethodsInterface textureEditorMethods,
            MB2_LogLevel LOG_LEVEL)
        {
            Debug.Assert(!data.OnlyOneTextureInAtlasReuseTextures());
            //MB3_TextureCombinerPackerRoot.MakeProceduralTexturesReadable(progressInfo, result, data, combiner, textureEditorMethods, LOG_LEVEL);
            yield break;
        }

        public virtual AtlasPackingResult[] CalculateAtlasRectangles(MB3_TextureCombinerPipeline.TexturePipelineData data, bool doMultiAtlas, MB2_LogLevel LOG_LEVEL)
        {
            Debug.Assert(!data.OnlyOneTextureInAtlasReuseTextures());
            return MB3_TextureCombinerPackerRoot.CalculateAtlasRectanglesStatic(data, doMultiAtlas, LOG_LEVEL);
        }

        public IEnumerator CreateAtlases(ProgressUpdateDelegate progressInfo,
            MB3_TextureCombinerPipeline.TexturePipelineData data, MB3_TextureCombiner combiner,
            AtlasPackingResult packedAtlasRects,
            Texture2D[] atlases, MB2_EditorMethodsInterface textureEditorMethods,
            MB2_LogLevel LOG_LEVEL)
        {
            Debug.Assert(!data.OnlyOneTextureInAtlasReuseTextures());
            Rect[] uvRects = packedAtlasRects.rects;

            int atlasSizeX = packedAtlasRects.atlasX;
            int atlasSizeY = packedAtlasRects.atlasY;
            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Generated atlas will be " + atlasSizeX + "x" + atlasSizeY);

            //create a game object
            GameObject renderAtlasesGO = null;
            try
            {
                renderAtlasesGO = new GameObject("MBrenderAtlasesGO");
                MB3_AtlasPackerRenderTexture atlasRenderTexture = renderAtlasesGO.AddComponent<MB3_AtlasPackerRenderTexture>();
                renderAtlasesGO.AddComponent<Camera>();
                if (data._considerNonTextureProperties && LOG_LEVEL >= MB2_LogLevel.warn) Debug.LogError("Blend Non-Texture Properties has limited functionality when used with Mesh Baker Texture Packer Fast. If no texture is pesent, then a small texture matching the non-texture property will be created and used in the atlas. But non-texture properties will not be blended into texture.");

                for (int propIdx = 0; propIdx < data.numAtlases; propIdx++)
                {
                    Texture2D atlas = null;
                    if (!MB3_TextureCombinerPipeline._ShouldWeCreateAtlasForThisProperty(propIdx, data._considerNonTextureProperties, data.allTexturesAreNullAndSameColor))
                    {
                        atlas = null;
                        if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Not creating atlas for " + data.texPropertyNames[propIdx].name + " because textures are null and default value parameters are the same.");
                    }
                    else
                    {
                        GC.Collect();

                        MB3_TextureCombinerPackerRoot.CreateTemporaryTexturesForAtlas(data.distinctMaterialTextures, combiner, propIdx, data);

                        if (progressInfo != null) progressInfo("Creating Atlas '" + data.texPropertyNames[propIdx].name + "'", .01f);
                        // ===========
                        // configure it
                        if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("About to render " + data.texPropertyNames[propIdx].name + " isNormal=" + data.texPropertyNames[propIdx].isNormalMap);
                        atlasRenderTexture.LOG_LEVEL = LOG_LEVEL;
                        atlasRenderTexture.width = atlasSizeX;
                        atlasRenderTexture.height = atlasSizeY;
                        atlasRenderTexture.padding = data._atlasPadding;
                        atlasRenderTexture.rects = uvRects;
                        atlasRenderTexture.textureSets = data.distinctMaterialTextures;
                        atlasRenderTexture.indexOfTexSetToRender = propIdx;
                        atlasRenderTexture.texPropertyName = data.texPropertyNames[propIdx];
                        atlasRenderTexture.isNormalMap = data.texPropertyNames[propIdx].isNormalMap;
                        atlasRenderTexture.fixOutOfBoundsUVs = data._fixOutOfBoundsUVs;
                        atlasRenderTexture.considerNonTextureProperties = data._considerNonTextureProperties;
                        atlasRenderTexture.resultMaterialTextureBlender = data.nonTexturePropertyBlender;
                        // call render on it
                        atlas = atlasRenderTexture.OnRenderAtlas(combiner);

                        // destroy it
                        // =============
                        if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Saving atlas " + data.texPropertyNames[propIdx].name + " w=" + atlas.width + " h=" + atlas.height + " id=" + atlas.GetInstanceID());
                    }
                    atlases[propIdx] = atlas;
                    if (progressInfo != null) progressInfo("Saving atlas: '" + data.texPropertyNames[propIdx].name + "'", .04f);
                    if (data.resultType == MB2_TextureBakeResults.ResultType.atlas)
                    {
                        MB3_TextureCombinerPackerRoot.SaveAtlasAndConfigureResultMaterial(data, textureEditorMethods, atlases[propIdx], data.texPropertyNames[propIdx], propIdx);
                    }

                    combiner._destroyTemporaryTextures(data.texPropertyNames[propIdx].name); // need to save atlases before doing this				
                }
            }
            catch (Exception ex)
            {
                //Debug.LogError(ex);
                Debug.LogError(ex.Message + "\n" + ex.StackTrace.ToString());
            }
            finally
            {
                if (renderAtlasesGO != null)
                {
                    MB_Utility.Destroy(renderAtlasesGO);
                }
            }
            yield break;
        }
    }
}
