using VRBuilder.Core.Behaviors;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.UI.Behaviors
{
    /// <inheritdoc />
    public class DisableComponentByTagMenuItem : MenuItem<IBehavior>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "Environment/Disable Component/By Tag";

        /// <inheritdoc />
        public override IBehavior GetNewItem()
        {
            return new SetComponentEnabledByTagBehavior(false, "Disable Component (Tag)");
        }
    }
}
