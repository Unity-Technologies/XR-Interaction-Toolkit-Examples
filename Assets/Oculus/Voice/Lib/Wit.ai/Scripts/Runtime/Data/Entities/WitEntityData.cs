/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace Meta.WitAi.Data.Entities
{
    public abstract class WitEntityDataBase<T>
    {
        public WitResponseNode responseNode;
        public string id;
        public string name;
        public string role;

        public int start;
        public int end;

        public string type;

        public string body;
        public T value;

        public float confidence;

        public bool hasData;

        public WitResponseArray entities;

        [Preserve]
        public WitEntityDataBase<T> FromEntityWitResponseNode(WitResponseNode node)
        {
            responseNode = node;
            WitEntityDataBase<T> result = this;
            JsonConvert.DeserializeIntoObject(ref result, node);
            return result;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }

    public class WitEntityData : WitEntityDataBase<string>
    {
        [Preserve]
        public WitEntityData() {}

        [Preserve]
        public WitEntityData(WitResponseNode node)
        {
            FromEntityWitResponseNode(node);
        }

        public static implicit operator bool(WitEntityData data) => null != data && !string.IsNullOrEmpty(data.value);
        public static implicit operator string(WitEntityData data) => data.value;
        public static bool operator ==(WitEntityData data, object value) => Equals(data?.value, value);
        public static bool operator !=(WitEntityData data, object value) => !Equals(data?.value, value);

        public override bool Equals(object obj)
        {
            if (obj is string s) return s == value;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class WitEntityFloatData : WitEntityDataBase<float>
    {
        [Preserve]
        public WitEntityFloatData() {}

        [Preserve]
        public WitEntityFloatData(WitResponseNode node)
        {
            FromEntityWitResponseNode(node);
        }

        public static implicit operator bool(WitEntityFloatData data) =>
            null != data && data.hasData;

        public bool Approximately(float v, float tolerance = .001f) => Math.Abs(v - value) < tolerance;
        public static bool operator ==(WitEntityFloatData data, float value) => data?.value == value;
        public static bool operator !=(WitEntityFloatData data, float value) => !(data == value);
        public static bool operator ==(WitEntityFloatData data, int value) => data?.value == value;
        public static bool operator !=(WitEntityFloatData data, int value) => !(data == value);
        public static bool operator ==(float value, WitEntityFloatData data) => data?.value == value;
        public static bool operator !=(float value, WitEntityFloatData data) => !(data == value);
        public static bool operator ==(int value, WitEntityFloatData data) => data?.value == value;
        public static bool operator !=(int value, WitEntityFloatData data) => !(data == value);

        public override bool Equals(object obj)
        {
            if (obj is float f) return f == value;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class WitEntityIntData : WitEntityDataBase<int>
    {
        [Preserve]
        public WitEntityIntData() {}

        [Preserve]
        public WitEntityIntData(WitResponseNode node)
        {
            FromEntityWitResponseNode(node);
        }

        public static implicit operator bool(WitEntityIntData data) =>
            null != data && data.hasData;
        public static bool operator ==(WitEntityIntData data, int value) => data?.value == value;
        public static bool operator !=(WitEntityIntData data, int value) => !(data == value);
        public static bool operator ==(int value, WitEntityIntData data) => data?.value == value;
        public static bool operator !=(int value, WitEntityIntData data) => !(data == value);

        public override bool Equals(object obj)
        {
            if (obj is int i) return i == value;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
