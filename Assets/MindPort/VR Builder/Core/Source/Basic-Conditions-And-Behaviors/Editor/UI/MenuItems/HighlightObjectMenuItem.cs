using VRBuilder.Core.Behaviors;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.UI.Behaviors
{
    /// <inheritdoc />
    public class HighlightObjectMenuItem : MenuItem<IBehavior>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "Guidance/Highlight Object";

        /// <inheritdoc />
        public override IBehavior GetNewItem()
        {
            return new HighlightObjectBehavior();
        }
    }
}
