using UnityEngine;
using System.Collections;
using System;
using Oculus.Avatar;
using System.Runtime.InteropServices;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if AVATAR_INTERNAL
using UnityEngine.Events;
#endif

[System.Serializable]
public class AvatarLayer
{
    public int layerIndex;
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(AvatarLayer))]
public class AvatarLayerPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, GUIContent.none, property);
        SerializedProperty layerIndex = property.FindPropertyRelative("layerIndex");
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        layerIndex.intValue = EditorGUI.LayerField(position, layerIndex.intValue);
        EditorGUI.EndProperty();
    }
}
#endif

[System.Serializable]
public class PacketRecordSettings
{
    internal bool RecordingFrames = false;
    public float UpdateRate = 1f / 30f; // 30 hz update of packets
    internal float AccumulatedTime;
};

public class OvrAvatar : MonoBehaviour
{
    [Header("Avatar")]
    public IntPtr sdkAvatar = IntPtr.Zero;
    public string oculusUserID;
    public OvrAvatarDriver Driver;

    [Header("Capabilities")]
    public bool EnableBody = true;
    public bool EnableHands = true;
    public bool EnableBase = true;
    public bool EnableExpressive = false;

    [Header("Network")]
    public bool RecordPackets;
    public bool UseSDKPackets = true;
    public PacketRecordSettings PacketSettings = new PacketRecordSettings();

    [Header("Visibility")]
    public bool StartWithControllers;
    public AvatarLayer FirstPersonLayer;
    public AvatarLayer ThirdPersonLayer;
    public bool ShowFirstPerson = true;
    public bool ShowThirdPerson;
    internal ovrAvatarCapabilities Capabilities = ovrAvatarCapabilities.Body;

    [Header("Performance")]
#if UNITY_ANDROID
    [Tooltip(
        "LOD mesh complexity and texture resolution. Highest LOD recommended on PC and simple mobile apps." +
        " Medium LOD recommended on mobile devices or for background characters on PC." +
        " Lowest LOD recommended for background characters on mobile.")]
    [SerializeField]
    internal ovrAvatarAssetLevelOfDetail LevelOfDetail = ovrAvatarAssetLevelOfDetail.Medium;
#else
    [SerializeField]
    internal ovrAvatarAssetLevelOfDetail LevelOfDetail = ovrAvatarAssetLevelOfDetail.Highest;
#endif
#if UNITY_ANDROID && UNITY_5_5_OR_NEWER && !UNITY_EDITOR
    [Tooltip(
        "Enable to use combined meshes to reduce draw calls. Currently only available on mobile devices. " +
        "Will be forced to false on PC.")]
    private bool CombineMeshes = true;
#else
    private bool CombineMeshes = false;
#endif
    [Tooltip(
        "Enable to use transparent queue, disable to use geometry queue. Requires restart to take effect.")]
    public bool UseTransparentRenderQueue = true;

    [Header("Shaders")]
    public Shader Monochrome_SurfaceShader;
    public Shader Monochrome_SurfaceShader_SelfOccluding;
    public Shader Monochrome_SurfaceShader_PBS;
    public Shader Skinshaded_SurfaceShader_SingleComponent;
    public Shader Skinshaded_VertFrag_SingleComponent;
    public Shader Skinshaded_VertFrag_CombinedMesh;
    public Shader Skinshaded_Expressive_SurfaceShader_SingleComponent;
    public Shader Skinshaded_Expressive_VertFrag_SingleComponent;
    public Shader Skinshaded_Expressive_VertFrag_CombinedMesh;
    public Shader Loader_VertFrag_CombinedMesh;
    public Shader EyeLens;
    public Shader ControllerShader;

    [Header("Other")]
    public bool CanOwnMicrophone = true;
    [Tooltip(
        "Enable laughter detection and animation as part of OVRLipSync.")]
    public bool EnableLaughter = true;
    public GameObject MouthAnchor;
    public Transform LeftHandCustomPose;
    public Transform RightHandCustomPose;

    // Avatar asset
    private HashSet<UInt64> assetLoadingIds = new HashSet<UInt64>();
    private bool assetsFinishedLoading = false;

    // Material manager
    private OvrAvatarMaterialManager materialManager;
    private bool waitingForCombinedMesh = false;

    // Global expressive system initialization
    private static bool doneExpressiveGlobalInit = false;

    // Clothing offsets
    private Vector4 clothingAlphaOffset = new Vector4(0f, 0f, 0f, 1f);
    private UInt64 clothingAlphaTexture = 0;

    // Lipsync
    private OVRLipSyncMicInput micInput = null;
    private OVRLipSyncContext lipsyncContext = null;
    private OVRLipSync.Frame currentFrame = new OVRLipSync.Frame();
    private float[] visemes = new float[VISEME_COUNT];
    private AudioSource audioSource;
    private ONSPAudioSource spatializedSource;
    private List<float[]> voiceUpdates = new List<float[]>();
    private static ovrAvatarVisemes RuntimeVisemes;

    // Custom hand poses
    private Transform cachedLeftHandCustomPose;
    private Transform[] cachedCustomLeftHandJoints;
    private ovrAvatarTransform[] cachedLeftHandTransforms;
    private Transform cachedRightHandCustomPose;
    private Transform[] cachedCustomRightHandJoints;
    private ovrAvatarTransform[] cachedRightHandTransforms;
    private bool showLeftController;
    private bool showRightController;

    // Consts
#if UNITY_ANDROID && !UNITY_EDITOR
    private const bool USE_MOBILE_TEXTURE_FORMAT = true;
#else
    private const bool USE_MOBILE_TEXTURE_FORMAT = false;
#endif
    private static readonly Vector3 MOUTH_HEAD_OFFSET = new Vector3(0, -0.085f, 0.09f);
    private const string MOUTH_HELPER_NAME = "MouthAnchor";
    // Initial 'silence' score, 14 viseme scores and 1 laughter score as last element
    private const int VISEME_COUNT = 16;
    // Lipsync animation speeds
    private const float ACTION_UNIT_ONSET_SPEED = 30f;
    private const float ACTION_UNIT_FALLOFF_SPEED = 20f;
    private const float VISEME_LEVEL_MULTIPLIER = 1.5f;

