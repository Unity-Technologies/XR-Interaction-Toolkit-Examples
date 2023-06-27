using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Audio;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.Configuration.Modes;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace VRBuilder.Core.Behaviors
{
    /// <summary>
    /// A behavior that plays audio.
    /// </summary>
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-behaviors/play-audio-file")]
    public class PlayAudioBehavior : Behavior<PlayAudioBehavior.EntityData>, IOptional
    {
        /// <summary>
        /// The "play audio" behavior's data.
        /// </summary>
        [DataContract(IsReference = true)]
        public class EntityData : IBackgroundBehaviorData
        {
            /// <summary>
            /// An audio data that contains an audio clip to play.
            /// </summary>
            [DataMember]
            public IAudioData AudioData { get; set; }

            /// <summary>
            /// A property that determines if the audio should be played at activation or deactivation (or both).
            /// </summary>
            [DataMember]
            [DisplayName("Execution stages")]
            public BehaviorExecutionStages ExecutionStages { get; set; }

            /// <summary>
            /// Audio volume this audio file should be played with.
            /// </summary>
            [DataMember]
            [DisplayName("Audio Volume (from 0 to 1)")]
            [UsesSpecificProcessDrawer("NormalizedFloatDrawer")]
            public float Volume { get; set; } = 1.0f;

            /// <summary>
            /// The Unity's audio source to play the sound. If not set, it will use <seealso cref="RuntimeConfigurator.Configuration.InstructionPlayer"/>.
            /// </summary>
            public AudioSource AudioPlayer { get; set; }

            /// <inheritdoc />
            public Metadata Metadata { get; set; }

            /// <inheritdoc />
            [IgnoreDataMember]
            public string Name
            {
                get
                {
                    string executionStages = "";

                    switch(ExecutionStages)
                    {
                        case BehaviorExecutionStages.Activation:
                            executionStages = " on activation";
                            break;
                        case BehaviorExecutionStages.Deactivation:
                            executionStages = " on deactivation";
                            break;
                        case BehaviorExecutionStages.ActivationAndDeactivation:
                            executionStages = " on activation and deactivation";
                            break;
                    }
                    return $"Play audio{executionStages}";
                }
            }

            /// <inheritdoc />
            public bool IsBlocking { get; set; }
        }

        private class PlayAudioProcess : StageProcess<EntityData>
        {
            private readonly BehaviorExecutionStages executionStages;
            IProcessAudioPlayer audioPlayer;

            public PlayAudioProcess(BehaviorExecutionStages executionStages, EntityData data) : base(data)
            {
                this.executionStages = executionStages;
            }

            /// <inheritdoc />
            public override void Start()
            {
                if (Data.AudioPlayer != null)
                {
                    audioPlayer = new DefaultAudioPlayer(Data.AudioPlayer);
                }
                else
                {
                    audioPlayer = RuntimeConfigurator.Configuration.ProcessAudioPlayer;
                }

                if ((Data.ExecutionStages & executionStages) > 0)
                {
                    if (Data.AudioData.HasAudioClip)
                    {
                        audioPlayer.PlayAudio(Data.AudioData, Mathf.Clamp(Data.Volume, 0.0f, 1.0f));
                    }
                    else
                    {
                        Debug.LogWarning("AudioData has no audio clip.");
                    }
                }
            }

            /// <inheritdoc />
            public override IEnumerator Update()
            {
                while ((Data.ExecutionStages & executionStages) > 0 && audioPlayer.IsPlaying)
                {
                    yield return null;
                }
            }

            /// <inheritdoc />
            public override void End()
            {
                if ((Data.ExecutionStages & executionStages) > 0)
                {
                    audioPlayer.Reset();
                }
            }

            /// <inheritdoc />
            public override void FastForward()
            {
                if ((Data.ExecutionStages & executionStages) > 0 && audioPlayer.IsPlaying)
                {
                    audioPlayer.Stop();
                }
            }
        }

        [JsonConstructor, Preserve]
        protected PlayAudioBehavior() : this(null, BehaviorExecutionStages.None)
        {
        }

        public PlayAudioBehavior(IAudioData audioData, BehaviorExecutionStages executionStages, AudioSource audioPlayer = null)
        {
            Data.AudioData = audioData;
            Data.ExecutionStages = executionStages;
            Data.AudioPlayer = audioPlayer;
            Data.IsBlocking = true;
        }

        public PlayAudioBehavior(IAudioData audioData, BehaviorExecutionStages executionStages, bool isBlocking, AudioSource audioPlayer = null) : this(audioData, executionStages, audioPlayer)
        {
            Data.IsBlocking = isBlocking;
        }

        /// <inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new PlayAudioProcess(BehaviorExecutionStages.Activation, Data);
        }

        /// <inheritdoc />
        public override IStageProcess GetDeactivatingProcess()
        {
            return new PlayAudioProcess(BehaviorExecutionStages.Deactivation, Data);
        }
    }
}
