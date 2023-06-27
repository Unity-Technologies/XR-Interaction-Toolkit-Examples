using VRBuilder.BasicInteraction.Conditions;
using VRBuilder.Core.Conditions;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.BasicInteraction.UI.Conditions
{
    public class TouchedMenuItem : MenuItem<ICondition>
    {
        public override string DisplayedName { get; } = "Interaction/Touch Object";

        public override ICondition GetNewItem()
        {
            return new TouchedCondition();
        }
    }
}
