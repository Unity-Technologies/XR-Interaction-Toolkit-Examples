//----------------------------------------------
//            MeshBaker
// Copyright © 2011-2012 Ian Deane
//----------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DigitalOpus.MB.Core;



/// <summary>
/// Component that handles baking materials into a combined material.
/// 
/// The result of the material baking process is a MB2_TextureBakeResults object, which 
/// becomes the input for the mesh baking.
/// 
/// This class uses the MB_TextureCombiner to do the combining.
/// 
/// This class is a Component (MonoBehavior) so it is serialized and found using GetComponent. If
/// you want to access the texture baking functionality without creating a Component then use MB_TextureCombiner
/// directly.
/// </summary>
public class MB3_TextureBaker : MB3_MeshBakerRoot
{
    public MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info;

    [SerializeField]
    protected MB2_TextureBakeResults _textureBakeResults;
    public override MB2_TextureBakeResults textureBakeResults
    {
        get { return _textureBakeResults; }
        set { _textureBakeResults = value; }
    }

    [SerializeField]
    protected int _atlasPadding = 1;
    public virtual int atlasPadding
    {
        get { return _atlasPadding; }
        set { _atlasPadding = value; }
    }

    [SerializeField]
    protected int _maxAtlasSize = 4096;
    public virtual int maxAtlasSize
    {
        get { return _maxAtlasSize; }
        set { _maxAtlasSize = value; }
    }

    [SerializeField]
    protected bool _useMaxAtlasWidthOverride = false;
    public virtual bool useMaxAtlasWidthOverride
    {
        get { return _useMaxAtlasWidthOverride; }
        set { _useMaxAtlasWidthOverride = value; }
    }

    [SerializeField]
    protected int _maxAtlasWidthOverride = 4096;
    public virtual int maxAtlasWidthOverride
    {
        get { return _maxAtlasWidthOverride; }
        set { _maxAtlasWidthOverride = value; }
    }

    [SerializeField]
    protected bool _useMaxAtlasHeightOverride = false;
    public virtual bool useMaxAtlasHeightOverride
    {
        get { return _useMaxAtlasHeightOverride; }
        set { _useMaxAtlasHeightOverride = value; }
    }

    [SerializeField]
    protected int _maxAtlasHeightOverride = 4096;
    public virtual int maxAtlasHeightOverride
    {
        get { return _maxAtlasHeightOverride; }
        set { _maxAtlasHeightOverride = value; }
    }

    [SerializeField]
    protected bool _resizePowerOfTwoTextures = false;
    public virtual bool resizePowerOfTwoTextures
    {
        get { return _resizePowerOfTwoTextures; }
        set { _resizePowerOfTwoTextures = value; }
    }

    [SerializeField]
    protected bool _fixOutOfBoundsUVs = false; //is considerMeshUVs but can't change because it would break all existing bakers
    public virtual bool fixOutOfBoundsUVs
    {
        get { return _fixOutOfBoundsUVs; }
        set { _fixOutOfBoundsUVs = value; }
    }

    [SerializeField]
    protected int _maxTilingBakeSize = 1024;
    public virtual int maxTilingBakeSize
    {
        get { return _maxTilingBakeSize; }
        set { _maxTilingBakeSize = value; }
    }

    [SerializeField]
    protected MB2_PackingAlgorithmEnum _packingAlgorithm = MB2_PackingAlgorithmEnum.MeshBakerTexturePacker;
    public virtual MB2_PackingAlgorithmEnum packingAlgorithm
    {
        get { return _packingAlgorithm; }
        set { _packingAlgorithm = value; }
    }

    [SerializeField]
    protected int _layerTexturePackerFastMesh = -1;
    public virtual int layerForTexturePackerFastMesh
    {
        get { return _layerTexturePackerFastMesh; }
        set { _layerTexturePackerFastMesh = value; }
    }

