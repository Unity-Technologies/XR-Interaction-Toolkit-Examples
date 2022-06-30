using System;
using Oculus.Avatar;
using UnityEngine;

public class OvrAvatarAssetTexture : OvrAvatarAsset
{
    public Texture2D texture;
    private const int ASTCHeaderSize = 16;

    public OvrAvatarAssetTexture(UInt64 _assetId, IntPtr asset) {
        assetID = _assetId;
        ovrAvatarTextureAssetData textureAssetData = CAPI.ovrAvatarAsset_GetTextureData(asset);
        TextureFormat format;
        IntPtr textureData = textureAssetData.textureData;
        int textureDataSize = (int)textureAssetData.textureDataSize;

        AvatarLogger.Log(
            "OvrAvatarAssetTexture - "
            + _assetId
            + ": "
            + textureAssetData.format.ToString()
            + " "
            + textureAssetData.sizeX
            + "x"
            + textureAssetData.sizeY);

        switch (textureAssetData.format)
        {
            case ovrAvatarTextureFormat.RGB24:
                format = TextureFormat.RGB24;
                break;
            case ovrAvatarTextureFormat.DXT1:
                format = TextureFormat.DXT1;
                break;
            case ovrAvatarTextureFormat.DXT5:
                format = TextureFormat.DXT5;
                break;
            case ovrAvatarTextureFormat.ASTC_RGB_6x6:
#if UNITY_2020_1_OR_NEWER
                format = TextureFormat.ASTC_6x6;
#else
                format = TextureFormat.ASTC_RGB_6x6;
#endif
                textureData = new IntPtr(textureData.ToInt64() + ASTCHeaderSize);
                textureDataSize -= ASTCHeaderSize;
                break;
            case ovrAvatarTextureFormat.ASTC_RGB_6x6_MIPMAPS:
#if UNITY_2020_1_OR_NEWER
                format = TextureFormat.ASTC_6x6;
#else
                format = TextureFormat.ASTC_RGB_6x6;
#endif
                break;
            default:
                throw new NotImplementedException(
                    string.Format("Unsupported texture format {0}",
                                  textureAssetData.format.ToString()));
        }
        texture = new Texture2D(
            (int)textureAssetData.sizeX, (int)textureAssetData.sizeY,
            format, textureAssetData.mipCount > 1,
            QualitySettings.activeColorSpace == ColorSpace.Gamma ? false : true)
        {
            filterMode = FilterMode.Trilinear,
            anisoLevel = 4,
        };
        texture.LoadRawTextureData(textureData, textureDataSize);
        texture.Apply(true, false);
    }
}
