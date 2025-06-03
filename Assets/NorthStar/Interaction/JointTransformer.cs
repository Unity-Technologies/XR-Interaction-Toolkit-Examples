// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// Implementation of the ITransformer interface for physical interaction
    /// </summary>
    public class JointTransformer : MonoBehaviour, ITransformer
    {
        [SerializeField] private Vector3 m_anchor = Vector3.zero;
        [SerializeField] private float m_maxJointForce = float.PositiveInfinity, m_maxJointTorque = float.PositiveInfinity;
        private HandGrabInteractable m_handGrabInteractable;
        private Dictionary<HandGrabInteractor, ConfigurableJoint> m_joints = new();
        private Rigidbody m_body;

        public delegate void InteractionCallBack();
        public InteractionCallBack OnInteraction;
        public InteractionCallBack OnEndinteraction;

        public List<Handle> Handles = new();

        public bool HasInteractor()
        {
            return m_joints.Count > 0;
        }

        private void Awake()
        {
            m_handGrabInteractable = transform.GetComponent<HandGrabInteractable>();
            m_handGrabInteractable.WhenSelectingInteractorAdded.Action += AddInteractor;
            m_handGrabInteractable.WhenSelectingInteractorRemoved.Action += RemoveInteractor;
            m_body = m_handGrabInteractable.Rigidbody;
        }

        private void AddInteractor(HandGrabInteractor interactor)
        {
            var physicalHandRef = interactor.GetComponent<PhysicalHandRef>();
            var hand = physicalHandRef.Hand;
            if (m_joints.ContainsKey(interactor))
                return;

            var wasKinematic = m_body.isKinematic;
            hand.StartInteraction();
            var joint = CreatePreConfiguredJoint();
            var startRotation = transform.rotation;
            if (Handles.Count > 0)
            {

                var closestHandle = Handles[0];
                var closestDistance = Vector3.Distance(closestHandle.transform.position, hand.transform.position);
                for (var i = 1; i < Handles.Count; i++)
                {
                    var distance = Vector3.Distance(Handles[i].transform.position, hand.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestHandle = Handles[i];
                    }
                }

                var handRotation = hand.transform.rotation * Quaternion.Euler(hand.JointAnchorRotation);
                var forward = Quaternion.Inverse(closestHandle.transform.localRotation) * GetAxis(handRotation * Vector3.forward, closestHandle.transform.forward, closestHandle.ForwardAxisCanBeInverted);
                var up = Quaternion.Inverse(closestHandle.transform.localRotation) * GetAxis(handRotation * Vector3.up, closestHandle.transform.up, closestHandle.UpCanBeInverted);

                joint.anchor = closestHandle.transform.localPosition;
                joint.axis = forward;
                joint.secondaryAxis = up;
                m_body.detectCollisions = false;
                m_body.isKinematic = true;
                transform.rotation = Quaternion.LookRotation(forward, up);

            }
            else
            {
                joint.anchor = m_anchor;
            }
            joint.connectedAnchor = hand.JointAnchor;
            joint.connectedBody = hand.Rigidbody;
            m_joints.Add(interactor, joint);
            transform.rotation = startRotation;
            LockJoint(joint);
            m_body.detectCollisions = true;
            m_body.isKinematic = wasKinematic;
            OnInteraction?.Invoke();
        }

        private Vector3 GetAxis(Vector3 direction, Vector3 handleDirection, bool canBeInverted)
        {
            var result = direction;
            if (canBeInverted)
            {
                result *= Mathf.Sign(Vector3.Dot(handleDirection, direction));
            }
            return result.normalized;
        }

        private void Update()
        {
            foreach (var hand in m_joints.Keys)
            {
                var joint = m_joints[hand];
                if (joint.currentForce.magnitude > m_maxJointForce || joint.currentTorque.magnitude > m_maxJointTorque)
                {
                    RemoveInteractor(hand);
                }
            }
        }

        private void RemoveInteractor(HandGrabInteractor interactor)
        {
            var physicalHandRef = interactor.GetComponent<PhysicalHandRef>();
            var hand = physicalHandRef.Hand;
            if (!m_joints.ContainsKey(interactor))
                return;
            var joint = m_joints[interactor];
            Destroy(joint);
            _ = m_joints.Remove(interactor);
            hand.EndInteraction();
            OnEndinteraction?.Invoke();
        }

        private ConfigurableJoint CreatePreConfiguredJoint()
        {
            var joint = m_body.gameObject.AddComponent<ConfigurableJoint>();
            joint.swapBodies = true;
            joint.axis = Vector3.forward;
            joint.enableCollision = false;
            joint.autoConfigureConnectedAnchor = false;
            return joint;
        }

        private void LockJoint(ConfigurableJoint joint)
        {
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
        }

        private void UnlockJoint(ConfigurableJoint joint)
        {
            joint.xMotion = ConfigurableJointMotion.Free;
            joint.yMotion = ConfigurableJointMotion.Free;
            joint.zMotion = ConfigurableJointMotion.Free;

            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;
        }

        void ITransformer.Initialize(IGrabbable grabbable)
        {
            //Intentionally left empty
        }

        void ITransformer.BeginTransform()
        {
            //Intentionally left empty
        }

        void ITransformer.UpdateTransform()
        {
            //Intentionally left empty
        }

        void ITransformer.EndTransform()
        {
            //Intentionally left empty
        }
    }
}
