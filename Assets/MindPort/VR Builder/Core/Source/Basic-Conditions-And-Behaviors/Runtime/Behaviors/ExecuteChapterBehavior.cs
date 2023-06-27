using System.Collections;
using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;
using Newtonsoft.Json;
using UnityEngine.Scripting;
using VRBuilder.Core.EntityOwners;
using System.Collections.Generic;

namespace VRBuilder.Core.Behaviors
{
    /// <summary>
    /// Behavior that executes a stored chapter and completes when the chapter ends.
    /// </summary>
    [DataContract(IsReference = true)]
    public class ExecuteChapterBehavior : Behavior<ExecuteChapterBehavior.EntityData>
    {
        /// <summary>
        /// Execute chapter behavior data.
        /// </summary>
        [DisplayName("Step Group")]
        [DataContract(IsReference = true)]
        public class EntityData : EntityCollectionData<IChapter>, IBehaviorData
        {
            [DataMember]
            public IChapter Chapter { get; set; }

            public string Name { get; set; }

            public override IEnumerable<IChapter> GetChildren()
            {
                return new List<IChapter>() { Chapter };
            }
        }

        [JsonConstructor, Preserve]
        public ExecuteChapterBehavior() : this(null)
        {
        }

        public ExecuteChapterBehavior(IChapter chapter)
        {
            Data.Chapter = chapter;
            Data.Name = "Step Group";
        }

        private class ActivatingProcess : StageProcess<EntityData>
        {
            public ActivatingProcess(EntityData data) : base(data)
            {
            }

            /// <inheritdoc />
            public override void Start()
            {
                Data.Chapter.LifeCycle.Activate();
            }

            /// <inheritdoc />
            public override IEnumerator Update()
            {
                while (Data.Chapter.LifeCycle.Stage != Stage.Active)
                {
                    Data.Chapter.Update();
                    yield return null;
                }
            }

            /// <inheritdoc />
            public override void End()
            {
            }

            /// <inheritdoc />
            public override void FastForward()
            {
                if (Data.Chapter.Data.Current == null)
                {
                    Data.Chapter.Data.Current = Data.Chapter.Data.FirstStep;
                }

                Data.Chapter.LifeCycle.MarkToFastForwardStage(Stage.Activating);
            }
        }

        private class DeactivatingProcess : StageProcess<EntityData>
        {
            public DeactivatingProcess(EntityData data) : base(data)
            {
            }

            /// <inheritdoc />
            public override void Start()
            {
                Data.Chapter.LifeCycle.Deactivate();
            }

            /// <inheritdoc />
            public override IEnumerator Update()
            {
                while (Data.Chapter.LifeCycle.Stage != Stage.Inactive)
                {
                    Data.Chapter.Update();
                    yield return null;
                }
            }

            /// <inheritdoc />
            public override void End()
            {
            }

            /// <inheritdoc />
            public override void FastForward()
            {
                Data.Chapter.LifeCycle.MarkToFastForwardStage(Stage.Deactivating);
            }
        }

        /// <inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new ActivatingProcess(Data);
        }

        /// <inheritdoc />
        public override IStageProcess GetDeactivatingProcess()
        {
            return new DeactivatingProcess(Data);
        }

        /// <inheritdoc />
        public override IBehavior Clone()
        {
            ExecuteChapterBehavior clonedBehavior = new ExecuteChapterBehavior();
            clonedBehavior.Data.Chapter = Data.Chapter.Clone();
            return clonedBehavior;
        }
    }
}
