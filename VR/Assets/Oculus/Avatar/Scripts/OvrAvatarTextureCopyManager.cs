using System.Collections;
using System.Collections.Generic;
using Oculus.Avatar;
using UnityEngine;

public class OvrAvatarTextureCopyManager : MonoBehaviour
{
    [System.Serializable]
    public struct FallbackTextureSet
    {
        public bool Initialized;
        public Texture2D DiffuseRoughness;
        public Texture2D Normal;
    }
    // Fallback texture sets are indexed with ovrAvatarAssetLevelOfDetail.
    // We currently only use 1, 3 (mobile default), 5 (PC default).
    public FallbackTextureSet[] FallbackTextureSets = new FallbackTextureSet[(int)ovrAvatarAssetLevelOfDetail.Highest + 1];

    struct CopyTextureParams
    {
        public Texture Src;
        public Texture Dst;
        public int Mip;
        public int SrcSize;
        public int DstElement;

        public CopyTextureParams(
            Texture src, 
            Texture dst, 
            int mip, 
            int srcSize, 
            int dstElement)
        {
            Src = src;
            Dst = dst;
            Mip = mip;  
            SrcSize = srcSize;
            DstElement = dstElement;
        }
    }
    private Queue<CopyTextureParams> texturesToCopy;

    public struct TextureSet
    {
        // Contains all texture asset IDs that are part of an avatar spec.
        // Used by DeleteTextureSet().
        // Textures that are part of combined mesh avatars can be safely deleted once they have been
        // uploaded to the texture arrays.
        // Textures that are part of single component meshes will remain in memory.
        public Dictionary<ulong, bool> TextureIDSingleMeshPair;
        public bool IsProcessed;

        public TextureSet(
            Dictionary<ulong, bool> textureIDSingleMeshPair,
            bool isProcessed)
        {
            TextureIDSingleMeshPair = textureIDSingleMeshPair;
            IsProcessed = isProcessed;
        }
    }
    private Dictionary<int, TextureSet> textureSets;
    
    private const int TEXTURES_TO_COPY_QUEUE_CAPACITY = 256;
    private const int COPIES_PER_FRAME = 8;

    // Fallback texture paths are indexed with ovrAvatarAssetLevelOfDetail
    // We currently only use 1, 3 (mobile default), 5 (PC default) 
    private readonly string[] FALLBACK_TEXTURE_PATHS_DIFFUSE_ROUGHNESS = new string[]
    {
        "null",
        PATH_LOWEST_DIFFUSE_ROUGHNESS,
        "null",
        PATH_MEDIUM_DIFFUSE_ROUGHNESS,
        "null",
        PATH_HIGHEST_DIFFUSE_ROUGHNESS,
    };
    private readonly string[] FALLBACK_TEXTURE_PATHS_NORMAL = new string[]
    {
        "null",
        PATH_LOWEST_NORMAL,
        "null",
        PATH_MEDIUM_NORMAL,
        "null",
        PATH_HIGHEST_NORMAL,
    };

    private const string PATH_HIGHEST_DIFFUSE_ROUGHNESS = "FallbackTextures/fallback_diffuse_roughness_2048";
    private const string PATH_MEDIUM_DIFFUSE_ROUGHNESS = "FallbackTextures/fallback_diffuse_roughness_1024";
    private const string PATH_LOWEST_DIFFUSE_ROUGHNESS = "FallbackTextures/fallback_diffuse_roughness_256";
    private const string PATH_HIGHEST_NORMAL = "FallbackTextures/fallback_normal_2048";
    private const string PATH_MEDIUM_NORMAL = "FallbackTextures/fallback_normal_1024";
    private const string PATH_LOWEST_NORMAL = "FallbackTextures/fallback_normal_256";

    private const int GPU_TEXTURE_COPY_WAIT_TIME = 10;

    public OvrAvatarTextureCopyManager()
    {
        texturesToCopy = new Queue<CopyTextureParams>(TEXTURES_TO_COPY_QUEUE_CAPACITY);
        textureSets = new Dictionary<int, TextureSet>();
    }

    public void Update()
    {
        if (texturesToCopy.Count == 0)
        {
            return;
        }

        lock (texturesToCopy)
        {
            for (int i = 0; i < Mathf.Min(COPIES_PER_FRAME, texturesToCopy.Count); ++i)
            {
                CopyTexture(texturesToCopy.Dequeue());
            }
        }
    }

    public int GetTextureCount()
    {
        return texturesToCopy.Count;
    }

    public void CopyTexture(
        Texture src,
        Texture dst,
        int mipLevel,
        int mipSize,
        int dstElement,
        bool useQueue = true)
    {
        var copyTextureParams = new CopyTextureParams(src, dst, mipLevel, mipSize, dstElement);

        if (useQueue)
        {
            lock (texturesToCopy)
            {
                if (texturesToCopy.Count < TEXTURES_TO_COPY_QUEUE_CAPACITY)
                {
                    texturesToCopy.Enqueue(copyTextureParams);
                }
                else
                {
                    // Queue is full so copy texture immediately
                    CopyTexture(copyTextureParams);
                }
            }
        }
        else
        {
            CopyTexture(copyTextureParams);
        }
    }

