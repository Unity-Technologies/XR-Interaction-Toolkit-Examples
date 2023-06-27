// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace VRBuilder.Editor.TestTools
{
    /// <summary>
    /// Json converter for an IMGUI event.
    /// </summary>
    internal class ImguiEventConverter : JsonConverter
    {
        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Event imguiEvent = (Event)value;

            if (imguiEvent == null)
            {
                return;
            }

            JObject jObject = new JObject
            {
                {"alt", imguiEvent.alt},
                {"button", imguiEvent.button},
                {"capsLock", imguiEvent.capsLock},
                {"character", imguiEvent.character},
                {"clickCount", imguiEvent.clickCount},
                {"command", imguiEvent.command},
                {"commandName", imguiEvent.commandName},
                {"control", imguiEvent.control},
                {"delta.x", imguiEvent.delta.x},
                {"delta.y", imguiEvent.delta.y},
                {"displayIndex", imguiEvent.displayIndex},
                {"keyCode", (int)imguiEvent.keyCode},
                {"modifiers", (int)imguiEvent.modifiers},
                {"mousePosition.x", imguiEvent.mousePosition.x},
                {"mousePosition.y", imguiEvent.mousePosition.y},
                {"numeric", imguiEvent.numeric},
                {"pressure", imguiEvent.pressure},
                {"shift", imguiEvent.shift},
                {"type", (int)imguiEvent.type},
            };
            jObject.WriteTo(writer);
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            JObject jObject = JObject.Load(reader);

            return new Event()
            {
                alt = jObject["alt"].Value<bool>(),
                button = jObject["button"].Value<int>(),
                capsLock = jObject["capsLock"].Value<bool>(),
                character = jObject["character"].Value<char>(),
                clickCount = jObject["clickCount"].Value<int>(),
                command = jObject["command"].Value<bool>(),
                commandName = jObject["commandName"].Value<string>(),
                control = jObject["control"].Value<bool>(),
                delta = new Vector2((float) jObject["delta.x"].Value<double>(), (float) jObject["delta.y"].Value<double>()),
                displayIndex = jObject["displayIndex"].Value<int>(),
                keyCode = (KeyCode)jObject["keyCode"].Value<int>(),
                modifiers = (EventModifiers)jObject["modifiers"].Value<int>(),
                mousePosition = new Vector2((float) jObject["mousePosition.x"].Value<double>(), (float) jObject["mousePosition.y"].Value<double>()),
                numeric = jObject["numeric"].Value<bool>(),
                pressure = (float) jObject["pressure"].Value<double>(),
                shift = jObject["shift"].Value<bool>(),
                type = (EventType)jObject["type"].Value<int>(),
            };
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return typeof(Event).IsAssignableFrom(objectType);
        }
    }
}
