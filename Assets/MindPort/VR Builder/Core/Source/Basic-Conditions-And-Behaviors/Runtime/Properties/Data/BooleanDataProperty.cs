using UnityEngine;
using UnityEngine.Events;

namespace VRBuilder.Core.Properties
{
    /// <summary>
    /// Boolean implementation of the <see cref="DataProperty{T}"/> class.
    /// </summary>
    public class BooleanDataProperty : DataProperty<bool>
    {
        [Header("Settings")]
        [SerializeField]
        private bool defaultValue;

        [Header("Events")]
        [SerializeField]
        private UnityEvent<bool> valueChanged = new UnityEvent<bool>();

        [SerializeField]
        private UnityEvent valueReset = new UnityEvent();

        /// <inheritdoc/>
        public override UnityEvent<bool> OnValueChanged => valueChanged;

        /// <inheritdoc/>
        public override UnityEvent OnValueReset => valueReset;

        /// <inheritdoc/>
        public override bool DefaultValue => defaultValue;

        /// <summary>
        /// Changes the property's value from true to false and viceversa.
        /// </summary>
        public void InvertValue()
        {
            SetValue(GetValue() == false);
        }
    }
}
