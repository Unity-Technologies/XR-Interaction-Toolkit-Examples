// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core.Conditions;

namespace VRBuilder.Core
{
    /// <summary>
    /// An <see cref="Entity{TData}"/> which can be completed. Entities can be completed only during their Active <seealso cref="Stage"/>.
    /// </summary>
    public abstract class CompletableEntity<TData> : Entity<TData>, ICompletableEntity where TData : class, ICompletableData, new()
    {
        /// <summary>
        /// Override this method to return a custom <see cref="Autocompleter{TData}"/>.
        /// </summary>
        /// <returns>By default, it returns and empty autocompleter which does nothing.</returns>
        protected virtual IAutocompleter GetAutocompleter()
        {
            return new EmptyAutocompleter();
        }

        /// <summary>
        /// Returns true if entity is completed.
        /// </summary>
        public bool IsCompleted
        {
            get { return Data.IsCompleted; }
        }

        /// <summary>
        /// If the entity is in the Active <see cref="Stage"/>, it will invoke its Autocompleter and set <see cref="ICompletableData.IsCompleted"/> to true.
        /// </summary>
        public void Autocomplete()
        {
            if (LifeCycle.Stage == Stage.Active)
            {
                GetAutocompleter().Complete();
                Data.IsCompleted = true;
            }
        }
    }
}
