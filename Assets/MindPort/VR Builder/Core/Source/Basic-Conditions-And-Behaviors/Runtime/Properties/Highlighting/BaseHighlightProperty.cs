using System;
using UnityEngine;
using UnityEngine.Events;

namespace VRBuilder.Core.Properties
{
    /// <summary>
    /// Abstract base property for highlight properties.
    /// </summary>
    public abstract class BaseHighlightProperty : ProcessSceneObjectProperty, IHighlightProperty
    {
        [Header("Events")]
        [SerializeField]
        private UnityEvent<HighlightPropertyEventArgs> highlightStarted = new UnityEvent<HighlightPropertyEventArgs>();

        [SerializeField]
        private UnityEvent<HighlightPropertyEventArgs> highlightEnded = new UnityEvent<HighlightPropertyEventArgs>();

        /// <summary>
        /// Event data for events of <see cref="BaseHighlightProperty"/>.
        /// </summary>
        public class HighlightEventArgs : EventArgs { }

        /// <inheritdoc/>
        public event EventHandler<EventArgs> Highlighted;

        /// <inheritdoc/>
        public event EventHandler<EventArgs> Unhighlighted;

        /// <summary>
        /// Is currently highlighted.
        /// </summary>
        public bool IsHighlighted { get; protected set; }

        /// <inheritdoc/>
        public UnityEvent<HighlightPropertyEventArgs> HighlightStarted => highlightStarted;

        /// <inheritdoc/>
        public UnityEvent<HighlightPropertyEventArgs> HighlightEnded => highlightEnded;

        /// <inheritdoc/>
        public abstract void Highlight(Color highlightColor);

        /// <inheritdoc/>
        public abstract void Unhighlight();

        /// <summary>
        /// Emits an event when the property is highlighted.
        /// </summary>
        public void EmitHighlightEvent(HighlightPropertyEventArgs args)
        {
            if (Highlighted != null)
            {
                Highlighted.Invoke(this, new HighlightEventArgs());
            }

            HighlightStarted?.Invoke(args);
        }

        /// <summary>
        /// Emits an event when the property is unhighlighted.
        /// </summary>
        public void EmitUnhighlightEvent(HighlightPropertyEventArgs args)
        {
            if (Unhighlighted != null)
            {
                Unhighlighted.Invoke(this, new HighlightEventArgs());
            }

            HighlightEnded?.Invoke(args);
        }
    }
}
