// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.TestTools
{
    /// <summary>
    /// Utility window which draws itself on top of a given window and intercepts the events.
    /// </summary>
    internal class EditorWindowTestRecorder : EditorWindow
    {
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new ImguiEventConverter()
            }
        };

        private readonly IList<UserAction> userActions = new List<UserAction>();
        private IEditorImguiTest test;
        private EditorWindow recordedWindow;

        /// <summary>
        /// True if this <see cref="EditorWindowTestRecorder"/> is currently recording a <see cref="IEditorImguiTest"/>.
        /// </summary>
        public static bool IsRecording { get; private set; }

        /// <summary>
        /// Starts recording given <paramref name="test"/>.
        /// </summary>
        public void StartRecording(IEditorImguiTest test)
        {
            IsRecording = true;

            recordedWindow = test.BaseGiven();
            this.test = test;

            userActions.Clear();

            TestableEditorElements.StartRecording();
        }

        private void OnDestroy()
        {
            if (IsRecording)
            {
                TestableEditorElements.Panic();
                if (recordedWindow != null)
                {
                    recordedWindow.Close();
                }
            }
        }

        private void Abort()
        {
            IsRecording = false;
            TestableEditorElements.Panic();
            if (recordedWindow != null)
            {
                recordedWindow.Close();
            }

            Close();
        }

        private void SaveAndTerminate()
        {
            JsonSerializer serializer = JsonSerializer.Create(serializerSettings);

            List<string> lastPrepickedSelections = TestableEditorElements.StopRecording();

            if (userActions.Any())
            {
                userActions.Last().PrepickedSelections = lastPrepickedSelections;
            }

            string serialized = JArray.FromObject(userActions, serializer).ToString();
            Directory.CreateDirectory(Path.GetDirectoryName(test.PathToRecordedActions));

            StreamWriter file = null;
            try
            {
                file = File.CreateText(test.PathToRecordedActions);
                file.Write(serialized);
            }
            finally
            {
                IsRecording = false;

                file?.Close();

                if (recordedWindow != null)
                {
                    recordedWindow.Close();
                }

                AssetDatabase.ImportAsset(test.PathToRecordedActions);

                Close();
            }
        }

        private bool ShouldRecordEvent()
        {
            if (Event.current.type == EventType.Layout)
            {
                return false;
            }

            if (Event.current.type == EventType.Repaint)
            {
                return false;
            }

            if (Event.current.isMouse && recordedWindow.position.Contains(Event.current.mousePosition + recordedWindow.position.position) == false)
            {
                return false;
            }

            return true;
        }

        private void OnGUI()
        {
            try
            {
                if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "Abort")
                {
                    Abort();
                    return;
                }

                if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "SaveAndTerminate")
                {
                    SaveAndTerminate();
                    return;
                }

                Rect newPos = recordedWindow.position;

                minSize = newPos.size;
                maxSize = newPos.size;
                position = newPos;

                MethodInfo onGui = recordedWindow.GetType().GetMethod("OnGUI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (Event.current.type != EventType.Used)
                {
                    if (ShouldRecordEvent())
                    {
                        if (userActions.Any())
                        {
                            userActions.Last().PrepickedSelections = TestableEditorElements.StopRecording();
                            TestableEditorElements.StartRecording();
                        }

                        Event toRecord = JsonConvert.DeserializeObject<Event>(JsonConvert.SerializeObject(Event.current, serializerSettings), serializerSettings);

                        UserAction userActionToRecord = new UserAction { Event = toRecord };

                        if (ShouldReplaceLastUserAction())
                        {
                            userActions[userActions.Count - 1] = userActionToRecord;
                        }
                        else
                        {
                            userActions.Add(userActionToRecord);
                        }
                    }

                    onGui.Invoke(recordedWindow, new object[0]);
                }

                Focus();
            }
            catch
            {
                IsRecording = false;
                TestableEditorElements.Panic();
                if (recordedWindow != null)
                {
                    recordedWindow.Close();
                }

                Close();
                throw;
            }
        }

        private bool ShouldReplaceLastUserAction()
        {
            if (userActions.Any() == false)
            {
                return false;
            }

            if (userActions.Last().Event.type != EventType.KeyDown)
            {
                return false;
            }

            if (Event.current.type != EventType.ValidateCommand && Event.current.type != EventType.ExecuteCommand)
            {
                return false;
            }

            Event lastEvent = userActions.Last().Event;

            if (lastEvent.control || lastEvent.command)
            {
                switch (Event.current.commandName)
                {
                    case "Paste" when lastEvent.keyCode == KeyCode.V:
                    case "Copy" when lastEvent.keyCode == KeyCode.C:
                    case "Cut" when lastEvent.keyCode == KeyCode.X:
                    case "Duplicate" when lastEvent.keyCode == KeyCode.D:
                    case "Find" when lastEvent.keyCode == KeyCode.F:
                    case "SelectAll" when lastEvent.keyCode == KeyCode.A:
                        return true;
                    default:
                        return false;
                }
            }

            switch (Event.current.commandName)
            {
                case "Delete" when lastEvent.keyCode == KeyCode.Delete:
                case "SoftDelete" when lastEvent.keyCode == KeyCode.Delete:
                    return true;
                default:
                    return false;
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
