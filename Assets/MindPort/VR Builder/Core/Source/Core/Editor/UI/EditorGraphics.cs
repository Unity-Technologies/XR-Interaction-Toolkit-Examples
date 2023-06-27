// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core.Utils;
using VRBuilder.Editor.UI.Graphics;
using UnityEngine;
using UnityEngine.UIElements;
using Edge = UnityEngine.RectTransform.Edge;

namespace VRBuilder.Editor.UI
{
    /// <summary>
    /// UI events handler and graphical elements drawer.
    /// </summary>
    internal class EditorGraphics
    {
        #region constants
        private const float contentBorderThickness = 200f;
        #endregion

        private readonly IList<GraphicalElement> rootElements = new List<GraphicalElement>();

        private GraphicalEventHandler initiallyPressedHandler;
        private GraphicalEventHandler initiallyContextPressedHandler;
        private GraphicalEventHandler initiallyHoveredHandler;

        /// <summary>
        /// Cached result of the last invocation of <see cref="CalculateBoundingBox"/> method.
        /// </summary>
        public Rect BoundingBox { get; private set; }

        /// <summary>
        /// Defines colors used in the Process Editor.
        /// </summary>
        public WorkflowEditorColorPalette ColorPalette { get; set; }

        /// <summary>
        /// The last element user clicked at. If user clicked at empty place, Selected is null.
        /// </summary>
        public GraphicalEventHandler Selected { get; private set; }

        public GraphicalEventHandler Canvas { get; private set; }

        private List<IEditorGraphicDrawer> additionalDrawer = new List<IEditorGraphicDrawer>();

        public EditorGraphics(WorkflowEditorColorPalette workflowEditorColorPalette)
        {
            // Guarantees that at least default palette is assigned.
            if (workflowEditorColorPalette == null)
            {
                ColorPalette = WorkflowEditorColorPalette.GetDefaultPalette();
            }
            else
            {
                ColorPalette = workflowEditorColorPalette;
            }

            Canvas = new GraphicalEventHandler();

            additionalDrawer.Clear();
            ReflectionUtils.GetConcreteImplementationsOf<IEditorGraphicDrawer>().ToList().ForEach(type =>
            {
                additionalDrawer.Add((IEditorGraphicDrawer)ReflectionUtils.CreateInstanceOfType(type));
            });
            additionalDrawer.Sort((drawer1, drawer2) => drawer1.Priority.CompareTo(drawer2.Priority));

            Reset();
        }

        /// <summary>
        /// Registers graphical element to enable UI events and drawing for it.
        /// </summary>
        public void Register(GraphicalElement element)
        {
            if (element.Parent == null)
            {
                rootElements.Add(element);
            }

            element.HandleRegistration();

            foreach (GraphicalElement child in element.Children)
            {
                Register(child);
            }
        }

        /// <summary>
        /// Deregisters graphical element.
        /// </summary>
        public void Deregister(GraphicalElement element)
        {
            rootElements.Remove(element);

            element.HandleDeregistration();

            foreach (GraphicalElement child in element.Children.ToList())
            {
                Deregister(child);
            }
        }

        /// <summary>
        /// Resets this <see cref="EditorGraphics"/> instance to it's initial state.
        /// </summary>
        public void Reset()
        {
            foreach (GraphicalElement element in rootElements.ToList())
            {
                Deregister(element);

                if (element.IsReceivingEvents)
                {
                    element.GraphicalEventHandler.Reset();
                }
            }

            initiallyPressedHandler = null;
            initiallyContextPressedHandler = null;

            Canvas.Reset();

            Selected = null;
        }

        private IEnumerable<GraphicalElement> GetOrderedElements()
        {
            IList<IList<GraphicalElement>> ordered = new List<IList<GraphicalElement>>();

            foreach (GraphicalElement element in rootElements)
            {
                CollectElementsRecursively(element, ref ordered);
            }

            return ordered.SelectMany(layer => layer);
        }

        private void Draw(Rect windowRect)
        {
            foreach (GraphicalElement element in GetOrderedElements())
            {
                if (element.CanBeDrawn && element.IsVisibleInRect(windowRect))
                {
                    element.Renderer.Draw();
                }
            }

            additionalDrawer.ForEach(drawer => drawer.Draw(windowRect));
        }

        private void InsertElement(GraphicalElement element, ref IList<IList<GraphicalElement>> collection)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (collection[i][0].Layer < element.Layer)
                {
                    collection.Insert(i, new List<GraphicalElement> { element });
                    return;
                }

