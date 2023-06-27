// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

﻿using System;
 using VRBuilder.Core.SceneObjects;

 namespace VRBuilder.Core.Exceptions
{
    public class PropertyNotFoundException : ProcessException
    {
        public PropertyNotFoundException(string message) : base(message) { }
        public PropertyNotFoundException(ISceneObject sourceObject, Type missingType) : base(string.Format("SceneObject '{0}' does not contain a property of type '{1}'", sourceObject.UniqueName, missingType.Name)) { }
    }
}
