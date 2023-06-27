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
using Meta.WitAi.Json;
using UnityEngine;

namespace Oculus.Voice.Demo.UIShapesDemo
{
    public class ColorChangerConduit : MonoBehaviour
    {
        /// <summary>
        /// Sets the ColorName of the specified transform.
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="color"></param>
        private void SetColor(Transform trans, Color color)
        {
            trans.GetComponent<Renderer>().material.color = color;
        }

        /// <summary>
        /// Gets called automatically by ConduitDispatcher. Updates the colors of a shape
        /// </summary>
        /// <param name="color">The names of the colors to be processed</param>
        /// <param name="shape">The ShapeName names or if empty all shapes</param>
        [MatchIntent("change_color")]
        private void ChangeColor(ColorName color, ShapeName shape)
        {
            if (!ColorUtility.TryParseHtmlString(color.ToString(), out var _color)) return;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (String.Equals(shape.ToString(), child.name,
                        StringComparison.CurrentCultureIgnoreCase))
                {
                    SetColor(child, _color);
                    break;
                }
            }
        }
    }
}
