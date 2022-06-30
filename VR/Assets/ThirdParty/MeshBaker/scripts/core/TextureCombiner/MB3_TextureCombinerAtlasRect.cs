using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;

namespace DigitalOpus.MB.Core
{

    /*
     Like a material but also stores its tiling info since the same texture
     with different tiling may need to be baked to a separate spot in the atlas
     note that it is sometimes possible for textures with different tiling to share an atlas rectangle
     To accomplish this need to store:
             uvTiling per TexSet (can be set to 0,0,1,1 by pushing tiling down into material tiling)
             matTiling per MeshBakerMaterialTexture (this is the total tiling baked into the atlas)
             matSubrectInFullSamplingRect per material (a MeshBakerMaterialTexture can be used by multiple materials. This is the subrect in the atlas)
     Normally UVTilings is applied first then material tiling after. This is difficult for us to use when baking meshes. It is better to apply material
     tiling first then UV Tiling. There is a transform for modifying the material tiling to handle this.
     once the material tiling is applied first then the uvTiling can be pushed down into the material tiling.

         Also note that this can wrap a procedural texture. The procedural texture is converted to a Texture2D in Step2 NOT BEFORE. This is important so that can
         build packing layout quickly. 

             Should always check if texture is null using 'isNull' function since Texture2D could be null but ProceduralTexture not
             Should not call GetTexture2D before procedural textures are created

         there will be one of these per material texture property (maintex, bump etc...)
     */
    public class MeshBakerMaterialTexture
    {
        //private ProceduralTexture _procT;
        private Texture2D _t;

        public Texture2D t
        {
            set { _t = value; }
        }

        public float texelDensity; //how many pixels per polygon area
        internal static bool readyToBuildAtlases = false;
        //if these are the same for all properties then these can be merged
        /// <summary>
        /// sampling rect including both material tiling and uv Tiling. Most of the time this is the
        /// same for maintex, bumpmap, etc... but it does not need to be. Could have maintex with one
        /// tiling and bumpmap with another. If these are the same for all properties then these can be merged
        /// </summary>
        private DRect encapsulatingSamplingRect;

        /// <summary>
        /// IMPORTANT: There are two materialTilingRects. These ones are stored per result-material-texture-property.
        /// The are used when baking atlases, NOT for mapping materials to atlas rects and transforming UVs.
        /// The material tiling for a texture. These can be different for different properties: maintex, bumpmap etc....
        /// If these are the same for all properties then the MB_TexSets can be merged.
        /// </summary>
        public DRect matTilingRect { get; private set; }

        /// <summary>
        /// Returns -1 if this texture was imported as a normal map
        /// Returns 1 if this texture was not imported as a normal map
        /// Returns 0 if unknown
        /// </summary>
        public int isImportedAsNormalMap { get; private set; }

        /*
        public MeshBakerMaterialTexture() { }
        public MeshBakerMaterialTexture(Texture tx)
        {
            if (tx is Texture2D)
            {
                _t = (Texture2D)tx;
            }
            //else if (tx is ProceduralTexture)
            //{
            //    _procT = (ProceduralTexture)tx;
            //}
            else if (tx == null)
            {
                //do nothing
            }
            else
            {
                Debug.LogError("An error occured. Texture must be Texture2D " + tx);
            }
        }
        */

        public MeshBakerMaterialTexture(Texture tx, Vector2 matTilingOffset, Vector2 matTilingScale, float texelDens, int isImportedAsNormalMap)
        {
            if (tx is Texture2D)
            {
                _t = (Texture2D)tx;
            }
            //else if (tx is ProceduralTexture)
            //{
            //    _procT = (ProceduralTexture)tx;
            //}
            else if (tx == null)
            {
                //do nothing
            }
            else
            {
                Debug.LogError("An error occured. Texture must be Texture2D " + tx);
            }
            matTilingRect = new DRect(matTilingOffset, matTilingScale);
            texelDensity = texelDens;
            this.isImportedAsNormalMap = isImportedAsNormalMap;
        }

        public DRect GetEncapsulatingSamplingRect()
        {
            return encapsulatingSamplingRect;
        }

        /*
        public void SetMaterialTilingTo0011()
        {
            matTilingRect = new DRect(0, 0, 1, 1);
        }
        */

