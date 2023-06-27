using VRBuilder.BasicInteraction.Conditions;
using VRBuilder.Core.Conditions;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.BasicInteraction.UI.Conditions
{
    public class UsedMenuItem : MenuItem<ICondition>
    {
        public override string DisplayedName { get; } = "Interaction/Use Object";

        public override ICondition GetNewItem()
        {
            return new UsedCondition();
        }
    }
}