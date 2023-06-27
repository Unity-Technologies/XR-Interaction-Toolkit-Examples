using VRBuilder.Core.Behaviors;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.UI.Behaviors
{
    /// <inheritdoc />
    public class DisableGameObjectMenuItem : MenuItem<IBehavior>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "Environment/Disable Objects/By Reference";

        /// <inheritdoc />
        public override IBehavior GetNewItem()
        {
            return new DisableGameObjectBehavior();
        }
    }
}
