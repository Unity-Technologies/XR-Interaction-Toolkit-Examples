// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.Utilities;
using Meta.Utilities.Ropes;
using UnityEngine;

namespace NorthStar
{
    public class RopeGrabHandConstraint : MonoBehaviour
    {
        private GrabbableRope[] m_hands;
        [SerializeField] private BurstRope m_rope;
        private ConfigurableJoint m_joint;
        [SerializeField, AutoSet] private Rigidbody m_body;
        [SerializeField] private float m_jointLeniency = 2;

        private void Awake()
        {
            m_hands = m_rope.GetComponentsInChildren<GrabbableRope>();
            m_joint = gameObject.AddComponent<ConfigurableJoint>();
            SetupJoint(m_joint);
        }
        private void Update()
        {
            m_rope.Binds[0] = new BindingPoint
            {
                Target = m_rope.ToRopeSpace(transform.position),
                Index = 0,
                Bound = true
            };
            var lowestBind = new BindingPoint()
            {
                Index = int.MaxValue,
            };
            foreach (var bind in m_rope.Binds)
            {
                if (bind.Bound && bind.Index < lowestBind.Index && bind.Index != 0)
                {
                    lowestBind = bind;
                }
            }

            foreach (var hand in m_hands)
            {
                if (hand.TargetIndex == lowestBind.Index)
                {
                    m_joint.connectedBody = hand.GetComponent<Rigidbody>();
                    m_joint.swapBodies = true;
                    m_joint.connectedAnchor = Vector3.zero;
                    m_joint.linearLimit = new()
                    {
                        limit = lowestBind.Index * m_rope.NodeDistance * m_jointLeniency,
                    };
                    return;
                }
            }

            if (lowestBind.Bound)
            {
                m_joint.connectedBody = null;
                m_joint.connectedAnchor = m_rope.FromRopeSpace(lowestBind.Target);
                m_joint.swapBodies = true;
                m_joint.linearLimit = new()
                {
                    limit = lowestBind.Index * m_rope.NodeDistance * m_jointLeniency,
                };
            }

        }

        private void SetupJoint(ConfigurableJoint joint)
        {
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.autoConfigureConnectedAnchor = false;
            m_joint.connectedAnchor = Vector3.zero;
        }
    }
}
