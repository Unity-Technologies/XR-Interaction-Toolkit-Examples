using VRBuilder.Core.Behaviors;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.UI.Behaviors
{
    public class ConfettiMenuItem : MenuItem<IBehavior>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "Guidance/Spawn Confetti";

        /// <inheritdoc />
        public override IBehavior GetNewItem()
        {
            return new ConfettiBehavior();
        }
    }
}
