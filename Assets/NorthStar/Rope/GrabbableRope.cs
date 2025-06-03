// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.Utilities;
using Meta.Utilities.Ropes;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace NorthStar
{
    public class GrabbableRope : MonoBehaviour
    {
        [SerializeField] private bool m_leftHand;
        [SerializeField] private BurstRope m_rope;
        [SerializeField, AutoSet] private PhysicsTransformer m_transform;
        [SerializeField, AutoSet] private Rigidbody m_body;
        private bool m_grabing = false;
        private Transform m_hand;
        public bool Grabbed { get => m_grabing; set { } }
        [SerializeField] public int BindingPointIndex;
        [SerializeField] public int TargetIndex;
        private ConfigurableJoint m_topJoint, m_bottomJoint;
        [SerializeField] private bool m_useLimit;
        [SerializeField] private float m_jointSpringForce = 50;
        [SerializeField] private float m_jointLeniency = 1.5f;
        [SerializeField] private bool m_allowSlipping;
        [SerializeField] private float m_slipForce = 1500;
        private void Awake()
        {
            m_transform.OnInteraction += Grab;
            m_transform.OnEndInteraction += EndGrab;
            m_topJoint = gameObject.AddComponent<ConfigurableJoint>();
            m_bottomJoint = gameObject.AddComponent<ConfigurableJoint>();
        }

        private void Start()
        {
            m_hand = m_leftHand ? BodyPositions.GetLeftHand() : BodyPositions.GetRightHand();
        }

        private void EndGrab(HandGrabInteractor interactor)
        {
            if (!m_grabing) return; m_grabing = false;
            m_rope.Binds[BindingPointIndex] = new();
        }

        private void Grab(HandGrabInteractor interactor)
        {
            if (m_grabing) return; m_grabing = true;
            TargetIndex = m_rope.ClosestIndexToPoint(transform.position);
            var binding = new BindingPoint()
            {
                Index = TargetIndex,
                Target = m_rope.transform.InverseTransformPoint(transform.position),
                Bound = true
            };

            m_rope.Binds[BindingPointIndex] = binding;

        }

        private void Update()
        {
            if (m_grabing)
            {
                if (m_useLimit)
                {
                    var nextClosest = -1;
                    var point = transform.position;
                    foreach (var bp in m_rope.Binds)
                    {
                        if (bp.Index == TargetIndex)
                            continue;
                        if (bp.Index > nextClosest && bp.Index < TargetIndex && bp.Bound)
                        {
                            nextClosest = bp.Index;
                            point = m_rope.FromRopeSpace(bp.Target);
                        }
                    }
                    var topDistance = m_rope.NodeDistance * m_jointLeniency * (TargetIndex - nextClosest);
                    ConfigureJoint(m_topJoint, point, topDistance, nextClosest != -1);

                    nextClosest = m_rope.NodeCount;
                    point = transform.position;
                    foreach (var bp in m_rope.Binds)
                    {
                        if (bp.Index == TargetIndex)
                            continue;
                        if (bp.Index < nextClosest && bp.Index > TargetIndex)
                        {
                            nextClosest = bp.Index;
                            point = m_rope.FromRopeSpace(bp.Target);
                        }
                    }

                    var bottomDistance = m_rope.NodeDistance * (nextClosest - TargetIndex);
                    ConfigureJoint(m_bottomJoint, point, bottomDistance, nextClosest != m_rope.NodeCount);
                    //Debug.Log(new Vector2(topDistance, bottomDistance));
                    if (m_allowSlipping)
                    {
                        var topShouldSlip = m_topJoint.currentForce.magnitude > m_slipForce;
                        var bottomShouldSlip = m_bottomJoint.currentForce.magnitude > m_slipForce;

                        if (topShouldSlip && !bottomShouldSlip)
                        {
                            TargetIndex++;
                        }
                        else if (bottomShouldSlip && !topShouldSlip)
                        {
                            TargetIndex--;
                        }
                    }
                }

                var bind = m_rope.Binds[BindingPointIndex];
                bind.Target = m_rope.transform.InverseTransformPoint(transform.position);
                bind.Index = TargetIndex;
                m_rope.Binds[BindingPointIndex] = bind;
            }
            else
            {
                transform.position = m_rope.ClosestPointOnRope(m_hand.position);
                ConfigureJoint(m_topJoint, Vector3.zero, float.PositiveInfinity, false);
                ConfigureJoint(m_bottomJoint, Vector3.zero, float.PositiveInfinity, false);
            }
        }

        private void ConfigureJoint(ConfigurableJoint joint, Vector3 point, float distance, bool bound)
        {
            joint.autoConfigureConnectedAnchor = false;
            joint.linearLimit = new()
            {
                limit = distance,
            };
            joint.linearLimitSpring = new()
            {
                spring = m_jointSpringForce,
            };
            //joint.swapBodies = true;
            joint.connectedAnchor = point;// transform.TransformPoint(point);
            joint.xMotion = bound ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
            joint.yMotion = bound ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
            joint.zMotion = bound ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
        }
    }
}

