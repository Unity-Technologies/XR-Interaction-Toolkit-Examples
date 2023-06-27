using System;
using UnityEngine;
using UnityEngine.Events;
using VRBuilder.Core.Utils.Logging;
using VRBuilder.Unity;

namespace VRBuilder.Core.Properties
{
    /// <summary>
    /// Base implementation for process data properties.
    /// </summary>    
    [DisallowMultipleComponent]
    public abstract class DataProperty<T> : ProcessSceneObjectProperty, IDataProperty<T>
    {
        /// <summary>
        /// Defines a default value for the property.
        /// </summary>
        public abstract T DefaultValue { get; }

        /// <inheritdoc/>
        protected T storedValue;

        /// <inheritdoc/>
        public event EventHandler<EventArgs> ValueChanged;

        /// <inheritdoc/>
        public event EventHandler<EventArgs> ValueReset;

        /// <inheritdoc/>
        public abstract UnityEvent<T> OnValueChanged { get; }

        /// <inheritdoc/>
        public abstract UnityEvent OnValueReset { get; }

        private void Awake()
        {
            ResetValue();
        }

        /// <inheritdoc/>
        public T GetValue()
        {
            return storedValue;
        }

        /// <inheritdoc/>
        public void ResetValue()
        {
            SetValue(DefaultValue);
            ValueReset?.Invoke(this, EventArgs.Empty);
            OnValueReset?.Invoke();
        }

        /// <inheritdoc/>
        public void SetValue(T value)
        {
            if((storedValue == null && value == null) || value.Equals(storedValue))
            {
                return;
            }

            if(LifeCycleLoggingConfig.Instance.LogDataPropertyChanges)
            {
                Debug.Log($"{ConsoleUtils.GetTabs()}<b>{GetType().Name}</b> on <i>'{SceneObject.UniqueName}'</i> changed from <b>{ValueToString(storedValue)}</b> to <b>{ValueToString(value)}</b>.\n");
            }

            storedValue = value;
            ValueChanged?.Invoke(this, EventArgs.Empty);
            OnValueChanged?.Invoke(storedValue);
        }

        protected virtual string ValueToString(T value)
        {
            return value != null ? value.ToString() : "null";
        }
    }
}
