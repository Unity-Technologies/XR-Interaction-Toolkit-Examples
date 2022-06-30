using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class OvrAvatarMaterialManager : MonoBehaviour
{
    private Renderer TargetRenderer;
    private AvatarTextureArrayProperties[] TextureArrays;

    public enum TextureType
    {
        DiffuseTextures = 0,
        NormalMaps,
        RoughnessMaps,

        Count
    }

    // Material properties required to render a single component
    public struct AvatarComponentMaterialProperties
    {
        public ovrAvatarBodyPartType TypeIndex;
        public Color Color;
        public Texture2D[] Textures;
        public float DiffuseIntensity;
        public float RimIntensity;
        public float ReflectionIntensity;
    }

    // Texture arrays
    public struct AvatarTextureArrayProperties
    {
        public Texture2D[] Textures;
        public Texture2DArray TextureArray;
    }

    // Material property arrays that are pushed to the shader
    public struct AvatarMaterialPropertyBlock
    {
        public Vector4[] Colors;
        public float[] DiffuseIntensities;
        public float[] RimIntensities;
        public float[] ReflectionIntensities;
    }

    private readonly string[] TextureTypeToShaderProperties =
    {
        "_MainTex",       // TextureType.DiffuseTextures = 0
        "_NormalMap",     // TextureType.NormalMaps
        "_RoughnessMap"   // TextureType.RoughnessMaps
    };

    // Container class for all the data relating to an avatar material description
    [System.Serializable]
    public class AvatarMaterialConfig
    {
        public AvatarComponentMaterialProperties[] ComponentMaterialProperties;
        public AvatarMaterialPropertyBlock MaterialPropertyBlock;
    }

    // Local config that this manager instance will render
    public AvatarMaterialConfig LocalAvatarConfig = new AvatarMaterialConfig();

    public List<ReflectionProbeBlendInfo> ReflectionProbes = new List<ReflectionProbeBlendInfo>();

    // Cache the previous shader when swapping in the loading shader.
    private Shader CombinedShader;
    // Shader properties
    public static string AVATAR_SHADER_LOADER = "OvrAvatar/Avatar_Mobile_Loader";
    public static string AVATAR_SHADER_MAINTEX = "_MainTex";
    public static string AVATAR_SHADER_NORMALMAP = "_NormalMap";
    public static string AVATAR_SHADER_ROUGHNESSMAP = "_RoughnessMap";
    public static string AVATAR_SHADER_COLOR = "_BaseColor";
    public static string AVATAR_SHADER_DIFFUSEINTENSITY = "_DiffuseIntensity";
    public static string AVATAR_SHADER_RIMINTENSITY = "_RimIntensity";
    public static string AVATAR_SHADER_REFLECTIONINTENSITY = "_ReflectionIntensity";
    public static string AVATAR_SHADER_CUBEMAP = "_Cubemap";
    public static string AVATAR_SHADER_ALPHA = "_Alpha";
    public static string AVATAR_SHADER_LOADING_DIMMER = "_LoadingDimmer";

    public static string AVATAR_SHADER_IRIS_COLOR = "_MaskColorIris";
    public static string AVATAR_SHADER_LIP_COLOR = "_MaskColorLips";
    public static string AVATAR_SHADER_BROW_COLOR = "_MaskColorBrows";
    public static string AVATAR_SHADER_LASH_COLOR = "_MaskColorLashes";
    public static string AVATAR_SHADER_SCLERA_COLOR = "_MaskColorSclera";
    public static string AVATAR_SHADER_GUM_COLOR = "_MaskColorGums";
    public static string AVATAR_SHADER_TEETH_COLOR = "_MaskColorTeeth";
    public static string AVATAR_SHADER_LIP_SMOOTHNESS = "_LipSmoothness";

    // Diffuse Intensity constants: body, clothes, eyewear, hair, beard
    public static float[] DiffuseIntensities = new[] {0.3f, 0.1f, 0f, 0.15f, 0.15f};
    // Rim Intensity constants: body, clothes, eyewear, hair, beard
    public static float[] RimIntensities = new[] {5f, 2f, 2.84f, 4f, 4f};
    // Reflection Intensity constants: body, clothes, eyewear, hair, beard
    public static float[] ReflectionIntensities = new[] {0f, 0.3f, 0.4f, 0f, 0f};

    // Loading animation
    private const float LOADING_ANIMATION_AMPLITUDE = 0.5f;
    private const float LOADING_ANIMATION_PERIOD = 0.35f;
    private const float LOADING_ANIMATION_CURVE_SCALE = 0.25f;
    private const float LOADING_ANIMATION_DIMMER_MIN = 0.3f;

    public void CreateTextureArrays()
    {
        const int componentCount = (int)ovrAvatarBodyPartType.Count;
        const int textureTypeCount = (int)TextureType.Count;

        LocalAvatarConfig.ComponentMaterialProperties = new AvatarComponentMaterialProperties[componentCount];
        LocalAvatarConfig.MaterialPropertyBlock.Colors = new Vector4[componentCount];
        LocalAvatarConfig.MaterialPropertyBlock.DiffuseIntensities = new float[componentCount];
        LocalAvatarConfig.MaterialPropertyBlock.RimIntensities = new float[componentCount];
        LocalAvatarConfig.MaterialPropertyBlock.ReflectionIntensities = new float[componentCount];

        for (int i = 0; i < LocalAvatarConfig.ComponentMaterialProperties.Length; ++i)
        {
            LocalAvatarConfig.ComponentMaterialProperties[i].Textures = new Texture2D[textureTypeCount];
        }

        TextureArrays = new AvatarTextureArrayProperties[textureTypeCount];
    }

    public void SetRenderer(Renderer renderer)
    {
        TargetRenderer = renderer;
        TargetRenderer.GetClosestReflectionProbes(ReflectionProbes);
    }

    public void OnCombinedMeshReady()
    {
        InitTextureArrays();
        SetMaterialPropertyBlock();
        // Callback to delete texture set once the avatar is fully loaded
        StartCoroutine(RunLoadingAnimation(DeleteTextureSet));
    }

    // Add a texture ID so that it's managed for deletion
    public void AddTextureIDToTextureManager(ulong assetID, bool isSingleComponent)
    {
        OvrAvatarSDKManager.Instance.GetTextureCopyManager().AddTextureIDToTextureSet(
            GetInstanceID(), assetID, isSingleComponent);
    }

    // Once avatar loading is completed trigger the texture set for deletion
    private void DeleteTextureSet()
    {
        OvrAvatarSDKManager.Instance.GetTextureCopyManager().DeleteTextureSet(GetInstanceID());
    }

    // Prepare texture arrays and copy to GPU
    public void InitTextureArrays()
    {
        var localProps = LocalAvatarConfig.ComponentMaterialProperties[0];

        for (int i = 0; i < TextureArrays.Length && i < localProps.Textures.Length; i++)
        {
            TextureArrays[i].TextureArray = new Texture2DArray(
                localProps.Textures[0].height, localProps.Textures[0].width,
                LocalAvatarConfig.ComponentMaterialProperties.Length,
                localProps.Textures[0].format,
                true,
                QualitySettings.activeColorSpace == ColorSpace.Gamma ? false : true
            ) { filterMode = FilterMode.Trilinear,
                //Can probably get away with 4 for roughness maps as well, once we switch
                //to BC7/ASTC4x4 texture compression.
                anisoLevel = (TextureType)i == TextureType.RoughnessMaps ? 16 : 4 };
            //So a name shows up in Renderdoc
            TextureArrays[i].TextureArray.name = string.Format("Texture Array Type: {0}", (TextureType)i);

            TextureArrays[i].Textures
                = new Texture2D[LocalAvatarConfig.ComponentMaterialProperties.Length];

            for (int j = 0; j < LocalAvatarConfig.ComponentMaterialProperties.Length; j++)
            {
                TextureArrays[i].Textures[j]
                    = LocalAvatarConfig.ComponentMaterialProperties[j].Textures[i];
                //So a name shows up in Renderdoc
                TextureArrays[i].Textures[j].name = string.Format("Texture Type: {0} Component: {1}", (TextureType)i, j);
            }

            ProcessTexturesWithMips(
                TextureArrays[i].Textures,
                localProps.Textures[i].height,
                TextureArrays[i].TextureArray);
        }
    }

    private void ProcessTexturesWithMips(
        Texture2D[] textures,
        int texArrayResolution,
        Texture2DArray texArray)
    {
        for (int i = 0; i < textures.Length; i++)
        {
            int currentMipSize = texArrayResolution;
            int correctNumberOfMips = textures[i].mipmapCount - 1;

            // Add mips to copyTexture queue in low-high order from correctNumberOfMips..0
            for (int mipLevel = correctNumberOfMips; mipLevel >= 0; mipLevel--)
            {
                int mipSize = texArrayResolution / currentMipSize;
                OvrAvatarSDKManager.Instance.GetTextureCopyManager().CopyTexture(
                    textures[i],
                    texArray,
                    mipLevel,
                    mipSize,
                    i,
                    false);

                currentMipSize /= 2;
            }
        }
    }

    private void SetMaterialPropertyBlock()
    {
        if (TargetRenderer != null)
        {
            for (int i = 0; i < LocalAvatarConfig.ComponentMaterialProperties.Length; i++)
            {
                LocalAvatarConfig.MaterialPropertyBlock.Colors[i]
                    = LocalAvatarConfig.ComponentMaterialProperties[i].Color;
                LocalAvatarConfig.MaterialPropertyBlock.DiffuseIntensities[i] = DiffuseIntensities[i];
                LocalAvatarConfig.MaterialPropertyBlock.RimIntensities[i] = RimIntensities[i];
                LocalAvatarConfig.MaterialPropertyBlock.ReflectionIntensities[i] = ReflectionIntensities[i];
            }
        }
    }

    private void ApplyMaterialPropertyBlock()
    {
        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        materialPropertyBlock.SetVectorArray(AVATAR_SHADER_COLOR,
            LocalAvatarConfig.MaterialPropertyBlock.Colors);
        materialPropertyBlock.SetFloatArray(AVATAR_SHADER_DIFFUSEINTENSITY,
            LocalAvatarConfig.MaterialPropertyBlock.DiffuseIntensities);
        materialPropertyBlock.SetFloatArray(AVATAR_SHADER_RIMINTENSITY,
            LocalAvatarConfig.MaterialPropertyBlock.RimIntensities);
        materialPropertyBlock.SetFloatArray(AVATAR_SHADER_REFLECTIONINTENSITY,
            LocalAvatarConfig.MaterialPropertyBlock.ReflectionIntensities);
        TargetRenderer.GetClosestReflectionProbes(ReflectionProbes);

        if (ReflectionProbes != null && ReflectionProbes.Count > 0 && ReflectionProbes[0].probe.texture != null)
        {
            materialPropertyBlock.SetTexture(AVATAR_SHADER_CUBEMAP, ReflectionProbes[0].probe.texture);
        }

        for (int i = 0; i < TextureArrays.Length; i++)
        {
            materialPropertyBlock.SetTexture(TextureTypeToShaderProperties[i],
                TextureArrays[(int)(TextureType)i].TextureArray);
        }

        TargetRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    // Return a component type based on name
    public static ovrAvatarBodyPartType GetComponentType(string objectName)
    {
        if (objectName.Contains("0"))
        {
            return ovrAvatarBodyPartType.Body;
        }
        else if (objectName.Contains("1"))
        {
            return ovrAvatarBodyPartType.Clothing;
        }
        else if (objectName.Contains("2"))
        {
            return ovrAvatarBodyPartType.Eyewear;
        }
        else if (objectName.Contains("3"))
        {
            return ovrAvatarBodyPartType.Hair;
        }
        else if (objectName.Contains("4"))
        {
            return ovrAvatarBodyPartType.Beard;
        }

        return ovrAvatarBodyPartType.Count;
    }

    UInt64 GetTextureIDForType(ovrAvatarPBSMaterialState materialState, TextureType type)
    {
        if (type == TextureType.DiffuseTextures)
        {
            return materialState.albedoTextureID;
        }
        else if (type == TextureType.NormalMaps)
        {
            return materialState.normalTextureID;
        }
        else if (type == TextureType.RoughnessMaps)
        {
            return materialState.metallicnessTextureID;
        }

        return 0;
    }

    public void ValidateTextures(ovrAvatarPBSMaterialState[] materialStates)
    {
        var props = LocalAvatarConfig.ComponentMaterialProperties;

        int[] heights = new int[(int)TextureType.Count];
        TextureFormat[] formats = new TextureFormat[(int)TextureType.Count];

        for (var propIndex = 0; propIndex < props.Length; propIndex++)
        {
            for (var index = 0; index < props[propIndex].Textures.Length; index++)
            {
                if (props[propIndex].Textures[index] == null)
                {
                    throw new System.Exception(
                        props[propIndex].TypeIndex.ToString()
                        + "Invalid: "
                        + ((TextureType)index).ToString());
                }

                heights[index] = props[propIndex].Textures[index].height;
                formats[index] = props[propIndex].Textures[index].format;
            }
        }

        for (int textureIndex = 0; textureIndex < (int)TextureType.Count; textureIndex++)
        {
            for (var propIndex = 1; propIndex < props.Length; propIndex++)
            {
                if (props[propIndex - 1].Textures[textureIndex].height
                    != props[propIndex].Textures[textureIndex].height)
                {
                    throw new System.Exception(
                        props[propIndex].TypeIndex.ToString()
                        + " Mismatching Resolutions: "
                        + ((TextureType)textureIndex).ToString()
                        + " "
                        + props[propIndex - 1].Textures[textureIndex].height
                        + " (ID: "
                        + GetTextureIDForType(materialStates[propIndex - 1], (TextureType)textureIndex)
                        + ") vs "
                        + props[propIndex].Textures[textureIndex].height
                        + " (ID: "
                        + GetTextureIDForType(materialStates[propIndex], (TextureType)textureIndex)
                        + ") Ensure you are using ASTC texture compression on Android or turn off CombineMeshes");
                }

                if (props[propIndex - 1].Textures[textureIndex].format
                    != props[propIndex].Textures[textureIndex].format)
                {
                    throw new System.Exception(
                        props[propIndex].TypeIndex.ToString()
                        + " Mismatching Formats: "
                        + ((TextureType)textureIndex).ToString()
                        + " "
                        + props[propIndex - 1].Textures[textureIndex].format
                        + " (ID: "
                        + GetTextureIDForType(materialStates[propIndex - 1], (TextureType)textureIndex)
                        + ") vs "
                        + props[propIndex].Textures[textureIndex].format
                        + " (ID: "
                        + GetTextureIDForType(materialStates[propIndex], (TextureType)textureIndex)
                        + ") Ensure you are using ASTC texture compression on Android or turn off CombineMeshes");
                }
            }
        }
    }

    // Loading animation on the Dimmer properyt
    // Smooth sine lerp every 0.3 seconds between 0.25 and 0.5
    private IEnumerator RunLoadingAnimation(Action callBack)
    {
        // Set the material to single component while the avatar loads
        CombinedShader = TargetRenderer.sharedMaterial.shader;

        // Save shader properties
        int srcBlend = TargetRenderer.sharedMaterial.GetInt("_SrcBlend");
        int dstBlend = TargetRenderer.sharedMaterial.GetInt("_DstBlend");
        string lightModeTag = TargetRenderer.sharedMaterial.GetTag("LightMode", false);
        string renderTypeTag = TargetRenderer.sharedMaterial.GetTag("RenderType", false);
        string renderQueueTag = TargetRenderer.sharedMaterial.GetTag("Queue", false);
        string ignoreProjectorTag = TargetRenderer.sharedMaterial.GetTag("IgnoreProjector", false);
        int renderQueue = TargetRenderer.sharedMaterial.renderQueue;
        bool transparentQueue = TargetRenderer.sharedMaterial.IsKeywordEnabled("_ALPHATEST_ON");

        // Swap in loading shader
        TargetRenderer.sharedMaterial.shader = Shader.Find(AVATAR_SHADER_LOADER);
        TargetRenderer.sharedMaterial.SetColor(AVATAR_SHADER_COLOR, Color.white);

        while (OvrAvatarSDKManager.Instance.GetTextureCopyManager().GetTextureCount() > 0)
        {
            float distance = (LOADING_ANIMATION_AMPLITUDE * Mathf.Sin(Time.timeSinceLevelLoad / LOADING_ANIMATION_PERIOD) +
                LOADING_ANIMATION_AMPLITUDE) * (LOADING_ANIMATION_CURVE_SCALE) + LOADING_ANIMATION_DIMMER_MIN;
            TargetRenderer.sharedMaterial.SetFloat(AVATAR_SHADER_LOADING_DIMMER, distance);
            yield return null;
        }
        // Swap back main shader
        TargetRenderer.sharedMaterial.SetFloat(AVATAR_SHADER_LOADING_DIMMER, 1f);
        TargetRenderer.sharedMaterial.shader = CombinedShader;

        // Restore shader properties
        TargetRenderer.sharedMaterial.SetInt("_SrcBlend", srcBlend);
        TargetRenderer.sharedMaterial.SetInt("_DstBlend", dstBlend);
        TargetRenderer.sharedMaterial.SetOverrideTag("LightMode", lightModeTag);
        TargetRenderer.sharedMaterial.SetOverrideTag("RenderType", renderTypeTag);
        TargetRenderer.sharedMaterial.SetOverrideTag("Queue", renderQueueTag);
        TargetRenderer.sharedMaterial.SetOverrideTag("IgnoreProjector", ignoreProjectorTag);
        if (transparentQueue)
        {
            TargetRenderer.sharedMaterial.EnableKeyword("_ALPHATEST_ON");
            TargetRenderer.sharedMaterial.EnableKeyword("_ALPHABLEND_ON");
            TargetRenderer.sharedMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        else
        {
            TargetRenderer.sharedMaterial.DisableKeyword("_ALPHATEST_ON");
            TargetRenderer.sharedMaterial.DisableKeyword("_ALPHABLEND_ON");
            TargetRenderer.sharedMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        TargetRenderer.sharedMaterial.renderQueue = renderQueue;

        ApplyMaterialPropertyBlock();

        if (callBack != null)
        {
            callBack();
        }
    }
}
