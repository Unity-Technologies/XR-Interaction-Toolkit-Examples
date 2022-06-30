using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DigitalOpus.MB.Core{
	
	public delegate void ProgressUpdateDelegate(string msg, float progress);
    public delegate bool ProgressUpdateCancelableDelegate(string msg, float progress);

    public enum MB_ObjsToCombineTypes{
		prefabOnly,
		sceneObjOnly,
		dontCare
	}
	
	public enum MB_OutputOptions{
		bakeIntoPrefab,
		bakeMeshsInPlace,
		bakeTextureAtlasesOnly,
		bakeIntoSceneObject
	}
	
	public enum MB_RenderType{
		meshRenderer,
		skinnedMeshRenderer
	}
	
	public enum MB2_OutputOptions{
		bakeIntoSceneObject,
		bakeMeshAssetsInPlace,
		bakeIntoPrefab
	}
	
	public enum MB2_LightmapOptions{
		preserve_current_lightmapping,
		ignore_UV2,
		copy_UV2_unchanged,
		generate_new_UV2_layout,
        copy_UV2_unchanged_to_separate_rects,
    }

	public enum MB2_PackingAlgorithmEnum{
		UnitysPackTextures,
		MeshBakerTexturePacker,
		MeshBakerTexturePacker_Fast,
        MeshBakerTexturePacker_Horizontal, //special packing packs all horizontal. makes it possible to use an atlas with tiling textures.
        MeshBakerTexturePacker_Vertical, //special packing packs all Vertical. makes it possible to use an atlas with tiling textures.
		MeshBakerTexturePaker_Fast_V2_Beta, // new version of MeshBakerTexterPackerFast that uses a mesh. Is compatible with URP and HDRP.
    }

    public enum MB_TextureTilingTreatment{
        none,
        considerUVs,
        edgeToEdgeX,
        edgeToEdgeY,
        edgeToEdgeXY, // One image in atlas.
        unknown,
    }
	
	public enum MB2_ValidationLevel{
		none,
		quick,
		robust
	}

	/// <summary>
	/// M b2_ texture combiner editor methods.
	/// Contains functionality such as changeing texture formats
	/// Which is only available in the editor. These methods have all been put in a
	/// class so that the UnityEditor namespace does not need to be included in any of the
	/// the runtime classes.
	/// </summary>
	public interface MB2_EditorMethodsInterface{
		void Clear();
		void RestoreReadFlagsAndFormats(ProgressUpdateDelegate progressInfo);
		void SetReadWriteFlag(Texture2D tx, bool isReadable, bool addToList);
		void ConvertTextureFormat_DefaultPlatform(Texture2D tx, TextureFormat targetFormat, bool isNormalMap);
		void ConvertTextureFormat_PlatformOverride(Texture2D tx, TextureFormat targetFormat, bool isNormalMap);
		void SaveTextureArrayToAssetDatabase(Texture2DArray atlas, TextureFormat foramt, string texPropertyName, int atlasNum, Material resMat);
        void SaveAtlasToAssetDatabase(Texture2D atlas, ShaderTextureProperty texPropertyName, int atlasNum, bool doAnySrcMatsHaveProperty, Material resMat);
		bool IsNormalMap(Texture2D tx);
		string GetPlatformString();
		void SetTextureSize(Texture2D tx, int size);
		bool IsCompressed(Texture2D tx);
		void CheckBuildSettings(long estimatedAtlasSize);
		bool CheckPrefabTypes(MB_ObjsToCombineTypes prefabType, List<GameObject> gos);
		bool ValidateSkinnedMeshes(List<GameObject> mom);
		void CommitChangesToAssets();
        void OnPreTextureBake();
        void OnPostTextureBake();

        /// <summary>
        /// Safe Destroy. Won't destroy assets.
        /// </summary>
		void Destroy(UnityEngine.Object o);
        void DestroyAsset(UnityEngine.Object o);
        bool IsAnAsset(UnityEngine.Object o);
        Texture2D CreateTemporaryAssetCopy(ShaderTextureProperty prop, Texture2D sliceTex, int w, int h, TextureFormat format, MB2_LogLevel logLevel);
        bool TextureImporterFormatExistsForTextureFormat(TextureFormat texFormat);
        bool ConvertTexture2DArray(Texture2DArray inArray, Texture2DArray outArray, TextureFormat outFormat);
        void GetMaterialPrimaryKeysIfAddressables(MB2_TextureBakeResults textureBakeResults);
    }	
}
