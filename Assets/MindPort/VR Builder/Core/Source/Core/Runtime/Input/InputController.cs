// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using VRBuilder.Unity;

namespace VRBuilder.Core.Input
{
    /// <summary>
    /// Central controller for input via the new Input System using C# events.
    /// </summary>
    public abstract class InputController : UnitySceneSingleton<InputController>
    {
        public class InputEventArgs : EventArgs
        {
            public readonly object Context;

            public InputEventArgs(object context)
            {
                Context = context;
            }
        }

        public class InputFocusEventArgs : EventArgs
        {
            public readonly IInputFocus InputFocus;

            public InputFocusEventArgs(IInputFocus inputFocus)
            {
                InputFocus = inputFocus;
            }
        }

        /// <summary>
        /// Information of the listener registered.
        /// </summary>
        protected struct ListenerInfo
        {
            public readonly IInputActionListener ActionListener;
            public readonly Action<InputEventArgs> Action;

            public ListenerInfo(IInputActionListener actionListener, Action<InputEventArgs> action)
            {
                ActionListener = actionListener;
                Action = action;
            }
        }

        /// <summary>
        /// Will be called when an object is focused.
        /// </summary>
        public EventHandler<InputFocusEventArgs> OnFocused;

        /// <summary>
        /// Will be called when the focus on an object is released.
        /// </summary>
        public EventHandler<InputFocusEventArgs> OnFocusReleased;

        /// <summary>
        /// Currently focused object.
        /// </summary>
        protected IInputFocus CurrentInputFocus { get; set; } = null;

        /// <summary>
        /// Registered listener.
        /// </summary>
        protected Dictionary<string, List<ListenerInfo>> ListenerDictionary { get; } = new Dictionary<string, List<ListenerInfo>>();

        /// <summary>
        /// Registers an action event to input.
        /// </summary>
        /// <param name="listener">The listener owning the action.</param>
        /// <param name="action">The action method which will be called.</param>
        public void RegisterEvent(IInputActionListener listener, Action<InputEventArgs> action)
        {
            string actionName = action.Method.Name;
            if (ListenerDictionary.ContainsKey(actionName) == false)
            {
                ListenerDictionary.Add(actionName, new List<ListenerInfo>());
            }

            List<ListenerInfo> infoList = ListenerDictionary[actionName];

            infoList.Add(new ListenerInfo(listener, action));
            infoList.Sort((l1, l2) => l1.ActionListener.Priority.CompareTo(l2.ActionListener.Priority) * -1);
        }

        /// <summary>
        /// Unregisters the given listeners action.
        /// </summary>
        public void UnregisterEvent(IInputActionListener listener, Action<InputEventArgs> action)
        {
            string actionName = action.Method.Name;
            List<ListenerInfo> infoList = ListenerDictionary[actionName];
            infoList.RemoveAll(info => info.ActionListener == listener && info.Action.Method.Name == actionName);
        }

        /// <summary>
        /// Focus the given input focus target.
        /// </summary>
        public abstract void Focus(IInputFocus target);

        /// <summary>
        /// Releases the focus, if possible.
        /// </summary>
        public abstract void ReleaseFocus();


        protected override void Awake()
        {
            base.Awake();
            Setup();
        }

        protected virtual void Reset()
        {
            Setup();
        }

        /// <summary>
        /// will be called on Reset (in editor time) and Awake (in play mode).
        /// Intended to setup the input controller properly.
        /// </summary>
        protected abstract void Setup();
    }
}
