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
using static OVRPlugin;

public readonly partial struct OVRLocatable : IOVRAnchorComponent<OVRLocatable>, IEquatable<OVRLocatable>
{
    SpaceComponentType IOVRAnchorComponent<OVRLocatable>.Type => Type;

    ulong IOVRAnchorComponent<OVRLocatable>.Handle => Handle;

    OVRLocatable IOVRAnchorComponent<OVRLocatable>.FromAnchor(OVRAnchor anchor) => new OVRLocatable(anchor);

    /// <summary>
    /// A null representation of an OVRLocatable.
    /// </summary>
    /// <remarks>
    /// Use this to compare with another component to determine whether it is null.
    /// </remarks>
    public static readonly OVRLocatable Null = default;

    /// <summary>
    /// Whether this object represents a valid anchor component.
    /// </summary>
    public bool IsNull => Handle == 0;

    /// <summary>
    /// True if this component is enabled and no change to its enabled status is pending.
    /// </summary>
    public bool IsEnabled => !IsNull && GetSpaceComponentStatus(Handle, Type, out var enabled, out var pending) && enabled && !pending;

    /// <summary>
    /// Sets the enabled status of this component.
    /// </summary>
    /// <remarks>
    /// A component must be enabled to access its data.
    /// </remarks>
    /// <param name="enabled">The desired state of the component.</param>
    /// <param name="timeout">The timeout, in seconds, for the operation. Use zero to indicate an infinite timeout.</param>
    /// <returns>Returns an <see cref="OVRTask{T}" /> whose result indicates the result of the operation.</returns>
    public OVRTask<bool> SetEnabledAsync(bool enabled, double timeout = 0) => SetSpaceComponentStatus(Handle, Type, enabled, timeout, out var requestId)
            ? OVRTask.FromRequest<bool>(requestId)
            : OVRTask.FromResult(false);

    /// <summary>
    /// Compares this component for equality with <paramref name="other" />.
    /// </summary>
    /// <param name="other">The other component to compare with.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public bool Equals(OVRLocatable other) => Handle == other.Handle;

    /// <summary>
    /// Compares two components for equality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator ==(OVRLocatable lhs, OVRLocatable rhs) => lhs.Equals(rhs);

    /// <summary>
    /// Compares two components for inequality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if the components do not belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator !=(OVRLocatable lhs, OVRLocatable rhs) => !lhs.Equals(rhs);

    /// <summary>
    /// Compares this component for equality with <paramref name="obj" />.
    /// </summary>
    /// <param name="obj">The `object` to compare with.</param>
    /// <returns>True if <paramref name="obj" /> is an OVRLocatable and <see cref="Equals(OVRLocatable)" /> is true, otherwise false.</returns>
    public override bool Equals(object obj) => obj is OVRLocatable other && Equals(other);

    /// <summary>
    /// Gets a hashcode suitable for use in a Dictionary or HashSet.
    /// </summary>
    /// <returns>A hashcode for this component.</returns>
    public override int GetHashCode() => unchecked(Handle.GetHashCode() * 486187739 + ((int)Type).GetHashCode());

    /// <summary>
    /// Gets a string representation of this component.
    /// </summary>
    /// <returns>A string representation of this component.</returns>
    public override string ToString() => $"{Handle}.Locatable";

    internal SpaceComponentType Type => SpaceComponentType.Locatable;

    internal ulong Handle { get; }

    private OVRLocatable(OVRAnchor anchor) => Handle = anchor.Handle;
}

