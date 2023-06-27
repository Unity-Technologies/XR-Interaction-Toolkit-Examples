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
    /// Converts Unity color into json and back.
    /// </summary>
    [NewtonsoftConverter]
    internal class UnityColorConverter : JsonConverter
    {
        /// <inheritDoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Color color = (Color) value;
            JObject data = new JObject();

            data.Add("r",color.r);
            data.Add("g",color.g);
            data.Add("b",color.b);
            data.Add("a",color.a);

            data.WriteTo(writer);
        }

        /// <inheritDoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                try
                {
                    JObject data = (JObject) JToken.ReadFrom(reader);

                    float r = data["r"].Value<float>();
                    float g = data["g"].Value<float>();
                    float b = data["b"].Value<float>();
                    float a = 1.0f;
                    if (data.Count == 4)
                    {
                        a = data["a"].Value<float>();
                    }

                    return new Color(r, g, b, a);
                }
                catch (Exception ex)
                {
                    Debug.LogErrorFormat("Exception occured while trying to parse a color.\n{0}", ex.Message);
                    return Color.magenta;
                }
            }
            Debug.LogWarning("Can't read/parse color from JSON.");
            return Color.magenta;
        }


        /// <inheritDoc/>
        public override bool CanConvert(Type objectType)
        {
            return typeof(Color) == objectType;
        }
    }
}
