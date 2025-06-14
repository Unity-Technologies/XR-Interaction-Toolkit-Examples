// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.Utilities.Narrative
{
    /// <summary>
    /// Manages the narrative task system, including task definitions and task ids (ensuring uniqueness and valid references)
    /// </summary>
    [CreateAssetMenu(menuName = "Data/Narrative Sequencing/Task Manager", fileName = "Task Manager")]
    public class TaskManager : ScriptableObject
    {
        #region Singleton and static initialization

#if UNITY_EDITOR
        static TaskManager()
        {
            SceneManager.sceneLoaded += OnSceneLoad;

#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpen;
#endif

            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // make sure everything is reset correctly when entering or exiting play mode
            // regardless of whether the domain is set to reload when exiting play mode
            static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change)
            {
                switch (change)
                {
                    case UnityEditor.PlayModeStateChange.EnteredEditMode: break;
                    case UnityEditor.PlayModeStateChange.ExitingEditMode: Uninitialize(); break;
                    case UnityEditor.PlayModeStateChange.EnteredPlayMode: break;
                    case UnityEditor.PlayModeStateChange.ExitingPlayMode: Uninitialize(); break;
                    default: throw new ArgumentOutOfRangeException(nameof(change), change, null);
                }
            }
        }
#endif

        private const string RESOURCE_PATH = "NarrativeSequence/Task Manager";

        private static bool s_shownInstanceLoadError;

        private static TaskManager s_instance;
        public static TaskManager Instance
        {
            get
            {
                if (s_instance) { return s_instance; }
                var asset = Resources.Load<TaskManager>(RESOURCE_PATH);

                if (asset)
                {
                    s_instance = asset;
#if UNITY_EDITOR
                    if (!s_instance.m_isDefaultInstance)
                    {
                        asset.m_isDefaultInstance = true;
                        UnityEditor.EditorUtility.SetDirty(s_instance);
                    }
#endif
                    return s_instance;
                }
                if (s_shownInstanceLoadError) { return null; }
                Debug.LogError("Could not load Task Manager from resources! "
                               + "Please ensure a TaskManager asset is located at "
                               + $"/Assets[/...]/Resources/{RESOURCE_PATH}.asset");
                s_shownInstanceLoadError = true;
                return null;
            }
        }

        private static void OnSceneLoad(Scene scene, LoadSceneMode mode) => RefreshAll();

#if UNITY_EDITOR
        private static void OnSceneOpen(Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
            => RefreshAll();
#endif

        public static void Uninitialize()
        {
            if (DebugLogs) { Debug.Log("Task Manager uninitialising"); }

            s_handlersByID.Clear();
            s_idsByHandler.Clear();

            s_tasksByID = null;
            s_taskDefinitionsByID = null;
            s_taskIDDisplayPopupOptions = null;
            s_taskIDRawPopupOptions = null;
            s_idSet = null;
            s_instance = null;

            s_shownInstanceLoadError = false;
        }

        #endregion

        public static bool DebugLogs;

        public bool DebugLogging;
        public bool Loop = true;
        public List<TaskSequence> Sequences;

        [SerializeField, HideInInspector] private bool m_isDefaultInstance;

        private static readonly Dictionary<TaskID, TaskHandler> s_handlersByID = new();
        private static readonly Dictionary<TaskHandler, TaskID> s_idsByHandler = new();

        private static HashSet<string> s_idSet;
        private static Dictionary<TaskID, Task> s_tasksByID;
        private static Dictionary<TaskID, TaskSequence.TaskDefinition> s_taskDefinitionsByID;

        public static bool RefreshingIDSets { get; private set; }

        private static string[] s_taskIDDisplayPopupOptions;
        public static string[] TaskIDDisplayPopupOptions
        {
            get
            {
                if (s_taskIDDisplayPopupOptions == null) { RefreshIDSets(); }
                return s_taskIDDisplayPopupOptions;
            }
        }

        private static List<string> s_taskIDRawPopupOptions;
        private static List<string> TaskIDRawPopupOptions
        {
            get
            {
                if (s_taskIDRawPopupOptions == null) { RefreshIDSets(); }
                return s_taskIDRawPopupOptions;
            }
        }

        public static bool IDSetInitialised => s_idSet != null;

        public static List<Task> CurrentTasks
            => s_tasksByID?.Values.Where(task => task.Current).ToList();

        private void OnValidate()
        {
            for (var i = 0; i < Sequences.Count - 1; i++)
            {
                Sequences[i].NextSequence = Sequences[i + 1];
            }

            if (s_instance && s_instance != this) { m_isDefaultInstance = false; }

            DebugLogs = DebugLogging;
        }

        public bool IsFirstTask(TaskID taskID)
            => Sequences.Count > 0 && Sequences[0]?.TaskDefinitions.Count > 0
                                   && Sequences[0].TaskDefinitions[0]?.ID == taskID;

        public static Task GetTask(TaskID id)
        {
#if VERBOSE_LOGGING
            if (!Application.isPlaying) { return null; }
#endif
            if (s_tasksByID == null) { RefreshIDSets(); }

            return s_tasksByID.TryGetValue(id, out var result) ? result : null;
        }

        public static TaskSequence.TaskDefinition GetTaskDefinition(TaskID id)
        {
            if (s_taskDefinitionsByID == null) { RefreshIDSets(); }

            return s_taskDefinitionsByID.GetValueOrDefault(id);
        }

        #region Narrative flow

        public static void StartNarrativeFromTaskID(TaskID? startFrom)
        {
            if (startFrom.HasValue && GetTaskDefinition(startFrom.Value) is { } taskDefinition && taskDefinition.Sequence is { } sequence)
            {
                StartNarrative(sequence);
            }
            else
            {
                StartNarrative(null);
            }
        }

        public static void StartNarrative(TaskSequence startFrom = null)
        {
            if (!Instance || !Instance.Sequences.Any())
            {
#if VERBOSE_LOGGING
                Debug.LogError("Can't start narrative sequence "
                               + "because no task sequences are defined");
#endif

                return;
            }

            // if no sequence passed, default to first
            startFrom ??= Instance.Sequences.FirstOrDefault();

            if (startFrom)
            {
                var found = false;
                foreach (var sequence in Instance.Sequences)
                {
                    // since it's possible we're restarting the game, we need to reset the tasks on every sequence
                    sequence.ResetTasks();

                    // since it's possible we're starting from a sequence other than the first,
                    // we need to mark previous tasks as completed (if applicable)
                    if (sequence == startFrom)
                        found = true;
                    else if (!found)
                        sequence.TasksOrdered.ForEach(t => t.State = TaskState.Complete);
                }

                RefreshAll();

                // now we can start the requested sequence
                startFrom.Start();
            }
        }

        public static void SequenceCompleted(TaskSequence sequence)
        {
            if (Instance && Instance.Loop && Instance.Sequences.Last() == sequence)
            {
                Debug.Log($"Task sequence '{sequence.name}' complete; restarting narrative...");
                StartNarrative();
                return;
            }

            // TODO: check if it can start yet; log error if not
            if (sequence.NextSequence)
            {
#if VERBOSE_LOGGING
                Debug.Log($"Task sequence '{sequence.name}' complete; starting next...");
#endif
                sequence.NextSequence.Start();
            }
            else
            {
                Debug.Log("Narrative is complete!");
            }
        }

        #endregion

        #region Task Handlers

        public static TaskHandler HandlerForTask(TaskID taskID)
        {
            if (s_handlersByID.Count == 0) { RefreshIDSets(); }

            return s_handlersByID.GetValueOrDefault(taskID);
        }

        public static void RegisterHandler(TaskHandler handler)
        {
            if (!handler) { return; }

            if (s_idsByHandler.ContainsKey(handler))
            {
                if (s_idsByHandler[handler] == handler.TaskID) { return; }

                _ = s_handlersByID.Remove(s_idsByHandler[handler]);
                _ = s_idsByHandler.Remove(handler);
            }

            if (string.IsNullOrWhiteSpace(handler.TaskID)
                || !s_handlersByID.TryAdd(handler.TaskID, handler))
            {
                return;
            }

            if (DebugLogs)
            {
                Debug.Log($"TaskManager registered handler '{handler.name}' "
                          + $"for task '{handler.TaskID}', handler");
            }

            s_idsByHandler[handler] = handler.TaskID;
        }

        public static void DeregisterHandler(TaskHandler handler)
        {
            if (!handler || !s_idsByHandler.TryGetValue(handler, out var value)) { return; }

            _ = s_handlersByID.Remove(value);
            _ = s_idsByHandler.Remove(handler);

            if (DebugLogs)
            {
                Debug.Log($"TaskManager deregistered handler '{handler.name}' "
                          + $"for task '{handler.TaskID}', handler");
            }
        }

        #endregion

        #region Task IDs

        public static int GetPopupIndexForID(string id)
            => string.IsNullOrWhiteSpace(id) ? 0 : TaskIDRawPopupOptions.IndexOf(id);

        public static string GetIDFromPopupIndex(int index)
            => index <= 0 ? null : TaskIDRawPopupOptions[index];


        /// <summary>
        /// Get a guaranteed unique task ID based on the one provided. 
        /// </summary>
        /// <param name="currentID">Current/desired ID.</param>
        /// <returns>The current ID if it is not yet defined, otherwise the same ID
        /// with an incremented numerical suffix that is not yet used.</returns>
        public static string GetNextAvailableID(string currentID)
        {
            if (!IDIsDefined(currentID)) { return currentID; }

            int suffixChars;

            for (suffixChars = 0; suffixChars < currentID.Length; suffixChars++)
            {
                if (!char.IsDigit(currentID[^(suffixChars + 1)])) { break; }
            }

            var num = 0;

            if (suffixChars > 0)
            {
                num = int.Parse(currentID[^suffixChars..]);
                currentID = currentID[..^suffixChars];
            }

            do { num++; } while (IDIsDefined($"{currentID}{num}"));

            return $"{currentID}{num}";
        }

        public static bool IDIsDefined(string id)
        {
            if (s_idSet == null) { RefreshIDSets(); }

            return s_idSet!.Contains(id);
        }


        public static void RefreshIDSets()
        {
            // concurrent refresh calls can happen in OnValidate of sequences
            // when the TaskManager instance is loaded.
            // since ID conflicts are unlikely to occur outside of manually editing task sequences,
            // which happens on the main thread, we can skip these extra refreshes 
            if (RefreshingIDSets)
            {
                if (DebugLogs) { Debug.LogWarning("TaskManager already refreshing; skipping..."); }

                return;
            }

            if (DebugLogs) { Debug.Log("Task Manager refreshing task ID sets"); }

            RefreshingIDSets = true;

            var idPopupOptionsList = new List<string> { "No ID" };

            var tempIDSet = new HashSet<string>();
            var tempTasksByID = new Dictionary<TaskID, Task>();
            var tempTaskDefinitionsByID = new Dictionary<TaskID, TaskSequence.TaskDefinition>();

            if (!Instance)
            {
                RefreshingIDSets = false;
                return;
            }

            for (var index = 0; index < Instance.Sequences.Count; index++)
            {
                var sequence = Instance.Sequences[index];

                if (!sequence) { continue; }

                sequence.Index = index;

                if (index < Instance.Sequences.Count - 1)
                {
                    sequence.NextSequence = Instance.Sequences[index + 1];
                }

                sequence.Initialise();

                for (var i = 0; i < sequence.TaskDefinitions.Count; i++)
                {
                    var taskDef = sequence.TaskDefinitions[i];

                    if (!tempIDSet.Add(taskDef.ID))
                    {
                        Debug.LogError(
                            $"Duplicate task ID '{taskDef.ID}' defined in {sequence}",
                            sequence);

                        continue;
                    }

                    idPopupOptionsList.Add($"{sequence.name}/{taskDef.ID}");
                    tempTaskDefinitionsByID[taskDef.ID] = taskDef;

                    if (Application.isPlaying)
                    {
                        tempTasksByID[taskDef.ID] = sequence.TasksOrdered[i];
                    }

                }
            }

            s_idSet = tempIDSet;
            s_tasksByID = tempTasksByID;
            s_taskDefinitionsByID = tempTaskDefinitionsByID;

            s_taskIDDisplayPopupOptions = idPopupOptionsList.ToArray();

            s_taskIDRawPopupOptions = idPopupOptionsList
                                      .Select(path => path[(path.IndexOf('/') + 1)..]).ToList();

            RefreshingIDSets = false;
        }

        #endregion

        public static void RefreshAll()
        {
            if (DebugLogs) { Debug.Log("Task Manager refreshing all tasks and task handlers"); }

            s_handlersByID.Clear();
            s_idsByHandler.Clear();

            var handlers = FindObjectsByType<TaskHandler>(FindObjectsInactive.Include,
                                                          FindObjectsSortMode.None);

            foreach (var handler in handlers)
            {
                s_handlersByID[handler.TaskID] = handler;
                s_idsByHandler[handler] = handler.TaskID;
            }

            RefreshIDSets();
        }

        #region Context menu

        [ContextMenu("Refresh")]
        public void RefreshTasks() => RefreshAll();

        [ContextMenu("Start Narrative")]
        private void DebugStart()
        {
            if (!Application.isPlaying) { return; }

            StartNarrative();
        }

        #endregion
    }
}