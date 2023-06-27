// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Runtime.Serialization;
using VRBuilder.Core.Exceptions;
using VRBuilder.Core.Runtime.Properties;

namespace VRBuilder.Core.SceneObjects
{
    /// <summary>
    /// Base class for references to process objects and their properties.
    /// </summary>
    [DataContract(IsReference = true)]
    public abstract class ObjectReference<T> : UniqueNameReference, ICanBeEmpty where T : class
    {
        public override string UniqueName
        {
            get
            {
                return base.UniqueName;
            }
            set
            {
                if (base.UniqueName != value)
                {
                    cachedValue = null;
                }

                base.UniqueName = value;
            }
        }

        private T cachedValue;

        public T Value
        {
            get
            {
                cachedValue = DetermineValue(cachedValue);
                return cachedValue;
            }
        }

        internal override Type GetReferenceType()
        {
            return typeof(T);
        }

        public static implicit operator T(ObjectReference<T> reference)
        {
            return reference.Value;
        }

        protected ObjectReference()
        {
        }

        protected ObjectReference(string uniqueName) : base(uniqueName)
        {
        }

        protected abstract T DetermineValue(T cachedValue);

        /// <inheritdoc/>
        public bool IsEmpty()
        {
            try
            {
                return string.IsNullOrEmpty(UniqueName) || Value == null;
            }
            catch (MissingEntityException)
            {
                return true;
            }
        }
    }
}
