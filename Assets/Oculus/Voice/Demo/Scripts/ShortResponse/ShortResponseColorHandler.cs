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

using System;
using Meta.WitAi;
using Meta.WitAi.Data;
using Meta.WitAi.Data.Configuration;
using UnityEngine;

namespace Oculus.Voice.Demo
{
    [Serializable]
    public struct ColorOverride
    {
        public string colorID;
        public Color color;
    }

    public class ShortResponseColorHandler : MonoBehaviour
    {
        [Header("Shape Settings")]
        // Shape container
        [SerializeField] private Transform _shapeContainer;
        // Found shapes
        private Renderer[] _shapes;
        // Selected shape
        private int _shapeSelected = -1;
        // On shape selected
        public Action<Renderer> OnShapeSelected;

        [Header("Color Settings")]
        // Color override
#if UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5
        [NonReorderable]
#endif
        [SerializeField] private ColorOverride[] _colorOverride;
        // On shape selected
        public Action<Renderer, Color> OnShapeColorChanged;

        // Constants
        private const string SHAPE_SELECT_INTENT_ID = "shape_select";
        private const string COLOR_SET_INTENT_ID = "color_set";
        private const float MIN_CONFIDENCE = 0.8f;

        // On enable, find shapes
        private void OnEnable()
        {
            VoiceService service = GameObject.FindObjectOfType<VoiceService>();
            if (service != null)
            {
                WitConfiguration configuration = service.GetComponent<IWitRuntimeConfigProvider>()?.RuntimeConfiguration?.witConfiguration;
                if (configuration != null && !configuration.useConduit)
                {
                    VLog.E("ShortResponseDemo only works with Conduit!  Please enable Conduit on your wit configuration to try this demo.");
                }
            }
            if (_shapeContainer != null)
            {
                _shapes = _shapeContainer.GetComponentsInChildren<Renderer>(true);
            }
            SelectShape(-1);
        }

        #region SHAPES
        // Validate & set shape
        [ValidatePartialIntent(intent:SHAPE_SELECT_INTENT_ID, minConfidence:MIN_CONFIDENCE)]
        public void OnValidateShapeSelect(VoiceSession sessionData, string shape)
        {
            int index;
            if (TryGetShapeIndex(shape, out index))
            {
                SelectShape(index);
                sessionData.validResponse = true;
            }
        }

        // Check if shape select
        private bool TryGetShapeIndex(string shapeName, out int index)
        {
            index = _shapes == null ? -1 : Array.FindIndex(_shapes, (s) => string.Equals(s.gameObject.name, shapeName, StringComparison.CurrentCultureIgnoreCase));
            return index != -1;
        }

        // Select shape
        public void SelectShape(int shapeIndex)
        {
            if (_shapeSelected != shapeIndex)
            {
                // Set index
                _shapeSelected = shapeIndex;

                // Return shape
                Renderer shape = _shapes != null && _shapeSelected >= 0 && _shapeSelected < _shapes.Length
                    ? _shapes[_shapeSelected]
                    : null;
                OnShapeSelected?.Invoke(shape);
            }
        }
        #endregion

        #region COLORS
        // Validate & set color
        [ValidatePartialIntent(intent:COLOR_SET_INTENT_ID,minConfidence:MIN_CONFIDENCE)]
        public void OnValidateColorSet(VoiceSession sessionData, string color)
        {
            Color c;
            if (TryGetColor(color, out c))
            {
                SetColor(c);
                sessionData.validResponse = true;
            }
        }

        // Try to get a color
        private bool TryGetColor(string colorName, out Color color)
        {
            // Check overrides
            if (_colorOverride != null)
            {
                int overrideIndex = Array.FindIndex(_colorOverride, (c) => string.Equals(c.colorID, colorName, StringComparison.CurrentCultureIgnoreCase));
                if (overrideIndex != -1)
                {
                    color = _colorOverride[overrideIndex].color;
                    return true;
                }
            }
            // Check default
            if (ColorUtility.TryParseHtmlString(colorName, out color))
            {
                return true;
            }
            // Failed
            return false;
        }

        // Set color
        public void SetColor(Color newColor)
        {
            // Set all colors
            if (_shapes == null || _shapeSelected < 0 || _shapeSelected >= _shapes.Length)
            {
                foreach (var shape in _shapes)
                {
                    SetColor(shape, newColor);
                }
            }
            // Set selected color
            else
            {
                SetColor(_shapes[_shapeSelected], newColor);
            }
        }
        // Set color on a renderer
        private void SetColor(Renderer shape, Color color)
        {
            if (shape != null)
            {
                shape.material.color = color;
                OnShapeColorChanged?.Invoke(shape, color);
            }
        }
        #endregion
    }
}
