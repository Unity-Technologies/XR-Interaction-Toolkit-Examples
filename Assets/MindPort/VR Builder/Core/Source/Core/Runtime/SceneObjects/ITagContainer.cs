using System;
using System.Collections.Generic;

namespace VRBuilder.Core.SceneObjects
{
    /// <summary>
    /// Event args for taggable objects events.
    /// </summary>
    public class TaggableObjectEventArgs : EventArgs
    {
        public readonly string Tag;            

        public TaggableObjectEventArgs(string tag)
        {
            Tag = tag;            
        }
    }

    /// <summary>
    /// A container for a list of guids that are associated to an object as tags.
    /// </summary>
    public interface ITagContainer
    {
        /// <summary>
        /// Raised when a tag is added.
        /// </summary>
        event EventHandler<TaggableObjectEventArgs> TagAdded;

        /// <summary>
        /// Raised when a tag is removed.
        /// </summary>
        event EventHandler<TaggableObjectEventArgs> TagRemoved;

        /// <summary>
        /// All tags on the object.
        /// </summary>
        IEnumerable<Guid> Tags { get; }

        /// <summary>
        /// True if the object has the specified tag.
        /// </summary>
        bool HasTag(Guid tag);

        /// <summary>
        /// Add the specified tag.
        /// </summary>        
        void AddTag(Guid tag);

        /// <summary>
        /// Remove the specified tag.
        /// </summary>
        bool RemoveTag(Guid tag);
    }
}
