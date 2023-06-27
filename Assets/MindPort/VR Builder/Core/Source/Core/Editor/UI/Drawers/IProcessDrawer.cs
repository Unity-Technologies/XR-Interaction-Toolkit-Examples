// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Reflection;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Interface of a drawer for process members.
    /// </summary>
    public interface IProcessDrawer
    {
        /// <summary>
        /// Draw editor view in given Rect.
        /// </summary>
        /// <param name="rect">A rectangle in which editor view should fit. The height value is ignored.</param>
        /// <param name="currentValue">Current value of a member.</param>
        /// <param name="changeValueCallback">
        /// Delegate for a method that changes value of a member. Done that way to allow non-instantaneous assignments (for example, from generic menus).
        /// Invoke only when the value (or values of child members) has actually changed.
        /// </param>
        /// <param name="label">Label text to display.</param>
        /// <returns>The area that was taken by the property.</returns>
        Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, string label);

        /// <summary>
        /// Draw editor view in given Rect.
        /// </summary>
        /// <param name="rect">A rectangle in which editor view should fit. The height value is ignored.</param>
        /// <param name="currentValue">Current value of a member.</param>
        /// <param name="changeValueCallback">
        /// Delegate for a method that changes value of a member. Done that way to allow non-instantaneous assignments (for example, from generic menus).
        /// Invoke only when child member's value has changed;
        /// Otherwise, if the value itself has changed, call <see cref="ChangeValue"/> so it could manage Do/Undo stack in a proper way.
        /// </param>
        /// <param name="label">Label content to display.</param>
        /// <returns>The area that was taken by the property.</returns>
        Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label);

        /// <summary>
        /// Return a label for a property/field <paramref name="memberInfo"/> of an object <paramref name="memberOwner"/>.
        /// </summary>
        GUIContent GetLabel(MemberInfo memberInfo, object memberOwner);

        /// <summary>
        /// Return a label for a <paramref name="value"/> of <paramref name="declaredType"/>.
        /// </summary>
        GUIContent GetLabel(object value, Type declaredType);

        /// <summary>
        /// Call when the value has changed; it will create proper <see cref="ProcessCommand"/> to handle Do/Undo logic.
        /// <see cref="AbstractDrawer"/> for a proper implementation.
        /// <param name="getNewValueCallback">A delegate that returns a new value for the drawn entity. Invoked during the <see cref="ProcessCommand.Do"/> call. The result is passed to the <paramref name="assignValueCallback"/>.</param>
        /// <param name="getOldValueCallback">A delegate that returns an old value for the drawn entity. Invoked during the <see cref="ProcessCommand.Undo"/> call. The result is passed to the <paramref name="assignValueCallback"/>.</param>
        /// <param name="assignValueCallback">A delegate that actually assigns a value to the property/field.</param>
        /// </summary>
        void ChangeValue(Func<object> getNewValueCallback, Func<object> getOldValueCallback, Action<object> assignValueCallback);
    }
}
