using UnityEngine;
using System;
using System.Runtime.InteropServices;
using AOT;
using Oculus.Avatar;

//This needs to be the csharp equivalent of ovrAvatarCapabilities in OVR_Avatar.h
[Flags]
public enum ovrAvatarCapabilities
{
    Body = 1 << 0,
    Hands = 1 << 1,
    Base = 1 << 2,
    BodyTilt = 1 << 4,
    Expressive = 1 << 5,
    All = -1
};

// This needs to be the csharp equivalent of ovrAvatarMessageType in OVR_Avatar.h
public enum ovrAvatarMessageType {
    AvatarSpecification,
    AssetLoaded,
    Count
};

// This needs to be the csharp equivalent of ovrAvatarMessage_AvatarSpecification in OVR_Avatar.h
public struct ovrAvatarMessage_AvatarSpecification {
    public IntPtr avatarSpec; //ovrAvatarSpecification*, opaque pointer
    public UInt64 oculusUserID;
};

// This needs to be the csharp equivalent of ovrAvatarMessage_AssetLoaded in OVR_Avatar.h
public struct ovrAvatarMessage_AssetLoaded {
    public UInt64 assetID;
    public IntPtr asset; //ovrAvatarAsset*, opaque pointer
};

// This needs to be the csharp equivalent of ovrAvatarAssetType in OVR_Avatar.h
public enum ovrAvatarAssetType {
    Mesh,
    Texture,
    Pose,
    Material,
    CombinedMesh,
    PBSMaterial,
    FailedLoad,
    Count
};

// This needs to be the csharp equivalent of ovrAvatarMeshVertex in OVR_Avatar.h
public struct ovrAvatarMeshVertex
{
    public float x;
    public float y;
    public float z;
    public float nx;
    public float ny;
    public float nz;
    public float tx;
    public float ty;
    public float tz;
    public float tw;
    public float u;
    public float v;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] blendIndices;     ///< Indices into the bind pose

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] blendWeights;     ///< Blend weights for each component in the bind pose
};

// This needs to be the csharp equivalent of ovrAvatarMeshVertex in OVR_Avatar.h
public struct ovrAvatarMeshVertexV2
{
    public float x;
    public float y;
    public float z;
    public float nx;
    public float ny;
    public float nz;
    public float tx;
    public float ty;
    public float tz;
    public float tw;
    public float u;
    public float v;
    public float r;
    public float g;
    public float b;
    public float a;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] blendIndices;     ///< Indices into the bind pose

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] blendWeights;     ///< Blend weights for each component in the bind pose
};

// This needs to be the csharp equivalent of ovrAvatarMeshVertex in OVR_Avatar.h
public struct ovrAvatarBlendVertex
{
    public float x;
    public float y;
    public float z;
    public float nx;
    public float ny;
    public float nz;
    public float tx;
    public float ty;
    public float tz;
};

// This needs to be the csharp equivalent of ovrAvatarMeshAssetData in OVR_Avatar.h
public struct ovrAvatarMeshAssetData
{
    public UInt32 vertexCount;
    public IntPtr vertexBuffer; //const ovrAvatarMeshVertex*
    public UInt32 indexCount;
    public IntPtr indexBuffer; //const uint16t*
    public ovrAvatarSkinnedMeshPose skinnedBindPose;
};

/// Mesh Asset Data V2
///
public struct ovrAvatarMeshAssetDataV2
{
    public UInt32 vertexCount;
    public IntPtr vertexBuffer; //const ovrAvatarMeshVertexV2*
    public UInt32 indexCount;
    public IntPtr indexBuffer; //const uint16t*
    public ovrAvatarSkinnedMeshPose skinnedBindPose;
};

// This needs to be the csharp equivalent of ovrAvatarTextureFormat in OVR_Avatar.h
public enum ovrAvatarTextureFormat {
    RGB24,
    DXT1,
    DXT5,
    ASTC_RGB_6x6,
    ASTC_RGB_6x6_MIPMAPS,
    Count
};

// This needs to be the csharp equivalent of ovrAvatarTextureAssetData in OVR_Avatar.h
public struct ovrAvatarTextureAssetData {
    public ovrAvatarTextureFormat format;
    public UInt32 sizeX;
    public UInt32 sizeY;
    public UInt32 mipCount;
    public UInt64 textureDataSize;
    public IntPtr textureData; // const uint8_t*
};

// This needs to be the csharp equivalent of ovrAvatarRenderPartType in OVR_Avatar.h
public enum ovrAvatarRenderPartType
{
    SkinnedMeshRender,
    SkinnedMeshRenderPBS,
    ProjectorRender,
    SkinnedMeshRenderPBS_V2,
    Count
};

/// Avatar Logging Level
/// Matches the Android Log Levels
public enum ovrAvatarLogLevel
{
    Unknown,
    Default,
    Verbose,
    Debug,
    Info,
    Warn,
    Error,
    Fatal,
    Silent
};

// This needs to be the csharp equivalent of ovrAvatarTransform in OVR_Avatar.h
public struct ovrAvatarTransform
{
    public Vector3 position;
    public Quaternion orientation;
    public Vector3 scale;
};

// This needs to be the csharp equivalent of ovrAvatarButton in OVR_Avatar.h
[Flags]
public enum ovrAvatarButton
{
    One = 0x0001,
    Two = 0x0002,
    Three = 0x0004,
    Joystick = 0x0008,
}

// This needs to be the csharp equivalent of ovrAvatarTouch in OVR_Avatar.h
[Flags]
public enum ovrAvatarTouch
{
    One = 0x0001,
    Two = 0x0002,
    Joystick = 0x0004,
    ThumbRest = 0x0008,
    Index = 0x0010,
    Pointing = 0x0040,
    ThumbUp = 0x0080,
}

// This needs to be the csharp equivalent of ovrAvatarHandInputState in OVR_Avatar.h
public struct ovrAvatarHandInputState
{
    public ovrAvatarTransform transform;
    public ovrAvatarButton buttonMask;
    public ovrAvatarTouch touchMask;
    public float joystickX;
    public float joystickY;
    public float indexTrigger;
    public float handTrigger;
    [MarshalAs(UnmanagedType.I1)]
    public bool isActive;
};

// This needs to be the csharp equivalent of ovrAvatarComponent in OVR_Avatar.h
public struct ovrAvatarComponent
{
    public ovrAvatarTransform transform;
    public UInt32 renderPartCount;
    public IntPtr renderParts; //const ovrAvatarRenderPart* const*

    [MarshalAs(UnmanagedType.LPStr)]
    public string name;
};

struct ovrAvatarComponent_Offsets
{
    public static long transform = Marshal.OffsetOf(typeof(ovrAvatarComponent), "transform").ToInt64();
    public static Int32 renderPartCount = Marshal.OffsetOf(typeof(ovrAvatarComponent), "renderPartCount").ToInt32();
    public static Int32 renderParts = Marshal.OffsetOf(typeof(ovrAvatarComponent), "renderParts").ToInt32();
    public static Int32 name = Marshal.OffsetOf(typeof(ovrAvatarComponent), "name").ToInt32();
};

// This needs to be the csharp equivalent of ovrAvatarBodyComponent in OVR_Avatar.h
public struct ovrAvatarBaseComponent
{
    public Vector3 basePosition;
    public IntPtr renderComponent; //const ovrAvatarComponent*
};

// This needs to be the csharp equivalent of ovrAvatarBodyComponent in OVR_Avatar.h
public struct ovrAvatarBodyComponent {
    public ovrAvatarTransform  leftEyeTransform;
    public ovrAvatarTransform  rightEyeTransform;
    public ovrAvatarTransform  centerEyeTransform;
    public IntPtr              renderComponent; //const ovrAvatarComponent*
};

public struct ovrAvatarBodyComponent_Offsets
{
    public static long leftEyeTransform = Marshal.OffsetOf(typeof(ovrAvatarBodyComponent), "leftEyeTransform").ToInt64();
    public static long rightEyeTransform = Marshal.OffsetOf(typeof(ovrAvatarBodyComponent), "rightEyeTransform").ToInt64();
    public static long centerEyeTransform = Marshal.OffsetOf(typeof(ovrAvatarBodyComponent), "centerEyeTransform").ToInt64();
    public static long renderComponent = Marshal.OffsetOf(typeof(ovrAvatarBodyComponent), "renderComponent").ToInt64();
};

