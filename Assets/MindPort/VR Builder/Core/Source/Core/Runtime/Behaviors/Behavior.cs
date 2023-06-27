// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEngine;
using System.Runtime.Serialization;
using VRBuilder.Core.Utils.Logging;
using VRBuilder.Unity;

namespace VRBuilder.Core.Behaviors
{
    /// <summary>
    /// Inherit from this abstract class when creating your own behaviors.
    /// </summary>
    /// <typeparam name="TData">The type of the behavior's data.</typeparam>
    [DataContract(IsReference = true)]
    public abstract class Behavior<TData> : Entity<TData>, IBehavior where TData : class, IBehaviorData, new()
    {
        /// <inheritdoc />
        IBehaviorData IDataOwner<IBehaviorData>.Data
        {
            get { return Data; }
        }

        protected Behavior()
        {
            if (LifeCycleLoggingConfig.Instance.LogBehaviors)
            {
                LifeCycle.StageChanged += (sender, args) =>
                {
                    Debug.LogFormat("{0}<b>Behavior</b> <i>'{1} ({2})'</i> is <b>{3}</b>.\n", ConsoleUtils.GetTabs(2), Data.Name, GetType().Name, LifeCycle.Stage);
                };
            }
        }

        /// <inheritdoc />
        public virtual IBehavior Clone()
        {
            return MemberwiseClone() as IBehavior;
        }
    }
}