    // Internals
    internal UInt64 oculusUserIDInternal;
    internal OvrAvatarBase Base = null;
    internal OvrAvatarTouchController ControllerLeft = null;
    internal OvrAvatarTouchController ControllerRight = null;
    internal OvrAvatarBody Body = null;
    internal OvrAvatarHand HandLeft = null;
    internal OvrAvatarHand HandRight = null;
    internal ovrAvatarLookAndFeelVersion LookAndFeelVersion = ovrAvatarLookAndFeelVersion.Two;
    internal ovrAvatarLookAndFeelVersion FallbackLookAndFeelVersion = ovrAvatarLookAndFeelVersion.Two;
#if AVATAR_INTERNAL
    public AvatarControllerBlend BlendController;
    public UnityEvent AssetsDoneLoading = new UnityEvent();
#endif

    // Avatar packets
    public class PacketEventArgs : EventArgs
    {
        public readonly OvrAvatarPacket Packet;
        public PacketEventArgs(OvrAvatarPacket packet)
        {
            Packet = packet;
        }
    }
    private OvrAvatarPacket CurrentUnityPacket;
    public EventHandler<PacketEventArgs> PacketRecorded;

    public enum HandType
    {
        Right,
        Left,

        Max
    };

    public enum HandJoint
    {
        HandBase,
        IndexBase,
        IndexTip,
        ThumbBase,
        ThumbTip,

        Max,
    }

    private static string[,] HandJoints = new string[(int)HandType.Max, (int)HandJoint.Max]
    {
        {
            "hands:r_hand_world",
            "hands:r_hand_world/hands:b_r_hand/hands:b_r_index1",
            "hands:r_hand_world/hands:b_r_hand/hands:b_r_index1/hands:b_r_index2/hands:b_r_index3/hands:b_r_index_ignore",
            "hands:r_hand_world/hands:b_r_hand/hands:b_r_thumb1/hands:b_r_thumb2",
            "hands:r_hand_world/hands:b_r_hand/hands:b_r_thumb1/hands:b_r_thumb2/hands:b_r_thumb3/hands:b_r_thumb_ignore"
        },
        {
            "hands:l_hand_world",
            "hands:l_hand_world/hands:b_l_hand/hands:b_l_index1",
            "hands:l_hand_world/hands:b_l_hand/hands:b_l_index1/hands:b_l_index2/hands:b_l_index3/hands:b_l_index_ignore",
            "hands:l_hand_world/hands:b_l_hand/hands:b_l_thumb1/hands:b_l_thumb2",
            "hands:l_hand_world/hands:b_l_hand/hands:b_l_thumb1/hands:b_l_thumb2/hands:b_l_thumb3/hands:b_l_thumb_ignore"
        }
    };

    static OvrAvatar()
    {
        // This size has to match the 'MarshalAs' attribute in the ovrAvatarVisemes declaration.
        RuntimeVisemes.visemeParams = new float[32];
        RuntimeVisemes.visemeParamCount = VISEME_COUNT;
    }

    void OnDestroy()
    {
        if (sdkAvatar != IntPtr.Zero)
        {
            CAPI.ovrAvatar_Destroy(sdkAvatar);
        }
    }

    public void AssetLoadedCallback(OvrAvatarAsset asset)
    {
        assetLoadingIds.Remove(asset.assetID);
    }

    public void CombinedMeshLoadedCallback(IntPtr assetPtr)
    {
        if (!waitingForCombinedMesh)
        {
            return;
        }

        var meshIDs = CAPI.ovrAvatarAsset_GetCombinedMeshIDs(assetPtr);
        foreach (var id in meshIDs)
        {
            assetLoadingIds.Remove(id);
        }

        CAPI.ovrAvatar_GetCombinedMeshAlphaData(sdkAvatar, ref clothingAlphaTexture, ref clothingAlphaOffset);

        waitingForCombinedMesh = false;
    }

    private OvrAvatarSkinnedMeshRenderComponent AddSkinnedMeshRenderComponent(GameObject gameObject, ovrAvatarRenderPart_SkinnedMeshRender skinnedMeshRender)
    {
        OvrAvatarSkinnedMeshRenderComponent skinnedMeshRenderer = gameObject.AddComponent<OvrAvatarSkinnedMeshRenderComponent>();
        skinnedMeshRenderer.Initialize(skinnedMeshRender, Monochrome_SurfaceShader, Monochrome_SurfaceShader_SelfOccluding, ThirdPersonLayer.layerIndex, FirstPersonLayer.layerIndex);
        return skinnedMeshRenderer;
    }

    private OvrAvatarSkinnedMeshRenderPBSComponent AddSkinnedMeshRenderPBSComponent(GameObject gameObject, ovrAvatarRenderPart_SkinnedMeshRenderPBS skinnedMeshRenderPBS)
    {
        OvrAvatarSkinnedMeshRenderPBSComponent skinnedMeshRenderer = gameObject.AddComponent<OvrAvatarSkinnedMeshRenderPBSComponent>();
        skinnedMeshRenderer.Initialize(skinnedMeshRenderPBS, Monochrome_SurfaceShader_PBS, ThirdPersonLayer.layerIndex, FirstPersonLayer.layerIndex);
        return skinnedMeshRenderer;
    }

    private OvrAvatarSkinnedMeshPBSV2RenderComponent AddSkinnedMeshRenderPBSV2Component(
        IntPtr renderPart,
        GameObject go,
        ovrAvatarRenderPart_SkinnedMeshRenderPBS_V2 skinnedMeshRenderPBSV2,
        bool isBodyPartZero,
        bool isControllerModel)
    {
        OvrAvatarSkinnedMeshPBSV2RenderComponent skinnedMeshRenderer = go.AddComponent<OvrAvatarSkinnedMeshPBSV2RenderComponent>();
        skinnedMeshRenderer.Initialize(
            renderPart,
            skinnedMeshRenderPBSV2,
            materialManager,
            ThirdPersonLayer.layerIndex,
            FirstPersonLayer.layerIndex,
            isBodyPartZero && CombineMeshes,
            LevelOfDetail,
            isBodyPartZero && EnableExpressive,
            this,
            isControllerModel);

        return skinnedMeshRenderer;
    }

