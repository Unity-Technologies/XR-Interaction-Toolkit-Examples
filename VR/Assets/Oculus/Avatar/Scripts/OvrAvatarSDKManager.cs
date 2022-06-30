using UnityEngine;
using Oculus.Avatar;
using System;
using System.Collections.Generic;

public delegate void specificationCallback(IntPtr specification);
public delegate void assetLoadedCallback(OvrAvatarAsset asset);
public delegate void combinedMeshLoadedCallback(IntPtr asset);

public class OvrAvatarSDKManager : MonoBehaviour
{
    private static OvrAvatarSDKManager _instance;
    private bool initialized = false;
    private Dictionary<ulong, HashSet<specificationCallback>> specificationCallbacks;
    private Dictionary<ulong, HashSet<assetLoadedCallback>> assetLoadedCallbacks;
    private Dictionary<IntPtr, combinedMeshLoadedCallback> combinedMeshLoadedCallbacks;
    private Dictionary<UInt64, OvrAvatarAsset> assetCache;
    private OvrAvatarTextureCopyManager textureCopyManager;

    public ovrAvatarLogLevel LoggingLevel = ovrAvatarLogLevel.Info;
    private Queue<AvatarSpecRequestParams> avatarSpecificationQueue;
    private List<int> loadingAvatars;
    private bool avatarSpecRequestAvailable = true;
    private float lastDispatchedAvatarSpecRequestTime = 0f;
    private const float AVATAR_SPEC_REQUEST_TIMEOUT = 5f;

#if AVATAR_DEBUG
    private ovrAvatarDebugContext debugContext = ovrAvatarDebugContext.None;
#endif

    public struct AvatarSpecRequestParams
    {
        public UInt64 _userId;
        public specificationCallback _callback;
        public bool _useCombinedMesh;
        public ovrAvatarAssetLevelOfDetail _lod;
        public bool _forceMobileTextureFormat;
        public ovrAvatarLookAndFeelVersion _lookVersion;
        public ovrAvatarLookAndFeelVersion _fallbackVersion;
        public bool _enableExpressive;

        public AvatarSpecRequestParams(
            UInt64 userId,
            specificationCallback callback,
            bool useCombinedMesh,
            ovrAvatarAssetLevelOfDetail lod,
            bool forceMobileTextureFormat,
            ovrAvatarLookAndFeelVersion lookVersion,
            ovrAvatarLookAndFeelVersion fallbackVersion,
            bool enableExpressive)
        {
            _userId = userId;
            _callback = callback;
            _useCombinedMesh = useCombinedMesh;
            _lod = lod;
            _forceMobileTextureFormat = forceMobileTextureFormat;
            _lookVersion = lookVersion;
            _fallbackVersion = fallbackVersion;
            _enableExpressive = enableExpressive;
        }
    }

