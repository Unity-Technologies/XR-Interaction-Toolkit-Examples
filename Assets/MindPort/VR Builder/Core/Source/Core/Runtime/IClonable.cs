namespace VRBuilder.Core
{
    /// <summary>
    /// Interface for objects that can be duplicated.
    /// </summary>    
    public interface IClonable<T>
    {
        /// <summary>
        /// Returns a deep copy of the object.
        /// </summary>        
        T Clone();
    }
}
