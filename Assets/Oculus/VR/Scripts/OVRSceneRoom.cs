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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Represents a <see cref="OVRRoomLayout"/> type Scene anchor.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(OVRSceneAnchor))]
public class OVRSceneRoom : MonoBehaviour, IOVRSceneComponent
{
    /// <summary>
    /// The <see cref="OVRScenePlane"/> representing the floor of the room.
    /// </summary>
    public OVRScenePlane Floor { get; private set; }

    /// <summary>
    /// The <see cref="OVRScenePlane"/> representing the ceiling of the room.
    /// </summary>
    public OVRScenePlane Ceiling { get; private set; }

    /// <summary>
    /// The set of <see cref="OVRScenePlane"/> representing the walls of the room.
    /// </summary>
    public OVRScenePlane[] Walls { get; private set; } = Array.Empty<OVRScenePlane>();

    internal List<OVRScenePlane> _walls = new List<OVRScenePlane>();

    private readonly Dictionary<Guid, int> _orderedRoomGuids = new Dictionary<Guid, int>();
    private Comparison<OVRScenePlane> _wallOrderComparer;

    private OVRSceneAnchor _sceneAnchor;
    private OVRSceneManager _sceneManager;

    internal HashSet<Guid> _uuidToQuery = new HashSet<Guid>();
    private List<OVRAnchor> _roomAnchors = OVRObjectPool.Get<List<OVRAnchor>>();

    internal static readonly Dictionary<Guid, OVRSceneRoom> SceneRooms = new Dictionary<Guid, OVRSceneRoom>();
    internal static readonly List<OVRSceneRoom> SceneRoomsList = new List<OVRSceneRoom>();
    private int _taskCount;

    private Action<bool> _onFetchAnchorsCompleted;
    private Action<bool, OVRAnchor> _onAnchorLocalizationCompleted;

    private void Awake()
    {
        _sceneAnchor = GetComponent<OVRSceneAnchor>();
        _sceneManager = FindObjectOfType<OVRSceneManager>();

        _wallOrderComparer = (planeA, planeB) =>
        {
            bool TryGetUuid(OVRScenePlane plane, out int index)
            {
                var guid = plane.GetComponent<OVRSceneAnchor>().Uuid;
                if (_orderedRoomGuids.TryGetValue(guid, out index)) return true;

                OVRSceneManager.Development.LogWarning(nameof(OVRSceneRoom),
                    $"{nameof(OVRScenePlane)} {guid} does not belong to the current room layout.");
                return false;
            }

            if (!TryGetUuid(planeA, out var indexA)) return 0;
            if (!TryGetUuid(planeB, out var indexB)) return 0;

            return indexA.CompareTo(indexB);
        };

        if (_sceneAnchor.Space.Valid)
        {
            ((IOVRSceneComponent)this).Initialize();
        }

        _onFetchAnchorsCompleted = OnFetchAnchorsCompleted;
        _onAnchorLocalizationCompleted = OnLocalizationCompleted;
    }

    void IOVRSceneComponent.Initialize()
    {
        GetUuidsToQuery();
        SceneRooms[_sceneAnchor.Uuid] = this;
        SceneRoomsList.Add(this);
    }

    internal void LoadRoom()
    {
        if (!_uuidToQuery.Any()) return;

        _roomAnchors.Clear();
        OVRAnchor.FetchAnchorsAsync(_uuidToQuery, _roomAnchors).ContinueWith(_onFetchAnchorsCompleted);
    }

    private void OnFetchAnchorsCompleted(bool success)
    {
        if (!success)
        {
            _sceneManager.Verbose?.LogWarning(nameof(OVRSceneRoom), "Failed to fetch the room anchors.");
            return;
        }

        _walls.Clear();
        _taskCount = _roomAnchors.Count;

        foreach (var anchor in _roomAnchors)
        {
            // Check if already exist.
            if (OVRSceneAnchor.SceneAnchors.ContainsKey(anchor.Uuid))
            {
                OVRSceneAnchor.SceneAnchors[anchor.Uuid].IsTracked = true;
                continue;
            }

            if (!anchor.TryGetComponent(out OVRLocatable locatableComponent))
            {
                continue;
            }

            locatableComponent.SetEnabledAsync(true).ContinueWith(_onAnchorLocalizationCompleted, anchor);
        }
    }

