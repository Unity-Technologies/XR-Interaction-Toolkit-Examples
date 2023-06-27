// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRBuilder.Core.UI.Drawers.Metadata;
using VRBuilder.Core.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace VRBuilder.Core.Serialization.NewtonsoftJson
{
    /// <summary>
    /// This serializer uses NewtonsoftJson to serialize data, the outcome is a json file in the UTF-8 encoding.
    /// </summary>
    public class NewtonsoftJsonProcessSerializer : IProcessSerializer
    {
        protected virtual int Version { get; } = 1;

        private static JsonSerializerSettings CreateSettings(IList<JsonConverter> converters)
        {
            return new JsonSerializerSettings
            {
                Converters = converters,
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                Formatting = Formatting.Indented,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                SerializationBinder = new ProcessSerializationBinder(),
                TypeNameHandling = TypeNameHandling.All
            };
        }

        /// <summary>
        /// Returns the json serializer settings used by the process deserialization.
        /// </summary>
        public static JsonSerializerSettings ProcessSerializerSettings
        {
            get { return CreateSettings(GetJsonConverters()); }
        }

        private static JsonSerializerSettings StepSerializerSettings
        {
            get
            {
                List<JsonConverter> converters = new List<JsonConverter> { new IndividualStepTransitionConverter() };

                converters.AddRange(GetJsonConverters());

                return CreateSettings(converters);
            }
        }

        /// <summary>
        /// Creates a list of JsonConverters via reflection. It adds all JsonConverters with the <seealso cref="NewtonsoftConverterAttribute"/>
        /// will be added by default.
        /// </summary>
        /// <returns>A list of all found JsonConverters.</returns>
        private static List<JsonConverter> GetJsonConverters()
        {
            return ReflectionUtils.GetConcreteImplementationsOf<JsonConverter>()
                .WhichHaveAttribute<NewtonsoftConverterAttribute>()
                .OrderBy(type => type.GetAttribute<NewtonsoftConverterAttribute>().Priority)
                .Select(type => ReflectionUtils.CreateInstanceOfType(type) as JsonConverter)
                .ToList();
        }

        /// <inheritdoc/>
        public virtual string Name { get; } = "Newtonsoft Json Importer";

        /// <inheritdoc/>
        public virtual string FileFormat { get; } = "json";

        protected  byte[] Serialize(IEntity entity, JsonSerializerSettings settings)
        {
            JObject jObject = JObject.FromObject(entity, JsonSerializer.Create(settings));
            jObject.Add("$serializerVersion", Version);
            return new UTF8Encoding().GetBytes(jObject.ToString());
        }

        protected T Deserialize<T>(byte[] data, JsonSerializerSettings settings)
        {
            string stringData = new UTF8Encoding().GetString(data);
            return (T)JsonConvert.DeserializeObject(stringData, settings);
        }

        /// <inheritdoc/>
        public virtual byte[] ProcessToByteArray(IProcess process)
        {
            return Serialize(process, ProcessSerializerSettings);
        }

        /// <inheritdoc/>
        public virtual IProcess ProcessFromByteArray(byte[] data)
        {
            JObject dataObject = JsonConvert.DeserializeObject<JObject>(new UTF8Encoding().GetString(data), ProcessSerializerSettings);

            // Check if process was serialized with version 1
            int version = dataObject.GetValue("$serializerVersion").ToObject<int>();
            if (version != 1)
            {
                throw new Exception($"The loaded process is serialized with a serializer version {version}, which in compatible with this serializer.");
            }

            return Deserialize<IProcess>(data, ProcessSerializerSettings);
        }

        /// <inheritdoc/>
        public virtual byte[] StepToByteArray(IStep step)
        {
            return Serialize(step, StepSerializerSettings);
        }

        /// <inheritdoc/>
        public virtual IStep StepFromByteArray(byte[] data)
        {
            return Deserialize<IStep>(data, StepSerializerSettings);
        }

        internal class ProcessSerializationBinder : DefaultSerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName == "VRBuilder.Editor.UI.Drawers.Metadata.ReorderableElementMetadata")
                {
                    return typeof(ReorderableElementMetadata);
                }

                return base.BindToType(assemblyName, typeName);
            }
        }
    }
}
