using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using DigitalOpus.MB.Core;
using UnityEngine.Serialization;


/// <summary>
/// Used internally during the material baking process
/// </summary>
[Serializable]
public class MB_AtlasesAndRects{
    /// <summary>
    /// One atlas per texture property.
    /// </summary>
	public Texture2D[] atlases;
    [NonSerialized]
	public List<MB_MaterialAndUVRect> mat2rect_map;
	public string[] texPropertyNames;
}

public class MB_TextureArrayResultMaterial
{
    public MB_AtlasesAndRects[] slices;
}

[System.Serializable]
public class MB_MultiMaterial{
	public Material combinedMaterial;
    public bool considerMeshUVs;
	public List<Material> sourceMaterials = new List<Material>();
}

[System.Serializable]
public class MB_TexArraySliceRendererMatPair
{
    public Material sourceMaterial;
    public GameObject renderer;
}

[System.Serializable]
public class MB_TexArraySlice
{
    public bool considerMeshUVs;
    public List<MB_TexArraySliceRendererMatPair> sourceMaterials = new List<MB_TexArraySliceRendererMatPair>();
    public bool ContainsMaterial(Material mat)
    {
        for (int i = 0; i < sourceMaterials.Count; i++)
        {
            if (sourceMaterials[i].sourceMaterial == mat) return true;
        }

        return false;
    }

    public HashSet<Material> GetDistinctMaterials()
    {
        HashSet<Material> distinctMats = new HashSet<Material>();
        if (sourceMaterials == null) return distinctMats;
        for (int i = 0; i < sourceMaterials.Count; i++)
        {
            distinctMats.Add(sourceMaterials[i].sourceMaterial);
        }

        return distinctMats;
    }

    public bool ContainsMaterialAndMesh(Material mat, Mesh mesh)
    {
        for (int i = 0; i < sourceMaterials.Count; i++)
        {
            if (sourceMaterials[i].sourceMaterial == mat &&
                MB_Utility.GetMesh(sourceMaterials[i].renderer) == mesh) return true;
        }

        return false;
    }

    public List<Material> GetAllUsedMaterials(List<Material> usedMats)
    {
        usedMats.Clear();
        for (int i = 0; i < sourceMaterials.Count; i++)
        {
            usedMats.Add(sourceMaterials[i].sourceMaterial);
        }
        return usedMats;
    }

    public List<GameObject> GetAllUsedRenderers(List<GameObject> allObjsFromTextureBaker)
    {
        if (considerMeshUVs)
        {
            List<GameObject> usedRendererGOs = new List<GameObject>();
            for (int i = 0; i < sourceMaterials.Count; i++)
            {
                usedRendererGOs.Add(sourceMaterials[i].renderer);
            }

            return usedRendererGOs;
        }
        else
        {
            return allObjsFromTextureBaker;
        }
    }
}

[System.Serializable]
public class MB_TextureArrayReference
{
    public string texFromatSetName;
    public Texture2DArray texArray;

    public MB_TextureArrayReference(string formatSetName, Texture2DArray ta)
    {
        texFromatSetName = formatSetName;
        texArray = ta;
    }
}

[System.Serializable]
public class MB_TexArrayForProperty
{
    public string texPropertyName;
    public MB_TextureArrayReference[] formats = new MB_TextureArrayReference[0];

    public MB_TexArrayForProperty(string name, MB_TextureArrayReference[] texRefs)
    {
        texPropertyName = name;
        formats = texRefs;
    }
}

[System.Serializable]
public class MB_MultiMaterialTexArray
{
    public Material combinedMaterial;
    public List<MB_TexArraySlice> slices = new List<MB_TexArraySlice>();
    public List<MB_TexArrayForProperty> textureProperties = new List<MB_TexArrayForProperty>();
}

[System.Serializable]
public class MB_TextureArrayFormat
{
    public string propertyName;
    public TextureFormat format;
}

[System.Serializable]
public class MB_TextureArrayFormatSet
{
    public string name;
    public TextureFormat defaultFormat;
    public MB_TextureArrayFormat[] formatOverrides;

    public bool ValidateTextureImporterFormatsExistsForTextureFormats(MB2_EditorMethodsInterface editorMethods, int idx)
    {
        if (editorMethods == null) return true;
        if (!editorMethods.TextureImporterFormatExistsForTextureFormat(defaultFormat))
        {
            Debug.LogError("TextureImporter format does not exist for Texture Array Output Formats: " + idx + " Defaut Format " + defaultFormat);
            return false;
        }

        for (int i = 0; i < formatOverrides.Length; i++)
        {
            if (!editorMethods.TextureImporterFormatExistsForTextureFormat(formatOverrides[i].format))
            {
                Debug.LogError("TextureImporter format does not exist for Texture Array Output Formats: " + idx + " Format Overrides: " + i + " (" + formatOverrides[i].format + ")");
                return false;
            }
        }
        return true;
    }

