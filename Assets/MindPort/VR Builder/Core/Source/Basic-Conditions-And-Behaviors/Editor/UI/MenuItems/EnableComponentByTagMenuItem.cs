using VRBuilder.Core.Behaviors;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.UI.Behaviors
{
    /// <inheritdoc />
    public class EnableComponentByTagMenuItem : MenuItem<IBehavior>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "Environment/Enable Component/By Tag";

        /// <inheritdoc />
        public override IBehavior GetNewItem()
        {
            return new SetComponentEnabledByTagBehavior(true, "Enable Component (Tag)");
        }
    }
}
