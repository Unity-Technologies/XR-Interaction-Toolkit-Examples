using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

namespace VRBuilder.Core.Serialization
{
    /// <summary>
    /// Converter that serializes and deserializes <see cref="Keyframe"/>.
    /// </summary>
    [NewtonsoftConverter]
    public class KeyframeConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return typeof(Keyframe) == objectType;
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                JObject data = (JObject)JToken.ReadFrom(reader);
                Keyframe keyframe = new Keyframe(data["Time"].Value<float>(), data["Value"].Value<float>(), data["InTangent"].Value<float>(), data["OutTangent"].Value<float>(), data["InWeight"].Value<float>(), data["OutWeight"].Value<float>());
                keyframe.weightedMode = (WeightedMode)data["WeightedMode"].Value<int>();
                return keyframe;
            }

            return new Keyframe();
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Keyframe keyframe = (Keyframe)value;            
            JObject data = new JObject();
            
            data.Add("Time", keyframe.time);
            data.Add("Value", keyframe.value);
            data.Add("InTangent", keyframe.inTangent);
            data.Add("OutTangent", keyframe.outTangent);
            data.Add("InWeight", keyframe.inWeight);
            data.Add("OutWeight", keyframe.outWeight);
            data.Add("WeightedMode", (int)keyframe.weightedMode);

            data.WriteTo(writer);
        }
    }
}
