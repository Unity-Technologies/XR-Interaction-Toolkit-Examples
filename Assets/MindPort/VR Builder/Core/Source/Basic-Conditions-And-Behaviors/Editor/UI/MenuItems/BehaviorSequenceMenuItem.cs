using VRBuilder.Core.Behaviors;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.UI.Behaviors
{
    /// <inheritdoc />
    public class BehaviorSequenceMenuItem : MenuItem<IBehavior>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "Utility/Behaviors Sequence";

        /// <inheritdoc />
        public override IBehavior GetNewItem()
        {
            return new BehaviorSequence();
        }
    }
}
