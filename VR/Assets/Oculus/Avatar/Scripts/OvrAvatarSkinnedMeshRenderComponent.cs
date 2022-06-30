using UnityEngine;
using System.Collections;
using System;
using Oculus.Avatar;

public class OvrAvatarSkinnedMeshRenderComponent : OvrAvatarRenderComponent
{
    Shader surface;
    Shader surfaceSelfOccluding;
    bool previouslyActive = false;
        
    internal void Initialize(ovrAvatarRenderPart_SkinnedMeshRender skinnedMeshRender, Shader surface, Shader surfaceSelfOccluding, int thirdPersonLayer, int firstPersonLayer)
    {
        this.surfaceSelfOccluding = surfaceSelfOccluding != null ? surfaceSelfOccluding :  Shader.Find("OvrAvatar/AvatarSurfaceShaderSelfOccluding");
        this.surface = surface != null ? surface : Shader.Find("OvrAvatar/AvatarSurfaceShader");
        this.mesh = CreateSkinnedMesh(skinnedMeshRender.meshAssetID, skinnedMeshRender.visibilityMask, thirdPersonLayer, firstPersonLayer);
        bones = mesh.bones;
        UpdateMeshMaterial(skinnedMeshRender.visibilityMask, mesh);
    }

    public void UpdateSkinnedMeshRender(OvrAvatarComponent component, OvrAvatar avatar, IntPtr renderPart)
    {
        ovrAvatarVisibilityFlags visibilityMask = CAPI.ovrAvatarSkinnedMeshRender_GetVisibilityMask(renderPart);
        ovrAvatarTransform localTransform = CAPI.ovrAvatarSkinnedMeshRender_GetTransform(renderPart);
        UpdateSkinnedMesh(avatar, bones, localTransform, visibilityMask, renderPart);

        UpdateMeshMaterial(visibilityMask, mesh);
        bool isActive = this.gameObject.activeSelf;

        if( mesh != null )
        {
            bool changedMaterial = CAPI.ovrAvatarSkinnedMeshRender_MaterialStateChanged(renderPart);
            if (changedMaterial || (!previouslyActive && isActive))
            {
                ovrAvatarMaterialState materialState = CAPI.ovrAvatarSkinnedMeshRender_GetMaterialState(renderPart);
                component.UpdateAvatarMaterial(mesh.sharedMaterial, materialState);
            }
        }
        previouslyActive = isActive;
    }

    private void UpdateMeshMaterial(ovrAvatarVisibilityFlags visibilityMask, SkinnedMeshRenderer rootMesh)
    {
        Shader shader = (visibilityMask & ovrAvatarVisibilityFlags.SelfOccluding) != 0 ? surfaceSelfOccluding : surface;
        if (rootMesh.sharedMaterial == null || rootMesh.sharedMaterial.shader != shader)
        {
            rootMesh.sharedMaterial = CreateAvatarMaterial(gameObject.name + "_material", shader);
        }
    }
}
