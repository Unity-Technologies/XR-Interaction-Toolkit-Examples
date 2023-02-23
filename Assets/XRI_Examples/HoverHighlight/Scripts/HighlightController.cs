using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.XR.Content.Rendering
{
    /// <summary>
    /// The HighlightController manages scripts that highlight objects in some way - those that inherit from IMaterialHighlight
    /// It is in charge of locating all applicable renderers, and swapping/doing additional drawing passes as needed to represent the highlights
    /// </summary>
    [System.Serializable]
    public class HighlightController
    {
        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<Renderer> k_RendererComponents = new List<Renderer>();

        /// <summary>
        /// Holds additional data for renderers that need additional drawing passes
        /// This is specically MeshRenderers with more than one submesh - we can't just extend the material array
        /// to get additional drawing passes, so we use this data to draw them manually.
        /// </summary>
        class CustomHighlightLayer
        {
            internal Material[] m_HighlightMaterials;
            internal Mesh m_SharedMesh;
            internal Transform m_Transform;
        }

        [SerializeField]
        [Tooltip("Used to set the mode of capturing renderers on an object or to use only manually set renderers.")]
        RendererCaptureDepth m_RendererCaptureDepth = RendererCaptureDepth.AllChildRenderers;

        [SerializeField]
        [Tooltip("Manually set renderers to be affected by the highlight")]
        protected Renderer[] m_ManuallySetRenderers = new Renderer[0];

        // Cached data about any renderers that will be highlighted, and materials that are swapped in and out
        int m_MaterialAdditions = 0;

        List<IMaterialHighlight> m_CacheUsers = new List<IMaterialHighlight>();
        HashSet<Renderer> m_Renderers = new HashSet<Renderer>();
        Dictionary<int, Material[]> m_OriginalMaterials = new Dictionary<int, Material[]>();
        Dictionary<int, Material[]> m_HighlightMaterials = new Dictionary<int, Material[]>();
        Dictionary<int, CustomHighlightLayer> m_CustomLayerMaterials = new Dictionary<int, CustomHighlightLayer>();

        bool m_DelayedUnhighlight = false;
        bool m_Highlighting = false;
        float m_UnhighlightTimer = 0.0f;

        /// <summary>
        /// The transform that will be highlighted - it is searched for any child renderers
        /// </summary>
        public Transform rendererSource { get; set; }

        /// <summary>
        /// Registers a highlight script - this will provide materials to replace or layer when highlighting an object
        /// </summary>
        /// <param name="cacheUser">The highlight script to apply to the cached child renderers</param>
        public void RegisterCacheUser(IMaterialHighlight cacheUser)
        {
            if (cacheUser.highlightMode == MaterialHighlightMode.Layer)
                m_MaterialAdditions++;

            // Set cache user to know about this
            m_CacheUsers.Add(cacheUser);
        }

        /// <summary>
        /// Unregisters a highlight script so that it will no longer influence the cached renderers
        /// </summary>
        /// <param name="cacheUser">The highlight script to remove from influencing renderers</param>
        public void UnregisterCacheUser(IMaterialHighlight cacheUser)
        {
            if (cacheUser.highlightMode == MaterialHighlightMode.Layer)
                m_MaterialAdditions--;

            m_CacheUsers.Remove(cacheUser);
        }

        /// <summary>
        /// Ensures that all renderers, materials, and highlight scripts have their materials ready
        /// </summary>
        public void Initialize()
        {
            if (rendererSource == null)
            {
                Debug.LogError("Trying to use a Highlight Controller before setting the root gameobject!");
                return;
            }

            foreach (var cacheUser in m_CacheUsers)
            {
                cacheUser.Initialize();
            }

            // Cache the renderers
            UpdateRendererCache();

            // Generate the original material list and implement the materials from the included highlights
            UpdateMaterialCache();
        }

        /// <summary>
        /// Ensures that any materials or other objects allocated by highlight scripts can be cleaned up
        /// </summary>
        public void Deinitialize()
        {
            foreach (var cacheUser in m_CacheUsers)
            {
                if (cacheUser != null)
                    cacheUser.Deinitialize();
            }
        }

        /// <summary>
        /// Handles fading out materials when highlights are disabled and also manually drawing layered highlights as needed
        /// </summary>
        public void Update()
        {
            if (m_DelayedUnhighlight)
            {
                m_UnhighlightTimer -= Time.deltaTime;
                if (m_UnhighlightTimer <= 0.0f)
                {
                    m_DelayedUnhighlight = false;
                    m_Highlighting = false;
                    UpdateMaterialCache();
                    foreach (var renderer in m_Renderers)
                    {
                        var rendererID = renderer.GetInstanceID();
                        renderer.materials = m_OriginalMaterials[rendererID];
                    }
                }
            }
            if (m_Highlighting && m_CustomLayerMaterials.Count > 0)
            {
                foreach (var customLayer in m_CustomLayerMaterials.Values)
                {
                    for (var matIndex = 0; matIndex < customLayer.m_HighlightMaterials.Length; ++matIndex)
                    {
                        for (var submeshIndex = 0; submeshIndex < customLayer.m_SharedMesh.subMeshCount; ++submeshIndex)
                        {
                            Graphics.DrawMesh(
                                customLayer.m_SharedMesh,
                                customLayer.m_Transform.localToWorldMatrix,
                                customLayer.m_HighlightMaterials[matIndex],
                                customLayer.m_Transform.gameObject.layer,
                                null,
                                submeshIndex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Applies highlight materials to all the targeted renderers
        /// </summary>
        public void Highlight()
        {
            m_DelayedUnhighlight = false;
            m_Highlighting = true;
            UpdateMaterialCache();
            foreach (var renderer in m_Renderers)
            {
                var rendererID = renderer.GetInstanceID();
                renderer.materials = m_HighlightMaterials[rendererID];
            }
            foreach (var cacheUser in m_CacheUsers)
            {
                if (cacheUser != null)
                    cacheUser.OnHighlight();
            }
        }

        /// <summary>
        /// Restores the original materials to all the targeted renderers
        /// </summary>
        /// <param name="force">If true, the original materials are restored instantly.  Otherwise, a fade can occur.</param>
        public void Unhighlight(bool force = false)
        {
            UpdateMaterialCache();

            var maxDelay = 0.0f;
            foreach (var cacheUser in m_CacheUsers)
            {
                if (cacheUser != null)
                    maxDelay = Mathf.Max(cacheUser.OnUnhighlight());
            }

            if (maxDelay <= 0.0f)
            {
                foreach (var renderer in m_Renderers)
                {
                    var rendererID = renderer.GetInstanceID();
                    renderer.materials = m_OriginalMaterials[rendererID];
                }
                m_Highlighting = false;
            }
            else
            {
                m_DelayedUnhighlight = true;
                m_UnhighlightTimer = maxDelay;
            }
        }

        void UpdateRendererCache()
        {
            m_Renderers.Clear();
            m_Renderers.UnionWith(m_ManuallySetRenderers.Where(r => r != null));

            switch (m_RendererCaptureDepth)
            {
                case RendererCaptureDepth.AllChildRenderers:
                    rendererSource.GetComponentsInChildren(true, k_RendererComponents);

                    foreach (var renderer in k_RendererComponents)
                    {
                        var textMesh = renderer.GetComponent<TextMesh>();
                        var meshFilter = renderer.GetComponent<MeshFilter>();

                        if (textMesh == null)
                            m_Renderers.Add(renderer);

                        if (meshFilter != null && meshFilter.mesh.subMeshCount > 1)
                            m_CustomLayerMaterials.Add(renderer.GetInstanceID(), new CustomHighlightLayer { m_SharedMesh = meshFilter.sharedMesh, m_Transform = renderer.transform });
                    }
                    k_RendererComponents.Clear();

                    break;
                case RendererCaptureDepth.CurrentRenderer:
                    rendererSource.GetComponents(k_RendererComponents);

                    foreach (var renderer in k_RendererComponents)
                    {
                        var textMesh = renderer.GetComponent<TextMesh>();
                        var meshFilter = renderer.GetComponent<MeshFilter>();

                        if (textMesh == null)
                            m_Renderers.Add(renderer);

                        if (meshFilter != null && meshFilter.mesh.subMeshCount > 1)
                            m_CustomLayerMaterials.Add(renderer.GetInstanceID(), new CustomHighlightLayer { m_SharedMesh = meshFilter.sharedMesh, m_Transform = renderer.transform });
                    }
                    k_RendererComponents.Clear();
                    break;
                case RendererCaptureDepth.ManualOnly:
                    break;
                default:
                    Debug.LogError($"{rendererSource.name} highlight has an invalid renderer capture mode {m_RendererCaptureDepth}.", rendererSource);
                    break;
            }

            if (m_Renderers.Count == 0)
                Debug.LogWarning($"{rendererSource.name} highlight has no renderers set.", rendererSource);
        }

        void UpdateMaterialCache()
        {
            foreach (var renderer in m_Renderers)
            {
                var rendererID = renderer.GetInstanceID();
                if (m_OriginalMaterials.ContainsKey(rendererID))
                    continue;

                var sharedMaterials = renderer.sharedMaterials;
                var sharedLength = sharedMaterials.Length;
                m_OriginalMaterials[rendererID] = sharedMaterials;

                CustomHighlightLayer highlightLayer = null;
                Material[] highlightMaterials;
                Material[] layerMaterials;
                var addOffset = sharedLength;

                if (m_CustomLayerMaterials.TryGetValue(rendererID, out highlightLayer))
                {
                    highlightMaterials = new Material[sharedLength];
                    highlightLayer.m_HighlightMaterials = new Material[m_MaterialAdditions];
                    layerMaterials = highlightLayer.m_HighlightMaterials;
                    addOffset = 0;
                }
                else
                {
                    highlightMaterials = new Material[sharedLength + m_MaterialAdditions];
                    layerMaterials = highlightMaterials;
                }

                for (var matIndex = 0; matIndex < sharedLength; matIndex++)
                {
                    highlightMaterials[matIndex] = sharedMaterials[matIndex];
                }

                for (var i = 0; i < m_CacheUsers.Count; i++)
                {
                    var cacheUser = m_CacheUsers[i];
                    if (cacheUser.highlightMode == MaterialHighlightMode.Replace)
                    {
                        for (var matIndex = 0; matIndex < sharedLength; matIndex++)
                        {
                            highlightMaterials[matIndex] = cacheUser.highlightMaterial;
                        }
                    }
                    else
                    {
                        layerMaterials[addOffset] = cacheUser.highlightMaterial;
                        addOffset++;
                    }
                }
                m_HighlightMaterials[rendererID] = highlightMaterials;
            }
        }
    }
}
