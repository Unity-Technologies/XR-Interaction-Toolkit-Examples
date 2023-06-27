using System;
using System.Collections.Generic;
using VRBuilder.Core.Input;

namespace VRBuilder.UX
{
    /// <summary>
    /// Default process controller.
    /// </summary>
    public class SpectatorProcessController : BaseProcessController
    {
        /// <inheritdoc />
        public override string Name { get; } = "Spectator Camera";

        /// <inheritdoc />
        protected override string PrefabName { get; } = "SpectatorProcessController";

        /// <inheritdoc />
        public override int Priority { get; } = 64;

        /// <inheritdoc />
        public override List<Type> GetRequiredSetupComponents()
        {
            List<Type> requiredSetupComponents = base.GetRequiredSetupComponents();
            requiredSetupComponents.Add(InputController.ConcreteType);
            requiredSetupComponents.Add(typeof(SpectatorController));
            return requiredSetupComponents;
        }
    }
}
