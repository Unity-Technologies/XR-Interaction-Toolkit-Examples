using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Properties;
using VRBuilder.Core.Utils;
using VRBuilder.Core.Validation;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace VRBuilder.Core.Conditions
{
    /// <summary>
    /// Condition which is completed when `TargetObject` gets inside `TriggerProperty`'s collider.
    /// </summary>
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-conditions/move-object-in-collider")]
    public class ObjectInColliderCondition : Condition<ObjectInColliderCondition.EntityData>
    {
        /// <summary>
        /// The "object in collider" condition's data.
        /// </summary>
        [DisplayName("Move Object into Collider")]
        [DataContract(IsReference = true)]
        public class EntityData : IObjectInTargetData
        {
            /// <summary>
            /// The object that has to enter the collider.
            /// </summary>
            [DataMember]
            [DisplayName("Object")]
            public SceneObjectReference TargetObject { get; set; }

            /// <summary>
            /// The collider with trigger to enter.
            /// </summary>
            [DataMember]
            [DisplayName("Collider")]
#if CREATOR_PRO
            [CheckForCollider]
            [ColliderAreTrigger]
#endif
            public ScenePropertyReference<ColliderWithTriggerProperty> TriggerProperty { get; set; }

            /// <inheritdoc />
            public bool IsCompleted { get; set; }

            /// <inheritdoc />
            [HideInProcessInspector]
            [IgnoreDataMember]
            public string Name
            {
                get
                {
                    string targetObject = TargetObject.IsEmpty() ? "[NULL]" : TargetObject.Value.GameObject.name;
                    string triggerProperty = TriggerProperty.IsEmpty() ? "[NULL]" : TriggerProperty.Value.SceneObject.GameObject.name;

                    return $"Move {targetObject} in collider {triggerProperty}";
                }
            }

            /// <inheritdoc />
#if CREATOR_PRO
            [OptionalValue]
#endif
            [DataMember]
            [DisplayName("Required seconds inside")]
            public float RequiredTimeInside { get; set; }

            /// <inheritdoc />
            public Metadata Metadata { get; set; }
        }

        [JsonConstructor, Preserve]
        public ObjectInColliderCondition() : this("", "")
        {
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        public ObjectInColliderCondition(ColliderWithTriggerProperty targetPosition, ISceneObject targetObject, float requiredTimeInTarget = 0)
            : this(ProcessReferenceUtils.GetNameFrom(targetPosition), ProcessReferenceUtils.GetNameFrom(targetObject), requiredTimeInTarget)
        {
        }

        public ObjectInColliderCondition(string targetPosition, string targetObject, float requiredTimeInTarget = 0)
        {
            Data.TriggerProperty = new ScenePropertyReference<ColliderWithTriggerProperty>(targetPosition);
            Data.TargetObject = new SceneObjectReference(targetObject);
            Data.RequiredTimeInside = requiredTimeInTarget;
        }

        private class ActiveProcess : ObjectInTargetActiveProcess<EntityData>
        {
            public ActiveProcess(EntityData data) : base(data)
            {
            }

            /// <inheritdoc />
            protected override bool IsInside()
            {
                return Data.TriggerProperty.Value.IsTransformInsideTrigger(Data.TargetObject.Value.GameObject.transform);
            }
        }

        private class EntityAutocompleter : Autocompleter<EntityData>
        {
            public EntityAutocompleter(EntityData data) : base(data)
            {
            }

            /// <inheritdoc />
            public override void Complete()
            {
                Data.TriggerProperty.Value.FastForwardEnter(Data.TargetObject.Value);
            }
        }

        /// <inheritdoc />
        public override IStageProcess GetActiveProcess()
        {
            return new ActiveProcess(Data);
        }

        /// <inheritdoc />
        protected override IAutocompleter GetAutocompleter()
        {
            return new EntityAutocompleter(Data);
        }
    }
}