public readonly partial struct OVRStorable : IOVRAnchorComponent<OVRStorable>, IEquatable<OVRStorable>
{
    SpaceComponentType IOVRAnchorComponent<OVRStorable>.Type => Type;

    ulong IOVRAnchorComponent<OVRStorable>.Handle => Handle;

    OVRStorable IOVRAnchorComponent<OVRStorable>.FromAnchor(OVRAnchor anchor) => new OVRStorable(anchor);

    /// <summary>
    /// A null representation of an OVRStorable.
    /// </summary>
    /// <remarks>
    /// Use this to compare with another component to determine whether it is null.
    /// </remarks>
    public static readonly OVRStorable Null = default;

    /// <summary>
    /// Whether this object represents a valid anchor component.
    /// </summary>
    public bool IsNull => Handle == 0;

    /// <summary>
    /// True if this component is enabled and no change to its enabled status is pending.
    /// </summary>
    public bool IsEnabled => !IsNull && GetSpaceComponentStatus(Handle, Type, out var enabled, out var pending) && enabled && !pending;

    /// <summary>
    /// Sets the enabled status of this component.
    /// </summary>
    /// <remarks>
    /// A component must be enabled to access its data.
    /// </remarks>
    /// <param name="enabled">The desired state of the component.</param>
    /// <param name="timeout">The timeout, in seconds, for the operation. Use zero to indicate an infinite timeout.</param>
    /// <returns>Returns an <see cref="OVRTask{T}" /> whose result indicates the result of the operation.</returns>
    public OVRTask<bool> SetEnabledAsync(bool enabled, double timeout = 0) => SetSpaceComponentStatus(Handle, Type, enabled, timeout, out var requestId)
            ? OVRTask.FromRequest<bool>(requestId)
            : OVRTask.FromResult(false);

    /// <summary>
    /// Compares this component for equality with <paramref name="other" />.
    /// </summary>
    /// <param name="other">The other component to compare with.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public bool Equals(OVRStorable other) => Handle == other.Handle;

    /// <summary>
    /// Compares two components for equality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator ==(OVRStorable lhs, OVRStorable rhs) => lhs.Equals(rhs);

    /// <summary>
    /// Compares two components for inequality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if the components do not belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator !=(OVRStorable lhs, OVRStorable rhs) => !lhs.Equals(rhs);

    /// <summary>
    /// Compares this component for equality with <paramref name="obj" />.
    /// </summary>
    /// <param name="obj">The `object` to compare with.</param>
    /// <returns>True if <paramref name="obj" /> is an OVRStorable and <see cref="Equals(OVRStorable)" /> is true, otherwise false.</returns>
    public override bool Equals(object obj) => obj is OVRStorable other && Equals(other);

    /// <summary>
    /// Gets a hashcode suitable for use in a Dictionary or HashSet.
    /// </summary>
    /// <returns>A hashcode for this component.</returns>
    public override int GetHashCode() => unchecked(Handle.GetHashCode() * 486187739 + ((int)Type).GetHashCode());

    /// <summary>
    /// Gets a string representation of this component.
    /// </summary>
    /// <returns>A string representation of this component.</returns>
    public override string ToString() => $"{Handle}.Storable";

    internal SpaceComponentType Type => SpaceComponentType.Storable;

    internal ulong Handle { get; }

    private OVRStorable(OVRAnchor anchor) => Handle = anchor.Handle;
}

public readonly partial struct OVRSharable : IOVRAnchorComponent<OVRSharable>, IEquatable<OVRSharable>
{
    SpaceComponentType IOVRAnchorComponent<OVRSharable>.Type => Type;

    ulong IOVRAnchorComponent<OVRSharable>.Handle => Handle;

    OVRSharable IOVRAnchorComponent<OVRSharable>.FromAnchor(OVRAnchor anchor) => new OVRSharable(anchor);

    /// <summary>
    /// A null representation of an OVRSharable.
    /// </summary>
    /// <remarks>
    /// Use this to compare with another component to determine whether it is null.
    /// </remarks>
    public static readonly OVRSharable Null = default;

    /// <summary>
    /// Whether this object represents a valid anchor component.
    /// </summary>
    public bool IsNull => Handle == 0;

    /// <summary>
    /// True if this component is enabled and no change to its enabled status is pending.
    /// </summary>
    public bool IsEnabled => !IsNull && GetSpaceComponentStatus(Handle, Type, out var enabled, out var pending) && enabled && !pending;

    /// <summary>
    /// Sets the enabled status of this component.
    /// </summary>
    /// <remarks>
    /// A component must be enabled to access its data.
    /// </remarks>
    /// <param name="enabled">The desired state of the component.</param>
    /// <param name="timeout">The timeout, in seconds, for the operation. Use zero to indicate an infinite timeout.</param>
    /// <returns>Returns an <see cref="OVRTask{T}" /> whose result indicates the result of the operation.</returns>
    public OVRTask<bool> SetEnabledAsync(bool enabled, double timeout = 0) => SetSpaceComponentStatus(Handle, Type, enabled, timeout, out var requestId)
            ? OVRTask.FromRequest<bool>(requestId)
            : OVRTask.FromResult(false);

    /// <summary>
    /// Compares this component for equality with <paramref name="other" />.
    /// </summary>
    /// <param name="other">The other component to compare with.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public bool Equals(OVRSharable other) => Handle == other.Handle;

    /// <summary>
    /// Compares two components for equality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator ==(OVRSharable lhs, OVRSharable rhs) => lhs.Equals(rhs);

    /// <summary>
    /// Compares two components for inequality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if the components do not belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator !=(OVRSharable lhs, OVRSharable rhs) => !lhs.Equals(rhs);

    /// <summary>
    /// Compares this component for equality with <paramref name="obj" />.
    /// </summary>
    /// <param name="obj">The `object` to compare with.</param>
    /// <returns>True if <paramref name="obj" /> is an OVRSharable and <see cref="Equals(OVRSharable)" /> is true, otherwise false.</returns>
    public override bool Equals(object obj) => obj is OVRSharable other && Equals(other);

    /// <summary>
    /// Gets a hashcode suitable for use in a Dictionary or HashSet.
    /// </summary>
    /// <returns>A hashcode for this component.</returns>
    public override int GetHashCode() => unchecked(Handle.GetHashCode() * 486187739 + ((int)Type).GetHashCode());

    /// <summary>
    /// Gets a string representation of this component.
    /// </summary>
    /// <returns>A string representation of this component.</returns>
    public override string ToString() => $"{Handle}.Sharable";

    internal SpaceComponentType Type => SpaceComponentType.Sharable;

    internal ulong Handle { get; }

    private OVRSharable(OVRAnchor anchor) => Handle = anchor.Handle;
}

