// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Configuration.Modes;
using VRBuilder.Core.EntityOwners;
using VRBuilder.Core.EntityOwners.ParallelEntityCollection;

namespace VRBuilder.Core
{
    /// <summary>
    /// A collection of <see cref="IBehavior"/>s of a <see cref="IStep"/>.
    /// </summary>
    [DataContract(IsReference = true)]
    public class BehaviorCollection : Entity<BehaviorCollection.EntityData>, IBehaviorCollection
    {
        /// <summary>
        /// The data class for <see cref="IBehavior"/> collections.
        /// </summary>
        [DataContract(IsReference = true)]
        public class EntityData : EntityCollectionData<IBehavior>, IBehaviorCollectionData
        {
            /// <summary>
            /// List of all <see cref="IBehavior"/>s added.
            /// </summary>
            [DataMember]
            [DisplayName(""), ReorderableListOf(typeof(FoldableAttribute), typeof(DeletableAttribute), typeof(DrawIsBlockingToggleAttribute), typeof(HelpAttribute)), ExtendableList]
            public virtual IList<IBehavior> Behaviors { get; set; }

            /// <summary>
            /// Returns a list of all <see cref="IBehavior"/>s added.
            /// </summary>
            public override IEnumerable<IBehavior> GetChildren()
            {
                return Behaviors.ToList();
            }

            /// <summary>
            /// Reference to <see cref="IBehavior"/>'s current mode.
            /// </summary>
            public IMode Mode { get; set; }
        }

        /// <inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new ParallelActivatingProcess<EntityData>(Data);
        }

        /// <inheritdoc />
        public override IStageProcess GetActiveProcess()
        {
            return new ParallelActiveProcess<EntityData>(Data);
        }

        /// <inheritdoc />
        public override IStageProcess GetDeactivatingProcess()
        {
            return new ParallelDeactivatingProcess<EntityData>(Data);
        }

        /// <inheritdoc />
        protected override IConfigurator GetConfigurator()
        {
            return new ParallelConfigurator<IBehavior>(Data);
        }

        /// <inheritdoc />
        public IBehaviorCollection Clone()
        {
            BehaviorCollection clonedBehaviorCollection = new BehaviorCollection();
            clonedBehaviorCollection.Data.Behaviors = Data.Behaviors.Select(behavior => behavior.Clone()).ToList();
            return clonedBehaviorCollection;
        }

        /// <inheritdoc />
        IBehaviorCollectionData IDataOwner<IBehaviorCollectionData>.Data
        {
            get { return Data; }
        }

        public BehaviorCollection()
        {
            Data.Behaviors = new List<IBehavior>();
        }
    }
}
