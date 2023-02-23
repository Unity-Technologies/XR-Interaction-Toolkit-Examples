namespace UnityEngine.XR.Content.Rendering
{
    /// <summary>
    /// Specifies how a transform's hierarchy is traversed to locate renderers to highlight
    /// </summary>
    public enum RendererCaptureDepth
    {
        /// <summary>Get all active renders on an object, its children and manually set renderers.</summary>
        AllChildRenderers,
        /// <summary>Get all active renders on an object and manually set renderers. Ignores children.</summary>
        CurrentRenderer,
        /// <summary>Only uses manually set renderers.</summary>
        ManualOnly,
    }
}
