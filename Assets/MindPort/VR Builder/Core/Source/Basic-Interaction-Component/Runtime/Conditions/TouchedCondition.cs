using Newtonsoft.Json;
using System.Runtime.Serialization;
using UnityEngine.Scripting;
using VRBuilder.BasicInteraction.Properties;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;
using VRBuilder.Core.Validation;

namespace VRBuilder.BasicInteraction.Conditions
{
    /// <summary>
    /// Condition which is completed when TouchableProperty is touched.
    /// </summary>
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-conditions/touch-object")]
    public class TouchedCondition : Condition<TouchedCondition.EntityData>
    {
        [DisplayName("Touch Object")]
        public class EntityData : IConditionData
        {
#if CREATOR_PRO     
            [CheckForCollider]
#endif
            [DataMember]
            [DisplayName("Object")]
            public ScenePropertyReference<ITouchableProperty> TouchableProperty { get; set; }

            public bool IsCompleted { get; set; }

            [IgnoreDataMember]
            [HideInProcessInspector]
            public string Name
            {
                get
                {
                    string touchableProperty = TouchableProperty.IsEmpty() ? "[NULL]" : TouchableProperty.Value.SceneObject.GameObject.name;

                    return $"Touch {touchableProperty}";
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
                return Data.TouchableProperty.Value.IsBeingTouched;
            }
        }

        private class EntityAutocompleter : Autocompleter<EntityData>
        {
            public EntityAutocompleter(EntityData data) : base(data)
            {
            }

            public override void Complete()
            {
                Data.TouchableProperty.Value.FastForwardTouch();
            }
        }

        [JsonConstructor, Preserve]
        public TouchedCondition() : this("")
        {
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        public TouchedCondition(ITouchableProperty target) : this(ProcessReferenceUtils.GetNameFrom(target))
        {
        }

        public TouchedCondition(string target)
        {
            Data.TouchableProperty = new ScenePropertyReference<ITouchableProperty>(target);
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