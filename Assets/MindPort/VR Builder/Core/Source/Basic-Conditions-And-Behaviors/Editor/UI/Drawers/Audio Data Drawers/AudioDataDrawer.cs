using System;
using System.Reflection;
using VRBuilder.Core.Audio;
using VRBuilder.Editor.UI.Drawers;
using UnityEngine;

namespace VRBuilder.Editor.Core.UI.Drawers
{
    /// <summary>
    /// Process drawer for <see cref="IAudioData"/> members.
    /// </summary>
    [DefaultProcessDrawer(typeof(IAudioData))]
    public class AudioDataDrawer : ObjectDrawer
    {
        protected string tooltip;
        protected Texture image;
        
        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            tooltip = label.tooltip;
            image = label.image;
            // Has to run with a null label to not show two labels, dont ask me why.
            return base.Draw(rect, currentValue, changeValueCallback, GUIContent.none);
        }

        /// <inheritdoc />
        protected override void CheckValidationForValue(object currentValue, MemberInfo info, GUIContent label)
        {
            label.image = image;
            label.tooltip = tooltip;
        }
    }
}
