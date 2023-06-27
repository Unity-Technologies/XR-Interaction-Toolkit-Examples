using VRBuilder.BasicInteraction.Conditions;
using VRBuilder.Core.Conditions;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.BasicInteraction.UI.Conditions
{
    public class SnappedByTagMenuItem : MenuItem<ICondition>
    {
        public override string DisplayedName { get; } = "Interaction/Snap Object/By Tag";

        public override ICondition GetNewItem()
        {
            return new SnappedObjectWithTagCondition();
        }
    }
}