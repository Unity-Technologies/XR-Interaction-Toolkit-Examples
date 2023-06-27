// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Linq;
using VRBuilder.Editor.Tabs;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    [DefaultProcessDrawer(typeof(ITabsGroup))]
    internal class TabsGroupDrawer : AbstractDrawer
    {
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            ITabsGroup tabsGroup = (ITabsGroup)currentValue;

            // Draw tabs selector.
            float tabsHeight = DrawToolbox(rect, tabsGroup, changeValueCallback).height;

            // Get drawer for the object under the tab.
            IProcessDrawer tabValueDrawer = DrawerLocator.GetDrawerForValue(tabsGroup.Tabs[tabsGroup.Selected].GetValue(), typeof(object));

            void ChangeValueCallback(object newValue)
            {
                tabsGroup.Tabs[tabsGroup.Selected].SetValue(newValue);
                changeValueCallback(tabsGroup);
            }

            Rect tabValueRect = new Rect(rect.x, rect.y + tabsHeight, rect.width, 0);

            // Draw the object under the tab.
            rect.height = tabsHeight + tabValueDrawer.Draw(tabValueRect, tabsGroup.Tabs[tabsGroup.Selected].GetValue(), ChangeValueCallback, GUIContent.none).height;

            return rect;
        }

        private Rect DrawToolbox(Rect rect, ITabsGroup tabsGroup, Action<object> changeValueCallback)
        {
            rect.height = EditorStyles.toolbar.fixedHeight;

            GUIContent[] labels = tabsGroup.Tabs.Select(tab => tab.Label).ToArray();

            int oldSelected = tabsGroup.Selected;
            int selected = GUI.Toolbar(rect, oldSelected, labels);

            if (selected != oldSelected)
            {
                ChangeValue(() =>
                    {
                        tabsGroup.Tabs[oldSelected].OnUnselect();
                        tabsGroup.Tabs[selected].OnSelected();
                        tabsGroup.Selected = selected;
                        EditorUtils.ResetKeyboardElementFocus();
                        return tabsGroup;
                    },
                    () =>
                    {
                        tabsGroup.Tabs[selected].OnUnselect();
                        tabsGroup.Tabs[oldSelected].OnSelected();
                        tabsGroup.Selected = oldSelected;
                        EditorUtils.ResetKeyboardElementFocus();
                        return tabsGroup;
                    },
                    changeValueCallback);
            }

            return rect;
        }
    }
}
