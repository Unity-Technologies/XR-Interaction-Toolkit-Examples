// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Meta.Utilities.Narrative
{
    /// <summary>
    /// The basic unit of the narrative system is a task sequence, which consists of a number of task definitions
    /// 
    /// Tasks are able to trigger events as well as wait for specific conditions, allowing it to drive complex
    /// narrative and game logic
    /// </summary>
    [CreateAssetMenu(menuName = "Data/Narrative Sequencing/Task Sequence",
                     fileName = "New Task Sequence")]
    public class TaskSequence : ScriptableObject
    {
        [Serializable]
        public class TaskDefinition
        {
            public string ID;
            public TaskSequence Sequence;

            [SerializeField] private List<TaskID> m_startingPrerequisites;
            [SerializeField] private List<TaskID> m_completionPrerequisites;

            public IReadOnlyCollection<TaskID> StartingPrerequisites
                => m_startingPrerequisites.AsReadOnly();

            public IReadOnlyCollection<TaskID> CompletionPrerequisites
                => m_completionPrerequisites.AsReadOnly();
        }


        public List<TaskDefinition> TaskDefinitions;
        public TaskSequence NextSequence;

        [HideInInspector] public int Index;


        // Playmode-only data

        [NonSerialized] public List<Task> TasksOrdered;

        [NonSerialized] private bool m_playModeInitialised;

        public List<Task> Current => TasksOrdered.Where(task => task.Current).ToList();


        public override string ToString() => $"[Sequence #{Index}] {name}";


        private void OnValidate()
        {
            string prev = null;
            var changesMade = false;

            for (var i = 0; i < TaskDefinitions.Count; i++)
            {
                if (i > 0 && prev == TaskDefinitions[i].ID)
                {
                    changesMade = true;

                    // ensure ID is unique within this sequence. this will work even if
                    // the sequence isn't part of the task manager's sequence list
                    TaskDefinitions[i].ID = GetNextAvailableIDInSequence(TaskDefinitions[i].ID);

                    // ensure ID is unique across all sequences in the task manager
                    if (TaskManager.IDSetInitialised)
                    {
                        TaskDefinitions[i].ID
                            = TaskManager.GetNextAvailableID(TaskDefinitions[i].ID);
                    }
                }

                TaskDefinitions[i].Sequence = this;

                prev = TaskDefinitions[i].ID;
            }

            if (changesMade) { TaskManager.RefreshIDSets(); }
        }


        private string GetNextAvailableIDInSequence(string currentID)
        {
            if (TaskDefinitions.All(def => def.ID != currentID)) { return currentID; }

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

            do { num++; } while (TaskDefinitions.Any(def => def.ID == $"{currentID}{num}"));

            return $"{currentID}{num}";
        }


        public void Initialise()
        {
            if (Application.isPlaying)
            {
                if (m_playModeInitialised) { return; }

                m_playModeInitialised = true;

                ResetTasks();
            }
        }

        public void ResetTasks()
        {
            TasksOrdered = TaskDefinitions.Select((t, i) => new Task(t, i, this)).ToList();
        }

        public bool CanStart => TasksOrdered?.Count > 0 && TasksOrdered[0].CanStart;


        public void Start()
        {
            if (TasksOrdered == null)
            {
                Debug.LogWarning($"Tried to start task sequence '{name}' but it's uninitialised!",
                                 this);

                return;
            }

            if (TasksOrdered.Count == 0)
            {
                Debug.LogWarning($"Tried to start task sequence '{name}' "
                                 + "but it has no tasks defined!", this);

                return;
            }

            var startableTasks = TasksOrdered.Where(task => task.CanStart).ToList();

            if (startableTasks.Count == 0)
            {
                Debug.LogWarning($"Tried to start task sequence '{name}' "
                                 + "but it has no tasks that can be started!", this);

                return;
            }

            foreach (var task in startableTasks) { task.Start(); }

            Debug.Log($"[Narrative sequence] Started task sequence '{name}' "
                      + $"with {startableTasks.Count} initial task(s): "
                      + string.Join(", ", startableTasks.Select(task => task.LocalIdent)));
        }


        public void TaskCompleted(Task completedTask)
        {
            var startableTasks = TasksOrdered.Where(task => task.CanStart).ToList();

            // if there are new tasks to be started, start them
            if (startableTasks.Count != 0)
            {
                Debug.Log($"[Narrative sequence] Task '{completedTask.ID}' completed; "
                          + $"sequence '{name}' now starting task(s): "
                          + string.Join(", ", startableTasks.Select(task => task.LocalIdent)));

                foreach (var task in startableTasks) { task.Start(); }

                return;
            }

            // handle cases where no new task can be started

            // there are still tasks in progress
            if (TasksOrdered.Any(task => task.Current))
            {
                Debug.Log($"[Narrative sequence] Task '{completedTask.ID}' completed; "
                          + $"sequence '{name}' still has tasks in progress...");
            }
            // there are no tasks in progress, and there are still incomplete ones.
            // report the issue, but complete the sequence to prevent a softlock 
            else if (TasksOrdered.Any(task => task.State == TaskState.Inactive))
            {
                Debug.LogWarning($"[Narrative sequence] Task '{completedTask.ID}' completed; "
                                 + $"sequence '{name}' still has incomplete tasks but none "
                                 + "are able to be started - check task sequence setup! "
                                 + "Completing sequence as failsafe.");

                TaskManager.SequenceCompleted(this);
            }
            // all tasks are now complete
            else
            {
                Debug.Log($"[Narrative sequence] Task '{completedTask.ID}' completed; "
                          + $"all tasks in sequence '{name}' are complete!");

                TaskManager.SequenceCompleted(this);
            }
        }


        [ContextMenu("Refresh tasks")]
        public void RefreshTasks() => TaskManager.RefreshAll();


        [ContextMenu("Start narrative from here")]
        public void StartNarrativeFromHere()
        {
            if (!Application.isPlaying) { return; }
            TaskManager.StartNarrative(this);
        }
    }
}