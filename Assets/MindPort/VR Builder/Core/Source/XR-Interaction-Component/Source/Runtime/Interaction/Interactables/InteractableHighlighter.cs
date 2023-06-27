using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRBuilder.BasicInteraction;

namespace VRBuilder.XRInteraction
{
    /// <summary>
    /// Handles highlighting for attached <see cref="InteractableObject"/>.
    /// </summary>
    [RequireComponent(typeof(InteractableObject))]
    public sealed class InteractableHighlighter : DefaultHighlighter
    {
        private string hoverID = "Hover";
        private string selectID = "Select";
        private string activateID = "Activate";
        
        /// <summary>
        /// Reference to the <see cref="InteractableObject"/>.
        /// </summary>
        public InteractableObject InteractableObject => interactableObject;

        /// <summary>
        /// Determines if this <see cref="InteractableObject"/> should be highlighted when touched.
        /// </summary>
        public bool AllowOnTouchHighlight
        {
            get => allowOnTouchHighlight;
            set => allowOnTouchHighlight = value;
        }

        /// <summary>
        /// Determines if this <see cref="InteractableObject"/> should be highlighted when grabbed.
        /// </summary>
        public bool AllowOnGrabHighlight
        {
            get => allowOnGrabHighlight;
            set => allowOnGrabHighlight = value;
        }

        /// <summary>
        /// Determines if this <see cref="InteractableObject"/> should be highlighted when used.
        /// </summary>
        public bool AllowOnUseHighlight
        {
            get => allowOnUseHighlight;
            set => allowOnUseHighlight = value;
        }

        [SerializeField]
        private bool allowOnTouchHighlight = true;
        
        [SerializeField]
        private bool allowOnGrabHighlight = false;
        
        [SerializeField]
        private bool allowOnUseHighlight = false;

        [SerializeField]
        private Material touchHighlightMaterial = null;
        
        [SerializeField]
        private Material grabHighlightMaterial = null;
        
        [SerializeField]
        private Material useHighlightMaterial = null;

        [SerializeField]
        private Color touchHighlightColor = new Color32(64, 200, 255, 50);
        
        [SerializeField]
        private Color grabHighlightColor = new Color32(255, 0, 0, 50);
        
        [SerializeField]
        private Color useHighlightColor = new Color32(0, 255, 0, 50);

        private Material colorTouchMaterial;
        private Material colorGrabMaterial;
        private Material colorUseMaterial;

        private InteractableObject interactableObject;

        private void Start()
        { 
            hoverID = $"{hoverID}{GetInstanceID()}"; 
            selectID = $"{selectID}{GetInstanceID()}";
            activateID = $"{activateID}{GetInstanceID()}";
        }

        private void OnEnable()
        {
            if (interactableObject == false)
            {
                interactableObject = gameObject.GetComponent<InteractableObject>();
            }

            interactableObject.firstHoverEntered.AddListener(OnHoverEnter);
            interactableObject.lastHoverExited.AddListener(OnHoverExit);
            interactableObject.selectEntered.AddListener(OnSelectEnter);
            interactableObject.selectExited.AddListener(OnSelectExit);
            interactableObject.activated.AddListener(OnActivateEnter);
            interactableObject.deactivated.AddListener(OnActivateExit);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            if (interactableObject == false)
            {
                return;
            }
            
            interactableObject.firstHoverEntered.RemoveListener(OnHoverEnter);
            interactableObject.lastHoverExited.RemoveListener(OnHoverExit);
            interactableObject.selectEntered.RemoveListener(OnSelectEnter);
            interactableObject.selectExited.RemoveListener(OnSelectExit);
            interactableObject.activated.RemoveListener(OnActivateEnter);
            interactableObject.deactivated.RemoveListener(OnActivateExit);
        }

        private void OnValidate()
        {
            if (allowOnTouchHighlight && colorTouchMaterial != null)
            {
                colorTouchMaterial.color = touchHighlightColor;
            }

            if (allowOnGrabHighlight && colorGrabMaterial != null)
            {
                colorGrabMaterial.color = grabHighlightColor;
            }

            if (allowOnUseHighlight && colorUseMaterial != null)
            {
                colorUseMaterial.color = useHighlightColor;
            }
        }

