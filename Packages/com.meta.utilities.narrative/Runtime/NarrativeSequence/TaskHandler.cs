// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.Debug;


namespace Meta.Utilities.Narrative
{
    /// <summary>
    /// Handles task logic and waits for all relevant conditions to be satisfied before advancing to the enxt part of the sequence
    /// 
    /// Emits events when the task starts and completes, allowing designers to easily integrate gameplay and story elements to the narrative
    /// </summary>
    [ExecuteAlways]
    public class TaskHandler : MonoBehaviour
    {
        public enum CompletionRequirement { AnyConditionIsMet, AllConditionsAreMet }


        public TaskID TaskID;

        public Transform PlayerTransform;
        public Camera PlayerGazeCamera;

        [SerializeField] private CompletionRequirement m_completeWhen;

        [SerializeReference] public List<TaskCondition> CompletionConditions = new();

        [SerializeField] private UnityEvent m_onTaskStarted, m_onTaskCompleted;
        [SerializeField] private float m_reminderInterval;
        [SerializeField] private UnityEvent m_onReminder;

        private float m_nextReminderTime = Mathf.Infinity;

        [NonSerialized] private string m_registeredWithID;

        public Task Task => TaskManager.GetTask(TaskID);

        public TaskSequence.TaskDefinition TaskDefinition => TaskManager.GetTaskDefinition(TaskID);

        [ContextMenu("Refresh tasks")]
        public void RefreshTasks() => TaskManager.RefreshAll();


        private void OnValidate()
        {
            if (m_reminderInterval < 0f) { m_reminderInterval = 0f; }

            if (CompletionConditions != null)
                foreach (var condition in CompletionConditions) { condition?.OnValidate(this); }
        }

        public bool InitializeIfNecessary()
        {
            var changed = false;

            if (!PlayerTransform)
            {
                PlayerTransform = LocalPlayerTransform.Instance.transform;

                if (PlayerTransform) { changed = true; }
            }

            if (!PlayerGazeCamera)
            {
                PlayerGazeCamera = FindObjectsByType<Camera>(FindObjectsSortMode.None)
                    .FirstOrDefault(c => c.CompareTag("MainCamera")
                                         && c.GetComponent<AudioListener>());

                if (PlayerGazeCamera) { changed = true; }
            }

            return changed;
        }

        private void Awake()
        {
            if (TaskManager.DebugLogs)
            {
                Log($"TaskHandler.Awake() on handler '{name}'; registering with ID '{TaskID}'",
                    this);
            }

            TaskManager.RegisterHandler(this);
            m_registeredWithID = TaskID;
        }

        private void Start()
        {
            foreach (var condition in CompletionConditions) { condition.OnHandlerStart(this); }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (m_registeredWithID != TaskID)
            {
                TaskManager.RegisterHandler(this);
                m_registeredWithID = TaskID;
            }
#endif

            if (Task is not { Current: true })
            {
                if (Application.isPlaying) enabled = false;
                return;
            }

            if (CompletionConditions.Count == 0
                || (m_completeWhen == CompletionRequirement.AllConditionsAreMet
                    ? CompletionConditions.All(condition => condition.IsComplete(this))
                    : CompletionConditions.Any(condition => condition.IsComplete(this))))
            {
                Task.Complete();
                return;
            }

            CheckReminder();
        }

        private void CheckReminder()
        {
            if (!Application.isPlaying || !(Time.time > m_nextReminderTime)) { return; }

            m_onReminder?.Invoke();
            m_nextReminderTime = Time.time + m_reminderInterval;

            if (TaskManager.DebugLogs)
            {
                Log($"Handler '{name}' fired reminder for task '{TaskID}'; "
                    + $"next reminder in {m_reminderInterval} seconds", this);
            }
        }

        // This should only be called from the Task itself
        public void TaskStarted()
        {
            enabled = true;

            if (TaskManager.DebugLogs)
            {
                Log($"TaskHandler.TaskStarted() on handler '{name}'", this);
            }

            foreach (var condition in CompletionConditions) { condition.OnTaskStarted(this); }

            m_onTaskStarted.Invoke();

            if (m_reminderInterval <= 0f) { return; }

            m_nextReminderTime = Time.time + m_reminderInterval;

            if (TaskManager.DebugLogs)
            {
                Log($"Handler '{name}' started '{TaskID}'; "
                    + $"first reminder in {m_reminderInterval} seconds", this);
            }
        }

        // This should only be called from the Task itself
        public void TaskCompleted()
        {
            if (TaskManager.DebugLogs)
            {
                Log($"TaskHandler.TaskCompleted() on handler '{name}'", this);
            }

            m_nextReminderTime = Mathf.Infinity;
            m_onTaskCompleted.Invoke();

            enabled = false;
        }

        private void OnDestroy()
        {
            foreach (var condition in CompletionConditions) { condition.OnHandlerDestroy(this); }

            TaskManager.DeregisterHandler(this);
        }

        [ContextMenu("Skip")]
        public void Skip()
        {
            if (!Application.isPlaying) return;

            foreach (var seq in TaskManager.Instance.Sequences)
            {
                if (seq == Task.Sequence) { break; }

                TaskManager.HandlerForTask(seq.TaskDefinitions[^1].ID).Skip();
            }

            var currTaskIndex = Task.Sequence.TaskDefinitions.FindIndex(def => def.ID == TaskID);

            if (currTaskIndex != 0)
            {

                var lastTask = Task.Sequence.TaskDefinitions[currTaskIndex - 1];
                TaskManager.HandlerForTask(lastTask.ID).Skip();
            }

            foreach (var condition in CompletionConditions) { condition.ForceComplete(this); }

            Task.Complete();
        }

        [ContextMenu("Start Narrative")]
        private void DebugStart()
        {
            if (!Application.isPlaying) { return; }

            TaskManager.StartNarrative();
        }
    }
}