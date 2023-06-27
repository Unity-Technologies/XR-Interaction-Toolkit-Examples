using VRBuilder.Core.Behaviors;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.UI.Behaviors
{
    public class ScalingMenuItem : MenuItem<IBehavior>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "Animation/Scale Object";

        /// <inheritdoc />
        public override IBehavior GetNewItem()
        {
            return new ScalingBehavior();
        }
    }
}