    public TextureFormat GetFormatForProperty(string propName)
    {
        for (int i = 0; i < formatOverrides.Length; i++)
        {
            if (formatOverrides.Equals(formatOverrides[i].propertyName))
            {
                return formatOverrides[i].format;
            }
        }

        return defaultFormat;
    }
}

[System.Serializable]
public class MB_MaterialAndUVRect
{
    /// <summary>
    /// The source material that was baked into the atlas.
    /// </summary>
    public Material material;

    /// <summary>
    /// The addressables primary key for this material at bake time.
    /// DECIDED NOT TO USE THIS BECAUSE THERE IS NOT EDITOR API FOR GETTING ADDRESS.
    /// </summary>
    //public string matAddressablesPKey;

    /// <summary>
    /// The runtime material that corresponds to the bake time material.
    /// The user may be baking textures in the editor and baking meshes at runtime.
    /// Materials may at runtime loaded from asset bundles which are a different instance of the Material than the baked asset
    /// We use the Addressables address (cached at texture bake time) to look up the runtime material.
    /// DECIDED NOT TO USE THIS BECAUSE THERE IS NOT EDITOR API FOR GETTING ADDRESS.
    /// </summary>
    /// [System.NonSerialized]
    //public Material runtimeMaterial;
    
    /// <summary>
    /// The rectangle in the atlas where the texture (including all tiling) was copied to.
    /// </summary>
    public Rect atlasRect;

    /// <summary>
    /// For debugging. The name of the first srcObj that uses this MaterialAndUVRect.
    /// </summary>
    public string srcObjName;

    public int textureArraySliceIdx = -1;

    public bool allPropsUseSameTiling = true;

    /// <summary>
    /// Only valid if allPropsUseSameTiling = true. Else should be 0,0,0,0
    /// The material tiling on the source material
    /// </summary>
    [FormerlySerializedAs("sourceMaterialTiling")]
    public Rect allPropsUseSameTiling_sourceMaterialTiling;

    /// <summary>
    /// Only valid if allPropsUseSameTiling = true. Else should be 0,0,0,0
    /// The encapsulating sampling rect that was used to sample for the atlas. Note that the case
    /// of dont-considerMeshUVs is the same as do-considerMeshUVs where the uv rect is 0,0,1,1 
    /// </summary>
    [FormerlySerializedAs("samplingEncapsulatinRect")]
    public Rect allPropsUseSameTiling_samplingEncapsulatinRect;

    /// <summary>
    /// Only valid if allPropsUseSameTiling = false.
    /// The UVrect of the source mesh that was baked. We are using a trick here.
    /// Instead of storing the material tiling for each
    /// texture property here, we instead bake all those tilings into the atlases and here we pretend
    /// that all those tilings were 0,0,1,1. Then all we need is to store is the 
    /// srcUVsamplingRect
    /// </summary>
    public Rect propsUseDifferntTiling_srcUVsamplingRect;

    [NonSerialized]
    public List<GameObject> objectsThatUse;

    /// <summary>
    /// The tilling type for this rectangle in the atlas.
    /// </summary>
    public MB_TextureTilingTreatment tilingTreatment = MB_TextureTilingTreatment.unknown;

    /// <param name="mat">The Material</param>
    /// <param name="destRect">The rect in the atlas this material maps to</param>
    /// <param name="allPropsUseSameTiling">If true then use sourceMaterialTiling and samplingEncapsulatingRect.
    /// if false then use srcUVsamplingRect. None used values should be 0,0,0,0.</param>
    /// <param name="sourceMaterialTiling">allPropsUseSameTiling_sourceMaterialTiling</param>
    /// <param name="samplingEncapsulatingRect">allPropsUseSameTiling_samplingEncapsulatinRect</param>
    /// <param name="srcUVsamplingRect">propsUseDifferntTiling_srcUVsamplingRect</param>
    public MB_MaterialAndUVRect(Material mat, 
        Rect destRect, 
        bool allPropsUseSameTiling,
        Rect sourceMaterialTiling, 
        Rect samplingEncapsulatingRect,
        Rect srcUVsamplingRect,
        MB_TextureTilingTreatment treatment, 
        string objName)
    {
        if (allPropsUseSameTiling)
        {
            Debug.Assert(srcUVsamplingRect == new Rect(0, 0, 0, 0));
        }

        if (!allPropsUseSameTiling) {
            Debug.Assert(samplingEncapsulatingRect == new Rect(0, 0, 0, 0));
            Debug.Assert(sourceMaterialTiling == new Rect(0, 0, 0, 0));
        }

        material = mat;
        atlasRect = destRect;
        tilingTreatment = treatment;
        this.allPropsUseSameTiling = allPropsUseSameTiling;
        allPropsUseSameTiling_sourceMaterialTiling = sourceMaterialTiling;
        allPropsUseSameTiling_samplingEncapsulatinRect = samplingEncapsulatingRect;
        propsUseDifferntTiling_srcUVsamplingRect = srcUVsamplingRect;
        srcObjName = objName;
    }

