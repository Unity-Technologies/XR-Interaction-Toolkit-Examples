// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEngine;

namespace VRBuilder.Editor.UI.Graphics
{
    internal class GraphicalEventHandler
    {
        public event EventHandler<PointerGraphicalElementEventArgs> PointerDown;
        public event EventHandler<PointerGraphicalElementEventArgs> PointerUp;
        public event EventHandler<PointerGraphicalElementEventArgs> PointerClick;

        public event EventHandler<PointerGraphicalElementEventArgs> PointerHoverStart;
        public event EventHandler<PointerGraphicalElementEventArgs> PointerHoverStop;

        public event EventHandler<PointerGraphicalElementEventArgs> ContextPointerDown;
        public event EventHandler<PointerGraphicalElementEventArgs> ContextClick;
        public event EventHandler<PointerGraphicalElementEventArgs> ContextPointerUp;

        public event EventHandler<PointerDraggedGraphicalElementEventArgs> PointerDrag;

        public void InvokeContextPointerDown(Vector2 pointerPosition)
        {
            if (ContextPointerDown != null)
            {
                ContextPointerDown.Invoke(this, new PointerGraphicalElementEventArgs(pointerPosition));
            }
        }

        public void InvokeContextPointerUp(Vector2 pointerPosition)
        {
            if (ContextPointerUp != null)
            {
                ContextPointerUp.Invoke(this, new PointerGraphicalElementEventArgs(pointerPosition));
            }
        }

        public void InvokePointerDown(Vector2 pointerPosition)
        {
            if (PointerDown != null)
            {
                PointerDown.Invoke(this, new PointerGraphicalElementEventArgs(pointerPosition));
            }
        }

        public void InvokePointerUp(Vector2 pointerPosition)
        {
            if (PointerUp != null)
            {
                PointerUp.Invoke(this, new PointerGraphicalElementEventArgs(pointerPosition));
            }
        }

        public void InvokePointerClick(Vector2 pointerPosition)
        {
            if (PointerClick != null)
            {
                PointerClick.Invoke(this, new PointerGraphicalElementEventArgs(pointerPosition));
            }
        }

        public void InvokePointerHoverStart(Vector2 pointerPosition)
        {
            if (PointerHoverStart != null)
            {
                PointerHoverStart.Invoke(this, new PointerGraphicalElementEventArgs(pointerPosition));
            }
        }

        public void InvokePointerHoverStop(Vector2 pointerPosition)
        {
            if (PointerHoverStop != null)
            {
                PointerHoverStop.Invoke(this, new PointerGraphicalElementEventArgs(pointerPosition));
            }
        }

        public void InvokeContextPointerClick(Vector2 pointerPosition)
        {
            if (ContextClick != null)
            {
                ContextClick.Invoke(this, new PointerGraphicalElementEventArgs(pointerPosition));
            }
        }

        public void InvokePointerDrag(Vector2 pointerPosition, Vector2 delta)
        {
            if (PointerDrag != null)
            {
                PointerDrag.Invoke(this, new PointerDraggedGraphicalElementEventArgs(pointerPosition, delta));
            }
        }

        public void Reset()
        {
            PointerDown = null;
            PointerUp = null;
            PointerClick = null;
            PointerHoverStart = null;
            PointerHoverStop = null;
            ContextClick = null;
            PointerDrag = null;
        }
    }
}
