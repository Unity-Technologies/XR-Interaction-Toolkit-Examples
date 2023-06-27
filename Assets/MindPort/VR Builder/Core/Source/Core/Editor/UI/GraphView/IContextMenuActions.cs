using UnityEngine.UIElements;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Interface for a process graph object having custom context menu actions.
    /// </summary>
    public interface IContextMenuActions
    {
        /// <summary>
        /// Adds custom actions to the context menu.
        /// </summary>        
        void AddContextMenuActions(DropdownMenu menu);
    }
}
