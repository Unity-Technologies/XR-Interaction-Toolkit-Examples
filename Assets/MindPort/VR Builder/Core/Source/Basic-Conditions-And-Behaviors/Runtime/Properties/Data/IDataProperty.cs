using UnityEngine.Events;

namespace VRBuilder.Core.Properties
{
    /// <summary>
    /// Interface for a property that stores a single value.
    /// </summary>
    /// <typeparam name="T">Type of the value to be stored.</typeparam>
    public interface IDataProperty<T> : IDataPropertyBase
    {
        /// <summary>
        /// Raised when the stored value changes.
        /// </summary>
        UnityEvent<T> OnValueChanged { get; }

        /// <summary>
        /// Returns the value.
        /// </summary>
        T GetValue();

        /// <summary>
        /// Sets the value.
        /// </summary>
        void SetValue(T value);      
    }
}