// This needs to be the csharp equivalent of ovrAvatarControllerComponent in OVR_Avatar.h
public struct ovrAvatarControllerComponent
{
    public ovrAvatarHandInputState inputState;
    public IntPtr renderComponent; //const ovrAvatarComponent*
};

// This needs to be the csharp equivalent of ovrAvatarHandComponent in OVR_Avatar.h
public struct ovrAvatarHandComponent {
    public ovrAvatarHandInputState inputState;
    public IntPtr renderComponent; //const ovrAvatarComponent*
};

// This needs to be the csharp equivalent of ovrAvatarMaterialLayerBlendMode in OVR_Avatar.h
public enum ovrAvatarMaterialLayerBlendMode{
    Add,
    Multiply,
    Count
};

// This needs to be the csharp equivalent of ovrAvatarMaterialLayerSampleMode in OVR_Avatar.h
public enum ovrAvatarMaterialLayerSampleMode{
    Color,
    Texture,
    TextureSingleChannel,
    Parallax,
    Count
};

// This needs to be the csharp equivalent of ovrAvatarMaterialLayerMaskType in OVR_Avatar.h
public enum ovrAvatarMaterialMaskType{
    None,
    Positional,
    ViewReflection,
    Fresnel,
    Pulse,
    Count
};

// This needs to be the csharp equivalent of Controller Types from OVR_Avatar.h
public enum ovrAvatarControllerType
{
    Touch,
    Malibu, 
    Go,
    Quest,

    Count,
};

public enum ovrAvatarAssetLevelOfDetail
{
    Lowest = 1,
    Medium = 3,
    Highest = 5,
};

public enum ovrAvatarLookAndFeelVersion
{
    Unknown = -1,
    One = 0,
    Two = 1,
};

// This needs to be the csharp equivalent of ovrAvatarMaterialLayerState in OVR_Avatar.h
public struct ovrAvatarMaterialLayerState{
    public ovrAvatarMaterialLayerBlendMode  blendMode;
    public ovrAvatarMaterialLayerSampleMode sampleMode;
    public ovrAvatarMaterialMaskType        maskType;
    public Vector4                          layerColor;
    public Vector4                          sampleParameters;
    public UInt64                           sampleTexture;
    public Vector4                          sampleScaleOffset;
    public Vector4                          maskParameters;
    public Vector4                          maskAxis;

    static bool VectorEquals(Vector4 a, Vector4 b)
    {
        return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is ovrAvatarMaterialLayerState))
        {
            return false;
        }
        ovrAvatarMaterialLayerState other = (ovrAvatarMaterialLayerState)obj;
        if (blendMode != other.blendMode) return false;
        if (sampleMode != other.sampleMode) return false;
        if (maskType != other.maskType) return false;
        if (!VectorEquals(layerColor, other.layerColor)) return false;
        if (!VectorEquals(sampleParameters, other.sampleParameters)) return false;
        if (sampleTexture != other.sampleTexture) return false;
        if (!VectorEquals(sampleScaleOffset, other.sampleScaleOffset)) return false;
        if (!VectorEquals(maskParameters, other.maskParameters)) return false;
        if (!VectorEquals(maskAxis, other.maskAxis)) return false;
        return true;
    }
    public override int GetHashCode()
    {
        return blendMode.GetHashCode() ^
            sampleMode.GetHashCode() ^
            maskType.GetHashCode() ^
            layerColor.GetHashCode() ^
            sampleParameters.GetHashCode() ^
            sampleTexture.GetHashCode() ^
            sampleScaleOffset.GetHashCode() ^
            maskParameters.GetHashCode() ^
            maskAxis.GetHashCode();
    }
};

// This needs to be the csharp equivalent of ovrAvatarMaterialState in OVR_Avatar.h
public struct ovrAvatarMaterialState
{
    public Vector4 baseColor;
    public ovrAvatarMaterialMaskType baseMaskType;
    public Vector4 baseMaskParameters;
    public Vector4 baseMaskAxis;
    public ovrAvatarMaterialLayerSampleMode sampleMode;
    public UInt64 alphaMaskTextureID;
    public Vector4 alphaMaskScaleOffset;
    public UInt64 normalMapTextureID;
    public Vector4 normalMapScaleOffset;
    public UInt64 parallaxMapTextureID;
    public Vector4 parallaxMapScaleOffset;
    public UInt64 roughnessMapTextureID;
    public Vector4 roughnessMapScaleOffset;
    public UInt32 layerCount;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public ovrAvatarMaterialLayerState[] layers;

    static bool VectorEquals(Vector4 a, Vector4 b)
    {
        return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is ovrAvatarMaterialState))
        {
            return false;
        }
        ovrAvatarMaterialState other = (ovrAvatarMaterialState)obj;
        if (!VectorEquals(baseColor, other.baseColor)) return false;
        if (baseMaskType != other.baseMaskType) return false;
        if (!VectorEquals(baseMaskParameters, other.baseMaskParameters)) return false;
        if (!VectorEquals(baseMaskAxis, other.baseMaskAxis)) return false;
        if (sampleMode != other.sampleMode) return false;
        if (alphaMaskTextureID != other.alphaMaskTextureID) return false;
        if (!VectorEquals(alphaMaskScaleOffset, other.alphaMaskScaleOffset)) return false;
        if (normalMapTextureID != other.normalMapTextureID) return false;
        if (!VectorEquals(normalMapScaleOffset, other.normalMapScaleOffset)) return false;
        if (parallaxMapTextureID != other.parallaxMapTextureID) return false;
        if (!VectorEquals(parallaxMapScaleOffset, other.parallaxMapScaleOffset)) return false;
        if (roughnessMapTextureID != other.roughnessMapTextureID) return false;
        if (!VectorEquals(roughnessMapScaleOffset, other.roughnessMapScaleOffset)) return false;
        if (layerCount != other.layerCount) return false;
        for (int i = 0; i < layerCount; ++i)
        {
            if (!layers[i].Equals(other.layers[i])) return false;
        }
        return true;
    }

    public override int GetHashCode()
    {
        int hash = 0;
        hash ^= baseColor.GetHashCode();
        hash ^= baseMaskType.GetHashCode();
        hash ^= baseMaskParameters.GetHashCode();
        hash ^= baseMaskAxis.GetHashCode();
        hash ^= sampleMode.GetHashCode();
        hash ^= alphaMaskTextureID.GetHashCode();
        hash ^= alphaMaskScaleOffset.GetHashCode();
        hash ^= normalMapTextureID.GetHashCode();
        hash ^= normalMapScaleOffset.GetHashCode();
        hash ^= parallaxMapTextureID.GetHashCode();
        hash ^= parallaxMapScaleOffset.GetHashCode();
        hash ^= roughnessMapTextureID.GetHashCode();
        hash ^= roughnessMapScaleOffset.GetHashCode();
        hash ^= layerCount.GetHashCode();
        for (int i = 0; i < layerCount; ++i)
        {
            hash ^= layers[i].GetHashCode();
        }
        return hash;
    }
};

public struct ovrAvatarExpressiveParameters
{
    public Vector4 irisColor;
    public Vector4 scleraColor;
    public Vector4 lashColor;
    public Vector4 browColor;
    public Vector4 lipColor;
    public Vector4 teethColor;
    public Vector4 gumColor;
    public float browLashIntensity;
    public float lipSmoothness;

    static bool VectorEquals(Vector4 a, Vector4 b)
    {
        return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
    }
    public override bool Equals(object obj)
    {
        if (!(obj is ovrAvatarExpressiveParameters))
        {
            return false;
        }
        ovrAvatarExpressiveParameters other = (ovrAvatarExpressiveParameters)obj;
        if (!VectorEquals(irisColor, other.irisColor)) return false;
        if (!VectorEquals(scleraColor, other.scleraColor)) return false;
        if (!VectorEquals(lashColor, other.lashColor)) return false;
        if (!VectorEquals(browColor, other.browColor)) return false;
        if (!VectorEquals(lipColor, other.lipColor)) return false;
        if (!VectorEquals(teethColor, other.teethColor)) return false;
        if (!VectorEquals(gumColor, other.gumColor)) return false;
        if (browLashIntensity != other.browLashIntensity) return false;
        if (lipSmoothness != other.lipSmoothness) return false;

        return true;
    }
    public override int GetHashCode()
    {
        return irisColor.GetHashCode() ^
            scleraColor.GetHashCode() ^
            lashColor.GetHashCode() ^
            browColor.GetHashCode() ^
            lipColor.GetHashCode() ^
            teethColor.GetHashCode() ^
            gumColor.GetHashCode() ^
            browLashIntensity.GetHashCode() ^
            lipSmoothness.GetHashCode();
    }
}

