using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRBuilder.Core.SceneObjects;

namespace VRBuilder.BasicInteraction.Validation
{
    public class IsObjectWithTagValidation : Validator, ITagContainer
    {
        [SerializeField]
        private List<string> tags = new List<string>();
        public IEnumerable<Guid> Tags => tags.Select(tag => Guid.Parse(tag));

        public event EventHandler<TaggableObjectEventArgs> TagAdded;
        public event EventHandler<TaggableObjectEventArgs> TagRemoved;

        public void AddTag(Guid tag)
        {
            if(HasTag(tag) == false)
            {
                tags.Add(tag.ToString());
                TagAdded?.Invoke(this, new TaggableObjectEventArgs(tag.ToString()));
            }
        }

        public bool HasTag(Guid tag)
        {
            return Tags.Contains(tag);
        }

        public bool RemoveTag(Guid tag)
        {
            bool removed = false;

            if (HasTag(tag))
            {
                removed = tags.Remove(tag.ToString());
                TagRemoved?.Invoke(this, new TaggableObjectEventArgs(tag.ToString()));
            }

            return removed;
        }

        public override bool Validate(GameObject obj)
        {
            ProcessSceneObject processSceneObject = obj.GetComponent<ProcessSceneObject>();

            if (processSceneObject == null)
            {
                return false;
            }

            if (Tags.Count() == 0)
            {
                return true;
            }

            return Tags.Any(tag => processSceneObject.HasTag(tag));
        }
    }
}