//----------------------------------------------
//            MeshBaker
// Copyright © 2011-2012 Ian Deane
//----------------------------------------------
using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

/*
  
Notes on Normal Maps in Unity3d

Unity stores normal maps in a non standard format for some platforms. Think of the standard format as being english, unity's as being
french. The raw image files in the project folder are in english, the AssetImporter converts them to french. Texture2D.GetPixels returns 
french. This is a problem when we build an atlas from Texture2D objects and save the result in the project folder.
Unity wants us to flag this file as a normal map but if we do it is effectively translated twice.

Solutions:

    1) convert the normal map to english just before saving to project. Then set the normal flag and let the Importer do translation.
    This was rejected because Unity doesn't translate for all platforms. I would need to check with every version of Unity which platforms
    use which format.

    2) Uncheck "normal map" on importer before bake and re-check after bake. This is the solution I am using.

*/
namespace DigitalOpus.MB.Core
{

    [System.Serializable]
    public class ShaderTextureProperty
    {
        public string name;
        public bool isNormalMap;

        /// <summary>
        /// If we find a texture property in the result material we don't know if it is normal or not
        /// We can try to look at how it is used in the source materials. If the majority of those are normal
        /// then it is normal.
        /// </summary>
        [HideInInspector]
        public bool isNormalDontKnow = false;

        public ShaderTextureProperty(string n,
                                     bool norm)
        {
            name = n;
            isNormalMap = norm;
            isNormalDontKnow = false;
        }