public struct ovrAvatarPBSMaterialState
{
    public Vector4 baseColor;               ///< Underlying base color for the material
    public UInt64  albedoTextureID;         ///< Texture id for the albedo map
    public Vector4 albedoMultiplier;        ///< Multiplier for albedo texture sample
    public UInt64  metallicnessTextureID;   ///< Texture id for the metal map
    public float   glossinessScale;         ///< Glossiness factor
    public UInt64  normalTextureID;         ///< Texture id for the normal map
    public UInt64  heightTextureID;         ///< Texture id for the height map
    public UInt64  occlusionTextureID;      ///< Texture id for the occlusion map
    public UInt64  emissionTextureID;       ///< Texture id for the emission map
    public Vector4 emissionMultiplier;      ///< Multiplier for emission texture sample
    public UInt64  detailMaskTextureID;     ///< Texture id for the detail mask map
    public UInt64  detailAlbedoTextureID;   ///< Texture id for the detail albedo map
    public UInt64  detailNormalTextureID;   ///< Texture id for the detail normal map
    static bool VectorEquals(Vector4 a, Vector4 b)
    {
        return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is ovrAvatarPBSMaterialState))
        {
            return false;
        }
        ovrAvatarPBSMaterialState other = (ovrAvatarPBSMaterialState)obj;
        if (!VectorEquals(baseColor, other.baseColor)) return false;
        if (albedoTextureID != other.albedoTextureID) return false;
        if (!VectorEquals(albedoMultiplier, other.albedoMultiplier)) return false;
        if (metallicnessTextureID != other.metallicnessTextureID) return false;
        if (glossinessScale != other.glossinessScale) return false;
        if (normalTextureID != other.normalTextureID) return false;
        if (heightTextureID != other.heightTextureID) return false;
        if (occlusionTextureID != other.occlusionTextureID) return false;
        if (emissionTextureID != other.emissionTextureID) return false;
        if (!VectorEquals(emissionMultiplier, other.emissionMultiplier)) return false;
        if (detailMaskTextureID != other.detailMaskTextureID) return false;
        if (detailAlbedoTextureID != other.detailAlbedoTextureID) return false;
        if (detailNormalTextureID != other.detailNormalTextureID) return false;
        return true;
    }
    public override int GetHashCode()
    {
        return baseColor.GetHashCode() ^
            albedoTextureID.GetHashCode() ^
            albedoMultiplier.GetHashCode() ^
            metallicnessTextureID.GetHashCode() ^
            glossinessScale.GetHashCode() ^
            normalTextureID.GetHashCode() ^
            heightTextureID.GetHashCode() ^
            occlusionTextureID.GetHashCode() ^
            emissionTextureID.GetHashCode() ^
            emissionMultiplier.GetHashCode() ^
            detailMaskTextureID.GetHashCode() ^
            detailAlbedoTextureID.GetHashCode() ^
            detailNormalTextureID.GetHashCode();
    }
};

public class OvrAvatarAssetMaterial : OvrAvatarAsset
{
    public OvrAvatarAssetMaterial(UInt64 id, IntPtr mat) 
    {
        assetID = id;
        material = CAPI.ovrAvatarAsset_GetMaterialState(mat);
    }

    public ovrAvatarMaterialState material;
}
// This needs to be the csharp equivalent of ovrAvatarSkinnedMeshPose in OVR_Avatar.h
public struct ovrAvatarSkinnedMeshPose
{
    public UInt32 jointCount;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public ovrAvatarTransform[] jointTransform;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public int[] jointParents;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public IntPtr[] jointNames; //const char * jointNames[64];
};


[Flags]
public enum ovrAvatarVisibilityFlags
{
    FirstPerson = 1 << 0,
    ThirdPerson = 1 << 1,
    SelfOccluding = 1 << 2,
};

// This needs to be the csharp equivalent of ovrAvatarRenderPart_SkinnedMeshRender in OVR_Avatar.h
public struct ovrAvatarRenderPart_SkinnedMeshRender
{
    public ovrAvatarTransform localTransform;
    public ovrAvatarVisibilityFlags visibilityMask;
    public UInt64 meshAssetID;
    public ovrAvatarMaterialState materialState;
    public ovrAvatarSkinnedMeshPose skinnedPose;
};

// This needs to be the csharp equivalent of ovrAvatarRenderPart_SkinnedMeshRenderPBS in OVR_Avatar.h
public struct ovrAvatarRenderPart_SkinnedMeshRenderPBS
{
    public ovrAvatarTransform localTransform;
    public ovrAvatarVisibilityFlags visibilityMask;
    public UInt64 meshAssetID;
    public UInt64 albedoTextureAssetID;
    public UInt64 surfaceTextureAssetID;
    public ovrAvatarSkinnedMeshPose skinnedPose;
};

// This needs to be the csharp equivalent of ovrAvatarRenderPart_ProjectorRender in OVR_Avatar.h
public struct ovrAvatarRenderPart_ProjectorRender
{
    public ovrAvatarTransform localTransform;
    public UInt32 componentIndex;
    public UInt32 renderPartIndex;
    public ovrAvatarMaterialState materialState;
};

// This needs to be the csharp equivalent of ovrAvatarRenderPart_SkinnedMeshRenderPBS_V2 in OVR_Avatar.h
public struct ovrAvatarRenderPart_SkinnedMeshRenderPBS_V2
{
    public ovrAvatarTransform        localTransform;
    public ovrAvatarVisibilityFlags  visibilityMask;
    public UInt64                    meshAssetID;
    public ovrAvatarPBSMaterialState materialState;
    public ovrAvatarSkinnedMeshPose  skinnedPose;
};

// This needs to be the csharp equivalent of ovrAvatarHandGesture in OVR_Avatar.h
public enum ovrAvatarHandGesture {
    Default,
    GripSphere,
    GripCube,
    Count
};

public enum ovrAvatarBodyPartType
{
    Body,
    Clothing,
    Eyewear,
    Hair,
    Beard,
    Count
};

// This needs to be the csharp equivalent of ovrAvatarBlendShapeParams in OVR_Avatar.h
public struct ovrAvatarBlendShapeParams
{
    public UInt32 blendShapeParamCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public float[] blendShapeParams;
};

struct ovrAvatarBlendShapeParams_Offsets
{
    public static Int32 blendShapeParamCount = Marshal.OffsetOf(typeof(ovrAvatarBlendShapeParams), "blendShapeParamCount").ToInt32();
    // Bug with Marshal.OffsetOf is returning an incorrect offset, causing an off by 1 float issue in the blendShapeParams
    //public static long blendShapeParams = Marshal.OffsetOf(typeof(ovrAvatarBlendShapeParams), "blendShapeParams").ToInt64();
    public static long blendShapeParams = Marshal.SizeOf(typeof(UInt32));
};

// This needs to be the csharp equivalent of ovrAvatarVisemes in OVR_Avatar.h
public struct ovrAvatarVisemes
{
    public UInt32 visemeParamCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public float[] visemeParams;
};

struct ovrAvatarVisemes_Offsets
{
    public static Int32 visemeParamCount = Marshal.OffsetOf(typeof(ovrAvatarVisemes), "visemeParamCount").ToInt32();
    // Bug with Marshal.OffsetOf is returning an incorrect offset, causing an off by 1 float issue in the visemeParams
    //public static long visemeParams = Marshal.OffsetOf(typeof(ovrAvatarVisemes), "visemeParams").ToInt64();
    public static long visemeParams = Marshal.SizeOf(typeof(UInt32));
};

// This needs to be the csharp equivalent of ovrAvatarGazeTargetType in OVR_AvatarInternal.h
public enum ovrAvatarGazeTargetType {
    AvatarHead = 0,
    AvatarHand,
    Object,
    ObjectStatic,
    Count,
};

// This needs to be the csharp equivalent of ovrAvatarGazeTarget in OVR_AvatarInternal.h
public struct ovrAvatarGazeTarget
{
    public UInt32 id;
    public Vector3 worldPosition;
    public ovrAvatarGazeTargetType type;
};

struct ovrAvatarGazeTarget_Offsets
{
    public static Int32 id = 0;
    public static Int32 worldPosition = Marshal.SizeOf(typeof(UInt32));
    public static Int32 type = worldPosition + Marshal.SizeOf(typeof(Vector3));
};

