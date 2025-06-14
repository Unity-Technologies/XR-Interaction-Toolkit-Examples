// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Meta.Utilities.Narrative
{
    /// <summary>
    /// Task condition that waits until a playable has started/completed
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "NorthStar", sourceAssembly: "Assembly-CSharp")]
    public class WaitForPlayableCondition : TaskCondition
    {
        public enum NotPlayingBehaviour { CompleteImmediately, WaitForStart }

        public PlayableDirector Director;
        public NotPlayingBehaviour IfNotPlaying = NotPlayingBehaviour.WaitForStart;

        [LabelWidth(160)] public bool IncompleteIfRepeated;

        private bool m_completed;
        private bool m_animStarted;

        public override string Description
        {
            get
            {
                var desc = $"{(Director ? Director.name : "unset target")}";
                if (Director && Director.playableAsset)
                {
                    desc += $" ({Director.playableAsset.name})";
                }
                return desc;
            }
        }

        public override void OnTaskStarted(TaskHandler handler)
        {
            m_completed = false;
            m_animStarted = false;

            if (TaskManager.DebugLogs)
            {
                Debug.Log("WaitForPlayable condition started on handler for task "
                          + $"'{handler.TaskID}' with director {Director.name} and asset "
                          + $"{Director.playableAsset.name}; director state: {Director.state}");
            }
        }

        public override bool IsComplete(TaskHandler handler)
        {
            if (m_completed && !IncompleteIfRepeated) { return true; }

            var playing = Director != null && Director.state == PlayState.Playing && Director.time < Director.duration;

            if (IfNotPlaying == NotPlayingBehaviour.CompleteImmediately && !playing)
            {
                return m_completed = true;
            }

            m_animStarted |= playing;

            return m_animStarted && !playing && !IncompleteIfRepeated ? (m_completed = true) : !playing;
        }

        public override void ForceComplete(TaskHandler handler)
        {
            Director.Play();
            Director.Stop();
            Director.time = Director.duration;
            Director.Evaluate();
        }
    }
}