    public override int GetHashCode()
    {
        return material.GetInstanceID() ^ allPropsUseSameTiling_samplingEncapsulatinRect.GetHashCode() ^ propsUseDifferntTiling_srcUVsamplingRect.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (!(obj is MB_MaterialAndUVRect)) return false;
        MB_MaterialAndUVRect b = (MB_MaterialAndUVRect)obj;
        return material == b.material && 
            allPropsUseSameTiling_samplingEncapsulatinRect == b.allPropsUseSameTiling_samplingEncapsulatinRect &&
            allPropsUseSameTiling_sourceMaterialTiling == b.allPropsUseSameTiling_sourceMaterialTiling &&
            allPropsUseSameTiling == b.allPropsUseSameTiling &&
            propsUseDifferntTiling_srcUVsamplingRect == b.propsUseDifferntTiling_srcUVsamplingRect;
    }

    public Rect GetEncapsulatingRect()
    {
        if (allPropsUseSameTiling)
        {
            return allPropsUseSameTiling_samplingEncapsulatinRect;
        }
        else
        {
            return propsUseDifferntTiling_srcUVsamplingRect;
        }
    }

    public Rect GetMaterialTilingRect()
    {
        if (allPropsUseSameTiling)
        {
            return allPropsUseSameTiling_sourceMaterialTiling;
        }
        else
        {
            return new Rect(0, 0, 1, 1);
        }
    }
}

/// <summary>
/// This class stores the results from an MB2_TextureBaker when materials are combined into atlases. It stores
/// a list of materials and the corresponding UV rectangles in the atlases. It also stores the configuration
/// options that were used to generate the combined material.
/// 
/// It can be saved as an asset in the project so that textures can be baked in one scene and used in another.
/// </summary>
public class MB2_TextureBakeResults : ScriptableObject {
    public class CoroutineResult
    {
        public bool isComplete;
    }


    public enum ResultType
    {
        atlas,
        textureArray,
    }

    public static int VERSION { get { return 3252; } }

    public int version;

    public ResultType resultType = ResultType.atlas;

    /// <summary>
    /// Information about the atlas UV rects.
    /// IMPORTANT a source material can appear more than once in this list. If we are using considerUVs,
    /// then different parts of a source texture could be extracted to different atlas rects.
    /// </summary>
    public MB_MaterialAndUVRect[] materialsAndUVRects;

    /// <summary>
    /// This is like the combinedMeshRenderer.sharedMaterials. Some materials may be omitted if they have zero submesh triangles.
    /// There is one of these per MultiMaterial mapping.
    /// Lists source materials that were combined into each result material, and whether considerUVs was used.
    /// This is the result materials if building for atlases.
    /// </summary>
    public MB_MultiMaterial[] resultMaterials;


    /// <summary>
    /// This is like combinedMeshRenderer.sharedMaterials. Each result mat likely uses a different shader.
    /// There is one of these per MultiMaterialTexArray mapping.
    /// Each of these lists the slices and each slice has list of source mats that are in that slice.
    /// This is the result materials if building for texArrays.
    /// </summary>
    public MB_MultiMaterialTexArray[] resultMaterialsTexArray;

    public bool doMultiMaterial;

    public MB2_TextureBakeResults()
    {
        version = VERSION;
    }

    private void OnEnable()
    {
        // backward compatibility copy depricated fixOutOfBounds values to resultMaterials
        if (version < 3251)
        {
            for (int i = 0; i < materialsAndUVRects.Length; i++)
            {
                materialsAndUVRects[i].allPropsUseSameTiling = true;
            }
        }

        version = VERSION;
    }

    public int NumResultMaterials()
    {
        if (resultType == ResultType.atlas) return resultMaterials.Length;
        else return resultMaterialsTexArray.Length;
    }

    public Material GetCombinedMaterialForSubmesh(int idx)
    {
        if (resultType == ResultType.atlas)
        {
            return resultMaterials[idx].combinedMaterial;
        } else
        {
            return resultMaterialsTexArray[idx].combinedMaterial;
        }
    }

