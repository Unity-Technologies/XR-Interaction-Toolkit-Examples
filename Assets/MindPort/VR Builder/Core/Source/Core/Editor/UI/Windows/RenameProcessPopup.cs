// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core;
using VRBuilder.Editor.UndoRedo;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Windows
{
    /// <summary>
    /// Handles changing the process name.
    /// </summary>
    internal class RenameProcessPopup : EditorWindow
    {
        private static RenameProcessPopup instance;

        private readonly GUID textFieldIdentifier = new GUID();

        private IProcess process;
        private string newName;

        private bool isFocusSet;
        private EditorWindow parent;

        public bool IsClosed { get; private set; }

        public static RenameProcessPopup Open(IProcess process, Rect labelPosition, Vector2 offset, EditorWindow parent)
        {
            if (instance != null)
            {
                instance.Close();
            }

            instance = CreateInstance<RenameProcessPopup>();

            instance.process = process;
            instance.newName = instance.process.Data.Name;
            instance.parent = parent;
            
            instance.position = new Rect(labelPosition.x - offset.x, labelPosition.y - offset.y, labelPosition.width, labelPosition.height);
            instance.ShowPopup();
            instance.Focus();

            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                instance.Close();
                instance.IsClosed = true;
            };

            return instance;
        }

        private void OnGUI()
        {
            if (process == null || focusedWindow != this)
            {
                Close();
                instance.IsClosed = true;
            }

            GUI.SetNextControlName(textFieldIdentifier.ToString());
            newName = EditorGUILayout.TextField(newName);
            newName = newName.Trim();

            if (isFocusSet == false)
            {
                isFocusSet = true;
                EditorGUI.FocusTextInControl(textFieldIdentifier.ToString());
            }

            if ((Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                if (ProcessAssetUtils.CanRename(process, newName, out string error) == false)
                {
                    if (string.IsNullOrEmpty(error) == false && string.IsNullOrEmpty(error) == false)
                    {
                        TestableEditorElements.DisplayDialog("Cannot rename the process", error, "OK");
                    }
                }
                else
                {
                    string oldName = process.Data.Name;

                    RevertableChangesHandler.Do(new ProcessCommand(
                        () =>
                        {
                            if (ProcessAssetUtils.CanRename(process, newName, out string errorMessage) == false)
                            {
                                if (string.IsNullOrEmpty(errorMessage) == false)
                                {
                                    TestableEditorElements.DisplayDialog("Cannot rename the process", errorMessage, "OK");
                                }

                                RevertableChangesHandler.FlushStack();
                            }
                            else
                            {
                                ProcessAssetManager.RenameProcess(process, newName);
                            }
                        },
                        () =>
                        {
                            if (ProcessAssetUtils.CanRename(process, newName, out string errorMessage) == false)
                            {
                                if (string.IsNullOrEmpty(errorMessage) == false)
                                {
                                    TestableEditorElements.DisplayDialog("Cannot rename the process", errorMessage, "OK");
                                }

                                RevertableChangesHandler.FlushStack();
                            }
                            else
                            {
                                ProcessAssetManager.RenameProcess(process, oldName);
                            }
                        }
                    ));
                }

                Close();
                parent.Focus();
                instance.IsClosed = true;
                //Event.current.Use();
            }
            else if (Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                instance.IsClosed = true;
                //Event.current.Use();
            }
        }
    }
}
