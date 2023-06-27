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

using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using Oculus.Interaction.Body.PoseDetection;
using Oculus.Interaction.Body.Input;
using System.Reflection;

namespace Oculus.Interaction.Body.Editor
{
    using JointComparerConfig = BodyPoseComparerActiveState.JointComparerConfig;

    [CustomPropertyDrawer(typeof(JointComparerConfig))]
    public class BodyPoseComparerConfigPropertyDrawer : PropertyDrawer
    {
        private static GUIContent[] _jointNames;
        private static BodyJointId[] _joints;

        private int _controlCount = 0;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetLineHeight() * _controlCount;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (_jointNames == null || _joints == null)
            {
                UpdateJointLists();
            }

            _controlCount = 0;

            Rect pos = new Rect(position.x, position.y, position.width,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginProperty(position, label, property);
            var joint = property.FindPropertyRelative(nameof(JointComparerConfig.Joint));
            var maxAngle = property.FindPropertyRelative(nameof(JointComparerConfig.MaxDelta));
            var width = property.FindPropertyRelative(nameof(JointComparerConfig.Width));

            DrawJointPopup(joint, "Body Joint", ref pos);
            DrawControl(maxAngle, "Max Angle Delta", ref pos);
            DrawControl(width, "Threshold Width", ref pos);

            EditorGUI.EndProperty();
        }

        private void DrawJointPopup(SerializedProperty jointProp, string name, ref Rect position)
        {
            int currentJoint = GetDisplayIndexFromJoint((BodyJointId)jointProp.intValue);
            int selectedJoint = EditorGUI.Popup(position,
                new GUIContent(name, jointProp.tooltip),
                currentJoint, _jointNames);

            position.y += GetLineHeight();
            ++_controlCount;
            jointProp.intValue = (int)_joints[selectedJoint];
        }

        private void DrawControl(SerializedProperty property, string name, ref Rect position)
        {
            EditorGUI.PropertyField(position, property, new GUIContent(name, property.tooltip));
            position.y += GetLineHeight();
            ++_controlCount;
        }

        private float GetLineHeight()
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private static int GetDisplayIndexFromJoint(BodyJointId jointId)
        {
            for (int i = 0; i < _joints.Length; ++i)
            {
                if (jointId == _joints[i])
                {
                    return i;
                }
            }
            return 0;
        }

        private static void UpdateJointLists()
        {
            List<string> jointNames = new List<string>();
            List<BodyJointId> joints = new List<BodyJointId>();

            for (int i = (int)BodyJointId.Body_Hips; i < (int)BodyJointId.Body_End; ++i)
            {
                BodyJointId jointId = (BodyJointId)i;
                InspectorNameAttribute inspectorNameAttribute =
                    typeof(BodyJointId)
                    .GetMember(jointId.ToString())
                    .First()
                    .GetCustomAttribute<InspectorNameAttribute>();

                joints.Add(jointId);
                jointNames.Add(inspectorNameAttribute != null ?
                           inspectorNameAttribute.displayName :
                           jointId.ToString());
            }

            _joints = joints.ToArray();
            _jointNames = jointNames
                .Select(s => new GUIContent(s))
                .ToArray();
        }
    }
}
