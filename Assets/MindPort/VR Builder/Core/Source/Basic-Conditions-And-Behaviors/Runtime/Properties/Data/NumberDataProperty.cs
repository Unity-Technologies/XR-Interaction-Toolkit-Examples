using UnityEngine;
using UnityEngine.Events;

namespace VRBuilder.Core.Properties
{
    /// <summary>
    /// Float implementation of the <see cref="DataProperty{T}"/> class.
    /// </summary>
    public class NumberDataProperty : DataProperty<float>
    {
        [Header("Settings")]
        [SerializeField]
        private float defaultValue;

        [Header("Events")]
        [SerializeField]
        private UnityEvent<float> valueChanged = new UnityEvent<float>();

        [SerializeField]
        private UnityEvent valueReset = new UnityEvent();

        /// <inheritdoc/>
        public override UnityEvent<float> OnValueChanged => valueChanged;

        /// <inheritdoc/>
        public override UnityEvent OnValueReset => valueReset;

        /// <inheritdoc/>
        public override float DefaultValue => defaultValue;

        /// <summary>
        /// Increases the value of the data property by a given amount.
        /// </summary>        
        public void IncreaseValue(float increase)
        {
            SetValue(GetValue() + increase);
        }
    }
}
