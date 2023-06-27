using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine.Scripting;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Settings;

namespace VRBuilder.Core.Behaviors
{
    /// <summary>
    /// Enables/disables all components of a given type on a given game object.
    /// </summary>
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-behaviors/enable-object")]
    public class SetComponentEnabledByTagBehavior : Behavior<SetComponentEnabledByTagBehavior.EntityData>
    {
        /// <summary>
        /// The behavior's data.
        /// </summary>
        [DisplayName("Set Component Enabled (Tag)")]
        [DataContract(IsReference = true)]
        public class EntityData : IBehaviorData
        {
            /// <summary>
            /// Object the target component is on.
            /// </summary>
            [DataMember]
            [HideInProcessInspector]
            public SceneObjectTag<ISceneObject> TargetTag { get; set; }

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
                    string targetTag = SceneObjectTags.Instance.GetLabel(TargetTag.Guid);
                    targetTag = string.IsNullOrEmpty(targetTag) ? "<none>" : targetTag;
                    string setEnabled = SetEnabled ? "Enable" : "Disable";
                    string componentType = string.IsNullOrEmpty(ComponentType) ? "<none>" : ComponentType;
                    return $"{setEnabled} {componentType} for {targetTag} objects";
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
                IEnumerable<ISceneObject> sceneObjects = RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByTag(Data.TargetTag.Guid);

                foreach(ISceneObject sceneObject in sceneObjects)
                {
                    RuntimeConfigurator.Configuration.SceneObjectManager.SetComponentActive(sceneObject, Data.ComponentType, Data.SetEnabled);
                }
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
                    IEnumerable<ISceneObject> sceneObjects = RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByTag(Data.TargetTag.Guid);

                    foreach (ISceneObject sceneObject in sceneObjects)
                    {
                        RuntimeConfigurator.Configuration.SceneObjectManager.SetComponentActive(sceneObject, Data.ComponentType, !Data.SetEnabled);
                    }
                }
            }
        }

        [JsonConstructor, Preserve]
        public SetComponentEnabledByTagBehavior() : this(Guid.Empty, "", false, false)
        {
        }

        public SetComponentEnabledByTagBehavior(bool setEnabled, string name = "Set Component Enabled") : this(Guid.Empty, "", setEnabled, false)
        {
        }

        public SetComponentEnabledByTagBehavior(Guid tagGuid, string componentType, bool setEnabled, bool revertOnDeactivate)
        {
            Data.TargetTag = new SceneObjectTag<ISceneObject>(tagGuid);
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