    private void OnLocalizationCompleted(bool success, OVRAnchor anchor)
    {
        _taskCount--;

        if (!success)
        {
            _sceneManager.Verbose?.LogWarning(nameof(OVRSceneRoom),
                $"Failed to enable the {nameof(OVRLocatable)} component for anchor {nameof(anchor.Uuid)}");
            return;
        }

        OVRPlugin.GetSpaceComponentStatus(anchor.Handle, OVRPlugin.SpaceComponentType.Bounded2D,
            out bool bounded2dEnabled, out _);
        OVRPlugin.GetSpaceComponentStatus(anchor.Handle, OVRPlugin.SpaceComponentType.Bounded3D,
            out bool bounded3dEnabled, out _);
        var isStrictly2D = bounded2dEnabled && !bounded3dEnabled;

        // The plane prefab is for anchors that are only 2D, i.e. they only have
        // a 2D component. If a volume component exists, we use a volume prefab,
        // else we pass null (prefab overrides may be used)
        var prefab = isStrictly2D ? _sceneManager.PlanePrefab :
            (bounded3dEnabled ? _sceneManager.VolumePrefab : null);

        var sceneAnchor = _sceneManager.InstantiateSceneAnchor(anchor.Handle, anchor.Uuid, prefab);
        if (sceneAnchor != null)
        {
            sceneAnchor.transform.parent = transform;
            if (isStrictly2D)
                UpdateRoomInformation(sceneAnchor.GetComponent<OVRScenePlane>());
        }

        if (_taskCount == 0)
        {
            _walls.Sort(_wallOrderComparer);
            Walls = _walls.ToArray();
            _sceneManager.Verbose?.Log(nameof(OVRSceneRoom), "Scene room loaded successfully.");

            // Room load completed.
            _sceneManager.OnSceneRoomLoadCompleted();
        }
    }

    private void UpdateRoomInformation(OVRScenePlane plane)
    {
        if (!plane.TryGetComponent(out OVRSemanticClassification classification))
            return;

        foreach (var label in classification.Labels)
        {
            switch (label)
            {
                case OVRSceneManager.Classification.Floor:
                    Floor = plane;
                    break;
                case OVRSceneManager.Classification.Ceiling:
                    Ceiling = plane;
                    break;
                case OVRSceneManager.Classification.WallFace:
                    _walls.Add(plane);
                    break;
            }
        }
    }

    private void GetUuidsToQuery()
    {
        _uuidToQuery.Clear();

        if (!OVRPlugin.GetSpaceContainer(_sceneAnchor.Space, out var spaceContainerUuids))
        {
            return;
        }

        foreach (var uuid in spaceContainerUuids)
        {
            _uuidToQuery.Add(uuid);
        }

        _sceneManager.Verbose?.Log(nameof(OVRSceneRoom),
            $"{nameof(OVRPlugin.GetSpaceContainer)}: success [{true}], count [{_uuidToQuery.Count}]");


        if (!OVRPlugin.GetSpaceRoomLayout(_sceneAnchor.Space, out var roomLayout))
        {
            _sceneManager.Verbose?.LogWarning(nameof(OVRSceneRoom),
                $"[{_sceneAnchor.Uuid}] has component {nameof(OVRPlugin.SpaceComponentType.RoomLayout)} " +
                $"but {nameof(OVRPlugin.GetSpaceRoomLayout)} failed. Ignoring room.");
            return;
        }

        // save room ids and add to queryables (duplicates are filtered)
        if (!roomLayout.ceilingUuid.Equals(Guid.Empty))
            _uuidToQuery.Add(roomLayout.ceilingUuid);
        if (!roomLayout.floorUuid.Equals(Guid.Empty))
            _uuidToQuery.Add(roomLayout.floorUuid);

        _orderedRoomGuids.Clear();
        int validWallsCount = 0;
        foreach (var wallUuid in roomLayout.wallUuids)
        {
            _sceneManager.Verbose?.Log(nameof(OVRSceneManager),
                $"{nameof(OVRPlugin.GetSpaceRoomLayout)}: wall [{wallUuid}]");
            _orderedRoomGuids[wallUuid] = validWallsCount++;
            if (!wallUuid.Equals(Guid.Empty)) _uuidToQuery.Add(wallUuid);
        }
    }

    private void OnDestroy()
    {
        SceneRooms.Remove(_sceneAnchor.Uuid);
        SceneRoomsList.Remove(this);
    }
}
