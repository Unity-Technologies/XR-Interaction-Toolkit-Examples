// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Linq;
using UnityEngine;


namespace Meta.Utilities.Narrative
{
    public enum TaskState { Inactive, Current, Complete }

    /// <summary>
    /// Holds the state of a single active task
    /// </summary>
    public class Task
    {
        public readonly string ID;
        public readonly TaskSequence Sequence;
        public readonly int Index;
        public readonly TaskSequence.TaskDefinition Definition;

        public TaskState State = TaskState.Inactive;

        public bool Current => State == TaskState.Current;
        public bool Completed => State == TaskState.Complete;

        public override string ToString() => GlobalIdent;


        public string GlobalIdent => $"{Sequence} / {LocalIdent}";

        public string LocalIdent => $"[Task #{Index}] {ID}";

        public TaskHandler Handler => TaskManager.HandlerForTask(ID);

        public Task(TaskSequence.TaskDefinition definition, int index, TaskSequence sequence)
        {
            Definition = definition;
            Index = index;
            Sequence = sequence;
            ID = definition.ID;

            if (TaskManager.DebugLogs)
            {
                Debug.Log($"Constructed task '{ID}'");
            }
        }

        public bool CanStart => State == TaskState.Inactive
                                && Definition.StartingPrerequisites.All
                                    (id => TaskManager.GetTask(id)?.Completed ?? true);

        public void Start()
        {
            if (State != TaskState.Inactive)
            {
                Debug.LogWarning($"Tried to start task {ID} while its state is already {State}");
                return;
            }

            foreach (var id in Definition.StartingPrerequisites)
            {
                var task = TaskManager.GetTask(id);

                if (task == null || task.Completed) { continue; }

                Debug.LogWarning($"Tried to start task {ID} but prerequisite task {id} "
                                 + $"is not complete (state is {task.State})!");
                return;
            }

            State = TaskState.Current;

            if (Handler)
            {
                if (TaskManager.DebugLogs)
                {
                    Debug.Log($"Task '{ID}' starting; calling TaskStarted() "
                              + $"on handler '{Handler.name}'", Handler);
                }

                Handler.TaskStarted();
            }
            else if (TaskManager.DebugLogs)
            {
                Debug.Log($"Task '{ID}' starting with no handler", Handler);
            }
        }

        public void Complete()
        {
            if (State == TaskState.Complete)
            {
                Debug.LogWarning($"Tried to complete task {ID} which is already completed!");
                return;
            }

            foreach (var id in Definition.CompletionPrerequisites)
            {
                var task = TaskManager.GetTask(id);

                if (task == null || task.Completed) { continue; }

                Debug.LogWarning($"Tried to complete task {ID} but prerequisite task {id} "
                                 + $"is not complete (state is {task.State})!");
                return;
            }

            State = TaskState.Complete;

            if (Handler) { Handler.TaskCompleted(); }

            Sequence.TaskCompleted(this);
        }
    }
}