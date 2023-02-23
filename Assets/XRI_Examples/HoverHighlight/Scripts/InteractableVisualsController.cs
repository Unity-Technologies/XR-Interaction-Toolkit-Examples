using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEngine.XR.Content.Rendering
{
    /// <summary>
    /// All-in-one controller for animated object highlights in different states - hovered, selected, and activated
    /// </summary>
    public class InteractableVisualsController : MonoBehaviour
    {
        const float k_ShineTime = 0.2f;

        enum PriorityHighlightingState
        {
            Unknown,
            Highlighted,
            Unhighlighted,
        }

        static List<IXRTargetPriorityInteractor> s_InteractorList = new List<IXRTargetPriorityInteractor>();

#pragma warning disable 649
        [Header("Audio")]
        [SerializeField]
        [Tooltip("The hover audio source.")]
        AudioSource m_AudioHover;

        [SerializeField]
        [Tooltip("The click audio source.")]
        AudioSource m_AudioClick;

        [Header("Visual")]
        [SerializeField]
        [Tooltip("Material capture settings.")]
        HighlightController m_HighlightController = new HighlightController();

        [SerializeField]
        [Tooltip("The outline highlight for selection.")]
        OutlineHighlight m_OutlineHighlight;

        [SerializeField]
        [Tooltip("The material highlight for hover.")]
        MaterialHighlight m_MaterialHighlight;

        [SerializeField]
        [Tooltip("The outline hover color.")]
        Color m_HoverColor = new Color(0.25f, 0.7f, 0.9f, 1f);

        [SerializeField]
        [Tooltip("The outline hover color when the Interactable has the highest priority for selection.")]
        Color m_HoverPriorityColor = new Color(0.09411765f, 0.4392157f, 0.7137255f, 1f);

        [SerializeField]
        [Tooltip("The outline selection color.")]
        Color m_SelectionColor = new Color(1f, 0.4f, 0f, 1f);

        [SerializeField]
        [Tooltip("To play material activate anim.")]
        bool m_PlayMaterialActivateAnim;

        [SerializeField]
        [Tooltip("To play outline activate anim.")]
        bool m_PlayOutlineActivateAnim;

        [SerializeField]
        [Tooltip("If true, the highlight state will be on during hover.")]
        bool m_HighlightOnHover = true;

        [SerializeField]
        [Tooltip("If true, the highlight state will be on during hover when the Interactable has the highest priority for selection.")]
        bool m_HighlightOnHoverPriority = true;

        [SerializeField]
        [Tooltip("If true, the highlight state will be on during select.")]
        bool m_HighlightOnSelect = true;

        [SerializeField]
        [Tooltip("If true, the highlight state will be on during activate.")]
        bool m_HighlightOnActivate = true;

#pragma warning restore 649

        XRBaseInteractable m_Interactable;

        Material m_PulseMaterial;
        float m_StartingAlpha;
        float m_StartingWidth;

        int m_SelectedCount;
        int m_HoveredCount;
        bool m_Highlighting;
        PriorityHighlightingState m_PriorityHighlightingState;

        bool m_PlayShine;
        float m_ShineTimer;

        bool isActivated { get; set; }
        bool isSelected => m_SelectedCount > 0;
        bool isHovered => m_HoveredCount > 0;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Awake()
        {
            // Find the grab interactable
            m_Interactable = GetComponentInParent<XRBaseInteractable>();

            // Hook up to events
            if (m_Interactable is IXRHoverInteractable hoverInteractable)
            {
                hoverInteractable.hoverEntered.AddListener(OnHoverEntered);
                hoverInteractable.hoverExited.AddListener(OnHoverExited);
            }

            if (m_Interactable is IXRSelectInteractable selectInteractable)
            {
                selectInteractable.selectEntered.AddListener(OnSelectEntered);
                selectInteractable.selectExited.AddListener(OnSelectExited);
            }

            if (m_Interactable is IXRActivateInteractable activateInteractable)
            {
                activateInteractable.activated.AddListener(OnActivated);
                activateInteractable.deactivated.AddListener(OnDeactivated);
            }

            // Cache materials for highlighting
            m_HighlightController.rendererSource = m_Interactable.transform;

            // Tell the highlight objects to get renderers starting at the grab interactable down
            if (m_MaterialHighlight != null)
            {
                m_HighlightController.RegisterCacheUser(m_MaterialHighlight);
                m_PulseMaterial = m_MaterialHighlight.highlightMaterial;

                if (m_PulseMaterial != null)
                    m_StartingAlpha = m_PulseMaterial.GetFloat("_PulseMinAlpha");
            }
            if (m_OutlineHighlight != null)
                m_HighlightController.RegisterCacheUser(m_OutlineHighlight);

            m_HighlightController.Initialize();
            m_StartingWidth = m_OutlineHighlight.outlineScale;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Update()
        {
            UpdatePriorityHighlightingState();
            m_HighlightController.Update();
            if (m_MaterialHighlight != null)
            {
                // Do timer count up/count down
                if (m_PlayShine)
                {
                    m_ShineTimer += Time.deltaTime;

                    var shinePercent = Mathf.Clamp01(m_ShineTimer / k_ShineTime);
                    var shineValue = Mathf.PingPong(shinePercent, 0.5f) * 2.0f;

                    m_PulseMaterial.SetFloat("_PulseMinAlpha", Mathf.Lerp(m_StartingAlpha, 1f, shineValue));

                    if (shinePercent >= 1.0f)
                    {
                        m_PlayShine = false;
                        m_ShineTimer = 0.0f;
                    }
                }
            }
        }

        void UpdateHighlightState()
        {
            var shouldHighlight = false;

            if (isActivated)
                shouldHighlight = m_HighlightOnActivate;
            else
            {
                if (isSelected)
                    shouldHighlight = m_HighlightOnSelect;
                else if (isHovered)
                    shouldHighlight = m_HighlightOnHover || (m_HighlightOnHoverPriority && m_PriorityHighlightingState == PriorityHighlightingState.Highlighted);
            }

            if (shouldHighlight == m_Highlighting)
                return;

            m_Highlighting = shouldHighlight;

            if (m_Highlighting)
                m_HighlightController.Highlight();
            else
                m_HighlightController.Unhighlight();
        }

        void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor)
                return;

            m_HoveredCount++;

            if (isSelected)
                return;

            if (m_AudioHover != null)
                m_AudioHover.Play();

            if (m_MaterialHighlight != null)
                m_PulseMaterial.color = m_HoverColor;

            if (m_OutlineHighlight != null)
                m_OutlineHighlight.outlineColor = m_HoverColor;

            m_PriorityHighlightingState = PriorityHighlightingState.Unknown;
            UpdateHighlightState();
        }

        void OnHoverExited(HoverExitEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor)
                return;

            m_HoveredCount--;
            m_PriorityHighlightingState = PriorityHighlightingState.Unknown;
            UpdateHighlightState();
        }

        bool HasValidInteractor(List<IXRTargetPriorityInteractor> interactors)
        {
            foreach (var interactor in interactors)
            {
                if (!(interactor is XRSocketInteractor))
                    return true;
            }
            return false;
        }

        void UpdatePriorityHighlightingState()
        {
            if (!m_HighlightOnHoverPriority || !isHovered || isSelected)
                return;

            var manager = m_Interactable.interactionManager;
            if (manager == null)
                return;

            var highestPriorityForSelection = manager.IsHighestPriorityTarget(m_Interactable, s_InteractorList);
            if (!HasValidInteractor(s_InteractorList))
                return;

            if (highestPriorityForSelection && m_PriorityHighlightingState != PriorityHighlightingState.Highlighted)
            {
                m_PriorityHighlightingState = PriorityHighlightingState.Highlighted;

                if (m_PulseMaterial != null)
                    m_PulseMaterial.color = m_HoverPriorityColor;

                if (m_OutlineHighlight != null)
                    m_OutlineHighlight.outlineColor = m_HoverPriorityColor;

                UpdateHighlightState();
            }

            if (!highestPriorityForSelection && m_PriorityHighlightingState != PriorityHighlightingState.Unhighlighted)
            {
                m_PriorityHighlightingState = PriorityHighlightingState.Unhighlighted;

                if (m_PulseMaterial != null)
                    m_PulseMaterial.color = m_HoverColor;

                if (m_OutlineHighlight != null)
                    m_OutlineHighlight.outlineColor = m_HoverColor;

                UpdateHighlightState();
            }
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor)
                return;

            if (m_AudioClick != null)
                m_AudioClick.Play();

            if (m_OutlineHighlight != null)
            {
                m_OutlineHighlight.outlineColor = m_SelectionColor;
                m_OutlineHighlight.PlayPulseAnimation();
            }

            if (m_MaterialHighlight != null)
                m_PulseMaterial.color = m_SelectionColor;

            m_SelectedCount++;
            UpdateHighlightState();
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor)
                return;

            if (m_OutlineHighlight != null)
                m_OutlineHighlight.outlineColor = m_HoverColor;
            if (m_MaterialHighlight != null)
                m_PulseMaterial.color = m_HoverColor;

            m_OutlineHighlight.PlayPulseAnimation();

            // In case the Interactable is dropped while activated.
            isActivated = false;
            m_SelectedCount--;
            m_PriorityHighlightingState = PriorityHighlightingState.Unknown;
            UpdateHighlightState();
        }

        void OnActivated(ActivateEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor)
                return;

            if (m_OutlineHighlight != null)
            {
                if (m_PlayMaterialActivateAnim)
                    m_PlayShine = true;

                if (m_PlayOutlineActivateAnim)
                {
                    m_OutlineHighlight.outlineScale = 1f;
                    m_OutlineHighlight.PlayPulseAnimation();
                }
            }

            isActivated = true;
            UpdateHighlightState();
        }

        void OnDeactivated(DeactivateEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor)
                return;

            if (m_OutlineHighlight != null)
            {
                if (m_PlayOutlineActivateAnim)
                {
                    m_OutlineHighlight.outlineScale = m_StartingWidth;
                    m_OutlineHighlight.PlayPulseAnimation();
                }
            }

            isActivated = false;
            UpdateHighlightState();
        }
    }
}
