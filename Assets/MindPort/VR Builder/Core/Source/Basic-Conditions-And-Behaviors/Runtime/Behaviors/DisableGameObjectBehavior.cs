using Newtonsoft.Json;
using System.Runtime.Serialization;
using UnityEngine.Scripting;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;

namespace VRBuilder.Core.Behaviors
{
    /// <summary>
    /// Disables gameObject of target ISceneObject.
    /// </summary>
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-behaviors/disable-object")]
    public class DisableGameObjectBehavior : Behavior<DisableGameObjectBehavior.EntityData>
    {
        /// <summary>
        /// "Disable game object" behavior's data.
        /// </summary>
        [DisplayName("Disable Object")]
        [DataContract(IsReference = true)]
        public class EntityData : IBehaviorData
        {
            /// <summary>
            /// Object to disable.
            /// </summary>
            [DataMember]
            [DisplayName("Object")]
            public SceneObjectReference Target { get; set; }

            /// <inheritdoc />
            public Metadata Metadata { get; set; }

            /// <inheritdoc />
            [IgnoreDataMember]
            public string Name
            {
                get
                {
                    string target = Target.IsEmpty() ? "[NULL]" : Target.Value.GameObject.name;
                    return $"Disable {target}";
                }
            }
        }

        private class ActivatingProcess : InstantProcess<EntityData>
        {
            public ActivatingProcess(EntityData data) : base(data)
            {
            }

            /// <inheritdoc />
            public override void Start()
            {
                RuntimeConfigurator.Configuration.SceneObjectManager.SetSceneObjectActive(Data.Target.Value, false);
            }
        }

        /// <inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new ActivatingProcess(Data);
        }

        [JsonConstructor, Preserve]
        public DisableGameObjectBehavior() : this("")
        {
        }

        /// <param name="targetObject">scene object to disable.</param>
        public DisableGameObjectBehavior(ISceneObject targetObject) : this(ProcessReferenceUtils.GetNameFrom(targetObject))
        {
        }

        /// <param name="targetObject">Unique name of target scene object.</param>
        public DisableGameObjectBehavior(string targetObject)
        {
            Data.Target = new SceneObjectReference(targetObject);
        }
    }
}
