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
    /// Enables/disables all components of a given type on a given game object.
    /// </summary>
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-behaviors/enable-object")]
    public class SetComponentEnabledBehavior : Behavior<SetComponentEnabledBehavior.EntityData>
    {
        /// <summary>
        /// The behavior's data.
        /// </summary>
        [DisplayName("Set Component Enabled")]
        [DataContract(IsReference = true)]
        public class EntityData : IBehaviorData
        {
            /// <summary>
            /// Object the target component is on.
            /// </summary>
            [DataMember]
            [HideInProcessInspector]
            public SceneObjectReference Target { get; set; }

            /// <summary>
            /// Type of components to interact with.
            /// </summary>
            [DataMember]
            [HideInProcessInspector]
            public string ComponentType { get; set; }

            /// <summary>
            /// If true, the component will be enabled, otherwise it will disabled.
            /// </summary>
            [DataMember]
            [HideInProcessInspector]
            public bool SetEnabled { get; set; }

            /// <summary>
            /// If true, the component will revert to its original state when the behavior deactivates.
            /// </summary>
            [DataMember]
            [HideInProcessInspector]
            public bool RevertOnDeactivation { get; set; }

            /// <inheritdoc />
            public Metadata Metadata { get; set; }

            /// <inheritdoc />
            [IgnoreDataMember]
            public string Name
            {
                get
                {
                    string target = Target.IsEmpty() ? "[NULL]" : Target.Value.GameObject.name;
                    string setEnabled = SetEnabled ? "Enable" : "Disable";
                    string componentType = string.IsNullOrEmpty(ComponentType) ? "<none>" : ComponentType;
                    return $"{setEnabled} {componentType} on {target}";
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
                RuntimeConfigurator.Configuration.SceneObjectManager.SetComponentActive(Data.Target.Value, Data.ComponentType, Data.SetEnabled);
            }
        }

        private class DeactivatingProcess : InstantProcess<EntityData>
        {
            public DeactivatingProcess(EntityData data) : base(data)
            {
            }

            /// <inheritdoc />
            public override void Start()
            {
                if (Data.RevertOnDeactivation)
                {
                    RuntimeConfigurator.Configuration.SceneObjectManager.SetComponentActive(Data.Target.Value, Data.ComponentType, !Data.SetEnabled);
                }
            }
        }

        [JsonConstructor, Preserve]
        public SetComponentEnabledBehavior() : this("", "", false, false)
        {
        }

        public SetComponentEnabledBehavior(bool setEnabled) : this("", "", setEnabled, false)
        {
        }

        public SetComponentEnabledBehavior(ISceneObject targetObject, string componentType, bool setEnabled, bool revertOnDeactivate) : this(ProcessReferenceUtils.GetNameFrom(targetObject), componentType, setEnabled, revertOnDeactivate)
        {
        }

        public SetComponentEnabledBehavior(string targetObject, string componentType, bool setEnabled, bool revertOnDeactivate)
        {
            Data.Target = new SceneObjectReference(targetObject);
            Data.ComponentType = componentType;
            Data.SetEnabled = setEnabled;
            Data.RevertOnDeactivation = revertOnDeactivate;
        }

        /// <inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new ActivatingProcess(Data);
        }

        public override IStageProcess GetDeactivatingProcess()
        {
            return new DeactivatingProcess(Data);
        }
    }
}
