// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_NETCODE_GAMEOBJECTS && TOOLBAR_EXTENDER

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;
using static Meta.Utilities.NetworkSettings;

namespace Meta.Utilities.Editor
{
    [ExecuteInEditMode]
    public class NetworkSettingsToolbar
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static IEnumerable<Scene> GetOpenScenes()
        {
            for (var i = 0; i != SceneManager.sceneCount; i += 1)
                yield return SceneManager.GetSceneAt(i);
        }

        private static (string path, bool wasLoaded, bool active)[] s_editScenes;

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change is PlayModeStateChange.ExitingEditMode && Autostart)
            {
                var openScenes = GetOpenScenes();
                s_editScenes = openScenes.
                    Select(s => (s.path, s.isLoaded, SceneManager.GetActiveScene() == s)).
                    ToArray();

                var scene = EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(0), OpenSceneMode.Additive);
                _ = SceneManager.SetActiveScene(scene);
                foreach (var s in openScenes)
                {
                    if (s.path != scene.path)
                    {
                        _ = EditorSceneManager.CloseScene(s, false);
                    }
                }
            }
            else if (change is PlayModeStateChange.EnteredEditMode && s_editScenes != null)
            {
                LoadEditScenes();
                s_editScenes = null;
            }
        }

        private static void LoadEditScenes()
        {
            var tmpScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var (path, wasLoaded, isActiveScene) in s_editScenes)
            {
                var newScene = EditorSceneManager.OpenScene(path, wasLoaded ? OpenSceneMode.Additive : OpenSceneMode.AdditiveWithoutLoading);
                if (isActiveScene)
                    _ = SceneManager.SetActiveScene(newScene);
            }
            _ = EditorSceneManager.CloseScene(tmpScene, true);
        }

        private static void OnToolbarGUI()
        {
            Autostart = GUILayout.Toggle(Autostart, new GUIContent("Autostart", "On play, automatically host or join a session."), GUI.skin.button);

            if (Autostart)
            {
                GUILayout.Space(8);

                UseDeviceRoom = GUILayout.Toggle(UseDeviceRoom, new GUIContent("Use Device Room", "When autostarting, join a room unique to this device. Otherwise, join the room with the name entered to the right."), GUI.skin.button);

                if (!UseDeviceRoom)
                {
                    RoomName = GUILayout.TextField(RoomName, 128, GUILayout.MinWidth(128));
                }
            }

            GUILayout.FlexibleSpace();
        }
    }
}

#endif