    public static OvrAvatarSDKManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<OvrAvatarSDKManager>();
                if (_instance == null)
                {
                    GameObject manager = new GameObject("OvrAvatarSDKManager");
                    _instance = manager.AddComponent<OvrAvatarSDKManager>();
                    _instance.textureCopyManager = manager.AddComponent<OvrAvatarTextureCopyManager>();
                    _instance.initialized = _instance.Initialize();
                }
            }
            return _instance.initialized ? _instance : null;
        }
    }

    private bool Initialize()
    {
        CAPI.Initialize();

        string appId = GetAppId();

        if (appId == "")
        {
            AvatarLogger.LogError("No Oculus App ID has been provided for target platform. " +
                "Go to Oculus Avatar > Edit Configuration to supply one", OvrAvatarSettings.Instance);
            appId = "0";
        }

#if UNITY_ANDROID && !UNITY_EDITOR
#if AVATAR_XPLAT
        CAPI.ovrAvatar_Initialize(appId);
#else
        CAPI.ovrAvatar_InitializeAndroidUnity(appId);
#endif
#else
        CAPI.ovrAvatar_Initialize(appId);
        CAPI.SendEvent("initialize", appId);
#endif
        specificationCallbacks = new Dictionary<UInt64, HashSet<specificationCallback>>();
        assetLoadedCallbacks = new Dictionary<UInt64, HashSet<assetLoadedCallback>>();
        combinedMeshLoadedCallbacks = new Dictionary<IntPtr, combinedMeshLoadedCallback>();
        assetCache = new Dictionary<ulong, OvrAvatarAsset>();
        avatarSpecificationQueue = new Queue<AvatarSpecRequestParams>();
        loadingAvatars = new List<int>();

        CAPI.ovrAvatar_SetLoggingLevel(LoggingLevel);
        CAPI.ovrAvatar_RegisterLoggingCallback(CAPI.LoggingCallback);
#if AVATAR_DEBUG
        CAPI.ovrAvatar_SetDebugDrawContext((uint)debugContext);
#endif

        return true;
    }

    void OnDestroy()
    {
        CAPI.Shutdown();
        CAPI.ovrAvatar_RegisterLoggingCallback(null);
        CAPI.ovrAvatar_Shutdown();
    }

	void Update()
	{
	    if (Instance == null)
	    {
	        return;
	    }
#if AVATAR_DEBUG
        // Call before ovrAvatarMessage_Pop which flushes the state
        CAPI.ovrAvatar_DrawDebugLines();
#endif

        // Dispatch waiting avatar spec request
        if (avatarSpecificationQueue.Count > 0 &&
	        (avatarSpecRequestAvailable ||
	        Time.time - lastDispatchedAvatarSpecRequestTime >= AVATAR_SPEC_REQUEST_TIMEOUT))
	    {
	        avatarSpecRequestAvailable = false;
	        AvatarSpecRequestParams avatarSpec = avatarSpecificationQueue.Dequeue();
	        DispatchAvatarSpecificationRequest(avatarSpec);
            lastDispatchedAvatarSpecRequestTime = Time.time;
            AvatarLogger.Log("Avatar spec request dispatched: " + avatarSpec._userId);
        }

        IntPtr message = CAPI.ovrAvatarMessage_Pop();
        if (message == IntPtr.Zero)
        {
            return;
        }

        ovrAvatarMessageType messageType = CAPI.ovrAvatarMessage_GetType(message);
        switch (messageType)
        {
            case ovrAvatarMessageType.AssetLoaded:
                {
                    ovrAvatarMessage_AssetLoaded assetMessage = CAPI.ovrAvatarMessage_GetAssetLoaded(message);
                    IntPtr asset = assetMessage.asset;
                    UInt64 assetID = assetMessage.assetID;
                    ovrAvatarAssetType assetType = CAPI.ovrAvatarAsset_GetType(asset);
                    OvrAvatarAsset assetData = null;
                    IntPtr avatarOwner = IntPtr.Zero;

                    switch (assetType)
                    {
                        case ovrAvatarAssetType.Mesh:
                            assetData = new OvrAvatarAssetMesh(assetID, asset, ovrAvatarAssetType.Mesh);
                            break;
                        case ovrAvatarAssetType.Texture:
                            assetData = new OvrAvatarAssetTexture(assetID, asset);
                            break;
                        case ovrAvatarAssetType.Material:
                            assetData = new OvrAvatarAssetMaterial(assetID, asset);
                            break;
                        case ovrAvatarAssetType.CombinedMesh:
                            avatarOwner = CAPI.ovrAvatarAsset_GetAvatar(asset);
                            assetData = new OvrAvatarAssetMesh(assetID, asset, ovrAvatarAssetType.CombinedMesh);
                            break;
                        case ovrAvatarAssetType.FailedLoad:
                            AvatarLogger.LogWarning("Asset failed to load from SDK " + assetID);
                            break;
                        default:
                            throw new NotImplementedException(string.Format("Unsupported asset type format {0}", assetType.ToString()));
                    }

                    HashSet<assetLoadedCallback> callbackSet;
                    if (assetType == ovrAvatarAssetType.CombinedMesh)
                    {
                        if (!assetCache.ContainsKey(assetID))
                        {
                            assetCache.Add(assetID, assetData);
                        }

                        combinedMeshLoadedCallback callback;
                        if (combinedMeshLoadedCallbacks.TryGetValue(avatarOwner, out callback))
                        {
                            callback(asset);
                            combinedMeshLoadedCallbacks.Remove(avatarOwner);
                        }
                        else
                        {
                            AvatarLogger.LogWarning("Loaded a combined mesh with no owner: " + assetMessage.assetID);
                        }
                    }
                    else
                    {
                        if (assetData != null && assetLoadedCallbacks.TryGetValue(assetMessage.assetID, out callbackSet))
                        {
                            assetCache.Add(assetID, assetData);

                            foreach (var callback in callbackSet)
                            {
                                callback(assetData);
                            }

                            assetLoadedCallbacks.Remove(assetMessage.assetID);
                        }
                    }
                    break;
                }
            case ovrAvatarMessageType.AvatarSpecification:
            {
                    avatarSpecRequestAvailable = true;
                    ovrAvatarMessage_AvatarSpecification spec = CAPI.ovrAvatarMessage_GetAvatarSpecification(message);
                    HashSet<specificationCallback> callbackSet;
                    if (specificationCallbacks.TryGetValue(spec.oculusUserID, out callbackSet))
                    {
                        foreach (var callback in callbackSet)
                        {
                            callback(spec.avatarSpec);
                        }

                        specificationCallbacks.Remove(spec.oculusUserID);
                    }
                    else
                    {
                        AvatarLogger.LogWarning("Error, got an avatar specification callback from a user id we don't have a record for: " + spec.oculusUserID);
                    }
                    break;
                }
            default:
                throw new NotImplementedException("Unhandled ovrAvatarMessageType: " + messageType);
        }
        CAPI.ovrAvatarMessage_Free(message);
    }

    public bool IsAvatarSpecWaiting()
    {
        return avatarSpecificationQueue.Count > 0;
    }

    public bool IsAvatarLoading()
    {
        return loadingAvatars.Count > 0;
    }

    // Add avatar gameobject ID to loading list to keep track of loading avatars
    public void AddLoadingAvatar(int gameobjectID)
    {
        loadingAvatars.Add(gameobjectID);
    }

    // Remove avatar gameobject ID from loading list
    public void RemoveLoadingAvatar(int gameobjectID)
    {
        loadingAvatars.Remove(gameobjectID);
    }

    // Request an avatar specification to be loaded by adding to the queue.
    // Requests are dispatched in Update().
    public void RequestAvatarSpecification(AvatarSpecRequestParams avatarSpecRequest)
    {
        avatarSpecificationQueue.Enqueue(avatarSpecRequest);
        AvatarLogger.Log("Avatar spec request queued: " + avatarSpecRequest._userId.ToString());
    }

    private void DispatchAvatarSpecificationRequest(AvatarSpecRequestParams avatarSpecRequest)
    {
        textureCopyManager.CheckFallbackTextureSet(avatarSpecRequest._lod);
        CAPI.ovrAvatar_SetForceASTCTextures(avatarSpecRequest._forceMobileTextureFormat);

        HashSet<specificationCallback> callbackSet;
        if (!specificationCallbacks.TryGetValue(avatarSpecRequest._userId, out callbackSet))
        {
            callbackSet = new HashSet<specificationCallback>();
            specificationCallbacks.Add(avatarSpecRequest._userId, callbackSet);

            IntPtr specRequest = CAPI.ovrAvatarSpecificationRequest_Create(avatarSpecRequest._userId);
            CAPI.ovrAvatarSpecificationRequest_SetLookAndFeelVersion(specRequest, avatarSpecRequest._lookVersion);
            CAPI.ovrAvatarSpecificationRequest_SetFallbackLookAndFeelVersion(specRequest, avatarSpecRequest._fallbackVersion);
            CAPI.ovrAvatarSpecificationRequest_SetLevelOfDetail(specRequest, avatarSpecRequest._lod);
            CAPI.ovrAvatarSpecificationRequest_SetCombineMeshes(specRequest, avatarSpecRequest._useCombinedMesh);
            CAPI.ovrAvatarSpecificationRequest_SetExpressiveFlag(specRequest, avatarSpecRequest._enableExpressive);
            CAPI.ovrAvatar_RequestAvatarSpecificationFromSpecRequest(specRequest);
            CAPI.ovrAvatarSpecificationRequest_Destroy(specRequest);
        }

        callbackSet.Add(avatarSpecRequest._callback);
    }

    public void BeginLoadingAsset(
        UInt64 assetId,
        ovrAvatarAssetLevelOfDetail lod,
        assetLoadedCallback callback)
    {
        HashSet<assetLoadedCallback> callbackSet;
        if (!assetLoadedCallbacks.TryGetValue(assetId, out callbackSet))
        {
            callbackSet = new HashSet<assetLoadedCallback>();
            assetLoadedCallbacks.Add(assetId, callbackSet);
        }
        AvatarLogger.Log("Loading Asset ID: " + assetId);
        CAPI.ovrAvatarAsset_BeginLoadingLOD(assetId, lod);
        callbackSet.Add(callback);
    }

    public void RegisterCombinedMeshCallback(
        IntPtr sdkAvatar,
        combinedMeshLoadedCallback callback)
    {
        combinedMeshLoadedCallback currentCallback;
        if (!combinedMeshLoadedCallbacks.TryGetValue(sdkAvatar, out currentCallback))
        {
            combinedMeshLoadedCallbacks.Add(sdkAvatar, callback);
        }
        else
        {
            throw new Exception("Adding second combind mesh callback for same avatar");
        }
    }

    public OvrAvatarAsset GetAsset(UInt64 assetId)
    {
        OvrAvatarAsset asset;
        if (assetCache.TryGetValue(assetId, out asset))
        {
            return asset;
        }
        else
        {
            return null;
        }
    }

    public void DeleteAssetFromCache(UInt64 assetId)
    {
        if (assetCache.ContainsKey(assetId))
        {
            assetCache.Remove(assetId);
        }
    }

    public string GetAppId()
    {
        return UnityEngine.Application.platform == RuntimePlatform.Android ?
                OvrAvatarSettings.MobileAppID : OvrAvatarSettings.AppID;
    }

    public OvrAvatarTextureCopyManager GetTextureCopyManager()
    {
        if (textureCopyManager != null)
        {
            return textureCopyManager;
        }
        else
        {
            return null;
        }
    }
}
