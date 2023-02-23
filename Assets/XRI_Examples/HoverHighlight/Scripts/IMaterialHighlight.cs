namespace UnityEngine.XR.Content.Rendering
{
    /// <summary>
    /// Specifies how a material is applied to renderer for highlighting
    /// </summary>
    public enum MaterialHighlightMode
    {
        /// <summary>Adds a new material to the renderers materials array</summary>
        Layer,
        /// <summary>Replace the renderers materials with materials</summary>
        Replace,
    }

    /// <summary>
    /// Identifies a script as one that can apply a highlight to renderers
    /// </summary>
    public interface IMaterialHighlight
    {
        /// <summary>
        /// How a new material will be applied to the renderer's material array.
        /// </summary>
        MaterialHighlightMode highlightMode { get; set; }

        /// <summary>
        /// Material to use for highlighting
        /// </summary>
        Material highlightMaterial { get; }

        /// <summary>
        /// Used to set up any initial values or materials
        /// </summary>
        void Initialize();

        /// <summary>
        /// Used to remove any persistent objects
        /// </summary>
        void Deinitialize();

        /// <summary>
        /// Raised when a highlight operations has completed
        /// </summary>
        void OnHighlight();

        /// <summary>
        /// Raised when a un-highlight operations has completed
        /// </summary>
        /// <returns>A requested delay to transition out the highlight</returns>
        float OnUnhighlight();
    }
}
