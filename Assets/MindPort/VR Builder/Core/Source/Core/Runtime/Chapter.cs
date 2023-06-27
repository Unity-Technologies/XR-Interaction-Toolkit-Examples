// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Configuration.Modes;
using VRBuilder.Core.EntityOwners;
using VRBuilder.Core.Exceptions;
using VRBuilder.Core.Utils;
using VRBuilder.Core.Utils.Logging;
using System;

namespace VRBuilder.Core
{
    /// <summary>
    /// A chapter of a process <see cref="Process"/>.
    /// </summary>
    [DataContract(IsReference = true)]
    public class Chapter : Entity<Chapter.EntityData>, IChapter
    {
        /// <summary>
        /// The chapter's data class.
        /// </summary>
        [DataContract(IsReference = true)]
        public class EntityData : EntityCollectionData<IStep>, IChapterData
        {
            /// <inheritdoc />
            [DataMember]
            [HideInProcessInspector]
            public string Name { get; set; }

            /// <summary>
            /// The first step of the chapter.
            /// </summary>
            [DataMember]
            public IStep FirstStep { get; set; }

            /// <summary>
            /// All steps of the chapter.
            /// </summary>
            [DataMember]
            public IList<IStep> Steps { get; set; }

            /// <inheritdoc />
            public override IEnumerable<IStep> GetChildren()
            {
                return Steps.ToArray();
            }

            /// <inheritdoc />
            public void SetName(string name)
            {
                Name = name;
            }

            /// <inheritdoc />
            public IMode Mode { get; set; }

            /// <inheritdoc />
            [IgnoreDataMember]
            public IStep Current { get; set; }
        }

        private class ActivatingProcess : EntityIteratingProcess<IEntitySequenceDataWithMode<IStep>, IStep>
        {
            private readonly IStep firstStep;

            public ActivatingProcess(IChapterData data) : base(data)
            {
                firstStep = data.FirstStep;
            }

            private IEnumerator<IStep> enumerator;

            private IEnumerator<IStep> GetChildren()
            {
                IStep current = firstStep;

                while (current != null)
                {
                    yield return current;

                    current = current.Data.Transitions.Data.Transitions.First(transition => transition.IsCompleted).Data.TargetStep;
                }
            }

            /// <inheritdoc />
            public override void Start()
            {
                enumerator = GetChildren();
                base.Start();
            }

            /// <inheritdoc />
            protected override bool ShouldActivateCurrent()
            {
                return true;
            }

            /// <inheritdoc />
            protected override bool ShouldDeactivateCurrent()
            {
                return Data.Current.Data.Transitions.Data.Transitions.Any(transition => transition.IsCompleted);
            }

            /// <inheritdoc />
            public override void End()
            {
                enumerator = null;
                base.End();
            }

            /// <inheritdoc />
            protected override bool TryNext(out IStep entity)
            {
                if (enumerator != null && enumerator.MoveNext())
                {
                    entity = enumerator.Current;
                    return true;
                }
                else
                {
                    entity = null;
                    return false;
                }
            }

            /// <inheritdoc />
            public override void FastForward()
            {
                if (Data.Current == null)
                {
                    return;
                }

                if (Data.Current.FindPathInGraph(step => step.Data.Transitions.Data.Transitions.Select(transition => transition.Data.TargetStep), null, out IList<IStep> pathToChapterEnd) == false)
                {
                    throw new InvalidStateException("The end of the chapter is not reachable from the current step.");
                }

                foreach (IStep step in pathToChapterEnd)
                {
                    if (Data.Current.LifeCycle.Stage == Stage.Inactive)
                    {
                        Data.Current.LifeCycle.Activate();
                    }

                    Data.Current.LifeCycle.MarkToFastForward();

                    ITransition toAutocomplete = Data.Current.Data.Transitions.Data.Transitions.First(transition => transition.Data.TargetStep == step);
                    if (toAutocomplete.IsCompleted == false)
                    {
                        toAutocomplete.Autocomplete();
                    }

                    Data.Current.LifeCycle.Deactivate();

                    Data.Current = step;
                }
            }
        }

        /// <inheritdoc />
        [DataMember]
        public ChapterMetadata ChapterMetadata { get; set; }

        /// <inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new ActivatingProcess(Data);
        }

        /// <inheritdoc />
        public override IStageProcess GetDeactivatingProcess()
        {
            return new StopEntityIteratingProcess<IStep>(Data);
        }

        /// <inheritdoc />
        protected override IConfigurator GetConfigurator()
        {
            return new SequenceConfigurator<IStep>(Data);
        }

        /// <inheritdoc />
        public IChapter Clone()
        {
            IChapter clonedChapter = new Chapter(Data.Name, null);
            clonedChapter.ChapterMetadata.EntryNodePosition = ChapterMetadata.EntryNodePosition;

            Dictionary<IStep, IStep> clonedSteps = new Dictionary<IStep, IStep>();

            foreach(IStep step in Data.Steps)
            {
                IStep clonedStep = step.Clone();
                clonedChapter.Data.Steps.Add(clonedStep);
                if(Data.FirstStep == step)
                {
                    clonedChapter.Data.FirstStep = clonedStep;
                }

                clonedSteps.Add(step, clonedStep);
            }

            foreach (ITransition transition in clonedChapter.Data.Steps.SelectMany(step => step.Data.Transitions.Data.Transitions))
            {
                if (transition.Data.TargetStep != null && clonedSteps.ContainsKey(transition.Data.TargetStep))
                {
                    transition.Data.TargetStep = clonedSteps[transition.Data.TargetStep];
                }
            }

            return clonedChapter;
        }

        /// <inheritdoc />
        IChapterData IDataOwner<IChapterData>.Data
        {
            get { return Data; }
        }

        protected Chapter() : this(null, null)
        {
        }

        public Chapter(string name, IStep firstStep)
        {
            ChapterMetadata = new ChapterMetadata();
            ChapterMetadata.Guid = Guid.NewGuid();

            Data.Name = name;
            Data.FirstStep = firstStep;
            Data.Steps = new List<IStep>();

            if (firstStep != null)
            {
                Data.Steps.Add(firstStep);
            }

            if (LifeCycleLoggingConfig.Instance.LogChapters)
            {
                LifeCycle.StageChanged += (sender, args) =>
                {
                    Debug.LogFormat("<b>Chapter</b> <i>'{0}'</i> is <b>{1}</b>.\n", Data.Name, LifeCycle.Stage.ToString());
                };
            }
        }
    }
}