    [SerializeField]
    protected bool _meshBakerTexturePackerForcePowerOfTwo = true;
    public bool meshBakerTexturePackerForcePowerOfTwo
    {
        get { return _meshBakerTexturePackerForcePowerOfTwo; }
        set { _meshBakerTexturePackerForcePowerOfTwo = value; }
    }

    [SerializeField]
    protected List<ShaderTextureProperty> _customShaderProperties = new List<ShaderTextureProperty>();
    public virtual List<ShaderTextureProperty> customShaderProperties
    {
        get { return _customShaderProperties; }
        set { _customShaderProperties = value; }
    }

    
    [SerializeField]
    protected List<string> _texturePropNamesToIgnore = new List<string>();
    public virtual List<string> texturePropNamesToIgnore
    {
        get { return _texturePropNamesToIgnore; }
        set { _texturePropNamesToIgnore = value; }
    }

    //this is depricated
    [SerializeField]
    protected List<string> _customShaderPropNames_Depricated = new List<string>();
    public virtual List<string> customShaderPropNames
    {
        get { return _customShaderPropNames_Depricated; }
        set { _customShaderPropNames_Depricated = value; }
    }

    [SerializeField]
    protected MB2_TextureBakeResults.ResultType _resultType;
    public virtual MB2_TextureBakeResults.ResultType resultType
    {
        get { return _resultType; }
        set { _resultType = value; }
    }

    [SerializeField]
    protected bool _doMultiMaterial;
    public virtual bool doMultiMaterial
    {
        get { return _doMultiMaterial; }
        set { _doMultiMaterial = value; }
    }

    [SerializeField]
    protected bool _doMultiMaterialSplitAtlasesIfTooBig = true;
    public virtual bool doMultiMaterialSplitAtlasesIfTooBig
    {
        get { return _doMultiMaterialSplitAtlasesIfTooBig; }
        set { _doMultiMaterialSplitAtlasesIfTooBig = value; }
    }

    [SerializeField]
    protected bool _doMultiMaterialSplitAtlasesIfOBUVs = true;
    public virtual bool doMultiMaterialSplitAtlasesIfOBUVs
    {
        get { return _doMultiMaterialSplitAtlasesIfOBUVs; }
        set { _doMultiMaterialSplitAtlasesIfOBUVs = value; }
    }

    [SerializeField]
    protected Material _resultMaterial;
    public virtual Material resultMaterial
    {
        get { return _resultMaterial; }
        set { _resultMaterial = value; }
    }

    [SerializeField]
    protected bool _considerNonTextureProperties = false;
    public bool considerNonTextureProperties
    {
        get { return _considerNonTextureProperties; }
        set { _considerNonTextureProperties = value; }
    }

    [SerializeField]
    protected bool _doSuggestTreatment = true;
    public bool doSuggestTreatment
    {
        get { return _doSuggestTreatment; }
        set { _doSuggestTreatment = value; }
    }

    private MB3_TextureCombiner.CreateAtlasesCoroutineResult _coroutineResult;
    public MB3_TextureCombiner.CreateAtlasesCoroutineResult CoroutineResult
    {
        get
        {
            return _coroutineResult;
        }
    }

    public MB_MultiMaterial[] resultMaterials = new MB_MultiMaterial[0];

    public MB_MultiMaterialTexArray[] resultMaterialsTexArray = new MB_MultiMaterialTexArray[0];

    public MB_TextureArrayFormatSet[] textureArrayOutputFormats;

    public List<GameObject> objsToMesh; //todo make this Renderer

    public override List<GameObject> GetObjectsToCombine()
    {
        if (objsToMesh == null) objsToMesh = new List<GameObject>();
        return objsToMesh;
    }

    [ContextMenu("Purge Objects to Combine of null references")]
    public override void PurgeNullsFromObjectsToCombine()
    {
        if (objsToMesh == null)
        {
            objsToMesh = new List<GameObject>();
        }
        Debug.Log(string.Format("Purged {0} null references from objects to combine list.", objsToMesh.RemoveAll(obj => obj == null)));
    }

