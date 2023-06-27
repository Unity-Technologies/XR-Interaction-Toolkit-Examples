using UnityEditor;
using VRBuilder.Core.ProcessUtils;
using VRBuilder.Editor.UI.Drawers;

namespace VRBuilder.Editor.Core.UI.Drawers
{
    /// <summary>
    /// Implementation of <see cref="ProcessVariableDrawer{T}"/> that draws string variables.
    /// </summary>
    [DefaultProcessDrawer(typeof(ProcessVariable<string>))]
    internal class StringProcessVariableDrawer : ProcessVariableDrawer<string>
    {
        /// <inheritdoc/>
        protected override string DrawConstField(string value)
        {
            return EditorGUILayout.TextField("", value);
        }
    }
}