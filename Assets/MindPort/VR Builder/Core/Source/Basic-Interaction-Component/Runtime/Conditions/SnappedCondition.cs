using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using UnityEngine.Scripting;
using VRBuilder.BasicInteraction.Properties;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.Configuration.Modes;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;
using VRBuilder.Core.Validation;

namespace VRBuilder.BasicInteraction.Conditions
{
    /// <summary>
    /// Condition which is completed when `Target` is snapped into `ZoneToSnapInto`.
    /// </summary>
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-conditions/snap-object")]
    public class SnappedCondition : Condition<SnappedCondition.EntityData>
    {
        [DisplayName("Snap Object (Ref)")]
        [DataContract(IsReference = true)]
        public class EntityData : IConditionData
        {
#if CREATOR_PRO     
            [CheckForCollider]
#endif
            [DataMember]
            [DisplayName("Object")]
            public ScenePropertyReference<ISnappableProperty> Target { get; set; }

#if CREATOR_PRO        
            [CheckForCollider]
            [ColliderAreTrigger]
#endif
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
                    string target = "[NULL]";
                    string zoneToSnapInto = "[NULL]";

                    if (Target.IsEmpty() == false || ZoneToSnapInto.IsEmpty() == false)
                    {
                        target = Target.IsEmpty() ? "any valid object" : Target.Value.SceneObject.GameObject.name;
                        zoneToSnapInto = ZoneToSnapInto.IsEmpty() ? "any valid snap zone" : ZoneToSnapInto.Value.SceneObject.GameObject.name;
                    }

                    return $"Snap {target} in {zoneToSnapInto}";
                }
            }

            public Metadata Metadata { get; set; }
        }

        private class ActiveProcess : BaseActiveProcessOverCompletable<EntityData>
        {
            public ActiveProcess(EntityData data) : base(data)
            {
            }

            protected override bool CheckIfCompleted()
            {
                if(Data.Target.Value == null && Data.ZoneToSnapInto.Value == null)
                {
                    throw new NullReferenceException("Snapped condition is not configured.");
                }

                if (Data.Target.Value == null)
                {
                    return Data.ZoneToSnapInto.Value.SnappedObject != null;
                }
                else
                {
                    return Data.Target.Value.IsSnapped && (Data.ZoneToSnapInto.Value == null || Data.ZoneToSnapInto.Value == Data.Target.Value.SnappedZone);
                }
            }
        }

        private class EntityAutocompleter : Autocompleter<EntityData>
        {
            public EntityAutocompleter(EntityData data) : base(data)
            {
            }

            public override void Complete()
            {
                if (Data.ZoneToSnapInto.Value == null)
                {
                    return;
                }

                Data.Target.Value.FastForwardSnapInto(Data.ZoneToSnapInto.Value);
            }
        }

        private class EntityConfigurator : Configurator<EntityData>
        {
            public EntityConfigurator(EntityData data) : base(data)
            {
            }

            public override void Configure(IMode mode, Stage stage)
            {
                if(Data.ZoneToSnapInto.Value == null)
                {
                    return;
                }

                Data.ZoneToSnapInto.Value.Configure(mode);
            }
        }

        [JsonConstructor, Preserve]
        public SnappedCondition() : this("", "")
        {
        }

        public SnappedCondition(ISnappableProperty target, ISnapZoneProperty snapZone = null) : this(ProcessReferenceUtils.GetNameFrom(target), ProcessReferenceUtils.GetNameFrom(snapZone))
        {
        }

        public SnappedCondition(string target, string snapZone)
        {
            Data.Target = new ScenePropertyReference<ISnappableProperty>(target);
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