        [Obsolete("Use 'string StartHighlighting(Material highlightMaterial, string highlightID = null)' instead")]
        public void StartHighlighting(string highlightID, Material highlightMaterial)
        {
            StartHighlighting(highlightMaterial, highlightID);
        }
        
        [Obsolete("Use 'string StartHighlighting(Color highlightColor, string highlightID = null)' instead")]
        public void StartHighlighting(string highlightID, Color highlightColor)
        {
            StartHighlighting(highlightColor, highlightID);
        }
        
        [Obsolete("Use 'string StartHighlighting(Texture highlightTexture, string highlightID = null)' instead")]
        public void StartHighlighting(string highlightID, Texture highlightTexture)
        {
            StartHighlighting(highlightTexture, highlightID);
        }

        private void OnHoverEnter(HoverEnterEventArgs arguments)
        {
            HighlightHoverAction();
        }
        
        private void OnHoverExit(HoverExitEventArgs arg0)
        {
            StopHighlighting(hoverID);
        }
        
        private void OnSelectEnter(SelectEnterEventArgs arguments)
        {
            HighlightSelectAction();
        }
        
        private void OnSelectExit(SelectExitEventArgs arguments)
        {
            StopHighlighting(selectID);
        }
        
        private void OnActivateEnter(ActivateEventArgs arguments)
        {
            HighlightActivateAction();
        }

        private void OnActivateExit(DeactivateEventArgs arg0)
        {
            StopHighlighting(activateID);
        }

        private void HighlightHoverAction()
        {
            if (ShouldHighlightTouching())
            {
                Material highlightMaterial = null;
                
                if (touchHighlightMaterial != null)
                {
                    highlightMaterial = touchHighlightMaterial;
                }
                else 
                {
                    if (colorTouchMaterial == null)
                    {
                        colorTouchMaterial = CreateHighlightMaterial(touchHighlightColor);
                    }

                    highlightMaterial = colorTouchMaterial;
                }
                
                StartHighlighting(highlightMaterial, hoverID);
            }
        }

        private void HighlightSelectAction()
        {
            if (ShouldHighlightGrabbing())
            {
                Material highlightMaterial = null;
                
                if (grabHighlightMaterial != null)
                {
                    highlightMaterial = grabHighlightMaterial;
                }
                else 
                {
                    if (colorGrabMaterial == null)
                    {
                        colorGrabMaterial = CreateHighlightMaterial(grabHighlightColor);
                    }

                    highlightMaterial = colorGrabMaterial;
                }
                
                StartHighlighting(highlightMaterial, selectID);
            }
        }

        private void HighlightActivateAction()
        {
            if (ShouldHighlightUsing())
            {
                Material highlightMaterial = null;
                
                if (useHighlightMaterial != null)
                {
                    highlightMaterial = useHighlightMaterial;
                }
                else 
                {
                    if (colorUseMaterial == null)
                    {
                        colorUseMaterial = CreateHighlightMaterial(useHighlightColor);
                    }

                    highlightMaterial = colorUseMaterial;
                }
                
                StartHighlighting(highlightMaterial, activateID);
            }
        }

        private bool ShouldHighlightTouching()
        {
            if (interactableObject.IsInSocket)
            {
                return allowOnTouchHighlight && interactableObject.isHovered;
            }

            return allowOnTouchHighlight && interactableObject.isHovered && interactableObject.isSelected == false;
        }

        private bool ShouldHighlightGrabbing()
        {
            if (interactableObject.IsInSocket)
            {
                return false;
            }

            return allowOnGrabHighlight && interactableObject.isSelected && interactableObject.IsActivated == false;
        }

        private bool ShouldHighlightUsing()
        {
            return allowOnUseHighlight && interactableObject.IsActivated && interactableObject.isSelected;
        }
    }
}