    private void CopyTexture(CopyTextureParams copyTextureParams)
    {
        Graphics.CopyTexture(
            copyTextureParams.Src,
            0,
            copyTextureParams.Mip,
            copyTextureParams.Dst,
            copyTextureParams.DstElement,
            copyTextureParams.Mip);
    }

    public void AddTextureIDToTextureSet(int gameobjectID, ulong textureID, bool isSingleMesh)
    {
        if (!textureSets.ContainsKey(gameobjectID))
        {
            TextureSet newTextureSet = new TextureSet(new Dictionary<ulong, bool>(), false);
            newTextureSet.TextureIDSingleMeshPair.Add(textureID, isSingleMesh);
            textureSets.Add(gameobjectID, newTextureSet);
        }
        else
        {
            bool TexIDSingleMesh;
            if (textureSets[gameobjectID].TextureIDSingleMeshPair.TryGetValue(textureID, out TexIDSingleMesh))
            {
                if (!TexIDSingleMesh && isSingleMesh)
                {
                    textureSets[gameobjectID].TextureIDSingleMeshPair[textureID] = true;
                }
            }
            else
            {
                textureSets[gameobjectID].TextureIDSingleMeshPair.Add(textureID, isSingleMesh);
            }
        }
    }

    // This is called by a fully loaded avatar using combined mesh to safely delete unused textures.
    public void DeleteTextureSet(int gameobjectID)
    {
        TextureSet textureSetToDelete;
        if (!textureSets.TryGetValue(gameobjectID, out textureSetToDelete))
        {
            return;
        };

        if (textureSetToDelete.IsProcessed)
        {
            return;
        }

        StartCoroutine(DeleteTextureSetCoroutine(textureSetToDelete, gameobjectID));
    }

    private IEnumerator DeleteTextureSetCoroutine(TextureSet textureSetToDelete, int gameobjectID)
    {
        // Wait a conservative amount of time for gpu upload to finish. Unity 2017 doesn't support async GPU calls,
        // so this 10 second time is a very conservative delay for this process to occur, which should be <1 sec.
        yield return new WaitForSeconds(GPU_TEXTURE_COPY_WAIT_TIME);

        // Spin if an avatar is loading
        while (OvrAvatarSDKManager.Instance.IsAvatarLoading())
        {
            yield return null;
        }

        // The avatar's texture set is compared against all other loaded or loading avatar texture sets.
        foreach (var textureIdAndSingleMeshFlag in textureSetToDelete.TextureIDSingleMeshPair)
        {
            bool triggerDelete = !textureIdAndSingleMeshFlag.Value;
            if (triggerDelete)
            {
                foreach (KeyValuePair<int, TextureSet> textureSet in textureSets)
                {
                    if (textureSet.Key == gameobjectID)
                    {
                        continue;
                    }

                    foreach (var comparisonTextureIDSingleMeshPair in textureSet.Value.TextureIDSingleMeshPair)
                    {
                        // Mark the texture as not deletable if it's present in another set and that set hasn't been processed
                        // or that texture ID is marked as part of a single mesh component.
                        if (comparisonTextureIDSingleMeshPair.Key == textureIdAndSingleMeshFlag.Key &&
                            (!textureSet.Value.IsProcessed || comparisonTextureIDSingleMeshPair.Value))
                        {
                            triggerDelete = false;
                            break;
                        }
                    }

                    if (!triggerDelete)
                    {
                        break;
                    }
                }
            }

            if (triggerDelete)
            {
                Texture2D textureToDelete = OvrAvatarComponent.GetLoadedTexture(textureIdAndSingleMeshFlag.Key);
                if (textureToDelete != null)
                {
                    AvatarLogger.Log("Deleting texture " + textureIdAndSingleMeshFlag.Key);
                    OvrAvatarSDKManager.Instance.DeleteAssetFromCache(textureIdAndSingleMeshFlag.Key);
                    Destroy(textureToDelete);
                }
            }
        }
        textureSetToDelete.IsProcessed = true;
        textureSets.Remove(gameobjectID);
    }

    public void CheckFallbackTextureSet(ovrAvatarAssetLevelOfDetail lod)
    {
        if (FallbackTextureSets[(int)lod].Initialized)
        {
            return;
        }

        InitFallbackTextureSet(lod);
    }

    private void InitFallbackTextureSet(ovrAvatarAssetLevelOfDetail lod)
    {
        FallbackTextureSets[(int)lod].DiffuseRoughness = FallbackTextureSets[(int)lod].DiffuseRoughness =
            Resources.Load<Texture2D>(FALLBACK_TEXTURE_PATHS_DIFFUSE_ROUGHNESS[(int)lod]);
        FallbackTextureSets[(int)lod].Normal = FallbackTextureSets[(int)lod].Normal =
            Resources.Load<Texture2D>(FALLBACK_TEXTURE_PATHS_NORMAL[(int)lod]);
        FallbackTextureSets[(int)lod].Initialized = true;
    }
}
