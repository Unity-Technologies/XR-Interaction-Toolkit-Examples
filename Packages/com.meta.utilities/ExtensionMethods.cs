// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.Utilities
{
    public static class ExtensionMethods
    {
        public static bool IsCloseTo(this float a, float b, float epsilon = 0.0001f)
        {
            return Mathf.Abs(a - b) < epsilon;
        }

        public static bool IsCloseTo(this Vector3 a, Vector3 b, float epsilonRadius = 0.0001f)
        {
            return (a - b).sqrMagnitude < epsilonRadius * epsilonRadius;
        }

        public static Delegate GetMethod<Delegate>(this object target, string name)
        {
            var flags = BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic;
            var methods = target.GetType().GetMethods(flags);
            foreach (var method in methods)
            {
                if (method.Name == name)
                {
                    try
                    {
                        return (Delegate)(object)method.CreateDelegate(typeof(Delegate), target);
                    }
                    catch (ArgumentException) { }
                }
            }
            return default;
        }

        public static IEnumerable<FieldInfo> GetAllFields(this Type target)
        {
            var flags = BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic;
            for (var type = target; type != null; type = type.BaseType)
            {
                var fields = type.GetFields(flags);
                foreach (var field in fields)
                {
                    yield return field;
                }
            }
        }

        public static object GetField(this object target, string name)
        {
            var flags = BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic;
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var fields = type.GetFields(flags);
                foreach (var field in fields)
                {
                    if (field.Name == name)
                    {
                        try
                        {
                            return field.GetValue(target);
                        }
                        catch (ArgumentException) { }
                    }
                }
            }
            return default;
        }

        public static object GetProperty(this object target, string name)
        {
            var flags = BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic;
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var props = type.GetProperties(flags);
                foreach (var prop in props)
                {
                    if (prop.Name == name)
                    {
                        try
                        {
                            return prop.GetValue(target);
                        }
                        catch (ArgumentException) { }
                    }
                }
            }
            return default;
        }

        public static void SetProperty(this object target, string name, object value)
        {
            var flags = BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic;
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var props = type.GetProperties(flags);
                foreach (var prop in props)
                {
                    if (prop.Name == name)
                    {
                        try
                        {
                            prop.SetValue(target, value);
                        }
                        catch (ArgumentException) { }
                    }
                }
            }
        }

        public static string ListToString<T>(this IEnumerable<T> list, string separator = ", ") => string.Join(separator, list);

        public static Vector3 WithZ(this Vector2 vec, float z) => new(vec.x, vec.y, z);

        public static string ToJson(this object obj, bool pretty = true) => JsonUtility.ToJson(obj, pretty);

        public static IEnumerable<(A first, B second)> Zip<A, B>(this IEnumerable<A> enumA, IEnumerable<B> enumB)
        {
            var a = enumA.GetEnumerator();
            var b = enumB.GetEnumerator();
            while (a.MoveNext() && b.MoveNext())
            {
                yield return (a.Current, b.Current);
            }
        }

        public static IEnumerable<KeyValuePair<int, T>> Enumerate<T>(this IEnumerable<T> values)
        {
            var i = 0;
            foreach (var value in values)
            {
                yield return new(i, value);
                i += 1;
            }
        }

        public static IEnumerable<T?> AsNullables<T>(this IEnumerable<T> values) where T : struct => values.Cast<T?>();

        public static IEnumerable<T> Except<T>(this IEnumerable<T> values, T exempt)
        {
            var comparer = EqualityComparer<T>.Default;
            foreach (var value in values)
                if (!comparer.Equals(value, exempt))
                    yield return value;
        }

        public static NativeArray<T> ToTempArray<T>(this IEnumerable<T> values, int maxLength) where T : unmanaged
        {
            var array = new NativeList<T>(maxLength, Allocator.Temp);
            foreach (var value in values)
            {
                array.AddNoResize(value);
            }
            return array.AsArray();
        }

        public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);
        public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);

        public static string IfNullOrEmpty(this string str, string other) => str.IsNullOrEmpty() ? other : str;

        public static IEnumerable<int> CountTo(this int end)
        {
            Assert.IsTrue(end >= 0, $"Cannot count to {end}.");

            for (var i = 0; i != end; ++i)
                yield return i;
        }

        public static IEnumerable<T> AsEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        public static IEnumerable AsEnumerable(this IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        public static IEnumerable<T> AsEnumerable<T>(this IEnumerator enumerator) =>
            enumerator.AsEnumerable().Cast<T>();

        public static Quaternion FromTo(this Quaternion from, Quaternion to) => Quaternion.Inverse(from) * to;
        public static Quaternion Scale(this Quaternion rotation, float t) => Quaternion.SlerpUnclamped(Quaternion.identity, rotation, t);

        /// in degrees
        public static Quaternion AxisAngleToQuaternion(this Vector3 axisAngle) =>
            Quaternion.AngleAxis(axisAngle.magnitude, axisAngle.normalized);

        /// in degrees
        public static Vector3 QuaternionToAxisAngle(this Quaternion rotation)
        {
            rotation.ToAngleAxis(out var angle, out var axis);
            return angle * axis;
        }

        /// in degrees
        public static Quaternion GetAngularVelocity(this Rigidbody rigidbody) =>
            (rigidbody.angularVelocity * Mathf.Rad2Deg).AxisAngleToQuaternion();

        /// in degrees
        public static void SetAngularVelocity(this Rigidbody rigidbody, Quaternion angularVelocity) =>
            rigidbody.angularVelocity = angularVelocity.QuaternionToAxisAngle() * Mathf.Deg2Rad;

        public static IEnumerator Then(this IEnumerator routine, Action action)
        {
            while (routine.MoveNext())
                yield return routine.Current;
            action();
        }

        public static IEnumerator CatchExceptions(this IEnumerator routine)
        {
            while (true)
            {
                try
                {
                    if (!routine.MoveNext())
                        break;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    break;
                }

                yield return routine.Current;
            }
        }

        public static void Deconstruct<TKey, TElement>(
            this IGrouping<TKey, TElement> grouping,
            out TKey key,
            out IEnumerable<TElement> values) =>
            (key, values) = (grouping.Key, grouping);

        public static int? IndexOf<T>(this IEnumerable<T> values, T value)
        {
            var comparer = EqualityComparer<T>.Default;
            var count = 0;
            foreach (var el in values)
            {
                if (comparer.Equals(el, value))
                    return count;
                count += 1;
            }
            return null;
        }

        public static IEnumerable<T> WhereExists<T>(this IEnumerable<T> values) where T : UnityEngine.Object
        {
            foreach (var value in values)
                if (value != null)
                    yield return value;
        }

        public static IEnumerable<T> WhereNonNull<T>(this IEnumerable<T> values) where T : class
        {
            foreach (var value in values)
                if (value != null)
                    yield return value;
        }

        public static IEnumerable<T> WhereNonNull<T>(this IEnumerable<T?> values) where T : struct
        {
            foreach (var value in values)
                if (value.HasValue)
                    yield return value.Value;
        }

        public static IEnumerable<T> WhereNonDefault<T>(this IEnumerable<T> values)
        {
            var comparer = EqualityComparer<T>.Default;
            foreach (var value in values)
                if (!comparer.Equals(value, default))
                    yield return value;
        }

        public static Vector3 Sum(this IEnumerable<Vector3> values)
        {
            var sum = Vector3.zero;
            foreach (var value in values)
                sum += value;
            return sum;
        }

        public static Vector3 Average(this IEnumerable<Vector3> values)
        {
            var avg = Vector3.zero;
            foreach (var (i, value) in values.Enumerate())
            {
                var mul = 1.0f / (i + 1);
                avg = avg * (i * mul) + value * mul;
            }
            return avg;
        }

        public static ref T RandomElement<T>(this T[] values) =>
            ref values[UnityEngine.Random.Range(0, values.Length)];

        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void FixEventImpl<T>(ref T evt) where T : Delegate
        {
            if (evt != null)
            {
                foreach (var d in evt.GetInvocationList())
                {
                    if (d.Target is UnityEngine.Object obj && obj == null)
                    {
                        var exception = new MissingReferenceException($"Event handler {d.Method.DeclaringType}.{d.Method.Name} is on a null target object and must be unsubscribed from in OnDisable or OnDestroy.");
                        Debug.LogException(exception, obj);
                        evt = (T)Delegate.Remove(evt, d);
                    }
                }
            }
        }

        public static T FixEvent<T>(this T evt) where T : Delegate
        {
            FixEventImpl(ref evt);
            return evt;
        }

        public static void SetLayerToChilds(this GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (var i = 0; i < t.childCount; ++i)
            {
                var child = t.GetChild(i);
                child.gameObject.SetLayerToChilds(layer);
            }
        }

        public static IEnumerator RoutineThen(this IEnumerator first, IEnumerator second)
        {
            yield return first;
            yield return second;
        }

        public static IEnumerator RoutineThen(this YieldInstruction first, IEnumerator second)
        {
            yield return first;
            yield return second;
        }

        public static WaitUntil ToRoutine(this Task task) => new(() => task.IsCompleted);

        public static T MaxByOrDefault<T>(this IEnumerable<T> values, Func<T, float> toKey)
        {
            var iter = values.GetEnumerator();
            if (!iter.MoveNext())
                return default;

            var maxValue = iter.Current;
            var max = toKey(maxValue);
            while (iter.MoveNext())
            {
                var currentMax = toKey(iter.Current);
                if (max < currentMax)
                {
                    max = currentMax;
                    maxValue = iter.Current;
                }
            }

            return maxValue;
        }

        public static NativeSlice<T> AsNativeSlice<T>(this Span<T> value) where T : unmanaged
        {
            if (value.Length == 0)
                return default;

            unsafe
            {
                var ptr = UnsafeUtility.AddressOf(ref value[0]);
                var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(ptr, UnsafeUtility.SizeOf<T>(), value.Length);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
#endif
                return slice;
            }
        }

        public static object GetNamedArgument(this CustomAttributeData attr, string memberName)
        {
            return attr.NamedArguments.
                FirstOrDefault(arg => arg.MemberName == memberName).
                TypedValue.Value;
        }

#if UNITY_EDITOR
        public static IEnumerable<UnityEditor.SerializedProperty> GetSerializedProperties(this UnityEditor.SerializedObject serializedObject)
        {
            var property = serializedObject.GetIterator();
            while (true)
            {
                yield return property;

                if (!property.Next(true))
                    break;
            }
        }

        private const BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static FieldInfo GetField(this Type type, UnityEditor.SerializedProperty property)
        {
            var propertyName = property.name;
            while (type != null)
            {
                var field = type.GetField(propertyName, BINDING_FLAGS);
                if (field != null)
                    return field;
                type = type.BaseType;
            }
            return null;
        }
#endif

#nullable enable
        public static void Deconstruct<T0, T1>(this (T0, T1)? value, out T0? a, out T1? b)
        {
            (a, b) = value is { } data ? data : (default, default);
        }

        public static void Deconstruct<T0, T1, T2>(this (T0, T1, T2)? value, out T0? a, out T1? b, out T2? c)
        {
            (a, b, c) = value is { } data ? data : (default, default, default);
        }
#nullable restore

        /// <summary>
        /// Checks recursively if the transform is the parent of the target gameobject
        /// </summary>
        /// <param name="transform">The transform of the potential parent</param>
        /// <param name="target">The gameobject of the potential child</param>
        /// <returns>A boolean stating if the target is a child of the transform</returns>
        public static bool IsParentOf(this Transform transform, GameObject target)
        {
            if (transform.gameObject == target)
                return true;

            var found = false;
            for (var i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).IsParentOf(target))
                {
                    found = true;
                    break;
                }
            }
            return found;
        }

        /// <summary>
        /// Checks recursively if the array of gameobjects contains a parent of the target gameobject
        /// </summary>
        /// <param name="gameObjects">An array of potential parent gameobjects</param>
        /// <param name="target">A potential child gameobject</param>
        /// <returns>A boolean stating if the target is a child of one of the gameobjects in the array</returns>
        public static bool IsParentOf(this GameObject[] gameObjects, GameObject target)
        {
            foreach (var obj in gameObjects)
            {
                if (obj.transform.IsParentOf(target))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
