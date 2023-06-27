using System;
using UnityEngine;
using UnityEngine.Events;

namespace VRBuilder.Core.Properties
{
    /// <summary>
    /// Interface for scene objects that can be highlighted.
    /// </summary>
    public interface IHighlightProperty : ISceneObjectProperty
    {
        /// <summary>
        /// Emitted when the object gets highlighted.
        /// </summary>
        [Obsolete("Use HighlightStarted instead.")]
        event EventHandler<EventArgs> Highlighted;

        /// <summary>
        /// Emitted when the object gets unhighlighted.
        /// </summary>
        [Obsolete("Use HighlightEnded instead.")]
        event EventHandler<EventArgs> Unhighlighted;

        /// <summary>
        /// Emitted when the object gets highlighted.
        /// </summary>
        UnityEvent<HighlightPropertyEventArgs> HighlightStarted { get; }

        /// <summary>
        /// Emitted when the object gets unhighlighted.
        /// </summary>
        UnityEvent<HighlightPropertyEventArgs> HighlightEnded { get; }

        /// <summary>
        /// Is object currently highlighted.
        /// </summary>
        bool IsHighlighted { get; }

        /// <summary>
        /// Highlight this object and use <paramref name="highlightColor"/>.
        /// </summary>
        /// <param name="highlightColor">Color to use for highlighting.</param>
        void Highlight(Color highlightColor);

        /// <summary>
        /// Disable highlight.
        /// </summary>
        void Unhighlight();
    }

    public class HighlightPropertyEventArgs : EventArgs
    {
        public readonly Color? HighlightColor;

        public HighlightPropertyEventArgs(Color? highlightColor)
        {
            HighlightColor = highlightColor;
        }
    }
}
