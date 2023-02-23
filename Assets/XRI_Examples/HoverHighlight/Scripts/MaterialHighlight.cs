namespace UnityEngine.XR.Content.Rendering
{
    /// <summary>
    /// Used to change the materials array of an object when highlighted. Can either add the highlight material to the
    /// renderers materials array or replace the renderers materials with the highlight material.
    /// </summary>
    public class MaterialHighlight : MonoBehaviour, IMaterialHighlight
    {
        [SerializeField]
        [Tooltip("How the highlight material will be applied to the renderer's material array.")]
        MaterialHighlightMode m_HighlightMode = MaterialHighlightMode.Replace;

        [SerializeField, Tooltip("Material to use for highlighting. The assigned material will be instantiated and used for highlighting.")]
        Material m_HighlightMaterial;

        Material m_InstanceHighlightMaterial;

        /// <summary>
        /// How the highlight material will be applied to the renderer's material array.
        /// </summary>
        public MaterialHighlightMode highlightMode
        {
            get => m_HighlightMode;
            set => m_HighlightMode = value;
        }

        /// <summary>
        /// Material to use for highlighting. The assigned material will be instantiated and used for highlighting.
        /// </summary>
        public Material highlightMaterial
        {
            get => m_HighlightMaterial;
            set => m_HighlightMaterial = value;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Awake()
        {
            if (m_HighlightMaterial == null)
                return;

            m_InstanceHighlightMaterial = Instantiate(m_HighlightMaterial);
            m_HighlightMaterial = m_InstanceHighlightMaterial;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnDestroy()
        {
            if (m_InstanceHighlightMaterial != null)
                Destroy(m_InstanceHighlightMaterial);
        }

        /// <inheritdoc />
        void IMaterialHighlight.Initialize()
        {
        }

        /// <inheritdoc />
        void IMaterialHighlight.Deinitialize()
        {
        }

        /// <inheritdoc />
        void IMaterialHighlight.OnHighlight()
        {
        }

        /// <inheritdoc />
        float IMaterialHighlight.OnUnhighlight() => 0f;
    }
}
