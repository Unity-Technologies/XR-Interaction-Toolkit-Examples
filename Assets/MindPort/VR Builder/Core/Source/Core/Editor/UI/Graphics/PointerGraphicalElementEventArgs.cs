// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEngine;

namespace VRBuilder.Editor.UI.Graphics
{
    internal class PointerGraphicalElementEventArgs : GraphicalElementEventArgs
    {
        public Vector2 PointerPosition { get; private set; }

        public PointerGraphicalElementEventArgs(Vector2 pointerPosition)
        {
            PointerPosition = pointerPosition;
        }
    }
}
