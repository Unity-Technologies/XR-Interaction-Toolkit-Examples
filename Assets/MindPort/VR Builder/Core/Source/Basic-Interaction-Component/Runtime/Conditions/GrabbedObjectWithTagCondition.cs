using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine.Scripting;
using VRBuilder.BasicInteraction.Properties;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.RestrictiveEnvironment;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Settings;

namespace VRBuilder.BasicInteraction.Conditions
{
    /// <summary>
    /// Condition which is completed when a <see cref="IGrabbableProperty"/> with the given tag is grabbed.
    /// </summary>
    [DataContract(IsReference = true)]
    public class GrabbedObjectWithTagCondition : Condition<GrabbedObjectWithTagCondition.EntityData>
    {
        [DisplayName("Grab Object with Tag")]
        public class EntityData : IConditionData
        {
            [DataMember]
            [DisplayName("Tag")]
            public SceneObjectTag<IGrabbableProperty> Tag { get; set; }

            public bool IsCompleted { get; set; }

            [IgnoreDataMember]
            [HideInProcessInspector]
            public string Name
            {
                get
                {
                    string tag = SceneObjectTags.Instance.GetLabel(Tag.Guid);
                    tag = string.IsNullOrEmpty(tag) ? "<none>" : tag;

                    return $"Grab a {tag} object";
                }
            }

            [DataMember]
            [DisplayName("Keep objects grabbable after step")]
            public bool KeepUnlocked = true;

            public Metadata Metadata { get; set; }
        }

        private class EntityAutocompleter : Autocompleter<EntityData>
        {
            public EntityAutocompleter(EntityData data) : base(data)
            {
            }

            public override void Complete()
            {
                IGrabbableProperty grabbableProperty = RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByTag(Data.Tag.Guid)
                    .Where(sceneObject => sceneObject.Properties.Any(property => property is IGrabbableProperty))
                    .Select(sceneObject => sceneObject.Properties.First(property => property is IGrabbableProperty))
                    .Cast<IGrabbableProperty>()
                    .FirstOrDefault();

                if(grabbableProperty != null)
                {
                    grabbableProperty.FastForwardGrab();
                }
            }
        }

        private class ActiveProcess : BaseActiveProcessOverCompletable<EntityData>
        {
            IEnumerable<IGrabbableProperty> grabbableProperties;

            public override void Start()
            {
                base.Start();

                grabbableProperties = RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByTag(Data.Tag.Guid)
                    .Where(sceneObject => sceneObject.Properties.Any(property => property is IGrabbableProperty))
                    .Select(sceneObject => sceneObject.Properties.First(property => property is IGrabbableProperty))
                    .Cast<IGrabbableProperty>();
            }

            protected override bool CheckIfCompleted()
            {
                return grabbableProperties.Any(property => property.IsGrabbed);
            }

            public ActiveProcess(EntityData data) : base(data)
            {
            }
        }

        [JsonConstructor, Preserve]
        public GrabbedObjectWithTagCondition() : this(Guid.Empty)
        {
        }

        public GrabbedObjectWithTagCondition(Guid guid)
        {
            Data.Tag = new SceneObjectTag<IGrabbableProperty>(guid);
        }

        public override IEnumerable<LockablePropertyData> GetLockableProperties()
        {
            IEnumerable<LockablePropertyData> references = base.GetLockableProperties();
            foreach (LockablePropertyData propertyData in references)
            {
                propertyData.EndStepLocked = !Data.KeepUnlocked;
            }

            return references;
        }

        public override IStageProcess GetActiveProcess()
        {
            return new ActiveProcess(Data);
        }

        protected override IAutocompleter GetAutocompleter()
        {
            return new EntityAutocompleter(Data);
        }
    }
}