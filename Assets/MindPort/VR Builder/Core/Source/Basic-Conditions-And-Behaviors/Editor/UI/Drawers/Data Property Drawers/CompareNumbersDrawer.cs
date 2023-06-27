using System;
using UnityEngine;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.ProcessUtils;
using VRBuilder.Editor.UI.Drawers;

namespace VRBuilder.Editor.Core.UI.Drawers
{
    /// <summary>
    /// Implementation of <see cref="CompareValuesDrawer{T}"/> for comparing floats.
    /// </summary>
    [DefaultProcessDrawer(typeof(CompareValuesCondition<float>.EntityData))]
    internal class CompareNumbersDrawer : CompareValuesDrawer<float>
    {
        private enum Operator
        {
            EqualTo,
            NotEqualTo,
            GreaterThan,
            LessThan,
            GreaterThanOrEqual,
            LessThanOrEqual,
        }

        /// <inheritdoc/>
        protected override Rect DrawOperatorDropdown(Action<object> changeValueCallback, Rect nextPosition, CompareValuesCondition<float>.EntityData data)
        {
            Operator currentOperator = GetCurrentOperator(data);
            nextPosition = DrawerLocator.GetDrawerForValue(currentOperator, typeof(Operator)).Draw(nextPosition, currentOperator, (value) => UpdateOperator(value, data, changeValueCallback), "Operator");

            return nextPosition;
        }

        private void UpdateOperator(object value, CompareValuesCondition<float>.EntityData data, Action<object> changeValueCallback)
        {
            Operator newOperator = (Operator)value;
            Operator oldOperator = GetCurrentOperator(data);

            if (newOperator != oldOperator)
            {
                switch (newOperator)
                {
                    case Operator.EqualTo:
                        data.Operation = new EqualToOperation<float>();
                        break;
                    case Operator.NotEqualTo:
                        data.Operation = new NotEqualToOperation<float>();
                        break;
                    case Operator.GreaterThan:
                        data.Operation = new GreaterThanOperation<float>();
                        break;
                    case Operator.LessThan:
                        data.Operation = new LessThanOperation<float>();
                        break;
                    case Operator.GreaterThanOrEqual:
                        data.Operation = new GreaterOrEqualOperation<float>();
                        break;
                    case Operator.LessThanOrEqual:
                        data.Operation = new LessThanOrEqualOperation<float>();
                        break;
                }

                changeValueCallback(data);
            }
        }

        private Operator GetCurrentOperator(CompareValuesCondition<float>.EntityData data)
        {
            Operator currentOperator = Operator.EqualTo;

            if (data.Operation.GetType() == typeof(EqualToOperation<float>))
            {
                currentOperator = Operator.EqualTo;
            }
            else if (data.Operation.GetType() == typeof(NotEqualToOperation<float>))
            {
                currentOperator = Operator.NotEqualTo;
            }
            else if (data.Operation.GetType() == typeof(GreaterThanOperation<float>))
            {
                currentOperator = Operator.GreaterThan;
            }
            else if (data.Operation.GetType() == typeof(LessThanOperation<float>))
            {
                currentOperator = Operator.LessThan;
            }
            else if (data.Operation.GetType() == typeof(GreaterOrEqualOperation<float>))
            {
                currentOperator = Operator.GreaterThanOrEqual;
            }
            else if (data.Operation.GetType() == typeof(LessThanOrEqualOperation<float>))
            {
                currentOperator = Operator.LessThanOrEqual;
            }

            return currentOperator;
        }

    }
}
