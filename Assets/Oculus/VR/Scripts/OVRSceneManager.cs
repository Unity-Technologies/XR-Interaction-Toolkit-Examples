/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

/// <summary>
/// A manager for <see cref="OVRSceneAnchor"/>s created using the Room Setup feature.
/// </summary>
public class OVRSceneManager : MonoBehaviour
{
    /// <summary>
    /// A prefab that will be used to instantiate any Plane found
    /// when querying the Scene model. If the anchor contains both
    /// Volume and Plane elements, <see cref="VolumePrefab"/> will
    /// be used instead.
    /// </summary>
    [FormerlySerializedAs("planePrefab")]
    [Tooltip("A prefab that will be used to instantiate any Plane found " +
             "when querying the Scene model. If the anchor contains both " +
             "Volume and Plane elements, Volume will be used instead.")]
    public OVRSceneAnchor PlanePrefab;

    /// <summary>
    /// A prefab that will be used to instantiate any Volume found
    /// when querying the Scene model. This anchor may also contain
    /// Plane elements.
    /// </summary>
    [FormerlySerializedAs("volumePrefab")]
    [Tooltip("A prefab that will be used to instantiate any Volume found " +
             "when querying the Scene model. This anchor may also contain " +
             "Plane elements.")]
    public OVRSceneAnchor VolumePrefab;

    /// <summary>
    /// Overrides the instantiation of the generic Plane and Volume prefabs with specialized ones.
    /// </summary>
    [FormerlySerializedAs("prefabOverrides")]
    [Tooltip("Overrides the instantiation of the generic Plane/Volume prefabs with specialized ones.")]
    public List<OVRScenePrefabOverride> PrefabOverrides = new List<OVRScenePrefabOverride>();

    /// <summary>
    /// When true, verbose debug logs will be emitted.
    /// </summary>
    [FormerlySerializedAs("verboseLogging")]
    [Tooltip("When enabled, verbose debug logs will be emitted.")]
    public bool VerboseLogging;

    /// <summary>
    /// The maximum number of scene anchors that will be updated each frame.
    /// </summary>
    [Tooltip("The maximum number of scene anchors that will be updated each frame.")]
    public int MaxSceneAnchorUpdatesPerFrame = 3;

    /// <summary>
    /// The parent transform to which each new <see cref="OVRSceneAnchor"/> or <see cref="OVRSceneRoom"/>
    /// will be parented upon instantiation.
    /// </summary>
    /// <remarks>
    /// if null, <see cref="OVRSceneRoom"/>(s) instantiated by <see cref="OVRSceneManager"/> will have no parent, and
    /// <see cref="OVRSceneAnchor"/>(s) will have either a <see cref="OVRSceneRoom"/> as their parent or null, that is
    /// they will be instantiated at the scene root. If non-null, <see cref="OVRSceneAnchor"/>(s) that do not
    /// belong to any <see cref="OVRSceneRoom"/>, and <see cref="OVRSceneRoom"/>(s) along with its child
    /// <see cref="OVRSceneAnchor"/>(s) will be parented to <see cref="InitialAnchorParent"/>.
    ///
    /// Changing this value does not affect existing <see cref="OVRSceneAnchor"/>(s) or <see cref="OVRSceneRoom"/>(s).
    /// </remarks>
    public Transform InitialAnchorParent
    {
        get => _initialAnchorParent;
        set => _initialAnchorParent = value;
    }

    [SerializeField]
    [Tooltip("(Optional) The parent transform for each new scene anchor. " +
             "Changing this value does not affect existing scene anchors. May be null.")]
    internal Transform _initialAnchorParent;

    #region Events

    /// <summary>
    /// This event fires when the OVR Scene Manager has correctly loaded the scene definition and
    /// instantiated the prefabs for the planes and volumes. Trap it to know that the logic of the
    /// experience can now continue.
    /// </summary>
    public Action SceneModelLoadedSuccessfully;

    /// <summary>
    /// This event fires when a query load the Scene Model returns no result. It can indicate that the,
    /// user never used the Room Setup in the space they are in.
    /// </summary>
    public Action NoSceneModelToLoad;

    /// <summary>
    /// This event will fire after the Room Setup successfully returns. It can be trapped to load the
    /// scene Model.
    /// </summary>
    public Action SceneCaptureReturnedWithoutError;

    /// <summary>
    /// This event will fire if an error occurred while trying to send the user to Room Setup.
    /// </summary>
    public Action UnexpectedErrorWithSceneCapture;

