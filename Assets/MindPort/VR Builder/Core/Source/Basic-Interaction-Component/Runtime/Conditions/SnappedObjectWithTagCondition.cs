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
using VRBuilder.Core.Configuration.Modes;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Settings;

namespace VRBuilder.BasicInteraction.Conditions
{
    /// <summary>
    /// Condition which is completed when an object with the given tag is snapped into `ZoneToSnapInto`.
    /// </summary>
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-conditions/snap-object")]
    public class SnappedObjectWithTagCondition : Condition<SnappedObjectWithTagCondition.EntityData>
    {
        [DisplayName("Snap Object (Tag)")]
        [DataContract(IsReference = true)]
        public class EntityData : IConditionData
        {
            [DataMember]
            [DisplayName("Tag")]
            public SceneObjectTag<ISnappableProperty> Tag { get; set; }

            [DataMember]
            [DisplayName("Zone to snap into")]
            public ScenePropertyReference<ISnapZoneProperty> ZoneToSnapInto { get; set; }

            public bool IsCompleted { get; set; }

            [IgnoreDataMember]
            [HideInProcessInspector]
            public string Name
            {
                get
                {
                    string tag = SceneObjectTags.Instance.GetLabel(Tag.Guid);
                    tag = string.IsNullOrEmpty(tag) ? "<none>" : tag;
                    string zoneToSnapInto = ZoneToSnapInto.IsEmpty() ? "[NULL]" : ZoneToSnapInto.Value.SceneObject.GameObject.name;

                    return $"Snap a {tag} object in {zoneToSnapInto}";
                }
            }

            public Metadata Metadata { get; set; }
        }

        private class ActiveProcess : BaseActiveProcessOverCompletable<EntityData>
        {
            IEnumerable<ISnappableProperty> snappableProperties;

            public ActiveProcess(EntityData data) : base(data)
            {
            }

            public override void Start()
            {
                base.Start();

                snappableProperties = RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByTag(Data.Tag.Guid)
                    .Where(sceneObject => sceneObject.Properties.Any(property => property is ISnappableProperty))
                    .Select(sceneObject => sceneObject.Properties.First(property => property is ISnappableProperty))
                    .Cast<ISnappableProperty>();
            }

            protected override bool CheckIfCompleted()
            {
                return snappableProperties.Where(property => property.IsSnapped).Any(property => property.SnappedZone == Data.ZoneToSnapInto.Value);
            }
        }

        private class EntityAutocompleter : Autocompleter<EntityData>
        {
            public EntityAutocompleter(EntityData data) : base(data)
            {
            }

            public override void Complete()
            {
                ISnappableProperty snappable = RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByTag(Data.Tag.Guid)
                    .Where(sceneObject => sceneObject.Properties.Any(property => property is ISnappableProperty))
                    .Select(sceneObject => sceneObject.Properties.First(property => property is ISnappableProperty))
                    .Cast<ISnappableProperty>()
                    .Where(snappable => snappable.IsSnapped == false)
                    .FirstOrDefault();

                if (snappable != null && Data.ZoneToSnapInto.Value.IsObjectSnapped == false)
                {
                    snappable.FastForwardSnapInto(Data.ZoneToSnapInto.Value);
                }
            }
        }

        private class EntityConfigurator : Configurator<EntityData>
        {
            public EntityConfigurator(EntityData data) : base(data)
            {
            }

            public override void Configure(IMode mode, Stage stage)
            {
                Data.ZoneToSnapInto.Value.Configure(mode);
            }
        }

        [JsonConstructor, Preserve]
        public SnappedObjectWithTagCondition() : this(Guid.Empty, "")
        {
        }

        public SnappedObjectWithTagCondition(Guid guid, string snapZone)
        {
            Data.Tag = new SceneObjectTag<ISnappableProperty>(guid);
            Data.ZoneToSnapInto = new ScenePropertyReference<ISnapZoneProperty>(snapZone);
        }

        public override IStageProcess GetActiveProcess()
        {
            return new ActiveProcess(Data);
        }

        protected override IConfigurator GetConfigurator()
        {
            return new EntityConfigurator(Data);
        }

        protected override IAutocompleter GetAutocompleter()
        {
            return new EntityAutocompleter(Data);
        }
    }
}