public readonly partial struct OVRBounded2D : IOVRAnchorComponent<OVRBounded2D>, IEquatable<OVRBounded2D>
{
    SpaceComponentType IOVRAnchorComponent<OVRBounded2D>.Type => Type;

    ulong IOVRAnchorComponent<OVRBounded2D>.Handle => Handle;

    OVRBounded2D IOVRAnchorComponent<OVRBounded2D>.FromAnchor(OVRAnchor anchor) => new OVRBounded2D(anchor);

    /// <summary>
    /// A null representation of an OVRBounded2D.
    /// </summary>
    /// <remarks>
    /// Use this to compare with another component to determine whether it is null.
    /// </remarks>
    public static readonly OVRBounded2D Null = default;

    /// <summary>
    /// Whether this object represents a valid anchor component.
    /// </summary>
    public bool IsNull => Handle == 0;

    /// <summary>
    /// True if this component is enabled and no change to its enabled status is pending.
    /// </summary>
    public bool IsEnabled => !IsNull && GetSpaceComponentStatus(Handle, Type, out var enabled, out var pending) && enabled && !pending;

    OVRTask<bool> IOVRAnchorComponent<OVRBounded2D>.SetEnabledAsync(bool enabled, double timeout)
        => throw new NotSupportedException("The Bounded2D component cannot be enabled or disabled.");

    /// <summary>
    /// Compares this component for equality with <paramref name="other" />.
    /// </summary>
    /// <param name="other">The other component to compare with.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public bool Equals(OVRBounded2D other) => Handle == other.Handle;

    /// <summary>
    /// Compares two components for equality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator ==(OVRBounded2D lhs, OVRBounded2D rhs) => lhs.Equals(rhs);

    /// <summary>
    /// Compares two components for inequality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if the components do not belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator !=(OVRBounded2D lhs, OVRBounded2D rhs) => !lhs.Equals(rhs);

    /// <summary>
    /// Compares this component for equality with <paramref name="obj" />.
    /// </summary>
    /// <param name="obj">The `object` to compare with.</param>
    /// <returns>True if <paramref name="obj" /> is an OVRBounded2D and <see cref="Equals(OVRBounded2D)" /> is true, otherwise false.</returns>
    public override bool Equals(object obj) => obj is OVRBounded2D other && Equals(other);

    /// <summary>
    /// Gets a hashcode suitable for use in a Dictionary or HashSet.
    /// </summary>
    /// <returns>A hashcode for this component.</returns>
    public override int GetHashCode() => unchecked(Handle.GetHashCode() * 486187739 + ((int)Type).GetHashCode());

    /// <summary>
    /// Gets a string representation of this component.
    /// </summary>
    /// <returns>A string representation of this component.</returns>
    public override string ToString() => $"{Handle}.Bounded2D";

    internal SpaceComponentType Type => SpaceComponentType.Bounded2D;

    internal ulong Handle { get; }

    private OVRBounded2D(OVRAnchor anchor) => Handle = anchor.Handle;
}