    public MB_AtlasesAndRects[] CreateAtlases()
    {
        return CreateAtlases(null, false, null);
    }

    public delegate void OnCombinedTexturesCoroutineSuccess();
    public delegate void OnCombinedTexturesCoroutineFail();
    public OnCombinedTexturesCoroutineSuccess onBuiltAtlasesSuccess;
    public OnCombinedTexturesCoroutineFail onBuiltAtlasesFail;
    public MB_AtlasesAndRects[] OnCombinedTexturesCoroutineAtlasesAndRects;

    public IEnumerator CreateAtlasesCoroutine(ProgressUpdateDelegate progressInfo, MB3_TextureCombiner.CreateAtlasesCoroutineResult coroutineResult, bool saveAtlasesAsAssets = false, MB2_EditorMethodsInterface editorMethods = null, float maxTimePerFrame = .01f)
    {
        yield return _CreateAtlasesCoroutine(progressInfo, coroutineResult, saveAtlasesAsAssets, editorMethods, maxTimePerFrame);

        if (coroutineResult.success && onBuiltAtlasesSuccess != null)
        {
            onBuiltAtlasesSuccess();
        }

        if (!coroutineResult.success && onBuiltAtlasesFail != null)
        {
            onBuiltAtlasesFail();
        }
    }

    private IEnumerator _CreateAtlasesCoroutineAtlases(MB3_TextureCombiner combiner, ProgressUpdateDelegate progressInfo, MB3_TextureCombiner.CreateAtlasesCoroutineResult coroutineResult, bool saveAtlasesAsAssets = false, MB2_EditorMethodsInterface editorMethods = null, float maxTimePerFrame = .01f)
    {
        int numResults = 1;
        if (_doMultiMaterial)
        {
            numResults = resultMaterials.Length;
        }

        OnCombinedTexturesCoroutineAtlasesAndRects = new MB_AtlasesAndRects[numResults];
        for (int i = 0; i < OnCombinedTexturesCoroutineAtlasesAndRects.Length; i++)
        {
            OnCombinedTexturesCoroutineAtlasesAndRects[i] = new MB_AtlasesAndRects();
        }

        //Do the material combining.
        for (int i = 0; i < OnCombinedTexturesCoroutineAtlasesAndRects.Length; i++)
        {
            Material resMatToPass = null;
            List<Material> sourceMats = null;
            if (_doMultiMaterial)
            {
                sourceMats = resultMaterials[i].sourceMaterials;
                resMatToPass = resultMaterials[i].combinedMaterial;
                combiner.fixOutOfBoundsUVs = resultMaterials[i].considerMeshUVs;
            }
            else
            {
                resMatToPass = _resultMaterial;
            }

            MB3_TextureCombiner.CombineTexturesIntoAtlasesCoroutineResult coroutineResult2 = new MB3_TextureCombiner.CombineTexturesIntoAtlasesCoroutineResult();
            yield return combiner.CombineTexturesIntoAtlasesCoroutine(progressInfo, OnCombinedTexturesCoroutineAtlasesAndRects[i], resMatToPass, objsToMesh, sourceMats, texturePropNamesToIgnore, editorMethods, coroutineResult2, maxTimePerFrame,
                onlyPackRects: false, splitAtlasWhenPackingIfTooBig: false);
            coroutineResult.success = coroutineResult2.success;
            if (!coroutineResult.success)
            {
                coroutineResult.isFinished = true;
                yield break;
            }
        }

        unpackMat2RectMap(OnCombinedTexturesCoroutineAtlasesAndRects);
        if (coroutineResult.success && editorMethods != null) editorMethods.GetMaterialPrimaryKeysIfAddressables(textureBakeResults);
        //Save the results
        textureBakeResults.resultType = MB2_TextureBakeResults.ResultType.atlas;
        textureBakeResults.resultMaterialsTexArray = new MB_MultiMaterialTexArray[0];
        textureBakeResults.doMultiMaterial = _doMultiMaterial;
        if (_doMultiMaterial)
        {
            textureBakeResults.resultMaterials = resultMaterials;
        }
        else
        {
            MB_MultiMaterial[] resMats = new MB_MultiMaterial[1];
            resMats[0] = new MB_MultiMaterial();
            resMats[0].combinedMaterial = _resultMaterial;
            resMats[0].considerMeshUVs = _fixOutOfBoundsUVs;
            resMats[0].sourceMaterials = new List<Material>();
            for (int i = 0; i < textureBakeResults.materialsAndUVRects.Length; i++)
            {
                resMats[0].sourceMaterials.Add(textureBakeResults.materialsAndUVRects[i].material);
            }
            textureBakeResults.resultMaterials = resMats;
        }

        if (LOG_LEVEL >= MB2_LogLevel.info) Debug.Log("Created Atlases");
    }