    static public IntPtr GetRenderPart(ovrAvatarComponent component, UInt32 renderPartIndex)
    {
        return Marshal.ReadIntPtr(component.renderParts, Marshal.SizeOf(typeof(IntPtr)) * (int)renderPartIndex);
    }

    private static string GetRenderPartName(ovrAvatarComponent component, uint renderPartIndex)
    {
        return component.name + "_renderPart_" + (int)renderPartIndex;
    }

    internal static void ConvertTransform(float[] transform, ref ovrAvatarTransform target)
    {
        target.position.x = transform[0];
        target.position.y = transform[1];
        target.position.z = transform[2];

        target.orientation.x = transform[3];
        target.orientation.y = transform[4];
        target.orientation.z = transform[5];
        target.orientation.w = transform[6];

        target.scale.x = transform[7];
        target.scale.y = transform[8];
        target.scale.z = transform[9];
    }

    internal static void ConvertTransform(ovrAvatarTransform transform, Transform target)
    {
        Vector3 position = transform.position;
        position.z = -position.z;
        Quaternion orientation = transform.orientation;
        orientation.x = -orientation.x;
        orientation.y = -orientation.y;
        target.localPosition = position;
        target.localRotation = orientation;
        target.localScale = transform.scale;
    }

    public static ovrAvatarTransform CreateOvrAvatarTransform(Vector3 position, Quaternion orientation)
    {
        return new ovrAvatarTransform
        {
            position = new Vector3(position.x, position.y, -position.z),
            orientation = new Quaternion(-orientation.x, -orientation.y, orientation.z, orientation.w),
            scale = Vector3.one
        };
    }

    private static ovrAvatarGazeTarget CreateOvrGazeTarget(uint targetId, Vector3 targetPosition, ovrAvatarGazeTargetType targetType)
    {
        return new ovrAvatarGazeTarget
        {
            id = targetId,
            // Do coordinate system switch.
            worldPosition = new Vector3(targetPosition.x, targetPosition.y, -targetPosition.z),
            type = targetType
        };
    }

    private void BuildRenderComponents()
    {
        ovrAvatarBaseComponent baseComponnet = new ovrAvatarBaseComponent();
        ovrAvatarHandComponent leftHandComponnet = new ovrAvatarHandComponent();
        ovrAvatarHandComponent rightHandComponnet = new ovrAvatarHandComponent();
        ovrAvatarControllerComponent leftControllerComponent = new ovrAvatarControllerComponent();
        ovrAvatarControllerComponent rightControllerComponent = new ovrAvatarControllerComponent();
        ovrAvatarBodyComponent bodyComponent = new ovrAvatarBodyComponent();

        ovrAvatarComponent dummyComponent = new ovrAvatarComponent();

        const bool FetchName = true;

        if (CAPI.ovrAvatarPose_GetLeftHandComponent(sdkAvatar, ref leftHandComponnet))
        {
            CAPI.ovrAvatarComponent_Get(leftHandComponnet.renderComponent, FetchName, ref dummyComponent);
            AddAvatarComponent(ref HandLeft, dummyComponent);
            HandLeft.isLeftHand = true;
        }

        if (CAPI.ovrAvatarPose_GetRightHandComponent(sdkAvatar, ref rightHandComponnet))
        {
            CAPI.ovrAvatarComponent_Get(rightHandComponnet.renderComponent, FetchName, ref dummyComponent);
            AddAvatarComponent(ref HandRight, dummyComponent);
            HandRight.isLeftHand = false;
        }

        if (CAPI.ovrAvatarPose_GetBodyComponent(sdkAvatar, ref bodyComponent))
        {
            CAPI.ovrAvatarComponent_Get(bodyComponent.renderComponent, FetchName, ref dummyComponent);
            AddAvatarComponent(ref Body, dummyComponent);
        }

        if (CAPI.ovrAvatarPose_GetLeftControllerComponent(sdkAvatar, ref leftControllerComponent))
        {
            CAPI.ovrAvatarComponent_Get(leftControllerComponent.renderComponent, FetchName, ref dummyComponent);
            AddAvatarComponent(ref ControllerLeft, dummyComponent);
            ControllerLeft.isLeftHand = true;
        }

        if (CAPI.ovrAvatarPose_GetRightControllerComponent(sdkAvatar, ref rightControllerComponent))
        {
            CAPI.ovrAvatarComponent_Get(rightControllerComponent.renderComponent, FetchName, ref dummyComponent);
            AddAvatarComponent(ref ControllerRight, dummyComponent);
            ControllerRight.isLeftHand = false;
        }

        if (CAPI.ovrAvatarPose_GetBaseComponent(sdkAvatar, ref baseComponnet))
        {
            CAPI.ovrAvatarComponent_Get(baseComponnet.renderComponent, FetchName, ref dummyComponent);
            AddAvatarComponent(ref Base, dummyComponent);
        }
    }

    private void AddAvatarComponent<T>(ref T root, ovrAvatarComponent nativeComponent) where T : OvrAvatarComponent
    {
        GameObject componentObject = new GameObject();
        componentObject.name = nativeComponent.name;
        componentObject.transform.SetParent(transform);
        root = componentObject.AddComponent<T>();
        root.SetOvrAvatarOwner(this);
        AddRenderParts(root, nativeComponent, componentObject.transform);
    }

