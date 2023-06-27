using System;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Process drawer for a 0-1 float slider.
    /// </summary>
    internal class NormalizedFloatDrawer : AbstractDrawer
    {
        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            rect.height = EditorDrawingHelper.SingleLineHeight;

            float value = (float)currentValue;
            float newValue = EditorGUI.Slider(rect, label, value, 0f, 1f);

            // Rounding error can't take place here.
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (value != newValue)
            {
                ChangeValue(() => newValue, () => value, changeValueCallback);
            }

            return rect;
        }
    }
}
