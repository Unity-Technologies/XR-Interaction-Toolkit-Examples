using VRBuilder.Core.Conditions;
using VRBuilder.BasicInteraction.Conditions;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.BasicInteraction.UI.Conditions
{
    /// <inheritdoc />
    public class TeleportMenuItem : MenuItem<ICondition>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "VR User/Teleport";

        /// <inheritdoc />
        public override ICondition GetNewItem()
        {
            return new TeleportCondition();
        }
    }
}

