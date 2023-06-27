// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.Configuration.Modes;
using VRBuilder.Core.EntityOwners;
using VRBuilder.Core.EntityOwners.FoldedEntityCollection;
using VRBuilder.Core.RestrictiveEnvironment;
using VRBuilder.Core.Utils.Logging;
using VRBuilder.Unity;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Properties;

namespace VRBuilder.Core
{
    /// <summary>
    /// An implementation of <see cref="IStep"/> interface.
    /// </summary>
    [DataContract(IsReference = true)]
    public class Step : Entity<Step.EntityData>, IStep
    {
        public class EntityData : EntityCollectionData<IStepChild>, IStepData, ILockableStepData
        {
            ///<inheritdoc />
            [DataMember]
            [DrawingPriority(0)]
            [HideInProcessInspector]
            public string Name { get; set; }

            ///<inheritdoc />
            [DataMember]
            [DrawingPriority(1)]
            public string Description { get; set; }

            ///<inheritdoc />
            [DataMember]
            [HideInProcessInspector]
            public IBehaviorCollection Behaviors { get; set; }

            ///<inheritdoc />
            [DataMember]
            [HideInProcessInspector]
            public ITransitionCollection Transitions { get; set; }

            ///<inheritdoc />
            public override IEnumerable<IStepChild> GetChildren()
            {
                return new List<IStepChild>
                {
                    Behaviors,
                    Transitions
                };
            }

            /// <inheritdoc />
            public void SetName(string name)
            {
                Name = name;
            }

            ///<inheritdoc />
            [IgnoreDataMember]
            public IStepChild Current { get; set; }

            ///<inheritdoc />
            public IMode Mode { get; set; }

            ///<inheritdoc />
            [HideInProcessInspector]
            public IEnumerable<LockablePropertyReference> ToUnlock { get; set; } = new List<LockablePropertyReference>();

            ///<inheritdoc />
            [HideInProcessInspector]
            public IDictionary<Guid, IEnumerable<Type>> TagsToUnlock { get; set; } = new Dictionary<Guid, IEnumerable<Type>>();

            public EntityData()
            {
            }
        }

        public override void Configure(IMode mode)
        {
#if UNITY_EDITOR
            try
            {
#endif
                base.Configure(mode);
#if UNITY_EDITOR
            }
            catch (Exception e)
            {
                if (Parent is Chapter chapter)
                {
                    Debug.LogError($"Configure failed for Chapter: '{chapter.Data?.Name}, Step: {Data?.Name}'");
                    Debug.LogException(e);
                }
            }
#endif
        }

        private class UnlockProcess : StageProcess<EntityData>
        {
            private readonly IEnumerable<LockablePropertyData> toUnlock;

            public UnlockProcess(EntityData data) : base(data)
            {
                toUnlock = Data.ToUnlock.Select(reference => new LockablePropertyData(reference.GetProperty())).ToList();

                foreach (Guid tag in Data.TagsToUnlock.Keys)
                {
                    foreach (ISceneObject sceneObject in RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByTag(tag))
                    {
                        toUnlock = toUnlock.Union(sceneObject.Properties.Where(property => Data.TagsToUnlock[tag].Contains(property.GetType())).Select(property => new LockablePropertyData(property as LockableProperty))).ToList();
                    }
                }
            }

            ///<inheritdoc />
            public override void Start()
            {
                RuntimeConfigurator.Configuration.StepLockHandling.Unlock(Data, toUnlock);
            }

            ///<inheritdoc />
            public override IEnumerator Update()
            {
                yield return null;
            }

            ///<inheritdoc />
            public override void End()
            {
            }

            ///<inheritdoc />
            public override void FastForward()
            {
            }
        }

        private class LockProcess : StageProcess<EntityData>
        {
            private readonly IEnumerable<LockablePropertyData> toUnlock;

            public LockProcess(EntityData data) : base(data)
            {
                toUnlock = Data.ToUnlock.Select(reference => new LockablePropertyData(reference.GetProperty())).ToList();

                foreach (Guid tag in Data.TagsToUnlock.Keys)
                {
                    foreach (ISceneObject sceneObject in RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByTag(tag))
                    {
                        toUnlock = toUnlock.Union(sceneObject.Properties.Where(property => Data.TagsToUnlock[tag].Contains(property.GetType())).Select(property => new LockablePropertyData(property as LockableProperty))).ToList();
                    }
                }
            }

            ///<inheritdoc />
            public override void Start()
            {
            }

            ///<inheritdoc />
            public override IEnumerator Update()
            {
                yield return null;
            }

            ///<inheritdoc />
            public override void End()
            {
                RuntimeConfigurator.Configuration.StepLockHandling.Lock(Data, toUnlock);
            }

            ///<inheritdoc />
            public override void FastForward()
            {
            }
        }

        private class ActiveProcess : StageProcess<EntityData>
        {
            private readonly IEnumerable<LockablePropertyData> toUnlock;

            public ActiveProcess(EntityData data) : base(data)
            {
            }

            ///<inheritdoc />
            public override void Start()
            {
            }

            ///<inheritdoc />
            public override IEnumerator Update()
            {
                while (Data.Transitions.Data.Transitions.Any(transition => transition.IsCompleted) == false)
                {
                    yield return null;
                }
            }

            ///<inheritdoc />
            public override void End()
            {
            }

            ///<inheritdoc />
            public override void FastForward()
            {
            }
        }

        ///<inheritdoc />
        [DataMember]
        public StepMetadata StepMetadata { get; set; }

        ///<inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new CompositeProcess(new FoldedActivatingProcess<IStepChild>(Data), new UnlockProcess(Data));
        }

        ///<inheritdoc />
        public override IStageProcess GetActiveProcess()
        {
            return new CompositeProcess(new FoldedActiveProcess<IStepChild>(Data), new ActiveProcess(Data));
        }

        ///<inheritdoc />
        public override IStageProcess GetDeactivatingProcess()
        {
            return new CompositeProcess(new FoldedDeactivatingProcess<IStepChild>(Data), new LockProcess(Data));
        }

        ///<inheritdoc />
        protected override IConfigurator GetConfigurator()
        {
            return new FoldedLifeCycleConfigurator<IStepChild>(Data);
        }

        ///<inheritdoc />
        public IStep Clone()
        {
            Step clonedStep = new Step(Data.Name);
            clonedStep.StepMetadata = StepMetadata;
            clonedStep.Data.Transitions = Data.Transitions.Clone();
            clonedStep.Data.Behaviors = Data.Behaviors.Clone();
            clonedStep.Data.Name = Data.Name;
            clonedStep.Data.Description = Data.Description;
            clonedStep.Data.ToUnlock = Data.ToUnlock;
            clonedStep.Data.TagsToUnlock = Data.TagsToUnlock;

            return clonedStep;
        }

        ///<inheritdoc />
        IStepData IDataOwner<IStepData>.Data
        {
            get { return Data; }
        }

        protected Step() : this(null)
        {
        }

        public Step(string name)
        {
            StepMetadata = new StepMetadata();
            Data.Transitions = new TransitionCollection();
            Data.Behaviors = new BehaviorCollection();
            Data.Name = name;            

            if (LifeCycleLoggingConfig.Instance.LogSteps)
            {
                LifeCycle.StageChanged += (sender, args) => { Debug.LogFormat("{0}<b>Step</b> <i>'{1}'</i> is <b>{2}</b>.\n", ConsoleUtils.GetTabs(), Data.Name, LifeCycle.Stage); };
            }
        }
    }
}
