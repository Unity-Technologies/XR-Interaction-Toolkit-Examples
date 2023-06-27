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

namespace Oculus.Interaction.HandGrab.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DistanceHandGrabInteractable))]
    public partial class DistanceHandGrabInteractableEditor : SimplifiedEditor
    {
        private DistanceHandGrabInteractable _target;
        private HandGrabScaleKeysEditor<DistanceHandGrabInteractable> _listDrawer;

        private void Awake()
        {
            _target = target as DistanceHandGrabInteractable;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _listDrawer = new HandGrabScaleKeysEditor<DistanceHandGrabInteractable>(serializedObject,
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
    }
}
