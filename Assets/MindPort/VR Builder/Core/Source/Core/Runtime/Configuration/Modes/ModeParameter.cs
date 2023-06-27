// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core.Configuration.Modes
{
    /// <summary>
    /// ModeParameter is responsible for fetching its parameter value from a <see cref="IMode"/>.
    /// If the value changes while being configured, an event will be triggered.
    /// </summary>
    public class ModeParameter<T>
    {
        public EventHandler<EventArgs> ParameterModified;

        /// <summary>
        /// Is true when the current value is different to the default value.
        /// </summary>
        public bool IsModified { get; private set; }

        /// <summary>
        /// Returns the current value, if set and different to the default value it will invoke a ParameterModified event.
        /// </summary>
        public T Value
        {
            get
            {
                return value;
            }
            set
            {
                if (this.value.Equals(value))
                {
                    return;
                }

                IsModified = true;
                this.value = value;
                EmitParameterModified();
            }
        }

        private readonly T defaultValue;

        private T value;

        private readonly string key;

        public ModeParameter(string key, T defaultValue = default(T))
        {
            this.key = key;
            this.defaultValue = defaultValue;
            value = defaultValue;
        }

        /// <summary>
        /// Configures this parameter with the given mode.
        /// </summary>
        public void Configure(IMode mode)
        {
            if (mode.ContainsParameter<T>(key))
            {
                Value = mode.GetParameter<T>(key);
            }
            else
            {
                Reset();
            }
        }

        /// <summary>
        /// Resets the parameter, will triggered modified if the value changes.
        /// </summary>
        public void Reset()
        {
            if (IsModified == false)
            {
                return;
            }

            value = defaultValue;
            IsModified = false;
            EmitParameterModified();
        }

        private void EmitParameterModified()
        {
            if (ParameterModified != null)
            {
                ParameterModified.Invoke(this, new EventArgs());
            }
        }
    }
}