    /// <summary>
    /// If using addressables and baking meshes at runtime with atlases that were baked in the editor. 
    /// This method should be called after asset bundles have completed loading but before baking meshes. 
    /// 
    /// In this case materials used by a mesh at runtime may have been loaded from somewhere other
    /// than the build. These materials will be different instances than the Material assets in the project folder.
    /// Looking up assets will fail.
    /// </summary>
    public IEnumerator FindRuntimeMaterialsFromAddresses(CoroutineResult isComplete)
    {
        yield return MBVersion.FindRuntimeMaterialsFromAddresses(this, isComplete);
        isComplete.isComplete = true;
    }

    public bool GetConsiderMeshUVs(int idxInSrcMats, Material srcMaterial)
    {
        if (resultType == ResultType.atlas)
        {
            return resultMaterials[idxInSrcMats].considerMeshUVs;
        }
        else
        {
            // TODO do this once and cache.
            List<MB_TexArraySlice> slices = resultMaterialsTexArray[idxInSrcMats].slices;
            for (int i = 0; i < slices.Count; i++)
            {
                if (slices[i].ContainsMaterial(srcMaterial)) return slices[i].considerMeshUVs;
            }

            Debug.LogError("There were no source materials for any slice in this result material.");
            return false;
        }
    }

    public List<Material> GetSourceMaterialsUsedByResultMaterial(int resultMatIdx)
    {
        if (resultType == ResultType.atlas)
        {
            return resultMaterials[resultMatIdx].sourceMaterials;
        } else
        {
            // TODO do this once and cache.
            HashSet<Material> output = new HashSet<Material>();
            List<MB_TexArraySlice> slices = resultMaterialsTexArray[resultMatIdx].slices;
            for (int i = 0; i < slices.Count; i++)
            {
                List<Material> usedMaterials = new List<Material>();
                slices[i].GetAllUsedMaterials(usedMaterials);
                for (int j = 0; j < usedMaterials.Count; j++)
                {
                    output.Add(usedMaterials[j]);
                }
                
            }

            List<Material> outMats = new List<Material>(output);
            return outMats;
        }
    }

    /// <summary>
    /// Creates for materials on renderer.
    /// </summary>
    /// <returns>Generates an MB2_TextureBakeResult that can be used if all objects to be combined use the same material.
    /// Returns a MB2_TextureBakeResults that will map all materials used by renderer r to
    /// the rectangle 0,0..1,1 in the atlas.</returns>
    public static MB2_TextureBakeResults CreateForMaterialsOnRenderer(GameObject[] gos, List<Material> matsOnTargetRenderer)
    {
        HashSet<Material> fullMaterialList = new HashSet<Material>(matsOnTargetRenderer);
        for (int i = 0; i < gos.Length; i++)
        {
            if (gos[i] == null)
            {
                Debug.LogError(string.Format("Game object {0} in list of objects to add was null", i));
                return null;
            }
            Material[] oMats = MB_Utility.GetGOMaterials(gos[i]);
            if (oMats.Length == 0)
            {
                Debug.LogError(string.Format("Game object {0} in list of objects to add no renderer", i));
                return null;
            }
            for (int j = 0; j < oMats.Length; j++)
            {
                if (!fullMaterialList.Contains(oMats[j])) { fullMaterialList.Add(oMats[j]); }
            }
        }

        Material[] rms = new Material[fullMaterialList.Count];
        fullMaterialList.CopyTo(rms);
        MB2_TextureBakeResults tbr = (MB2_TextureBakeResults) ScriptableObject.CreateInstance( typeof(MB2_TextureBakeResults) );
        List<MB_MaterialAndUVRect> mss = new List<MB_MaterialAndUVRect>();
        for (int i = 0; i < rms.Length; i++)
        {
            if (rms[i] != null)
            {
                MB_MaterialAndUVRect matAndUVRect = new MB_MaterialAndUVRect(rms[i], new Rect(0f, 0f, 1f, 1f), true, new Rect(0f,0f,1f,1f), new Rect(0f,0f,1f,1f), new Rect(0,0,0,0), MB_TextureTilingTreatment.none, "");
                if (!mss.Contains(matAndUVRect))
                {
                    mss.Add(matAndUVRect);
                }
            }
        }

        tbr.resultMaterials = new MB_MultiMaterial[mss.Count];
        for (int i = 0; i < mss.Count; i++){
			tbr.resultMaterials[i] = new MB_MultiMaterial();
			List<Material> sourceMats = new List<Material>();
			sourceMats.Add (mss[i].material);
			tbr.resultMaterials[i].sourceMaterials = sourceMats;
			tbr.resultMaterials[i].combinedMaterial = mss[i].material;
            tbr.resultMaterials[i].considerMeshUVs = false;
		}
        if (rms.Length == 1)
        {
            tbr.doMultiMaterial = false;
        } else
        {
            tbr.doMultiMaterial = true;
        }

        tbr.materialsAndUVRects = mss.ToArray();
        return tbr;
	}
	
