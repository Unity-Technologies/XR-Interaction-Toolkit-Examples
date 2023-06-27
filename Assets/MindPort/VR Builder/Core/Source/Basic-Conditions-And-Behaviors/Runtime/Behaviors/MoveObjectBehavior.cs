using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;
using VRBuilder.Core.Validation;
using Newtonsoft.Json;
using UnityEngine.Scripting;
using VRBuilder.Core.Configuration;

namespace VRBuilder.Core.Behaviors
{
    /// <summary>
    /// Behavior that moves target SceneObject to the position and rotation of another TargetObject.
    /// It takes `Duration` seconds, even if the target was in the place already.
    /// If `Duration` is equal or less than 0, transition is instantaneous.
    /// </summary>
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-behaviors/move-object")]
    public class MoveObjectBehavior : Behavior<MoveObjectBehavior.EntityData>
    {
        /// <summary>
        /// The "move object" behavior's data.
        /// </summary>
        [DisplayName("Move Object")]
        [DataContract(IsReference = true)]
        public class EntityData : IBehaviorData
        {
            /// <summary>
            /// Target scene object to be moved.
            /// </summary>
            [DataMember]
            [DisplayName("Object")]
            public SceneObjectReference Target { get; set; }

            /// <summary>
            /// Target's position and rotation is linearly interpolated to match PositionProvider's position and rotation at the end of transition.
            /// </summary>
            [DataMember]
            [DisplayName("Final position provider")]
            public SceneObjectReference PositionProvider { get; set; }
            
            /// <summary>
            /// Duration of the transition. If duration is equal or less than zero, target object movement is instantaneous.
            /// </summary>
#if CREATOR_PRO            
            [OptionalValue]
#endif
            [DataMember]
            [DisplayName("Animation (in seconds)")]
            public float Duration { get; set; }

            /// <inheritdoc />
            public Metadata Metadata { get; set; }

            /// <inheritdoc />
            [IgnoreDataMember]
            public string Name
            {
                get
                {
                    string target = Target.IsEmpty() ? "[NULL]" : Target.Value.GameObject.name;
                    string positionProvider = PositionProvider.IsEmpty() ? "[NULL]" : PositionProvider.Value.GameObject.name;
                    return $"Move {target} to {positionProvider}";
                }
            }
        }

        private class ActivatingProcess : StageProcess<EntityData>
        {
            private float startingTime;

            public ActivatingProcess(EntityData data) : base(data)
            {
            }

            /// <inheritdoc />
            public override void Start()
            {
                startingTime = Time.time;

                RuntimeConfigurator.Configuration.SceneObjectManager.RequestAuthority(Data.Target.Value);

                Rigidbody movingRigidbody = Data.Target.Value.GameObject.GetComponent<Rigidbody>();
                if (movingRigidbody != null)
                {
                    movingRigidbody.velocity = Vector3.zero;
                    movingRigidbody.angularVelocity = Vector3.zero;
                }
            }

            /// <inheritdoc />
            public override IEnumerator Update()
            {
                Transform movingTransform = Data.Target.Value.GameObject.transform;
                Transform targetPositionTransform = Data.PositionProvider.Value.GameObject.transform;

                Vector3 initialPosition = movingTransform.position;
                Quaternion initialRotation = movingTransform.rotation;

                while (Time.time - startingTime < Data.Duration)
                {
                    RuntimeConfigurator.Configuration.SceneObjectManager.RequestAuthority(Data.Target.Value);

                    if (movingTransform == null || movingTransform.Equals(null) || targetPositionTransform == null || targetPositionTransform.Equals(null))
                    {
                        string warningFormat = "The process scene object's game object is null, transition movement is not completed, behavior activation is forcefully finished.";
                        warningFormat += "Target object unique name: {0}, Position provider's unique name: {1}";
                        Debug.LogWarningFormat(warningFormat, Data.Target.UniqueName, Data.PositionProvider.UniqueName);
                        yield break;
                    }

                    float progress = (Time.time - startingTime) / Data.Duration;

                    movingTransform.position = Vector3.Lerp(initialPosition, targetPositionTransform.position, progress);
                    movingTransform.rotation = Quaternion.Slerp(initialRotation, targetPositionTransform.rotation, progress);

                    yield return null;
                }
            }

            /// <inheritdoc />
            public override void End()
            {
                RuntimeConfigurator.Configuration.SceneObjectManager.RequestAuthority(Data.Target.Value);

                Transform movingTransform = Data.Target.Value.GameObject.transform;
                Transform targetPositionTransform = Data.PositionProvider.Value.GameObject.transform;

                movingTransform.position = targetPositionTransform.position;
                movingTransform.rotation = targetPositionTransform.rotation;

                Rigidbody movingRigidbody = Data.Target.Value.GameObject.GetComponent<Rigidbody>();
                if (movingRigidbody != null)
                {
                    movingRigidbody.velocity = Vector3.zero;
                    movingRigidbody.angularVelocity = Vector3.zero;
                }
            }

            public override void FastForward()
            {
            }
        }

        [JsonConstructor, Preserve]
        public MoveObjectBehavior() : this("", "", 0f)
        {
        }

        public MoveObjectBehavior(ISceneObject target, ISceneObject positionProvider, float duration) : this(ProcessReferenceUtils.GetNameFrom(target), ProcessReferenceUtils.GetNameFrom(positionProvider), duration)
        {
        }

        public MoveObjectBehavior(string targetName, string positionProviderName, float duration)
        {
            Data.Target = new SceneObjectReference(targetName);
            Data.PositionProvider = new SceneObjectReference(positionProviderName);
            Data.Duration = duration;
        }

        /// <inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new ActivatingProcess(Data);
        }
    }
}
