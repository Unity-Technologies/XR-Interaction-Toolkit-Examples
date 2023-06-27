using VRBuilder.BasicInteraction.Conditions;
using VRBuilder.Core.Conditions;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.BasicInteraction.UI.Conditions
{
    /// <inheritdoc />
    public class GrabbedByTagMenuItem : MenuItem<ICondition>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "Interaction/Grab Object/By Tag";

        /// <inheritdoc />
        public override ICondition GetNewItem()
        {
            return new GrabbedObjectWithTagCondition();
        }
    }
}