                if (collection[i][0].Layer == element.Layer)
                {
                    collection[i].Add(element);
                    return;
                }
            }

            collection.Add(new List<GraphicalElement> { element });
        }

        private void CollectElementsRecursively(GraphicalElement element, ref IList<IList<GraphicalElement>> collection)
        {
            InsertElement(element, ref collection);

            foreach (GraphicalElement child in element.Children)
            {
                CollectElementsRecursively(child, ref collection);
            }
        }

        private bool ResolveContextPointerUpAndClick(Event currentEvent, GraphicalEventHandler handler)
        {
            bool used = false;
            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 1 && handler != null && handler == initiallyContextPressedHandler || (currentEvent.type == EventType.MouseLeaveWindow))
            {
                if (initiallyContextPressedHandler != null)
                {
                    if (handler == initiallyContextPressedHandler)
                    {
                        initiallyContextPressedHandler.InvokeContextPointerClick(currentEvent.mousePosition);
                    }

                    initiallyContextPressedHandler.InvokeContextPointerUp(currentEvent.mousePosition);

                    StartHovering(currentEvent, handler);
                    used = true;
                }

                initiallyContextPressedHandler = null;
            }

            return used;
        }

        private bool ResolvePointerUpAndClick(Event currentEvent, GraphicalEventHandler handler)
        {
            bool used = false;

            // Left mouse button was released. If mouse had left the screen, we treat it as if left mouse button was released.
            if ((currentEvent.type == EventType.MouseUp && currentEvent.button == 0) || (currentEvent.type == EventType.MouseLeaveWindow))
            {
                if (initiallyPressedHandler != null)
                {
                    // Also, it is a click if we clicked and released LMB over the same element.
                    if (handler == initiallyPressedHandler)
                    {
                        initiallyPressedHandler.InvokePointerClick(currentEvent.mousePosition);
                    }

                    // Initially pressed element recieves the event in any case,
                    initiallyPressedHandler.InvokePointerUp(currentEvent.mousePosition);

                    StartHovering(currentEvent, handler);

                    used = true;
                }

                Selected = null;
                initiallyPressedHandler = null;
            }

            return used;
        }

        private bool ResolvePointerHover(Event currentEvent, GraphicalEventHandler handler)
        {
            if (initiallyHoveredHandler != null && initiallyHoveredHandler != handler)
            {
                StopHovering(currentEvent);
                initiallyHoveredHandler = null;
                return false;
            }

            if (initiallyHoveredHandler != handler && handler != null)
            {
                StartHovering(currentEvent, handler);
                return true;
            }

            return false;
        }

        private bool ResolvePointerDrag(Event currentEvent)
        {
            bool used = false;

            // If we are holding left mouse button and moving it, initially pressed elements receives drag event.
            if (initiallyPressedHandler != null)
            {
                initiallyPressedHandler.InvokePointerDrag(currentEvent.mousePosition, currentEvent.delta);
                used = true;
            }

            return used;
        }

        private bool ResolvePointerDown(Event currentEvent, GraphicalEventHandler handler)
        {
            bool used = false;

            // Left mouse button was pressed down.
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                initiallyPressedHandler = handler;

                if (handler != null)
                {
                    Selected = handler;
                    handler.InvokePointerDown(currentEvent.mousePosition);

                    StopHovering(currentEvent);

                    used = true;
                }
            }

            return used;
        }

        private bool ResolveContextPointerDown(Event currentEvent, GraphicalEventHandler handler)
        {
            bool used = false;

            // Left mouse button was pressed down.
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
            {
                initiallyContextPressedHandler = handler;

                if (handler != null)
                {
                    handler.InvokeContextPointerDown(currentEvent.mousePosition);

                    StopHovering(currentEvent);

                    used = true;
                }
            }

            return used;
        }

        private void StartHovering(Event currentEvent, GraphicalEventHandler handler)
        {
            if (initiallyHoveredHandler != null)
            {
                StopHovering(currentEvent);
            }

            initiallyHoveredHandler = handler;
            handler.InvokePointerHoverStart(currentEvent.mousePosition);
        }

        private void StopHovering(Event currentEvent)
        {
            if (initiallyHoveredHandler == null)
            {
                return;
            }

            initiallyHoveredHandler.InvokePointerHoverStop(currentEvent.mousePosition);
        }

        /// <summary>
        /// Process current event.
        /// </summary>
        /// <param name="currentEvent">Event that is currently being processed.</param>
        /// <param name="windowRect">Current window rect. Elements outside the window rect are ignored.</param>
        public void HandleEvent(Event currentEvent, Rect windowRect)
        {
            if (currentEvent.type == EventType.Layout)
            {
                Layout();
                return;
            }

            if (currentEvent.isMouse)
            {
                if (windowRect.Contains(currentEvent.mousePosition) == false)
                {
                    if (initiallyPressedHandler != null)
                    {
                        // Initially pressed element recieves the event in any case,
                        initiallyPressedHandler.InvokePointerUp(currentEvent.mousePosition);
                    }

                    Selected = null;
                    initiallyPressedHandler = null;
                    return;
                }

                GraphicalElement element = GetGraphicalElementWithHandlerAtPoint(currentEvent.mousePosition).FirstOrDefault();

                GraphicalEventHandler handler = Canvas;

                if (element != null)
                {
                    handler = element.GraphicalEventHandler;
                }

                if (currentEvent.type == EventType.MouseDrag)
                {
                    if (ResolvePointerDrag(currentEvent))
                    {
                        Event.current.Use();
                    }
                }
                else if (ResolvePointerHover(currentEvent, handler)
                    // | is on purpose.
                    | ResolveContextPointerUpAndClick(currentEvent, handler)
                    | ResolvePointerUpAndClick(currentEvent, handler)
                    | ResolveContextPointerDown(currentEvent, handler)
                    | ResolvePointerDown(currentEvent, handler))
                {
                    Event.current.Use();
                }
            }

            if (currentEvent.type == EventType.Repaint)
            {
                Draw(windowRect);
            }
        }

        public IEnumerable<GraphicalElement> GetGraphicalElementWithHandlerAtPoint(Vector2 position)
        {
            IEnumerator<GraphicalElement> enumerator = GetOrderedElements()
                .Where(e => e.IsReceivingEvents)
                .Where(e => e.IsPointInsideGeometry(position))
                .Reverse()
                .GetEnumerator();

            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }

            enumerator.Dispose();
        }

        private void Layout()
        {
            foreach (GraphicalElement element in rootElements)
            {
                LayoutRecursive(element);
            }
        }

        private void LayoutRecursive(GraphicalElement element)
        {
            element.Layout();

            foreach (GraphicalElement child in element.Children)
            {
                LayoutRecursive(child);
            }
        }

        /// <summary>
        /// Calculates smallest possible `Rect` which contains all graphical elements' bounding boxes and stores the result in <see cref="BoundingBox"/> property.
        /// </summary>
        /// <returns></returns>
        public void CalculateBoundingBox()
        {
            Dictionary<Edge, Rect> mostOutlyingRects = Enum.GetValues(typeof(Edge))
                .Cast<Edge>()
                .ToDictionary(edge => edge, edge => GetOrderedElements().Aggregate((first, second) => CompareElementsByOutlyingness(first, second, edge)).BoundingBox);

            float x = -contentBorderThickness + mostOutlyingRects[Edge.Left].xMin;
            float y = -contentBorderThickness + mostOutlyingRects[Edge.Top].yMin;
            float width = contentBorderThickness * 2f + (mostOutlyingRects[Edge.Right].xMax - mostOutlyingRects[Edge.Left].xMin);
            float height = contentBorderThickness * 2f + (mostOutlyingRects[Edge.Bottom].yMax - mostOutlyingRects[Edge.Top].yMin);
            BoundingBox = new Rect(x, y, width, height);
        }

        private static GraphicalElement CompareElementsByOutlyingness(GraphicalElement firstElement, GraphicalElement secondElement, Edge edge)
        {
            Rect first = firstElement.BoundingBox;

            Rect second = secondElement.BoundingBox;
            switch (edge)
            {
                case Edge.Left:
                    if (first.xMin <= second.xMin)
                    {
                        return firstElement;
                    }

                    break;
                case Edge.Right:
                    if (first.xMax >= second.xMax)
                    {
                        return firstElement;
                    }

                    break;
                case Edge.Top:
                    if (first.yMin <= second.yMin)
                    {
                        return firstElement;
                    }

                    break;
                case Edge.Bottom:
                    if (first.yMax >= second.yMax)
                    {
                        return firstElement;
                    }

                    break;
            }

            return secondElement;
        }

        public void BringToTop(GraphicalElement rootElement)
        {
            int elementIndex = rootElements.IndexOf(rootElement);
            rootElements.Insert(rootElements.Count, rootElement);
            rootElements.RemoveAt(elementIndex);
        }
    }
}
