using VRBuilder.BasicInteraction;
using UnityEngine;

namespace VRBuilder.XRInteraction
{
    /// <summary>
    /// Draws a preview of SnapZone highlight.
    /// </summary>
    [ExecuteInEditMode]
    public class SnapZonePreviewDrawer : MonoBehaviour, IExcludeFromHighlightMesh
    {
        /// <summary>
        /// The parent SnapZone.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private SnapZone parent;

        private MeshFilter filter;
        private MeshRenderer meshRenderer;
        
        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                DestroyPreview();
                DestroyImmediate(this);
                return;
            }
            
            filter = gameObject.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = gameObject.AddComponent<MeshFilter>();
            }

            meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            if (parent == null)
            {
                parent = transform.parent.GetComponent<SnapZone>();
                if (parent == null)
                {
                    DestroyPreview();
                    return;
                }
            }

            if (filter.sharedMesh == null)
            {
                filter.sharedMesh = parent.PreviewMesh;
                meshRenderer.material = parent.HighlightMeshMaterial;
            }
        }

        private void Update()
        {
            meshRenderer.enabled = parent.ShowHighlightInEditor;
        }

        private void DestroyPreview()
        {
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                DestroyImmediate(meshRenderer);
            }
            
            filter = gameObject.GetComponent<MeshFilter>();
            if (filter != null)
            {
                DestroyImmediate(filter);
            }
            
            DestroyImmediate(this);
        }

        /// <summary>
        /// Forces an update of the mesh.
        /// </summary>
        public void UpdateMesh()
        {
            filter.sharedMesh = parent.PreviewMesh;
            meshRenderer.material = parent.HighlightMeshMaterial;
        }
    }
}