        /// <summary>
        /// The ts variable serves no functional purpose. I would like this method to ONLY be called from 
        /// MB_TexSet but there is no way in C# to enforce that. If you want to use this 
        /// outside  of MB_TexSet then add a method to MB_TexSet that does the add on your 
        /// behalf. The new method should assert that MB_TexSet is in the correct state and should 
        /// do any necessary stated changes to MB_TexSet depending on the context.
        /// 
        /// After you are done you should "FindAllReferences" for this function to ensure that it is only
        /// called by MB_TexSet.
        /// </summary>
        /// <param name="ts">
        /// Not used, provided as an indicator to developers that all
        /// access to this must go through MB_TexSet.
        /// </param>
        public void SetEncapsulatingSamplingRect(MB_TexSet ts, DRect r)
        {
            encapsulatingSamplingRect = r;
        }

        // This should never be called until we are readyToBuildAtlases. The reason is that the textures
        // may not exist, temporary textures may need to be created.
        public Texture2D GetTexture2D()
        {
            if (!readyToBuildAtlases)
            {
                Debug.LogError("This function should not be called before Step3. For steps 1 and 2 should always call methods like isNull, width, height");
                throw new Exception("GetTexture2D called before ready to build atlases");
            }
            return _t;
        }

        public bool isNull
        {
            get { return _t == null/* && _procT == null*/; }
        }

        public int width
        {
            get
            {
                if (_t != null) return _t.width;
                //else if (_procT != null) return _procT.width;
                throw new Exception("Texture was null. can't get width");
            }
        }

        public int height
        {
            get
            {
                if (_t != null) return _t.height;
                //else if (_procT != null) return _procT.height;
                throw new Exception("Texture was null. can't get height");
            }
        }

        public string GetTexName()
        {
            if (_t != null) return _t.name;
            //else if (_procT != null) return _procT.name;
            return "null";
        }

        public bool AreTexturesEqual(MeshBakerMaterialTexture b)
        {
            if (_t == b._t /*&& _procT == b._procT*/) return true;
            return false;
        }

        /*
        public bool IsProceduralTexture()
        {
            return (_procT != null);
        }

        public ProceduralTexture GetProceduralTexture()
        {
            return _procT;
        }

        public Texture2D ConvertProceduralToTexture2D(List<Texture2D> temporaryTextures)
        {
            int w = _procT.width;
            int h = _procT.height;

            bool mips = true;
            bool isLinear = false;
            GC.Collect(3, GCCollectionMode.Forced);
            Texture2D tex = new Texture2D(w, h, TextureFormat.ARGB32, mips, isLinear);
            Color32[] pixels = _procT.GetPixels32(0, 0, w, h);
            tex.SetPixels32(0, 0, w, h, pixels);
            tex.Apply();
            tex.name = _procT.name;
            temporaryTextures.Add(tex);
            return tex;
        }
        */
    }

    public class MatAndTransformToMerged
    {
        public Material mat;

        /// <summary>
        /// If considerUVs = true is set to the UV rect of the source mesh
        /// otherwise if considerUVs = false is set to 0,0,1,1
        /// </summary>
        public DRect obUVRectIfTilingSame { get; private set; }
        public DRect samplingRectMatAndUVTiling { get; private set; }

        /// <summary>
        /// IMPORTANT: There are two materialTilingRects. These ones are stored per source material.
        /// For mapping materials to atlas rects and transforming UVs, NOT for baking atlases.    
        /// Is set to materialTiling if allTexturesUseSameMatTiling otherwise
        /// is set to 0,0,1,1
        /// </summary>
        public DRect materialTiling { get; private set; }
        public string objName;

        public MatAndTransformToMerged(DRect obUVrect, bool fixOutOfBoundsUVs)
        {
            _init(obUVrect, fixOutOfBoundsUVs, null);
        }

        public MatAndTransformToMerged(DRect obUVrect, bool fixOutOfBoundsUVs, Material m)
        {
            _init(obUVrect, fixOutOfBoundsUVs, m);
        }

        private void _init(DRect obUVrect, bool fixOutOfBoundsUVs, Material m)
        {
            if (fixOutOfBoundsUVs)
            {
                obUVRectIfTilingSame = obUVrect;
            }
            else
            {
                obUVRectIfTilingSame = new DRect(0, 0, 1, 1);
            }
            mat = m;
        }

