using System;
using System.Linq;
using System.Collections.Generic;
using VRBuilder.Core.Properties;
using UnityEngine;
using UnityEngine.Rendering;
using VRBuilder.Unity;

namespace VRBuilder.BasicInteraction
{
    /// <summary>
    /// Collects render information from a <see cref="IHighlighter"/> object and provides basic utilities for highlighting. 
    /// </summary>
    public abstract class AbstractHighlighter : MonoBehaviour, IHighlighter
    {
        [SerializeField, HideInInspector]
        protected Renderer[] renderers = {};
        
        [SerializeField, HideInInspector]
        protected MeshRenderer highlightMeshRenderer = null;
        
        [SerializeField , HideInInspector]
        protected MeshFilter highlightMeshFilter = null;
        
        /// <inheritdoc/>
        public abstract bool IsHighlighting { get; }

        /// <inheritdoc/>
        public abstract void StartHighlighting(Material highlightMaterial);

        /// <inheritdoc/>
        public abstract void StopHighlighting();
        
        /// <inheritdoc/>
        public abstract Material GetHighlightMaterial();

        protected void ClearCacheRenderers()
        {
            renderers = default;
        }

        protected void RefreshCachedRenderers()
        {
            if (highlightMeshRenderer != null && renderers != null && renderers.Any())
            {
                return;
            }
            
            if (highlightMeshRenderer == null)
            {
                GenerateHighlightRenderer();
            }
            else
            {
                highlightMeshRenderer.enabled = false;
                highlightMeshRenderer.gameObject.SetActive(false);
            }

            renderers = HighlightUtils.FindAllIncludedRenderer(gameObject);

            if (renderers == null || renderers.Any() == false)
            {
                throw new NullReferenceException($"{name} has no renderers to be highlighted.");
            }

            highlightMeshFilter.mesh = HighlightUtils.GeneratePreviewMesh(gameObject, renderers);
        }

        private void GenerateHighlightRenderer()
        {
            Transform child = transform.Find("Highlight Renderer");

            if (child == null)
            {
                child = new GameObject("Highlight Renderer").transform;
            }
            
            child.SetPositionAndRotation(transform.position, transform.rotation);
            child.SetParent(transform);
            
            highlightMeshFilter = child.gameObject.GetOrAddComponent<MeshFilter>();
            highlightMeshRenderer = child.gameObject.GetOrAddComponent<MeshRenderer>();

            highlightMeshRenderer.enabled = false;
            highlightMeshRenderer.gameObject.SetActive(false);
        }

        /// <summary>
        /// Creates a highlight material with given color.
        /// </summary>
        protected virtual Material CreateHighlightMaterial(Color highlightColor)
        {
            return HighlightUtils.CreateDefaultHighlightMaterial(highlightColor);
        }
        
        /// <summary>
        /// Creates a highlight material with given texture.
        /// </summary>
        protected virtual Material CreateHighlightMaterial(Texture mainTexture)
        {
            Shader shader = HighlightUtils.GetDefaultHighlightShader();
            Material material = new Material(shader) {mainTexture = mainTexture};
            return material;
        }

        protected virtual bool CanObjectBeHighlighted()
        {
            if (enabled == false)
            {
                Debug.LogError($"{GetType().Name} component is disabled for {name} and can not be highlighted.", gameObject);
                return false;
            }
            
            if (gameObject.activeInHierarchy == false)
            {
                Debug.LogError($"{name} is disabled and can not be highlighted.", gameObject);
                return false;
            }

            return true;
        }
    }
}