public readonly partial struct OVRBounded3D : IOVRAnchorComponent<OVRBounded3D>, IEquatable<OVRBounded3D>
{
    SpaceComponentType IOVRAnchorComponent<OVRBounded3D>.Type => Type;

    ulong IOVRAnchorComponent<OVRBounded3D>.Handle => Handle;

    OVRBounded3D IOVRAnchorComponent<OVRBounded3D>.FromAnchor(OVRAnchor anchor) => new OVRBounded3D(anchor);

    /// <summary>
    /// A null representation of an OVRBounded3D.
    /// </summary>
    /// <remarks>
    /// Use this to compare with another component to determine whether it is null.
    /// </remarks>
    public static readonly OVRBounded3D Null = default;

    /// <summary>
    /// Whether this object represents a valid anchor component.
    /// </summary>
    public bool IsNull => Handle == 0;

    /// <summary>
    /// True if this component is enabled and no change to its enabled status is pending.
    /// </summary>
    public bool IsEnabled => !IsNull && GetSpaceComponentStatus(Handle, Type, out var enabled, out var pending) && enabled && !pending;

    OVRTask<bool> IOVRAnchorComponent<OVRBounded3D>.SetEnabledAsync(bool enabled, double timeout)
        => throw new NotSupportedException("The Bounded3D component cannot be enabled or disabled.");

    /// <summary>
    /// Compares this component for equality with <paramref name="other" />.
    /// </summary>
    /// <param name="other">The other component to compare with.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public bool Equals(OVRBounded3D other) => Handle == other.Handle;

    /// <summary>
    /// Compares two components for equality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator ==(OVRBounded3D lhs, OVRBounded3D rhs) => lhs.Equals(rhs);

    /// <summary>
    /// Compares two components for inequality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if the components do not belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator !=(OVRBounded3D lhs, OVRBounded3D rhs) => !lhs.Equals(rhs);

    /// <summary>
    /// Compares this component for equality with <paramref name="obj" />.
    /// </summary>
    /// <param name="obj">The `object` to compare with.</param>
    /// <returns>True if <paramref name="obj" /> is an OVRBounded3D and <see cref="Equals(OVRBounded3D)" /> is true, otherwise false.</returns>
    public override bool Equals(object obj) => obj is OVRBounded3D other && Equals(other);

    /// <summary>
    /// Gets a hashcode suitable for use in a Dictionary or HashSet.
    /// </summary>
    /// <returns>A hashcode for this component.</returns>
    public override int GetHashCode() => unchecked(Handle.GetHashCode() * 486187739 + ((int)Type).GetHashCode());

    /// <summary>
    /// Gets a string representation of this component.
    /// </summary>
    /// <returns>A string representation of this component.</returns>
    public override string ToString() => $"{Handle}.Bounded3D";

    internal SpaceComponentType Type => SpaceComponentType.Bounded3D;

    internal ulong Handle { get; }

    private OVRBounded3D(OVRAnchor anchor) => Handle = anchor.Handle;
}

