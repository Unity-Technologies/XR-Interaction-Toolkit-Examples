// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Core
{
    /// <summary>
    /// A chapter is a high-level grouping of several <see cref="IStep"/>s.
    /// </summary>
    public interface IChapter : IEntity, IDataOwner<IChapterData>, IClonable<IChapter>
    {
        /// <summary>
        /// Utility data which is used by VR Builder custom editors.
        /// </summary>
        ChapterMetadata ChapterMetadata { get; }
    }
}
