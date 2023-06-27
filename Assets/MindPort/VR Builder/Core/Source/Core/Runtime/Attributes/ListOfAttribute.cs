// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace VRBuilder.Core.Attributes
{
    /// <summary>
    /// Declares that children of this list have metadata attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ListOfAttribute : MetadataAttribute
    {
        /// <summary>
        /// Reference to the child's attributes and metadata.
        /// </summary>
        [DataContract(IsReference = true)]
        public class Metadata
        {
            /// <summary>
            /// Reference to the child's attributes.
            /// </summary>
            [DataMember]
            public List<MetadataAttribute> ChildAttributes { get; set; }

            /// <summary>
            /// Reference to the child metadata.
            /// </summary>
            [DataMember]
            public List<Dictionary<string, object>> ChildMetadata { get; set; }
        };

        private readonly List<MetadataAttribute> childAttributes;

        /// <inheritdoc />
        public ListOfAttribute(params Type[] childAttributes)
        {
            Type[] uniqueTypes = childAttributes.Distinct().ToArray();

            if (uniqueTypes.Length != childAttributes.Length)
            {
                Debug.LogError("Child attributes of ListOf attribute have to be unique. Duplicates are omitted.");
            }

            this.childAttributes = new List<MetadataAttribute>(uniqueTypes.Where(attribute => (typeof(MetadataAttribute).IsAssignableFrom(attribute)))
                .Where(attribute => (typeof(ListOfAttribute).IsAssignableFrom(attribute) == false))
                .Where(attribute => attribute.GetConstructor(new Type[0]) != null)
                .Select(Activator.CreateInstance)
                .Cast<MetadataAttribute>());
        }

        /// <inheritdoc />
        public override object GetDefaultMetadata(MemberInfo owner)
        {
            return new Metadata
            {
                ChildAttributes = new List<MetadataAttribute>(childAttributes),
                ChildMetadata = new List<Dictionary<string, object>>(),
            };
        }

        /// <summary>
        /// <inheritdoc />
        /// ListOf attribute checks that metadata of all children is valid, too.
        /// </summary>
        public override bool IsMetadataValid(object metadata)
        {
            Metadata listOfMetadata = (Metadata)metadata;

            if (AreSetsTheSame(childAttributes, listOfMetadata.ChildAttributes, attribute => attribute.Name) == false)
            {
                return false;
            }

            foreach (Dictionary<string, object> entryMetadata in listOfMetadata.ChildMetadata)
            {
                foreach (MetadataAttribute childAttribute in listOfMetadata.ChildAttributes)
                {
                    if (childAttributes.Any(attribute => attribute.Name == childAttribute.Name) == false)
                    {
                        return false;
                    }

                    if (entryMetadata.ContainsKey(childAttribute.Name) == false)
                    {
                        return false;
                    }

                    if (childAttribute.IsMetadataValid(entryMetadata[childAttribute.Name]) == false)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool AreSetsTheSame<T>(IEnumerable<T> first, IEnumerable<T> second, Func<T, IComparable> toComparable)
        {
            if (first == null)
            {
                return second == null;
            }

            if (second == null)
            {
                return false;
            }

            return first.OrderBy(toComparable).SequenceEqual(second.OrderBy(toComparable));
        }
    }
}
