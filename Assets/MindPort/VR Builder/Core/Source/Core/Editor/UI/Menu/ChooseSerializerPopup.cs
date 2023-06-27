// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core.Serialization;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.BuilderMenu
{
    /// <summary>
    /// Allows user to select which serializer they want to use.
    /// </summary>
    internal class ChooseSerializerPopup : EditorWindow
    {
        private static ChooseSerializerPopup instance;

        private List<IProcessSerializer> serializer;
        private string[] names;

        private int selected = 0;
        private Action<IProcessSerializer> closeAction;

        /// <summary>
        /// Show the popup.
        /// </summary>
        /// <param name="serializer">Selectable serializer</param>
        /// <param name="closeAction">Action which will be invoked when closed successfully.</param>
        public static void Show(List<IProcessSerializer> serializer, Action<IProcessSerializer> closeAction)
        {
            if (instance != null)
            {
                instance.Close();
            }

            instance = CreateInstance<ChooseSerializerPopup>();
            instance.serializer = serializer;
            instance.closeAction = closeAction;

            Rect position = new Rect(0, 0, 320, 92);
            position.center = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height).center;
            instance.position = position;

            instance.ShowPopup();
            instance.Focus();
        }

        private void OnGUI()
        {
            if (focusedWindow != this)
            {
                Close();
                instance = null;
            }

            if (names == null)
            {
                names = serializer.Select(t => t.Name).ToArray();
            }

            EditorGUILayout.LabelField("Select the serializer you want to use:");
            selected = EditorGUILayout.Popup("", selected, names);

            if (GUILayout.Button("Import"))
            {
                closeAction.Invoke(serializer[selected]);
                Close();
            }

            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
        }
    }
}
