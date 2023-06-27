namespace VRBuilder.Core.ProcessUtils
{
    public interface IParticleMachine
    {
        /// <summary>
        /// Activates the particle machine.
        /// </summary>
        void Activate();

        /// <summary>
        /// Activates the particle machine.
        /// </summary>
        /// <param name="radius">New radius of the emission area.</param>
        /// <param name="duration">New duration of the emission.</param>
        void Activate(float radius, float duration);

        /// <summary>
        /// Deactivates the particle machine.
        /// </summary>
        void Deactivate();

        /// <summary>
        /// True if particle machine is currently active and emitting particles.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Changes the radius of the emission area.
        /// </summary>
        /// <param name="radius">New radius of the emission area.</param>
        void ChangeAreaRadius(float radius);

        /// <summary>
        /// Changes the duration of the emission of the particle systems.
        /// </summary>
        /// <param name="duration">New duration of the emission.</param>
        void ChangeEmissionDuration(float duration);
    }
}
