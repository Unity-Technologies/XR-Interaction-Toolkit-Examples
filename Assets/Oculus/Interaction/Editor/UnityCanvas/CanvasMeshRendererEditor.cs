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

using props = Oculus.Interaction.UnityCanvas.CanvasMeshRenderer.Properties;

namespace Oculus.Interaction.UnityCanvas.Editor
{
    [CustomEditor(typeof(CanvasMeshRenderer))]
    public class CanvasMeshRendererEditor : UnityEditor.Editor
    {
        private EditorBase _editorDrawer;

        public new CanvasMeshRenderer target
        {
            get
            {
                return base.target as CanvasMeshRenderer;
            }
        }

        protected virtual void OnEnable()
        {
            _editorDrawer = new EditorBase(serializedObject);
            var renderingModeProp = serializedObject.FindProperty(props.RenderingMode);

            _editorDrawer.Draw(props.RenderingMode, (modeProp) =>
            {
                RenderingMode value = (RenderingMode)modeProp.intValue;
                value = (RenderingMode)EditorGUILayout.EnumPopup("Rendering Mode", value);
                modeProp.intValue = (int)value;
            });

            _editorDrawer.Draw(props.UseAlphaToMask, props.AlphaCutoutThreshold, (maskProp, cutoutProp) =>
            {
                if (renderingModeProp.intValue == (int)RenderingMode.AlphaCutout)
                {
                    EditorGUILayout.PropertyField(maskProp);

                    if (maskProp.boolValue == false)
                    {
                        EditorGUILayout.PropertyField(cutoutProp);
                    }
                }
            });
        }

        public override void OnInspectorGUI()
        {
            _editorDrawer.DrawFullInspector();
        }

    }
}
