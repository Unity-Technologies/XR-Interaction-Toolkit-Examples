// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VRBuilder.Editor.UI.Graphics.Renderers;
using UnityEngine;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Base class for all views in the Workflow window.
    /// </summary>
    internal abstract class GraphicalElement
    {
        private readonly List<GraphicalElement> children = new List<GraphicalElement>();
        private GraphicalElement parent;
        private Vector2 relativePosition;

        public event EventHandler<GraphicalElementEventArgs> RelativePositionChanged;

        /// <summary>
        /// Encapsulates rendering of this graphical element.
        /// If this graphical element is not supposed to be drawn, Renderer is null.
        /// </summary>
        public abstract GraphicalElementRenderer Renderer { get; }

        /// <summary>
        /// Encapsulates handling of UI events that are related to this graphical element.
        /// If this graphical element is not supposed to receive any events, GraphicalEventHandler is null.
        /// </summary>
        public GraphicalEventHandler GraphicalEventHandler { get; private set; }

        /// <summary>
        /// True if this graphical element receives UI events.
        /// </summary>
        public bool IsReceivingEvents { get; private set; }

        /// <summary>
        /// True if this graphical element has a visual representation to be drawn.
        /// </summary>
        public bool CanBeDrawn
        {
            get
            {
                return Renderer != null;
            }
        }

        /// <summary>
        /// Owner EditorGraphics object that relays UI events and manages rendering.
        /// </summary>
        protected EditorGraphics Graphics { get; private set; }

        /// <summary>
        /// GraphicalElement position is calculated relative to Parent element.
        /// </summary>
        public GraphicalElement Parent
        {
            get
            {
                return parent;
            }
            set
            {
                Vector2 position = Position;

                if (parent != null)
                {
                    parent.RemoveChild(this);
                }

                parent = value;

                if (parent != null)
                {
                    parent.AddChild(this);
                }

                Position = position;
            }
        }

        /// <summary>
        /// List of graphical elements for which this graphical element is parent.
        /// </summary>
        public ReadOnlyCollection<GraphicalElement> Children
        {
            get
            {
                return children.AsReadOnly();
            }
        }

        /// <summary>
        /// Relative position from Parent.
        /// </summary>
        public Vector2 RelativePosition
        {
            get
            {
                return relativePosition;
            }
            set
            {
                relativePosition = value;
                if (RelativePositionChanged != null)
                {
                    RelativePositionChanged.Invoke(this, new GraphicalElementEventArgs());
                }
            }
        }

        /// <summary>
        /// Position in the editor window (from top-left).
        /// </summary>
        public Vector2 Position
        {
            get
            {
                if (Parent == null)
                {
                    return RelativePosition;
                }

                return Parent.Position + RelativePosition;
            }
            set
            {
                if (Parent == null)
                {
                    RelativePosition = value;
                    return;
                }

                RelativePosition = value - Parent.Position;
            }
        }

        /// <summary>
        /// Rect that bounds the graphical element in the containing window coordinates.
        /// </summary>
        public abstract Rect BoundingBox { get; }

        /// <summary>
        /// Rect that bounds the graphical element in the coordinates relative to its position.
        /// </summary>
        public Rect LocalBoundingBox
        {
            get
            {
                return new Rect(BoundingBox.position - Position, BoundingBox.size);
            }
        }

        /// <summary>
        /// Elements with higher Layer are checked for pointer events first and drawn last.
        /// </summary>
        public abstract int Layer { get; }

        /// <param name="editorGraphics">Owner EditorGraphics object that relays UI events to this element and manages its rendering.</param>
        /// <param name="isReceivingEvents">If true, new instance of GraphicalEventHandler is added to this graphical element.</param>
        /// <param name="parent">GraphicalElement position is calculated relative to Parent element. Can be null.</param>
        protected GraphicalElement(EditorGraphics editorGraphics, bool isReceivingEvents, GraphicalElement parent = null)
        {
            IsReceivingEvents = isReceivingEvents;
            if (isReceivingEvents)
            {
                GraphicalEventHandler = new GraphicalEventHandler();
            }

            Parent = parent;
            Graphics = editorGraphics;
            Graphics.Register(this);
        }

        public void AddChild(GraphicalElement child)
        {
            children.Add(child);
        }

        public bool RemoveChild(GraphicalElement child)
        {
            return children.Remove(child);
        }

        /// <summary>
        /// Check if point is inside the element.
        /// </summary>
        /// <param name="position">Position of the pointer</param>
        public virtual bool IsPointInsideGeometry(Vector2 position)
        {
            return BoundingBox.Contains(position, true);
        }

        /// <summary>
        /// Check if element is at least partially inside the rect (used to determine if we have to draw an element or we can ignore it).
        /// </summary>
        public virtual bool IsVisibleInRect(Rect rect)
        {
            return BoundingBox.Overlaps(rect, true);
        }

        /// <summary>
        /// Called when element is registered in a EditorGraphics object.
        /// </summary>
        public virtual void HandleRegistration()
        {
        }

        /// <summary>
        /// Called when element is unregistered from its owner Graphics.
        /// </summary>
        public virtual void HandleDeregistration()
        {
        }

        // Called once per Unity Editor frame.
        public virtual void Layout()
        {
        }
    }
}