    /// <summary>
    /// This event fires when the OVR Scene Manager detects a change in the room layout.
    /// It indicates that the user performed Room Setup while the application was paused.
    /// Upon receiving this event, user can call <see cref="LoadSceneModel" /> to reload the scene model.
    /// </summary>
    public Action NewSceneModelAvailable;

    #endregion

    /// <summary>
    /// Represents the available classifications for each <see cref="OVRSceneAnchor"/>.
    /// </summary>
    public static class Classification
    {
        /// <summary>
        /// Represents an <see cref="OVRSceneAnchor"/> that is classified as a floor.
        /// </summary>
        public const string Floor = "FLOOR";

        /// <summary>
        /// Represents an <see cref="OVRSceneAnchor"/> that is classified as a ceiling.
        /// </summary>
        public const string Ceiling = "CEILING";

        /// <summary>
        /// Represents an <see cref="OVRSceneAnchor"/> that is classified as a wall face.
        /// </summary>
        public const string WallFace = "WALL_FACE";

        /// <summary>
        /// Represents an <see cref="OVRSceneAnchor"/> that is classified as a desk.
        /// This label has been deprecated in favor of <see cref="Table"/>.
        /// </summary>
        [Obsolete("Deprecated. Use Table classification instead.")]
        public const string Desk = "DESK";

        /// <summary>
        /// Represents an <see cref="OVRSceneAnchor"/> that is classified as a couch.
        /// </summary>
        public const string Couch = "COUCH";

        /// <summary>
        /// Represents an <see cref="OVRSceneAnchor"/> that is classified as a door frame.
        /// </summary>
        public const string DoorFrame = "DOOR_FRAME";

        /// <summary>
        /// Represents an <see cref="OVRSceneAnchor"/> that is classified as a window frame.
        /// </summary>
        public const string WindowFrame = "WINDOW_FRAME";

        /// <summary>
        /// Represents an <see cref="OVRSceneAnchor"/> that is classified as other.
        /// </summary>
        public const string Other = "OTHER";

        /// <summary>
        /// Represents an <see cref="OVRSceneAnchor"/> that is classified as a storage (e.g., cabinet, shelf).
        /// </summary>
        public const string Storage = "STORAGE";

        /// <summary>
        /// Represents an <see cref="OVRSceneAnchor"/> that is classified as a bed.
        /// </summary>
        public const string Bed = "BED";

        /// <summary>
        /// Represents an <see cref="OVRSceneAnchor"/> that is classified as a screen (e.g., TV, computer monitor).
        /// </summary>
        public const string Screen = "SCREEN";

        /// <summary>
        /// Represents an <see cref="OVRSceneAnchor"/> that is classified as a lamp.
        /// </summary>
        public const string Lamp = "LAMP";

        /// <summary>
        /// Represents an <see cref="OVRSceneAnchor"/> that is classified as a plant.
        /// </summary>
        public const string Plant = "PLANT";

        /// <summary>
        /// Represents an <see cref="OVRSceneAnchor"/> that is classified as a table.
        /// </summary>
        public const string Table = "TABLE";



        /// <summary>
        /// The list of possible semantic labels.
        /// </summary>

        public static IReadOnlyList<string> List { get; } = new[]
        {
            Floor,
            Ceiling,
            WallFace,
#pragma warning disable CS0618 // Type or member is obsolete
            Desk,
#pragma warning restore CS0618 // Type or member is obsolete
            Couch,
            DoorFrame,
            WindowFrame,
            Other,
            Storage,
            Bed,
            Screen,
            Lamp,
            Plant,
            Table,
        };
    }

    /// <summary>
    /// A container for the set of <see cref="OVRSceneAnchor"/>s representing a room.
    /// </summary>
    [Obsolete("RoomLayoutInformation is obsoleted. For each room's layout information " +
              "(floor, ceiling, walls) see " + nameof(OVRSceneRoom) + ".", false)]
    public class RoomLayoutInformation
    {
        /// <summary>
        /// The <see cref="OVRScenePlane"/> representing the floor of the room.
        /// </summary>
        public OVRScenePlane Floor;

        /// <summary>
        /// The <see cref="OVRScenePlane"/> representing the ceiling of the room.
        /// </summary>
        public OVRScenePlane Ceiling;

        /// <summary>
        /// The set of <see cref="OVRScenePlane"/> representing the walls of the room.
        /// </summary>
        public List<OVRScenePlane> Walls = new List<OVRScenePlane>();
    }

