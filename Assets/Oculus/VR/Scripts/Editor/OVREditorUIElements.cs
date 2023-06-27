/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

internal class OVREditorUIElements
{
    internal static bool RenderWarningWithButton(string labelString, string buttonString)
    {
        Assert.IsNotNull(labelString);
        Assert.IsNotNull(buttonString);
        bool isButtonClicked;

        GUIContent Warning = EditorGUIUtility.IconContent("console.warnicon@2x");
        var alignByCenter = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        using (var z = new EditorGUILayout.VerticalScope("HelpBox"))
        {
            EditorGUI.LabelField(z.rect, Warning, EditorStyles.helpBox);
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            var horizontalSpace =
                EditorGUIUtility.standardVerticalSpacing * 3 +
                EditorGUIUtility.singleLineHeight * 2 + 5;

            GUILayout.BeginHorizontal();
            GUILayout.Space(horizontalSpace);
            GUILayout.Label(labelString, alignByCenter);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(horizontalSpace);
            isButtonClicked = GUILayout.Button(buttonString);

            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
        }

        return isButtonClicked;
    }
}
