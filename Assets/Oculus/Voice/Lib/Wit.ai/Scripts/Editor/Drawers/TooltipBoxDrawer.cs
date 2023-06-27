/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Attributes;
using Meta.WitAi.Windows;
using UnityEditor;
using UnityEngine;

namespace Meta.WitAi.Drawers
{
    [CustomPropertyDrawer(typeof(TooltipBoxAttribute))]
    public class TooltipBoxDrawer : DecoratorDrawer
    {
        private float spaceAfterBox = 4;
        private float iconSize = 32;
        
        public override float GetHeight()
        {
            if (!WitWindow.ShowTooltips) return 0;
            
            TooltipBoxAttribute infoBoxAttribute = (TooltipBoxAttribute)attribute;
            var height = EditorStyles.helpBox.CalcHeight(new GUIContent(infoBoxAttribute.Text), EditorGUIUtility.currentViewWidth - iconSize);
            return Mathf.Max(iconSize, height) + spaceAfterBox;
        }
        
        public override void OnGUI(Rect position)
        {
            if (!WitWindow.ShowTooltips) return;
            
            var iconRect = EditorGUI.IndentedRect(position);
            iconRect.width = iconSize;
            iconRect.height = iconSize;
            GUIContent infoIcon = EditorGUIUtility.IconContent("console.infoicon");
            infoIcon.tooltip = "You can turn off these tooltips in Voice SDK Settings.";
            EditorGUI.LabelField(iconRect, infoIcon);
            
            var tooltip = (TooltipBoxAttribute) attribute;
            var rect = EditorGUI.IndentedRect(position);
            rect.x += iconSize;
            rect.width -= iconSize;
            rect.height -= spaceAfterBox;
            EditorGUI.TextArea(rect, tooltip.Text, EditorStyles.helpBox);
        }
    }
}
