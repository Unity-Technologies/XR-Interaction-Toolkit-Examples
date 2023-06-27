using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using System.Runtime.Serialization;
using VRBuilder.Core.Utils;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Configuration;
using Object = UnityEngine.Object;
using VRBuilder.Core.ProcessUtils;
using UnityEngine.Scripting;
using VRBuilder.Core.Properties;
using System.Collections.Generic;

namespace VRBuilder.Core.Behaviors
{
    /// <summary>
    /// This behavior causes confetti to rain.
    /// </summary>
    [DataContract(IsReference = true)]
    public class ConfettiBehavior : Behavior<ConfettiBehavior.EntityData>
    {
        [DisplayName("Spawn Confetti")]
        [DataContract(IsReference = true)]
        public class EntityData : IBehaviorData
        {
            /// <summary>
            /// Bool to check whether the confetti machine should spawn above the user or at the position of the position provider.
            /// </summary>
            [DataMember]
            [DisplayName("Spawn Above User")]
            public bool IsAboveUser { get; set; }

            /// <summary>
            /// Name of the process object where to spawn the confetti machine.
            /// Only needed if "Spawn Above User" is not checked.
            /// </summary>
#if CREATOR_PRO     
            [OptionalValue]
#endif
            [DataMember]
            [DisplayName("Position Provider")]
            public SceneObjectReference PositionProvider { get; set; }

            /// <summary>
            /// Path to the desired confetti machine prefab.
            /// </summary>
            [DataMember]
            [DisplayName("Confetti Machine Path")]
            public string ConfettiMachinePrefabPath { get; set; }

            /// <summary>
            /// Radius of the spawning area.
            /// </summary>
            [DataMember]
            [DisplayName("Area Radius")]
            public float AreaRadius { get; set; }

            /// <summary>
            /// Duration of the animation in seconds.
            /// </summary>
            [DataMember]
            [DisplayName("Duration")]
            public float Duration { get; set; }

            /// <summary>
            /// Activation mode of this behavior.
            /// </summary>
            [DataMember]
            public BehaviorExecutionStages ExecutionStages { get; set; }

            public GameObject ConfettiMachine { get; set; }

            public Metadata Metadata { get; set; }
            
            [IgnoreDataMember]
            public string Name
            {
                get
                {
                    string positionProvider = "user";
                    if(IsAboveUser == false)
                    {
                        positionProvider = PositionProvider.IsEmpty() ? "[NULL]" : PositionProvider.Value.GameObject.name;
                    }

                    return $"Spawn confetti on {positionProvider}";
                }
            }
        }

        private const float defaultDuration = 15f;
        private const float defaultRadius = 1f;
        private const float distanceAboveUser = 3f;

        [JsonConstructor, Preserve]
        public ConfettiBehavior() : this(true, "", "", defaultRadius, defaultDuration, BehaviorExecutionStages.Activation)
        {
        }

        public ConfettiBehavior(bool isAboveUser, ISceneObject positionProvider, string confettiMachinePrefabPath, float radius, float duration, BehaviorExecutionStages executionStages)
            : this(isAboveUser, ProcessReferenceUtils.GetNameFrom(positionProvider), confettiMachinePrefabPath, radius, duration, executionStages)
        {
        }

        public ConfettiBehavior(bool isAboveUser, string positionProviderSceneObjectName, string confettiMachinePrefabPath, float radius, float duration, BehaviorExecutionStages executionStages)
        {
            Data.IsAboveUser = isAboveUser;
            Data.PositionProvider = new SceneObjectReference(positionProviderSceneObjectName);
            Data.ConfettiMachinePrefabPath = confettiMachinePrefabPath;
            Data.AreaRadius = radius;
            Data.Duration = duration;
            Data.ExecutionStages = executionStages;

            if (string.IsNullOrEmpty(Data.ConfettiMachinePrefabPath) && RuntimeConfigurator.Exists)
            {
                Data.ConfettiMachinePrefabPath = RuntimeConfigurator.Configuration.SceneConfiguration.DefaultConfettiPrefab;
            }
        }

        private class EmitConfettiProcess : StageProcess<EntityData>
        {
            private readonly BehaviorExecutionStages stages;
            private float timeStarted;
            private GameObject confettiPrefab;
            private List<GameObject> confettiMachines = new List<GameObject>();

            public EmitConfettiProcess(EntityData data, BehaviorExecutionStages stages) : base(data)
            {
                this.stages = stages;
            }

            /// <inheritdoc />
            public override void Start()
            {
                if (ShouldExecuteCurrentStage(Data) == false)
                {
                    return;
                }

                // Load the given prefab and stop the coroutine if not possible.
                confettiPrefab = Resources.Load<GameObject>(Data.ConfettiMachinePrefabPath);

                if (confettiPrefab == null)
                {
                    Debug.LogWarning("No valid prefab path provided.");
                    return;
                }

                if (Data.IsAboveUser)
                {
                    foreach (UserSceneObject user in RuntimeConfigurator.Configuration.Users)
                    {
                        Vector3 spawnPosition;
                        spawnPosition = user.GameObject.transform.position;
                        spawnPosition.y += distanceAboveUser;

                        CreateConfettiMachine(spawnPosition);
                    }
                }
                else
                {
                    CreateConfettiMachine(Data.PositionProvider.Value.GameObject.transform.position);
                }

                if (Data.Duration > 0f)
                {
                    timeStarted = Time.time;
                }
            }

            /// <inheritdoc />
            public override IEnumerator Update()
            {
                if (ShouldExecuteCurrentStage(Data) == false)
                {
                    yield break;
                }

                if (confettiMachines.Count == 0)
                {
                    yield break;
                }

                if (Data.Duration > 0)
                {
                    while (Time.time - timeStarted < Data.Duration)
                    {
                        yield return null;
                    }
                }
            }

            /// <inheritdoc />
            public override void End()
            {
                if (ShouldExecuteCurrentStage(Data))
                {
                    foreach(GameObject confettiMachine in confettiMachines)
                    {
                        Object.Destroy(confettiMachine);
                    }

                    confettiMachines.Clear();
                }
            }

            /// <inheritdoc />
            public override void FastForward() {}

            private bool ShouldExecuteCurrentStage(EntityData data)
            {
                return (data.ExecutionStages & stages) > 0;
            }

            private void CreateConfettiMachine(Vector3 spawnPosition)
            {
                // Spawn the machine and check if it has the interface IParticleMachine
                GameObject confettiMachine = RuntimeConfigurator.Configuration.SceneObjectManager.InstantiatePrefab(confettiPrefab, spawnPosition, Quaternion.Euler(90, 0, 0));

                if (confettiMachine == null)
                {
                    Debug.LogWarning("The provided prefab is missing.");
                    return;
                }

                if (confettiMachine.GetComponent(typeof(IParticleMachine)) == null)
                {
                    Debug.LogWarning("The provided prefab does not have any component of type \"IParticleMachine\".");
                    return;
                }

                confettiMachines.Add(confettiMachine);

                // Change the settings and activate the machine
                IParticleMachine particleMachine = confettiMachine.GetComponent<IParticleMachine>();
                particleMachine.Activate(Data.AreaRadius, Data.Duration);
            }
        }


        /// <inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new EmitConfettiProcess(Data, BehaviorExecutionStages.Activation);
        }
        
        /// <inheritdoc />
        public override IStageProcess GetDeactivatingProcess()
        {
            return new EmitConfettiProcess(Data, BehaviorExecutionStages.Deactivation);
        }
    }
}
