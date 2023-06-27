using VRBuilder.Core.Behaviors;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.UI.Behaviors
{
    /// <inheritdoc />
    public class DelayMenuItem : MenuItem<IBehavior>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "Utility/Delay";

        /// <inheritdoc />
        public override IBehavior GetNewItem()
        {
            return new DelayBehavior();
        }
    }
}