    internal IEnumerator _CreateAtlasesCoroutineTextureArray(MB3_TextureCombiner combiner, ProgressUpdateDelegate progressInfo, MB3_TextureCombiner.CreateAtlasesCoroutineResult coroutineResult, bool saveAtlasesAsAssets = false, MB2_EditorMethodsInterface editorMethods = null, float maxTimePerFrame = .01f)
    {
        MB_TextureArrayResultMaterial[] bakedMatsAndSlices = null;

        // Validate the formats
        if (textureArrayOutputFormats == null || textureArrayOutputFormats.Length == 0)
        {
            Debug.LogError("No Texture Array Output Formats. There must be at least one entry.");
            coroutineResult.isFinished = true;
            yield break;
        }

        for (int i = 0; i < textureArrayOutputFormats.Length; i++)
        {
            if (!textureArrayOutputFormats[i].ValidateTextureImporterFormatsExistsForTextureFormats(editorMethods, i))
            {
                Debug.LogError("Could not map the selected texture format to a Texture Importer Format. Safest options are ARGB32, or RGB24.");
                coroutineResult.isFinished = true;
                yield break;
            }
        }

        for (int resMatIdx = 0; resMatIdx < resultMaterialsTexArray.Length; resMatIdx++)
        {
            MB_MultiMaterialTexArray textureArraySliceConfig = resultMaterialsTexArray[resMatIdx];
            if (textureArraySliceConfig.combinedMaterial == null)
            {
                Debug.LogError("Material is null for Texture Array Slice Configuration: " + resMatIdx + ".");
                coroutineResult.isFinished = true;
                yield break;
            }


            List<MB_TexArraySlice> slices = textureArraySliceConfig.slices;
            for (int sliceIdx = 0; sliceIdx < slices.Count; sliceIdx++)
            {
                for (int srcMatIdx = 0; srcMatIdx < slices[sliceIdx].sourceMaterials.Count; srcMatIdx++)
                {
                    MB_TexArraySliceRendererMatPair sourceMat = slices[sliceIdx].sourceMaterials[srcMatIdx];
                    if (sourceMat.sourceMaterial == null)
                    {
                        Debug.LogError("Source material is null for Texture Array Slice Configuration: " + resMatIdx + " slice: " + sliceIdx);
                        coroutineResult.isFinished = true;
                        yield break;
                    }

                    if (slices[sliceIdx].considerMeshUVs)
                    {
                        if (sourceMat.renderer == null)
                        {
                            Debug.LogError("Renderer is null for Texture Array Slice Configuration: " + resMatIdx + " slice: " + sliceIdx + ". If considerUVs is enabled then a renderer must be supplied for each source material. The same source material can be used multiple times.");
                            coroutineResult.isFinished = true;
                            yield break;
                        }
                    }
                    else
                    {
                        // TODO check for duplicate source mats.
                    }
                }
            }
        }

        // initialize structure to store results. For texture arrays the structure is two layers deep.
        // First layer is resultMaterial / submesh (each result material can use a different shader)
        // Second layer is a set of TextureArrays for the TextureProperties on that result material.
        int numResultMats = resultMaterialsTexArray.Length;
        bakedMatsAndSlices = new MB_TextureArrayResultMaterial[numResultMats];
        for (int resMatIdx = 0; resMatIdx < bakedMatsAndSlices.Length; resMatIdx++)
        {
            bakedMatsAndSlices[resMatIdx] = new MB_TextureArrayResultMaterial();
            int numSlices = resultMaterialsTexArray[resMatIdx].slices.Count;
            MB_AtlasesAndRects[] slices = bakedMatsAndSlices[resMatIdx].slices = new MB_AtlasesAndRects[numSlices];
            for (int j = 0; j < numSlices; j++) slices[j] = new MB_AtlasesAndRects();
        }

        // Some of the slices will be atlases (more than one atlas per slice).
        // Do the material combining for these. First loop over the result materials (1 per submeshes).
        for (int resMatIdx = 0; resMatIdx < bakedMatsAndSlices.Length; resMatIdx++)
        {
            yield return MB_TextureArrays._CreateAtlasesCoroutineSingleResultMaterial(resMatIdx, bakedMatsAndSlices[resMatIdx], resultMaterialsTexArray[resMatIdx],
                objsToMesh,
                combiner, 
                textureArrayOutputFormats,
                resultMaterialsTexArray,
                customShaderProperties,
                texturePropNamesToIgnore,
                progressInfo, coroutineResult, saveAtlasesAsAssets, editorMethods, maxTimePerFrame);
            if (!coroutineResult.success) yield break;
        }

        if (coroutineResult.success)
        {
            // Save the results into the TextureBakeResults.
            unpackMat2RectMap(bakedMatsAndSlices);
            if (editorMethods != null) editorMethods.GetMaterialPrimaryKeysIfAddressables(textureBakeResults);
            textureBakeResults.resultType = MB2_TextureBakeResults.ResultType.textureArray;
            textureBakeResults.resultMaterials = new MB_MultiMaterial[0];
            textureBakeResults.resultMaterialsTexArray = resultMaterialsTexArray;
            if (LOG_LEVEL >= MB2_LogLevel.info) Debug.Log("Created Texture2DArrays");
        }
        else
        {
            if (LOG_LEVEL >= MB2_LogLevel.info) Debug.Log("Failed to create Texture2DArrays");
        }
    }

