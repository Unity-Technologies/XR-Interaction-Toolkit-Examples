/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace Meta.WitAi.Json
{
    /// <summary>
    /// Class for decoding
    /// </summary>
    public static class JsonConvert
    {
        // Default converters
        public static JsonConverter[] DefaultConverters => _defaultConverters;
        private static JsonConverter[] _defaultConverters = new JsonConverter[] { new ColorConverter(), new DateTimeConverter(), new HashSetConverter<string>() };
        // Binding flags to be used for encoding/decoding
        private const BindingFlags BIND_FLAGS = BindingFlags.Public | BindingFlags.Instance;

        // Ensure object exists
        private static object EnsureExists(Type objType, object obj)
        {
            if (obj == null && objType != null)
            {
                if (objType == typeof(string))
                {
                    return string.Empty;
                }
                else if (objType.IsArray)
                {
                    return Activator.CreateInstance(objType, new object[] {0});
                }
                else
                {
                    return Activator.CreateInstance(objType);
                }
            }
            return obj;
        }

        #region Deserialize
        /// <summary>
        /// Safely parse a string into a json node
        /// </summary>
        /// <param name="jsonString">Json parseable string</param>
        /// <returns>Returns json node for easy decoding</returns>
        public static WitResponseNode DeserializeToken(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                VLog.W($"Parse Failed\nNo content provided");
                return null;
            }

            try
            {
                return WitResponseNode.Parse(jsonString);
            }
            catch (Exception e)
            {
                VLog.W($"Parse Failed\n{e}\n\n{jsonString}");
                return null;
            }
        }

        /// <summary>
        /// Generate a default instance, deserialize and return async
        /// </summary>
        public static void DeserializeObjectAsync<IN_TYPE>(string jsonString, Action<IN_TYPE, bool> onComplete, JsonConverter[] customConverters = null, bool suppressWarnings = false)
        {
            IN_TYPE instance = (IN_TYPE)EnsureExists(typeof(IN_TYPE), null);
            ThreadUtility.PerformInBackground(
                () => DeserializeIntoObject<IN_TYPE>(ref instance, jsonString, customConverters, suppressWarnings),
                (success, error) => onComplete?.Invoke(instance, success));
        }
        /// <summary>
        /// Generate a default instance, deserialize and return async
        /// </summary>
        public static void DeserializeObjectAsync<IN_TYPE>(WitResponseNode jsonToken, Action<IN_TYPE, bool> onComplete, JsonConverter[] customConverters = null, bool suppressWarnings = false)
        {
            IN_TYPE instance = (IN_TYPE)EnsureExists(typeof(IN_TYPE), null);
            ThreadUtility.PerformInBackground(
                () => DeserializeIntoObject<IN_TYPE>(ref instance, jsonToken, customConverters, suppressWarnings),
                (success, error) => onComplete?.Invoke(instance, success));
        }
        /// <summary>
        /// Generate a default instance, deserialize and return
        /// </summary>
        public static IN_TYPE DeserializeObject<IN_TYPE>(string jsonString, JsonConverter[] customConverters = null, bool suppressWarnings = false)
        {
            IN_TYPE instance = (IN_TYPE)EnsureExists(typeof(IN_TYPE), null);
            DeserializeIntoObject<IN_TYPE>(ref instance, jsonString, customConverters, suppressWarnings);
            return instance;
        }
        /// <summary>
        /// Generate a default instance, deserialize and return
        /// </summary>
        public static IN_TYPE DeserializeObject<IN_TYPE>(WitResponseNode jsonToken, JsonConverter[] customConverters = null, bool suppressWarnings = false)
        {
            IN_TYPE instance = (IN_TYPE)EnsureExists(typeof(IN_TYPE), null);
            DeserializeIntoObject<IN_TYPE>(ref instance, jsonToken, customConverters, suppressWarnings);
            return instance;
        }

        /// <summary>
        /// Deserialize json string into an existing instance
        /// </summary>
        public static bool DeserializeIntoObject<IN_TYPE>(ref IN_TYPE instance, string jsonString, JsonConverter[] customConverters = null, bool suppressWarnings = false)
        {
            // Parse json
            WitResponseNode jsonToken = DeserializeToken(jsonString);
            return DeserializeIntoObject<IN_TYPE>(ref instance, jsonToken, customConverters, suppressWarnings);
        }
        /// <summary>
        /// Deserialize json string into an existing instance
        /// </summary>
        public static bool DeserializeIntoObject<IN_TYPE>(ref IN_TYPE instance, WitResponseNode jsonToken, JsonConverter[] customConverters = null, bool suppressWarnings = false)
        {
            // Could not parse
            if (jsonToken == null)
            {
                return false;
            }
            // Use default if no customs are added
            if (customConverters == null)
            {
                customConverters = DefaultConverters;
            }

            // Auto cast
            Type iType = typeof(IN_TYPE);
            if (iType == typeof(WitResponseNode))
            {
                object result = jsonToken;
                instance = (IN_TYPE)result;
                return true;
            }
            if (iType == typeof(WitResponseClass))
            {
                object result = jsonToken.AsObject;
                instance = (IN_TYPE)result;
                return true;
            }
            if (iType == typeof(WitResponseArray))
            {
                object result = jsonToken.AsArray;
                instance = (IN_TYPE)result;
                return true;
            }

            try
            {
                StringBuilder log = new StringBuilder();
                instance = (IN_TYPE)DeserializeToken(iType, instance, jsonToken, log, customConverters);
                if (log.Length > 0 && !suppressWarnings)
                {
                    VLog.D($"Deserialize Warnings\n{log}");
                }
                return true;
            }
            catch (Exception e)
            {
                VLog.E($"Deserialize Failed\nTo: {typeof(IN_TYPE)}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// Deserialize json node into an instance of a specified type
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private static object DeserializeToken(Type toType, object oldValue, WitResponseNode jsonToken, StringBuilder log, JsonConverter[] customConverters)
        {
            // Iterate custom converters
            if (customConverters != null)
            {
                foreach (var converter in customConverters)
                {
                    if (converter.CanRead && converter.CanConvert(toType))
                    {
                        return converter.ReadJson(jsonToken, toType, oldValue);
                    }
                }
            }
            // Return default
            if (toType == typeof(string))
            {
                return jsonToken.Value;
            }
            // Enum parse
            if (toType.IsEnum)
            {
                string enumStr = jsonToken.Value;
                foreach (var enumVal in Enum.GetValues(toType))
                {
                    foreach (JsonPropertyAttribute renameAttribute in toType.GetMember(enumVal.ToString())[0].GetCustomAttributes(typeof(JsonPropertyAttribute), false))
                    {
                        if (!string.IsNullOrEmpty(renameAttribute.PropertyName) && string.Equals(jsonToken.Value, renameAttribute.PropertyName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            enumStr = enumVal.ToString();
                            break;
                        }
                    }
                }
                // Call try parse
                return DeserializeEnum(toType, EnsureExists(toType, oldValue), enumStr, log);
            }
            // Deserialize dictionary
            if (toType.GetInterfaces().Contains(typeof(IDictionary)))
            {
                return DeserializeDictionary(toType, EnsureExists(toType, oldValue), jsonToken.AsObject, log, customConverters);
            }
            // Deserialize List
            if (toType.GetInterfaces().Contains(typeof(IEnumerable)))
            {
                // Element type
                Type elementType = toType.GetElementType();
                if (elementType == null)
                {
                    // Try arguments
                    Type[] genericArguments = toType.GetGenericArguments();
                    if (genericArguments != null && genericArguments.Length > 0)
                    {
                        elementType = genericArguments[0];
                    }
                }

                if (elementType != null)
                {
                    // Make array
                    object newArray = newArray = typeof(JsonConvert)
                        .GetMethod("DeserializeArray", BindingFlags.Public | BindingFlags.Static)
                        .MakeGenericMethod(new Type[] { elementType })
                        .Invoke(null, new object[] { oldValue, jsonToken, log, customConverters });

                    // Return array
                    if (toType.IsArray)
                    {
                        return newArray;
                    }
                    // Convert to list
                    if (toType.GetInterfaces().Contains(typeof(IList)))
                    {
                        return Activator.CreateInstance(toType, new object[] { newArray });
                    }
                }
            }
            // Deserialize class
            if (toType.IsClass)
            {
                return DeserializeClass(toType, oldValue, jsonToken.AsObject, log, customConverters);
            }
            // Deserialize struct
            if (toType.IsValueType && !toType.IsPrimitive)
            {
                object oldStruct = Activator.CreateInstance(toType);
                object newStruct = DeserializeClass(toType, oldStruct, jsonToken.AsObject, log, customConverters);
                return newStruct;
            }

            try
            {
                // Convert to basic values
                return Convert.ChangeType(jsonToken.Value, toType);
            }
            catch (Exception e)
            {
                // Could not cast
                log.AppendLine($"\nJson Deserializer failed to cast '{jsonToken.Value}' to type '{toType}'\n{e}");
                return oldValue;
            }
        }

        // Deserialize enum
        private static MethodInfo _enumParseMethod;
        private static object DeserializeEnum(Type toType, object oldValue, string enumString, StringBuilder log)
        {
            // Find enum parse method
            if (_enumParseMethod == null)
            {
                _enumParseMethod = typeof(Enum).GetMethods().ToList().Find(method =>
                    method.IsGenericMethod && method.GetParameters().Length == 3 &&
                    string.Equals(method.Name, "TryParse"));
            }

            // Attempt to parse (Enum.TryParse<TEnum>(enumString, false, out oldValue))
            var parseMethod = _enumParseMethod.MakeGenericMethod(new[] {toType});
            object[] parseParams = new object[] {enumString, false, Activator.CreateInstance(toType)};

            // Invoke
            if ((bool)parseMethod.Invoke(null, parseParams))
            {
                // Return the parsed enum
                return parseParams[2];
            }

            // Failed
            log.AppendLine($"\nJson Deserializer Failed to cast '{enumString}' to enum type '{toType}'");
            return oldValue;
        }

        /// <summary>
        /// Deserialize a specific array
        /// </summary>
        /// <param name="node"></param>
        /// <param name="oldValue"></param>
        /// <param name="log"></param>
        /// <typeparam name="NODE_TYPE"></typeparam>
        /// <returns></returns>
        [Preserve]
        public static ITEM_TYPE[] DeserializeArray<ITEM_TYPE>(object oldArray, WitResponseNode jsonToken, StringBuilder log, JsonConverter[] customConverters)
        {
            // Failed
            if (jsonToken == null)
            {
                return (ITEM_TYPE[])oldArray;
            }

            // Generate array
            WitResponseArray jsonArray = jsonToken.AsArray;
            ITEM_TYPE[] newArray = new ITEM_TYPE[jsonArray.Count];

            // Deserialize array elements
            Type elementType = typeof(ITEM_TYPE);
            for (int i = 0; i < jsonArray.Count; i++)
            {
                object oldItem = EnsureExists(elementType, null);
                ITEM_TYPE newItem = (ITEM_TYPE) DeserializeToken(elementType, oldItem, jsonArray[i], log, customConverters);
                newArray[i] = newItem;
            }

            // Return array
            return newArray;
        }

        /// <summary>
        /// Deserialize json class into object
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private static object DeserializeClass(Type toType, object oldObject, WitResponseClass jsonClass, StringBuilder log, JsonConverter[] customConverters)
        {
            // Failed
            if (jsonClass == null)
            {
                return oldObject;
            }

            // Use old value
            object newObject = oldObject;
            // Generate new if needed
            if (newObject == null)
            {
                newObject = Activator.CreateInstance(toType);
            }

            // Add renames
            Dictionary<string, FieldInfo> fieldRenames = new Dictionary<string, FieldInfo>();
            foreach (var field in toType.GetFields().Where(field => field.IsDefined(typeof(JsonPropertyAttribute), false)))
            {
                foreach (var renameAttribute in field.GetCustomAttributes<JsonPropertyAttribute>())
                {
                    if (!string.IsNullOrEmpty(renameAttribute.PropertyName))
                    {
                        fieldRenames[renameAttribute.PropertyName] = field;
                    }
                }
            }
            Dictionary<string, PropertyInfo> propRenames = new Dictionary<string, PropertyInfo>();
            foreach (var prop in toType.GetProperties().Where(prop => prop.IsDefined(typeof(JsonPropertyAttribute), false)))
            {
                foreach (var renameAttribute in prop.GetCustomAttributes<JsonPropertyAttribute>())
                {
                    if (!string.IsNullOrEmpty(renameAttribute.PropertyName))
                    {
                        propRenames[renameAttribute.PropertyName] = prop;
                    }
                }
            }

            // Iterate each child node
            foreach (var childTokenName in jsonClass.ChildNodeNames)
            {
                // Check field
                FieldInfo field = toType.GetField(childTokenName, BIND_FLAGS);
                if (fieldRenames.ContainsKey(childTokenName))
                {
                    field = fieldRenames[childTokenName];
                }
                if (field != null)
                {
                    // Get old value
                    object oldValue = field.GetValue(newObject);

                    // Deserialize new value
                    object newValue = DeserializeToken(field.FieldType, oldValue, jsonClass[childTokenName], log, customConverters);

                    // Apply new value
                    field.SetValue(newObject, newValue);
                    continue;
                }

                // Check property
                PropertyInfo property = toType.GetProperty(childTokenName, BIND_FLAGS);
                if (propRenames.ContainsKey(childTokenName))
                {
                    property = propRenames[childTokenName];
                }
                if (property != null && property.GetSetMethod() != null)
                {
                    // Get old value
                    object oldValue = null;
                    if (property.GetGetMethod() != null)
                    {
                        oldValue = property.GetValue(newObject);
                    }
                    oldValue = EnsureExists(property.PropertyType, oldValue);

                    // Deserialize new value
                    object newValue = DeserializeToken(property.PropertyType, oldValue, jsonClass[childTokenName], log, customConverters);

                    // Apply new value
                    property.SetValue(newObject, newValue);
                    continue;
                }

                // Not found
                log.AppendLine($"\t{toType.FullName} does not have a public '{childTokenName}' field or property.");
            }

            // Use deserializer if applicable
            if (toType.GetInterfaces().Contains(typeof(IJsonDeserializer)))
            {
                IJsonDeserializer deserializer = newObject as IJsonDeserializer;
                if (!deserializer.DeserializeObject(jsonClass))
                {
                    log.AppendLine($"\tIJsonDeserializer '{toType}' failed");
                }
            }

            // Success
            return newObject;
        }

        /// <summary>
        /// Deserialize a specific array
        /// </summary>
        /// <param name="node"></param>
        /// <param name="oldValue"></param>
        /// <param name="log"></param>
        /// <typeparam name="NODE_TYPE"></typeparam>
        /// <returns></returns>
        private static object DeserializeDictionary(Type toType, object oldObject, WitResponseClass jsonClass, StringBuilder log, JsonConverter[] customConverters)
        {
            // Ensure types are correct
            Type[] dictGenericTypes = toType.GetGenericArguments();
            if (dictGenericTypes == null || dictGenericTypes.Length != 2)
            {
                return oldObject;
            }

            // Generate dictionary
            IDictionary newDictionary = oldObject as IDictionary;

            // Get types
            Type keyType = dictGenericTypes[0];
            Type valType = dictGenericTypes[1];

            // Iterate children
            foreach (var childName in jsonClass.ChildNodeNames)
            {
                // Cast key if possible
                object childKey = Convert.ChangeType(childName, keyType);

                // Cast value if possible
                object newChildValue = DeserializeToken(valType, null, jsonClass[childName], log, customConverters);

                // Apply
                newDictionary[childKey] = newChildValue;
            }

            // Return dictionary
            return newDictionary;
        }
        #endregion

        #region Serialize
        // Serialize object into json
        public static string SerializeObject<FROM_TYPE>(FROM_TYPE inObject, JsonConverter[] customConverters = null, bool suppressWarnings = false)
        {
            // Decode token
            WitResponseNode jsonToken = SerializeToken<FROM_TYPE>(inObject, customConverters, suppressWarnings);
            if (jsonToken != null)
            {
                try
                {
                    return jsonToken.ToString();
                }
                catch (Exception e)
                {
                    VLog.E($"Serialize Object Failed\n{e}");
                }
            }

            // Default value
            return "{}";
        }
        /// <summary>
        /// Serialize object into WitResponseNode
        /// </summary>
        /// <param name="inObject">Serialize object</param>
        /// <returns></returns>
        public static WitResponseNode SerializeToken<FROM_TYPE>(FROM_TYPE inObject, JsonConverter[] customConverters = null, bool suppressWarnings = false)
        {
            // Use default if no customs are added
            if (customConverters == null)
            {
                customConverters = DefaultConverters;
            }
            try
            {
                StringBuilder log = new StringBuilder();
                WitResponseNode jsonToken = SerializeToken(typeof(FROM_TYPE), inObject, log, customConverters);
                if (log.Length > 0 && !suppressWarnings)
                {
                    VLog.W($"Serialize Token Warnings\n{log}");
                }
                return jsonToken;
            }
            catch (Exception e)
            {
                VLog.E($"Serialize Token Failed\n{e}");
            }
            return null;
        }
        // Convert data to node
        private static WitResponseNode SerializeToken(Type inType, object inObject, StringBuilder log, JsonConverter[] customConverters)
        {
            // Use object type instead if possible
            if (inObject != null && inType == typeof(object))
            {
                inType = inObject.GetType();
            }

            // Iterate custom converters
            if (customConverters != null)
            {
                foreach (var converter in customConverters)
                {
                    if (converter.CanWrite && converter.CanConvert(inType))
                    {
                        return converter.WriteJson(inObject);
                    }
                }
            }

            // Null
            if (inObject == null)
            {
                return null;
            }
            // Most likely error in this class
            if (inType == null)
            {
                throw new ArgumentException("In Type cannot be null");
            }

            // Serialize to string
            if (inType == typeof(string))
            {
                return new WitResponseData((string)inObject);
            }
            // Convert to bool
            if (inType == typeof(bool))
            {
                return new WitResponseData((bool)inObject);
            }
            // Convert to int
            else if (inType == typeof(int))
            {
                return new WitResponseData((int)inObject);
            }
            // Convert to float
            else if (inType == typeof(float))
            {
                return new WitResponseData((float)inObject);
            }
            // Convert to double
            else if (inType == typeof(double))
            {
                return new WitResponseData((double)inObject);
            }
            // Convert to enum
            else if (inType.IsEnum)
            {
                return new WitResponseData(inObject.ToString());
            }
            // Serialize a dictionary into a node
            else if (inType.GetInterfaces().Contains(typeof(IDictionary)))
            {
                IDictionary oldDictionary = (IDictionary) inObject;
                WitResponseClass newDictionary = new WitResponseClass();
                Type valType = inType.GetGenericArguments()[1];
                foreach (var key in oldDictionary.Keys)
                {
                    object newObj = oldDictionary[key];
                    if (newObj == null)
                    {
                        if (valType == typeof(string))
                        {
                            newObj = string.Empty;
                        }
                        else
                        {
                            newObj = Activator.CreateInstance(valType);
                        }
                    }
                    newDictionary.Add(key.ToString(), SerializeToken(valType, newObj, log, customConverters));
                }
                return newDictionary;
            }
            // Serialize enumerable into array
            else if (inType.GetInterfaces().Contains(typeof(IEnumerable)))
            {
                // Get enum
                WitResponseArray newArray = new WitResponseArray();
                IEnumerator oldEnumerable = ((IEnumerable) inObject).GetEnumerator();

                // Array[]
                Type elementType = inType.GetElementType();

                // Try generic argument (List<>)
                if (elementType == null)
                {
                    Type[] genericArguments = inType.GetGenericArguments();
                    if (genericArguments != null && genericArguments.Length > 0)
                    {
                        elementType = genericArguments[0];
                    }
                }

                // Serialize each
                while (oldEnumerable.MoveNext())
                {
                    object newObj = EnsureExists(elementType, oldEnumerable.Current);
                    newArray.Add(string.Empty, SerializeToken(elementType, newObj, log, customConverters));
                }
                return newArray;
            }
            // Serialize a class or a struct into a node
            else if (inType.IsClass || (inType.IsValueType && !inType.IsPrimitive))
            {
                WitResponseClass newClass = new WitResponseClass();
                foreach (var field in inType.GetFields(BIND_FLAGS))
                {
                    JsonPropertyAttribute[] fieldAttributes = (JsonPropertyAttribute[])field.GetCustomAttributes(typeof(JsonPropertyAttribute));
                    SerializeProperty(newClass, field.FieldType, field.Name, field.GetValue(inObject),
                        fieldAttributes, log, customConverters);
                }
                foreach (var property in inType.GetProperties(BIND_FLAGS))
                {
                    MethodInfo getter = property.GetGetMethod();
                    if (getter != null && getter.GetParameters().Length == 0)
                    {
                        JsonPropertyAttribute[] propertyAttributes = (JsonPropertyAttribute[])property.GetCustomAttributes(typeof(JsonPropertyAttribute));
                        SerializeProperty(newClass, property.PropertyType, property.Name, property.GetValue(inObject),
                            propertyAttributes, log, customConverters);
                    }
                }
                if (inType.GetInterfaces().Contains(typeof(IJsonSerializer)))
                {
                    IJsonSerializer serializer = inObject as IJsonSerializer;
                    if (!serializer.SerializeObject(newClass))
                    {
                        log.AppendLine($"\tIJsonSerializer '{inType}' failed");
                    }
                }
                return newClass;
            }

            // Warn & incode to string
            log.AppendLine($"\tJson Serializer cannot serialize: {inType}");
            return inObject == null ? null : new WitResponseData(inObject.ToString());
        }
        // Serialize a property using property attributes
        private static void SerializeProperty(WitResponseClass newClass, Type propertyType, string propertyName,
            object propertyValue, JsonPropertyAttribute[] propertyAttributes,
            StringBuilder log, JsonConverter[] customConverters)
        {
            // Get default object
            object newObj = EnsureExists(propertyType, propertyValue);

            // If properties exist, use them to decode
            if (propertyAttributes != null && propertyAttributes.Length > 0)
            {
                foreach (var attribute in propertyAttributes)
                {
                    // Ignore unless property name exists
                    if (!string.IsNullOrEmpty(attribute.PropertyName))
                    {
                        //
                        newClass.Add(attribute.PropertyName, SerializeToken(propertyType, newObj, log, customConverters));
                    }
                }
                return;
            }

            // Use default property name
            newClass.Add(propertyName, SerializeToken(propertyType, newObj, log, customConverters));
        }
        #endregion
    }
}
