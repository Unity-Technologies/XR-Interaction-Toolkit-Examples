// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using Meta.Utilities;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// Overrides the Itransformer to provide rigidboydy based interactions
    /// </summary>
    public class PhysicsTransformer : MonoBehaviour, ITransformer
    {
        private HandGrabInteractable[] m_interactables;

        [SerializeField, AutoSet] private Rigidbody m_body;
        [SerializeField] private float m_additionalDragOnGrab;
        [SerializeField] private float m_additionalAngularDragOnGrab;
        [SerializeField, Range(-1, 1)] private float m_moveStrengthModifier = 0;
        [SerializeField, Range(-1, 1)] private float m_rotationStrengthModifier = 0;
        [SerializeField] private float m_softGrabTime = .2f;
        [SerializeField] private float m_bonusBreakTimer = 0;
        [SerializeField] private bool m_toggleIntertiaTensorOnGrab;
        [SerializeField, Range(0, 2)] private float m_throwVelocityPercent = 0;
        [SerializeField] private int m_velocityHistoryCapacity = 20;

        private Dictionary<HandGrabInteractor, JointState> m_joints = new();
        private List<Vector3> m_velocityHistory;

        public delegate void InteractionCallBack(HandGrabInteractor interactor);
        public InteractionCallBack OnInteraction;
        public InteractionCallBack OnEndInteraction;

        private class JointState
        {
            public Rigidbody Body;
            public ConfigurableJoint Joint;
            public HandGrabInteractable Interactable;
            public float CreationTime;
            public bool Done = false;
        }

        private void Awake()
        {
            m_interactables = GetComponentsInChildren<HandGrabInteractable>();
            m_velocityHistory = new(m_velocityHistoryCapacity);
            foreach (var interactable in m_interactables)
            {
                interactable.WhenSelectingInteractorAdded.Action += (HandGrabInteractor i) => AddInteractor(i, interactable);
                interactable.WhenSelectingInteractorRemoved.Action += RemoveInteractor;
            }
        }

        private void AddInteractor(HandGrabInteractor interactor, HandGrabInteractable interactable)
        {
            Debug.Log(interactable.gameObject.name, interactable.gameObject);
            if (m_joints.ContainsKey(interactor))
                return;

            var joint = gameObject.AddComponent<ConfigurableJoint>();

            var physicalHand = interactor.GetComponent<PhysicalHandRef>().Hand;
            physicalHand.LocalMovementStrengthModifier += m_moveStrengthModifier;
            physicalHand.LocalRotationStrengthModifier += m_rotationStrengthModifier;
            physicalHand.ExcessBreakTimer += m_bonusBreakTimer;

            var jointState = new JointState()
            {
                Interactable = interactable,
                Body = physicalHand.Rigidbody,
                Joint = joint,
                CreationTime = Time.time
            };

            if (m_joints.Count == 0)
            {
                m_velocityHistory.Clear();
                m_body.linearDamping += m_additionalDragOnGrab;
                m_body.angularDamping += m_additionalAngularDragOnGrab;
                if (m_toggleIntertiaTensorOnGrab)
                {
                    m_body.automaticInertiaTensor = false;
                    m_body.inertiaTensor = Vector3.one;
                    m_body.inertiaTensorRotation = Quaternion.identity;
                }
            }
            m_joints.Add(interactor, jointState);
            OnInteraction?.Invoke(interactor);
        }

        private void Update()
        {
            foreach (var jointState in m_joints.Values)
            {
                if (jointState.Done)
                    continue;
                var t = (Time.time - jointState.CreationTime) / m_softGrabTime;
                var endPoint = GetPoint(jointState.Body.transform, jointState.Interactable, out var endRotation);
                var pos = Vector3.Lerp(transform.InverseTransformPoint(jointState.Body.transform.position), endPoint, t);
                var rot = Quaternion.Slerp(jointState.Body.transform.rotation, transform.rotation * endRotation, t);

                SetJointState(jointState.Body, jointState.Joint, pos, rot);
                if (t > 1)
                    jointState.Done = true;
            }
        }

        private void FixedUpdate()
        {
            if (m_velocityHistoryCapacity == 0 || m_throwVelocityPercent == 0 || m_joints.Count == 0)
                return;
            if (m_velocityHistory.Count == m_velocityHistoryCapacity)
                m_velocityHistory.RemoveAt(0);
            m_velocityHistory.Add(m_body.linearVelocity);
        }

        private Vector3 GetPoint(Transform hand, HandGrabInteractable interactable, out Quaternion rotation)
        {
            var point = Vector3.zero;
            rotation = Quaternion.identity;
            if (interactable.UsesHandPose)
            {
                var pose = interactable.HandGrabPoses[0];
                point = pose.transform.position;
                rotation = Quaternion.Inverse(transform.rotation) * pose.transform.rotation;
            }
            if (interactable.gameObject.TryGetComponent(out ExtraInteractionData data))
            {
                var handRot = Quaternion.identity;
                if (data.FreeRotation)
                {
                    var poseRot = data.LineSegment.GetRotationRelative(rotation);
                    handRot = data.LineSegment.GetRotationRelative(Quaternion.Inverse(transform.rotation) * hand.rotation);
                    handRot *= Quaternion.Inverse(poseRot);
                    rotation = handRot * rotation;
                }
                var lineSegmentOffset = point - data.LineSegment.ClosestPoint(point, true);
                var closestPointToHand = transform.InverseTransformPoint(data.LineSegment.ClosestPoint(hand.position, true) + lineSegmentOffset);
                point = handRot * closestPointToHand;
            }
            else
                point = transform.InverseTransformPoint(point);
            return point;
        }

        private void RemoveInteractor(HandGrabInteractor interactor)
        {
            if (!m_joints.ContainsKey(interactor))
                return;
            var joint = m_joints[interactor];
            Destroy(joint.Joint);
            _ = m_joints.Remove(interactor);
            if (m_joints.Count == 0)
            {
                m_body.linearDamping -= m_additionalDragOnGrab;
                m_body.angularDamping -= m_additionalAngularDragOnGrab;
                if (m_toggleIntertiaTensorOnGrab)
                {
                    m_body.automaticInertiaTensor = true;
                    m_body.ResetInertiaTensor();
                }

                if (m_throwVelocityPercent > 0)
                {
                    var avgVelocity = Vector3.zero;
                    foreach (var velocity in m_velocityHistory)
                        avgVelocity += velocity;
                    avgVelocity /= Mathf.Max(m_velocityHistory.Count, 1);
                    m_velocityHistory.Clear();
                    m_body.linearVelocity += avgVelocity * m_throwVelocityPercent;
                }
            }
            OnEndInteraction?.Invoke(interactor);
            var physicalHand = interactor.GetComponent<PhysicalHandRef>().Hand;
            physicalHand.LocalMovementStrengthModifier -= m_moveStrengthModifier;
            physicalHand.LocalRotationStrengthModifier -= m_rotationStrengthModifier;
            physicalHand.ExcessBreakTimer -= m_bonusBreakTimer;
        }

        //To set the joints anchor rotation you have to rotate the target to your desired relative rotation bind the joint then move it back
        private static void SetJointState(Rigidbody target, ConfigurableJoint joint, Vector3 position, Quaternion rotation)
        {
            joint.connectedBody = null;
            target.isKinematic = true;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = position;

            var oldRotation = target.transform.rotation;
            target.transform.rotation = rotation;
            joint.connectedBody = target;

            target.isKinematic = false;
            target.transform.rotation = oldRotation;
            LockJoint(joint);
        }


        private static void LockJoint(ConfigurableJoint joint)
        {
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
        }

        private static void UnlockJoint(ConfigurableJoint joint)
        {
            joint.xMotion = ConfigurableJointMotion.Free;
            joint.yMotion = ConfigurableJointMotion.Free;
            joint.zMotion = ConfigurableJointMotion.Free;

            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;
        }

        #region ITransformer implements
        //Effectively telling the transformer to ignore the interaction
        void ITransformer.BeginTransform()
        {

        }
        void ITransformer.EndTransform() { }
        void ITransformer.Initialize(IGrabbable grabbable)
        {
        }

        void ITransformer.UpdateTransform() { }
        #endregion
    }
}
