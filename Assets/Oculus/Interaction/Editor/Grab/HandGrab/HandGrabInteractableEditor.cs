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

using Oculus.Interaction.Editor;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.HandGrab.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HandGrabInteractable))]
    public partial class HandGrabInteractableEditor : SimplifiedEditor
    {
        private HandGrabInteractable _target;
        private HandGrabScaleKeysEditor<HandGrabInteractable> _listDrawer;
        private void Awake()
        {
            _target = target as HandGrabInteractable;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _listDrawer = new HandGrabScaleKeysEditor<HandGrabInteractable>(serializedObject,
                _target.HandGrabPoses, "_handGrabPoses", true);
            _editorDrawer.Draw("_handGrabPoses", (modeProp) =>
            {
                _listDrawer.DrawInspector();
            });
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _listDrawer.TearDown();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Create Mirrored HandGrabInteractable"))
            {
                Mirror();
            }
        }

        private void Mirror()
        {
            HandGrabInteractable mirrorInteractable =
                   HandGrabUtils.CreateHandGrabInteractable(_target.RelativeTo,
                       $"{_target.gameObject.name}_mirror");

            var data = HandGrabUtils.SaveData(_target);
            data.poses = null;
            HandGrabUtils.LoadData(mirrorInteractable, data);
            foreach (HandGrabPose point in _target.HandGrabPoses)
            {
                HandGrabPose mirrorPose = HandGrabUtils.CreateHandGrabPose(mirrorInteractable.transform,
                    mirrorInteractable.RelativeTo);
                HandGrabUtils.MirrorHandGrabPose(point, mirrorPose, _target.RelativeTo);
                mirrorPose.transform.SetParent(mirrorInteractable.transform);
                mirrorInteractable.HandGrabPoses.Add(mirrorPose);
            }
        }
    }
}
