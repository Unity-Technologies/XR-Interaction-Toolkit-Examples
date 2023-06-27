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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    public static class AssertUtils
    {
        public const string HiglightColor = "#3366ff";

        /// <summary>
        /// Asserts that the expression is True.
        /// In case of failure, it will print an error showing where the failure happened.
        /// </summary>
        /// <param name="component">The component to which the value expression belongs</param>
        /// <param name="value">The expression that should be true</param>
        /// <param name="whyItFailed">Optional parameter specifying the reason the assert failed</param>
        /// <param name="whereItFailed">Optional parameter specifying where the failure can be found.
        /// If none is provided it will print the component address (GameObject->Component).</param>
        /// <param name="howToFix">Optional parameter suggesting how to fix the problem.</param>
        [Conditional("UNITY_ASSERTIONS")]
        public static void AssertIsTrue(this Component component,
            bool value,
            string whyItFailed = null, string whereItFailed = null, string howToFix = null)
        {
            string gameObjectName = component.name;
            string componentName = component.GetType().Name;

            Assert.IsTrue(value,
                (whereItFailed ?? $"At GameObject <color={ HiglightColor}><b>{ gameObjectName}</b></color>, component <b>{ componentName}</b>. ") +
                (whyItFailed ?? "") +
                (howToFix ?? ""));
        }

        /// <summary>
        /// Asserts that an Aspect exists.
        /// In case of failure, it will print an error showing why it failed, where it failed
        /// and suggest how to fix it.
        /// </summary>
        /// <typeparam name="TValue">The type of Aspect to be checked</typeparam>
        /// <param name="component">The component requiring this Aspect</param>
        /// <param name="aspect">The Aspect to be checked</param>
        /// <param name="aspectLocation">The expected location of the Aspect</param>
        /// <param name="whyItFailed">Optional parameter specifying the reason the assert failed.
        /// If none is provided it will print the expected aspect type and location.</param>
        /// <param name="whereFailed">Optional parameter specifying where the failure can be found.
        /// If none is provided it will print the component address (GameObject->Component).</param>
        /// <param name="howToFix">Optional parameter suggesting how to fix the problem.
        /// If none is provided it will suggest assigning an Aspect of the required type to the expected location.</param>
        [Conditional("UNITY_ASSERTIONS")]
        public static void AssertAspect<TValue>(this Component component,
            TValue aspect, string aspectLocation,
            string whyItFailed = null, string whereFailed = null, string howToFix = null)
            where TValue : class
        {
            string gameObjectName = component.name;
            string componentName = component.GetType().Name;
            string niceVariableName = Nicify(aspectLocation);
            string aspectType = typeof(TValue).Name;

            Assert.IsNotNull(aspect,
                (whereFailed ?? $"At GameObject <color={HiglightColor}><b>{gameObjectName}</b></color>, component <b>{componentName}</b>. ") +
                (whyItFailed ?? $"Could not find the required Aspect <b>{aspectType}</b> in the associated <b>{niceVariableName}</b>. ") +
                (howToFix ?? $"Assign an Aspect of type <b>{aspectType}</b> to the associated <b>{niceVariableName}</b>."));
        }

        /// <summary>
        /// Asserts that a Serialized Field in a Component is not null.
        /// In case of failure, it will print an error showing why it failed, where it failed
        /// and suggest how to fix it.
        /// </summary>
        /// <typeparam name="TValue">The type of field to be checked</typeparam>
        /// <param name="component">The component to which this field belongs.</param>
        /// <param name="value">The value of the field.</param>
        /// <param name="variableName">The printed name of the serialized field.</param>
        /// <param name="whyItFailed">Optional parameter specifying the reason the assert failed.
        /// If none is provided it will indicate that the field value was null.</param>
        /// <param name="whereItFailed">Optional parameter specifying where the failure can be found.
        /// If none is provided it will print the component address (GameObject->Component->Field Name).</param>
        /// <param name="howToFix">Optional parameter suggesting how to fix the problem.
        /// If none is provided it will suggest assigning a value of the required type to the field.</param>
        [Conditional("UNITY_ASSERTIONS")]
        public static void AssertField<TValue>(this Component component,
            TValue value, string variableName,
            string whyItFailed = null, string whereItFailed = null, string howToFix = null)
            where TValue : class
        {
            string gameObjectName = component.name;
            string componentName = component.GetType().Name;
            string niceVariableName = Nicify(variableName);
            string variableType = typeof(TValue).Name;

            Assert.IsNotNull(value,
                (whereItFailed ?? $"At GameObject <color={HiglightColor}><b>{gameObjectName}</b></color>, component <b>{componentName}</b>. ") +
                (whyItFailed ?? $"Required <b>{niceVariableName}</b> reference is missing. ") +
                (howToFix ?? $"Assign a <b>{variableType}</b> to the field <b>{niceVariableName}</b>."));
        }

        /// <summary>
        /// Asserts that a Serialized collection in a Component is not null, is not empty and
        /// all of its items exist.
        /// In case of failure, it will print an error showing why it failed, where it failed
        /// and suggest how to fix it.
        /// </summary>
        /// <typeparam name="TValue">The type of the items in the collection.</typeparam>
        /// <param name="component">The component to which this collection belongs.</param>
        /// <param name="value">The value of the collection.</param>
        /// <param name="variableName">The printed name of the serialized collection.</param>
        /// <param name="whyItFailed">Optional parameter specifying the reason the assert failed.
        /// If none is provided it will indicate that the collection value needs at least one valid item.</param>
        /// <param name="whereFailed">Optional parameter specifying where the failure can be found.
        /// If none is provided it will print the component address (GameObject->Component->Collection Name).</param>
        /// <param name="howToFix">Optional parameter suggesting how to fix the problem.
        /// If none is provided it will suggest assigning at least one valid item of the required type to the collection.</param>
        [Conditional("UNITY_ASSERTIONS")]
        public static void AssertCollectionField<TValue>(this Component component,
            IEnumerable<TValue> value, string variableName,
            string whyItFailed = null, string whereFailed = null, string howToFix = null)
        {

            string gameObjectName = component.name;
            string componentName = component.GetType().Name;
            string niceVariableName = Nicify(variableName);
            string variableType = typeof(TValue).Name;

            Assert.IsTrue(value != null && value.Count() > 0,
                (whereFailed ?? $"At GameObject <color={HiglightColor}><b>{gameObjectName}</b></color>: the <b>{componentName}</b> component has an missing or empty <b>{niceVariableName}</b> collection. ") +
                (whyItFailed ?? "") +
                (howToFix ?? $"Assign at least one <b>{variableType}</b> to the collection <b>{niceVariableName}</b>. "));

            component.AssertCollectionItems(value, variableName);
        }

        /// <summary>
        /// Asserts that each item in a Serialized collection in a Component is not null.
        /// Note that the collection it might contain 0 items.
        /// In case of failure, it will print an error showing why it failed, where it failed
        /// and suggest how to fix it.
        /// </summary>
        /// <typeparam name="TValue">The type of the items in the collection</typeparam>
        /// <param name="component">The component to which the collection belongs.</param>
        /// <param name="value">The value of the collection.</param>
        /// <param name="variableName">The printed name of the serialized collection.</param>
        /// <param name="whyItFailed">Optional parameter specifying the reason the assert failed.
        /// If none is provided it will indicate, for each failed item, that the item must not be null.</param>
        /// <param name="whereItFailed">Optional parameter specifying where the failure can be found.
        /// If none is provided it will print the component address (GameObject->Component->Collection Name).</param>
        /// <param name="howToFix">Optional parameter suggesting how to fix the problem.
        /// If none is provided it will suggest assigning a valid item of the required type to the collection at each missing index.</param>
        [Conditional("UNITY_ASSERTIONS")]
        public static void AssertCollectionItems<TValue>(this Component component,
            IEnumerable<TValue> value, string variableName,
            string whyItFailed = null, string whereItFailed = null, string howToFix = null)
        {
            string gameObjectName = component.name;
            string componentName = component.GetType().Name;
            string niceVariableName = Nicify(variableName);
            string variableType = typeof(TValue).Name;

            int index = 0;
            foreach (TValue item in value)
            {
                Assert.IsFalse(item is null,
                    (whereItFailed ?? $"At GameObject <color={HiglightColor}><b>{gameObjectName}</b></color>, component <b>{componentName}</b>. ") +
                    (whyItFailed ?? $"Invalid item in the collection <b>{niceVariableName}</b> at index <b>{index}</b>. ") +
                    (howToFix ?? $"Assign a <b>{variableType}</b> to the collection <b>{niceVariableName}</b> at index <b>{index}</b>. "));
                index++;
            }
        }

        /// <summary>
        /// Make a displayable name for a variable.
        /// This function will insert spaces before capital letters and remove optional m_, _ or k followed by uppercase letter in front of the name.
        /// </summary>
        /// <param name="variableName">The variable name as used in code</param>
        /// <returns>The nicified variable</returns>
        public static string Nicify(string variableName)
        {
            variableName = Regex.Replace(variableName, "_([a-z])", match => match.Value.ToUpper(), RegexOptions.Compiled);
            variableName = Regex.Replace(variableName, "m_|_", " ", RegexOptions.Compiled);
            variableName = Regex.Replace(variableName, "k([A-Z])", "$1", RegexOptions.Compiled);
            variableName = Regex.Replace(variableName, "([A-Z])", " $1", RegexOptions.Compiled);
            variableName = variableName.Trim();
            return variableName;
        }
    }
}