    /// <summary>
    /// Describes the room layout of a room in the scene model.
    /// </summary>
    [Obsolete(
        "RoomLayout is obsoleted. For each room's layout information (floor, ceiling, walls) see " +
        nameof(OVRSceneRoom) +
        ".",
        false)]
    public RoomLayoutInformation RoomLayout;

    #region Private Vars

    // We use this to store the request id when attempting to load the scene
    private UInt64 _sceneCaptureRequestId = UInt64.MaxValue;

    private OVRCameraRig _cameraRig;
    private int _sceneAnchorUpdateIndex;
    private List<OVRAnchor> _roomLayoutAnchors = new List<OVRAnchor>();
    private int _roomCounter;
    private Action<bool, List<OVRAnchor>> _onAnchorsFetchCompleted;

    #endregion

    internal struct LogForwarder
    {
        public void Log(string context, string message) => Debug.Log($"[{context}] {message}");
        public void LogWarning(string context, string message) => Debug.LogWarning($"[{context}] {message}");
        public void LogError(string context, string message) => Debug.LogError($"[{context}] {message}");
    }

    internal LogForwarder? Verbose => VerboseLogging ? new LogForwarder() : (LogForwarder?)null;

    internal static class Development
    {
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public static void Log(string context, string message) => Debug.Log($"[{context}] {message}");

        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public static void LogWarning(string context, string message) => Debug.LogWarning($"[{context}] {message}");

        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public static void LogError(string context, string message) => Debug.LogError($"[{context}] {message}");
    }

    void Awake()
    {
        // Only allow one instance at runtime.
        if (FindObjectsOfType<OVRSceneManager>().Length > 1)
        {
            new LogForwarder().LogError(nameof(OVRSceneManager),
                $"Found multiple {nameof(OVRSceneManager)}s. Destroying '{name}'.");
            enabled = false;
            DestroyImmediate(this);
        }

        _onAnchorsFetchCompleted = OnAnchorsFetchCompleted;
    }

    internal async void OnApplicationPause(bool isPaused)
    {
        if (isPaused) return;

        _roomLayoutAnchors.Clear();
        var success = await OVRAnchor.FetchAnchorsAsync<OVRRoomLayout>(_roomLayoutAnchors);
        if (!success)
        {
            Verbose?.Log(nameof(OVRSceneManager), "Failed to retrieve scene model information on resume.");
            return;
        }

        foreach (var anchor in _roomLayoutAnchors)
        {
            if (!OVRSceneAnchor.SceneAnchors.ContainsKey(anchor.Uuid))
            {
                Verbose?.Log(nameof(OVRSceneManager),
                    $"Scene model changed. Invoking {nameof(NewSceneModelAvailable)} event.");
                NewSceneModelAvailable?.Invoke();
                break;
            }
        }

        QueryForExistingAnchorsTransform();
    }

    private async void QueryForExistingAnchorsTransform()
    {
        using (new OVRObjectPool.ListScope<OVRAnchor>(out var anchors))
        using (new OVRObjectPool.ListScope<Guid>(out var uuids))
        {
            foreach (var anchor in OVRSceneAnchor.SceneAnchorsList)
            {
                if (!anchor.Space.Valid || !anchor.IsTracked)
                    continue;

                uuids.Add(anchor.Uuid);
            }

            await OVRAnchor.FetchAnchorsAsync(uuids, anchors);
            UpdateAllSceneAnchors();
        }
    }

    /// <summary>
    /// Loads the scene model from the Room Setup.
    /// </summary>
    /// <remarks>
    /// When running on Quest, Scene is queried to retrieve the entities describing the Scene Model. In the Editor,
    /// the Scene Model is loaded over Link.
    /// </remarks>
    /// <returns>Returns true if the query was successfully registered</returns>
    public bool LoadSceneModel()
    {
        _roomLayoutAnchors.Clear();
        DestroyExistingAnchors();

        var task = OVRAnchor.FetchAnchorsAsync<OVRRoomLayout>(_roomLayoutAnchors);
        task.ContinueWith(_onAnchorsFetchCompleted, _roomLayoutAnchors);

        return task.IsPending;
    }

    private void OnAnchorsFetchCompleted(bool success, List<OVRAnchor> roomLayoutAnchors)
    {
        if (!success) return;

        if (!roomLayoutAnchors.Any())
        {
            Development.LogWarning(nameof(OVRSceneManager),
                "Loading the Scene definition yielded no result. "
                + "Typically, this means the user has not captured the room they are in yet. "
                + "Alternatively, an internal error may be preventing this app from accessing scene. "
                + $"Invoking {nameof(NoSceneModelToLoad)}.");

            NoSceneModelToLoad?.Invoke();
            return;
        }

        InstantiateSceneRooms(roomLayoutAnchors);
    }