    void UpdateCustomPoses()
    {
        // Check to see if the pose roots changed
        if (UpdatePoseRoot(LeftHandCustomPose, ref cachedLeftHandCustomPose, ref cachedCustomLeftHandJoints, ref cachedLeftHandTransforms))
        {
            if (cachedLeftHandCustomPose == null && sdkAvatar != IntPtr.Zero)
            {
                CAPI.ovrAvatar_SetLeftHandGesture(sdkAvatar, ovrAvatarHandGesture.Default);
            }
        }
        if (UpdatePoseRoot(RightHandCustomPose, ref cachedRightHandCustomPose, ref cachedCustomRightHandJoints, ref cachedRightHandTransforms))
        {
            if (cachedRightHandCustomPose == null && sdkAvatar != IntPtr.Zero)
            {
                CAPI.ovrAvatar_SetRightHandGesture(sdkAvatar, ovrAvatarHandGesture.Default);
            }
        }

        // Check to see if the custom gestures need to be updated
        if (sdkAvatar != IntPtr.Zero)
        {
            if (cachedLeftHandCustomPose != null && UpdateTransforms(cachedCustomLeftHandJoints, cachedLeftHandTransforms))
            {
                CAPI.ovrAvatar_SetLeftHandCustomGesture(sdkAvatar, (uint)cachedLeftHandTransforms.Length, cachedLeftHandTransforms);
            }
            if (cachedRightHandCustomPose != null && UpdateTransforms(cachedCustomRightHandJoints, cachedRightHandTransforms))
            {
                CAPI.ovrAvatar_SetRightHandCustomGesture(sdkAvatar, (uint)cachedRightHandTransforms.Length, cachedRightHandTransforms);
            }
        }
    }

    static bool UpdatePoseRoot(Transform poseRoot, ref Transform cachedPoseRoot, ref Transform[] cachedPoseJoints, ref ovrAvatarTransform[] transforms)
    {
        if (poseRoot == cachedPoseRoot)
        {
            return false;
        }

        if (!poseRoot)
        {
            cachedPoseRoot = null;
            cachedPoseJoints = null;
            transforms = null;
        }
        else
        {
            List<Transform> joints = new List<Transform>();
            OrderJoints(poseRoot, joints);
            cachedPoseRoot = poseRoot;
            cachedPoseJoints = joints.ToArray();
            transforms = new ovrAvatarTransform[joints.Count];
        }
        return true;
    }

    static bool UpdateTransforms(Transform[] joints, ovrAvatarTransform[] transforms)
    {
        bool updated = false;
        for (int i = 0; i < joints.Length; ++i)
        {
            Transform joint = joints[i];
            ovrAvatarTransform transform = CreateOvrAvatarTransform(joint.localPosition, joint.localRotation);
            if (transform.position != transforms[i].position || transform.orientation != transforms[i].orientation)
            {
                transforms[i] = transform;
                updated = true;
            }
        }
        return updated;
    }


    private static void OrderJoints(Transform transform, List<Transform> joints)
    {
        joints.Add(transform);
        for (int i = 0; i < transform.childCount; ++i)
        {
            Transform child = transform.GetChild(i);
            OrderJoints(child, joints);
        }
    }

    void AvatarSpecificationCallback(IntPtr avatarSpecification)
    {
        sdkAvatar = CAPI.ovrAvatar_Create(avatarSpecification, Capabilities);
        ShowLeftController(showLeftController);
        ShowRightController(showRightController);

        // Pump the Remote driver once to push the controller type through
        if (Driver != null)
        {
            Driver.UpdateTransformsFromPose(sdkAvatar);
        }

        //Fetch all the assets that this avatar uses.
        UInt32 assetCount = CAPI.ovrAvatar_GetReferencedAssetCount(sdkAvatar);
        for (UInt32 i = 0; i < assetCount; ++i)
        {
            UInt64 id = CAPI.ovrAvatar_GetReferencedAsset(sdkAvatar, i);
            if (OvrAvatarSDKManager.Instance.GetAsset(id) == null)
            {
                OvrAvatarSDKManager.Instance.BeginLoadingAsset(
                    id,
                    LevelOfDetail,
                    AssetLoadedCallback);

                assetLoadingIds.Add(id);
            }
        }

        if (CombineMeshes)
        {
            OvrAvatarSDKManager.Instance.RegisterCombinedMeshCallback(
                sdkAvatar,
                CombinedMeshLoadedCallback);
        }
    }

    void Start()
    {
        if (OvrAvatarSDKManager.Instance == null)
        {
            return;
        }
#if !UNITY_ANDROID
        if (CombineMeshes)
        {
            CombineMeshes = false;
            AvatarLogger.Log("Combined Meshes currently only supported on mobile");
        }
#endif
#if !UNITY_5_5_OR_NEWER
        if (CombineMeshes)
        {
            CombineMeshes = false;
            AvatarLogger.LogWarning("Combined Meshes requires Unity 5.5.0+");
        }
#endif
        materialManager = gameObject.AddComponent<OvrAvatarMaterialManager>();

        try
        {
            oculusUserIDInternal = UInt64.Parse(oculusUserID);
        }
        catch (Exception)
        {
            oculusUserIDInternal = 0;
            AvatarLogger.LogWarning("Invalid Oculus User ID Format");
        }

        // If no oculus ID is supplied then turn off combine meshes to prevent the texture arrays
        // being populated by invalid textures.
        if (oculusUserIDInternal == 0)
        {
            AvatarLogger.LogWarning("Oculus User ID set to 0. Provide actual user ID: " + gameObject.name);
            CombineMeshes = false;
        }

        AvatarLogger.Log("Starting OvrAvatar " + gameObject.name);
        AvatarLogger.Log(AvatarLogger.Tab + "LOD: " + LevelOfDetail.ToString());
        AvatarLogger.Log(AvatarLogger.Tab + "Combine Meshes: " + CombineMeshes);
        AvatarLogger.Log(AvatarLogger.Tab + "Force Mobile Textures: " + USE_MOBILE_TEXTURE_FORMAT);
        AvatarLogger.Log(AvatarLogger.Tab + "Oculus User ID: " + oculusUserIDInternal);

        Capabilities = 0;

        if (EnableBody) Capabilities |= ovrAvatarCapabilities.Body;
        if (EnableHands) Capabilities |= ovrAvatarCapabilities.Hands;
        if (EnableBase && EnableBody) Capabilities |= ovrAvatarCapabilities.Base;
        if (EnableExpressive) Capabilities |= ovrAvatarCapabilities.Expressive;

        // Enable body tilt on 6dof devices
        if(OVRPlugin.positionSupported)
        {
            Capabilities |= ovrAvatarCapabilities.BodyTilt;
        }

        ShowLeftController(StartWithControllers);
        ShowRightController(StartWithControllers);

        OvrAvatarSDKManager.AvatarSpecRequestParams avatarSpecRequest = new OvrAvatarSDKManager.AvatarSpecRequestParams(
            oculusUserIDInternal,
            this.AvatarSpecificationCallback,
            CombineMeshes,
            LevelOfDetail,
            USE_MOBILE_TEXTURE_FORMAT,
            LookAndFeelVersion,
            FallbackLookAndFeelVersion,
            EnableExpressive);

        OvrAvatarSDKManager.Instance.RequestAvatarSpecification(avatarSpecRequest);
        OvrAvatarSDKManager.Instance.AddLoadingAvatar(GetInstanceID());

        waitingForCombinedMesh = CombineMeshes;
        if (Driver != null)
        {
            Driver.Mode = UseSDKPackets ? OvrAvatarDriver.PacketMode.SDK : OvrAvatarDriver.PacketMode.Unity;
        }
    }

