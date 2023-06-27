using System;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Drawer for <see cref="AnimationCurve"/>
    /// </summary>
    [DefaultProcessDrawer(typeof(AnimationCurve))]
    internal class AnimationCurveDrawer : AbstractDrawer
    {
        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            rect.height = EditorDrawingHelper.SingleLineHeight;

            AnimationCurve curve = (AnimationCurve)currentValue;
            AnimationCurve newCurve = EditorGUI.CurveField(rect, label, curve);
            if (newCurve != curve)
            {            
                ChangeValue(() => newCurve, () => curve, changeValueCallback);
            }

            return rect;
        }
    }
}
