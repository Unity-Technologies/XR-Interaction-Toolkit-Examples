using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DigitalOpus.MB.Core
{
    internal class MB3_TextureCombinerPackerOneTextureInAtlas : MB_ITextureCombinerPacker
    {
        public virtual bool Validate(MB3_TextureCombinerPipeline.TexturePipelineData data)
        {
            if (!data.OnlyOneTextureInAtlasReuseTextures())
            {
                Debug.LogError("There must be only one texture in the atlas to use MB3_TextureCombinerPackerOneTextureInAtlas");
                return false;
            }

            return true;
        }

        public IEnumerator ConvertTexturesToReadableFormats(ProgressUpdateDelegate progressInfo,
            MB3_TextureCombiner.CombineTexturesIntoAtlasesCoroutineResult result,
            MB3_TextureCombinerPipeline.TexturePipelineData data,
            MB3_TextureCombiner combiner,
            MB2_EditorMethodsInterface textureEditorMethods,
            MB2_LogLevel LOG_LEVEL)
        {
            Debug.Assert(data.OnlyOneTextureInAtlasReuseTextures());
            yield break;
        }

        public AtlasPackingResult[] CalculateAtlasRectangles(MB3_TextureCombinerPipeline.TexturePipelineData data, bool doMultiAtlas, MB2_LogLevel LOG_LEVEL)
        {
            Debug.Assert(data.OnlyOneTextureInAtlasReuseTextures());
            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Only one image per atlas. Will re-use original texture");
            AtlasPackingResult[] packerRects = new AtlasPackingResult[1];
            AtlasPadding[] paddings = new AtlasPadding[] { new AtlasPadding(data._atlasPadding) };
            packerRects[0] = new AtlasPackingResult(paddings);
            packerRects[0].rects = new Rect[1];
            packerRects[0].srcImgIdxs = new int[] { 0 };
            packerRects[0].rects[0] = new Rect(0f, 0f, 1f, 1f);

            MeshBakerMaterialTexture dmt = null;
            if (data.distinctMaterialTextures[0].ts.Length > 0)
            {
                dmt = data.distinctMaterialTextures[0].ts[0];

            }
            if (dmt == null || dmt.isNull)
            {
                packerRects[0].atlasX = 16;
                packerRects[0].atlasY = 16;
                packerRects[0].usedW = 16;
                packerRects[0].usedH = 16;
            }
            else
            {
                packerRects[0].atlasX = dmt.width;
                packerRects[0].atlasY = dmt.height;
                packerRects[0].usedW = dmt.width;
                packerRects[0].usedH = dmt.height;
            }
            return packerRects;
        }

        public IEnumerator CreateAtlases(ProgressUpdateDelegate progressInfo,
            MB3_TextureCombinerPipeline.TexturePipelineData data, MB3_TextureCombiner combiner,
            AtlasPackingResult packedAtlasRects,
            Texture2D[] atlases, MB2_EditorMethodsInterface textureEditorMethods,
            MB2_LogLevel LOG_LEVEL)
        {
            Debug.Assert(data.OnlyOneTextureInAtlasReuseTextures());
            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Only one image per atlas. Will re-use original texture");
            for (int i = 0; i < data.numAtlases; i++)
            {
                MeshBakerMaterialTexture dmt = data.distinctMaterialTextures[0].ts[i];
                atlases[i] = dmt.GetTexture2D();
                if (data.resultType == MB2_TextureBakeResults.ResultType.atlas)
                {
                    data.resultMaterial.SetTexture(data.texPropertyNames[i].name, atlases[i]);
                    data.resultMaterial.SetTextureScale(data.texPropertyNames[i].name, Vector2.one);
                    data.resultMaterial.SetTextureOffset(data.texPropertyNames[i].name, Vector2.zero);
                }
            }

            yield break;
        }
    }
}
