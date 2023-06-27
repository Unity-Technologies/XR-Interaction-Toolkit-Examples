// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.ObjectModel;
using VRBuilder.Editor.UI.Graphics.Renderers;
using UnityEngine;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Represents transition arrow between two steps.
    /// </summary>
    internal class TransitionElement : GraphicalElement
    {
        private readonly TransitionRenderer renderer;

        private Rect boundingBox;
        private ExitJoint start;

        /// <summary>
        /// Amount of segments a bezier curve consists of.
        /// </summary>
        public static int CurveSegmentCount = 33;

        /// <summary>
        /// Points forming the bezier curve.
        /// </summary>
        public ReadOnlyCollection<Vector2> PolylinePoints { get; private set; }

        /// <summary>
        /// Joint that arrow is pointing at.
        /// </summary>
        public EntryJoint Destination { get; private set; }

        /// <summary>
        /// Joint from which transition starts.
        /// </summary>
        public ExitJoint Start
        {
            get { return start; }

            private set
            {
                start = value;
                Parent = start;
            }
        }

        /// <inheritdoc />
        public override Rect BoundingBox
        {
            get { return boundingBox; }
        }

        /// <inheritdoc />
        public override int Layer
        {
            get { return 80; }
        }

        /// <inheritdoc />
        public override GraphicalElementRenderer Renderer
        {
            get { return renderer; }
        }

        /// <inheritdoc />
        public TransitionElement(EditorGraphics editorGraphics, ExitJoint start, EntryJoint destination) : base(editorGraphics, false, start)
        {
            Destination = destination;
            Start = start;
            renderer = new TransitionRenderer(this, editorGraphics.ColorPalette);
        }

        public override void HandleDeregistration()
        {
            base.HandleDeregistration();
            Start = null;
            Destination = null;
        }

        public override void Layout()
        {
            base.Layout();

            RelativePosition = (Destination.Position - Start.Position) / 2f;

            if (Mathf.Abs(Start.Position.y - Destination.Position.y) > 1.0 || start.Position.x > Destination.Position.x)
            {
                Vector2[] controlPoints = BezierCurveHelper.CalculateControlPointsForTransition(Start.Position, Destination.Position, Start.Parent.BoundingBox, Destination.Parent.BoundingBox);
                PolylinePoints = Array.AsReadOnly(BezierCurveHelper.CalculateDeCastejauCurve(Start.Position, controlPoints[0], controlPoints[1], Destination.Position, CurveSegmentCount));
            }
            else
            {
                PolylinePoints = Array.AsReadOnly(new Vector2[] {Start.Position, Destination.Position});
            }

            boundingBox = BezierCurveHelper.CalculateBoundingBoxForPolyline(PolylinePoints);
        }
    }
}
