// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRBuilder.Editor.UndoRedo
{
    /// <summary>
    /// Utility class to hook up non-serializeable changes to Unity's `Undo`.
    /// </summary>
    public static class RevertableChangesHandler
    {
        [Serializable]
        private sealed class SerializedRevertableChangesHandler : ScriptableObject
        {
            [SerializeField]
            private int serializedIndex = 0;

            public int CommandIndex { get { return serializedIndex; } set { serializedIndex = value; } }
        }

        private static SerializedRevertableChangesHandler serialized;
        private static readonly Dictionary<string, List<IRevertableCommand>> groupedCommands = new Dictionary<string, List<IRevertableCommand>>();
        private static int commandIndex;

        private static List<IRevertableCommand> commands
        {
            get
            {
                if (groupedCommands.ContainsKey("main") == false)
                {
                    groupedCommands.Add("main", new List<IRevertableCommand>());
                }

                return  groupedCommands["main"];
            }
        }

        static RevertableChangesHandler()
        {
            serialized = ScriptableObject.CreateInstance<SerializedRevertableChangesHandler>();

            UnityEditor.Undo.undoRedoPerformed += HandleUndoRedoPerformed;

            UnityEditor.Undo.ClearAll();
        }

        /// <summary>
        /// Registers <paramref name="revertableCommand"/> and executes it.
        /// </summary>
        /// <remarks>
        /// If <typeparamref name="commandGroup"/> is empty, <paramref name="revertableCommand"/> will be added to the main stack, otherwise,
        /// it will remind in a group called as <typeparamref name="commandGroup"/> content until <seealso cref="CollapseUndoOperations"/> is called.
        /// </remarks>
        public static void Do(IRevertableCommand revertableCommand, string commandGroup = "")
        {
            if (revertableCommand == null)
            {
                Debug.LogError("Command can't be null, ignoring.");
                return;
            }

            if (string.IsNullOrEmpty(commandGroup))
            {
                RegisterCommand(revertableCommand);
            }
            else
            {
                RegisterCommandInGroup(revertableCommand, commandGroup);
            }

            DoCommand(revertableCommand);
        }

        /// <summary>
        /// Collapses all undo operation up to group index together into the main stack.
        /// </summary>
        public static void CollapseUndoOperations(string groupName)
        {
            if (string.IsNullOrEmpty(groupName) || groupedCommands.ContainsKey(groupName) == false)
            {
                return;
            }

            if (groupedCommands[groupName].Count > 0)
            {
                List<IRevertableCommand> undoCommands = groupedCommands[groupName];
                List<IRevertableCommand> redoCommands = undoCommands;

                RegisterCommand(new ProcessCommand(
                    ()=>
                    {
                        foreach (IRevertableCommand command in redoCommands)
                        {
                            command.Do();
                        }
                    }, ()=>
                    {
                        undoCommands.Reverse();

                        foreach (IRevertableCommand command in undoCommands)
                        {
                            command.Undo();
                        }
                    }));
            }

            groupedCommands.Remove(groupName);
        }

        /// <summary>
        /// Clear Unity's `Undo` stack and reset the command handler.
        /// </summary>
        public static void FlushStack()
        {
            UnityEditor.Undo.ClearAll();
            commandIndex = 0;
            serialized = ScriptableObject.CreateInstance<SerializedRevertableChangesHandler>();
            groupedCommands.Clear();
            commands.Clear();
        }

        private static void HandleUndoRedoPerformed()
        {
            // Another object is undone/redone, ignore.
            if (serialized.CommandIndex == commandIndex)
            {
                return;
            }

            if (serialized.CommandIndex > commandIndex)
            {
                // Redo.
                if (DoCommand(commands[commandIndex]))
                {
                    commandIndex++;
                }
            }
            else
            {
                // Undo.
                if (UndoCommand(commands[serialized.CommandIndex]))
                {
                    commandIndex--;
                }
            }
        }

        private static void RegisterCommand(IRevertableCommand revertableCommand)
        {
            if (serialized == null)
            {
                FlushStack();
            }

            UnityEditor.Undo.IncrementCurrentGroup();
            UnityEditor.Undo.RecordObject(serialized, "");

            // Flush outdated.
            if (commands.Count > commandIndex)
            {
                commands.RemoveRange(commandIndex, commands.Count - commandIndex);
            }

            commands.Add(revertableCommand);
            commandIndex++;
            serialized.CommandIndex = commandIndex;
        }

        private static void RegisterCommandInGroup(IRevertableCommand revertableCommand, string commandGroup)
        {
            if (groupedCommands.ContainsKey(commandGroup) == false)
            {
                groupedCommands.Add(commandGroup, new List<IRevertableCommand>());
            }

            groupedCommands[commandGroup].Add(revertableCommand);
        }

        private static bool DoCommand(IRevertableCommand revertableCommand)
        {
            try
            {
                revertableCommand.Do();
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Can't do the command.\n{0}", e.Message);
                Panic();
                return false;
            }

            return true;
        }

        private static bool UndoCommand(IRevertableCommand revertableCommand)
        {
            try
            {
                revertableCommand.Undo();
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Can't undo the command.\n{0}", e);
                Panic();
                return false;
            }

            return true;
        }

        private static void Panic()
        {
            Debug.LogError("Flushing the Undo/Redo stack to prevent data corruption...");
            FlushStack();
        }
    }
}
