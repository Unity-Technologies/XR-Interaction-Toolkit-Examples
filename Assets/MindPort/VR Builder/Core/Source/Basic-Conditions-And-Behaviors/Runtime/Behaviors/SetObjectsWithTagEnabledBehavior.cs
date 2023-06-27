using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using UnityEngine.Scripting;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Settings;

namespace VRBuilder.Core.Behaviors
{
    /// <summary>
    /// Sets enabled or disabled all objects with a given tag.
    /// </summary>
    [DataContract(IsReference = true)]
    public class SetObjectsWithTagEnabledBehavior : Behavior<SetObjectsWithTagEnabledBehavior.EntityData>
    {
        /// <summary>
        /// Behavior data for <see cref="SetObjectsWithTagEnabledBehavior"/>.
        /// </summary>
        [DisplayName("Enable Objects by Tag")]
        [DataContract(IsReference = true)]
        public class EntityData : IBehaviorData
        {
            /// <summary>
            /// The object to enable.
            /// </summary>
            [DataMember]
            [DisplayName("Tag")]
            public SceneObjectTag<ISceneObject> Tag { get; set; }

            [DataMember]
            [HideInProcessInspector]
            public bool SetEnabled { get; set; }

            /// <inheritdoc />
            public Metadata Metadata { get; set; }

            [DataMember]
            [DisplayName("Revert after step is complete")]
            public bool RevertOnDeactivation { get; set; }

            /// <inheritdoc />
            [IgnoreDataMember]
            public string Name
            {
                get
                {
                    string tag = SceneObjectTags.Instance.GetLabel(Tag.Guid);
                    tag = string.IsNullOrEmpty(tag) ? "<none>" : tag;
                    string setEnabled = SetEnabled ? "Enable" : "Disable";
                    return $"{setEnabled} {tag} objects";
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
                foreach(ISceneObject sceneObject in RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByTag(Data.Tag.Guid))
                {
                    RuntimeConfigurator.Configuration.SceneObjectManager.SetSceneObjectActive(sceneObject, Data.SetEnabled);
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
                    foreach (ISceneObject sceneObject in RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByTag(Data.Tag.Guid))
                    {
                        RuntimeConfigurator.Configuration.SceneObjectManager.SetSceneObjectActive(sceneObject, !Data.SetEnabled);
                    }
                }
            }
        }

        [JsonConstructor, Preserve]
        public SetObjectsWithTagEnabledBehavior() : this(Guid.Empty, false)
        {
        }

        public SetObjectsWithTagEnabledBehavior(bool setEnabled) : this(Guid.Empty, setEnabled, false)
        {
        }

        public SetObjectsWithTagEnabledBehavior(Guid tag, bool setEnabled, bool revertOnDeactivate = false)
        {
            Data.Tag = new SceneObjectTag<ISceneObject>(tag);
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
