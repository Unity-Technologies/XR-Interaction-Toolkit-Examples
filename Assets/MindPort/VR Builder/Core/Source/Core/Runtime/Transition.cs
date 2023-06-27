// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.Configuration.Modes;
using VRBuilder.Core.EntityOwners;
using VRBuilder.Core.RestrictiveEnvironment;
using VRBuilder.Core.Utils.Logging;
using VRBuilder.Unity;
using UnityEngine;

namespace VRBuilder.Core
{
    /// <summary>
    /// A class for a transition from one step to another.
    /// </summary>
    [DataContract(IsReference = true)]
    public class Transition : CompletableEntity<Transition.EntityData>, ITransition, ILockablePropertiesProvider
    {
        /// <summary>
        /// The transition's data class.
        /// </summary>
        [DisplayName("Transition")]
        public class EntityData : EntityCollectionData<ICondition>, ITransitionData
        {
            ///<inheritdoc />
            [DataMember]
            [DisplayName("Conditions"), Foldable, ReorderableListOf(typeof(FoldableAttribute), typeof(DeletableAttribute), typeof(HelpAttribute)), ExtendableList]
            public IList<ICondition> Conditions { get; set; }

            ///<inheritdoc />
            public override IEnumerable<ICondition> GetChildren()
            {
                return Conditions.ToArray();
            }

            ///<inheritdoc />
            [HideInProcessInspector]
            [DataMember]
            public IStep TargetStep { get; set; }

            ///<inheritdoc />
            public IMode Mode { get; set; }

            ///<inheritdoc />
            public bool IsCompleted { get; set; }
        }

        private class ActivatingProcess : InstantProcess<EntityData>
        {
            public ActivatingProcess(EntityData data) : base(data)
            {
            }

            ///<inheritdoc />
            public override void Start()
            {
                Data.IsCompleted = false;
            }
        }

        private class ActiveProcess : BaseActiveProcessOverCompletable<EntityData>
        {
            public ActiveProcess(EntityData data) : base(data)
            {
            }

            ///<inheritdoc />
            protected override bool CheckIfCompleted()
            {
                return Data.Conditions
                    .Where(condition => Data.Mode.CheckIfSkipped(condition.GetType()) == false)
                    .All(condition => condition.IsCompleted);
            }
        }

        private class EntityAutocompleter : Autocompleter<EntityData>
        {
            public EntityAutocompleter(EntityData data) : base(data)
            {
            }

            ///<inheritdoc />
            public override void Complete()
            {
                foreach (ICondition condition in Data.Conditions.Where(condition => Data.Mode.CheckIfSkipped(condition.GetType()) == false))
                {
                    condition.Autocomplete();
                }
            }
        }

        ///<inheritdoc />
        ITransitionData IDataOwner<ITransitionData>.Data
        {
            get { return Data; }
        }

        ///<inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new CompositeProcess(new EntityOwners.ParallelEntityCollection.ParallelActivatingProcess<EntityData>(Data), new ActivatingProcess(Data));
        }

        ///<inheritdoc />
        public override IStageProcess GetActiveProcess()
        {
            return new CompositeProcess(new EntityOwners.ParallelEntityCollection.ParallelActiveProcess<EntityData>(Data), new ActiveProcess(Data));
        }

        ///<inheritdoc />
        public override IStageProcess GetDeactivatingProcess()
        {
            return new EntityOwners.ParallelEntityCollection.ParallelDeactivatingProcess<EntityData>(Data);
        }

        ///<inheritdoc />
        protected override IConfigurator GetConfigurator()
        {
            return new ParallelConfigurator<ICondition>(Data);
        }

        ///<inheritdoc />
        protected override IAutocompleter GetAutocompleter()
        {
            return new EntityAutocompleter(Data);
        }

        /// <inheritdoc />
        public Transition()
        {
            Data.Conditions = new List<ICondition>();
            Data.TargetStep = null;

            if (LifeCycleLoggingConfig.Instance.LogTransitions)
            {
                LifeCycle.StageChanged += (sender, args) =>
                {
                    Debug.LogFormat("{0}<b>Transition to</b> <i>{1}</i> is <b>{2}</b>.\n", ConsoleUtils.GetTabs(3), Data.TargetStep != null ? Data.TargetStep.Data.Name + " (Step)" : "chapter's end", LifeCycle.Stage);
                };
            }
        }

        /// <inheritdoc />
        public IEnumerable<LockablePropertyData> GetLockableProperties()
        {
            IEnumerable<LockablePropertyData> lockable = new List<LockablePropertyData>();
            foreach (ICondition condition in Data.Conditions)
            {
                if (condition is ILockablePropertiesProvider lockableCondition)
                {
                    lockable = lockable.Union(lockableCondition.GetLockableProperties());
                }
            }
            return lockable;
        }

        /// <inheritdoc />
        public ITransition Clone()
        {
            Transition clonedTransition = new Transition();
            clonedTransition.Data.Conditions = Data.Conditions.Select(condition => condition.Clone()).ToList();
            clonedTransition.Data.TargetStep = Data.TargetStep;
            return clonedTransition;            
        }
    }
}