    void Update()
    {
        if (!OvrAvatarSDKManager.Instance || sdkAvatar == IntPtr.Zero || materialManager == null)
        {
            return;
        }

        if (Driver != null)
        {
            Driver.UpdateTransforms(sdkAvatar);

            foreach (float[] voiceUpdate in voiceUpdates)
            {
                CAPI.ovrAvatarPose_UpdateVoiceVisualization(sdkAvatar, voiceUpdate);
            }

            voiceUpdates.Clear();
#if AVATAR_INTERNAL
            if (BlendController != null)
            {
                BlendController.UpdateBlend(sdkAvatar);
            }
#endif
            CAPI.ovrAvatarPose_Finalize(sdkAvatar, Time.deltaTime);
        }

        if (RecordPackets)
        {
            RecordFrame();
        }

        if (assetLoadingIds.Count == 0)
        {
            if (!assetsFinishedLoading)
            {
                try
                {
                    BuildRenderComponents();
                }
                catch (Exception e)
                {
                    assetsFinishedLoading = true;
                    throw e; // rethrow the original exception to preserve callstack
                }
#if AVATAR_INTERNAL
                AssetsDoneLoading.Invoke();
#endif
                InitPostLoad();
                assetsFinishedLoading = true;
                OvrAvatarSDKManager.Instance.RemoveLoadingAvatar(GetInstanceID());
            }

            UpdateVoiceBehavior();
            UpdateCustomPoses();
            if (EnableExpressive)
            {
                UpdateExpressive();
            }
        }
    }

    public static ovrAvatarHandInputState CreateInputState(ovrAvatarTransform transform, OvrAvatarDriver.ControllerPose pose)
    {
        ovrAvatarHandInputState inputState = new ovrAvatarHandInputState();
        inputState.transform = transform;
        inputState.buttonMask = pose.buttons;
        inputState.touchMask = pose.touches;
        inputState.joystickX = pose.joystickPosition.x;
        inputState.joystickY = pose.joystickPosition.y;
        inputState.indexTrigger = pose.indexTrigger;
        inputState.handTrigger = pose.handTrigger;
        inputState.isActive = pose.isActive;
        return inputState;
    }

    public void ShowControllers(bool show)
    {
        ShowLeftController(show);
        ShowRightController(show);
    }

    public void ShowLeftController(bool show)
    {
        if (sdkAvatar != IntPtr.Zero)
        {
            CAPI.ovrAvatar_SetLeftControllerVisibility(sdkAvatar, show);
        }
        showLeftController = show;
    }

    public void ShowRightController(bool show)
    {
        if (sdkAvatar != IntPtr.Zero)
        {
            CAPI.ovrAvatar_SetRightControllerVisibility(sdkAvatar, show);
        }
        showRightController = show;
    }

    public void UpdateVoiceVisualization(float[] voiceSamples)
    {
        voiceUpdates.Add(voiceSamples);
    }

    void RecordFrame()
    {
        if(UseSDKPackets)
        {
            RecordSDKFrame();
        }
        else
        {
            RecordUnityFrame();
        }
    }

    // Meant to be used mutually exclusively with RecordSDKFrame to give user more options to optimize or tweak packet data
    private void RecordUnityFrame()
    {
        var deltaSeconds = Time.deltaTime;
        var frame = Driver.GetCurrentPose();
        // If this is our first packet, store the pose as the initial frame
        if (CurrentUnityPacket == null)
        {
            CurrentUnityPacket = new OvrAvatarPacket(frame);
            deltaSeconds = 0;
        }

        float recordedSeconds = 0;
        while (recordedSeconds < deltaSeconds)
        {
            float remainingSeconds = deltaSeconds - recordedSeconds;
            float remainingPacketSeconds = PacketSettings.UpdateRate - CurrentUnityPacket.Duration;

            // If we're not going to fill the packet, just add the frame
            if (remainingSeconds < remainingPacketSeconds)
            {
                CurrentUnityPacket.AddFrame(frame, remainingSeconds);
                recordedSeconds += remainingSeconds;
            }

            // If we're going to fill the packet, interpolate the pose, send the packet,
            // and open a new one
            else
            {
                // Interpolate between the packet's last frame and our target pose
                // to compute a pose at the end of the packet time.
                OvrAvatarDriver.PoseFrame a = CurrentUnityPacket.FinalFrame;
                OvrAvatarDriver.PoseFrame b = frame;
                float t = remainingPacketSeconds / remainingSeconds;
                OvrAvatarDriver.PoseFrame intermediatePose = OvrAvatarDriver.PoseFrame.Interpolate(a, b, t);
                CurrentUnityPacket.AddFrame(intermediatePose, remainingPacketSeconds);
                recordedSeconds += remainingPacketSeconds;

                // Broadcast the recorded packet
                if (PacketRecorded != null)
                {
                    PacketRecorded(this, new PacketEventArgs(CurrentUnityPacket));
                }

                // Open a new packet
                CurrentUnityPacket = new OvrAvatarPacket(intermediatePose);
            }
        }
    }

