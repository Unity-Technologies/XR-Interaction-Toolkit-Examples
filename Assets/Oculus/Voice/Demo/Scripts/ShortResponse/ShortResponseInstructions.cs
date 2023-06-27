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
using UnityEngine.UI;

namespace Oculus.Voice.Demo
{
    public class ShortResponseInstructions : MonoBehaviour
    {
        // Instructions label
        [Header("UI Settings")]
        [SerializeField] private Text _instructionsLabel;
        // Handles shape changes
        private ShortResponseColorHandler _handler;

        [Header("Text Settings")]
        private string _shapeMissingText = "Say a phrase to select a shape such as 'Select Cube'.";
        private string _shapeSelectedText = $"{SHAPE_KEY} Selected.  Say a color to set the [SHAPE]'s color.";
        private const string SHAPE_KEY = "[SHAPE]";

        // Add delegates
        private void OnEnable()
        {
            if (_handler == null)
            {
                _handler = gameObject.GetComponent<ShortResponseColorHandler>();
            }
            ShapeSelected(null);
            if (_handler != null)
            {
                _handler.OnShapeSelected += ShapeSelected;
            }
        }
        // Remove delegates
        private void OnDisable()
        {
            if (_handler != null)
            {
                _handler.OnShapeSelected -= ShapeSelected;
            }
        }

        // Shape selected
        private void ShapeSelected(Renderer newShape)
        {
            if (newShape == null)
            {
                _instructionsLabel.text = _shapeMissingText;
            }
            else
            {
                string shapeName = newShape.gameObject.name;
                _instructionsLabel.text = _shapeSelectedText.Replace(SHAPE_KEY, shapeName);
            }
        }
    }
}
