using UnityEngine.UIElements;
using VRBuilder.Core;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Instantiator for the End Chapter node.
    /// </summary>
    public class EndChapterNodeInstantiator : IStepNodeInstantiator
    {
        /// <inheritdoc/>
        public string Name => "End Chapter";

        /// <inheritdoc/>
        public bool IsInNodeMenu => true;

        /// <inheritdoc/>
        public int Priority => 150;

        /// <inheritdoc/>
        public string StepType => "endChapter";

        /// <inheritdoc/>
        public DropdownMenuAction.Status GetContextMenuStatus(IEventHandler target, IChapter currentChapter)
        {
            if(GlobalEditorHandler.GetCurrentProcess().Data.Chapters.Contains(currentChapter))
            {
                return DropdownMenuAction.Status.Normal;
            }
            else
            {
                return DropdownMenuAction.Status.Disabled;
            }
        }

        /// <inheritdoc/>
        public ProcessGraphNode InstantiateNode(IStep step)
        {
            return new EndChapterNode(step);
        }
    }
}
