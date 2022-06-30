/**************************************************************************************************
 * Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.
 *
 * Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
 * under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 **************************************************************************************************/

using System;
using Facebook.WitAi;
using Facebook.WitAi.Lib;
using UnityEngine;

namespace Oculus.Voice.Demo.UIShapesDemo
{
    public class ColorChanger : MonoBehaviour
    {
        /// <summary>
        /// Directly processes a command result getting the slots with WitResult utilities
        /// </summary>
        /// <param name="commandResult">Result data from Wit.ai activation to be processed</param>
        public void UpdateColor(WitResponseNode commandResult)
        {
            string colorName = commandResult.GetFirstEntityValue("color:color");
            string shape = commandResult.GetFirstEntityValue("shape:shape");
            UpdateColor(colorName, shape);
        }

        /// <summary>
        /// Processes the values of a result handler with a color and shape filter.
        /// </summary>
        /// <param name="results">Results from result handler [0] color name, [1] shape</param>
        public void UpdateColor(string[] results)
        {
            var colorName = results[0];
            var shape = results[1];

            UpdateColor(colorName, shape);
        }

        /// <summary>
        /// Updates the color of a shape or all shapes
        /// </summary>
        /// <param name="colorName">The name of a color to be processed</param>
        /// <param name="shape">The shape name or if empty all shapes</param>
        public void UpdateColor(string colorName, string shape)
        {
            if (ColorUtility.TryParseHtmlString(colorName, out var color))
            {
                if (string.IsNullOrEmpty(shape) || shape == "color")
                {
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        transform.GetChild(i).GetComponent<Renderer>().material.color = color;
                    }
                }
                else
                {
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        Transform child = transform.GetChild(i);
                        if (String.Equals(shape, child.name,
                            StringComparison.CurrentCultureIgnoreCase))
                        {
                            child.GetComponent<Renderer>().material.color = color;
                        }
                    }
                }
            }
        }
    }
}
