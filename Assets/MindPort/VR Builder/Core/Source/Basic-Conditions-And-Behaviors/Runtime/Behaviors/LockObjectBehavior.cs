using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using UnityEngine.Scripting;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;

namespace VRBuilder.Core.Behaviors
{
    /// <summary>
    /// Behavior that locks the target SceneObject while active, and unlocks it again on deactivation (unless it was locked initially).
    /// </summary>
    [Obsolete("Locking scene objects is obsoleted, consider using the 'Unlocked Objects' list in the Step window.")]
    [DataContract(IsReference = true)]
    public class LockObjectBehavior : Behavior<LockObjectBehavior.EntityData>
    {
        /// <summary>
        /// "Lock object" behavior's data.
        /// </summary>
        [DisplayName("Lock Object")]
        [DataContract(IsReference = true)]
        public class EntityData : IBehaviorData
        {
            /// <summary>
            /// The object to lock.
            /// </summary>
            [DataMember]
            [DisplayName("Object")]
            public SceneObjectReference Target { get; set; }

            /// <summary>
            /// If set to true, the behavior will unlock the <see cref="Target"/> at the end of the step.
            /// </summary>
            [DataMember]
            [DisplayName("Lock only during this step")]
            public bool IsOnlyLockedInStep { get; set; }

            /// <summary>
            /// A field to record if the object was locked at the beginning of the step.
            /// </summary>
            public bool WasLockedOnActivate { get; set; }

            ///<inheritdoc />
            public Metadata Metadata { get; set; }

            ///<inheritdoc />
            public string Name { get; set; }
        }

        private class ActivatingProcess : InstantProcess<EntityData>
        {
            public ActivatingProcess(EntityData data) : base(data)
            {
            }

            ///<inheritdoc />
            public override void Start()
            {
                Data.WasLockedOnActivate = Data.Target.Value.IsLocked;
                if (Data.WasLockedOnActivate == false)
                {
                    Data.Target.Value.SetLocked(true);
                }
            }
        }

        private class DeactivatingProcess : InstantProcess<EntityData>
        {
            public DeactivatingProcess(EntityData data) : base(data)
            {
            }

            ///<inheritdoc />
            public override void Start()
            {
                if (Data.WasLockedOnActivate == false && Data.IsOnlyLockedInStep)
                {
                    Data.Target.Value.SetLocked(false);
                }
            }
        }

        [JsonConstructor, Preserve]
        public LockObjectBehavior() : this("")
        {
        }

        public LockObjectBehavior(ISceneObject target) : this(ProcessReferenceUtils.GetNameFrom(target))
        {
        }

        public LockObjectBehavior(ISceneObject target, bool isOnlyLockedInStep) : this(ProcessReferenceUtils.GetNameFrom(target), isOnlyLockedInStep: isOnlyLockedInStep)
        {
        }

        public LockObjectBehavior(string targetName, string name = "Lock Object", bool isOnlyLockedInStep = true)
        {
            Data.Target = new SceneObjectReference(targetName);
            Data.Name = name;
            Data.IsOnlyLockedInStep = isOnlyLockedInStep;
        }

        ///<inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new ActivatingProcess(Data);
        }

        ///<inheritdoc />
        public override IStageProcess GetDeactivatingProcess()
        {
            return new DeactivatingProcess(Data);
        }
    }
}
