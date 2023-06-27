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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Oculus.Interaction.Editor
{
    [InitializeOnLoad]
    public static class UnityObjectAddedBroadcaster
    {
        public static event Action<GameObject> WhenGameObjectHierarchyAdded = (_) => {};
        public static event Action<Component> WhenComponentAdded = (_) => {};
        private static int _objectAddedUndoNestingCounter = 0;
        private static int _objectAddedUndoGroupId = -1;

        static UnityObjectAddedBroadcaster()
        {
            HashSet<int> knownIds = new HashSet<int>();

            EditorSceneManager.SceneOpenedCallback handleSceneOpened = (scene, mode) =>
            {
                UnityObjectAddedBroadcaster.HandleSceneOpened(scene, mode, knownIds);
            };

            Action handleHierarchyChanged = () =>
            {
                UnityObjectAddedBroadcaster.HandleHierarchyChanged(knownIds);
            };

            Action<Component> handleComponentWasAdded = (component) =>
            {
                UnityObjectAddedBroadcaster.HandleComponentWasAdded(component);
            };

            AssemblyReloadEvents.AssemblyReloadCallback handleBeforeAssemblyReload = null;
            handleBeforeAssemblyReload = () =>
            {
                UnityObjectAddedBroadcaster.WhenGameObjectHierarchyAdded = (_) => { };
                UnityObjectAddedBroadcaster.WhenComponentAdded = (_) => { };

                EditorSceneManager.sceneOpened -= handleSceneOpened;
                EditorApplication.hierarchyChanged -= handleHierarchyChanged;
                ObjectFactory.componentWasAdded -= handleComponentWasAdded;
                AssemblyReloadEvents.beforeAssemblyReload -= handleBeforeAssemblyReload;
            };

            EditorSceneManager.sceneOpened += handleSceneOpened;
            EditorApplication.hierarchyChanged += handleHierarchyChanged;
            ObjectFactory.componentWasAdded += handleComponentWasAdded;
            AssemblyReloadEvents.beforeAssemblyReload += handleBeforeAssemblyReload;

            for (int idx = 0; idx < SceneManager.loadedSceneCount; ++idx)
            {
                handleSceneOpened(EditorSceneManager.GetSceneAt(idx), OpenSceneMode.Additive);
            }
        }

        private static void HandleSceneOpened(Scene scene, OpenSceneMode mode, HashSet<int> knownIds)
        {
            if (mode == OpenSceneMode.Single)
            {
                knownIds.Clear();
            }

            AddInstanceIdsFromSubHierarchyToCache(knownIds, scene.GetRootGameObjects());
        }

        /// <summary>
        /// Fires signals for GameObjects and Components added through the addition of a prefab,
        /// checking whether the selected GameObject at the moment of a hierarchy change is
        /// unfamiliar (i.e., has an instance ID which is not already in known IDs) and signaling
        /// appropriately. Note that this will NOT signal GameObjects or Components added to the
        /// scene through prefab updating, which are added without modifying the Editor's
        /// selection variable, upon which this handler relies.
        /// </summary>
        /// <param name="knownIds">Cache of known GameObject instance IDs</param>
        private static void HandleHierarchyChanged(HashSet<int> knownIds)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            var selection = Selection.activeGameObject;

            if (selection == null)
            {
                return;
            }

            if (!knownIds.Contains(selection.GetInstanceID()))
            {
                AddInstanceIdsFromSubHierarchyToCache(knownIds, selection);

                StartUndoGroup();
                UnityObjectAddedBroadcaster.WhenGameObjectHierarchyAdded(selection);

                // ObjectFactory.componentWasAdded is not called for components added to the scene
                // as part of a prefab, so we manually iterate them here so that
                // Signaler.WhenComponentAdded presents a more complete picture of activity in
                // the scene.
                var addedComponents = selection.GetComponentsInChildren<Component>(true);
                foreach (var component in addedComponents)
                {
                    UnityObjectAddedBroadcaster.WhenComponentAdded(component);
                }
                EndUndoGroup();
            }
        }

        /// <summary>
        /// Fires signals for Components added to existing GameObjects. Note that this will
        /// NOT signal Components added to the scene through prefab updating, which are added
        /// without triggering the ObjectFactory, upon which this handler relies.
        /// </summary>
        /// <param name="component">The component added to the scene</param>
        private static void HandleComponentWasAdded(Component component)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            StartUndoGroup();
            UnityObjectAddedBroadcaster.WhenComponentAdded(component);
            EndUndoGroup();
        }

        private static void AddInstanceIdsFromSubHierarchyToCache(HashSet<int> cache, params GameObject[] subHierarchyRoots)
        {
            foreach (var gameObject in subHierarchyRoots)
            {
                cache.Add(gameObject.GetInstanceID());
                for (int idx = 0; idx < gameObject.transform.childCount; ++idx)
                {
                    AddInstanceIdsFromSubHierarchyToCache(cache, gameObject.transform.GetChild(idx).gameObject);
                }
            }
        }

        private static void StartUndoGroup()
        {
            if (_objectAddedUndoNestingCounter == 0)
            {
                _objectAddedUndoGroupId = Undo.GetCurrentGroup() - 1;
            }
            _objectAddedUndoNestingCounter++;
        }

        private static void EndUndoGroup()
        {
            _objectAddedUndoNestingCounter--;
            if (_objectAddedUndoNestingCounter == 0)
            {
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(_objectAddedUndoGroupId);
            }
        }
    }
}
