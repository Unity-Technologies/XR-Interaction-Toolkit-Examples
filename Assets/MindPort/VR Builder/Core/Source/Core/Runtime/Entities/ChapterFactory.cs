// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Unity;

namespace VRBuilder.Core
{
    /// <summary>
    /// Factory implementation for <see cref="IChapter"/> objects.
    /// </summary>
    internal class ChapterFactory : Singleton<ChapterFactory>
    {
        /// <summary>
        /// Creates a new <see cref="IChapter"/>.
        /// </summary>
        /// <param name="name"><see cref="IChapter"/>'s name.</param>
        public IChapter Create(string name)
        {
            return new Chapter(name, null);
        }
    }
}
