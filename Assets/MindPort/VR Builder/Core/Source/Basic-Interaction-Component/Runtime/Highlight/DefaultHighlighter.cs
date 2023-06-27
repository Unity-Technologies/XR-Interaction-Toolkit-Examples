using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;



namespace VRBuilder.BasicInteraction
{
    /// <inheritdoc cref="VRBuilder.BasicInteraction.IHighlighter" />
    /// <remarks>
    /// Highlights are always queued following a LIFO (Last In First Out) scheme. 
    /// </remarks>
    [DisallowMultipleComponent]
    public class DefaultHighlighter : AbstractHighlighter
    {
        private class HighlightInfoList
        {
            private readonly List<KeyValuePair<string, Material>> list = new List<KeyValuePair<string, Material>>();

            public void Add(string name, Material material)
            {
                list.Add(new KeyValuePair<string, Material>(name, material));
            }

            public void Clear()
            {
                list.Clear();
            }

            public bool Any()
            {
                return list.Count > 0;
            }

            public bool Remove(string key)
            {
                KeyValuePair<string, Material> info = GetHitInfo(key);
                return list.Remove(info);
            }

            public bool ContainsKey(string key)
            {
                return list.Any(info => info.Key == key);
            }

            public KeyValuePair<string, Material> GetLastItem()
            {
                if (list.Count > 0)
                {
                    return list[list.Count - 1];
                }
            
                return new KeyValuePair<string, Material>(string.Empty, null);
            }
            
            private KeyValuePair<string, Material> GetHitInfo(string key)
            {
                return list.First(info => info.Key == key);
            }
        }
        
        /// <inheritdoc/>
        public override bool IsHighlighting => activeHighlights.Any();
        
        private readonly HighlightInfoList activeHighlights = new HighlightInfoList();

        protected virtual void Reset()
        {
            RefreshCachedRenderers();
        }

        protected virtual void OnDisable()
        {
            if (IsHighlighting)
            {
                ReenableRenderers();
                activeHighlights.Clear();
            }
        }

        /// <summary>
        /// Highlights this object with given <paramref name="highlightColor"/>.
        /// </summary>
        /// <remarks>Every highlight requires an ID to avoid duplications.</remarks>
        /// <returns>An ID corresponding to the highlight, should be used in <see cref="StopHighlighting"/>.</returns>
        public string StartHighlighting(Color highlightColor, string highlightID)
        {
            Material highlightMaterial = CreateHighlightMaterial(highlightColor);
            return Highlight(highlightMaterial, highlightID);
        }
        
        /// <summary>
        /// Highlights this object with given <paramref name="highlightMaterial"/>.
        /// </summary>
        /// <remarks>Every highlight requires an ID to avoid duplications.</remarks>
        /// <returns>An ID corresponding to the highlight, should be used in <see cref="StopHighlighting"/>.</returns>
        public string StartHighlighting(Material highlightMaterial, string highlightID)
        {
            return Highlight(highlightMaterial, highlightID);
        }
        
        /// <summary>
        /// Highlights this object with given <paramref name="highlightTexture"/>.
        /// </summary>
        /// <remarks>Every highlight requires an ID to avoid duplications.</remarks>
        /// <returns>An ID corresponding to the highlight, should be used in <see cref="StopHighlighting"/>.</returns>
        public string StartHighlighting(Texture highlightTexture, string highlightID)
        {
            Material highlightMaterial = CreateHighlightMaterial(highlightTexture);
            return Highlight(highlightMaterial, highlightID);
        }
        
        /// <inheritdoc/>
        public override void StartHighlighting(Material highlightMaterial)
        {
            string highlightID = Guid.NewGuid().ToString();
            StartHighlighting(highlightMaterial, highlightID);
        }

        /// <inheritdoc/>
        public override void StopHighlighting()
        {
            activeHighlights.Clear();
            ReenableRenderers();
        }
        
        /// <summary>
        /// Stops a highlight of given <paramref name="highlightID"/>.
        /// </summary>
        public void StopHighlighting(string highlightID)
        {
            if (activeHighlights.ContainsKey(highlightID))
            {
                activeHighlights.Remove(highlightID);

                if (activeHighlights.Any())
                {
                    KeyValuePair<string, Material> activeHighlight = activeHighlights.GetLastItem();
                    highlightMeshRenderer.sharedMaterial = activeHighlight.Value;
                }
                else
                {
                    ReenableRenderers();
                }
            }
        }

        /// <inheritdoc/>
        public override Material GetHighlightMaterial()
        {
            Material highlightMaterial = null;
            
            if (activeHighlights.Any())
            {
                highlightMaterial = activeHighlights.GetLastItem().Value;
            }

            return highlightMaterial;
        }

        private string Highlight(Material highlightMaterial, string highlightID)
        {
            if (CanObjectBeHighlighted() == false)
            {
                return highlightID;
            }

            if (highlightMeshRenderer == null || renderers == null || renderers.Length == 0)
            {
                RefreshCachedRenderers();
            }
            
            if (activeHighlights.ContainsKey(highlightID) == false)
            {
                activeHighlights.Add(highlightID, highlightMaterial);
            }
            
            DisableRenders();
            highlightMeshRenderer.sharedMaterial = highlightMaterial;
            
            return highlightID;
        }

        /// <summary>
        /// Regenerates the cached renderers.
        /// </summary>
        public void ForceRefreshCachedRenderers()
        {
            if (IsHighlighting)
            {
                return;
            }
            
            ReenableRenderers();

            if (Application.isPlaying && gameObject.isStatic)
            {
                return;
            }
            
            ClearCacheRenderers();
            RefreshCachedRenderers();
        }

        protected void DisableRenders()
        {
            if (highlightMeshRenderer != null)
            {
                highlightMeshRenderer.enabled = true;
                highlightMeshRenderer.gameObject.SetActive(true);
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }

        protected void ReenableRenderers()
        {
            if (highlightMeshRenderer != null)
            {
                highlightMeshRenderer.enabled = false;
                highlightMeshRenderer.gameObject.SetActive(false);
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }
        }
    }
}
