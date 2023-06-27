// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core;
using VRBuilder.Editor.UndoRedo;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Windows
{
    internal class ChangeNamePopup : EditorWindow
    {
        private static ChangeNamePopup instance;

        private readonly GUID textFieldIdentifier = new GUID();

        private IRenameableData nameable;
        private string newName;

        private bool isFocusSet;
        private EditorWindow parent;

        public bool IsClosed { get; protected set; }

        public static ChangeNamePopup Open(IRenameableData renameable, Rect labelPosition, Vector2 offset, EditorWindow parent)
        {
            if (instance != null)
            {
                instance.Close();
            }

            instance = CreateInstance<ChangeNamePopup>();

            instance.nameable = renameable;
            instance.newName = renameable.Name;
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
            if (nameable == null || focusedWindow != this)
            {
                Close();
                instance.IsClosed = true;
            }

            GUI.SetNextControlName(textFieldIdentifier.ToString());
            newName = EditorGUILayout.TextField(newName);

            if (isFocusSet == false)
            {
                isFocusSet = true;
                EditorGUI.FocusTextInControl(textFieldIdentifier.ToString());
            }

            if (focusedWindow != this)
            {
                return;
            }

            if (Event.current.isKey == false)
            {
                return;
            }

            if ((Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                if (string.IsNullOrEmpty(newName))
                {
                    return;
                }

                string oldName = nameable.Name;
                RevertableChangesHandler.Do(new ProcessCommand(
                    // ReSharper disable once ImplicitlyCapturedClosure
                    () =>
                    {
                        nameable.SetName(newName);
                    },
                    // ReSharper disable once ImplicitlyCapturedClosure
                    () =>
                    {
                        nameable.SetName(oldName);
                    }
                ));
                Close();
                parent.Focus();
                instance.IsClosed = true;
                Event.current.Use();
            }
            else if (Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                instance.IsClosed = true;
                Event.current.Use();
            }
        }
    }
}
