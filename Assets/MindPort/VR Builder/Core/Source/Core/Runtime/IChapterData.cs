// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using VRBuilder.Core.EntityOwners;

namespace VRBuilder.Core
{
    /// <summary>
    /// The <see cref="IChapter"/>'s data interface.
    /// </summary>
    public interface IChapterData : IEntitySequenceDataWithMode<IStep>, IRenameableData
    {
        /// <summary>
        /// The <see cref="IStep"/> from which the chapter starts.
        /// </summary>
        IStep FirstStep { get; set; }

        /// <summary>
        /// The list of all <see cref="IStep"/>s in the chapter.
        /// </summary>
        IList<IStep> Steps { get; set; }
    }
}
