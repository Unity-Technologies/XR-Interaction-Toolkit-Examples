// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Linq;
using UnityEngine;

namespace Meta.Utilities.Narrative
{
    /// <summary>
    /// Task conditoin that waits until an animation has started/completed
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "NorthStar", sourceAssembly: "Assembly-CSharp")]
    public class WaitForAnimationCondition : TaskCondition
    {
        public enum AnimNotPlayingBehaviour { CompleteImmediately, WaitForAnimationStart }

        public Behaviour Animatable;
        public string ClipOrStateName;
        public AnimNotPlayingBehaviour IfNotPlaying = AnimNotPlayingBehaviour.WaitForAnimationStart;

        [LabelWidth(220)] public bool IncompleteIfAnimationRepeats;

        private bool m_completed;
        private bool m_animStarted;

        private Animator Animator => Animatable as Animator;

        private Animation Animation => Animatable as Animation;

        private bool AnimPlaying
        {
            get
            {
                if (Animation)
                {
                    return string.IsNullOrWhiteSpace(ClipOrStateName)
                        ? Animation.isPlaying
                        : Animation.IsPlaying(ClipOrStateName);
                }
                if (!Animator) { return true; }
                var stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
                return stateInfo.normalizedTime is < 1f and > 0f
&& (string.IsNullOrWhiteSpace(ClipOrStateName)
|| stateInfo.IsName(ClipOrStateName)
|| Animator.GetCurrentAnimatorClipInfo(0)
                               .Any(clipInfo => clipInfo.clip.name == ClipOrStateName));
            }
        }

        public override string Description
        {
            get
            {
                var clip = ClipOrStateName;
                if (string.IsNullOrWhiteSpace(clip)) { clip = "Any clip"; }
                return $"{clip} on {(Animatable ? Animatable.name : "unset target")}";
            }
        }


        public override void OnValidate(TaskHandler handler)
        {
            if (Animatable && !Animator && !Animation) { Animatable = null; }
        }


        public override void OnTaskStarted(TaskHandler handler)
        {
            m_completed = false;
            m_animStarted = false;
        }


        // TODO: verify this logic
        public override bool IsComplete(TaskHandler handler)
        {
            if (m_completed && !IncompleteIfAnimationRepeats) { return true; }

            var playing = AnimPlaying;

            if (IfNotPlaying == AnimNotPlayingBehaviour.CompleteImmediately && !playing)
            {
                return m_completed = true;
            }

            m_animStarted |= playing;

            return m_animStarted && !playing && !IncompleteIfAnimationRepeats ? (m_completed = true) : !playing;
        }
    }
}