public readonly partial struct OVRSemanticLabels : IOVRAnchorComponent<OVRSemanticLabels>, IEquatable<OVRSemanticLabels>
{
    SpaceComponentType IOVRAnchorComponent<OVRSemanticLabels>.Type => Type;

    ulong IOVRAnchorComponent<OVRSemanticLabels>.Handle => Handle;

    OVRSemanticLabels IOVRAnchorComponent<OVRSemanticLabels>.FromAnchor(OVRAnchor anchor) => new OVRSemanticLabels(anchor);

    /// <summary>
    /// A null representation of an OVRSemanticLabels.
    /// </summary>
    /// <remarks>
    /// Use this to compare with another component to determine whether it is null.
    /// </remarks>
    public static readonly OVRSemanticLabels Null = default;

    /// <summary>
    /// Whether this object represents a valid anchor component.
    /// </summary>
    public bool IsNull => Handle == 0;

    /// <summary>
    /// True if this component is enabled and no change to its enabled status is pending.
    /// </summary>
    public bool IsEnabled => !IsNull && GetSpaceComponentStatus(Handle, Type, out var enabled, out var pending) && enabled && !pending;

    OVRTask<bool> IOVRAnchorComponent<OVRSemanticLabels>.SetEnabledAsync(bool enabled, double timeout)
        => throw new NotSupportedException("The SemanticLabels component cannot be enabled or disabled.");

    /// <summary>
    /// Compares this component for equality with <paramref name="other" />.
    /// </summary>
    /// <param name="other">The other component to compare with.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public bool Equals(OVRSemanticLabels other) => Handle == other.Handle;

    /// <summary>
    /// Compares two components for equality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator ==(OVRSemanticLabels lhs, OVRSemanticLabels rhs) => lhs.Equals(rhs);

    /// <summary>
    /// Compares two components for inequality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if the components do not belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator !=(OVRSemanticLabels lhs, OVRSemanticLabels rhs) => !lhs.Equals(rhs);

    /// <summary>
    /// Compares this component for equality with <paramref name="obj" />.
    /// </summary>
    /// <param name="obj">The `object` to compare with.</param>
    /// <returns>True if <paramref name="obj" /> is an OVRSemanticLabels and <see cref="Equals(OVRSemanticLabels)" /> is true, otherwise false.</returns>
    public override bool Equals(object obj) => obj is OVRSemanticLabels other && Equals(other);

    /// <summary>
    /// Gets a hashcode suitable for use in a Dictionary or HashSet.
    /// </summary>
    /// <returns>A hashcode for this component.</returns>
    public override int GetHashCode() => unchecked(Handle.GetHashCode() * 486187739 + ((int)Type).GetHashCode());

    /// <summary>
    /// Gets a string representation of this component.
    /// </summary>
    /// <returns>A string representation of this component.</returns>
    public override string ToString() => $"{Handle}.SemanticLabels";

    internal SpaceComponentType Type => SpaceComponentType.SemanticLabels;

    internal ulong Handle { get; }

    private OVRSemanticLabels(OVRAnchor anchor) => Handle = anchor.Handle;
}

public readonly partial struct OVRRoomLayout : IOVRAnchorComponent<OVRRoomLayout>, IEquatable<OVRRoomLayout>
{
    SpaceComponentType IOVRAnchorComponent<OVRRoomLayout>.Type => Type;

    ulong IOVRAnchorComponent<OVRRoomLayout>.Handle => Handle;

    OVRRoomLayout IOVRAnchorComponent<OVRRoomLayout>.FromAnchor(OVRAnchor anchor) => new OVRRoomLayout(anchor);

    /// <summary>
    /// A null representation of an OVRRoomLayout.
    /// </summary>
    /// <remarks>
    /// Use this to compare with another component to determine whether it is null.
    /// </remarks>
    public static readonly OVRRoomLayout Null = default;

    /// <summary>
    /// Whether this object represents a valid anchor component.
    /// </summary>
    public bool IsNull => Handle == 0;

    /// <summary>
    /// True if this component is enabled and no change to its enabled status is pending.
    /// </summary>
    public bool IsEnabled => !IsNull && GetSpaceComponentStatus(Handle, Type, out var enabled, out var pending) && enabled && !pending;

    OVRTask<bool> IOVRAnchorComponent<OVRRoomLayout>.SetEnabledAsync(bool enabled, double timeout)
        => throw new NotSupportedException("The RoomLayout component cannot be enabled or disabled.");

    /// <summary>
    /// Compares this component for equality with <paramref name="other" />.
    /// </summary>
    /// <param name="other">The other component to compare with.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public bool Equals(OVRRoomLayout other) => Handle == other.Handle;

    /// <summary>
    /// Compares two components for equality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator ==(OVRRoomLayout lhs, OVRRoomLayout rhs) => lhs.Equals(rhs);

    /// <summary>
    /// Compares two components for inequality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if the components do not belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator !=(OVRRoomLayout lhs, OVRRoomLayout rhs) => !lhs.Equals(rhs);

    /// <summary>
    /// Compares this component for equality with <paramref name="obj" />.
    /// </summary>
    /// <param name="obj">The `object` to compare with.</param>
    /// <returns>True if <paramref name="obj" /> is an OVRRoomLayout and <see cref="Equals(OVRRoomLayout)" /> is true, otherwise false.</returns>
    public override bool Equals(object obj) => obj is OVRRoomLayout other && Equals(other);

    /// <summary>
    /// Gets a hashcode suitable for use in a Dictionary or HashSet.
    /// </summary>
    /// <returns>A hashcode for this component.</returns>
    public override int GetHashCode() => unchecked(Handle.GetHashCode() * 486187739 + ((int)Type).GetHashCode());

    /// <summary>
    /// Gets a string representation of this component.
    /// </summary>
    /// <returns>A string representation of this component.</returns>
    public override string ToString() => $"{Handle}.RoomLayout";

    internal SpaceComponentType Type => SpaceComponentType.RoomLayout;

    internal ulong Handle { get; }

    private OVRRoomLayout(OVRAnchor anchor) => Handle = anchor.Handle;
}

