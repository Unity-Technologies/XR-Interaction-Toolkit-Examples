using UnityEngine;
using System.Collections.Generic;
using System;
using Oculus.Avatar;

public class OvrAvatarSkinnedMeshPBSV2RenderComponent : OvrAvatarRenderComponent
{
    private OvrAvatarMaterialManager avatarMaterialManager;
    private bool previouslyActive = false;
    private bool isCombinedMaterial = false;
    private ovrAvatarExpressiveParameters ExpressiveParameters;
    private bool EnableExpressive = false;
    private int blendShapeCount = 0;
    private ovrAvatarBlendShapeParams blendShapeParams;

    private const string MAIN_MATERIAL_NAME = "main_material";
    private const string EYE_MATERIAL_NAME = "eye_material";
    private const string DEFAULT_MATERIAL_NAME = "_material";

    internal void Initialize(
        IntPtr renderPart,
        ovrAvatarRenderPart_SkinnedMeshRenderPBS_V2 skinnedMeshRender,
        OvrAvatarMaterialManager materialManager,
        int thirdPersonLayer, 
        int firstPersonLayer, 
        bool combinedMesh,
        ovrAvatarAssetLevelOfDetail lod,
        bool assignExpressiveParams,
        OvrAvatar avatar,
        bool isControllerModel)
    {
        avatarMaterialManager = materialManager;
        isCombinedMaterial = combinedMesh;

        mesh = CreateSkinnedMesh(
            skinnedMeshRender.meshAssetID, 
            skinnedMeshRender.visibilityMask, 
            thirdPersonLayer,
            firstPersonLayer);

        EnableExpressive = assignExpressiveParams;

#if UNITY_ANDROID
        var singleComponentShader = EnableExpressive 
            ? avatar.Skinshaded_Expressive_VertFrag_SingleComponent 
            : avatar.Skinshaded_VertFrag_SingleComponent;
#else
        var singleComponentShader = EnableExpressive
            ? avatar.Skinshaded_Expressive_SurfaceShader_SingleComponent
            : avatar.Skinshaded_SurfaceShader_SingleComponent;
#endif
        var combinedComponentShader = EnableExpressive
            ? avatar.Skinshaded_Expressive_VertFrag_CombinedMesh
            : avatar.Skinshaded_VertFrag_CombinedMesh;

        var mainShader = isCombinedMaterial ? combinedComponentShader : singleComponentShader;

        if (isControllerModel)
        {
            mainShader = avatar.ControllerShader;
        }

       AvatarLogger.Log("OvrAvatarSkinnedMeshPBSV2RenderComponent Shader is: " + mainShader != null 
           ? mainShader.name : "null");

        if (EnableExpressive)
        {
            ExpressiveParameters = CAPI.ovrAvatar_GetExpressiveParameters(avatar.sdkAvatar);
            var eyeShader = avatar.EyeLens;

            Material[] matArray = new Material[2];
            matArray[0] = CreateAvatarMaterial(gameObject.name + MAIN_MATERIAL_NAME, mainShader);
            matArray[1] = CreateAvatarMaterial(gameObject.name + EYE_MATERIAL_NAME, eyeShader);

            if (avatar.UseTransparentRenderQueue)
            {
                SetMaterialTransparent(matArray[0]);
            }
            else
            {
                SetMaterialOpaque(matArray[0]);
            }
            // Eye lens shader queue is transparent and set from shader
            matArray[1].renderQueue = -1;
            mesh.materials = matArray;
        }
        else
        {
            mesh.sharedMaterial = CreateAvatarMaterial(gameObject.name + DEFAULT_MATERIAL_NAME, mainShader);
            if (avatar.UseTransparentRenderQueue && !isControllerModel)
            {
                SetMaterialTransparent(mesh.sharedMaterial);
            }
            else
            {
                SetMaterialOpaque(mesh.sharedMaterial);
            }
        }
        bones = mesh.bones;

        if (isCombinedMaterial)
        {
            avatarMaterialManager.SetRenderer(mesh);
            InitializeCombinedMaterial(renderPart, (int)lod);
            avatarMaterialManager.OnCombinedMeshReady();
        }

        blendShapeParams = new ovrAvatarBlendShapeParams();
        blendShapeParams.blendShapeParamCount = 0;
        blendShapeParams.blendShapeParams = new float[64];

        blendShapeCount = mesh.sharedMesh.blendShapeCount;
    }

    public void UpdateSkinnedMeshRender(
        OvrAvatarComponent component, 
        OvrAvatar avatar, 
        IntPtr renderPart)
    {
        ovrAvatarVisibilityFlags visibilityMask 
            = CAPI.ovrAvatarSkinnedMeshRenderPBSV2_GetVisibilityMask(renderPart);

        ovrAvatarTransform localTransform 
            = CAPI.ovrAvatarSkinnedMeshRenderPBSV2_GetTransform(renderPart);

        UpdateSkinnedMesh(avatar, bones, localTransform, visibilityMask, renderPart);

        bool isActive = gameObject.activeSelf;

        if (mesh != null && !previouslyActive && isActive)
        {
            if (!isCombinedMaterial)
            {
                InitializeSingleComponentMaterial(renderPart, (int)avatar.LevelOfDetail - 1);
            }
        }

        if (blendShapeCount > 0)
        {
            const float BLEND_MULTIPLIER = 100.0f;
            CAPI.ovrAvatarSkinnedMeshRender_GetBlendShapeParams(renderPart, ref blendShapeParams);
            for (uint i = 0; i < blendShapeParams.blendShapeParamCount && i < blendShapeCount; i++)
            {
                float value = blendShapeParams.blendShapeParams[i];
                mesh.SetBlendShapeWeight((int)i, value * BLEND_MULTIPLIER);
            }
        }

        previouslyActive = isActive;
    }