        public override bool Equals(object obj)
        {
            if (obj is MatAndTransformToMerged)
            {
                MatAndTransformToMerged o = (MatAndTransformToMerged)obj;


                if (o.mat == mat && o.obUVRectIfTilingSame == obUVRectIfTilingSame)
                {
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return mat.GetHashCode() ^ obUVRectIfTilingSame.GetHashCode() ^ samplingRectMatAndUVTiling.GetHashCode();
        }

        public string GetMaterialName()
        {
            if (mat != null)
            {
                return mat.name;
            }
            else if (objName != null)
            {
                return string.Format("[matFor: {0}]", objName);
            }
            else
            {
                return "Unknown";
            }
        }

        public void AssignInitialValuesForMaterialTilingAndSamplingRectMatAndUVTiling(bool allTexturesUseSameMatTiling, DRect matTiling)
        {
            if (allTexturesUseSameMatTiling)
            {
                materialTiling = matTiling;

            }
            else
            {
                materialTiling = new DRect(0f, 0f, 1f, 1f);
            }
            DRect tmpMatTiling = materialTiling;
            DRect obUVrect = obUVRectIfTilingSame;
            samplingRectMatAndUVTiling = MB3_UVTransformUtility.CombineTransforms(ref obUVrect, ref tmpMatTiling);
        }
    }

    public class MatsAndGOs
    {
        public List<MatAndTransformToMerged> mats;
        public List<GameObject> gos;
    }

    /// <summary>
    /// A set of textures one for each "maintex","bump" that one or more materials use. These
    /// Will be baked into a rectangle in the atlas.
    /// </summary>
    public class MB_TexSet
    {
        /// <summary>
        /// There is different handing of how things are baked into atlases depending on:
        ///   do all TexturesUseSameMaterialTiling
        ///   are the textures edge to edge.
        /// We try to capture those differences a clearly defined way.
        /// </summary>
        private interface PipelineVariation{
            void GetRectsForTextureBakeResults(out Rect allPropsUseSameTiling_encapsulatingSamplingRect,
                                                        out Rect propsUseDifferntTiling_obUVRect);

            void SetTilingTreatmentAndAdjustEncapsulatingSamplingRect(MB_TextureTilingTreatment newTilingTreatment);

            Rect GetMaterialTilingRectForTextureBakerResults(int materialIndex);

            void AdjustResultMaterialNonTextureProperties(Material resultMaterial, List<ShaderTextureProperty> props);
        }

        private class PipelineVariationAllTexturesUseSameMatTiling : PipelineVariation
        {
            private MB_TexSet texSet;

            public PipelineVariationAllTexturesUseSameMatTiling(MB_TexSet ts)
            {
                texSet = ts;
                Debug.Assert(texSet.allTexturesUseSameMatTiling == true);
            }

            public void GetRectsForTextureBakeResults(out Rect allPropsUseSameTiling_encapsulatingSamplingRect,
                                                        out Rect propsUseDifferntTiling_obUVRect)
            {
                Debug.Assert(texSet.allTexturesUseSameMatTiling == true);
                propsUseDifferntTiling_obUVRect = new Rect(0, 0, 0, 0);
                allPropsUseSameTiling_encapsulatingSamplingRect = texSet.GetEncapsulatingSamplingRectIfTilingSame();
                //adjust for tilingTreatment
                if (texSet.tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeX)
                {
                    allPropsUseSameTiling_encapsulatingSamplingRect.x = 0;
                    allPropsUseSameTiling_encapsulatingSamplingRect.width = 1;
                }
                else if (texSet.tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeY)
                {
                    allPropsUseSameTiling_encapsulatingSamplingRect.y = 0;
                    allPropsUseSameTiling_encapsulatingSamplingRect.height = 1;
                }
                else if (texSet.tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeXY)
                {
                    allPropsUseSameTiling_encapsulatingSamplingRect = new Rect(0, 0, 1, 1);
                }
            }

            public void SetTilingTreatmentAndAdjustEncapsulatingSamplingRect(MB_TextureTilingTreatment newTilingTreatment)
            {
                Debug.Assert(texSet.allTexturesUseSameMatTiling == true);
                if (texSet.tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeX)
                {
                    foreach (MeshBakerMaterialTexture t in texSet.ts)
                    {
                        DRect r = t.GetEncapsulatingSamplingRect();
                        r.width = 1;
                        t.SetEncapsulatingSamplingRect(texSet, r);
                    }
                }
                else if (texSet.tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeY)
                {
                    foreach (MeshBakerMaterialTexture t in texSet.ts)
                    {
                        DRect r = t.GetEncapsulatingSamplingRect();
                        r.height = 1;
                        t.SetEncapsulatingSamplingRect(texSet, r);
                    }
                }
                else if (texSet.tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeXY)
                {
                    foreach (MeshBakerMaterialTexture t in texSet.ts)
                    {
                        DRect r = t.GetEncapsulatingSamplingRect();
                        r.height = 1;
                        r.width = 1;
                        t.SetEncapsulatingSamplingRect(texSet, r);
                    }
                }
            }

            public Rect GetMaterialTilingRectForTextureBakerResults(int materialIndex)
            {
                Debug.Assert(texSet.allTexturesUseSameMatTiling == true);
                return texSet.matsAndGOs.mats[materialIndex].materialTiling.GetRect();
            }

            public void AdjustResultMaterialNonTextureProperties(Material resultMaterial, List<ShaderTextureProperty> props)
            {
                Debug.Assert(texSet.allTexturesUseSameMatTiling == true);
            }
        }

        private class PipelineVariationSomeTexturesUseDifferentMatTiling : PipelineVariation
        {
            private MB_TexSet texSet;

            public PipelineVariationSomeTexturesUseDifferentMatTiling(MB_TexSet ts)
            {
                texSet = ts;
                Debug.Assert(texSet.allTexturesUseSameMatTiling == false);
            }

            public void GetRectsForTextureBakeResults(out Rect allPropsUseSameTiling_encapsulatingSamplingRect,
                                                        out Rect propsUseDifferntTiling_obUVRect)
            {
                Debug.Assert(texSet.allTexturesUseSameMatTiling == false);
                allPropsUseSameTiling_encapsulatingSamplingRect = new Rect(0,0,0,0);
                propsUseDifferntTiling_obUVRect = texSet.obUVrect.GetRect();
                //adjust for tilingTreatment
                if (texSet.tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeX)
                {
                    propsUseDifferntTiling_obUVRect.x = 0;
                    propsUseDifferntTiling_obUVRect.width = 1;
                }
                else if (texSet.tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeY)
                {
                    propsUseDifferntTiling_obUVRect.y = 0;
                    propsUseDifferntTiling_obUVRect.height = 1;
                }
                else if (texSet.tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeXY)
                {
                    propsUseDifferntTiling_obUVRect = new Rect(0, 0, 1, 1);
                }
            }

            public void SetTilingTreatmentAndAdjustEncapsulatingSamplingRect(MB_TextureTilingTreatment newTilingTreatment)
            {
                Debug.Assert(texSet.allTexturesUseSameMatTiling == false);
                if (texSet.tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeX)
                {
                    foreach (MeshBakerMaterialTexture t in texSet.ts)
                    {
                        DRect r = t.GetEncapsulatingSamplingRect();
                        r.width = 1;
                        t.SetEncapsulatingSamplingRect(texSet, r);
                    }
                }
                else if (texSet.tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeY)
                {
                    foreach (MeshBakerMaterialTexture t in texSet.ts)
                    {
                        DRect r = t.GetEncapsulatingSamplingRect();
                        r.height = 1;
                        t.SetEncapsulatingSamplingRect(texSet, r);
                    }
                }
                else if (texSet.tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeXY)
                {
                    foreach (MeshBakerMaterialTexture t in texSet.ts)
                    {
                        DRect r = t.GetEncapsulatingSamplingRect();
                        r.height = 1;
                        r.width = 1;
                        t.SetEncapsulatingSamplingRect(texSet, r);
                    }
                }
            }

            public Rect GetMaterialTilingRectForTextureBakerResults(int materialIndex)
            {
                Debug.Assert(texSet.allTexturesUseSameMatTiling == false);
                return new Rect(0,0,0,0);
            }

            public void AdjustResultMaterialNonTextureProperties(Material resultMaterial, List<ShaderTextureProperty> props)
            {
                Debug.Assert(texSet.allTexturesUseSameMatTiling == false);
                if (texSet.thisIsOnlyTexSetInAtlas)
                {
                    for (int i = 0; i < props.Count; i++)
                    {
                        if (resultMaterial.HasProperty(props[i].name))
                        {

                            resultMaterial.SetTextureOffset(props[i].name, texSet.ts[i].matTilingRect.min);
                            resultMaterial.SetTextureScale(props[i].name, texSet.ts[i].matTilingRect.size);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// One per "maintex", "bump".
        /// Stores encapsulatingSamplingRect (can be different for maintex, bump...)
        /// Stores materialTiling for mapping materials to atlas rects and transforming UVs not for baking atlases
        /// 
        /// </summary>
        public MeshBakerMaterialTexture[] ts;

        public MatsAndGOs matsAndGOs;

        public bool allTexturesUseSameMatTiling { get; private set; }

        public bool thisIsOnlyTexSetInAtlas { get; private set; }

        public MB_TextureTilingTreatment tilingTreatment { get; private set; }

        public Vector2 obUVoffset { get; private set; }
        public Vector2 obUVscale { get; private set; }
        public int idealWidth; //all textures will be resized to this size
        public int idealHeight;

        private PipelineVariation pipelineVariation;

        internal DRect obUVrect
        {
            get { return new DRect(obUVoffset, obUVscale); }
        }

        public MB_TexSet(MeshBakerMaterialTexture[] tss, Vector2 uvOffset, Vector2 uvScale, MB_TextureTilingTreatment treatment)
        {
            ts = tss;
            tilingTreatment = treatment;
            obUVoffset = uvOffset;
            obUVscale = uvScale;
            allTexturesUseSameMatTiling = false;
            thisIsOnlyTexSetInAtlas = false;
            matsAndGOs = new MatsAndGOs();
            matsAndGOs.mats = new List<MatAndTransformToMerged>();
            matsAndGOs.gos = new List<GameObject>();
            pipelineVariation = new PipelineVariationSomeTexturesUseDifferentMatTiling(this);
        }

        // The two texture sets are equal if they are using the same 
        // textures/color properties for each map and have the same
        // tiling for each of those color properties
        internal bool IsEqual(object obj, bool fixOutOfBoundsUVs, MB3_TextureCombinerNonTextureProperties resultMaterialTextureBlender)
        {
            if (!(obj is MB_TexSet))
            {
                return false;
            }
            MB_TexSet other = (MB_TexSet)obj;
            if (other.ts.Length != ts.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < ts.Length; i++)
                {
                    if (ts[i].matTilingRect != other.ts[i].matTilingRect)
                        return false;
                    if (!ts[i].AreTexturesEqual(other.ts[i]))
                        return false;

                    if (!resultMaterialTextureBlender.NonTexturePropertiesAreEqual(matsAndGOs.mats[0].mat, other.matsAndGOs.mats[0].mat))
                    {
                        return false;
                    }
                }

                //IMPORTANT don't use Vector2 != Vector2 because it is only acurate to about 5 decimal places
                //this can lead to tiled rectangles that can't accept rectangles.
                if (fixOutOfBoundsUVs && (obUVoffset.x != other.obUVoffset.x ||
                                            obUVoffset.y != other.obUVoffset.y))
                    return false;
                if (fixOutOfBoundsUVs && (obUVscale.x != other.obUVscale.x ||
                                            obUVscale.y != other.obUVscale.y))
                    return false;
                return true;
            }
        }

        public Vector2 GetMaxRawTextureHeightWidth()
        {
            Vector2 max = new Vector2(0, 0);
            for (int propIdx = 0; propIdx < ts.Length; propIdx++)
            {
                MeshBakerMaterialTexture tx = ts[propIdx];
                if (!tx.isNull)
                {
                    max.x = Mathf.Max(max.x, tx.width);
                    max.y = Mathf.Max(max.y, tx.height);
                }
            }
            return max;
        }

        private Rect GetEncapsulatingSamplingRectIfTilingSame()
        {
            Debug.Assert(allTexturesUseSameMatTiling, "This should never be called if different properties use different tiling. ");
            if (ts.Length > 0)
            {
                return ts[0].GetEncapsulatingSamplingRect().GetRect();
            }
            return new Rect(0, 0, 1, 1);
        }

        public void SetEncapsulatingSamplingRectWhenMergingTexSets(DRect newEncapsulatingSamplingRect)
        {
            Debug.Assert(allTexturesUseSameMatTiling, "This should never be called if different properties use different tiling. ");
            for (int propIdx = 0; propIdx < ts.Length; propIdx++)
            {
                ts[propIdx].SetEncapsulatingSamplingRect(this, newEncapsulatingSamplingRect);
            }
        }

        public void SetEncapsulatingSamplingRectForTesting(int propIdx, DRect newEncapsulatingSamplingRect)
        {
            ts[propIdx].SetEncapsulatingSamplingRect(this, newEncapsulatingSamplingRect);
        }

        public void SetEncapsulatingRect(int propIdx, bool considerMeshUVs)
        {
            if (considerMeshUVs)
            {
                ts[propIdx].SetEncapsulatingSamplingRect(this, obUVrect);
            }
            else
            {
                ts[propIdx].SetEncapsulatingSamplingRect(this, new DRect(0, 0, 1, 1));
            }
        }

        public void CreateColoredTexToReplaceNull(string propName, int propIdx, bool considerMeshUVs, MB3_TextureCombiner combiner, Color col, bool isLinear)
        {
            MeshBakerMaterialTexture matTex = ts[propIdx];
            Texture2D tt = combiner._createTemporaryTexture(propName, 16, 16, TextureFormat.ARGB32, true, isLinear);
            matTex.t = tt;
            MB_Utility.setSolidColor(matTex.GetTexture2D(), col);
        }

        public void SetThisIsOnlyTexSetInAtlasTrue()
        {
            Debug.Assert(thisIsOnlyTexSetInAtlas == false);
            thisIsOnlyTexSetInAtlas = true;
        }

        public void SetAllTexturesUseSameMatTilingTrue()
        {
            Debug.Assert(allTexturesUseSameMatTiling == false);
            allTexturesUseSameMatTiling = true;
            pipelineVariation = new PipelineVariationAllTexturesUseSameMatTiling(this);
        }

        public void AdjustResultMaterialNonTextureProperties(Material resultMaterial, List<ShaderTextureProperty> props)
        {
            pipelineVariation.AdjustResultMaterialNonTextureProperties(resultMaterial, props);
        }

        public void SetTilingTreatmentAndAdjustEncapsulatingSamplingRect(MB_TextureTilingTreatment newTilingTreatment)
        {
            tilingTreatment = newTilingTreatment;
            pipelineVariation.SetTilingTreatmentAndAdjustEncapsulatingSamplingRect(newTilingTreatment);
        }


        internal void GetRectsForTextureBakeResults(out Rect allPropsUseSameTiling_encapsulatingSamplingRect,
                                                    out Rect propsUseDifferntTiling_obUVRect)
        {
            pipelineVariation.GetRectsForTextureBakeResults(out allPropsUseSameTiling_encapsulatingSamplingRect, out propsUseDifferntTiling_obUVRect);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="materialIndex">Should be an index in matsAndGOs.mats List</param>
        /// <returns></returns>
        internal Rect GetMaterialTilingRectForTextureBakerResults(int materialIndex)
        {
            return pipelineVariation.GetMaterialTilingRectForTextureBakerResults(materialIndex);
        }

        //assumes all materials use the same obUVrects.
        internal void CalcInitialFullSamplingRects(bool fixOutOfBoundsUVs)
        {
            DRect validFullSamplingRect = new Core.DRect(0, 0, 1, 1);
            if (fixOutOfBoundsUVs)
            {
                validFullSamplingRect = obUVrect;
            }

            for (int propIdx = 0; propIdx < ts.Length; propIdx++)
            {
                if (!ts[propIdx].isNull)
                {
                    DRect matTiling = ts[propIdx].matTilingRect;
                    DRect ruv;
                    if (fixOutOfBoundsUVs)
                    {
                        ruv = obUVrect;
                    }
                    else
                    {
                        ruv = new DRect(0.0, 0.0, 1.0, 1.0);
                    }

                    ts[propIdx].SetEncapsulatingSamplingRect(this, MB3_UVTransformUtility.CombineTransforms(ref ruv, ref matTiling));
                    validFullSamplingRect = ts[propIdx].GetEncapsulatingSamplingRect();
                }
            }

            //if some of the textures were null make them match the sampling of one of the other textures
            for (int propIdx = 0; propIdx < ts.Length; propIdx++)
            {
                if (ts[propIdx].isNull)
                {
                    ts[propIdx].SetEncapsulatingSamplingRect(this, validFullSamplingRect);
                }
            }
        }

        internal void CalcMatAndUVSamplingRects()
        {
            DRect matTiling = new DRect(0f, 0f, 1f, 1f);
            if (allTexturesUseSameMatTiling)
            {

                for (int propIdx = 0; propIdx < ts.Length; propIdx++)
                {
                    if (!ts[propIdx].isNull)
                    {
                        matTiling = ts[propIdx].matTilingRect;
                        break;
                    }
                }
            }

            for (int matIdx = 0; matIdx < matsAndGOs.mats.Count; matIdx++)
            {
                matsAndGOs.mats[matIdx].AssignInitialValuesForMaterialTilingAndSamplingRectMatAndUVTiling(allTexturesUseSameMatTiling, matTiling);
            }
        }

        public bool AllTexturesAreSameForMerge(MB_TexSet other, bool considerNonTextureProperties, MB3_TextureCombinerNonTextureProperties resultMaterialTextureBlender)
        {
            if (other.ts.Length != ts.Length)
            {
                return false;
            }
            else
            {
                if (!other.allTexturesUseSameMatTiling || !allTexturesUseSameMatTiling)
                {
                    return false;
                }
                // must use same set of textures
                int idxOfFirstNoneNull = -1;
                for (int i = 0; i < ts.Length; i++)
                {
                    if (!ts[i].AreTexturesEqual(other.ts[i]))
                        return false;
                    if (idxOfFirstNoneNull == -1 && !ts[i].isNull)
                    {
                        idxOfFirstNoneNull = i;
                    }
                    if (considerNonTextureProperties)
                    {
                        if (!resultMaterialTextureBlender.NonTexturePropertiesAreEqual(matsAndGOs.mats[0].mat, other.matsAndGOs.mats[0].mat))
                        {
                            return false;
                        }
                    }
                }
                if (idxOfFirstNoneNull != -1)
                {
                    //check that all textures are the same. Have already checked all tiling is same
                    for (int i = 0; i < ts.Length; i++)
                    {
                        if (!ts[i].AreTexturesEqual(other.ts[i]))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        public void DrawRectsToMergeGizmos(Color encC, Color innerC)
        {
            DRect r = ts[0].GetEncapsulatingSamplingRect();
            r.Expand(.05f);
            Gizmos.color = encC;
            Gizmos.DrawWireCube(r.center.GetVector2(), r.size);
            for (int i = 0; i < matsAndGOs.mats.Count; i++)
            {
                DRect rr = matsAndGOs.mats[i].samplingRectMatAndUVTiling;
                DRect trans = MB3_UVTransformUtility.GetShiftTransformToFitBinA(ref r, ref rr);
                Vector2 xy = MB3_UVTransformUtility.TransformPoint(ref trans, rr.min);
                rr.x = xy.x;
                rr.y = xy.y;
                //Debug.Log("r " + r + " rr" + rr);
                Gizmos.color = innerC;
                Gizmos.DrawWireCube(rr.center.GetVector2(), rr.size);
            }
        }

        internal string GetDescription()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[GAME_OBJS=");
            for (int i = 0; i < matsAndGOs.gos.Count; i++)
            {
                sb.AppendFormat("{0},", matsAndGOs.gos[i].name);
            }
            sb.AppendFormat("MATS=");
            for (int i = 0; i < matsAndGOs.mats.Count; i++)
            {
                sb.AppendFormat("{0},", matsAndGOs.mats[i].GetMaterialName());
            }
            sb.Append("]");
            return sb.ToString();
        }

        internal string GetMatSubrectDescriptions()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < matsAndGOs.mats.Count; i++)
            {
                sb.AppendFormat("\n    {0}={1},", matsAndGOs.mats[i].GetMaterialName(), matsAndGOs.mats[i].samplingRectMatAndUVTiling);
            }
            return sb.ToString();
        }
    }

    /*
    class ProceduralMaterialInfo
    {
        //public ProceduralMaterial proceduralMat;
        public bool originalIsReadableVal;
    }
    */

}
