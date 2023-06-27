using VRBuilder.BasicInteraction.Behaviors;
using VRBuilder.Core.Behaviors;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.BasicInteraction.UI.Behaviors
{
    /// <inheritdoc/>
    public class UnsnapMenuItem : MenuItem<IBehavior>
    {
        public override string DisplayedName { get; } = "Environment/Unsnap Object";

        public override IBehavior GetNewItem()
        {
            return new UnsnapBehavior();
        }
    }
}