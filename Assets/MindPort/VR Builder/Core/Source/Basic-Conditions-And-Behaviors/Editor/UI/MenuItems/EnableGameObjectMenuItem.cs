using VRBuilder.Core.Behaviors;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.UI.Behaviors
{
    /// <inheritdoc />
    public class EnableGameObjectMenuItem : MenuItem<IBehavior>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "Environment/Enable Objects/By Reference";

        /// <inheritdoc />
        public override IBehavior GetNewItem()
        {
            return new EnableGameObjectBehavior();
        }
    }
}