    private void InitializeSingleComponentMaterial(IntPtr renderPart, int lodIndex)
    {
        ovrAvatarPBSMaterialState materialState =
            CAPI.ovrAvatarSkinnedMeshRenderPBSV2_GetPBSMaterialState(renderPart);

        int componentType = (int)OvrAvatarMaterialManager.GetComponentType(gameObject.name);

        Texture2D diffuseTexture = OvrAvatarComponent.GetLoadedTexture(materialState.albedoTextureID);
        Texture2D normalTexture = OvrAvatarComponent.GetLoadedTexture(materialState.normalTextureID);
        Texture2D metallicTexture = OvrAvatarComponent.GetLoadedTexture(materialState.metallicnessTextureID);
        
        if (diffuseTexture != null)
        {
            avatarMaterialManager.AddTextureIDToTextureManager(materialState.albedoTextureID, true);
        }
        else
        {
            diffuseTexture = OvrAvatarSDKManager.Instance.GetTextureCopyManager().FallbackTextureSets[lodIndex].DiffuseRoughness;
        }
        diffuseTexture.anisoLevel = 4;
        if (normalTexture != null)
        {
            avatarMaterialManager.AddTextureIDToTextureManager(materialState.normalTextureID, true);
        }
        else
        {
            normalTexture = OvrAvatarSDKManager.Instance.GetTextureCopyManager().FallbackTextureSets[lodIndex].Normal;
        }
        normalTexture.anisoLevel = 4;
        if (metallicTexture != null)
        {
            avatarMaterialManager.AddTextureIDToTextureManager(materialState.metallicnessTextureID, true);
        }
        else
        {
            metallicTexture = OvrAvatarSDKManager.Instance.GetTextureCopyManager().FallbackTextureSets[lodIndex].DiffuseRoughness;
        }
        metallicTexture.anisoLevel = 16;

        mesh.materials[0].SetTexture(OvrAvatarMaterialManager.AVATAR_SHADER_MAINTEX, diffuseTexture);
        mesh.materials[0].SetTexture(OvrAvatarMaterialManager.AVATAR_SHADER_NORMALMAP, normalTexture);
        mesh.materials[0].SetTexture(OvrAvatarMaterialManager.AVATAR_SHADER_ROUGHNESSMAP, metallicTexture);

        mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_COLOR, materialState.albedoMultiplier);

        mesh.materials[0].SetFloat(OvrAvatarMaterialManager.AVATAR_SHADER_DIFFUSEINTENSITY,
            OvrAvatarMaterialManager.DiffuseIntensities[componentType]);
        mesh.materials[0].SetFloat(OvrAvatarMaterialManager.AVATAR_SHADER_RIMINTENSITY,
            OvrAvatarMaterialManager.RimIntensities[componentType]);
        mesh.materials[0].SetFloat(OvrAvatarMaterialManager.AVATAR_SHADER_REFLECTIONINTENSITY,
            OvrAvatarMaterialManager.ReflectionIntensities[componentType]);

        mesh.GetClosestReflectionProbes(avatarMaterialManager.ReflectionProbes);
        if (avatarMaterialManager.ReflectionProbes != null &&
            avatarMaterialManager.ReflectionProbes.Count > 0)
        {
            mesh.materials[0].SetTexture(OvrAvatarMaterialManager.AVATAR_SHADER_CUBEMAP,
                avatarMaterialManager.ReflectionProbes[0].probe.texture);
        }

