using VRBuilder.Core.Behaviors;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.UI.Conditions
{
    /// <inheritdoc />
    public class SetParentMenuItem : MenuItem<IBehavior>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "Utility/Set Parent";

        /// <inheritdoc />
        public override IBehavior GetNewItem()
        {
            return new SetParentBehavior();
        }
    }
}
