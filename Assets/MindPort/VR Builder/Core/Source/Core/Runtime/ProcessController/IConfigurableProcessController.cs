namespace VRBuilder.ProcessController
{
    /// <summary>
    /// Interface for a process controller that can be configured in the setup object.
    /// </summary>
    public interface IConfigurableProcessController
    {
        /// <summary>
        /// If true, the process will start automatically as soon as the process controller is loaded.
        /// </summary>
        bool AutoStartProcess { get; set; }
    }
}