using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine.Scripting;
using VRBuilder.BasicInteraction.Properties;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.RestrictiveEnvironment;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;
using VRBuilder.Core.Validation;

namespace VRBuilder.BasicInteraction.Conditions
{
    /// <summary>
    /// Condition which is completed when `GrabbableProperty` is grabbed.
    /// </summary>
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-conditions/grab-object")]
    public class GrabbedCondition : Condition<GrabbedCondition.EntityData>
    {
        [DisplayName("Grab Object")]
        public class EntityData : IConditionData
        {
#if CREATOR_PRO
            [CheckForCollider]
#endif
            [DataMember]
            [DisplayName("Object")]
            public ScenePropertyReference<IGrabbableProperty> GrabbableProperty { get; set; }
            
            public bool IsCompleted { get; set; }

            [IgnoreDataMember]
            [HideInProcessInspector]
            public string Name
            {
                get
                {
                    string grabbableProperty = GrabbableProperty.IsEmpty() ? "[NULL]" : GrabbableProperty.Value.SceneObject.GameObject.name;

                    return $"Grab {grabbableProperty}";
                }
            }

            [DataMember]
            [DisplayName("Keep object grabbable after step")]
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
                Data.GrabbableProperty.Value.FastForwardGrab();
            }
        }

        private class ActiveProcess : BaseActiveProcessOverCompletable<EntityData>
        {
            protected override bool CheckIfCompleted()
            {
                return Data.GrabbableProperty.Value.IsGrabbed;
            }

            public ActiveProcess(EntityData data) : base(data)
            {
            }
        }

        [JsonConstructor, Preserve]
        public GrabbedCondition() : this("")
        {
        }

        public GrabbedCondition(IGrabbableProperty target) : this(ProcessReferenceUtils.GetNameFrom(target))
        {
        }

        public GrabbedCondition(string target)
        {
            Data.GrabbableProperty = new ScenePropertyReference<IGrabbableProperty>(target);
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