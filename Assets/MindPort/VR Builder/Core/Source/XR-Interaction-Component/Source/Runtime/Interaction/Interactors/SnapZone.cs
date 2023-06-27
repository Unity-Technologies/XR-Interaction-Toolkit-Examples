using System;
using System.Linq;
using System.Collections.Generic;
using VRBuilder.BasicInteraction;
using VRBuilder.BasicInteraction.Properties;
using VRBuilder.BasicInteraction.Validation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRBuilder.XRInteraction
{
    /// <summary>
    /// Interactor used for holding interactables via a socket.  This component is not designed to be attached to a controller
    /// (thus does not derive from XRBaseControllerInteractor) and instead will always attempt to select an interactable that it is
    /// hovering (though will not perform exclusive selection of that interactable).
    /// </summary>
    /// <remarks>
    /// Adds the functionality to force the selection and unselection of specific interactables.
    /// It also adds a highlighter to emphasize the position of the socket.
    /// </remarks>
    public partial class SnapZone : XRSocketInteractor, ISnapZone
    {
        /// <summary>
        /// Gets or sets whether <see cref="ShownHighlightObject"/> is shown or not.
        /// </summary>
        public bool ShowHighlightObject { get; set; }

        [SerializeField]
        private GameObject shownHighlightObject = null;

        /// <inheritdoc />
        public bool IsEmpty => SnappedObject == null;

        /// <inheritdoc />
        public ISnappableProperty SnappedObject => hasSelection ? interactablesSelected[0].transform.GetComponent<ISnappableProperty>() : null;

        /// <inheritdoc />
        public Transform Anchor => attachTransform;
        
        /// <summary>
        /// The 'GameObject' whose mesh is drawn to emphasize the position of the snap zone.
        /// If none is supplied, no highlight object is shown.
        /// </summary>
        public GameObject ShownHighlightObject
        {
            get => shownHighlightObject;
            set
            {
                shownHighlightObject = value;
                UpdateHighlightMeshFilterCache();
            }
        }
        
        /// <summary>
        /// Shows the highlight 
        /// </summary>
        
        public bool ShowHighlightInEditor = true;

        [SerializeField]
        [Tooltip("The material used for drawing the mesh.")]
        private Material highlightMeshMaterial;

        /// <summary>
        /// The material used for drawing the mesh of the <see cref="ShownHighlightObject"/>. 
        /// </summary>
        public Material HighlightMeshMaterial
        {
            get
            {
                if (highlightMeshMaterial == null)
                {
                    Debug.LogWarning($"No highlight material set for snap zone on {gameObject.name}. Assign the material in the inspector or through Project Settings > Snap Zones.");
                    highlightMeshMaterial = CreateFallbackMaterial();
                }

                return highlightMeshMaterial;
            }
            set => highlightMeshMaterial = value;
        }

        [SerializeField]
        [Tooltip("Will be used when a valid object hovers the SnapZone")]
        private Material validationMaterial;

        /// <summary>
        /// The material used for drawing when an <see cref="InteractableObject"/> is hovering this <see cref="SnapZone"/>.
        /// </summary>
        public Material ValidationMaterial
        {
            get
            {
                if (validationMaterial == null)
                {
                    Debug.LogWarning($"No validation material set for snap zone on {gameObject.name}. Assign the material in the inspector or through Project Settings > Snap Zones.");
                    validationMaterial = CreateFallbackMaterial();
                }

                return validationMaterial;
            }
            set => validationMaterial = value;
        }
        
        [SerializeField]
        [Tooltip("Will be used when an invalid object hovers the SnapZone")]
        private Material invalidMaterial;

        /// <summary>
        /// The material used for drawing when an invalid <see cref="InteractableObject"/> is hovering this <see cref="SnapZone"/>.
        /// </summary>
        public Material InvalidMaterial
        {
            get
            {
                if (invalidMaterial == null)
                {
                    Debug.LogWarning($"No invalid material set for snap zone on {gameObject.name}. Assign the material in the inspector or through Project Settings > Snap Zones.");
                    invalidMaterial = CreateFallbackMaterial();
                }

                return invalidMaterial;
            }
            set => invalidMaterial = value;
        }

        /// <summary>
        /// Forces the socket interactor to unselect the given target, if it is not null.
        /// </summary>
        [Obsolete("Use ForceUnselectInteractable instead.")]
        protected XRBaseInteractable ForceUnselectTarget { get; set; }

        /// <summary>
        /// Forces the socket interactor to unselect the given target, if it is not null.
        /// </summary>
        protected IXRSelectInteractable ForceUnselectInteractable { get; set; }
        
        /// <summary>
        /// Forces the socket interactor to select the given target, if it is not null.
        /// </summary>
        [Obsolete("Use ForceSelectInteractable instead.")]
        protected XRBaseInteractable ForceSelectTarget { get; set; }

        /// <summary>
        /// Forces the socket interactor to unselect the given target, if it is not null.
        /// </summary>
        protected IXRSelectInteractable ForceSelectInteractable { get; set; }

        /// <summary>
        /// True when an object is about to be snapped to the snapzone.
        /// </summary>
        public bool IsSnapping => ForceSelectInteractable != null;

        /// <summary>
        /// True when an object is about to be unsnapped from the snapzone.
        /// </summary>
        public bool IsUnsnapping => ForceUnselectInteractable != null;
        
        [SerializeField]
        private Mesh previewMesh;
        
        /// <summary>
        /// Returns the preview mesh used for this SnapZone.
        /// </summary>
        public Mesh PreviewMesh 
        {
            get
            {
                if (previewMesh == null && shownHighlightObject != null)
                {
                    UpdateHighlightMeshFilterCache();
                }

                return previewMesh;
            }
            
            set => previewMesh = value;
        }

        private Transform initialParent;
        private Material activeMaterial;
        private Vector3 tmpCenterOfMass;
        private List<Validator> validators = new List<Validator>();
        private readonly List<XRBaseInteractable> snapZoneHoverTargets = new List<XRBaseInteractable>();
        
        protected override void Awake()
        {
            base.Awake();
            
            validators = GetComponents<Validator>().ToList();

            if (GetComponentsInChildren<Collider>()?.Any(foundCollider => foundCollider.isTrigger) == false)
            {
                Debug.LogError($"The Snap Zone '{name}' does not have any trigger collider. "
                    + "Make sure you have at least one collider with the property `Is Trigger` enabled.", gameObject);
            }

            ShowHighlightObject = ShownHighlightObject != null;

            activeMaterial = HighlightMeshMaterial;
            
            if (ShownHighlightObject != null)
            {
                Mesh mesh = ShownHighlightObject.GetComponentInChildren<MeshFilter>().sharedMesh;
                if (mesh!=null && mesh.isReadable == false)
                {
                    Debug.LogWarning($"The mesh <i>{mesh.name}</i> on <i>{ShownHighlightObject.name}</i> is not set readable. In builds, the mesh will not be visible in the snap zone highlight. Please enable <b>Read/Write</b> in the mesh import settings.");
                }

                UpdateHighlightMeshFilterCache();
            }
        }

        internal void AddHoveredInteractable(XRBaseInteractable interactable)
        {  
            if (interactable != null)
            {
                if (snapZoneHoverTargets.Contains(interactable) == false)
                {
                    snapZoneHoverTargets.Add(interactable);
                }
            }
        }

        internal void RemoveHoveredInteractable(XRBaseInteractable interactable)
        {
            snapZoneHoverTargets.Remove(interactable);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            snapZoneHoverTargets.Clear();
            
            selectEntered.AddListener(OnAttach);
            selectExited.AddListener(OnDetach);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            snapZoneHoverTargets.Clear();

            selectEntered.RemoveListener(OnAttach);
            selectExited.RemoveListener(OnDetach);
        }
        
        private void OnAttach(SelectEnterEventArgs arguments)
        {
            IXRSelectInteractable interactable = arguments.interactableObject;
            
            if (interactable != null)
            {
                Rigidbody rigid = interactable.transform.gameObject.GetComponent<Rigidbody>();
                tmpCenterOfMass = rigid.centerOfMass;
                rigid.centerOfMass = Vector3.zero;
            }
        }
        
        private void OnDetach(SelectExitEventArgs arguments)
        {
            IXRSelectInteractable interactable = arguments.interactableObject;
            
            if (interactable != null)
            {
                Rigidbody rigid = interactable.transform.gameObject.GetComponent<Rigidbody>();
                rigid.centerOfMass = tmpCenterOfMass;
            }
        }
        
        private void DetachParent()
        {
            initialParent = transform.parent;
            
            if (initialParent != null)
            {
                transform.SetParent(null);
            }
        }

        private void AttachParent()
        {
            if (initialParent != null)
            {
                transform.SetParent(initialParent);
                initialParent = null;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, "Import", false);
        }

        protected virtual void Update()
        {
            if (socketActive && hasSelection == false)
            {
                DrawHighlightMesh();
            }
        }

        /// <summary>
        /// Updates the <see cref="previewMesh"/> property using the current <see cref="ShownHighlightObject"/>.
        /// </summary>
        protected virtual void UpdateHighlightMeshFilterCache()
        {
            if (ShownHighlightObject == null)
            {
                previewMesh = null;
                return;
            }

            var savedPosition = ShownHighlightObject.transform.position;
            var savedRotation = ShownHighlightObject.transform.rotation;
            if (ShownHighlightObject.scene.name != null)
            {
                ShownHighlightObject.transform.position = Vector3.zero;
                ShownHighlightObject.transform.rotation = Quaternion.identity;
            }

            List<CombineInstance> meshes = new List<CombineInstance>();

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in ShownHighlightObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (skinnedMeshRenderer.sharedMesh == null)
                {
                    continue;
                }
                
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.subMeshCount; i++)
                {
                    CombineInstance combineInstance = new CombineInstance
                    {
                        mesh = skinnedMeshRenderer.sharedMesh,
                        subMeshIndex = i,
                        transform = skinnedMeshRenderer.transform.localToWorldMatrix
                    };

                    meshes.Add(combineInstance);
                }
            }
            
            foreach (MeshFilter meshFilter in ShownHighlightObject.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.sharedMesh == null)
                {
                    continue;
                }

                for (int i = 0; i < meshFilter.sharedMesh.subMeshCount; i++)
                {
                    CombineInstance combineInstance = new CombineInstance
                    {
                        subMeshIndex = i,
                        mesh = meshFilter.sharedMesh,
                        transform = meshFilter.transform.localToWorldMatrix
                    };

                    meshes.Add(combineInstance);
                }
            }

            if (meshes.Any())
            {
                previewMesh = new Mesh();
                previewMesh.CombineMeshes(meshes.ToArray());
                previewMesh.UploadMeshData(true);
            }
            else
            {
                Debug.LogErrorFormat(ShownHighlightObject, "Shown Highlight Object '{0}' has no MeshFilter. It cannot be drawn.", ShownHighlightObject);
            }

            if (ShownHighlightObject.scene.name != null)
            {
                ShownHighlightObject.transform.position = savedPosition;
                ShownHighlightObject.transform.rotation = savedRotation;
            }
        }

        /// <summary>
        /// This method is called by the interaction manager to update the interactor. 
        /// Please see the interaction manager documentation for more details on update order.
        /// </summary>
        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                base.ProcessInteractor(updatePhase);
            }
            
            if (socketActive)
            {
                snapZoneHoverTargets.RemoveAll(target => target == null || target.enabled == false);
                
                CheckForReleasedHoverTargets();
                
                ShowHighlight();
            }
        }

        private void CheckForReleasedHoverTargets()
        {
            if (hasSelection)
            {
                return;
            }
            
            foreach (XRBaseInteractable target in snapZoneHoverTargets)
            {
                if (interactablesHovered.Contains(target) || target.isSelected)
                {
                    continue;
                }
#pragma warning disable 618
                if (CanSelect(target))
#pragma warning restore 618
                {
                    ForceSelect(target);
                    return;
                }
            }
        }

        private void ShowHighlight()
        {
            if (snapZoneHoverTargets.Count == 0 && ShowHighlightObject)
            {
                activeMaterial = HighlightMeshMaterial;
            }
            else if (snapZoneHoverTargets.Count > 0 && showInteractableHoverMeshes)
            {
                activeMaterial = snapZoneHoverTargets.Any(IsValidSnapTarget) ? ValidationMaterial : InvalidMaterial;
            }
            else
            {
                activeMaterial = null;
            }
        }

        /// <summary>
        /// Creates a transparent <see cref="Material"/> using Unity's "Standard" shader.
        /// </summary>
        /// <returns>A transparent <see cref="Material"/>. Null, otherwise, if Unity's "Standard" shader cannot be found.</returns>
        protected virtual Material CreateFallbackMaterial()
        {
            string shaderName = GraphicsSettings.currentRenderPipeline ? "Universal Render Pipeline/Lit" : "Standard";
            Shader defaultShader = Shader.Find(shaderName);

            if (defaultShader == null)
            {
                throw new NullReferenceException($"{name} failed to create a default material," +
                    $" shader \"{shaderName}\" was not found. Make sure the shader is included into the game build.");
            }

            Material highlightMeshMaterial = new Material(defaultShader);

            if (highlightMeshMaterial != null)
            {
                if (GraphicsSettings.currentRenderPipeline)
                {
                    highlightMeshMaterial.SetFloat("_Surface", 1);
                }
                else
                {
                    highlightMeshMaterial.SetFloat("_Mode", 3);
                }

                highlightMeshMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                highlightMeshMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                highlightMeshMaterial.SetInt("_ZWrite", 0);
                highlightMeshMaterial.DisableKeyword("_ALPHATEST_ON");
                highlightMeshMaterial.EnableKeyword("_ALPHABLEND_ON");
                highlightMeshMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                highlightMeshMaterial.color = Color.magenta;
                highlightMeshMaterial.renderQueue = 3000;
            }

            return highlightMeshMaterial;
        }

        /// <summary>
        /// Draws a highlight mesh.
        /// </summary>
        protected virtual void DrawHighlightMesh()
        {
            if (PreviewMesh != null && activeMaterial != null)
            {
                Graphics.DrawMesh(PreviewMesh, attachTransform.localToWorldMatrix, activeMaterial, gameObject.layer, null);
            }
        }

        /// <summary>
        /// Forces to unselect any selected interactable object.
        /// </summary>
        public virtual void ForceUnselect()
        {
            if (hasSelection)
            {
                ForceUnselectInteractable = interactablesSelected[0];
            }
        }

        /// <summary>
        /// Unselects any selected interactable object and forces the provided interactable object to be selected if it is selectable.
        /// </summary>
        /// <param name="interactable">Interactable object to be selected.</param>
        public virtual void ForceSelect(IXRSelectInteractable interactable)
        {
            ForceUnselect();

            if (interactable.IsSelectableBy(this))
            {
                interactionManager.SelectEnter(this, interactable);                
                interactable.transform.SetPositionAndRotation(attachTransform.position, attachTransform.rotation);
                ForceSelectInteractable = interactable;
            }
            else
            {
                Debug.LogWarning($"Interactable '{interactable.transform.name}' is not selectable by Snap Zone '{name}'. "
                    + $"(Maybe the Interaction Layer Masks settings are not correct or the interactable object is locked?)", interactable.transform.gameObject);
            }
        }

        /// <summary>Determines if the interactable is valid for selection this frame.</summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns><c>true</c> if the interactable can be selected this frame.</returns>
        /// <remarks>Adds the functionality of selecting and unselecting specific interactables.</remarks>
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return IsValidSnapTarget(interactable) && base.CanSelect(interactable);
        }

        protected bool IsValidSnapTarget(IXRSelectInteractable interactable)
        {
            // If one specific target should be unselected,
            if (ForceUnselectInteractable == interactable)
            {
                ForceUnselectInteractable = null;
                return false;
            }

            // If one specific target should be selected,
            if (ForceSelectInteractable != null)
            {
                if (ForceSelectInteractable != interactable)
                {
                    return false;
                }

                ForceSelectInteractable = null;
                return true;
            }

            // If one active validator does not allow this to be snapped, return false.
            foreach (Validator validator in validators)
            {
                if (validator.isActiveAndEnabled && validator.Validate(interactable.transform.gameObject) == false)
                {
                    return false;
                }
            }

            return true;
        }

        [Obsolete]
        public override bool CanSelect(XRBaseInteractable interactable)
        {
            return CanSelect((IXRSelectInteractable)interactable);
        }

        /// <inheritdoc />
        public bool CanSnap(ISnappableProperty target)
        {
            IXRSelectInteractable interactableObject = target.SceneObject.GameObject.GetComponent<XRBaseInteractable>();

            if (interactableObject == null)
            {
                return false;
            }

            return CanSelect(interactableObject);
        }

        /// <inheritdoc />
        public bool ForceSnap(ISnappableProperty target)
        {
            XRBaseInteractable interactableObject = target.SceneObject.GameObject.GetComponent<XRBaseInteractable>();

            if (interactableObject == null)
            {
                return false;
            }
            
            ForceSelect(interactableObject);
            return true;
        }

        /// <inheritdoc />
        public bool ForceRelease()
        {
            if (IsEmpty)
            {
                return false;
            }
                        
            ForceUnselect();
            return true;
        }
    }
}