public struct ovrAvatarGazeTargets
{
    public UInt32 targetCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
    public ovrAvatarGazeTarget[] targets;
};

struct ovrAvatarGazeTargets_Offsets
{
    public static Int32 targetCount = Marshal.OffsetOf(typeof(ovrAvatarGazeTargets), "targetCount").ToInt32();
    // Bug with Marshal.OffsetOf is returning an incorrect offset, causing an off by 1 float issue in the targets
    //public static long targets = Marshal.OffsetOf(typeof(ovrAvatarGazeTargets), "targets").ToInt64();
    public static long targets = Marshal.SizeOf(typeof(UInt32));
};

// This needs to be the csharp equivalent of ovrAvatarLightType in OVR_AvatarInternal.h
public enum ovrAvatarLightType {
    Point = 0,
    Direction,
    Spot,
    Count,
};

// This needs to be the csharp equivalent of ovrAvatarLight in OVR_AvatarInternal.h
public struct ovrAvatarLight
{
    public UInt32 id;
    public ovrAvatarLightType type;
    public float intensity;
    public Vector3 worldDirection;
    public Vector3 worldPosition;
    public float range;
    public float spotAngleDeg;
};

struct ovrAvatarLight_Offsets
{
    public static long id = Marshal.OffsetOf(typeof(ovrAvatarLight), "id").ToInt64();
    public static long type = Marshal.OffsetOf(typeof(ovrAvatarLight), "type").ToInt64();
    public static long intensity = Marshal.OffsetOf(typeof(ovrAvatarLight), "intensity").ToInt64();
    public static long worldDirection = Marshal.OffsetOf(typeof(ovrAvatarLight), "worldDirection").ToInt64();
    public static long worldPosition = Marshal.OffsetOf(typeof(ovrAvatarLight), "worldPosition").ToInt64();
    public static long range = Marshal.OffsetOf(typeof(ovrAvatarLight), "range").ToInt64();
    public static long spotAngleDeg = Marshal.OffsetOf(typeof(ovrAvatarLight), "spotAngleDeg").ToInt64();
};

public struct ovrAvatarLights
{
    public float ambientIntensity;
    public UInt32 lightCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public ovrAvatarLight[] lights;
};

struct ovrAvatarLights_Offsets
{
    public static long ambientIntensity = Marshal.OffsetOf(typeof(ovrAvatarLights), "ambientIntensity").ToInt64();
    public static long lightCount = Marshal.OffsetOf(typeof(ovrAvatarLights), "lightCount").ToInt64();
    public static long lights = Marshal.OffsetOf(typeof(ovrAvatarLights), "lights").ToInt64();
};

// Debug Render
[Flags]
public enum ovrAvatarDebugContext : uint
{
    None = 0,
    GazeTarget = 0x01,
    Any = 0xffffffff
};

public struct ovrAvatarDebugLine
{
    public Vector3 startPoint;
    public Vector3 endPoint;
    public Vector3 color;
    public ovrAvatarDebugContext context;
    public IntPtr text;
};
public struct ovrAvatarDebugTransform
{
    public ovrAvatarTransform transform;
    public ovrAvatarDebugContext context;
    public IntPtr text;
};

namespace Oculus.Avatar
{
    public class CAPI
    {
#if UNITY_ANDROID && !UNITY_EDITOR
#if AVATAR_XPLAT
        private const string LibFile = "ovravatar";
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_Initialize(string appID);
#else
        private const string LibFile = "ovravatarloader";
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_InitializeAndroidUnity(string appID);
#endif
#else
        private const string LibFile = "libovravatar";

        public static readonly System.Version AvatarSDKVersion = new System.Version(1, 36, 0);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_Initialize(string appID);
#endif

