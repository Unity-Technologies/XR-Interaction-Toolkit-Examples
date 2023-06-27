namespace VRBuilder.Core
{
    /// <summary>
    /// Interface for data that can be renamed.
    /// </summary>
    public interface IRenameableData : INamedData
    {
        /// <summary>
        /// Set the new name.
        /// </summary>        
        void SetName(string name);
    }
}
