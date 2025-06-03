// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Utilities
{
    public class AnimationStateTriggers : StateMachineBehaviour
    {
        private AnimationStateTriggerListener m_listener;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (m_listener == null)
                m_listener = animator.GetComponent<AnimationStateTriggerListener>();
            m_listener.OnStateEnter(stateInfo, layerIndex);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (m_listener == null)
                m_listener = animator.GetComponent<AnimationStateTriggerListener>();
            m_listener.OnStateExit(stateInfo, layerIndex);
        }
    }
}
