// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace VRBuilder.Core.Serialization
{
    /// <summary>
    /// Converts Vector4 into json and back.
    /// </summary>
    [NewtonsoftConverter]
    internal class Vector4Converter : JsonConverter
    {
        /// <inheritDoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Vector4 vec = (Vector4) value;
            JObject data = new JObject();

            data.Add("x", vec.x);
            data.Add("y", vec.y);
            data.Add("z", vec.z);
            data.Add("w", vec.w);

            data.WriteTo(writer);
        }

        /// <inheritDoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                JObject data = (JObject)JToken.ReadFrom(reader);
                return new Vector4(data["x"].Value<float>(), data["y"].Value<float>(), data["z"].Value<float>(), data["w"].Value<float>());
            }

            return Vector4.zero;
        }

        /// <inheritDoc/>
        public override bool CanConvert(Type objectType)
        {
            return typeof(Vector4) == objectType;
        }
    }
}
