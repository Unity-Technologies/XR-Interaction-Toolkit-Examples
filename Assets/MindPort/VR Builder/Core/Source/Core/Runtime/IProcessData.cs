// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using VRBuilder.Core.EntityOwners;

namespace VRBuilder.Core
{
    /// <summary>
    /// The data class for a <see cref="IProcess"/>.
    /// </summary>
    public interface IProcessData : IEntityNonLinearSequenceDataWithMode<IChapter>, IRenameableData
    {
        /// <summary>
        /// The list of the <see cref="IChapter"/>s.
        /// </summary>
        IList<IChapter> Chapters { get; set; }

        /// <summary>
        /// The <see cref="IChapter"/> to start execution from.
        /// </summary>
        IChapter FirstChapter { get; }
    }
}
