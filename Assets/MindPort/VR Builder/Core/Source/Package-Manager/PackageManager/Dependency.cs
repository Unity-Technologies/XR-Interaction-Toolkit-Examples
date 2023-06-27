// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager.UI;

namespace VRBuilder.Editor.PackageManager
{
    /// <summary>
    /// Base class for dependencies used by the <see cref="DependencyManager"/>.
    /// </summary>
    public abstract class Dependency
    {
        /// <summary>
        /// A string representing the package to be added.
        /// </summary>
        public virtual string Package { get; } = "";

        /// <summary>
        /// A string representing the version of the package.
        /// </summary>
        public virtual string Version { get; set; } = "";

        /// <summary>
        /// Priority lets you tweak in which order each <see cref="Dependency"/> will be performed.
        /// The priority is considered from lowest to highest.
        /// </summary>
        public virtual int Priority { get; } = 0;

        /// <summary>
        /// Collection of samples to be imported from the Unity Package.
        /// </summary>
        public virtual string[] Samples { get; } = null;

        /// <summary>
        /// A list of layers to be added.
        /// </summary>
        protected virtual string[] Layers { get; } = null;

        /// <summary>
        /// Emitted when this <see cref="Dependency"/> is set as enabled.
        /// </summary>
        public event EventHandler<EventArgs> OnPackageEnabled;

        /// <summary>
        /// Emitted when this <see cref="Dependency"/> is set as disabled.
        /// </summary>
        public event EventHandler<EventArgs> OnPackageDisabled;

        /// <summary>
        /// Represents the current status of this <see cref="Dependency"/>.
        /// </summary>
        internal bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (isEnabled != value)
                {
                    if (value)
                    {
                        EmitOnEnabled();
                        AddMissingLayers();
                        ImportPackageSamples();
                    }
                    else
                    {
                        EmitOnDisabled();
                    }
                }

                isEnabled = value;
            }
        }

        private bool isEnabled;

        protected Dependency()
        {
            if (string.IsNullOrEmpty(Package) && Package.Contains('@'))
            {
                string[] packageData = Package.Split('@');
                Package = packageData.First();
                Version = packageData.Last();
            }
        }

        protected virtual void EmitOnEnabled()
        {
            OnPackageEnabled?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void EmitOnDisabled()
        {
            OnPackageDisabled?.Invoke(this, EventArgs.Empty);
        }

        private void ImportPackageSamples()
        {
            IEnumerable<Sample> samples = Sample.FindByPackage(Package, Version);

            if (Samples != null && samples != null && samples.Any())
            {
                foreach (Sample sample in samples)
                {
                    if (Samples.Any(s => s == sample.displayName && sample.isImported == false))
                    {
                        sample.Import();
                        AssetDatabase.Refresh();
                        Debug.Log($"{sample.displayName} was imported.");
                    }
                }
            }
        }

        private void AddMissingLayers()
        {
            if (Layers != null)
            {
                LayerUtils.AddLayers(Layers);
            }
        }
    }
}
