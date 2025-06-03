// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Events;

namespace Meta.Utilities
{
    public class AnimationStateTriggerListener : MonoBehaviour
    {
        [SerializeField] private UnityEvent m_onStateEnter;
        [SerializeField] private UnityEvent m_onStateExit;

        public virtual void OnStateEnter(AnimatorStateInfo stateInfo, int layerIndex)
        {
            m_onStateEnter?.Invoke();
        }

        public virtual void OnStateExit(AnimatorStateInfo stateInfo, int layerIndex)
        {
            m_onStateExit?.Invoke();
        }
    }
}
