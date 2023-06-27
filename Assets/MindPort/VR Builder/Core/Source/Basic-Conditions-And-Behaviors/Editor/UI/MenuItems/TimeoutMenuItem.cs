using VRBuilder.Core.Conditions;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.UI.Conditions
{
    /// <inheritdoc />
    public class TimeoutMenuItem : MenuItem<ICondition>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "Utility/Timeout";

        /// <inheritdoc />
        public override ICondition GetNewItem()
        {
            return new TimeoutCondition();
        }
    }
}