    private void InstantiateSceneRooms(List<OVRAnchor> roomLayoutAnchors)
    {
        _roomCounter = roomLayoutAnchors.Count;
        foreach (var anchor in roomLayoutAnchors)
        {
            // Check if anchor already exists
            if (OVRSceneAnchor.SceneAnchors.TryGetValue(anchor.Uuid, out var sceneAnchor))
            {
                sceneAnchor.IsTracked = true;
                return;
            }

            if (!(anchor.TryGetComponent(out OVRRoomLayout roomLayoutComponent) &&
                  roomLayoutComponent.IsEnabled))
            {
                continue;
            }

            var roomGO = new GameObject("Room " + anchor.Uuid);
            roomGO.transform.parent = _initialAnchorParent;

            sceneAnchor = roomGO.AddComponent<OVRSceneAnchor>();
            sceneAnchor.Initialize(anchor.Handle, anchor.Uuid);

            var sceneRoom = roomGO.AddComponent<OVRSceneRoom>();
            sceneRoom.LoadRoom();
        }
    }

    internal void OnSceneRoomLoadCompleted()
    {
        if (--_roomCounter > 0) return;

        SceneModelLoadedSuccessfully?.Invoke();
        Verbose?.Log(nameof(OVRSceneManager), "Scene model loading completed.");

#pragma warning disable CS0618
        RoomLayout = GetRoomLayoutInformation();
    }

    private void DestroyExistingAnchors()
    {
        // Remove all the scene entities in memory. Update with scene entities from new query.
        var anchors = new List<OVRSceneAnchor>();
        OVRSceneAnchor.GetSceneAnchors(anchors);
        foreach (var sceneAnchor in anchors)
        {
            Destroy(sceneAnchor.gameObject);
        }
    }

    /// <summary>
    /// Requests scene capture from the Room Setup.
    /// </summary>
    /// <returns>Returns true if scene capture succeeded, otherwise false.</returns>
    public bool RequestSceneCapture() => RequestSceneCapture("");

    /// <summary>
    /// Requests scene capture with specified types of <see cref="OVRSceneAnchor"/>
    /// </summary>
    /// <param name="requestedAnchorClassifications">A list of <see cref="OVRSceneManager.Classification"/>.</param>
    /// <returns>Returns true if scene capture succeeded, otherwise false.</returns>
    public bool RequestSceneCapture(IEnumerable<string> requestedAnchorClassifications)
    {
        if (requestedAnchorClassifications == null)
        {
            throw new ArgumentNullException(nameof(requestedAnchorClassifications));
        }

        var anchorClassifications = requestedAnchorClassifications.ToList();
        foreach (var classification in anchorClassifications)
        {
            if (!Classification.List.Contains(classification))
            {
                throw new ArgumentException(
                    $"{nameof(requestedAnchorClassifications)} contains invalid anchor {nameof(Classification)} {nameof(classification)}.");
            }
        }

        return RequestSceneCapture(String.Join(",", anchorClassifications));
    }

    private static void OnTrackingSpaceChanged(Transform trackingSpace)
    {
        // Tracking space changed, update all scene anchors using their cache
        UpdateAllSceneAnchors();
    }

    private void Update()
    {
        UpdateSomeSceneAnchors();
    }

    private static void UpdateAllSceneAnchors()
    {
        foreach (var sceneAnchor in OVRSceneAnchor.SceneAnchors.Values)
        {
            sceneAnchor.TryUpdateTransform(true);

            if (sceneAnchor.TryGetComponent(out OVRScenePlane plane))
            {
                plane.UpdateTransform();
                plane.RequestBoundary();
            }

            if (sceneAnchor.TryGetComponent(out OVRSceneVolume volume))
            {
                volume.UpdateTransform();
            }
        }
    }

    private void UpdateSomeSceneAnchors()
    {
        for (var i = 0; i < Math.Min(OVRSceneAnchor.SceneAnchorsList.Count, MaxSceneAnchorUpdatesPerFrame); i++)
        {
            _sceneAnchorUpdateIndex %= OVRSceneAnchor.SceneAnchorsList.Count;
            var anchor = OVRSceneAnchor.SceneAnchorsList[_sceneAnchorUpdateIndex++];
            anchor.TryUpdateTransform(false);
        }
    }

#pragma warning disable CS0618
    private RoomLayoutInformation GetRoomLayoutInformation()
    {
        var roomLayout = new RoomLayoutInformation();
        if (OVRSceneRoom.SceneRoomsList.Any())
        {
            roomLayout.Floor = OVRSceneRoom.SceneRoomsList[0].Floor;
            roomLayout.Ceiling = OVRSceneRoom.SceneRoomsList[0].Ceiling;
            roomLayout.Walls = OVRSceneRoom.SceneRoomsList[0]._walls;
        }

        return roomLayout;
    }

