using System;
using UnityEngine;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.ProcessUtils;
using VRBuilder.Editor.UI;
using VRBuilder.Editor.UI.Drawers;

namespace VRBuilder.Editor.Core.UI.Drawers
{
    /// <summary>
    /// Custom drawer for <see cref="CompareValuesCondition{T}"/>.
    /// </summary>    
    public abstract class CompareValuesDrawer<T> : NameableDrawer where T: IEquatable<T>, IComparable<T>
    {
        /// <summary>
        /// Draws the dropdown for selecting the operator depending on the operands' type
        /// </summary>
        protected abstract Rect DrawOperatorDropdown(Action<object> changeValueCallback, Rect nextPosition, CompareValuesCondition<T>.EntityData data);

        /// <inheritdoc/>
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            rect = base.Draw(rect, currentValue, changeValueCallback, label);
            float height = rect.height;
            height += EditorDrawingHelper.VerticalSpacing;

            Rect nextPosition = new Rect(rect.x, rect.y + height, rect.width, rect.height);

            CompareValuesCondition<T>.EntityData data = currentValue as CompareValuesCondition<T>.EntityData;

            ProcessVariable<T> left = new ProcessVariable<T>(data.LeftValue, data.LeftValueProperty.UniqueName, data.IsLeftConst);

            nextPosition = DrawerLocator.GetDrawerForValue(left, typeof(ProcessVariable<T>)).Draw(nextPosition, left, (value) => UpdateLeftOperand(value, data, changeValueCallback), "Left Operand");
            height += nextPosition.height;
            height += EditorDrawingHelper.VerticalSpacing;
            nextPosition.y = rect.y + height;

            nextPosition = DrawOperatorDropdown(changeValueCallback, nextPosition, data);
            height += nextPosition.height;
            height += EditorDrawingHelper.VerticalSpacing;
            nextPosition.y = rect.y + height;

            ProcessVariable<T> right = new ProcessVariable<T>(data.RightValue, data.RightValueProperty.UniqueName, data.IsRightConst);

            nextPosition = DrawerLocator.GetDrawerForValue(left, typeof(ProcessVariable<T>)).Draw(nextPosition, right, (value) => UpdateRightOperand(value, data, changeValueCallback), "Right Operand");
            height += nextPosition.height;
            nextPosition.y = rect.y + height;

            rect.height = height;
            return rect;
        }        

        private void UpdateLeftOperand(object value, CompareValuesCondition<T>.EntityData data, Action<object> changeValueCallback)
        {
            ProcessVariable<T> newOperand = (ProcessVariable<T>)value;
            ProcessVariable<T> oldOperand = new ProcessVariable<T>(data.LeftValue, data.LeftValueProperty.UniqueName, data.IsLeftConst);
            
            bool valueChanged = false;

            if(newOperand.PropertyReference.UniqueName != oldOperand.PropertyReference.UniqueName)
            {
                data.LeftValueProperty = newOperand.PropertyReference;
                valueChanged = true;
            }

            if (newOperand.ConstValue != null && newOperand.ConstValue.Equals(oldOperand.ConstValue) == false)
            {
                data.LeftValue = newOperand.ConstValue;
                valueChanged = true;
            }

            if (newOperand.IsConst != oldOperand.IsConst)
            {
                data.IsLeftConst = newOperand.IsConst;
                valueChanged = true;
            }

            if(valueChanged)
            {
                changeValueCallback(data);
            }
        }

        private void UpdateRightOperand(object value, CompareValuesCondition<T>.EntityData data, Action<object> changeValueCallback)
        {
            ProcessVariable<T> newOperand = (ProcessVariable<T>)value;
            ProcessVariable<T> oldOperand = new ProcessVariable<T>(data.RightValue, data.RightValueProperty.UniqueName, data.IsRightConst);

            bool valueChanged = false;

            if (newOperand.PropertyReference.UniqueName != oldOperand.PropertyReference.UniqueName)
            {
                data.RightValueProperty = newOperand.PropertyReference;
                valueChanged = true;
            }

            if (newOperand.ConstValue != null && newOperand.ConstValue.Equals(oldOperand.ConstValue) == false)
            {
                data.RightValue = newOperand.ConstValue;
                valueChanged = true;
            }

            if (newOperand.IsConst != oldOperand.IsConst)
            {
                data.IsRightConst = newOperand.IsConst;
                valueChanged = true;
            }

            if (valueChanged)
            {
                changeValueCallback(data);
            }
        }
    }
}
