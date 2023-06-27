using VRBuilder.Core.Conditions;
using VRBuilder.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.UI.Conditions
{
    /// <inheritdoc />
    public class ObjectInColliderMenuItem : MenuItem<ICondition>
    {
        /// <inheritdoc />
        public override string DisplayedName { get; } = "Environment/Move Object in Collider";

        /// <inheritdoc />
        public override ICondition GetNewItem()
        {
            return new ObjectInColliderCondition();
        }
    }
}