        public ShaderTextureProperty(string n,
                                     bool norm,
                                     bool isNormalDontKnow)
        {
            name = n;
            isNormalMap = norm;
            this.isNormalDontKnow = isNormalDontKnow;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ShaderTextureProperty)) return false;
            ShaderTextureProperty b = (ShaderTextureProperty)obj;
            if (!name.Equals(b.name)) return false;
            if (isNormalMap != b.isNormalMap) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static string[] GetNames(List<ShaderTextureProperty> props)
        {
            string[] ss = new string[props.Count];
            for (int i = 0; i < ss.Length; i++)
            {
                ss[i] = props[i].name;
            }
            return ss;
        }
    }

    [System.Serializable]
    public class MB3_TextureCombiner
    {
        public class CreateAtlasesCoroutineResult
        {
            public bool success = true;
            public bool isFinished = false;
        }

        internal class TemporaryTexture
        {
            internal string property;
            internal Texture2D texture;

            public TemporaryTexture(string prop, Texture2D tex)
            {
                property = prop;
                texture = tex;
            }
        }

        /**
         Same as CombineTexturesIntoAtlases except this version runs as a coroutine to spread the load of baking textures at runtime across several frames
         */

        public class CombineTexturesIntoAtlasesCoroutineResult
        {
            public bool success = true;
            public bool isFinished = false;
        }

        public MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info;

        [SerializeField]
        protected MB2_TextureBakeResults _textureBakeResults;
        public MB2_TextureBakeResults textureBakeResults
        {
            get { return _textureBakeResults; }
            set { _textureBakeResults = value; }
        }

        [SerializeField]
        protected int _atlasPadding = 1;
        public int atlasPadding
        {
            get { return _atlasPadding; }
            set { _atlasPadding = value; }
        }

        [SerializeField]
        protected int _maxAtlasSize = 1;
        public int maxAtlasSize
        {
            get { return _maxAtlasSize; }
            set { _maxAtlasSize = value; }
        }

        [SerializeField]
        protected int _maxAtlasWidthOverride = 4096;
        public virtual int maxAtlasWidthOverride
        {
            get { return _maxAtlasWidthOverride; }
            set { _maxAtlasWidthOverride = value; }
        }

        [SerializeField]
        protected int _maxAtlasHeightOverride = 4096;
        public virtual int maxAtlasHeightOverride
        {
            get { return _maxAtlasHeightOverride; }
            set { _maxAtlasHeightOverride = value; }
        }

        [SerializeField]
        protected bool _useMaxAtlasWidthOverride = false;
        public virtual bool useMaxAtlasWidthOverride
        {
            get { return _useMaxAtlasWidthOverride; }
            set { _useMaxAtlasWidthOverride = value; }
        }

        [SerializeField]
        protected bool _useMaxAtlasHeightOverride = false;
        public virtual bool useMaxAtlasHeightOverride
        {
            get { return _useMaxAtlasHeightOverride; }
            set { _useMaxAtlasHeightOverride = value; }
        }

        [SerializeField]
        protected bool _resizePowerOfTwoTextures = false;
        public bool resizePowerOfTwoTextures
        {
            get { return _resizePowerOfTwoTextures; }
            set { _resizePowerOfTwoTextures = value; }
        }

        [SerializeField]
        protected bool _fixOutOfBoundsUVs = false;
        public bool fixOutOfBoundsUVs
        {
            get { return _fixOutOfBoundsUVs; }
            set { _fixOutOfBoundsUVs = value; }
        }

        [SerializeField]
        protected int _layerTexturePackerFastMesh = -1;
        public int layerTexturePackerFastMesh
        {
            get { return _layerTexturePackerFastMesh; }
            set { _layerTexturePackerFastMesh = value; }
        }

        [SerializeField]
        protected int _maxTilingBakeSize = 1024;
        public int maxTilingBakeSize
        {
            get { return _maxTilingBakeSize; }
            set { _maxTilingBakeSize = value; }
        }

        [SerializeField]
        protected bool _saveAtlasesAsAssets = false;
        public bool saveAtlasesAsAssets
        {
            get { return _saveAtlasesAsAssets; }
            set { _saveAtlasesAsAssets = value; }
        }

        [SerializeField]
        protected MB2_TextureBakeResults.ResultType _resultType;

        public MB2_TextureBakeResults.ResultType resultType
        {
            get { return _resultType; }
            set { _resultType = value; }
        }

        [SerializeField]
        protected MB2_PackingAlgorithmEnum _packingAlgorithm = MB2_PackingAlgorithmEnum.UnitysPackTextures;
        public MB2_PackingAlgorithmEnum packingAlgorithm
        {
            get { return _packingAlgorithm; }
            set { _packingAlgorithm = value; }
        }

        [SerializeField]
        protected bool _meshBakerTexturePackerForcePowerOfTwo = true;
        public bool meshBakerTexturePackerForcePowerOfTwo
        {
            get { return _meshBakerTexturePackerForcePowerOfTwo; }
            set { _meshBakerTexturePackerForcePowerOfTwo = value; }
        }

        [SerializeField]
        protected List<ShaderTextureProperty> _customShaderPropNames = new List<ShaderTextureProperty>();
        public List<ShaderTextureProperty> customShaderPropNames
        {
            get { return _customShaderPropNames; }
            set { _customShaderPropNames = value; }
        }

        [SerializeField]
        protected bool _normalizeTexelDensity = false;

        [SerializeField]
        protected bool _considerNonTextureProperties = false;
        public bool considerNonTextureProperties
        {
            get { return _considerNonTextureProperties; }
            set { _considerNonTextureProperties = value; }
        }

        // Don't Serialize this. It should only be used in specific circumstances.
        protected bool _doMergeDistinctMaterialTexturesThatWouldExceedAtlasSize = false;
        public bool doMergeDistinctMaterialTexturesThatWouldExceedAtlasSize
        {
            get { return _doMergeDistinctMaterialTexturesThatWouldExceedAtlasSize; }
            set { _doMergeDistinctMaterialTexturesThatWouldExceedAtlasSize = value; }
        }

        //copies of textures created for the the atlas baking that should be destroyed in finalize
        private List<TemporaryTexture> _temporaryTextures = new List<TemporaryTexture>();

        //so we can undo read flag on procedural materials in finalize
        //internal List<ProceduralMaterialInfo> _proceduralMaterials = new List<ProceduralMaterialInfo>();

        //This runs a coroutine without pausing it is used to build the textures from the editor
        public static bool _RunCorutineWithoutPauseIsRunning = false;
        public static void RunCorutineWithoutPause(IEnumerator cor, int recursionDepth)
        {
            if (recursionDepth == 0)
            {
                _RunCorutineWithoutPauseIsRunning = true;
            }

            if (recursionDepth > 20)
            {
                Debug.LogError("Recursion Depth Exceeded.");
                return;
            }

            while (cor.MoveNext())
            {
                object retObj = cor.Current;
                if (retObj is YieldInstruction)
                {
                    //do nothing
                }
                else if (retObj == null)
                {
                    //do nothing
                }
                else if (retObj is IEnumerator)
                {
                    RunCorutineWithoutPause((IEnumerator)cor.Current, recursionDepth + 1);
                }
            }
            if (recursionDepth == 0)
            {
                _RunCorutineWithoutPauseIsRunning = false;
            }
        }

        /**<summary>Combines meshes and generates texture atlases. NOTE running coroutines at runtime does not work in Unity 4</summary>
        *  <param name="progressInfo">A delegate function that will be called to report progress.</param>
        *  <param name="textureEditorMethods">If called from the editor should be an instance of MB2_EditorMethods. If called at runtime should be null.</param>
        *  <remarks>Combines meshes and generates texture atlases</remarks> */
        public bool CombineTexturesIntoAtlases(ProgressUpdateDelegate progressInfo, MB_AtlasesAndRects resultAtlasesAndRects, Material resultMaterial, List<GameObject> objsToMesh, List<Material> allowedMaterialsFilter, List<string> texPropsToIgnore, MB2_EditorMethodsInterface textureEditorMethods = null, List<AtlasPackingResult> packingResults = null, bool onlyPackRects = false, bool splitAtlasWhenPackingIfTooBig = false)
        {
            CombineTexturesIntoAtlasesCoroutineResult result = new CombineTexturesIntoAtlasesCoroutineResult();
            RunCorutineWithoutPause(_CombineTexturesIntoAtlases(progressInfo, result, resultAtlasesAndRects, resultMaterial, objsToMesh, allowedMaterialsFilter, texPropsToIgnore, textureEditorMethods, packingResults, onlyPackRects, splitAtlasWhenPackingIfTooBig), 0);
            if (result.success == false) Debug.LogError("Failed to generate atlases.");
            return result.success;
        }

        //float _maxTimePerFrameForCoroutine;
        public IEnumerator CombineTexturesIntoAtlasesCoroutine(ProgressUpdateDelegate progressInfo, MB_AtlasesAndRects resultAtlasesAndRects, Material resultMaterial, List<GameObject> objsToMesh, List<Material> allowedMaterialsFilter, List<string> texPropsToIgnore, MB2_EditorMethodsInterface textureEditorMethods = null, CombineTexturesIntoAtlasesCoroutineResult coroutineResult = null, float maxTimePerFrame = .01f, List<AtlasPackingResult> packingResults = null, bool onlyPackRects = false, bool splitAtlasWhenPackingIfTooBig = false)
        {
            if (!_RunCorutineWithoutPauseIsRunning && (MBVersion.GetMajorVersion() < 5 || (MBVersion.GetMajorVersion() == 5 && MBVersion.GetMinorVersion() < 3)))
            {
                Debug.LogError("Running the texture combiner as a coroutine only works in Unity 5.3 and higher");
                yield return null;
            }
            coroutineResult.success = true;
            coroutineResult.isFinished = false;
            if (maxTimePerFrame <= 0f)
            {
                Debug.LogError("maxTimePerFrame must be a value greater than zero");
                coroutineResult.isFinished = true;
                yield break;
            }
            //_maxTimePerFrameForCoroutine = maxTimePerFrame;
            yield return _CombineTexturesIntoAtlases(progressInfo, coroutineResult, resultAtlasesAndRects, resultMaterial, objsToMesh, allowedMaterialsFilter, texPropsToIgnore, textureEditorMethods, packingResults, onlyPackRects, splitAtlasWhenPackingIfTooBig);
            coroutineResult.isFinished = true;
            yield break;
        }

        IEnumerator _CombineTexturesIntoAtlases(ProgressUpdateDelegate progressInfo, CombineTexturesIntoAtlasesCoroutineResult result, MB_AtlasesAndRects resultAtlasesAndRects, Material resultMaterial, List<GameObject> objsToMesh, List<Material> allowedMaterialsFilter, List<string> texPropsToIgnore, MB2_EditorMethodsInterface textureEditorMethods, List<AtlasPackingResult> atlasPackingResult, bool onlyPackRects, bool splitAtlasWhenPackingIfTooBig)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            try
            {
                _temporaryTextures.Clear();
                MeshBakerMaterialTexture.readyToBuildAtlases = false;

                if (textureEditorMethods != null)
                {
                    textureEditorMethods.Clear();
                    textureEditorMethods.OnPreTextureBake();
                }

                if (splitAtlasWhenPackingIfTooBig == true &&
                    onlyPackRects == false)
                {
                    Debug.LogError("Can only use 'splitAtlasWhenPackingIfTooLarge' with 'onlyPackRects'");
                    result.success = false;
                    yield break;
                }

                if (objsToMesh == null || objsToMesh.Count == 0)
                {
                    Debug.LogError("No meshes to combine. Please assign some meshes to combine.");
                    result.success = false;
                    yield break;
                }

                if (_atlasPadding < 0)
                {
                    Debug.LogError("Atlas padding must be zero or greater.");
                    result.success = false;
                    yield break;
                }

                if (_maxTilingBakeSize < 2 || _maxTilingBakeSize > 4096)
                {
                    Debug.LogError("Invalid value for max tiling bake size.");
                    result.success = false;
                    yield break;
                }

                for (int i = 0; i < objsToMesh.Count; i++)
                {
                    Material[] ms = MB_Utility.GetGOMaterials(objsToMesh[i]);
                    for (int j = 0; j < ms.Length; j++)
                    {
                        Material m = ms[j];
                        if (m == null)
                        {
                            Debug.LogError("Game object " + objsToMesh[i] + " has a null material");
                            result.success = false;
                            yield break;
                        }

                    }
                }

                if (progressInfo != null)
                    progressInfo("Collecting textures for " + objsToMesh.Count + " meshes.", .01f);

                MB3_TextureCombinerPipeline.TexturePipelineData data = LoadPipelineData(resultMaterial, new List<ShaderTextureProperty>(), objsToMesh, allowedMaterialsFilter, texPropsToIgnore, new List<MB_TexSet>());
                if (!MB3_TextureCombinerPipeline._CollectPropertyNames(data, LOG_LEVEL))
                {
                    result.success = false;
                    yield break;
                }

                if (_fixOutOfBoundsUVs && (_packingAlgorithm == MB2_PackingAlgorithmEnum.MeshBakerTexturePacker_Horizontal ||
                                            _packingAlgorithm == MB2_PackingAlgorithmEnum.MeshBakerTexturePacker_Vertical))
                {
                    if (LOG_LEVEL >= MB2_LogLevel.info)
                    {
                        Debug.LogWarning("'Consider Mesh UVs' is enabled but packing algorithm is MeshBakerTexturePacker_Horizontal or MeshBakerTexturePacker_Vertical. It is recommended to use these packers without using 'Consider Mesh UVs'");
                    }
                }

                data.nonTexturePropertyBlender.LoadTextureBlendersIfNeeded(data.resultMaterial);

                if (onlyPackRects)
                {
                    yield return __RunTexturePackerOnly(result, resultAtlasesAndRects, data, splitAtlasWhenPackingIfTooBig, textureEditorMethods, atlasPackingResult);
                }
                else
                {
                    yield return __CombineTexturesIntoAtlases(progressInfo, result, resultAtlasesAndRects, data, textureEditorMethods);
                }
            }
            /*
            catch (MissingReferenceException mrex){
                Debug.LogError("Creating atlases failed a MissingReferenceException was thrown. This is normally only happens when trying to create very large atlases and Unity is running out of Memory. Try changing the 'Texture Packer' to a different option, it may work with an alternate packer. This error is sometimes intermittant. Try baking again.");
                Debug.LogError(mrex);
            } catch (Exception ex){
                Debug.LogError(ex);
            }
            */
            finally
            {

                _destroyAllTemporaryTextures();
                _restoreProceduralMaterials();
                if (textureEditorMethods != null)
                {
                    textureEditorMethods.RestoreReadFlagsAndFormats(progressInfo);
                    textureEditorMethods.OnPostTextureBake();
                }
                if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("===== Done creating atlases for " + resultMaterial + " Total time to create atlases " + sw.Elapsed.ToString());
            }
        }

        MB3_TextureCombinerPipeline.TexturePipelineData LoadPipelineData(Material resultMaterial,
            List<ShaderTextureProperty> texPropertyNames,
            List<GameObject> objsToMesh,
            List<Material> allowedMaterialsFilter,
            List<string> texPropsToIgnore,
            List<MB_TexSet> distinctMaterialTextures)
        {
            MB3_TextureCombinerPipeline.TexturePipelineData data = new MB3_TextureCombinerPipeline.TexturePipelineData();
            data._textureBakeResults = _textureBakeResults;
            data._atlasPadding = _atlasPadding;
            if (_packingAlgorithm == MB2_PackingAlgorithmEnum.MeshBakerTexturePacker_Vertical && _useMaxAtlasHeightOverride)
            {
                data._maxAtlasHeight = _maxAtlasHeightOverride;
                data._useMaxAtlasHeightOverride = true;
            }
            else
            {
                data._maxAtlasHeight = _maxAtlasSize;
            }

            if (_packingAlgorithm == MB2_PackingAlgorithmEnum.MeshBakerTexturePacker_Horizontal && _useMaxAtlasWidthOverride)
            {
                data._maxAtlasWidth = _maxAtlasWidthOverride;
                data._useMaxAtlasWidthOverride = true;
            }
            else
            {
                data._maxAtlasWidth = _maxAtlasSize;
            }

            data._saveAtlasesAsAssets = _saveAtlasesAsAssets;
            data.resultType = _resultType;
            data._resizePowerOfTwoTextures = _resizePowerOfTwoTextures;
            data._fixOutOfBoundsUVs = _fixOutOfBoundsUVs;
            data._maxTilingBakeSize = _maxTilingBakeSize;
            data._packingAlgorithm = _packingAlgorithm;
            data._layerTexturePackerFastV2 = _layerTexturePackerFastMesh;
            data._meshBakerTexturePackerForcePowerOfTwo = _meshBakerTexturePackerForcePowerOfTwo;
            data._customShaderPropNames = _customShaderPropNames;
            data._normalizeTexelDensity = _normalizeTexelDensity;
            data._considerNonTextureProperties = _considerNonTextureProperties;
            data.doMergeDistinctMaterialTexturesThatWouldExceedAtlasSize = _doMergeDistinctMaterialTexturesThatWouldExceedAtlasSize;
            data.nonTexturePropertyBlender = new MB3_TextureCombinerNonTextureProperties(LOG_LEVEL, _considerNonTextureProperties);
            data.resultMaterial = resultMaterial;
            data.distinctMaterialTextures = distinctMaterialTextures;
            data.allObjsToMesh = objsToMesh;
            data.allowedMaterialsFilter = allowedMaterialsFilter;
            data.texPropertyNames = texPropertyNames;
            data.texPropNamesToIgnore = texPropsToIgnore;
            data.colorSpace = MBVersion.GetProjectColorSpace();
            return data;
        }

        //texPropertyNames is the list of texture properties in the resultMaterial
        //allowedMaterialsFilter is a list of materials. Objects without any of these materials will be ignored.
        //						 this is used by the multiple materials filter
        //textureEditorMethods encapsulates editor only functionality such as saving assets and tracking texture assets whos format was changed. Is null if using at runtime. 
        IEnumerator __CombineTexturesIntoAtlases(ProgressUpdateDelegate progressInfo, CombineTexturesIntoAtlasesCoroutineResult result, MB_AtlasesAndRects resultAtlasesAndRects, MB3_TextureCombinerPipeline.TexturePipelineData data, MB2_EditorMethodsInterface textureEditorMethods)
        {
            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("__CombineTexturesIntoAtlases texture properties in shader:" + data.texPropertyNames.Count + " objsToMesh:" + data.allObjsToMesh.Count + " _fixOutOfBoundsUVs:" + data._fixOutOfBoundsUVs);

            if (progressInfo != null) progressInfo("Collecting textures ", .01f);

            MB3_TextureCombinerPipeline pipeline = new MB3_TextureCombinerPipeline();
            /*
            each atlas (maintex, bump, spec etc...) will have distinctMaterialTextures.Count images in it.
            each distinctMaterialTextures record is a set of textures, one for each atlas. And a list of materials
            that use that distinct set of textures. 
            */
            List<GameObject> usedObjsToMesh = new List<GameObject>();
            yield return pipeline.__Step1_CollectDistinctMatTexturesAndUsedObjects(progressInfo, result, data, this, textureEditorMethods, usedObjsToMesh, LOG_LEVEL);
            if (!result.success)
            {
                yield break;
            }

            //Textures in each material (_mainTex, Bump, Spec ect...) must be same size
            //Calculate the best sized to use. Takes into account tiling
            //if only one texture in atlas re-uses original sizes	
            yield return pipeline.CalculateIdealSizesForTexturesInAtlasAndPadding(progressInfo, result, data, this, textureEditorMethods, LOG_LEVEL);
            if (!result.success)
            {
                yield break;
            }

            //buildAndSaveAtlases
            StringBuilder report = pipeline.GenerateReport(data);
            MB_ITextureCombinerPacker texturePaker = pipeline.CreatePacker(data.OnlyOneTextureInAtlasReuseTextures(), data._packingAlgorithm);
            if (!texturePaker.Validate(data))
            {
                result.success = false;
                yield break;
            }

            yield return texturePaker.ConvertTexturesToReadableFormats(progressInfo, result, data, this, textureEditorMethods, LOG_LEVEL);
            if (!result.success)
            {
                yield break;
            }

            AtlasPackingResult[] uvRects = texturePaker.CalculateAtlasRectangles(data, false, LOG_LEVEL);
            Debug.Assert(uvRects.Length == 1, "Error, there should not be more than one packing here.");
            yield return pipeline.__Step3_BuildAndSaveAtlasesAndStoreResults(result, progressInfo, data, this, texturePaker, uvRects[0], textureEditorMethods, resultAtlasesAndRects, report, LOG_LEVEL);
        }

        IEnumerator __RunTexturePackerOnly(CombineTexturesIntoAtlasesCoroutineResult result, MB_AtlasesAndRects resultAtlasesAndRects, MB3_TextureCombinerPipeline.TexturePipelineData data, bool splitAtlasWhenPackingIfTooBig, MB2_EditorMethodsInterface textureEditorMethods, List<AtlasPackingResult> packingResult)
        {
            MB3_TextureCombinerPipeline pipeline = new MB3_TextureCombinerPipeline();
            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("__RunTexturePacker texture properties in shader:" + data.texPropertyNames.Count + " objsToMesh:" + data.allObjsToMesh.Count + " _fixOutOfBoundsUVs:" + data._fixOutOfBoundsUVs);
            List<GameObject> usedObjsToMesh = new List<GameObject>();
            yield return pipeline.__Step1_CollectDistinctMatTexturesAndUsedObjects(null, result, data, this, textureEditorMethods, usedObjsToMesh, LOG_LEVEL);
            if (!result.success)
            {
                yield break;
            }

            data.allTexturesAreNullAndSameColor = new MB3_TextureCombinerPipeline.CreateAtlasForProperty[data.texPropertyNames.Count];
            yield return pipeline.CalculateIdealSizesForTexturesInAtlasAndPadding(null, result, data, this, textureEditorMethods, LOG_LEVEL);
            if (!result.success)
            {
                yield break;
            }

            MB_ITextureCombinerPacker texturePaker = pipeline.CreatePacker(data.OnlyOneTextureInAtlasReuseTextures(), data._packingAlgorithm);
            //    run the texture packer only
            AtlasPackingResult[] aprs = pipeline.RunTexturePackerOnly(data, splitAtlasWhenPackingIfTooBig, resultAtlasesAndRects, texturePaker, LOG_LEVEL);
            for (int i = 0; i < aprs.Length; i++)
            {
                packingResult.Add(aprs[i]);
            }
        }

        internal int _getNumTemporaryTextures()
        {
            return _temporaryTextures.Count;
        }

        //used to track temporary textures that were created so they can be destroyed
        public Texture2D _createTemporaryTexture(string propertyName, int w, int h, TextureFormat texFormat, bool mipMaps, bool linear)
        {
            Texture2D t = new Texture2D(w, h, texFormat, mipMaps, linear);
            t.name = string.Format("tmp{0}_{1}x{2}", _temporaryTextures.Count, w, h);
            MB_Utility.setSolidColor(t, Color.clear);
            TemporaryTexture txx = new TemporaryTexture(propertyName, t);
            _temporaryTextures.Add(txx);
            return t;
        }

        internal void AddTemporaryTexture(TemporaryTexture tt)
        {
            _temporaryTextures.Add(tt);
        }

        internal Texture2D _createTextureCopy(string propertyName, Texture2D t)
        {
            Texture2D tx = MB_Utility.createTextureCopy(t);
            tx.name = string.Format("tmpCopy{0}_{1}x{2}", _temporaryTextures.Count, tx.width, tx.height);
            TemporaryTexture txx = new TemporaryTexture(propertyName, tx);
            _temporaryTextures.Add(txx);
            return tx;
        }

        internal Texture2D _resizeTexture(string propertyName, Texture2D t, int w, int h)
        {
            Texture2D tx = MB_Utility.resampleTexture(t, w, h);
            tx.name = string.Format("tmpResampled{0}_{1}x{2}", _temporaryTextures.Count, w, h);
            TemporaryTexture txx = new TemporaryTexture(propertyName, tx);
            _temporaryTextures.Add(txx);
            return tx;
        }

        internal void _destroyAllTemporaryTextures()
        {
            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Destroying " + _temporaryTextures.Count + " temporary textures");
            for (int i = 0; i < _temporaryTextures.Count; i++)
            {
                MB_Utility.Destroy(_temporaryTextures[i].texture);
            }
            _temporaryTextures.Clear();
        }

        internal void _destroyTemporaryTextures(string propertyName)
        {
            int numDestroyed = 0;
            for (int i = _temporaryTextures.Count - 1; i >= 0; i--)
            {
                if (_temporaryTextures[i].property.Equals(propertyName))
                {
                    numDestroyed++;
                    MB_Utility.Destroy(_temporaryTextures[i].texture);
                    _temporaryTextures.RemoveAt(i);
                }
            }
            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Destroying " + numDestroyed + " temporary textures " + propertyName + " num remaining " + _temporaryTextures.Count);
        }

        /*
        public void _addProceduralMaterial(ProceduralMaterial pm)
        {
            ProceduralMaterialInfo pmi = new ProceduralMaterialInfo();
            pm.isReadable = pm.isReadable;
            pmi.proceduralMat = pm;
            _proceduralMaterials.Add(pmi);
        }
        */

        public void _restoreProceduralMaterials()
        {
            /*
            for (int i = 0; i < _proceduralMaterials.Count; i++)
            {
                ProceduralMaterialInfo pmi = _proceduralMaterials[i];
                pmi.proceduralMat.isReadable = pmi.originalIsReadableVal;
                pmi.proceduralMat.RebuildTexturesImmediately();
            }
            _proceduralMaterials.Clear();
             */
        }

        public void SuggestTreatment(List<GameObject> objsToMesh, Material[] resultMaterials, List<ShaderTextureProperty> _customShaderPropNames, List<string> texPropsToIgnore)
        {
            this._customShaderPropNames = _customShaderPropNames;
            StringBuilder sb = new StringBuilder();
            Dictionary<int, MB_Utility.MeshAnalysisResult[]> meshAnalysisResultsCache = new Dictionary<int, MB_Utility.MeshAnalysisResult[]>(); //cache results
            for (int i = 0; i < objsToMesh.Count; i++)
            {
                GameObject obj = objsToMesh[i];
                if (obj == null) continue;
                Material[] ms = MB_Utility.GetGOMaterials(objsToMesh[i]);
                if (ms.Length > 1)
                { // and each material is not mapped to its own layer
                    sb.AppendFormat("\nObject {0} uses {1} materials. Possible treatments:\n", objsToMesh[i].name, ms.Length);
                    sb.AppendFormat("  1) Collapse the submeshes together into one submesh in the combined mesh. Each of the original submesh materials will map to a different UV rectangle in the atlas(es) used by the combined material.\n");
                    sb.AppendFormat("  2) Use the multiple materials feature to map submeshes in the source mesh to submeshes in the combined mesh.\n");
                }
                Mesh m = MB_Utility.GetMesh(obj);

                MB_Utility.MeshAnalysisResult[] mar;
                if (!meshAnalysisResultsCache.TryGetValue(m.GetInstanceID(), out mar))
                {
                    mar = new MB_Utility.MeshAnalysisResult[m.subMeshCount];
                    MB_Utility.doSubmeshesShareVertsOrTris(m, ref mar[0]);
                    for (int j = 0; j < m.subMeshCount; j++)
                    {
                        MB_Utility.hasOutOfBoundsUVs(m, ref mar[j], j);
                        //DRect outOfBoundsUVRect = new DRect(mar[j].uvRect);
                        mar[j].hasOverlappingSubmeshTris = mar[0].hasOverlappingSubmeshTris;
                        mar[j].hasOverlappingSubmeshVerts = mar[0].hasOverlappingSubmeshVerts;
                    }
                    meshAnalysisResultsCache.Add(m.GetInstanceID(), mar);
                }

                for (int j = 0; j < ms.Length; j++)
                {
                    if (mar[j].hasOutOfBoundsUVs)
                    {
                        DRect r = new DRect(mar[j].uvRect);
                        sb.AppendFormat("\nObject {0} submesh={1} material={2} uses UVs outside the range 0,0 .. 1,1 to create tiling that tiles the box {3},{4} .. {5},{6}. This is a problem because the UVs outside the 0,0 .. 1,1 " +
                                        "rectangle will pick up neighboring textures in the atlas. Possible Treatments:\n", obj, j, ms[j], r.x.ToString("G4"), r.y.ToString("G4"), (r.x + r.width).ToString("G4"), (r.y + r.height).ToString("G4"));
                        sb.AppendFormat("    1) Ignore the problem. The tiling may not affect result significantly.\n");
                        sb.AppendFormat("    2) Use the 'fix out of bounds UVs' feature to bake the tiling and scale the UVs to fit in the 0,0 .. 1,1 rectangle.\n");
                        sb.AppendFormat("    3) Use the Multiple Materials feature to map the material on this submesh to its own submesh in the combined mesh. No other materials should map to this submesh. This will result in only one texture in the atlas(es) and the UVs should tile correctly.\n");
                        sb.AppendFormat("    4) Combine only meshes that use the same (or subset of) the set of materials on this mesh. The original material(s) can be applied to the result\n");
                    }
                }
                if (mar[0].hasOverlappingSubmeshVerts)
                {
                    sb.AppendFormat("\nObject {0} has submeshes that share vertices. This is a problem because each vertex can have only one UV coordinate and may be required to map to different positions in the various atlases that are generated. Possible treatments:\n", objsToMesh[i]);
                    sb.AppendFormat(" 1) Ignore the problem. The vertices may not affect the result.\n");
                    sb.AppendFormat(" 2) Use the Multiple Materials feature to map the submeshs that overlap to their own submeshs in the combined mesh. No other materials should map to this submesh. This will result in only one texture in the atlas(es) and the UVs should tile correctly.\n");
                    sb.AppendFormat(" 3) Combine only meshes that use the same (or subset of) the set of materials on this mesh. The original material(s) can be applied to the result\n");
                }
            }
            Dictionary<Material, List<GameObject>> m2gos = new Dictionary<Material, List<GameObject>>();
            for (int i = 0; i < objsToMesh.Count; i++)
            {
                if (objsToMesh[i] != null)
                {
                    Material[] ms = MB_Utility.GetGOMaterials(objsToMesh[i]);
                    for (int j = 0; j < ms.Length; j++)
                    {
                        if (ms[j] != null)
                        {
                            List<GameObject> lgo;
                            if (!m2gos.TryGetValue(ms[j], out lgo))
                            {
                                lgo = new List<GameObject>();
                                m2gos.Add(ms[j], lgo);
                            }
                            if (!lgo.Contains(objsToMesh[i])) lgo.Add(objsToMesh[i]);
                        }
                    }
                }
            }

            for (int i = 0; i < resultMaterials.Length; i++)
            {
                string resultMatShaderName = resultMaterials[i] != null ? "None" : resultMaterials[i].shader.name;
                MB3_TextureCombinerPipeline.TexturePipelineData data = LoadPipelineData(resultMaterials[i], new List<ShaderTextureProperty>(), objsToMesh, new List<Material>(), texPropsToIgnore, new List<MB_TexSet>());
                MB3_TextureCombinerPipeline._CollectPropertyNames(data, LOG_LEVEL);
                foreach (Material m in m2gos.Keys)
                {
                    for (int j = 0; j < data.texPropertyNames.Count; j++)
                    {
                        if (m.HasProperty(data.texPropertyNames[j].name))
                        {
                            Texture txx = MB3_TextureCombinerPipeline.GetTextureConsideringStandardShaderKeywords(resultMatShaderName, m, data.texPropertyNames[j].name);
                            if (txx != null)
                            {
                                Vector2 o = m.GetTextureOffset(data.texPropertyNames[j].name);
                                Vector3 s = m.GetTextureScale(data.texPropertyNames[j].name);
                                if (o.x < 0f || o.x + s.x > 1f ||
                                    o.y < 0f || o.y + s.y > 1f)
                                {
                                    sb.AppendFormat("\nMaterial {0} used by objects {1} uses texture {2} that is tiled (scale={3} offset={4}). If there is more than one texture in the atlas " +
                                                        " then Mesh Baker will bake the tiling into the atlas. If the baked tiling is large then quality can be lost. Possible treatments:\n", m, PrintList(m2gos[m]), txx, s, o);
                                    sb.AppendFormat("  1) Use the baked tiling.\n");
                                    sb.AppendFormat("  2) Use the Multiple Materials feature to map the material on this object/submesh to its own submesh in the combined mesh. No other materials should map to this submesh. The original material can be applied to this submesh.\n");
                                    sb.AppendFormat("  3) Combine only meshes that use the same (or subset of) the set of textures on this mesh. The original material can be applied to the result.\n");
                                }
                            }
                        }
                    }
                }
            }
            string outstr = "";
            if (sb.Length == 0)
            {
                outstr = "====== No problems detected. These meshes should combine well ====\n  If there are problems with the combined meshes please report the problem to digitalOpus.ca so we can improve Mesh Baker.";
            }
            else
            {
                outstr = "====== There are possible problems with these meshes that may prevent them from combining well. TREATMENT SUGGESTIONS (copy and paste to text editor if too big) =====\n" + sb.ToString();
            }
            Debug.Log(outstr);
        }

        public static bool ShouldTextureBeLinear(ShaderTextureProperty shaderTextureProperty)
        {
            if (shaderTextureProperty.isNormalMap) return true;
            else return false;
        }

        string PrintList(List<GameObject> gos)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < gos.Count; i++)
            {
                sb.Append(gos[i] + ",");
            }
            return sb.ToString();
        }

    }
}
