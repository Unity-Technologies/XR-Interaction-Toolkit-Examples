using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DigitalOpus.MB.Core
{
    //TODO
    //
    // What about normal maps?
    // 
    internal class MB3_TextureCombinerPackerMeshBakerFastV2 : MB_ITextureCombinerPacker
    {
        Mesh mesh;
        GameObject renderAtlasesGO;
        GameObject cameraGameObject;

        public bool Validate(MB3_TextureCombinerPipeline.TexturePipelineData data)
        {
            string layerName = LayerMask.LayerToName(data._layerTexturePackerFastV2);
            if (layerName == null || layerName.Length == 0)
            {
                Debug.LogError("The MB3_MeshBaker -> 'Atlas Render Layer' has not been set. This should be set to a layer that has no other renderers on it.");
                return false;
            }

            if (Application.isEditor)
            {
                Renderer[] rs = GameObject.FindObjectsOfType<Renderer>();
                bool isObjsOnLayer = false;
                for (int i = 0; i < rs.Length; i++)
                {
                    if (rs[i].gameObject.layer == data._layerTexturePackerFastV2)
                    {
                        isObjsOnLayer = true;
                    }
                }

                if (isObjsOnLayer)
                {
                    Debug.LogError("There are Renderers in the scene that are on layer '" + layerName + "'. 'Atlas Render Layer' layer should have no renderers that use it."); 
                    return false;
                }
            }

            // Tried adding a check for BuildSettings.DefineSymbols but it is messy because that is editor only code.

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
            //Rect[] uvRects = packedAtlasRects.rects;

            int atlasSizeX = packedAtlasRects.atlasX;
            int atlasSizeY = packedAtlasRects.atlasY;

            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Generated atlas will be " + atlasSizeX + "x" + atlasSizeY);

            int layer = data._layerTexturePackerFastV2;
            Debug.Assert(layer >= 0 && layer <= 32);

            //create a game object
            mesh = new Mesh();
            renderAtlasesGO = null;
            cameraGameObject = null;
            try
            {
                System.Diagnostics.Stopwatch db_time_MB3_TextureCombinerPackerMeshBakerFastV2_CreateAtlases = new System.Diagnostics.Stopwatch();
                db_time_MB3_TextureCombinerPackerMeshBakerFastV2_CreateAtlases.Start();
                renderAtlasesGO = new GameObject("MBrenderAtlasesGO");
                cameraGameObject = new GameObject("MBCameraGameObject");
                MB3_AtlasPackerRenderTextureUsingMesh atlasRenderer = new MB3_AtlasPackerRenderTextureUsingMesh();
                OneTimeSetup(atlasRenderer, renderAtlasesGO, cameraGameObject, atlasSizeX, atlasSizeY, data._atlasPadding, layer, LOG_LEVEL);

                if (data._considerNonTextureProperties && LOG_LEVEL >= MB2_LogLevel.warn) Debug.LogWarning("Blend Non-Texture Properties has limited functionality when used with Mesh Baker Texture Packer Fast.");

                List<Material> mats = new List<Material>();
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
                        if (progressInfo != null) progressInfo("Creating Atlas '" + data.texPropertyNames[propIdx].name + "'", .01f);

                        // configure it
                        if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("About to render " + data.texPropertyNames[propIdx].name + " isNormal=" + data.texPropertyNames[propIdx].isNormalMap);

                        // Create the mesh
                        mats.Clear();

                        MB3_AtlasPackerRenderTextureUsingMesh.MeshAtlas.BuildAtlas(packedAtlasRects, data.distinctMaterialTextures, propIdx, packedAtlasRects.atlasX, packedAtlasRects.atlasY, mesh, mats, data.texPropertyNames[propIdx], data, combiner, textureEditorMethods, LOG_LEVEL);
                        {
                            MeshFilter mf = renderAtlasesGO.GetComponent<MeshFilter>();
                            mf.sharedMesh = mesh;
                            MeshRenderer mrr = renderAtlasesGO.GetComponent<MeshRenderer>();
                            Material[] mrs = mats.ToArray();
                            mrr.sharedMaterials = mrs;
                        }

                        // Render
                        atlas = atlasRenderer.DoRenderAtlas(cameraGameObject, packedAtlasRects.atlasX, packedAtlasRects.atlasY, data.texPropertyNames[propIdx].isNormalMap, data.texPropertyNames[propIdx]);

                        {
                            for (int i = 0; i < mats.Count; i++)
                            {
                                MB_Utility.Destroy(mats[i]);
                            }
                            mats.Clear();
                        }

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

                if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.LogFormat("Timing MB3_TextureCombinerPackerMeshBakerFastV2.CreateAtlases={0}",
                                    db_time_MB3_TextureCombinerPackerMeshBakerFastV2_CreateAtlases.ElapsedMilliseconds * .001f);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message + "\n" + ex.StackTrace.ToString());
            }
            finally
            {
                if (renderAtlasesGO != null) { 
                    MB_Utility.Destroy(renderAtlasesGO); 
                }
                if (cameraGameObject != null) { 
                    MB_Utility.Destroy(cameraGameObject); 
                }
                if (mesh != null) { 
                    MB_Utility.Destroy(mesh); 
                }

            }
            yield break;
        }

        void OneTimeSetup(MB3_AtlasPackerRenderTextureUsingMesh atlasRenderer, GameObject atlasMesh, GameObject cameraGameObject, int atlasWidth, int atlasHeight, int padding, int layer, MB2_LogLevel logLevel)
        {
            {
                // Set up game object for holding the atlas mesh
                atlasMesh.AddComponent<MeshFilter>();
                atlasMesh.AddComponent<MeshRenderer>();
                atlasMesh.transform.rotation = Quaternion.Euler(0, 0, 0);
                atlasMesh.transform.position = new Vector3(0, 0, .5f);
                atlasMesh.gameObject.layer = layer;
            }

            // set up the camera
            {
                atlasRenderer.Initialize(layer,
                        atlasWidth,
                        atlasHeight,
                        padding,
                        logLevel);
                atlasRenderer.SetupCameraGameObject(cameraGameObject);
            }
        }

    }


    public class MB3_AtlasPackerRenderTextureUsingMesh
    {
        public int camMaskLayer;

        public int width;
        public int height;
        public int padding;
        public MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info;

        bool _initialized = false;
        bool _camSetup = false;

        public void Initialize(
            int camMaskLayer,
            int width,
            int height,
            int padding,
            MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info
            )
        {
            this.camMaskLayer = camMaskLayer;
            this.width = width;
            this.height = height;
            this.padding = padding;
            this.LOG_LEVEL = LOG_LEVEL;
            _initialized = true;
        }

        internal void SetupCameraGameObject(GameObject camGameObject)
        {
            Debug.Assert(_initialized);
            Debug.Assert(LayerMask.LayerToName(camMaskLayer) != null || LayerMask.LayerToName(camMaskLayer) != "");
            LayerMask camMask = 1 << camMaskLayer;
            Camera myCamera = camGameObject.AddComponent<Camera>();
            myCamera.enabled = false;
            myCamera.orthographic = true;
            myCamera.orthographicSize = height / 2f;
            myCamera.aspect = ((float)width) / height;
            myCamera.rect = new Rect(0, 0, 1, 1);
            myCamera.clearFlags = CameraClearFlags.Color;
            myCamera.cullingMask = camMask;
            Transform camTransform = myCamera.GetComponent<Transform>();
            camTransform.localPosition = new Vector3(width / 2.0f, height / 2f, 0);
            camTransform.localRotation = Quaternion.Euler(0, 0, 0);

            MBVersion.DoSpecialRenderPipeline_TexturePackerFastSetup(camGameObject);

            _camSetup = true;
        }

        internal Texture2D DoRenderAtlas(GameObject go, int width, int height, bool isNormalMap, ShaderTextureProperty propertyName)
        {
            System.Diagnostics.Stopwatch db_time_DoRenderAtlas = new System.Diagnostics.Stopwatch();
            db_time_DoRenderAtlas.Start();
            Debug.Assert(_initialized && _camSetup);
            RenderTexture _destinationTexture;
            if (isNormalMap)
            {
                _destinationTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            }
            else
            {
                _destinationTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            }

            _destinationTexture.filterMode = FilterMode.Point;
            Camera myCamera = go.GetComponent<Camera>();
            myCamera.targetTexture = _destinationTexture;
            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log(string.Format("Begin Camera.Render destTex w={0} h={1} camPos={2} camSize={3} camAspect={4}", width, height, go.transform.localPosition, myCamera.orthographicSize, myCamera.aspect.ToString("f5")));
            myCamera.Render();
            
            System.Diagnostics.Stopwatch db_ConvertRenderTextureToTexture2D = new System.Diagnostics.Stopwatch();
            db_ConvertRenderTextureToTexture2D.Start();
            Texture2D tempTexture = new Texture2D(_destinationTexture.width, _destinationTexture.height, TextureFormat.ARGB32, true, false);
            MB_TextureCombinerRenderTexture.ConvertRenderTextureToTexture2D(_destinationTexture, MB_TextureCombinerRenderTexture.YisFlipped(LOG_LEVEL), isNormalMap, LOG_LEVEL, tempTexture);

            if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("Finished rendering atlas " + propertyName.name + "  db_time_DoRenderAtlas:" + (db_time_DoRenderAtlas.ElapsedMilliseconds * .001f) + "  db_ConvertRenderTextureToTexture2D:" + (db_ConvertRenderTextureToTexture2D.ElapsedMilliseconds * .001f));
            MB_Utility.Destroy(_destinationTexture);
            return tempTexture;
        }

        public class MeshRectInfo
        {
            public int vertIdx;
            public int triIdx;
            public int atlasIdx;
        }

        public class MeshAtlas
        {
            internal static void BuildAtlas(
                AtlasPackingResult packedAtlasRects,
                List<MB_TexSet> distinctMaterialTextures,
                int propIdx,
                int atlasSizeX, int atlasSizeY,
                Mesh m,
                List<Material> generatedMats,
                ShaderTextureProperty property,
                MB3_TextureCombinerPipeline.TexturePipelineData data,
                MB3_TextureCombiner combiner,
                MB2_EditorMethodsInterface textureEditorMethods,
                MB2_LogLevel LOG_LEVEL)
            {
                // Collect vertices and quads for mesh that we will use for the atlas.
                Debug.Assert(generatedMats.Count == 0, "Previous mats should have been destroyed");
                generatedMats.Clear();
                List<Vector3> vs = new List<Vector3>();
                List<Vector2> uvs = new List<Vector2>();

                // One submesh and material per texture that we are packing
                List<int>[] ts = new List<int>[distinctMaterialTextures.Count];
                for (int i = 0; i < ts.Length; i++)
                {
                    ts[i] = new List<int>();
                }

                MeshBakerMaterialTexture.readyToBuildAtlases = true;
                GC.Collect();
                MB3_TextureCombinerPackerRoot.CreateTemporaryTexturesForAtlas(data.distinctMaterialTextures, combiner, propIdx, data);

                Rect[] uvRects = packedAtlasRects.rects;
                for (int texSetIdx = 0; texSetIdx < distinctMaterialTextures.Count; texSetIdx++)
                {
                    MB_TexSet texSet = distinctMaterialTextures[texSetIdx];
                    MeshBakerMaterialTexture matTex = texSet.ts[propIdx];

                    if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(string.Format("Adding texture {0} to atlas {1} for texSet {2} srcMat {3}", matTex.GetTexName(), property.name, texSetIdx, texSet.matsAndGOs.mats[0].GetMaterialName()));
                    Rect r = uvRects[texSetIdx];
                    Texture2D t = matTex.GetTexture2D();
                    int x = Mathf.RoundToInt(r.x * atlasSizeX);
                    int y = Mathf.RoundToInt(r.y * atlasSizeY);
                    int ww = Mathf.RoundToInt(r.width * atlasSizeX);
                    int hh = Mathf.RoundToInt(r.height * atlasSizeY);
                    r = new Rect(x, y, ww, hh);
                    if (ww == 0 || hh == 0) Debug.LogError("Image in atlas has no height or width " + r);
                    DRect samplingRect = texSet.ts[propIdx].GetEncapsulatingSamplingRect();
                    Debug.Assert(!texSet.ts[propIdx].isNull, string.Format("Adding texture {0} to atlas {1} for texSet {2} srcMat {3}", matTex.GetTexName(), property.name, texSetIdx, texSet.matsAndGOs.mats[0].GetMaterialName()));

                    AtlasPadding padding = packedAtlasRects.padding[texSetIdx];
                    AddNineSlicedRect(r, padding.leftRight, padding.topBottom, samplingRect.GetRect(), vs, uvs, ts[texSetIdx], t.width, t.height, t.name);

                    Material mt = new Material(Shader.Find("MeshBaker/Unlit/UnlitWithAlpha"));

                    bool isSavingAsANormalMapAssetThatWillBeImported = property.isNormalMap && data._saveAtlasesAsAssets;
                    MBVersion.PipelineType pipelineType = MBVersion.DetectPipeline();
                    if (pipelineType == MBVersion.PipelineType.URP)
                    {
                        ConfigureMaterial_DefaultPipeline(mt, t, isSavingAsANormalMapAssetThatWillBeImported, LOG_LEVEL);
                        //ConfigureMaterial_URP(mt, t, isSavingAsANormalMapAssetThatWillBeImported, LOG_LEVEL);
                    }
                    else if (pipelineType == MBVersion.PipelineType.HDRP)
                    {
                        ConfigureMaterial_DefaultPipeline(mt, t, isSavingAsANormalMapAssetThatWillBeImported, LOG_LEVEL);
                    } 
                    else
                    {
                        ConfigureMaterial_DefaultPipeline(mt, t, isSavingAsANormalMapAssetThatWillBeImported, LOG_LEVEL);
                    }

                    generatedMats.Add(mt);
                }

                // Apply to the mesh
                m.Clear();
                m.vertices = vs.ToArray();
                m.uv = uvs.ToArray();
                m.subMeshCount = ts.Length;
                for (int i = 0; i < m.subMeshCount; i++)
                {
                    m.SetIndices(ts[i].ToArray(), MeshTopology.Triangles, i);
                }
                MeshBakerMaterialTexture.readyToBuildAtlases = false;
            }

            static void ConfigureMaterial_DefaultPipeline(Material mt, Texture2D t, bool isSavingAsANormalMapAssetThatWillBeImported, MB2_LogLevel LOG_LEVEL)
            {
                Shader shad = null;
                shad = Shader.Find("MeshBaker/Unlit/UnlitWithAlpha");
                Debug.Assert(shad != null, "Could not find shader MeshBaker/Unlit/UnlitWithAlpha");
                mt.shader = shad;
                mt.SetTexture("_MainTex", t);
                if (isSavingAsANormalMapAssetThatWillBeImported)
                {
                    if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("Unswizling normal map channels NM");
                    mt.SetFloat("_SwizzleNormalMapChannelsNM", 1f);
                    mt.EnableKeyword("_SWIZZLE_NORMAL_CHANNELS_NM");
                }
                else
                {
                    //if (property.isNormalMap && LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("NOT Unswizling normal map channel savingAtlases=" + isSavingAsANormalMapAssetThatWillBeImported + " platformSwizzlesNormalMap=" + platformSwizzlesNormalMap);
                    mt.SetFloat("_SwizzleNormalMapChannelsNM", 0f);
                    mt.DisableKeyword("_SWIZZLE_NORMAL_CHANNELS_NM");
                }
            }

            public static MeshRectInfo AddQuad(Rect wldRect, Rect uvRect, List<Vector3> verts, List<Vector2> uvs, List<int> tris)
            {
                MeshRectInfo mri = new MeshRectInfo();
                int rootIdx = mri.vertIdx = verts.Count;
                mri.triIdx = tris.Count;

                verts.Add(new Vector3(wldRect.x, wldRect.y, 0));
                verts.Add(new Vector3(wldRect.x + wldRect.width, wldRect.y, 0));
                verts.Add(new Vector3(wldRect.x, wldRect.y + wldRect.height, 0));
                verts.Add(new Vector3(wldRect.x + wldRect.width, wldRect.y + wldRect.height, 0));

                uvs.Add(new Vector2(uvRect.x, uvRect.y));
                uvs.Add(new Vector2(uvRect.x + uvRect.width, uvRect.y));
                uvs.Add(new Vector2(uvRect.x, uvRect.y + uvRect.height));
                uvs.Add(new Vector2(uvRect.x + uvRect.width, uvRect.y + uvRect.height));

                tris.Add(rootIdx + 0); tris.Add(rootIdx + 2); tris.Add(rootIdx + 1);
                tris.Add(rootIdx + 2); tris.Add(rootIdx + 3); tris.Add(rootIdx + 1);
                return mri;
            }

            public static void AddNineSlicedRect(Rect atlasRectRaw, float paddingX, float paddingY, Rect srcUVRectt, List<Vector3> verts, List<Vector2> uvs, List<int> tris, float srcTexWidth, float srcTexHeight, string texName)
            {
                float singlePixelHalfWidth = .5f / srcTexWidth;
                float singlePixelHalfHeight = .5f / srcTexHeight;

                float smallWidth = 0f;
                float smallHeight = 0f;

                Rect srcUVRecttt = srcUVRectt;

                Rect srcRectMinusHalfPix = srcUVRectt;
                {
                    srcRectMinusHalfPix.x += singlePixelHalfWidth;
                    srcRectMinusHalfPix.y += singlePixelHalfHeight;
                    srcRectMinusHalfPix.width -= singlePixelHalfWidth * 2f;
                    srcRectMinusHalfPix.height -= singlePixelHalfHeight * 2f;
                }

                Rect atlasRect = atlasRectRaw;

                AddQuad(atlasRectRaw, srcUVRecttt, verts, uvs, tris);

                bool addTopBottom = paddingY > 0f;
                bool addLeftRight = paddingX > 0f;
                Rect uvRectPix;

                //Top

                if (addTopBottom)
                {
                    uvRectPix = new Rect(srcUVRecttt.x,
                                srcUVRecttt.y + srcUVRecttt.height - singlePixelHalfHeight - smallHeight,
                                srcUVRecttt.width,
                                smallHeight);
                    AddQuad(new Rect(atlasRect.x, atlasRect.y + atlasRect.height, atlasRect.width, paddingY), uvRectPix, verts, uvs, tris);
                }

                //Bottom
                if (addTopBottom)
                {
                    uvRectPix = new Rect(srcUVRecttt.x,
                            srcUVRecttt.y + singlePixelHalfHeight - smallHeight,
                            srcUVRecttt.width,
                            smallHeight);
                    AddQuad(new Rect(atlasRect.x, atlasRect.y - paddingY, atlasRect.width, paddingY), uvRectPix, verts, uvs, tris);
                }

                //Left
                if (addLeftRight)
                {
                    uvRectPix = new Rect(srcUVRecttt.x + singlePixelHalfWidth, srcUVRecttt.y, smallWidth, srcUVRecttt.height);
                    AddQuad(new Rect(atlasRect.x - paddingX, atlasRect.y, paddingX, atlasRect.height), uvRectPix, verts, uvs, tris);
                }

                //Right
                if (addLeftRight)
                {
                    uvRectPix = new Rect(srcUVRecttt.x + srcUVRecttt.width - singlePixelHalfWidth - smallWidth,
                            srcUVRecttt.y,
                            smallWidth,
                            srcUVRecttt.height);
                    AddQuad(new Rect(atlasRect.x + atlasRect.width, atlasRect.y, paddingX, atlasRect.height), uvRectPix, verts, uvs, tris);
                }


                // Bottom Left
                if (addTopBottom && addLeftRight)
                {
                    uvRectPix = new Rect(srcUVRecttt.x + singlePixelHalfWidth, srcUVRecttt.y + singlePixelHalfHeight, smallWidth, smallHeight);
                    AddQuad(new Rect(atlasRect.x - paddingX, atlasRect.y - paddingY, paddingX, paddingY), uvRectPix, verts, uvs, tris);
                }

                // Top Left
                if (addTopBottom && addLeftRight)
                {
                    uvRectPix = new Rect(
                        srcUVRecttt.x + singlePixelHalfWidth, 
                        srcUVRecttt.y + srcUVRecttt.height - singlePixelHalfHeight - smallHeight,
                        smallWidth, smallHeight);
                    AddQuad(new Rect(atlasRect.x - paddingX, atlasRect.y + atlasRect.height, paddingX, paddingY), uvRectPix, verts, uvs, tris);
                }


                // Top Right
                if (addTopBottom && addLeftRight)
                {
                    uvRectPix = new Rect(srcUVRecttt.x + srcUVRecttt.width - singlePixelHalfWidth - smallWidth, 
                            srcUVRecttt.y + srcUVRecttt.height - singlePixelHalfHeight - smallHeight, 
                            smallWidth, smallHeight);
                    AddQuad(new Rect(atlasRect.x + atlasRect.width, atlasRect.y + atlasRect.height, paddingX, paddingY), uvRectPix, verts, uvs, tris);
                }

                // Bot Right
                if (addTopBottom && addLeftRight)
                {
                    uvRectPix = new Rect(srcUVRecttt.x + srcUVRecttt.width - singlePixelHalfWidth - smallWidth, 
                            srcUVRecttt.y + singlePixelHalfHeight - smallHeight, 
                            smallWidth, smallHeight);
                    AddQuad(new Rect(atlasRect.x + atlasRect.width, atlasRect.y - paddingY, paddingX, paddingY), uvRectPix, verts, uvs, tris);
                }
            }
        }
    }

}
