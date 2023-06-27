// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;

namespace VRBuilder.Core
{
    /// <summary>
    /// General implementation of <see cref="IMetadata"/>.
    /// </summary>
    [DataContract(IsReference = true)]
    public class Metadata : IMetadata
    {
        [DataMember]
        private Dictionary<string, Dictionary<string, object>> values = new Dictionary<string, Dictionary<string, object>>();

        /// <summary>
        /// Sets a set of data in to specified <paramref name="member"/>.
        /// </summary>
        /// <param name="member">Member data.</param>
        /// <param name="attributeName">Key name of provided data.</param>
        /// <param name="data">Data to be saved as metadata for <paramref name="member"/>.</param>
        public void SetMetadata(MemberInfo member, string attributeName, object data)
        {
            if (values.ContainsKey(member.Name) == false)
            {
                values[member.Name] = new Dictionary<string, object>();
            }

            values[member.Name][attributeName] = data;
        }

        /// <summary>
        /// Returns a set of data extracted from specified <paramref name="attribute"/> of given <paramref name="member"/>.
        /// </summary>
        public object GetMetadata(MemberInfo member, MetadataAttribute attribute)
        {
            if (values.ContainsKey(member.Name) && values[member.Name].ContainsKey(attribute.Name))
            {
                return values[member.Name][attribute.Name];
            }

            return null;
        }

        /// <summary>
        /// Returns a set of data extracted from given <paramref name="member"/>.
        /// </summary>
        public Dictionary<string, object> GetMetadata(MemberInfo member)
        {
            if (values.ContainsKey(member.Name))
            {
                return values[member.Name].ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            return new Dictionary<string, object>();
        }

        public void Clear()
        {
            values = new Dictionary<string, Dictionary<string, object>>();
        }
    }
}