        static IntPtr nativeVisemeData = IntPtr.Zero;
        static IntPtr nativeGazeTargetsData = IntPtr.Zero;
        static IntPtr nativeAvatarLightsData = IntPtr.Zero;
        static IntPtr DebugLineCountData = IntPtr.Zero;
        static float[] scratchBufferFloat = new float[16];
        static GameObject debugLineGo;
        public static void Initialize()
        {
            nativeVisemeData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ovrAvatarVisemes)));
            nativeGazeTargetsData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ovrAvatarGazeTargets)));
            nativeAvatarLightsData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ovrAvatarLights)));
            DebugLineCountData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(uint)));

            debugLineGo = new GameObject();
            debugLineGo.name = "AvatarSDKDebugDrawHelper";
        }

        public static void Shutdown()
        {
            Marshal.FreeHGlobal(nativeVisemeData);
            Marshal.FreeHGlobal(nativeGazeTargetsData);
            Marshal.FreeHGlobal(nativeAvatarLightsData);
            Marshal.FreeHGlobal(DebugLineCountData);

            debugLineGo = null;
        }


        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_Shutdown();

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ovrAvatarMessage_Pop();

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarMessageType ovrAvatarMessage_GetType(IntPtr msg);

        public static ovrAvatarMessage_AvatarSpecification ovrAvatarMessage_GetAvatarSpecification(
            IntPtr msg)
        {
            IntPtr ptr = ovrAvatarMessage_GetAvatarSpecification_Native(msg);
            return (ovrAvatarMessage_AvatarSpecification)Marshal.PtrToStructure(
                ptr, typeof(ovrAvatarMessage_AvatarSpecification));
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint =
            "ovrAvatarMessage_GetAvatarSpecification")]
        private static extern IntPtr ovrAvatarMessage_GetAvatarSpecification_Native(IntPtr msg);

        public static ovrAvatarMessage_AssetLoaded ovrAvatarMessage_GetAssetLoaded(
            IntPtr msg)
        {
            IntPtr ptr = ovrAvatarMessage_GetAssetLoaded_Native(msg);
            return (ovrAvatarMessage_AssetLoaded)Marshal.PtrToStructure(
                ptr, typeof(ovrAvatarMessage_AssetLoaded));
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint =
            "ovrAvatarMessage_GetAssetLoaded")]
        private static extern IntPtr ovrAvatarMessage_GetAssetLoaded_Native(IntPtr msg);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatarMessage_Free(IntPtr msg);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ovrAvatarSpecificationRequest_Create(UInt64 userID);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatarSpecificationRequest_Destroy(IntPtr specificationRequest);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatarSpecificationRequest_SetCombineMeshes(IntPtr specificationRequest, bool useCombinedMesh);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatarSpecificationRequest_SetLookAndFeelVersion(IntPtr specificationRequest, ovrAvatarLookAndFeelVersion version);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatarSpecificationRequest_SetLevelOfDetail(IntPtr specificationRequest, ovrAvatarAssetLevelOfDetail lod);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_RequestAvatarSpecification(UInt64 userID);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_RequestAvatarSpecificationFromSpecRequest(IntPtr specificationRequest);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatarSpecificationRequest_SetFallbackLookAndFeelVersion(IntPtr specificationRequest, ovrAvatarLookAndFeelVersion version);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatarSpecificationRequest_SetExpressiveFlag(IntPtr specificationRequest, bool enable);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ovrAvatar_Create(IntPtr avatarSpecification,
            ovrAvatarCapabilities capabilities);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_Destroy(IntPtr avatar);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatarPose_UpdateBody(
            IntPtr avatar, ovrAvatarTransform headPose);

        public static void ovrAvatarPose_UpdateVoiceVisualization(
            IntPtr avatar, float[] pcmData)
        {
            ovrAvatarPose_UpdateVoiceVisualization_Native(
                avatar, (UInt32)pcmData.Length, pcmData);
        }
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint =
            "ovrAvatarPose_UpdateVoiceVisualization")]
        private static extern void ovrAvatarPose_UpdateVoiceVisualization_Native(
            IntPtr avatar, UInt32 pcmDataSize, [In] float[] pcmData);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatarPose_UpdateHands(
            IntPtr avatar,
            ovrAvatarHandInputState inputStateLeft,
            ovrAvatarHandInputState inputStateRight);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatarPose_UpdateHandsWithType(
            IntPtr avatar,
            ovrAvatarHandInputState inputStateLeft,
            ovrAvatarHandInputState inputStateRight,
            ovrAvatarControllerType type);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatarPose_Finalize(IntPtr avatar, float elapsedSeconds);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_SetLeftControllerVisibility(IntPtr avatar, bool show);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_SetRightControllerVisibility(IntPtr avatar, bool show);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_SetLeftHandVisibility(IntPtr avatar, bool show);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_SetRightHandVisibility(IntPtr avatar, bool show);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 ovrAvatarComponent_Count(IntPtr avatar);

        public static void ovrAvatarComponent_Get(IntPtr avatar, UInt32 index, bool includeName, ref ovrAvatarComponent component)
        {
            IntPtr ptr = ovrAvatarComponent_Get_Native(avatar, index);
            ovrAvatarComponent_Get(ptr, includeName, ref component);
        }

        public static void ovrAvatarComponent_Get(IntPtr componentPtr, bool includeName, ref ovrAvatarComponent component)
        {
            Marshal.Copy(new IntPtr(componentPtr.ToInt64() + ovrAvatarComponent_Offsets.transform), scratchBufferFloat, 0, 10);
            OvrAvatar.ConvertTransform(scratchBufferFloat, ref component.transform);

            component.renderPartCount = (UInt32)Marshal.ReadInt32(componentPtr, ovrAvatarComponent_Offsets.renderPartCount);
            component.renderParts = Marshal.ReadIntPtr(componentPtr, ovrAvatarComponent_Offsets.renderParts);

            if (includeName)
            {
                IntPtr namePtr = Marshal.ReadIntPtr(componentPtr, ovrAvatarComponent_Offsets.name);
                component.name = Marshal.PtrToStringAnsi(namePtr);
            }
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint =
            "ovrAvatarComponent_Get")]
        public static extern IntPtr ovrAvatarComponent_Get_Native(IntPtr avatar, UInt32 index);

        public static bool ovrAvatarPose_GetBaseComponent(IntPtr avatar, ref ovrAvatarBaseComponent component)
        {
            IntPtr ptr = ovrAvatarPose_GetBaseComponent_Native(avatar);
            if (ptr == IntPtr.Zero)
            {
                return false;
            }

            int renderComponentOffset = Marshal.SizeOf(typeof(ovrAvatarBaseComponent)) - Marshal.SizeOf(typeof(IntPtr));
            component.renderComponent = Marshal.ReadIntPtr(ptr, renderComponentOffset);
            return true;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint ="ovrAvatarPose_GetBaseComponent")]
        private static extern IntPtr ovrAvatarPose_GetBaseComponent_Native(IntPtr avatar);

        public static IntPtr MarshalRenderComponent<T>(IntPtr ptr) where T : struct
        {
            return Marshal.ReadIntPtr(new IntPtr(ptr.ToInt64() + Marshal.OffsetOf(typeof(T), "renderComponent").ToInt64()));
        }
        public static bool ovrAvatarPose_GetBodyComponent(IntPtr avatar, ref ovrAvatarBodyComponent component)
        {
            IntPtr ptr = ovrAvatarPose_GetBodyComponent_Native(avatar);

            if (ptr == IntPtr.Zero)
            {
                return false;
            }

            Marshal.Copy(new IntPtr(ptr.ToInt64() + ovrAvatarBodyComponent_Offsets.leftEyeTransform), scratchBufferFloat, 0, 10);
            OvrAvatar.ConvertTransform(scratchBufferFloat, ref component.leftEyeTransform);

            Marshal.Copy(new IntPtr(ptr.ToInt64() + ovrAvatarBodyComponent_Offsets.rightEyeTransform), scratchBufferFloat, 0, 10);
            OvrAvatar.ConvertTransform(scratchBufferFloat, ref component.rightEyeTransform);

            Marshal.Copy(new IntPtr(ptr.ToInt64() + ovrAvatarBodyComponent_Offsets.centerEyeTransform), scratchBufferFloat, 0, 10);
            OvrAvatar.ConvertTransform(scratchBufferFloat, ref component.centerEyeTransform);

            component.renderComponent = MarshalRenderComponent<ovrAvatarBodyComponent>(ptr);
            return true;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint ="ovrAvatarPose_GetBodyComponent")]
        private static extern IntPtr ovrAvatarPose_GetBodyComponent_Native(IntPtr avatar);

        public static bool ovrAvatarPose_GetLeftControllerComponent(IntPtr avatar, ref ovrAvatarControllerComponent component)
        {
            IntPtr ptr = ovrAvatarPose_GetLeftControllerComponent_Native(avatar);
            if (ptr == IntPtr.Zero)
            {
                return false;
            }

            int renderComponentOffset = Marshal.SizeOf(typeof(ovrAvatarControllerComponent)) - Marshal.SizeOf(typeof(IntPtr));
            component.renderComponent = Marshal.ReadIntPtr(ptr, renderComponentOffset);
            return true;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint =
            "ovrAvatarPose_GetLeftControllerComponent")]
        private static extern IntPtr ovrAvatarPose_GetLeftControllerComponent_Native(IntPtr avatar);

        public static bool ovrAvatarPose_GetRightControllerComponent(IntPtr avatar, ref ovrAvatarControllerComponent component)
        {
            IntPtr ptr = ovrAvatarPose_GetRightControllerComponent_Native(avatar);

            if (ptr == IntPtr.Zero)
            {
                return false;
            }

            int renderComponentOffset = Marshal.SizeOf(typeof(ovrAvatarControllerComponent)) - Marshal.SizeOf(typeof(IntPtr));
            component.renderComponent = Marshal.ReadIntPtr(ptr, renderComponentOffset);
            return true;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint =
            "ovrAvatarPose_GetRightControllerComponent")]
        private static extern IntPtr ovrAvatarPose_GetRightControllerComponent_Native(IntPtr avatar);

        public static bool ovrAvatarPose_GetLeftHandComponent(IntPtr avatar, ref ovrAvatarHandComponent component)
        {
            IntPtr ptr = ovrAvatarPose_GetLeftHandComponent_Native(avatar);
            if (ptr == IntPtr.Zero)
            {
                return false;
            }

            int renderComponentOffset = Marshal.SizeOf(typeof(ovrAvatarHandComponent)) - Marshal.SizeOf(typeof(IntPtr));
            component.renderComponent = Marshal.ReadIntPtr(ptr, renderComponentOffset);
            return true;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint =
            "ovrAvatarPose_GetLeftHandComponent")]
        private static extern IntPtr ovrAvatarPose_GetLeftHandComponent_Native(IntPtr avatar);

        public static bool ovrAvatarPose_GetRightHandComponent(IntPtr avatar, ref ovrAvatarHandComponent component)
        {
            IntPtr ptr = ovrAvatarPose_GetRightHandComponent_Native(avatar);
            if (ptr == IntPtr.Zero)
            {
                return false;
            }

            int renderComponentOffset = Marshal.SizeOf(typeof(ovrAvatarHandComponent)) - Marshal.SizeOf(typeof(IntPtr));
            component.renderComponent = Marshal.ReadIntPtr(ptr, renderComponentOffset);
            return true;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint =
            "ovrAvatarPose_GetRightHandComponent")]
        private static extern IntPtr ovrAvatarPose_GetRightHandComponent_Native(IntPtr avatar);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatarAsset_BeginLoading(UInt64 assetID);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ovrAvatarAsset_BeginLoadingLOD(UInt64 assetId, ovrAvatarAssetLevelOfDetail lod);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarAssetType ovrAvatarAsset_GetType(IntPtr assetHandle);

        public static ovrAvatarMeshAssetData ovrAvatarAsset_GetMeshData(
            IntPtr assetPtr)
        {
            IntPtr ptr = ovrAvatarAsset_GetMeshData_Native(assetPtr);
            return (ovrAvatarMeshAssetData)Marshal.PtrToStructure(
                ptr, typeof(ovrAvatarMeshAssetData));
        }

        public static ovrAvatarMeshAssetDataV2 ovrAvatarAsset_GetCombinedMeshData(
            IntPtr assetPtr)
        {
            IntPtr ptr = ovrAvatarAsset_GetCombinedMeshData_Native(assetPtr);
            return (ovrAvatarMeshAssetDataV2)Marshal.PtrToStructure(
                ptr, typeof(ovrAvatarMeshAssetDataV2));
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatarAsset_GetCombinedMeshData")]
        private static extern IntPtr ovrAvatarAsset_GetCombinedMeshData_Native(IntPtr assetPtr);


        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatarAsset_GetMeshData")]
        private static extern IntPtr ovrAvatarAsset_GetMeshData_Native(IntPtr assetPtr);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 ovrAvatarAsset_GetMeshBlendShapeCount(IntPtr assetPtr);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ovrAvatarAsset_GetMeshBlendShapeName(IntPtr assetPtr, UInt32 index);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 ovrAvatarAsset_GetSubmeshCount(IntPtr assetPtr);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 ovrAvatarAsset_GetSubmeshLastIndex(IntPtr assetPtr, UInt32 index);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ovrAvatarAsset_GetMeshBlendShapeVertices(IntPtr assetPtr);


        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ovrAvatarAsset_GetAvatar(IntPtr assetHandle);

        public static UInt64[] ovrAvatarAsset_GetCombinedMeshIDs(IntPtr assetHandle)
        {
            UInt32 count = 0;
            System.IntPtr countPtr = Marshal.AllocHGlobal(Marshal.SizeOf(count));
            IntPtr idBuffer = ovrAvatarAsset_GetCombinedMeshIDs_Native(assetHandle, countPtr);
            count = (UInt32)Marshal.PtrToStructure(countPtr, typeof(UInt32));
            UInt64[] meshIDs = new UInt64[count];

            for (int i = 0; i < count; i++)
            {
                meshIDs[i] = (UInt64)Marshal.ReadInt64(idBuffer, i * Marshal.SizeOf(typeof(UInt64)));
            }

            Marshal.FreeHGlobal(countPtr);

            return meshIDs;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatarAsset_GetCombinedMeshIDs")]
        public static extern IntPtr ovrAvatarAsset_GetCombinedMeshIDs_Native(IntPtr assetHandle, IntPtr count);

        public static void ovrAvatar_GetCombinedMeshAlphaData(IntPtr avatar, ref UInt64 textureID, ref Vector4 offset)
        {
            System.IntPtr textureIDPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UInt64)));
            System.IntPtr offsetPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Vector4)));

            ovrAvatar_GetCombinedMeshAlphaData_Native(avatar, textureIDPtr, offsetPtr);

            textureID = (UInt64)Marshal.PtrToStructure(textureIDPtr, typeof(UInt64));
            offset = (Vector4)Marshal.PtrToStructure(offsetPtr, typeof(Vector4));

            Marshal.FreeHGlobal(textureIDPtr);
            Marshal.FreeHGlobal(offsetPtr);
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar_GetCombinedMeshAlphaData")]
        public static extern IntPtr ovrAvatar_GetCombinedMeshAlphaData_Native(IntPtr avatar, IntPtr textureIDPtr, IntPtr offsetPtr);

        public static ovrAvatarTextureAssetData ovrAvatarAsset_GetTextureData(
            IntPtr assetPtr)
        {
            IntPtr ptr = ovrAvatarAsset_GetTextureData_Native(assetPtr);
            return (ovrAvatarTextureAssetData)Marshal.PtrToStructure(
                ptr, typeof(ovrAvatarTextureAssetData));
        }
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint =
            "ovrAvatarAsset_GetTextureData")]
        private static extern IntPtr ovrAvatarAsset_GetTextureData_Native(IntPtr assetPtr);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint =
            "ovrAvatarAsset_GetMaterialData")]
        private static extern IntPtr ovrAvatarAsset_GetMaterialData_Native(IntPtr assetPtr);
        public static ovrAvatarMaterialState ovrAvatarAsset_GetMaterialState(IntPtr assetPtr)
        {
            IntPtr ptr = ovrAvatarAsset_GetMaterialData_Native(assetPtr);
            return (ovrAvatarMaterialState)Marshal.PtrToStructure(ptr, typeof(ovrAvatarMaterialState));
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarRenderPartType ovrAvatarRenderPart_GetType(IntPtr renderPart);

        public static ovrAvatarRenderPart_SkinnedMeshRender ovrAvatarRenderPart_GetSkinnedMeshRender(IntPtr renderPart)
        {
            IntPtr ptr = ovrAvatarRenderPart_GetSkinnedMeshRender_Native(renderPart);
            return (ovrAvatarRenderPart_SkinnedMeshRender)Marshal.PtrToStructure(
                ptr, typeof(ovrAvatarRenderPart_SkinnedMeshRender));
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatarRenderPart_GetSkinnedMeshRender")]
        private static extern IntPtr ovrAvatarRenderPart_GetSkinnedMeshRender_Native(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarTransform ovrAvatarSkinnedMeshRender_GetTransform(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarTransform ovrAvatarSkinnedMeshRenderPBS_GetTransform(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarTransform ovrAvatarSkinnedMeshRenderPBSV2_GetTransform(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarVisibilityFlags ovrAvatarSkinnedMeshRender_GetVisibilityMask(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ovrAvatarSkinnedMeshRender_MaterialStateChanged(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ovrAvatarSkinnedMeshRenderPBSV2_MaterialStateChanged(IntPtr renderPart);


        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarVisibilityFlags ovrAvatarSkinnedMeshRenderPBS_GetVisibilityMask(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarVisibilityFlags ovrAvatarSkinnedMeshRenderPBSV2_GetVisibilityMask(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarMaterialState ovrAvatarSkinnedMeshRender_GetMaterialState(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarPBSMaterialState ovrAvatarSkinnedMeshRenderPBSV2_GetPBSMaterialState(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarExpressiveParameters ovrAvatar_GetExpressiveParameters(IntPtr avatar);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt64 ovrAvatarSkinnedMeshRender_GetDirtyJoints(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt64 ovrAvatarSkinnedMeshRenderPBS_GetDirtyJoints(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt64 ovrAvatarSkinnedMeshRenderPBSV2_GetDirtyJoints(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarTransform ovrAvatarSkinnedMeshRender_GetJointTransform(IntPtr renderPart, UInt32 jointIndex);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_SetActionUnitOnsetSpeed(IntPtr avatar, float onsetSpeed);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_SetActionUnitFalloffSpeed(IntPtr avatar, float falloffSpeed);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_SetVisemeMultiplier(IntPtr avatar, float visemeMultiplier);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarTransform ovrAvatarSkinnedMeshRenderPBS_GetJointTransform(IntPtr renderPart, UInt32 jointIndex);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatarTransform ovrAvatarSkinnedMeshRenderPBSV2_GetJointTransform(IntPtr renderPart, UInt32 jointIndex);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt64 ovrAvatarSkinnedMeshRenderPBS_GetAlbedoTextureAssetID(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt64 ovrAvatarSkinnedMeshRenderPBS_GetSurfaceTextureAssetID(IntPtr renderPart);

        public static ovrAvatarRenderPart_SkinnedMeshRenderPBS ovrAvatarRenderPart_GetSkinnedMeshRenderPBS(IntPtr renderPart)
        {
            IntPtr ptr = ovrAvatarRenderPart_GetSkinnedMeshRenderPBS_Native(renderPart);
            return (ovrAvatarRenderPart_SkinnedMeshRenderPBS)Marshal.PtrToStructure(
                ptr, typeof(ovrAvatarRenderPart_SkinnedMeshRenderPBS));
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatarRenderPart_GetSkinnedMeshRenderPBS")]
        private static extern IntPtr ovrAvatarRenderPart_GetSkinnedMeshRenderPBS_Native(IntPtr renderPart);

        public static ovrAvatarRenderPart_SkinnedMeshRenderPBS_V2 ovrAvatarRenderPart_GetSkinnedMeshRenderPBSV2(IntPtr renderPart)
        {
            IntPtr ptr = ovrAvatarRenderPart_GetSkinnedMeshRenderPBSV2_Native(renderPart);
            return (ovrAvatarRenderPart_SkinnedMeshRenderPBS_V2)Marshal.PtrToStructure(
                ptr, typeof(ovrAvatarRenderPart_SkinnedMeshRenderPBS_V2));
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatarRenderPart_GetSkinnedMeshRenderPBSV2")]
        private static extern IntPtr ovrAvatarRenderPart_GetSkinnedMeshRenderPBSV2_Native(IntPtr renderPart);

        public static void ovrAvatarSkinnedMeshRender_GetBlendShapeParams(IntPtr renderPart, ref ovrAvatarBlendShapeParams blendParams)
        {
            IntPtr ptr = ovrAvatarSkinnedMeshRender_GetBlendShapeParams_Native(renderPart);
            blendParams.blendShapeParamCount = (UInt32)Marshal.ReadInt32(ptr);
            Marshal.Copy(new IntPtr(ptr.ToInt64() + ovrAvatarBlendShapeParams_Offsets.blendShapeParams), blendParams.blendShapeParams, 0, (int)blendParams.blendShapeParamCount);
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatarSkinnedMeshRender_GetBlendShapeParams")]
        private static extern IntPtr ovrAvatarSkinnedMeshRender_GetBlendShapeParams_Native(IntPtr renderPart);

        public static ovrAvatarRenderPart_ProjectorRender ovrAvatarRenderPart_GetProjectorRender(IntPtr renderPart)
        {
            IntPtr ptr = ovrAvatarRenderPart_GetProjectorRender_Native(renderPart);
            return (ovrAvatarRenderPart_ProjectorRender)Marshal.PtrToStructure(
                ptr, typeof(ovrAvatarRenderPart_ProjectorRender));
        }

        public static ovrAvatarPBSMaterialState[] ovrAvatar_GetBodyPBSMaterialStates(IntPtr renderPart)
        {
            System.IntPtr countPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UInt32)));
            IntPtr ptrState = ovrAvatar_GetBodyPBSMaterialStates_Native(renderPart, countPtr);
            UInt32 count = (UInt32)Marshal.ReadInt32(countPtr);

            ovrAvatarPBSMaterialState[] states = new ovrAvatarPBSMaterialState[count];

            for (int i = 0; i < states.Length; i++)
            {
                IntPtr nextItem = new IntPtr(ptrState.ToInt64() + i * Marshal.SizeOf(typeof(ovrAvatarPBSMaterialState)));
                states[i] = (ovrAvatarPBSMaterialState)Marshal.PtrToStructure(nextItem, typeof(ovrAvatarPBSMaterialState));
            }

            Marshal.FreeHGlobal(countPtr);

            return states;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar_GetBodyPBSMaterialStates")]
        private static extern IntPtr ovrAvatar_GetBodyPBSMaterialStates_Native(IntPtr avatar, IntPtr count);


        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatarRenderPart_GetProjectorRender")]
        private static extern IntPtr ovrAvatarRenderPart_GetProjectorRender_Native(IntPtr renderPart);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 ovrAvatar_GetReferencedAssetCount(IntPtr avatar);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt64 ovrAvatar_GetReferencedAsset(IntPtr avatar, UInt32 index);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_SetLeftHandGesture(IntPtr avatar, ovrAvatarHandGesture gesture);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_SetRightHandGesture(IntPtr avatar, ovrAvatarHandGesture gesture);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_SetLeftHandCustomGesture(IntPtr avatar, UInt32 jointCount, [In] ovrAvatarTransform[] customJointTransforms);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_SetRightHandCustomGesture(IntPtr avatar, UInt32 jointCount, [In] ovrAvatarTransform[] customJointTransforms);

        //Native calls for efficient packet updates
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_UpdatePoseFromPacket(IntPtr avatar, IntPtr packet, float secondsFromStart);
        
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatarPacket_BeginRecording(IntPtr avatar);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ovrAvatarPacket_EndRecording(IntPtr avatar);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 ovrAvatarPacket_GetSize(IntPtr packet);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ovrAvatarPacket_GetDurationSeconds(IntPtr packet);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatarPacket_Free(IntPtr packet);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ovrAvatarPacket_Write(IntPtr packet, UInt32 bufferSize, [Out] byte[] buffer);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ovrAvatarPacket_Read(UInt32 bufferSize, [In] byte[] buffer);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ovrAvatar_SetInternalForceASTCTextures(bool value);

        // Renaming the outward facing method to remove Internal from name
        public static void ovrAvatar_SetForceASTCTextures(bool value)
        {
            ovrAvatar_SetInternalForceASTCTextures(value);
        }

        public static void ovrAvatar_OverrideExpressiveLogic(IntPtr avatar, ovrAvatarBlendShapeParams blendParams)
        {
            IntPtr statePtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ovrAvatarBlendShapeParams)));
            Marshal.StructureToPtr(blendParams, statePtr, false);
            ovrAvatar_OverrideExpressiveLogic_Native(avatar, statePtr);
            Marshal.FreeHGlobal(statePtr);
        }
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar_OverrideExpressiveLogic")]
        private static extern void ovrAvatar_OverrideExpressiveLogic_Native(IntPtr avatar, IntPtr state);

        public static void ovrAvatar_SetVisemes(IntPtr avatar, ovrAvatarVisemes visemes)
        {
            Marshal.WriteInt32(nativeVisemeData, (Int32)visemes.visemeParamCount);
            Marshal.Copy(visemes.visemeParams, 0, new IntPtr(nativeVisemeData.ToInt64() + ovrAvatarVisemes_Offsets.visemeParams), (int)visemes.visemeParamCount);

            ovrAvatar_SetVisemes_Native(avatar, nativeVisemeData);
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar_SetVisemes")]
        private static extern void ovrAvatar_SetVisemes_Native(IntPtr avatar, IntPtr visemes);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_UpdateWorldTransform(IntPtr avatar, ovrAvatarTransform transform);


        public static void ovrAvatar_UpdateGazeTargets(ovrAvatarGazeTargets targets)
        {
            Marshal.WriteInt32(nativeGazeTargetsData, (Int32)targets.targetCount);

            var targetOffset = ovrAvatarGazeTargets_Offsets.targets;
            for (uint index = 0; index < targets.targetCount; index++)
            {
                var baseOffset = targetOffset + index * Marshal.SizeOf(typeof(ovrAvatarGazeTarget));

                Marshal.WriteInt32(new IntPtr(nativeGazeTargetsData.ToInt64() + baseOffset + ovrAvatarGazeTarget_Offsets.id), (int)targets.targets[index].id);

                scratchBufferFloat[0] = targets.targets[index].worldPosition.x;
                scratchBufferFloat[1] = targets.targets[index].worldPosition.y;
                scratchBufferFloat[2] = targets.targets[index].worldPosition.z;
                Marshal.Copy(scratchBufferFloat, 0, new IntPtr(nativeGazeTargetsData.ToInt64() + baseOffset + ovrAvatarGazeTarget_Offsets.worldPosition), 3);

                Marshal.WriteInt32(new IntPtr(nativeGazeTargetsData.ToInt64() + baseOffset + ovrAvatarGazeTarget_Offsets.type), (int)targets.targets[index].type);
            }

            ovrAvatar_UpdateGazeTargets_Native(nativeGazeTargetsData);
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar_UpdateGazeTargets")]
        private static extern void ovrAvatar_UpdateGazeTargets_Native(IntPtr targets);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_RemoveGazeTargets(UInt32 targetCount, UInt32[] ids);

        public static void ovrAvatar_UpdateLights(ovrAvatarLights lights)
        {
            scratchBufferFloat[0] = lights.ambientIntensity;
            Marshal.Copy(scratchBufferFloat, 0, nativeAvatarLightsData, 1);

            Marshal.WriteInt32(new IntPtr(nativeAvatarLightsData.ToInt64() + Marshal.OffsetOf(typeof(ovrAvatarLights), "lightCount").ToInt64()), (int)lights.lightCount);

            var lightsOffset = Marshal.OffsetOf(typeof(ovrAvatarLights), "lights").ToInt64();
            for (uint index = 0; index < lights.lightCount; index++)
            {
                var baseOffset = lightsOffset + index * Marshal.SizeOf(typeof(ovrAvatarLight));

                Marshal.WriteInt32(new IntPtr(nativeAvatarLightsData.ToInt64() + baseOffset + Marshal.OffsetOf(typeof(ovrAvatarLight), "id").ToInt64()), (int)lights.lights[index].id);
                Marshal.WriteInt32(new IntPtr(nativeAvatarLightsData.ToInt64() + baseOffset + Marshal.OffsetOf(typeof(ovrAvatarLight), "type").ToInt64()), (int)lights.lights[index].type);

                scratchBufferFloat[0] = lights.lights[index].intensity;
                Marshal.Copy(scratchBufferFloat, 0, new IntPtr(nativeAvatarLightsData.ToInt64() + baseOffset + Marshal.OffsetOf(typeof(ovrAvatarLight), "intensity").ToInt64()), 1);

                scratchBufferFloat[0] = lights.lights[index].worldDirection.x;
                scratchBufferFloat[1] = lights.lights[index].worldDirection.y;
                scratchBufferFloat[2] = lights.lights[index].worldDirection.z;
                Marshal.Copy(scratchBufferFloat, 0, new IntPtr(nativeAvatarLightsData.ToInt64() + baseOffset + Marshal.OffsetOf(typeof(ovrAvatarLight), "worldDirection").ToInt64()), 3);

                scratchBufferFloat[0] = lights.lights[index].worldPosition.x;
                scratchBufferFloat[1] = lights.lights[index].worldPosition.y;
                scratchBufferFloat[2] = lights.lights[index].worldPosition.z;
                Marshal.Copy(scratchBufferFloat, 0, new IntPtr(nativeAvatarLightsData.ToInt64() + baseOffset + Marshal.OffsetOf(typeof(ovrAvatarLight), "worldPosition").ToInt64()), 3);

                scratchBufferFloat[0] = lights.lights[index].range;
                Marshal.Copy(scratchBufferFloat, 0, new IntPtr(nativeAvatarLightsData.ToInt64() + baseOffset + Marshal.OffsetOf(typeof(ovrAvatarLight), "range").ToInt64()), 1);

                scratchBufferFloat[0] = lights.lights[index].spotAngleDeg;
                Marshal.Copy(scratchBufferFloat, 0, new IntPtr(nativeAvatarLightsData.ToInt64() + baseOffset + Marshal.OffsetOf(typeof(ovrAvatarLight), "spotAngleDeg").ToInt64()), 1);
            }

            ovrAvatar_UpdateLights_Native(nativeAvatarLightsData);
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar_UpdateLights")]
        private static extern void ovrAvatar_UpdateLights_Native(IntPtr lights);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_RemoveLights(UInt32 lightCount, UInt32[] ids);

        private static string SDKRuntimePrefix = "[RUNTIME] - ";
        public delegate void LoggingDelegate(IntPtr str);

        [MonoPInvokeCallback(typeof(LoggingDelegate))]
        public static void LoggingCallback(IntPtr str)
        {
            string csharpStr = Marshal.PtrToStringAnsi(str);
            AvatarLogger.Log(SDKRuntimePrefix + csharpStr);
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_RegisterLoggingCallback(LoggingDelegate callback);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_SetLoggingLevel(ovrAvatarLogLevel level);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar_GetDebugTransforms")]
        public static extern IntPtr ovrAvatar_GetDebugTransforms_Native(IntPtr count);

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar_GetDebugLines")]
        public static extern IntPtr ovrAvatar_GetDebugLines_Native(IntPtr count);

        public static void ovrAvatar_DrawDebugLines()
        {
            IntPtr debugLinePtr = ovrAvatar_GetDebugLines_Native(DebugLineCountData);
            int lineCount = Marshal.ReadInt32(DebugLineCountData);

            ovrAvatarDebugLine tempLine = new ovrAvatarDebugLine();
            for (int i = 0; i < lineCount; i++)
            {
                var offset = i * Marshal.SizeOf(typeof(ovrAvatarDebugLine));
                Marshal.Copy(new IntPtr(debugLinePtr.ToInt64() + offset), scratchBufferFloat, 0, 9);
                tempLine.startPoint.x = scratchBufferFloat[0];
                tempLine.startPoint.y = scratchBufferFloat[1];
                tempLine.startPoint.z = -scratchBufferFloat[2];

                tempLine.endPoint.x = scratchBufferFloat[3];
                tempLine.endPoint.y = scratchBufferFloat[4];
                tempLine.endPoint.z = -scratchBufferFloat[5];

                tempLine.color.x = scratchBufferFloat[6];
                tempLine.color.y = scratchBufferFloat[7];
                tempLine.color.z = scratchBufferFloat[8];

                tempLine.context = (ovrAvatarDebugContext)Marshal.ReadInt32(new IntPtr(debugLinePtr.ToInt64() + offset + Marshal.OffsetOf(typeof(ovrAvatarDebugLine), "context").ToInt64()));
                tempLine.text = Marshal.ReadIntPtr(new IntPtr(debugLinePtr.ToInt64() + offset + Marshal.OffsetOf(typeof(ovrAvatarDebugLine), "text").ToInt64()));

                Debug.DrawLine(tempLine.startPoint, tempLine.endPoint, new Color(tempLine.color.x, tempLine.color.y, tempLine.color.z));

                // TODO: Decide what to do with the text. Can only debug render in OnGUI()
                //if (tempLine.text != IntPtr.Zero)
                //{
                //    string text = Marshal.PtrToStringAnsi(tempLine.text);
                //    AvatarLogger.Log(text);
                //}
            }

            debugLinePtr = ovrAvatar_GetDebugTransforms_Native(DebugLineCountData);
            lineCount = Marshal.ReadInt32(DebugLineCountData);

            ovrAvatarDebugTransform tempTrans = new ovrAvatarDebugTransform();
            for (int i = 0; i < lineCount; i++)
            {
                var offset = i * Marshal.SizeOf(typeof(ovrAvatarDebugTransform));
                Marshal.Copy(new IntPtr(debugLinePtr.ToInt64() + offset), scratchBufferFloat, 0, 10);

                OvrAvatar.ConvertTransform(scratchBufferFloat, ref tempTrans.transform);
                OvrAvatar.ConvertTransform(tempTrans.transform, debugLineGo.transform);

                tempTrans.context = (ovrAvatarDebugContext)Marshal.ReadInt32(new IntPtr(debugLinePtr.ToInt64() + offset + Marshal.OffsetOf(typeof(ovrAvatarDebugTransform), "context").ToInt64()));
                tempTrans.text = Marshal.ReadIntPtr(new IntPtr(debugLinePtr.ToInt64() + offset + Marshal.OffsetOf(typeof(ovrAvatarDebugTransform), "text").ToInt64()));

                const float SCALE_FACTOR = 0.1f;
                Vector3 transUp = SCALE_FACTOR * debugLineGo.transform.TransformVector(Vector3.up);
                Vector3 transRight = SCALE_FACTOR * debugLineGo.transform.TransformVector(Vector3.right);
                Vector3 transFwd = SCALE_FACTOR * debugLineGo.transform.TransformVector(Vector3.forward);

                Debug.DrawLine(debugLineGo.transform.position, debugLineGo.transform.position + transUp, Color.green);
                Debug.DrawLine(debugLineGo.transform.position, debugLineGo.transform.position + transRight, Color.red);
                Debug.DrawLine(debugLineGo.transform.position, debugLineGo.transform.position + transFwd, Color.blue);

                // TODO: Decide what to do with the text. Can only debug render in OnGUI()
                //if (tempTrans.text != IntPtr.Zero)
                //{
                //    string text = Marshal.PtrToStringAnsi(tempTrans.text);
                //    AvatarLogger.Log(text);
                //}
            }
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar_SetDebugDrawContext(UInt32 context);

        //OvrPlugin Hooks
        private const string ovrPluginDLL = "OVRPlugin";
        private static System.Version ovrPluginVersion;

        public enum Result
        {
          /// Success
          Success = 0,

          /// Failure
          Failure = -1000,
          Failure_InvalidParameter = -1001,
          Failure_NotInitialized = -1002,
          Failure_InvalidOperation = -1003,
          Failure_Unsupported = -1004,
          Failure_NotYetImplemented = -1005,
          Failure_OperationFailed = -1006,
          Failure_InsufficientSize = -1007,
        }

        public static bool SendEvent(string name, string param = "", string source = "")
        {
          try
          {
            if (ovrPluginVersion == null)
            {
              string version = ovrp_GetVersion();
              if (!String.IsNullOrEmpty(version))
              {
                ovrPluginVersion = new System.Version(version.Split('-')[0]);
              }
              else
              {
                ovrPluginVersion = new System.Version(0, 0, 0);
              }
            }
            if (ovrPluginVersion >= OVRP_1_30_0.version)
            {
              return OVRP_1_30_0.ovrp_SendEvent2(name, param, source.Length == 0 ? "avatar_sdk" : source) == Result.Success;
            }
            else
            {
              return false;
            }
          }
          catch (Exception)
          {
            return false;
          }
        }

        [DllImport(ovrPluginDLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetVersion")]
        private static extern IntPtr _ovrp_GetVersion();
        public static string ovrp_GetVersion() { return Marshal.PtrToStringAnsi(_ovrp_GetVersion()); }

        private static class OVRP_1_30_0
        {
          public static readonly System.Version version = new System.Version(1, 30, 0);
          [DllImport(ovrPluginDLL, CallingConvention = CallingConvention.Cdecl)]
          public static extern Result ovrp_SendEvent2(string name, string param, string source);
        }
  }
}
