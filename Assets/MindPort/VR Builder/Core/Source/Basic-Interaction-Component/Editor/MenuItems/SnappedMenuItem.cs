using VRBuilder.BasicInteraction.Conditions;
using VRBuilder.Core.Conditions;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.BasicInteraction.UI.Conditions
{
    public class SnappedMenuItem : MenuItem<ICondition>
    {
        public override string DisplayedName { get; } = "Interaction/Snap Object/By Reference";

        public override ICondition GetNewItem()
        {
            return new SnappedCondition();
        }
    }
}