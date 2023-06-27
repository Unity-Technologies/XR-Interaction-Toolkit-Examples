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
using System;
using UnityEngine;
using UnityEditor;

using props = Oculus.Interaction.UnityCanvas.OVRCanvasMeshRenderer.Properties;
using baseProps = Oculus.Interaction.UnityCanvas.CanvasMeshRenderer.Properties;
using rtprops = Oculus.Interaction.UnityCanvas.CanvasRenderTexture.Properties;

namespace Oculus.Interaction.UnityCanvas.Editor
{
    [CustomEditor(typeof(OVRCanvasMeshRenderer))]
    public class OVRCanvasMeshRendererEditor : UnityEditor.Editor
    {
        private EditorBase _editorDrawer;
        private const string DeferredSection = "Deferred";

        public new OVRCanvasMeshRenderer target
        {
            get
            {
                return base.target as OVRCanvasMeshRenderer;
            }
        }

        protected virtual void OnEnable()
        {
            _editorDrawer = new EditorBase(serializedObject);
            _editorDrawer.AddToSection(DeferredSection, baseProps.UseAlphaToMask, baseProps.AlphaCutoutThreshold);
            var renderingMode = serializedObject.FindProperty(baseProps.RenderingMode);

            bool CheckIsOVR()
            {
                return renderingMode.intValue == (int)OVRRenderingMode.Underlay ||
                       renderingMode.intValue == (int)OVRRenderingMode.Overlay;
            }

            _editorDrawer.Draw(props.RuntimeOffset, (offsetProp) =>
            {
                if (CheckIsOVR())
                {
                    EditorGUILayout.PropertyField(offsetProp);
                }
            });

            _editorDrawer.Draw(baseProps.RenderingMode, props.CanvasMesh, (modeProp, meshProp) =>
            {
                EditorGUILayout.PropertyField(meshProp);
                OVRRenderingMode value = (OVRRenderingMode)modeProp.intValue;
                value = (OVRRenderingMode)EditorGUILayout.EnumPopup("Rendering Mode", value);
                modeProp.intValue = (int)value;
            });

            _editorDrawer.Draw(props.EnableSuperSampling, props.EmulateWhileInEditor, props.DoUnderlayAntiAliasing, (sampleProp, emulateProp, aaProp) =>
            {
                if (CheckIsOVR())
                {
                    EditorGUILayout.PropertyField(sampleProp);
                    if (renderingMode.intValue == (int)OVRRenderingMode.Underlay)
                    {
                        EditorGUILayout.PropertyField(aaProp);
                    }
                    EditorGUILayout.PropertyField(emulateProp);
                }
            });

            _editorDrawer.Draw(baseProps.UseAlphaToMask, baseProps.AlphaCutoutThreshold, (maskProp, cutoutProp) =>
            {
                if (renderingMode.intValue == (int)OVRRenderingMode.AlphaCutout)
                {
                    EditorGUILayout.PropertyField(maskProp);

                    if (maskProp.boolValue == false)
                    {
                        EditorGUILayout.PropertyField(cutoutProp);
                    }
                }
            });
        }

        protected virtual void OnDisable()
        {
        }

        protected void BeforeInspector()
        {
            AutoFix(AutoFixIsUsingMipMaps(), AutoFixDisableMipMaps,
                $"{nameof(CanvasRenderTexture)} " +
                $"is generating mip maps, but these are ignored when using OVR Overlay/Underlay rendering.");
        }

        public override void OnInspectorGUI()
        {
            BeforeInspector();
            _editorDrawer.DrawFullInspector();
        }

        private bool AutoFix(bool needsFix, Action fixAction, string message)
        {
            if (needsFix)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(message, MessageType.Warning);
                    if (GUILayout.Button("Auto-Fix", GUILayout.ExpandHeight(true)))
                    {
                        fixAction();
                    }
                }
            }

            return needsFix;
        }

        private bool AutoFixIsUsingMipMaps()
        {
            var modeProp = serializedObject.FindProperty(baseProps.RenderingMode);
            OVRRenderingMode mode = (OVRRenderingMode)modeProp.intValue;
            if (mode != OVRRenderingMode.Overlay && mode != OVRRenderingMode.Underlay)
            {
                return false;
            }

            var rtProp = serializedObject.FindProperty(props.CanvasRenderTexture);
            CanvasRenderTexture canvasRT = rtProp.objectReferenceValue as CanvasRenderTexture;
            if (canvasRT == null)
            {
                return false;
            }

            var mipProp = new SerializedObject(canvasRT).FindProperty(rtprops.GenerateMipMaps);
            return mipProp.boolValue;
        }

        private void AutoFixDisableMipMaps()
        {
            var rtProp = serializedObject.FindProperty(props.CanvasRenderTexture);
            CanvasRenderTexture canvasRT = rtProp.objectReferenceValue as CanvasRenderTexture;
            var rtSO = new SerializedObject(canvasRT);
            var mipProp = rtSO.FindProperty(rtprops.GenerateMipMaps);
            mipProp.boolValue = false;
            rtSO.ApplyModifiedProperties();
        }
    }
}