public readonly partial struct OVRAnchorContainer : IOVRAnchorComponent<OVRAnchorContainer>, IEquatable<OVRAnchorContainer>
{
    SpaceComponentType IOVRAnchorComponent<OVRAnchorContainer>.Type => Type;

    ulong IOVRAnchorComponent<OVRAnchorContainer>.Handle => Handle;

    OVRAnchorContainer IOVRAnchorComponent<OVRAnchorContainer>.FromAnchor(OVRAnchor anchor) => new OVRAnchorContainer(anchor);

    /// <summary>
    /// A null representation of an OVRAnchorContainer.
    /// </summary>
    /// <remarks>
    /// Use this to compare with another component to determine whether it is null.
    /// </remarks>
    public static readonly OVRAnchorContainer Null = default;

    /// <summary>
    /// Whether this object represents a valid anchor component.
    /// </summary>
    public bool IsNull => Handle == 0;

    /// <summary>
    /// True if this component is enabled and no change to its enabled status is pending.
    /// </summary>
    public bool IsEnabled => !IsNull && GetSpaceComponentStatus(Handle, Type, out var enabled, out var pending) && enabled && !pending;

    OVRTask<bool> IOVRAnchorComponent<OVRAnchorContainer>.SetEnabledAsync(bool enabled, double timeout)
        => throw new NotSupportedException("The AnchorContainer component cannot be enabled or disabled.");

    /// <summary>
    /// Compares this component for equality with <paramref name="other" />.
    /// </summary>
    /// <param name="other">The other component to compare with.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public bool Equals(OVRAnchorContainer other) => Handle == other.Handle;

    /// <summary>
    /// Compares two components for equality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if both components belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator ==(OVRAnchorContainer lhs, OVRAnchorContainer rhs) => lhs.Equals(rhs);

    /// <summary>
    /// Compares two components for inequality.
    /// </summary>
    /// <param name="lhs">The component to compare with <paramref name="rhs" />.</param>
    /// <param name="rhs">The component to compare with <paramref name="lhs" />.</param>
    /// <returns>True if the components do not belong to the same <see cref="OVRAnchor" />, otherwise false.</returns>
    public static bool operator !=(OVRAnchorContainer lhs, OVRAnchorContainer rhs) => !lhs.Equals(rhs);

    /// <summary>
    /// Compares this component for equality with <paramref name="obj" />.
    /// </summary>
    /// <param name="obj">The `object` to compare with.</param>
    /// <returns>True if <paramref name="obj" /> is an OVRAnchorContainer and <see cref="Equals(OVRAnchorContainer)" /> is true, otherwise false.</returns>
    public override bool Equals(object obj) => obj is OVRAnchorContainer other && Equals(other);

    /// <summary>
    /// Gets a hashcode suitable for use in a Dictionary or HashSet.
    /// </summary>
    /// <returns>A hashcode for this component.</returns>
    public override int GetHashCode() => unchecked(Handle.GetHashCode() * 486187739 + ((int)Type).GetHashCode());

    /// <summary>
    /// Gets a string representation of this component.
    /// </summary>
    /// <returns>A string representation of this component.</returns>
    public override string ToString() => $"{Handle}.AnchorContainer";

    internal SpaceComponentType Type => SpaceComponentType.SpaceContainer;

    internal ulong Handle { get; }

    private OVRAnchorContainer(OVRAnchor anchor) => Handle = anchor.Handle;
}

