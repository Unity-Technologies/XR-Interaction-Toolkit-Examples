// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Properties;

namespace VRBuilder.Core.Utils
{
    public static class ProcessReferenceUtils
    {
        public static string GetNameFrom(ISceneObjectProperty property)
        {
            if (property == null)
            {
                return null;
            }

            if (property.SceneObject == null)
            {
                return null;
            }

            return property.SceneObject.UniqueName;
        }

        public static string GetNameFrom(ISceneObject sceneObject)
        {
            if (sceneObject == null)
            {
                return null;
            }

            return sceneObject.UniqueName;
        }
    }
}
