using VRBuilder.Core.Conditions;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.UI.Conditions
{
    /// <inheritdoc />
    public class ObjectInRangeMenuItem : MenuItem<ICondition>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "Environment/Object Nearby";

        /// <inheritdoc />
        public override ICondition GetNewItem()
        {
            return new ObjectInRangeCondition();
        }
    }
}
