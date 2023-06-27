using System;
using UnityEngine.Events;

namespace VRBuilder.Core.Properties
{
    /// <summary>
    /// Base interface for a property that stores a single value.
    /// </summary>
    public interface IDataPropertyBase : ISceneObjectProperty
    {
        /// <summary>
        /// Raised when the stored value changes.
        /// </summary>
        [Obsolete("This event is deprecated. Use OnValueChanged instead.")]
        event EventHandler<EventArgs> ValueChanged;

        /// <summary>
        /// Raised when the stored value is reset to the default.
        /// </summary>
        [Obsolete("This event is deprecated. Use OnValueReset instead.")]
        event EventHandler<EventArgs> ValueReset;

        /// <summary>
        /// Raised when the stored value is reset to the default.
        /// </summary>
        UnityEvent OnValueReset { get; }

        /// <summary>
        /// Resets the value to its default.
        /// </summary>
        void ResetValue();
    }
}
