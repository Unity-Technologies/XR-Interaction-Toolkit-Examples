using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Configuration.Modes;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Properties;
using VRBuilder.Core.Utils;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace VRBuilder.Core.Behaviors
{
    /// <summary>
    /// Behavior that highlights the target <see cref="ISceneObject"/> with the specified color until the behavior is being deactivated.
    /// </summary>
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-behaviors/highlight-object")]
    public class HighlightObjectBehavior : Behavior<HighlightObjectBehavior.EntityData>, IOptional
    {
        /// <summary>
        /// "Highlight object" behavior's data.
        /// </summary>
        [DisplayName("Highlight Object")]
        [DataContract(IsReference = true)]
        public class EntityData : IBehaviorData
        {
            /// <summary>
            /// <see cref="ModeParameter{T}"/> of the highlight color.
            /// Process modes can change the highlight color.
            /// </summary>
            public ModeParameter<Color> CustomHighlightColor { get; set; }

            /// <summary>
            /// Highlight color set in the Step Inspector.
            /// </summary>
            [DataMember]
            [DisplayName("Color")]
            public Color HighlightColor
            {
                get { return CustomHighlightColor.Value; }

                set { CustomHighlightColor = new ModeParameter<Color>("HighlightColor", value); }
            }

            /// <summary>
            /// Target scene object to be highlighted.
            /// </summary>
            [DataMember]
            [DisplayName("Object")]
            public ScenePropertyReference<IHighlightProperty> ObjectToHighlight { get; set; }

            /// <inheritdoc />
            public Metadata Metadata { get; set; }

            /// <inheritdoc />
            [IgnoreDataMember]
            public string Name
            {
                get
                {
                    string objectToHighlight = ObjectToHighlight.IsEmpty() ? "[NULL]" : ObjectToHighlight.Value.SceneObject.GameObject.name;
                    return $"Highlight {objectToHighlight}";
                }
            }
        }

        private class ActivatingProcess : InstantProcess<EntityData>
        {
            public ActivatingProcess(EntityData data) : base(data)
            {
            }

            /// <inheritdoc />
            public override void Start()
            {
                Data.ObjectToHighlight.Value?.Highlight(Data.HighlightColor);
            }
        }

        private class DeactivatingProcess : InstantProcess<EntityData>
        {
            public DeactivatingProcess(EntityData data) : base(data)
            {
            }

            /// <inheritdoc />
            public override void Start()
            {
                Data.ObjectToHighlight.Value?.Unhighlight();
            }
        }

        private class EntityConfigurator : Configurator<EntityData>
        {
            /// <inheritdoc />
            public override void Configure(IMode mode, Stage stage)
            {
                Data.CustomHighlightColor.Configure(mode);
            }

            public EntityConfigurator(EntityData data) : base(data)
            {
            }
        }

        [JsonConstructor, Preserve]
        public HighlightObjectBehavior() : this("", new Color32(231, 64, 255, 126))
        {
        }

        public HighlightObjectBehavior(string sceneObjectName, Color highlightColor)
        {
            Data.ObjectToHighlight = new ScenePropertyReference<IHighlightProperty>(sceneObjectName);
            Data.HighlightColor = highlightColor;
        }

        public HighlightObjectBehavior(IHighlightProperty target) : this(target, new Color32(231, 64, 255, 126))
        {
        }

        public HighlightObjectBehavior(IHighlightProperty target, Color highlightColor) : this(ProcessReferenceUtils.GetNameFrom(target), highlightColor)
        {
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
        protected override IConfigurator GetConfigurator()
        {
            return new EntityConfigurator(Data);
        }
    }
}
