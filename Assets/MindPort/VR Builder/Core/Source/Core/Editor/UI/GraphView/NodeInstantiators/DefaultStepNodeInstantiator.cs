using UnityEngine.UIElements;
using VRBuilder.Core;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Instantiator for a default <see cref="IStep"/> node.
    /// </summary>
    public class DefaultStepNodeInstantiator : IStepNodeInstantiator
    {
        /// <inheritdoc/>
        public string Name => "Step";

        /// <inheritdoc/>
        public bool IsInNodeMenu => true;

        /// <inheritdoc/>
        public string StepType => "default";

        /// <inheritdoc/>
        public int Priority => 100;

        /// <inheritdoc/>
        public DropdownMenuAction.Status GetContextMenuStatus(IEventHandler target, IChapter currentChapter)
        {
            return DropdownMenuAction.Status.Normal;
        }

        /// <inheritdoc/>
        public ProcessGraphNode InstantiateNode(IStep step)
        {
            return new StepGraphNode(step);
        }
    }
}
