// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEngine;

namespace VRBuilder.Editor.UI.Graphics
{
    internal class PointerDraggedGraphicalElementEventArgs : PointerGraphicalElementEventArgs
    {
        public Vector2 PointerDelta
        {
            get;
            private set;
        }

        public PointerDraggedGraphicalElementEventArgs(Vector2 pointerPosition, Vector2 pointerDelta) : base(pointerPosition)
        {
            PointerDelta = pointerDelta;
        }
    }
}
