// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core.SceneObjects;

namespace VRBuilder.Core.Exceptions
{
    public class NameNotUniqueException : ProcessException
    {
        public NameNotUniqueException(ISceneObject entity) : base(string.Format("Could not register Item with name '{0}', name already in use", entity.UniqueName))
        {
        }
    }
}
