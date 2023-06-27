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

/// <summary>
/// Descriptive labels of the <see cref="OVRAnchor"/>, as comma separated strings.
/// </summary>
/// <remarks>
/// This component can be accessed from an <see cref="OVRAnchor"/> that supports it by calling
/// <see cref="OVRAnchor.GetComponent{T}"/> from the anchor.
/// </remarks>
/// <seealso cref="Labels"/>
public readonly partial struct OVRSemanticLabels : IOVRAnchorComponent<OVRSemanticLabels>, IEquatable<OVRSemanticLabels>
{
    // Features
    /// <summary>
    /// Semantic Labels
    /// </summary>
    /// <returns>
    /// <para>Comma-separated values in one <see cref="string"/></para>
    /// </returns>
    /// <exception cref="Exception">If it fails to get the semantic labels</exception>
    public string Labels
    {
        get
        {
            if (!OVRPlugin.GetSpaceSemanticLabels(Handle, out var labels))
            {
                throw new Exception("Could not Get Semantic Labels");
            }

            return OVRSemanticClassification.ValidateAndUpgradeLabels(labels);
        }
    }
}
