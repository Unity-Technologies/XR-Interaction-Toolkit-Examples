// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace VRBuilder.Core.Configuration.Modes
{
    /// <summary>
    /// A process mode that is defined by its name, IConfigurables activation policy and a collection of parameters.
    /// Immutable.
    /// </summary>
    public sealed class Mode : IMode
    {
        /// <inheritdoc />
        public string Name { get; private set; }

        private readonly Dictionary<string, object> parameters;

        /// <summary>
        /// A rule that determines which <see cref="IOptional"/> implementations have to be skipped.
        /// </summary>
        private readonly TypeRule<IOptional> entitiesToSkip;

        /// <param name="name">Name of the process mode.</param>
        /// <param name="entitiesToSkip">A type rule which determines if an <see cref="IOptional"/> has to be skipped, depending on its type.</param>
        /// <param name="parameters">A string-to-object dictionary of process mode parameters.</param>
        public Mode(string name, TypeRule<IOptional> entitiesToSkip, Dictionary<string, object> parameters = null)
        {
            Name = name;
            this.entitiesToSkip = entitiesToSkip;

            if (parameters == null)
            {
                parameters = new Dictionary<string, object>();
            }
            this.parameters = parameters.ToDictionary(entry => entry.Key, entry => entry.Value);
        }

        /// <inheritdoc />
        public bool CheckIfSkipped<TSkippable>() where TSkippable : IOptional
        {
            return CheckIfSkipped(typeof(TSkippable));
        }

        /// <inheritdoc />
        public bool CheckIfSkipped(Type type)
        {
            return entitiesToSkip.IsQualifiedBy(type);
        }

        /// <inheritdoc />
        public TValue GetParameter<TValue>(string key)
        {
            return (TValue) parameters[key];
        }

        /// <inheritdoc />
        public bool ContainsParameter<TValue>(string key)
        {
            return parameters.ContainsKey(key) && parameters[key] is TValue;
        }
    }
}
