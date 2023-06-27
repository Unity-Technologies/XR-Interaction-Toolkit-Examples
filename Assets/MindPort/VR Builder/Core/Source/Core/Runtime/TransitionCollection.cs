// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Configuration.Modes;
using VRBuilder.Core.EntityOwners;
using VRBuilder.Core.EntityOwners.ParallelEntityCollection;

namespace VRBuilder.Core
{
    /// <summary>
    /// A collection of <see cref="ITransition"/>s.
    /// </summary>
    [DataContract(IsReference = true)]
    public class TransitionCollection : Entity<TransitionCollection.EntityData>, ITransitionCollection
    {
        /// <summary>
        /// The data class of the <see cref="ITransition"/>s' collection.
        /// </summary>
        [DataContract(IsReference = true)]
        public class EntityData : EntityCollectionData<ITransition>, ITransitionCollectionData
        {
            ///<inheritdoc />
            [DataMember]
            [DisplayName(""), KeepPopulated(typeof(Transition)), ReorderableListOf(typeof(FoldableAttribute), typeof(DeletableAttribute)), ExtendableList]
            public virtual IList<ITransition> Transitions { get; set; }

            ///<inheritdoc />
            public override IEnumerable<ITransition> GetChildren()
            {
                return Transitions.ToArray();
            }

            ///<inheritdoc />
            public IMode Mode { get; set; }
        }

        private class ActiveProcess : StageProcess<EntityData>
        {
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
                while (Data.Transitions.All(transition => transition.IsCompleted == false))
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
        protected override IConfigurator GetConfigurator()
        {
            return new ParallelConfigurator<ITransition>(Data);
        }

        ///<inheritdoc />
        ITransitionCollectionData IDataOwner<ITransitionCollectionData>.Data
        {
            get { return Data; }
        }

        public TransitionCollection()
        {
            Data.Transitions = new List<ITransition>();
        }

        ///<inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new ParallelActivatingProcess<EntityData>(Data);
        }

        ///<inheritdoc />
        public override IStageProcess GetActiveProcess()
        {
            return new CompositeProcess(new ParallelActiveProcess<EntityData>(Data), new ActiveProcess(Data));
        }

        ///<inheritdoc />
        public override IStageProcess GetDeactivatingProcess()
        {
            return new ParallelDeactivatingProcess<EntityData>(Data);
        }

        ///<inheritdoc />
        public ITransitionCollection Clone()
        {
            TransitionCollection clonedTransitionCollection = new TransitionCollection();
            clonedTransitionCollection.Data.Transitions = Data.Transitions.Select(transition => transition.Clone()).ToList();
            return clonedTransitionCollection;
        }
    }
}
