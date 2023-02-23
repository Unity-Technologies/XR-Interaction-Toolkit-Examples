namespace UnityEngine.XR.Content.Rendering
{
    /// <summary>
    /// Draws an outline on an object when highlighting. Can either transition the color and or size of the out line
    /// as selected or be instant on.
    /// </summary>
    public class OutlineHighlight : MonoBehaviour, IMaterialHighlight
    {
        enum OutlineSource
        {
            Shader = 0,
            Material
        }

        const float k_OutlineWidth = 0.005f;
        const string k_ShaderColorParameter = "_Color";
        const string k_ShaderWidthParameter = "g_flOutlineWidth";
        static readonly int k_GFlOutlineWidth = Shader.PropertyToID(k_ShaderWidthParameter);
        static readonly int k_Color = Shader.PropertyToID(k_ShaderColorParameter);

#pragma warning disable 649
        [SerializeField]
        [Tooltip("How the highlight material will be applied to the renderer's material array.")]
        MaterialHighlightMode m_HighlightMode = MaterialHighlightMode.Replace;

        [SerializeField]
        [Tooltip("Selects source for the highlight material. Either using a shader or material.")]
        OutlineSource m_OutlineSource = OutlineSource.Shader;

        [SerializeField]
        [Tooltip("Outline highlight shader to use for highlight material.")]
        Shader m_Shader;

        [SerializeField]
        [Tooltip("Material used for drawing the outline highlight.")]
        Material m_HighlightMaterial;

        [SerializeField]
        [Tooltip("Transition outline width over time")]
        bool m_TransitionWidth;

        [SerializeField]
        [Tooltip("The outline width used if no transition or the end value for transition width of outline")]
        [Range(0f, 1f)]
        float m_OutlineScale = 1f;

        [SerializeField]
        [Tooltip("Starting value for transition width of outline")]
        [Range(0f, 1f)]
        float m_StartingOutlineScale;

        [SerializeField]
        [Tooltip("Transition outline color over time")]
        bool m_TransitionColor;

        [SerializeField]
        [Tooltip("The outline color used if no transition or the end value for transition color of outline")]
        Color m_OutlineColor = new Color(0.3f, 0.6f, 1f, 1f);

        [SerializeField]
        [Tooltip("Starting value for transition color of outline")]
        Color m_StartingOutlineColor = Color.black;

        [SerializeField]
        [Tooltip("Time it takes to transition from start to end on highlight")]
        float m_TransitionDuration = 0.3f;

        [SerializeField]
        [Tooltip("Use material values for starting color and width")]
        bool m_StartWithMaterialValues;
#pragma warning restore 649

        Material m_InstanceOutlineMaterial;

        bool m_Animating = false;
        bool m_AnimatingIn = false;
        float m_TransitionTimer = 0.0f;

        /// <summary>
        /// Time it takes to transition from start to end on highlight
        /// </summary>
        public float transitionDuration
        {
            get => m_TransitionDuration;
            set => m_TransitionDuration = value;
        }

        /// <summary>
        /// Transition outline width over time
        /// </summary>
        public bool transitionWidth
        {
            get => m_TransitionWidth;
            set => m_TransitionWidth = value;
        }

        /// <summary>
        /// Transition outline color over time
        /// </summary>
        public bool transitionColor
        {
            get => m_TransitionColor;
            set => m_TransitionColor = value;
        }

        /// <summary>
        /// The outline color used if no transition or the end value for transition color of outline
        /// </summary>
        public Color outlineColor
        {
            get => m_OutlineColor;
            set => m_OutlineColor = value;
        }

        /// <summary>
        /// Starting value for transition color of outline
        /// </summary>
        public Color startingOutlineColor
        {
            get => m_StartingOutlineColor;
            set => m_StartingOutlineColor = value;
        }

        /// <summary>
        /// The outline width used if no transition or the end value for transition width of outline
        /// </summary>
        public float outlineScale
        {
            get => m_OutlineScale;
            set => m_OutlineScale = value;
        }

        /// <summary>
        /// Starting value for transition width of outline
        /// </summary>
        public float startingOutlineScale
        {
            get => m_StartingOutlineScale;
            set => m_StartingOutlineScale = value;
        }

        /// <summary>
        /// A 0-1 relative outline scale that takes into account the ideal base outline width,
        /// multiplied by the user specified value. This allows for more intuitive adjustment of the value.
        /// This is the value used if there is no transition otherwise this is the end value of a transition.
        /// </summary>
        float relativeOutlineScale => outlineScale * k_OutlineWidth;

        /// <summary>
        /// A 0-1 relative outline scale that takes into account the ideal base outline width,
        /// multiplied by the user specified value. This allows for more intuitive adjustment of the value.
        /// This is the start value of a transition otherwise this value is not used.
        /// </summary>
        float startingRelativeOutlineScale => startingOutlineScale * k_OutlineWidth;

        /// <summary>
        /// How the highlight material will be applied to the renderer's material array.
        /// </summary>
        public MaterialHighlightMode highlightMode
        {
            get => m_HighlightMode;
            set => m_HighlightMode = value;
        }

        /// <summary>
        /// Material to use for highlighting
        /// </summary>
        public Material highlightMaterial => m_InstanceOutlineMaterial;

        void IMaterialHighlight.Initialize()
        {
            InstantiateHighlightMaterial();

            if (m_StartWithMaterialValues)
            {
                startingOutlineScale = m_HighlightMaterial.GetFloat(k_GFlOutlineWidth) / k_OutlineWidth;
                startingOutlineColor = m_HighlightMaterial.GetColor(k_Color);
            }
        }

        void IMaterialHighlight.Deinitialize()
        {
            if (m_InstanceOutlineMaterial)
            {
                Destroy(m_InstanceOutlineMaterial);
                m_InstanceOutlineMaterial = null;
            }
        }

        protected void OnDestroy()
        {
            ((IMaterialHighlight)(this)).Deinitialize();
        }

        protected void Update()
        {
            if (m_Animating)
            {
                m_TransitionTimer += Time.unscaledDeltaTime;

                var transitionPercent = Mathf.Clamp01(m_TransitionTimer / m_TransitionDuration);
                var alpha = m_AnimatingIn ? transitionPercent : (1.0f - transitionPercent);

                if (m_TransitionWidth)
                {
                    var size = Mathf.Lerp(startingRelativeOutlineScale, relativeOutlineScale, alpha);
                    m_InstanceOutlineMaterial.SetFloat(k_GFlOutlineWidth, size);
                }

                if (m_TransitionColor)
                {
                    var color = Color.Lerp(startingOutlineColor, outlineColor, alpha);
                    m_InstanceOutlineMaterial.SetColor(k_Color, color);
                }

                if (transitionPercent >= 1.0f)
                {
                    m_TransitionTimer = 0.0f;
                    m_Animating = false;
                }
            }
        }

        void IMaterialHighlight.OnHighlight()
        {
            if (m_InstanceOutlineMaterial == null)
                return;

            PlayPulseAnimation();
        }

        float IMaterialHighlight.OnUnhighlight()
        {
            if (m_InstanceOutlineMaterial == null)
                return 0.0f;

            PlayPulseAnimation(false);

            if (!m_TransitionWidth && !m_TransitionColor || Mathf.Approximately(m_TransitionDuration, 0f))
                return 0.0f;
            else
                return m_TransitionDuration;
        }

        /// <summary>
        /// Pulses the highlight - even if it is already active
        /// </summary>
        /// <param name="pulseUp">Whether the highlight is fading in or out</param>
        public void PlayPulseAnimation(bool pulseUp = true)
        {
            if (!m_TransitionWidth && !m_TransitionColor || Mathf.Approximately(m_TransitionDuration, 0f))
            {
                m_InstanceOutlineMaterial.SetFloat(k_GFlOutlineWidth, relativeOutlineScale);
                m_InstanceOutlineMaterial.SetColor(k_Color, m_OutlineColor);
            }
            else
            {
                // If the same animation is already occurring, we just let it play.  If it is playing backwards, we seamlessly transition
                if (m_Animating)
                {
                    if (m_AnimatingIn != pulseUp)
                    {
                        m_TransitionTimer = 1.0f - m_TransitionTimer;
                    }
                }
                else
                {
                    m_Animating = true;
                    m_AnimatingIn = pulseUp;
                    m_TransitionTimer = 0.0f;
                }
            }
        }

        void InstantiateHighlightMaterial()
        {
            if (m_Shader == null && m_HighlightMaterial == null)
            {
                Debug.LogError($"{gameObject.name} has no highlight material or shader set!", this);
                enabled = false;
                return;
            }

            const string outlineMaterialName = "Outline Material Instance";

            switch (m_OutlineSource)
            {
                case OutlineSource.Material:
                    if (m_HighlightMaterial == null)
                    {
                        Debug.LogError($"{gameObject.name} Outline highlight has no material assigned. Please assign outline material.", this);
                        enabled = false;
                        break;
                    }

                    m_InstanceOutlineMaterial = new Material(m_HighlightMaterial) { name = outlineMaterialName };
                    break;
                case OutlineSource.Shader:
                    if (m_Shader == null)
                    {
                        Debug.LogError($"{gameObject.name} Outline highlight has no shader assigned. Please assign outline shader. ", this);
                        enabled = false;
                        break;
                    }

                    m_InstanceOutlineMaterial = new Material(m_Shader) { name = outlineMaterialName };
                    break;
                default:
                    Debug.LogError($"{gameObject.name} Outline highlight has an invalid highlight mode {m_OutlineSource}.", this);
                    enabled = false;
                    break;
            }
        }

        protected void OnValidate()
        {
            if (m_TransitionDuration < 0f)
            {
                m_TransitionDuration = 0f;
            }
        }
    }
}
