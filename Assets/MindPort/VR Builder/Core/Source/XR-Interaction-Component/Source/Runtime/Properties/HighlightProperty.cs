using System;
using VRBuilder.BasicInteraction;
using UnityEngine;
using VRBuilder.Core.Properties;
using VRBuilder.Unity;

namespace VRBuilder.XRInteraction.Properties
{
    /// <summary>
    /// Highlight property which enables an attached <see cref="InteractableObject"/>.
    /// </summary>
    public class HighlightProperty : BaseHighlightProperty
    {
        /// <summary>
        /// Returns the highlight color, if the object is currently highlighted.
        /// Returns null, otherwise.
        /// </summary>
        public Color? CurrentHighlightColor { get; protected set; }

        [Obsolete("Use 'DefaultHighlighter' instead.")]
        protected InteractableHighlighter Highlighter
        {
            get { return DefaultHighlighter as InteractableHighlighter; }
        }

        /// <summary>
        /// The <see cref="DefaultHighlighter"/> which is used to highlight the <see cref="Core.SceneObjects.ProcessSceneObject"/>.
        /// </summary>
        protected DefaultHighlighter DefaultHighlighter;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (DefaultHighlighter == null)
            {
                Initialize();
            }
        }
        
        protected override void Reset()
        {
            base.Reset();
            Initialize();
        }

        protected void Initialize()
        {
            InteractableObject ownInteractableObject = gameObject.GetComponent<InteractableObject>();

            // If gameObject was not interactable before, disable interactable functionality.
            if (ownInteractableObject == null)
            {
                Rigidbody ownRigidbody = gameObject.GetComponent<Rigidbody>();
                ownInteractableObject = gameObject.GetOrAddComponent<InteractableObject>();
                ownInteractableObject.IsGrabbable = false;
                ownInteractableObject.IsTouchable = false;
                ownInteractableObject.IsUsable = false;
                
                // If the gameObject had no rigidbody and thus was unaffected by physics, make it kinematic.
                if (ownRigidbody == null)
                {
                    gameObject.GetOrAddComponent<Rigidbody>().isKinematic = true;
                }
            }

            InteractableHighlighter interactableHighlighter = GetComponent<InteractableHighlighter>();
            DefaultHighlighter = interactableHighlighter == null ? gameObject.GetOrAddComponent<DefaultHighlighter>() : interactableHighlighter;
        }

        /// <inheritdoc/>
        public override void Highlight(Color highlightColor)
        {
            CurrentHighlightColor = highlightColor;
            IsHighlighted = true;
            DefaultHighlighter.StartHighlighting(highlightColor, SceneObject.UniqueName);
            EmitHighlightEvent(new HighlightPropertyEventArgs(CurrentHighlightColor));
        }

        /// <inheritdoc/>
        public override void Unhighlight()
        {
            CurrentHighlightColor = null;
            IsHighlighted = false;
            DefaultHighlighter.StopHighlighting(SceneObject.UniqueName);
            EmitUnhighlightEvent(new HighlightPropertyEventArgs(CurrentHighlightColor));
        }
    }
}
