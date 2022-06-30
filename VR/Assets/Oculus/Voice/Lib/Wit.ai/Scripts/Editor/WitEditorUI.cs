/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;

namespace Facebook.WitAi
{
    public class WitEditorUI
    {

        public static bool FallbackPopup(SerializedObject serializedObject, string propertyName,
            string[] names, ref int index)
        {
            var property = serializedObject.FindProperty(propertyName);
            string intent;
            if (null != names && names.Length > 0)
            {
                index = EditorGUILayout.Popup(property.displayName, index, names);
                if (index >= 0)
                {
                    intent = names[index];
                }
                else
                {
                    intent = EditorGUILayout.TextField(property.stringValue);
                }
            }
            else
            {
                intent = EditorGUILayout.TextField(property.displayName, property.stringValue);
            }

            if (intent != property.stringValue)
            {
                property.stringValue = intent;
                return true;
            }

            return false;
        }
    }
}
