using VRBuilder.BasicInteraction.Conditions;
using VRBuilder.Core.Conditions;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.BasicInteraction.UI.Conditions
{
    public class ReleasedMenuItem : MenuItem<ICondition>
    {
        public override string DisplayedName { get; } = "Interaction/Release Object";

        public override ICondition GetNewItem()
        {
            return new ReleasedCondition();
        }
    }
}
