// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;

namespace VRBuilder.Core.Configuration.Modes
{
    /// <summary>
    /// Base class for list-based implementations of the <see cref="TypeRule{TValueBase}"/> class.
    /// </summary>
    public abstract class ListTypeRule<TRecursive, TValueBase> : TypeRule<TValueBase> where TRecursive : ListTypeRule<TRecursive, TValueBase>, new()
    {
        private HashSet<Type> storedTypes = new HashSet<Type>();

        protected HashSet<Type> StoredTypes
        {
            get
            {
                return storedTypes;
            }
        }

        /// <summary>
        /// Adds an additional Type to the list and returns a changed instance of this rule.
        /// </summary>
        /// <typeparam name="T">Type which is added.</typeparam>
        public TRecursive Add<T>() where T : TValueBase
        {
            TRecursive result = Clone();
            if (result.storedTypes.Contains(typeof(T)))
            {
                return result;
            }

            result.storedTypes.Add(typeof(T));
            return result;
        }

        protected virtual TRecursive Clone()
        {
            TRecursive result = new TRecursive {storedTypes = new HashSet<Type>(storedTypes)};
            return result;
        }
    }
}
