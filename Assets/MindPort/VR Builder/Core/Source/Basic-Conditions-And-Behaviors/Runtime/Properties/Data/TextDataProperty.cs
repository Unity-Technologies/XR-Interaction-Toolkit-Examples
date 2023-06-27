using UnityEngine;
using UnityEngine.Events;

namespace VRBuilder.Core.Properties
{
    /// <summary>
    /// String implementation of the <see cref="DataProperty{T}"/> class.
    /// </summary>
    public class TextDataProperty : DataProperty<string>
    {
        [Header("Settings")]
        [SerializeField]
        private string defaultValue;

        [Header("Events")]
        [SerializeField]
        private UnityEvent<string> valueChanged = new UnityEvent<string>();

        [SerializeField]
        private UnityEvent valueReset = new UnityEvent();

        /// <inheritdoc/>
        public override UnityEvent<string> OnValueChanged => valueChanged;

        /// <inheritdoc/>
        public override UnityEvent OnValueReset => valueReset;

        /// <inheritdoc/>
        public override string DefaultValue => defaultValue;
    }
}