    private void RecordSDKFrame()
    {
        if (sdkAvatar == IntPtr.Zero)
        {
            return;
        }

        if (!PacketSettings.RecordingFrames)
        {
            CAPI.ovrAvatarPacket_BeginRecording(sdkAvatar);
            PacketSettings.AccumulatedTime = 0.0f;
            PacketSettings.RecordingFrames = true;
        }

        PacketSettings.AccumulatedTime += Time.deltaTime;

        if (PacketSettings.AccumulatedTime >= PacketSettings.UpdateRate)
        {
            PacketSettings.AccumulatedTime = 0.0f;
            var packet = CAPI.ovrAvatarPacket_EndRecording(sdkAvatar);
            CAPI.ovrAvatarPacket_BeginRecording(sdkAvatar);

            if (PacketRecorded != null)
            {
                PacketRecorded(this, new PacketEventArgs(new OvrAvatarPacket { ovrNativePacket = packet }));
            }

            CAPI.ovrAvatarPacket_Free(packet);
        }
    }

    private void AddRenderParts(
        OvrAvatarComponent ovrComponent,
        ovrAvatarComponent component,
        Transform parent)
    {
        bool isBody = ovrComponent.name == "body";
        bool isLeftController = ovrComponent.name == "controller_left";
        bool isReftController = ovrComponent.name == "controller_right";

        for (UInt32 renderPartIndex = 0; renderPartIndex < component.renderPartCount; renderPartIndex++)
        {
            GameObject renderPartObject = new GameObject();
            renderPartObject.name = GetRenderPartName(component, renderPartIndex);
            renderPartObject.transform.SetParent(parent);
            IntPtr renderPart = GetRenderPart(component, renderPartIndex);
            ovrAvatarRenderPartType type = CAPI.ovrAvatarRenderPart_GetType(renderPart);
            OvrAvatarRenderComponent ovrRenderPart = null;
            switch (type)
            {
                case ovrAvatarRenderPartType.SkinnedMeshRender:
                    ovrRenderPart = AddSkinnedMeshRenderComponent(renderPartObject, CAPI.ovrAvatarRenderPart_GetSkinnedMeshRender(renderPart));
                    break;
                case ovrAvatarRenderPartType.SkinnedMeshRenderPBS:
                    ovrRenderPart = AddSkinnedMeshRenderPBSComponent(renderPartObject, CAPI.ovrAvatarRenderPart_GetSkinnedMeshRenderPBS(renderPart));
                    break;
                case ovrAvatarRenderPartType.SkinnedMeshRenderPBS_V2:
                    {
                        ovrRenderPart = AddSkinnedMeshRenderPBSV2Component(
                            renderPart,
                            renderPartObject,
                            CAPI.ovrAvatarRenderPart_GetSkinnedMeshRenderPBSV2(renderPart),
                            isBody && renderPartIndex == 0,
                            isLeftController || isReftController);
                    }
                    break;
                default:
                    break;
            }

            if (ovrRenderPart != null)
            {
                ovrComponent.RenderParts.Add(ovrRenderPart);
            }
        }
    }

    public void RefreshBodyParts()
    {
        if (Body != null)
        {
            foreach (var part in Body.RenderParts)
            {
                Destroy(part.gameObject);
            }

            Body.RenderParts.Clear();

            var nativeAvatarComponent = Body.GetNativeAvatarComponent();
            if (nativeAvatarComponent.HasValue)
            {
                AddRenderParts(Body, nativeAvatarComponent.Value, Body.gameObject.transform);
            }
        }
    }

    public ovrAvatarBodyComponent? GetBodyComponent()
    {
        if (Body != null)
        {
            CAPI.ovrAvatarPose_GetBodyComponent(sdkAvatar, ref Body.component);
            return Body.component;
        }

        return null;
    }

    public Transform GetHandTransform(HandType hand, HandJoint joint)
    {
        if (hand >= HandType.Max || joint >= HandJoint.Max)
        {
            return null;
        }

        var HandObject = hand == HandType.Left ? HandLeft : HandRight;

        if (HandObject != null)
        {
            var AvatarComponent = HandObject.GetComponent<OvrAvatarComponent>();
            if (AvatarComponent != null && AvatarComponent.RenderParts.Count > 0)
            {
                var SkinnedMesh = AvatarComponent.RenderParts[0];
                return SkinnedMesh.transform.Find(HandJoints[(int)hand, (int)joint]);
            }
        }

        return null;
    }

    public void GetPointingDirection(HandType hand, ref Vector3 forward, ref Vector3 up)
    {
        Transform handBase = GetHandTransform(hand, HandJoint.HandBase);

        if (handBase != null)
        {
            forward = handBase.forward;
            up = handBase.up;
        }
    }

    static Vector3 MOUTH_POSITION_OFFSET = new Vector3(0, -0.018f, 0.1051f);
    static string VOICE_PROPERTY = "_Voice";
    static string MOUTH_POSITION_PROPERTY = "_MouthPosition";
    static string MOUTH_DIRECTION_PROPERTY = "_MouthDirection";
    static string MOUTH_SCALE_PROPERTY = "_MouthEffectScale";

    static float MOUTH_SCALE_GLOBAL = 0.007f;
    static float MOUTH_MAX_GLOBAL = 0.007f;
    static string NECK_JONT = "root_JNT/body_JNT/chest_JNT/neckBase_JNT/neck_JNT";

    public float VoiceAmplitude = 0f;
    public bool EnableMouthVertexAnimation = false;