    private IEnumerator _CreateAtlasesCoroutine(ProgressUpdateDelegate progressInfo, MB3_TextureCombiner.CreateAtlasesCoroutineResult coroutineResult, bool saveAtlasesAsAssets = false, MB2_EditorMethodsInterface editorMethods = null, float maxTimePerFrame = .01f)
    {
        MBVersionConcrete mbv = new MBVersionConcrete();
        if (!MB3_TextureCombiner._RunCorutineWithoutPauseIsRunning && (mbv.GetMajorVersion() < 5 || (mbv.GetMajorVersion() == 5 && mbv.GetMinorVersion() < 3)))
        {
            Debug.LogError("Running the texture combiner as a coroutine only works in Unity 5.3 and higher");
            coroutineResult.success = false;
            yield break;
        }
        this.OnCombinedTexturesCoroutineAtlasesAndRects = null;

        if (maxTimePerFrame <= 0f)
        {
            Debug.LogError("maxTimePerFrame must be a value greater than zero");
            coroutineResult.isFinished = true;
            yield break;
        }
        MB2_ValidationLevel vl = Application.isPlaying ? MB2_ValidationLevel.quick : MB2_ValidationLevel.robust;
        if (!DoCombinedValidate(this, MB_ObjsToCombineTypes.dontCare, null, vl))
        {
            coroutineResult.isFinished = true;
            yield break;
        }
        if (_doMultiMaterial && !_ValidateResultMaterials())
        {
            coroutineResult.isFinished = true;
            yield break;
        }
        else if (resultType == MB2_TextureBakeResults.ResultType.textureArray)
        {
            //TODO validate texture arrays.
        }
        else if (!_doMultiMaterial)
        {
            if (_resultMaterial == null)
            {
                Debug.LogError("Combined Material is null please create and assign a result material.");
                coroutineResult.isFinished = true;
                yield break;
            }
            Shader targShader = _resultMaterial.shader;
            for (int i = 0; i < objsToMesh.Count; i++)
            {
                Material[] ms = MB_Utility.GetGOMaterials(objsToMesh[i]);
                for (int j = 0; j < ms.Length; j++)
                {
                    Material m = ms[j];
                    if (m != null && m.shader != targShader)
                    {
                        Debug.LogWarning("Game object " + objsToMesh[i] + " does not use shader " + targShader + " it may not have the required textures. If not small solid color textures will be generated.");
                    }

                }
            }
        }

        MB3_TextureCombiner combiner = CreateAndConfigureTextureCombiner();
        combiner.saveAtlasesAsAssets = saveAtlasesAsAssets;

        OnCombinedTexturesCoroutineAtlasesAndRects = null;
        if (resultType == MB2_TextureBakeResults.ResultType.textureArray)
        {
            yield return _CreateAtlasesCoroutineTextureArray(combiner, progressInfo, coroutineResult, saveAtlasesAsAssets, editorMethods, maxTimePerFrame);
            if (!coroutineResult.success) yield break;
        }
        else
        {
            yield return _CreateAtlasesCoroutineAtlases(combiner, progressInfo, coroutineResult, saveAtlasesAsAssets, editorMethods, maxTimePerFrame);
            if (!coroutineResult.success) yield break;
        }

        //set the texture bake resultAtlasesAndRects on the Mesh Baker component if it exists
        MB3_MeshBakerCommon[] mb = GetComponentsInChildren<MB3_MeshBakerCommon>();
        for (int i = 0; i < mb.Length; i++)
        {
            mb[i].textureBakeResults = textureBakeResults;
        }

        coroutineResult.isFinished = true;
    }

