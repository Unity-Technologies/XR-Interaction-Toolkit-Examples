using System;
using UnityEngine;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.ProcessUtils;
using VRBuilder.Editor.UI.Drawers;

namespace VRBuilder.Editor.Core.UI.Drawers
{
    /// <summary>
    /// Implementation of <see cref="CompareValuesDrawer{T}"/> for comparing strings.
    /// </summary>
    [DefaultProcessDrawer(typeof(CompareValuesCondition<string>.EntityData))]
    internal class CompareTextDrawer : CompareValuesDrawer<string>
    {
        private enum Operator
        {
            EqualTo,
            NotEqualTo,
        }

        /// <inheritdoc/>
        protected override Rect DrawOperatorDropdown(Action<object> changeValueCallback, Rect nextPosition, CompareValuesCondition<string>.EntityData data)
        {
            Operator currentOperator = GetCurrentOperator(data);
            nextPosition = DrawerLocator.GetDrawerForValue(currentOperator, typeof(Operator)).Draw(nextPosition, currentOperator, (value) => UpdateOperator(value, data, changeValueCallback), "Operator");

            return nextPosition;
        }

        private void UpdateOperator(object value, CompareValuesCondition<string>.EntityData data, Action<object> changeValueCallback)
        {
            Operator newOperator = (Operator)value;
            Operator oldOperator = GetCurrentOperator(data);

            if (newOperator != oldOperator)
            {
                switch (newOperator)
                {
                    case Operator.EqualTo:
                        data.Operation = new EqualToOperation<string>();
                        break;
                    case Operator.NotEqualTo:
                        data.Operation = new NotEqualToOperation<string>();
                        break;
                }

                changeValueCallback(data);
            }
        }

        private Operator GetCurrentOperator(CompareValuesCondition<string>.EntityData data)
        {
            Operator currentOperator = Operator.EqualTo;

            if (data.Operation.GetType() == typeof(EqualToOperation<string>))
            {
                currentOperator = Operator.EqualTo;
            }
            else if (data.Operation.GetType() == typeof(NotEqualToOperation<string>))
            {
                currentOperator = Operator.NotEqualTo;
            }

            return currentOperator;
        }
    }
}