        if (EnableExpressive)
        {
            mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_IRIS_COLOR, 
                ExpressiveParameters.irisColor);
            mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_LIP_COLOR,
                ExpressiveParameters.lipColor);
            mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_BROW_COLOR,
                ExpressiveParameters.browColor);
            mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_LASH_COLOR,
                ExpressiveParameters.lashColor);
            mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_SCLERA_COLOR,
                ExpressiveParameters.scleraColor);
            mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_GUM_COLOR,
                ExpressiveParameters.gumColor);
            mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_TEETH_COLOR,
                ExpressiveParameters.teethColor);
            mesh.materials[0].SetFloat(OvrAvatarMaterialManager.AVATAR_SHADER_LIP_SMOOTHNESS,
                ExpressiveParameters.lipSmoothness);
        }
    }

    private void InitializeCombinedMaterial(IntPtr renderPart, int lodIndex)
    {
        ovrAvatarPBSMaterialState[] materialStates = CAPI.ovrAvatar_GetBodyPBSMaterialStates(renderPart);

        if (materialStates.Length == (int)ovrAvatarBodyPartType.Count)
        {
            avatarMaterialManager.CreateTextureArrays();
            var localProperties = avatarMaterialManager.LocalAvatarConfig.ComponentMaterialProperties;

            AvatarLogger.Log("InitializeCombinedMaterial - Loading Material States");

            for (int i = 0; i < materialStates.Length; i++)
            {
                localProperties[i].TypeIndex = (ovrAvatarBodyPartType)i;
                localProperties[i].Color = materialStates[i].albedoMultiplier;
                localProperties[i].DiffuseIntensity = OvrAvatarMaterialManager.DiffuseIntensities[i];
                localProperties[i].RimIntensity = OvrAvatarMaterialManager.RimIntensities[i];
                localProperties[i].ReflectionIntensity = OvrAvatarMaterialManager.ReflectionIntensities[i];
                
                var diffuse = OvrAvatarComponent.GetLoadedTexture(materialStates[i].albedoTextureID);
                var normal = OvrAvatarComponent.GetLoadedTexture(materialStates[i].normalTextureID);
                var roughness = OvrAvatarComponent.GetLoadedTexture(materialStates[i].metallicnessTextureID);
                
                if (diffuse != null)
                {
                    localProperties[i].Textures[(int)OvrAvatarMaterialManager.TextureType.DiffuseTextures] = diffuse;
                    avatarMaterialManager.AddTextureIDToTextureManager(materialStates[i].albedoTextureID, false);
                }
                else
                {
                    localProperties[i].Textures[(int)OvrAvatarMaterialManager.TextureType.DiffuseTextures] =
                        OvrAvatarSDKManager.Instance.GetTextureCopyManager().FallbackTextureSets[lodIndex].DiffuseRoughness;
                }
                localProperties[i].Textures[(int)OvrAvatarMaterialManager.TextureType.DiffuseTextures].anisoLevel = 4;

                if (normal != null)
                {
                    localProperties[i].Textures[(int)OvrAvatarMaterialManager.TextureType.NormalMaps] = normal;
                    avatarMaterialManager.AddTextureIDToTextureManager(materialStates[i].normalTextureID, false);
                }
                else
                {
                    localProperties[i].Textures[(int)OvrAvatarMaterialManager.TextureType.NormalMaps] =
                        OvrAvatarSDKManager.Instance.GetTextureCopyManager().FallbackTextureSets[lodIndex].Normal;
                }
                localProperties[i].Textures[(int)OvrAvatarMaterialManager.TextureType.NormalMaps].anisoLevel = 4;

                if (roughness != null)
                {
                    localProperties[i].Textures[(int)OvrAvatarMaterialManager.TextureType.RoughnessMaps] = roughness;
                    avatarMaterialManager.AddTextureIDToTextureManager(materialStates[i].metallicnessTextureID, false);
                }
                else
                {
                    localProperties[i].Textures[(int)OvrAvatarMaterialManager.TextureType.RoughnessMaps] =
                        OvrAvatarSDKManager.Instance.GetTextureCopyManager().FallbackTextureSets[lodIndex].DiffuseRoughness;
                }
                localProperties[i].Textures[(int)OvrAvatarMaterialManager.TextureType.RoughnessMaps].anisoLevel = 16;

                AvatarLogger.Log(localProperties[i].TypeIndex.ToString());
                AvatarLogger.Log(AvatarLogger.Tab + "Diffuse: " + materialStates[i].albedoTextureID);
                AvatarLogger.Log(AvatarLogger.Tab + "Normal: " + materialStates[i].normalTextureID);
                AvatarLogger.Log(AvatarLogger.Tab + "Metallic: " + materialStates[i].metallicnessTextureID);
            }

            if (EnableExpressive)
            {
                mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_IRIS_COLOR,
                    ExpressiveParameters.irisColor);
                mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_LIP_COLOR,
                    ExpressiveParameters.lipColor);
                mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_BROW_COLOR,
                    ExpressiveParameters.browColor);
                mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_LASH_COLOR,
                    ExpressiveParameters.lashColor);
                mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_SCLERA_COLOR,
                    ExpressiveParameters.scleraColor);
                mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_GUM_COLOR,
                    ExpressiveParameters.gumColor);
                mesh.materials[0].SetVector(OvrAvatarMaterialManager.AVATAR_SHADER_TEETH_COLOR,
                    ExpressiveParameters.teethColor);
                mesh.materials[0].SetFloat(OvrAvatarMaterialManager.AVATAR_SHADER_LIP_SMOOTHNESS,
                    ExpressiveParameters.lipSmoothness);
            }

            avatarMaterialManager.ValidateTextures(materialStates);
        }   
    }

    private void SetMaterialTransparent(Material mat)
    {
        // Initialize shader to use transparent render queue with alpha blending
        mat.SetOverrideTag("Queue", "Transparent");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void SetMaterialOpaque(Material mat)
    {
        // Initialize shader to use geometry render queue with no blending
        mat.SetOverrideTag("Queue", "Geometry");
        mat.SetOverrideTag("RenderType", "Opaque");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
    }
}
