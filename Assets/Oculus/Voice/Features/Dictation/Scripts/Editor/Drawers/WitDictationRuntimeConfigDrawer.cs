/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Dictation.Inspectors
{
    [CustomPropertyDrawer(typeof(WitDictationRuntimeConfigDrawer))]
    public class WitDictationRuntimeConfigDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);

        }
    }
}