    private void UpdateVoiceBehavior()
    {
        if (!EnableMouthVertexAnimation)
        {
            return;
        }

        if (Body != null)
        {
            OvrAvatarComponent component = Body.GetComponent<OvrAvatarComponent>();

            VoiceAmplitude = Mathf.Clamp(VoiceAmplitude, 0f, 1f);

            if (component.RenderParts.Count > 0)
            {
                var material = component.RenderParts[0].mesh.sharedMaterial;
                var neckJoint = component.RenderParts[0].mesh.transform.Find(NECK_JONT);
                var scaleDiff = neckJoint.TransformPoint(Vector3.up) - neckJoint.position;

                material.SetFloat(MOUTH_SCALE_PROPERTY, scaleDiff.magnitude);

                material.SetFloat(
                    VOICE_PROPERTY,
                    Mathf.Min(scaleDiff.magnitude * MOUTH_MAX_GLOBAL, scaleDiff.magnitude * VoiceAmplitude * MOUTH_SCALE_GLOBAL));

                material.SetVector(
                    MOUTH_POSITION_PROPERTY,
                    neckJoint.TransformPoint(MOUTH_POSITION_OFFSET));

                material.SetVector(MOUTH_DIRECTION_PROPERTY, neckJoint.up);
            }
        }
    }

    bool IsValidMic()
    {
        string[] devices = Microphone.devices;

        if (devices.Length < 1)
        {
            return false;
        }

        int selectedDeviceIndex = 0;
#if UNITY_STANDALONE_WIN
        for (int i = 1; i < devices.Length; i++)
        {
            if (devices[i].ToUpper().Contains("RIFT"))
            {
                selectedDeviceIndex = i;
                break;
            }
        }
#endif

        string selectedDevice = devices[selectedDeviceIndex];

        int minFreq;
        int maxFreq;
        Microphone.GetDeviceCaps(selectedDevice, out minFreq, out maxFreq);

        if (maxFreq == 0)
        {
            maxFreq = 44100;
        }

        AudioClip clip = Microphone.Start(selectedDevice, true, 1, maxFreq);
        if (clip == null)
        {
            return false;
        }

        Microphone.End(selectedDevice);
        return true;
    }

    void InitPostLoad()
    {
        ExpressiveGlobalInit();

        ConfigureHelpers();

        if (GetComponent<OvrAvatarLocalDriver>() != null)
        {
            // Use mic.
            lipsyncContext.audioLoopback = false;
            if (CanOwnMicrophone && IsValidMic())
            {
                micInput = MouthAnchor.gameObject.AddComponent<OVRLipSyncMicInput>();
                micInput.enableMicSelectionGUI = false;
                micInput.MicFrequency = 44100;
                micInput.micControl = OVRLipSyncMicInput.micActivation.ConstantSpeak;
            }

            // Set lipsync animation parameters in SDK
            CAPI.ovrAvatar_SetActionUnitOnsetSpeed(sdkAvatar, ACTION_UNIT_ONSET_SPEED);
            CAPI.ovrAvatar_SetActionUnitFalloffSpeed(sdkAvatar, ACTION_UNIT_FALLOFF_SPEED);
            CAPI.ovrAvatar_SetVisemeMultiplier(sdkAvatar, VISEME_LEVEL_MULTIPLIER);
        }
    }

    static ovrAvatarLights ovrLights = new ovrAvatarLights();
	static void ExpressiveGlobalInit()
	{
		if (doneExpressiveGlobalInit)
		{
			return;
		}

		doneExpressiveGlobalInit = true;

        // This array size has to match the 'MarshalAs' attribute in the ovrAvatarLights declaration.
        const int MAXSIZE = 16;
        ovrLights.lights = new ovrAvatarLight[MAXSIZE];

        InitializeLights();
	}

    static void InitializeLights()
    {
        // Set light info. Lights are shared across all avatar instances.
        ovrLights.ambientIntensity = RenderSettings.ambientLight.grayscale * 0.5f;

        Light[] sceneLights = FindObjectsOfType(typeof(Light)) as Light[];
        int i = 0;
        for (i = 0; i < sceneLights.Length && i < ovrLights.lights.Length; ++i)
        {
            Light sceneLight = sceneLights[i];
            if (sceneLight && sceneLight.enabled)
            {
                uint instanceID = (uint)sceneLight.transform.GetInstanceID();
                switch (sceneLight.type)
                {
                    case LightType.Directional:
                        {
                            CreateLightDirectional(instanceID, sceneLight.transform.forward, sceneLight.intensity, ref ovrLights.lights[i]);
                            break;
                        }
                    case LightType.Point:
                        {
                            CreateLightPoint(instanceID, sceneLight.transform.position, sceneLight.range, sceneLight.intensity, ref ovrLights.lights[i]);
                            break;
                        }
                    case LightType.Spot:
                        {
                            CreateLightSpot(instanceID, sceneLight.transform.position, sceneLight.transform.forward, sceneLight.spotAngle, sceneLight.range, sceneLight.intensity, ref ovrLights.lights[i]);
                            break;
                        }
                }
            }
        }

        ovrLights.lightCount = (uint)i;

        CAPI.ovrAvatar_UpdateLights(ovrLights);
    }

    static ovrAvatarLight CreateLightDirectional(uint id, Vector3 direction, float intensity, ref ovrAvatarLight light)
    {
        light.id = id;
        light.type = ovrAvatarLightType.Direction;
        light.worldDirection = new Vector3(direction.x, direction.y, -direction.z);
        light.intensity = intensity;
        return light;
    }

    static ovrAvatarLight CreateLightPoint(uint id, Vector3 position, float range, float intensity, ref ovrAvatarLight light)
    {
        light.id = id;
        light.type = ovrAvatarLightType.Point;
        light.worldPosition = new Vector3(position.x, position.y, -position.z);
        light.range = range;
        light.intensity = intensity;
        return light;
    }

    static ovrAvatarLight CreateLightSpot(uint id, Vector3 position, Vector3 direction, float spotAngleDeg, float range, float intensity, ref ovrAvatarLight light)
    {
        light.id = id;
        light.type = ovrAvatarLightType.Spot;
        light.worldPosition = new Vector3(position.x, position.y, -position.z);
        light.worldDirection = new Vector3(direction.x, direction.y, -direction.z);
        light.spotAngleDeg = spotAngleDeg;
        light.range = range;
        light.intensity = intensity;
        return light;
    }