    /// <summary>
    /// Creates the atlases.
    /// </summary>
    /// <returns>
    /// The atlases.
    /// </returns>
    /// <param name='progressInfo'>
    /// Progress info is a delegate function that displays a progress dialog. Can be null
    /// </param>
    /// <param name='saveAtlasesAsAssets'>
    /// if true atlases are saved as assets in the project folder. Othersise they are instances in memory
    /// </param>
    /// <param name='editorMethods'>
    /// Texture format tracker. Contains editor functionality such as save assets. Can be null.
    /// </param>
    public MB_AtlasesAndRects[] CreateAtlases(ProgressUpdateDelegate progressInfo, bool saveAtlasesAsAssets = false, MB2_EditorMethodsInterface editorMethods = null)
    {
        MB_AtlasesAndRects[] mAndAs = null;

        try
        {
            _coroutineResult = new MB3_TextureCombiner.CreateAtlasesCoroutineResult();
            MB3_TextureCombiner.RunCorutineWithoutPause(CreateAtlasesCoroutine(progressInfo, _coroutineResult, saveAtlasesAsAssets, editorMethods, 1000f), 0);
            if (_coroutineResult.success && textureBakeResults != null)
            {
                mAndAs = this.OnCombinedTexturesCoroutineAtlasesAndRects;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message + "\n" + ex.StackTrace.ToString());
        }
        finally
        {
            if (saveAtlasesAsAssets)
            { //Atlases were saved to project so we don't need these ones
                if (mAndAs != null)
                {
                    for (int j = 0; j < mAndAs.Length; j++)
                    {
                        MB_AtlasesAndRects mAndA = mAndAs[j];
                        if (mAndA != null && mAndA.atlases != null)
                        {
                            for (int i = 0; i < mAndA.atlases.Length; i++)
                            {
                                if (mAndA.atlases[i] != null)
                                {
                                    if (editorMethods != null) editorMethods.Destroy(mAndA.atlases[i]);
                                    else MB_Utility.Destroy(mAndA.atlases[i]);
                                }
                            }
                        }
                    }
                }
            }
        }

        return mAndAs;
    }

