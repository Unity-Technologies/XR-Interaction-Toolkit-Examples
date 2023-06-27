using UnityEngine;

namespace VRBuilder.BasicInteraction
{
    /// <summary>
    /// Adds highlighting functionality to a GameObject with Renderers.
    /// </summary>
    public interface IHighlighter
    {
        /// <summary>
        /// Returns true if there is this object is currently being highlighted.
        /// </summary>
        bool IsHighlighting { get; }
        
        /// <summary>
        /// Starts highlighting this object.
        /// </summary>
        /// <param name="highlightMaterial">Material to be applied as highlight.</param>
        void StartHighlighting(Material highlightMaterial);

        /// <summary>
        /// Stops highlighting this object.
        /// </summary>
        void StopHighlighting();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Material GetHighlightMaterial();
    }
}