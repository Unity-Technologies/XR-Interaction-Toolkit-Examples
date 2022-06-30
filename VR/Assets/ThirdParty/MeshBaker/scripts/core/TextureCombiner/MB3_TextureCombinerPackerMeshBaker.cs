using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DigitalOpus.MB.Core
{
    internal class MB3_TextureCombinerPackerMeshBaker : MB3_TextureCombinerPackerRoot
    {
        public override bool Validate(MB3_TextureCombinerPipeline.TexturePipelineData data)
        {
            return true;
        }

        public override IEnumerator CreateAtlases(ProgressUpdateDelegate progressInfo,
            MB3_TextureCombinerPipeline.TexturePipelineData data, MB3_TextureCombiner combiner,
            AtlasPackingResult packedAtlasRects,
            Texture2D[] atlases, MB2_EditorMethodsInterface textureEditorMethods,
            MB2_LogLevel LOG_LEVEL)
        {
            Rect[] uvRects = packedAtlasRects.rects;

            int atlasSizeX = packedAtlasRects.atlasX;
            int atlasSizeY = packedAtlasRects.atlasY;
            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Generated atlas will be " + atlasSizeX + "x" + atlasSizeY);

            for (int propIdx = 0; propIdx < data.numAtlases; propIdx++)
            {
                Texture2D atlas = null;
                ShaderTextureProperty property = data.texPropertyNames[propIdx];
                if (!MB3_TextureCombinerPipeline._ShouldWeCreateAtlasForThisProperty(propIdx, data._considerNonTextureProperties, data.allTexturesAreNullAndSameColor))
                {
                    atlas = null;
                    if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("=== Not creating atlas for " + property.name + " because textures are null and default value parameters are the same.");
                }
                else
                {
                    if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("=== Creating atlas for " + property.name);

                    GC.Collect();

                    CreateTemporaryTexturesForAtlas(data.distinctMaterialTextures, combiner, propIdx, data);

                    //use a jagged array because it is much more efficient in memory
                    Color[][] atlasPixels = new Color[atlasSizeY][];
                    for (int j = 0; j < atlasPixels.Length; j++)
                    {
                        atlasPixels[j] = new Color[atlasSizeX];
                    }

                    bool isNormalMap = false;
                    if (property.isNormalMap) isNormalMap = true;

                    for (int texSetIdx = 0; texSetIdx < data.distinctMaterialTextures.Count; texSetIdx++)
                    {
                        MB_TexSet texSet = data.distinctMaterialTextures[texSetIdx];
                        MeshBakerMaterialTexture matTex = texSet.ts[propIdx];
                        string s = "Creating Atlas '" + property.name + "' texture " + matTex.GetTexName();
                        if (progressInfo != null) progressInfo(s, .01f);
                        if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(string.Format("Adding texture {0} to atlas {1} for texSet {2} srcMat {3}", matTex.GetTexName(), property.name, texSetIdx, texSet.matsAndGOs.mats[0].GetMaterialName()));
                        Rect r = uvRects[texSetIdx];
                        Texture2D t = texSet.ts[propIdx].GetTexture2D();
                        int x = Mathf.RoundToInt(r.x * atlasSizeX);
                        int y = Mathf.RoundToInt(r.y * atlasSizeY);
                        int ww = Mathf.RoundToInt(r.width * atlasSizeX);
                        int hh = Mathf.RoundToInt(r.height * atlasSizeY);
                        if (ww == 0 || hh == 0) Debug.LogError("Image in atlas has no height or width " + r);
                        if (progressInfo != null) progressInfo(s + " set ReadWrite flag", .01f);
                        if (textureEditorMethods != null) textureEditorMethods.SetReadWriteFlag(t, true, true);
                        if (progressInfo != null) progressInfo(s + "Copying to atlas: '" + matTex.GetTexName() + "'", .02f);
                        DRect samplingRect = texSet.ts[propIdx].GetEncapsulatingSamplingRect();
                        Debug.Assert(!texSet.ts[propIdx].isNull, string.Format("Adding texture {0} to atlas {1} for texSet {2} srcMat {3}", matTex.GetTexName(), property.name, texSetIdx, texSet.matsAndGOs.mats[0].GetMaterialName()));
                        yield return CopyScaledAndTiledToAtlas(texSet.ts[propIdx], texSet, property, samplingRect, x, y, ww, hh, packedAtlasRects.padding[texSetIdx], atlasPixels, isNormalMap, data, combiner, progressInfo, LOG_LEVEL);
                    }

                    yield return data.numAtlases;
                    if (progressInfo != null) progressInfo("Applying changes to atlas: '" + property.name + "'", .03f);
                    atlas = new Texture2D(atlasSizeX, atlasSizeY, TextureFormat.ARGB32, true);
                    for (int j = 0; j < atlasPixels.Length; j++)
                    {
                        atlas.SetPixels(0, j, atlasSizeX, 1, atlasPixels[j]);
                    }

                    atlas.Apply();
                    if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Saving atlas " + property.name + " w=" + atlas.width + " h=" + atlas.height);
                }

                atlases[propIdx] = atlas;
                if (progressInfo != null) progressInfo("Saving atlas: '" + property.name + "'", .04f);
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                if (data.resultType == MB2_TextureBakeResults.ResultType.atlas)
                {
                    SaveAtlasAndConfigureResultMaterial(data, textureEditorMethods, atlases[propIdx], data.texPropertyNames[propIdx], propIdx);
                }

                combiner._destroyTemporaryTextures(data.texPropertyNames[propIdx].name);
            }

            yield break;
        }

        internal static IEnumerator CopyScaledAndTiledToAtlas(MeshBakerMaterialTexture source, MB_TexSet sourceMaterial,
            ShaderTextureProperty shaderPropertyName, DRect srcSamplingRect, int targX, int targY, int targW, int targH,
            AtlasPadding padding,
            Color[][] atlasPixels, bool isNormalMap,
            MB3_TextureCombinerPipeline.TexturePipelineData data,
            MB3_TextureCombiner combiner,
            ProgressUpdateDelegate progressInfo = null,
            MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info)
        {
            //HasFinished = false;
            Texture2D t = source.GetTexture2D();
            if (LOG_LEVEL >= MB2_LogLevel.debug)
            {
                Debug.Log(String.Format("CopyScaledAndTiledToAtlas: {0} inAtlasX={1} inAtlasY={2} inAtlasW={3} inAtlasH={4} paddX={5} paddY={6} srcSamplingRect={7}", 
                    t, targX, targY, targW, targH, padding.leftRight, padding.topBottom, srcSamplingRect));
            }
            float newWidth = targW;
            float newHeight = targH;
            float scx = (float)srcSamplingRect.width;
            float scy = (float)srcSamplingRect.height;
            float ox = (float)srcSamplingRect.x;
            float oy = (float)srcSamplingRect.y;
            int w = (int)newWidth;
            int h = (int)newHeight;
            if (data._considerNonTextureProperties)
            {
                t = combiner._createTextureCopy(shaderPropertyName.name, t);
                t = data.nonTexturePropertyBlender.TintTextureWithTextureCombiner(t, sourceMaterial, shaderPropertyName);
            }
            for (int i = 0; i < w; i++)
            {

                if (progressInfo != null && w > 0) progressInfo("CopyScaledAndTiledToAtlas " + (((float)i / (float)w) * 100f).ToString("F0"), .2f);
                for (int j = 0; j < h; j++)
                {
                    float u = i / newWidth * scx + ox;
                    float v = j / newHeight * scy + oy;
                    atlasPixels[targY + j][targX + i] = t.GetPixelBilinear(u, v);
                }
            }

            //bleed the border colors into the padding
            for (int i = 0; i < w; i++)
            {
                for (int j = 1; j <= padding.topBottom; j++)
                {
                    //top margin
                    atlasPixels[(targY - j)][targX + i] = atlasPixels[(targY)][targX + i];
                    //bottom margin
                    atlasPixels[(targY + h - 1 + j)][targX + i] = atlasPixels[(targY + h - 1)][targX + i];
                }
            }
            for (int j = 0; j < h; j++)
            {
                for (int i = 1; i <= padding.leftRight; i++)
                {
                    //left margin
                    atlasPixels[(targY + j)][targX - i] = atlasPixels[(targY + j)][targX];
                    //right margin
                    atlasPixels[(targY + j)][targX + w + i - 1] = atlasPixels[(targY + j)][targX + w - 1];
                }
            }
            //corners
            for (int i = 1; i <= padding.leftRight; i++)
            {
                for (int j = 1; j <= padding.topBottom; j++)
                {
                    atlasPixels[(targY - j)][targX - i] = atlasPixels[targY][targX];
                    atlasPixels[(targY + h - 1 + j)][targX - i] = atlasPixels[(targY + h - 1)][targX];
                    atlasPixels[(targY + h - 1 + j)][targX + w + i - 1] = atlasPixels[(targY + h - 1)][targX + w - 1];
                    atlasPixels[(targY - j)][targX + w + i - 1] = atlasPixels[targY][targX + w - 1];
                    yield return null;
                }
                yield return null;
            }
            //			Debug.Log("copyandscaledatlas finished too!");
            //HasFinished = true;
            yield break;
        }
    }
}
