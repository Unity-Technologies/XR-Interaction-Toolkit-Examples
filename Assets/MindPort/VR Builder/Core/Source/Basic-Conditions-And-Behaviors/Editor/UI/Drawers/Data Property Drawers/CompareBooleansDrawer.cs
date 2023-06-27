using System;
using UnityEngine;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.ProcessUtils;
using VRBuilder.Editor.UI.Drawers;

namespace VRBuilder.Editor.Core.UI.Drawers
{
    /// <summary>
    /// Implementation of <see cref="CompareValuesDrawer{T}"/> for comparing bools.
    /// </summary>
    [DefaultProcessDrawer(typeof(CompareValuesCondition<bool>.EntityData))]
    internal class CompareBooleansDrawer : CompareValuesDrawer<bool>
    {
        private enum Operator
        {
            EqualTo,
            NotEqualTo,
            And,
            Or,
        }

        /// <inheritdoc/>
        protected override Rect DrawOperatorDropdown(Action<object> changeValueCallback, Rect nextPosition, CompareValuesCondition<bool>.EntityData data)
        {
            Operator currentOperator = GetCurrentOperator(data);
            nextPosition = DrawerLocator.GetDrawerForValue(currentOperator, typeof(Operator)).Draw(nextPosition, currentOperator, (value) => UpdateOperator(value, data, changeValueCallback), "Operator");

            return nextPosition;
        }

        private void UpdateOperator(object value, CompareValuesCondition<bool>.EntityData data, Action<object> changeValueCallback)
        {
            Operator newOperator = (Operator)value;
            Operator oldOperator = GetCurrentOperator(data);

            if (newOperator != oldOperator)
            {
                switch (newOperator)
                {
                    case Operator.EqualTo:
                        data.Operation = new EqualToOperation<bool>();
                        break;
                    case Operator.NotEqualTo:
                        data.Operation = new NotEqualToOperation<bool>();
                        break;
                    case Operator.And:
                        data.Operation = new AndOperation();
                        break;
                    case Operator.Or:
                        data.Operation = new OrOperation();
                        break;
                }

                changeValueCallback(data);
            }
        }

        private Operator GetCurrentOperator(CompareValuesCondition<bool>.EntityData data)
        {
            Operator currentOperator = Operator.EqualTo;

            if (data.Operation.GetType() == typeof(EqualToOperation<bool>))
            {
                currentOperator = Operator.EqualTo;
            }
            else if (data.Operation.GetType() == typeof(NotEqualToOperation<bool>))
            {
                currentOperator = Operator.NotEqualTo;
            }
            else if (data.Operation.GetType() == typeof(AndOperation))
            {
                currentOperator = Operator.And;
            }
            else if (data.Operation.GetType() == typeof(OrOperation))
            {
                currentOperator = Operator.Or;
            }

            return currentOperator;
        }

    }
}
