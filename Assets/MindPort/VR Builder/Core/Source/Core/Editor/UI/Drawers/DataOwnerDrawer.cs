// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Linq;
using VRBuilder.Core;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    [DefaultProcessDrawer(typeof(IDataOwner))]
    internal class DataOwnerDrawer : AbstractDrawer
    {
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            if (currentValue == null)
            {
                throw new NullReferenceException("Attempting to draw null object.");
            }

            IData data = ((IDataOwner)currentValue).Data;

            IProcessDrawer dataDrawer = DrawerLocator.GetDrawerForMember(EditorReflectionUtils.GetFieldsAndPropertiesToDraw(currentValue).First(member => member.Name == "Data"), currentValue);

            return dataDrawer.Draw(rect, data, (value) => changeValueCallback(currentValue), label);
        }

        public override GUIContent GetLabel(object value, Type declaredType)
        {
            IData data = ((IDataOwner)value).Data;

            IProcessDrawer dataDrawer = DrawerLocator.GetDrawerForMember(EditorReflectionUtils.GetFieldsAndPropertiesToDraw(value).First(member => member.Name == "Data"), value);
            return dataDrawer.GetLabel(data, declaredType);
        }
    }
}
