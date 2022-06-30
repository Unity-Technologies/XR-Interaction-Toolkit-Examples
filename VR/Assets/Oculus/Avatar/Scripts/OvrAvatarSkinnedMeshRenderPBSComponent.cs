using UnityEngine;
using System.Collections;
using System;
using Oculus.Avatar;

public class OvrAvatarSkinnedMeshRenderPBSComponent : OvrAvatarRenderComponent {

    bool isMaterialInitilized = false;

    internal void Initialize(ovrAvatarRenderPart_SkinnedMeshRenderPBS skinnedMeshRenderPBS, Shader shader, int thirdPersonLayer, int firstPersonLayer)
    {
        if (shader == null)
        {
            shader = Shader.Find("OvrAvatar/AvatarSurfaceShaderPBS");
        }
        mesh = CreateSkinnedMesh(skinnedMeshRenderPBS.meshAssetID, skinnedMeshRenderPBS.visibilityMask, thirdPersonLayer, firstPersonLayer);
        mesh.sharedMaterial = CreateAvatarMaterial(gameObject.name + "_material", shader);
        bones = mesh.bones;
    }

    internal void UpdateSkinnedMeshRenderPBS(OvrAvatar avatar, IntPtr renderPart, Material mat)
    {
        if (!isMaterialInitilized)
        {
            isMaterialInitilized = true;
            UInt64 albedoTextureID = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetAlbedoTextureAssetID(renderPart);
            UInt64 surfaceTextureID = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetSurfaceTextureAssetID(renderPart);
            mat.SetTexture("_Albedo", OvrAvatarComponent.GetLoadedTexture(albedoTextureID));
            mat.SetTexture("_Surface", OvrAvatarComponent.GetLoadedTexture(surfaceTextureID));
        }

        ovrAvatarVisibilityFlags visibilityMask = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetVisibilityMask(renderPart);
        ovrAvatarTransform localTransform = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetTransform(renderPart);
        UpdateSkinnedMesh(avatar, bones, localTransform, visibilityMask, renderPart);


    }
}