    private bool RequestSceneCapture(string requestString)
    {
#if !UNITY_EDITOR
        return OVRPlugin.RequestSceneCapture(requestString, out _sceneCaptureRequestId);
#elif UNITY_EDITOR_WIN
        Development.LogWarning(nameof(OVRSceneManager),
            "Scene Capture does not work over Link. Please capture a scene with the HMD in standalone mode, then access the scene model over Link.");
        return false;
#else
        return false;
#endif
    }

    private void OnEnable()
    {
        // Bind events
        OVRManager.SceneCaptureComplete += OVRManager_SceneCaptureComplete;

        if (OVRManager.display != null)
        {
            OVRManager.display.RecenteredPose += UpdateAllSceneAnchors;
        }

        if (!_cameraRig)
        {
            _cameraRig = FindObjectOfType<OVRCameraRig>();
        }

        if (_cameraRig)
        {
            _cameraRig.TrackingSpaceChanged += OnTrackingSpaceChanged;
        }
    }

    private void OnDisable()
    {
        // Unbind events
        OVRManager.SceneCaptureComplete -= OVRManager_SceneCaptureComplete;

        if (OVRManager.display != null)
        {
            OVRManager.display.RecenteredPose -= UpdateAllSceneAnchors;
        }

        if (_cameraRig)
        {
            _cameraRig.TrackingSpaceChanged -= OnTrackingSpaceChanged;
        }
    }

    #region Action callbacks

    private void OVRManager_SceneCaptureComplete(UInt64 requestId, bool result)
    {
        if (requestId != _sceneCaptureRequestId)
        {
            Verbose?.LogWarning(nameof(OVRSceneManager),
                $"Scene Room Setup with requestId: [{requestId}] was ignored, as it was not issued by this Scene Load request.");
            return;
        }

        Development.Log(nameof(OVRSceneManager),
            $"{nameof(OVRManager_SceneCaptureComplete)}() requestId: [{requestId}] result: [{result}]");

        if (result)
        {
            // Either the user created a room, or they confirmed that the existing room is up to date. We can now load it.
            Development.Log(nameof(OVRSceneManager),
                $"The Room Setup returned without errors. Invoking {nameof(SceneCaptureReturnedWithoutError)}.");
            SceneCaptureReturnedWithoutError?.Invoke();
        }
        else
        {
            Development.LogError(nameof(OVRSceneManager),
                $"An error occurred when sending the user to the Room Setup. Invoking {nameof(UnexpectedErrorWithSceneCapture)}.");
            UnexpectedErrorWithSceneCapture?.Invoke();
        }
    }

    internal OVRSceneAnchor InstantiateSceneAnchor(OVRSpace space, Guid uuid, OVRSceneAnchor prefab)
    {
        // Query for the semantic classification of the object
        var hasSemanticLabels = OVRPlugin.GetSpaceSemanticLabels(space, out var labelString);
        var labels = hasSemanticLabels
            ? labelString.Split(',')
            : Array.Empty<string>();

        // Search the prefab override for a matching label, and if found override the prefab
        if (PrefabOverrides.Count > 0)
        {
            foreach (var label in labels)
            {
                // Skip empty labels
                if (string.IsNullOrEmpty(label)) continue;

                // Search the prefab override for an entry matching the label
                foreach (var @override in PrefabOverrides)
                {
                    if (@override.Prefab &&
                        @override.ClassificationLabel == label)
                    {
                        prefab = @override.Prefab;
                        break;
                    }
                }
            }
        }

        // This can occur if neither the prefab nor any matching override prefab is set in the inspector
        if (prefab == null)
        {
            Verbose?.Log(nameof(OVRSceneManager),
                $"No prefab was provided for space: [{space}]"
                + (labels.Length > 0 ? $" with semantic label {labels[0]}" : ""));
            return null;
        }

        var sceneAnchor = Instantiate(prefab, Vector3.zero, Quaternion.identity, _initialAnchorParent);
        sceneAnchor.Initialize(space, uuid);

        return sceneAnchor;
    }

    #endregion
}
