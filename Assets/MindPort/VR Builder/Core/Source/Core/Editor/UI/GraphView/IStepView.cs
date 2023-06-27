using VRBuilder.Core;

namespace VRBuilder.Editor.UI.Windows
{
    /// <summary>
    /// Interface for GUI step view.
    /// </summary>
    public interface IStepView
    {
        /// <summary>
        /// Sets a new step.
        /// </summary>        
        void SetStep(IStep newStep);

        /// <summary>
        /// Resets the step view.
        /// </summary>
        void ResetStepView();
    }
}
