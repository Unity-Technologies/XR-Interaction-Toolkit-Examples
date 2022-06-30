using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Oculus.Avatar;

public class OvrAvatarRenderComponent : MonoBehaviour {

    private bool firstSkinnedUpdate = true;
    public SkinnedMeshRenderer mesh;
    public Transform[] bones;
    bool isBodyComponent = false;

    protected void UpdateActive(OvrAvatar avatar, ovrAvatarVisibilityFlags mask)
    {
        bool doActiveHack = isBodyComponent && avatar.EnableExpressive && avatar.ShowFirstPerson && !avatar.ShowThirdPerson;
        if (doActiveHack)
        {
            bool showFirstPerson = (mask & ovrAvatarVisibilityFlags.FirstPerson) != 0;
            bool showThirdPerson = (mask & ovrAvatarVisibilityFlags.ThirdPerson) != 0;
            gameObject.SetActive(showThirdPerson || showThirdPerson);

            if (!showFirstPerson)
            {
                mesh.enabled = false;
            }
        }
        else
        {
            bool active = avatar.ShowFirstPerson && (mask & ovrAvatarVisibilityFlags.FirstPerson) != 0;
            active |= avatar.ShowThirdPerson && (mask & ovrAvatarVisibilityFlags.ThirdPerson) != 0;
            this.gameObject.SetActive(active);
            mesh.enabled = active;
        }
    }

    protected SkinnedMeshRenderer CreateSkinnedMesh(ulong assetID, ovrAvatarVisibilityFlags visibilityMask, int thirdPersonLayer, int firstPersonLayer)
    {
        isBodyComponent = name.Contains("body");

        OvrAvatarAssetMesh meshAsset = (OvrAvatarAssetMesh)OvrAvatarSDKManager.Instance.GetAsset(assetID);
        if (meshAsset == null)
        {
            throw new Exception("Couldn't find mesh for asset " + assetID);
        }
        if ((visibilityMask & ovrAvatarVisibilityFlags.ThirdPerson) != 0)
        {
            this.gameObject.layer = thirdPersonLayer;
        }
        else
        {
            this.gameObject.layer = firstPersonLayer;
        }
        SkinnedMeshRenderer renderer = meshAsset.CreateSkinnedMeshRendererOnObject(gameObject);
#if UNITY_ANDROID
        renderer.quality = SkinQuality.Bone2;
#else
        renderer.quality = SkinQuality.Bone4;
#endif
        renderer.updateWhenOffscreen = true;
        if ((visibilityMask & ovrAvatarVisibilityFlags.SelfOccluding) == 0)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        gameObject.SetActive(false);

        return renderer;
    }

    protected void UpdateSkinnedMesh(OvrAvatar avatar, Transform[] bones, ovrAvatarTransform localTransform, ovrAvatarVisibilityFlags visibilityMask, IntPtr renderPart)
    {
        UpdateActive(avatar, visibilityMask);
        OvrAvatar.ConvertTransform(localTransform, this.transform);
        ovrAvatarRenderPartType type = CAPI.ovrAvatarRenderPart_GetType(renderPart);
        UInt64 dirtyJoints;
        switch (type)
        {
            case ovrAvatarRenderPartType.SkinnedMeshRender:
                dirtyJoints = CAPI.ovrAvatarSkinnedMeshRender_GetDirtyJoints(renderPart);
                break;
            case ovrAvatarRenderPartType.SkinnedMeshRenderPBS:
                dirtyJoints = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetDirtyJoints(renderPart);
                break;
            case ovrAvatarRenderPartType.SkinnedMeshRenderPBS_V2:
                dirtyJoints = CAPI.ovrAvatarSkinnedMeshRenderPBSV2_GetDirtyJoints(renderPart);
                break;
            default:
                throw new Exception("Unhandled render part type: " + type);
        }
        for (UInt32 i = 0; i < 64; i++)
        {
            UInt64 dirtyMask = (ulong)1 << (int)i;
            // We need to make sure that we fully update the initial position of
            // Skinned mesh renderers, then, thereafter, we can only update dirty joints
            if ((firstSkinnedUpdate && i < bones.Length) ||
                (dirtyMask & dirtyJoints) != 0)
            {
                //This joint is dirty and needs to be updated
                Transform targetBone = bones[i];
                ovrAvatarTransform transform;
                switch (type)
                {
                    case ovrAvatarRenderPartType.SkinnedMeshRender:
                        transform = CAPI.ovrAvatarSkinnedMeshRender_GetJointTransform(renderPart, i);
                        break;
                    case ovrAvatarRenderPartType.SkinnedMeshRenderPBS:
                        transform = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetJointTransform(renderPart, i);
                        break;
                    case ovrAvatarRenderPartType.SkinnedMeshRenderPBS_V2:
                        transform = CAPI.ovrAvatarSkinnedMeshRenderPBSV2_GetJointTransform(renderPart, i);
                        break;
                    default:
                        throw new Exception("Unhandled render part type: " + type);
                }
                OvrAvatar.ConvertTransform(transform, targetBone);
            }
        }

        firstSkinnedUpdate = false;
    }

    protected Material CreateAvatarMaterial(string name, Shader shader)
    {
        if (shader == null)
        {
            throw new Exception("No shader provided for avatar material.");
        }
        Material mat = new Material(shader);
        mat.name = name;
        return mat;
    }

   
}
