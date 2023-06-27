using UnityEngine;

namespace VRBuilder.BasicInteraction
{
    /// <summary>
    /// Can be added to GameObjects to exclude them from automatically generated highlights.
    /// If you want to add this to your MonoBehaviour, use <see cref="IExcludeFromHighlightMesh"/>
    /// </summary>
    public sealed class ExcludeFromHighlightMesh : MonoBehaviour, IExcludeFromHighlightMesh
    {
        
    }
}