    void unpackMat2RectMap(MB_AtlasesAndRects[] rawResults)
    {
        List<MB_MaterialAndUVRect> mss = new List<MB_MaterialAndUVRect>();
        for (int i = 0; i < rawResults.Length; i++)
        {
            List<MB_MaterialAndUVRect> map = rawResults[i].mat2rect_map;
            if (map != null)
            {
                for (int j = 0; j < map.Count; j++)
                {
                    map[j].textureArraySliceIdx = -1;
                    mss.Add(map[j]);
                }
            }
        }

        textureBakeResults.version = MB2_TextureBakeResults.VERSION;
        textureBakeResults.materialsAndUVRects = mss.ToArray();
    }

    internal void unpackMat2RectMap(MB_TextureArrayResultMaterial[] rawResults)
    {
        List<MB_MaterialAndUVRect> mss = new List<MB_MaterialAndUVRect>();
        for (int resMatIdx = 0; resMatIdx < rawResults.Length; resMatIdx++)
        {
            MB_AtlasesAndRects[] slices = rawResults[resMatIdx].slices;
            for (int sliceIdx = 0; sliceIdx < slices.Length; sliceIdx++)
            {
                List<MB_MaterialAndUVRect> map = slices[sliceIdx].mat2rect_map;
                if (map != null)
                {
                    for (int rectIdx = 0; rectIdx < map.Count; rectIdx++)
                    {
                        map[rectIdx].textureArraySliceIdx = sliceIdx;
                        mss.Add(map[rectIdx]);
                    }
                }
            }
        }

        textureBakeResults.version = MB2_TextureBakeResults.VERSION;
        textureBakeResults.materialsAndUVRects = mss.ToArray();
    }

    public MB3_TextureCombiner CreateAndConfigureTextureCombiner()
    {
        MB3_TextureCombiner combiner = new MB3_TextureCombiner();
        combiner.LOG_LEVEL = LOG_LEVEL;
        combiner.atlasPadding = _atlasPadding;
        combiner.maxAtlasSize = _maxAtlasSize;
        combiner.maxAtlasHeightOverride = _maxAtlasHeightOverride;
        combiner.maxAtlasWidthOverride = _maxAtlasWidthOverride;
        combiner.useMaxAtlasHeightOverride = _useMaxAtlasHeightOverride;
        combiner.useMaxAtlasWidthOverride = _useMaxAtlasWidthOverride;
        combiner.customShaderPropNames = _customShaderProperties;
        combiner.fixOutOfBoundsUVs = _fixOutOfBoundsUVs;
        combiner.maxTilingBakeSize = _maxTilingBakeSize;
        combiner.packingAlgorithm = _packingAlgorithm;
        combiner.layerTexturePackerFastMesh = _layerTexturePackerFastMesh;
        combiner.resultType = _resultType;
        combiner.meshBakerTexturePackerForcePowerOfTwo = _meshBakerTexturePackerForcePowerOfTwo;
        combiner.resizePowerOfTwoTextures = _resizePowerOfTwoTextures;
        combiner.considerNonTextureProperties = _considerNonTextureProperties;
        return combiner;
    }

