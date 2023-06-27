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
    /// Converts Vector3 into json and back.
    /// </summary>
    [NewtonsoftConverter]
    internal class Vector3Converter : JsonConverter
    {
        /// <inheritDoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Vector3 vec = (Vector3) value;
            JObject data = new JObject();

            data.Add("x", vec.x);
            data.Add("y", vec.y);
            data.Add("z", vec.z);

            data.WriteTo(writer);
        }

        /// <inheritDoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                JObject data = (JObject)JToken.ReadFrom(reader);
                return new Vector3(data["x"].Value<float>(), data["y"].Value<float>(), data["z"].Value<float>());
            }

            return Vector3.zero;
        }

        /// <inheritDoc/>
        public override bool CanConvert(Type objectType)
        {
            return typeof(Vector3) == objectType;
        }
    }
}