    void UpdateExpressive()
    {
        ovrAvatarTransform baseTransform = OvrAvatar.CreateOvrAvatarTransform(transform.position, transform.rotation);
        CAPI.ovrAvatar_UpdateWorldTransform(sdkAvatar, baseTransform);

        UpdateFacewave();
    }

    private void ConfigureHelpers()
    {
        Transform head =
            transform.Find("body/body_renderPart_0/root_JNT/body_JNT/chest_JNT/neckBase_JNT/neck_JNT/head_JNT");
        if (head == null)
        {
            AvatarLogger.LogError("Avatar helper config failed. Cannot find head transform. All helpers spawning on root avatar transform");
            head = transform;
        }

        if (MouthAnchor == null)
        {
            MouthAnchor = CreateHelperObject(head, MOUTH_HEAD_OFFSET, MOUTH_HELPER_NAME);
        }

        if (GetComponent<OvrAvatarLocalDriver>() != null)
        {
            if (audioSource == null)
            {
                audioSource = MouthAnchor.gameObject.AddComponent<AudioSource>();
            }
            spatializedSource = MouthAnchor.GetComponent<ONSPAudioSource>();

            if (spatializedSource == null)
            {
                spatializedSource = MouthAnchor.gameObject.AddComponent<ONSPAudioSource>();
            }

            spatializedSource.UseInvSqr = true;
            spatializedSource.EnableRfl = false;
            spatializedSource.EnableSpatialization = true;
            spatializedSource.Far = 100f;
            spatializedSource.Near = 0.1f;

            // Add phoneme context to the mouth anchor
            lipsyncContext = MouthAnchor.GetComponent<OVRLipSyncContext>();
            if (lipsyncContext == null)
            {
                lipsyncContext = MouthAnchor.gameObject.AddComponent<OVRLipSyncContext>();
            }

            lipsyncContext.provider = EnableLaughter
                ? OVRLipSync.ContextProviders.Enhanced_with_Laughter
                : OVRLipSync.ContextProviders.Enhanced;

            // Ignore audio callback if microphone is owned by VoIP
            lipsyncContext.skipAudioSource = !CanOwnMicrophone;

            StartCoroutine(WaitForMouthAudioSource());
        }

        if (GetComponent<OvrAvatarRemoteDriver>() != null)
        {
            GazeTarget headTarget = head.gameObject.AddComponent<GazeTarget>();
            headTarget.Type = ovrAvatarGazeTargetType.AvatarHead;
            AvatarLogger.Log("Added head as gaze target");

            Transform hand = transform.Find("hand_left");
            if (hand == null)
            {
                AvatarLogger.LogWarning("Gaze target helper config failed: Cannot find left hand transform");
            }
            else
            {
                GazeTarget handTarget = hand.gameObject.AddComponent<GazeTarget>();
                handTarget.Type = ovrAvatarGazeTargetType.AvatarHand;
                AvatarLogger.Log("Added left hand as gaze target");
            }

            hand = transform.Find("hand_right");
            if (hand == null)
            {
                AvatarLogger.Log("Gaze target helper config failed: Cannot find right hand transform");
            }
            else
            {
                GazeTarget handTarget = hand.gameObject.AddComponent<GazeTarget>();
                handTarget.Type = ovrAvatarGazeTargetType.AvatarHand;
                AvatarLogger.Log("Added right hand as gaze target");
            }
        }
    }

    private IEnumerator WaitForMouthAudioSource()
    {
        while (MouthAnchor.GetComponent<AudioSource>() == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        AudioSource AS = MouthAnchor.GetComponent<AudioSource>();
        AS.minDistance = 0.3f;
        AS.maxDistance = 4f;
        AS.rolloffMode = AudioRolloffMode.Logarithmic;
        AS.loop = true;
        AS.playOnAwake = true;
        AS.spatialBlend = 1.0f;
        AS.spatialize = true;
        AS.spatializePostEffects = true;
    }

    public void DestroyHelperObjects()
    {
        if (MouthAnchor)
        {
            DestroyImmediate(MouthAnchor.gameObject);
        }
    }

    public GameObject CreateHelperObject(Transform parent, Vector3 localPositionOffset, string helperName,
        string helperTag = "")
    {
        GameObject helper = new GameObject();
        helper.name = helperName;
        if (helperTag != "")
        {
            helper.tag = helperTag;
        }
        helper.transform.SetParent(parent);
        helper.transform.localRotation = Quaternion.identity;
        helper.transform.localPosition = localPositionOffset;
        return helper;
    }

    public void UpdateVoiceData(short[] pcmData, int numChannels)
    {
      if (lipsyncContext != null && micInput == null)
      {
          lipsyncContext.ProcessAudioSamplesRaw(pcmData, numChannels);
      }
    }
    public void UpdateVoiceData(float[] pcmData, int numChannels)
    {
      if (lipsyncContext != null && micInput == null)
      {
          lipsyncContext.ProcessAudioSamplesRaw(pcmData, numChannels);
      }
    }


    private void UpdateFacewave()
    {
        if (lipsyncContext != null && (micInput != null || CanOwnMicrophone == false))
        {
            // Get the current viseme frame
            currentFrame = lipsyncContext.GetCurrentPhonemeFrame();

            // Verify length (-1 for laughter)
            if (currentFrame.Visemes.Length != (VISEME_COUNT - 1))
            {
                Debug.LogError("Unexpected number of visemes " + currentFrame.Visemes);
                return;
            }

            // Copy to viseme array
            currentFrame.Visemes.CopyTo(visemes, 0);
            // Copy laughter as final element
            visemes[VISEME_COUNT - 1] = EnableLaughter ? currentFrame.laughterScore : 0.0f;

            // Send visemes to native implementation.
            for (int i = 0; i < VISEME_COUNT; i++)
            {
                RuntimeVisemes.visemeParams[i] = visemes[i];
            }
            CAPI.ovrAvatar_SetVisemes(sdkAvatar, RuntimeVisemes);
        }
    }
}