    public static void ConfigureNewMaterialToMatchOld(Material newMat, Material original)
    {
        if (original == null)
        {
            Debug.LogWarning("Original material is null, could not copy properties to " + newMat + ". Setting shader to " + newMat.shader);
            return;
        }
        newMat.shader = original.shader;
        newMat.CopyPropertiesFromMaterial(original);
        ShaderTextureProperty[] texPropertyNames = MB3_TextureCombinerPipeline.shaderTexPropertyNames;
        for (int j = 0; j < texPropertyNames.Length; j++)
        {
            Vector2 scale = Vector2.one;
            Vector2 offset = Vector2.zero;
            if (newMat.HasProperty(texPropertyNames[j].name))
            {
                newMat.SetTextureOffset(texPropertyNames[j].name, offset);
                newMat.SetTextureScale(texPropertyNames[j].name, scale);
            }
        }
    }

    string PrintSet(HashSet<Material> s)
    {
        StringBuilder sb = new StringBuilder();
        foreach (Material m in s)
        {
            sb.Append(m + ",");
        }
        return sb.ToString();
    }

    bool _ValidateResultMaterials()
    {
        HashSet<Material> allMatsOnObjs = new HashSet<Material>();
        for (int i = 0; i < objsToMesh.Count; i++)
        {
            if (objsToMesh[i] != null)
            {
                Material[] ms = MB_Utility.GetGOMaterials(objsToMesh[i]);
                for (int j = 0; j < ms.Length; j++)
                {
                    if (ms[j] != null) allMatsOnObjs.Add(ms[j]);
                }
            }
        }

        if (resultMaterials.Length < 1)
        {
            Debug.LogError("Using multiple materials but there are no 'Source Material To Combined Mappings'. You need at least one.");
        }

        HashSet<Material> allMatsInMapping = new HashSet<Material>();
        for (int i = 0; i < resultMaterials.Length; i++)
        {
            for (int j = i + 1; j < resultMaterials.Length; j++)
            {
                if (resultMaterials[i].combinedMaterial == resultMaterials[j].combinedMaterial)
                {
                    Debug.LogError(String.Format("Source To Combined Mapping: Submesh {0} and Submesh {1} use the same combined material. These should be different", i, j));
                    return false;
                }
            }

            MB_MultiMaterial mm = resultMaterials[i];
            if (mm.combinedMaterial == null)
            {
                Debug.LogError("Combined Material is null please create and assign a result material.");
                return false;
            }
            Shader targShader = mm.combinedMaterial.shader;
            for (int j = 0; j < mm.sourceMaterials.Count; j++)
            {
                if (mm.sourceMaterials[j] == null)
                {
                    Debug.LogError("There are null entries in the list of Source Materials");
                    return false;
                }
                if (targShader != mm.sourceMaterials[j].shader)
                {
                    Debug.LogWarning("Source material " + mm.sourceMaterials[j] + " does not use shader " + targShader + " it may not have the required textures. If not empty textures will be generated.");
                }
                if (allMatsInMapping.Contains(mm.sourceMaterials[j]))
                {
                    Debug.LogError("A Material " + mm.sourceMaterials[j] + " appears more than once in the list of source materials in the source material to combined mapping. Each source material must be unique.");
                    return false;
                }
                allMatsInMapping.Add(mm.sourceMaterials[j]);
            }
        }

        if (allMatsOnObjs.IsProperSubsetOf(allMatsInMapping))
        {
            allMatsInMapping.ExceptWith(allMatsOnObjs);
            Debug.LogWarning("There are materials in the mapping that are not used on your source objects: " + PrintSet(allMatsInMapping));
        }
        if (resultMaterials != null && resultMaterials.Length > 0 && allMatsInMapping.IsProperSubsetOf(allMatsOnObjs))
        {
            allMatsOnObjs.ExceptWith(allMatsInMapping);
            Debug.LogError("There are materials on the objects to combine that are not in the mapping: " + PrintSet(allMatsOnObjs));
            return false;
        }
        return true;
    }
}

