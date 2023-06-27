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
using System.Collections.Generic;

/// <summary>
/// Represents a container for other <see cref="OVRAnchor"/>s.
/// </summary>
/// <remarks>
/// This component can be accessed from an <see cref="OVRAnchor"/> that supports it by calling
/// <see cref="OVRAnchor.GetComponent{T}"/> from the anchor.s
/// </remarks>
/// <seealso cref="Uuids"/>
/// <seealso cref="FetchChildrenAsync"/>
public readonly partial struct OVRAnchorContainer : IOVRAnchorComponent<OVRAnchorContainer>,
    IEquatable<OVRAnchorContainer>
{
    /// <summary>
    /// Uuids of the anchors contained by this Anchor Container.
    /// </summary>
    /// <seealso cref="OVRAnchor.FetchAnchorsAsync"/>
    /// <exception cref="InvalidOperationException">If it fails to retrieve the Uuids, which could happen if the component is not supported or enabled.</exception>
    public Guid[] Uuids => OVRPlugin.GetSpaceContainer(Handle, out var containerUuids)
        ? containerUuids
        : throw new InvalidOperationException("Could not get Uuids");

    /// <summary>
    /// Asynchronous method that fetches anchors contained by this Anchor Container.
    /// </summary>
    /// <param name="anchors">IList that will get cleared and populated with the requested anchors.</param>
    /// <remarks>Dispose of the returned <see cref="OVRTask{bool}"/> if you don't use the results</remarks>
    /// <returns>An <see cref="OVRTask{bool}"/> that will eventually let you test if the fetch was successful or not.
    /// If the result is true, then the <see cref="anchors"/> parameter has been populated with the requested anchors.</returns>
    /// <exception cref="InvalidOperationException">If it fails to retrieve the Uuids</exception>
    /// <exception cref="ArgumentNullException">If parameter anchors is null</exception>
    public OVRTask<bool> FetchChildrenAsync(List<OVRAnchor> anchors) => OVRAnchor.FetchAnchorsAsync(Uuids, anchors);
}