    public bool DoAnyResultMatsUseConsiderMeshUVs()
    {
        if (resultType == ResultType.atlas)
        {
            if (resultMaterials == null) return false;
            for (int i = 0; i < resultMaterials.Length; i++)
            {
                if (resultMaterials[i].considerMeshUVs) return true;
            }

            return false;
        }
        else
        {
            if (resultMaterialsTexArray == null) return false;
            for (int i = 0; i < resultMaterialsTexArray.Length; i++)
            {
                MB_MultiMaterialTexArray resMat = resultMaterialsTexArray[i];
                for (int j = 0; j < resMat.slices.Count; j++)
                {
                    if (resMat.slices[j].considerMeshUVs) return true;
                }
            }
            return false;
        }
    }

    public bool ContainsMaterial(Material m)
    {
        for (int i = 0; i < materialsAndUVRects.Length; i++)
        {
            if (materialsAndUVRects[i].material == m){
                return true;
            }
        }
        return false;
    }


	public string GetDescription(){
		StringBuilder sb = new StringBuilder();
		sb.Append("Shaders:\n");
		HashSet<Shader> shaders = new HashSet<Shader>();
		if (materialsAndUVRects != null){
			for (int i = 0; i < materialsAndUVRects.Length; i++){
                if (materialsAndUVRects[i].material != null)
                {
                    shaders.Add(materialsAndUVRects[i].material.shader);
                }	
			}
		}
		
		foreach(Shader m in shaders){
			sb.Append("  ").Append(m.name).AppendLine();
		}
		sb.Append("Materials:\n");
		if (materialsAndUVRects != null){
			for (int i = 0; i < materialsAndUVRects.Length; i++){
                if (materialsAndUVRects[i].material != null)
                {
                    sb.Append("  ").Append(materialsAndUVRects[i].material.name).AppendLine();
                }
			}
		}
		return sb.ToString();
	}

    public void UpgradeToCurrentVersion(MB2_TextureBakeResults tbr)
    {
        if (tbr.version < 3252)
        {
            for (int i = 0; i < tbr.materialsAndUVRects.Length; i++)
            {
                tbr.materialsAndUVRects[i].allPropsUseSameTiling = true;
            }
        }
    }

    public static bool IsMeshAndMaterialRectEnclosedByAtlasRect(MB_TextureTilingTreatment tilingTreatment, Rect uvR, Rect sourceMaterialTiling, Rect samplingEncapsulatinRect, MB2_LogLevel logLevel)
    {
        Rect potentialRect = new Rect();
        // test to see if this would fit in what was baked in the atlas

        potentialRect = MB3_UVTransformUtility.CombineTransforms(ref uvR, ref sourceMaterialTiling);
        if (logLevel >= MB2_LogLevel.trace)
        {
            if (logLevel >= MB2_LogLevel.trace) Debug.Log("IsMeshAndMaterialRectEnclosedByAtlasRect Rect in atlas uvR=" + uvR.ToString("f5") + " sourceMaterialTiling=" + sourceMaterialTiling.ToString("f5") + "Potential Rect (must fit in encapsulating) " + potentialRect.ToString("f5") + " encapsulating=" + samplingEncapsulatinRect.ToString("f5") + " tilingTreatment=" + tilingTreatment);
        }

        if (tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeX)
        {
            if (MB3_UVTransformUtility.LineSegmentContainsShifted(samplingEncapsulatinRect.y, samplingEncapsulatinRect.height, potentialRect.y, potentialRect.height))
            {
                return true;
            }
        }
        else if (tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeY)
        {
            if (MB3_UVTransformUtility.LineSegmentContainsShifted(samplingEncapsulatinRect.x, samplingEncapsulatinRect.width, potentialRect.x, potentialRect.width))
            {
                return true;
            }
        }
        else if (tilingTreatment == MB_TextureTilingTreatment.edgeToEdgeXY)
        {
            //only one rect in atlas and is edge to edge in both X and Y directions.
            return true;
        }
        else
        {
            if (MB3_UVTransformUtility.RectContainsShifted(ref samplingEncapsulatinRect, ref potentialRect))
            {
                return true;
            }
        }
        return false;
    }
}