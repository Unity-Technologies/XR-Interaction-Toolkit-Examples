using UnityEngine.UIElements;
using VRBuilder.Core;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Instantiates a node matching the 
    /// </summary>
    public interface IStepNodeInstantiator
    {
        /// <summary>
        /// Display name of the instantiated node.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// If true, it will appear in the node menu.
        /// </summary>
        bool IsInNodeMenu { get; }

        /// <summary>
        /// Nodes with a lower value will appear first in the menu.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Step type metadata.
        /// </summary>
        string StepType { get; }

        /// <summary>
        /// Creates a graphview node of the corresponding type. 
        /// </summary>
        ProcessGraphNode InstantiateNode(IStep step);

        /// <summary>
        /// Returns the status for the context menu entry to instantiate the node.
        /// </summary>
        DropdownMenuAction.Status GetContextMenuStatus(IEventHandler target, IChapter currentChapter);
    }
}
