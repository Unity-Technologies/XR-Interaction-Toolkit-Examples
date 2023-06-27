using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRBuilder.Core.Runtime.Utils;

namespace VRBuilder.Core.Settings
{
    /// <summary>
    /// Settings for global list of scene object tags.
    /// </summary>
    public class SceneObjectTags : SettingsObject<SceneObjectTags>
    {
        [Serializable]
        public class Tag
        {
            [SerializeField]
            private string label;

            /// <summary>
            /// Text label for this tag.
            /// </summary>
            public string Label => label;

            [SerializeField]
            private string guidString;

            private Guid guid;

            /// <summary>
            /// Guid representing the tag.
            /// </summary>
            public Guid Guid
            {
                get
                {
                    if (guid == null || guid == Guid.Empty) 
                    {
                        guid = Guid.Parse(guidString);
                    }

                    return guid;
                }
            }

            public Tag(string label)
            {
                this.label = label;
                this.guidString = Guid.NewGuid().ToString();
            }

            public Tag(string label, Guid guid)
            {
                this.label = label;
                this.guidString = guid.ToString();
            }

            public void Rename(string label)
            {
                this.label = label;
            }
        }

        [SerializeField, HideInInspector]
        private List<Tag> tags = new List<Tag>();

        /// <summary>
        /// All tags in the list.
        /// </summary>
        public IEnumerable<Tag> Tags => tags;   

        /// <summary>
        /// Create a new tag and add it to the list.
        /// </summary>
        public Tag CreateTag(string label, Guid guid)
        {
            if (tags.Any(tag => tag.Guid == guid))
            {
                return null;
            }

            Tag tag = new Tag(label, guid);

            if (RenameTag(tag, label))
            {
                tags.Add(tag);
                return tag;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// True if a tag with this label can be created.
        /// </summary>
        public bool CanCreateTag(string label)
        {
            return string.IsNullOrEmpty(label) == false &&
                Tags.Any(tag => tag.Label == label) == false;
        }

        /// <summary>
        /// Remove the specified tag from the list.
        /// </summary>
        public bool RemoveTag(Guid guid)
        {
            return tags.RemoveAll(tag => tag.Guid == guid) > 0;
        }

        /// <summary>
        /// True if the specified tag is present in the list.
        /// </summary>
        public bool TagExists(Guid guid)
        {
            return tags.Any(tag => tag.Guid == guid);
        }

        /// <summary>
        /// Returns the text label associated with the specified guid.
        /// </summary>
        public string GetLabel(Guid guid)
        {
            if (TagExists(guid))
            {
                return tags.First(tag => tag.Guid == guid).Label;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Attempts to rename a tag.
        /// </summary>
        public bool RenameTag(Tag tag, string label)
        {
            if (string.IsNullOrEmpty(label)) 
            {
                return false;
            }

            int counter = 0;
            string baseLabel = label;

            while (tags.Any(tag => tag.Label == label)) 
            {
                counter++;
                label = $"{baseLabel}_{counter}";
            }

            tag.Rename(label);
            return true;
        }
    }
}
