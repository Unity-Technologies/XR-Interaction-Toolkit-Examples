// Copyright (c) Meta Platforms, Inc. and affiliates.
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Serialization;
namespace NorthStar
{
    /// <summary>
    /// Script that handles moving the hand objects as physical objects
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicalHand : MonoBehaviour
    {
        [SerializeField] private Transform m_handAnchor;
        [SerializeField] protected HumanBodyBones m_bone;
        [SerializeField] private bool m_boundToParent;
        [SerializeField] private Rigidbody m_parent;
        private Vector3 m_lastBodyPosition;
        [SerializeField, Interface(typeof(IHand))] private Object m_handObject;
        private IHand m_hand;

        public HandColliders Colliders { get; private set; }

        public float LocalMovementStrengthModifier = 1;
        public float LocalRotationStrengthModifier = 1;

        private ConfigurableJoint m_joint, m_rotationJoint;
        public CriticallyDampendSpringJoint DampendSpringJoint;
        private bool m_connected;

        public Vector3 JointAnchor => m_jointAnchor;
        [SerializeField] private Vector3 m_jointAnchor;
        public Vector3 JointAnchorRotation => m_jointAnchorRotation;
        [SerializeField] private Vector3 m_jointAnchorRotation;
        [SerializeField] private HandGrabInteractor m_interactor;
        public Rigidbody Rigidbody { get; private set; }
        public Rigidbody WristBody { get; private set; }
        private float m_breakTimer;
        public float ExcessBreakTimer = 0;

        private void Awake()
        {
            m_joint = GetComponent<ConfigurableJoint>();
            m_rotationJoint = GetComponent<ConfigurableJoint>();// = transform.GetChild(0).GetComponent<ConfigurableJoint>();
            Colliders = GetComponent<HandColliders>();
            m_hand = m_handObject as IHand;
            Rigidbody = GetComponent<Rigidbody>();
            WristBody = m_rotationJoint.GetComponent<Rigidbody>();
        }

        private bool GetHandEnabled()
        {
            return m_hand.IsConnected && m_hand.IsHighConfidence && m_hand.IsTrackedDataValid;
        }

        private void OnHandConnected()
        {
            m_connected = true;
            Colliders.enabled = true;
            Rigidbody.position = m_handAnchor.position;
            Rigidbody.rotation = m_handAnchor.rotation;
            Rigidbody.isKinematic = false;
        }

        private void OnHandDisconnected()
        {
            m_connected = false;
            Colliders.enabled = false;
            Rigidbody.isKinematic = true;
        }

        public void StartInteraction()
        {
            Colliders.enabled = false;
        }

        public void EndInteraction()
        {
            Colliders.enabled = true;
        }

        private void UpdateDrive()
        {
            DampendSpringJoint.PositionSpring = LocalMovementStrengthModifier;
            DampendSpringJoint.RotationSpring = LocalRotationStrengthModifier;
        }

        private void OnValidate()
        {
            m_joint = GetComponent<ConfigurableJoint>();

            m_joint.connectedBody = m_boundToParent ? m_parent : null;
        }

        private void SyncPositions()
        {
            DampendSpringJoint.TargetPoint = m_boundToParent ? (m_handAnchor.position - m_parent.position) : m_handAnchor.position;
            DampendSpringJoint.TargetRotation = m_handAnchor.rotation;

            m_rotationJoint.targetRotation = m_handAnchor.rotation;
            if (GlobalSettings.PlayerSettings.MaxHandDistance != float.PositiveInfinity && Vector3.Distance(transform.position, m_handAnchor.position) > GlobalSettings.PlayerSettings.MaxHandDistance)
            {
                Rigidbody.position = m_handAnchor.position;
                transform.position = Rigidbody.position;
            }
            DampendSpringJoint.AddForce();
        }
        private void Update()
        {
            if ((!GlobalSettings.PlayerSettings.HandsReleaseOnInvalidPosition) || BodyPositions.Instance.IsHandWithinLimits(m_bone))
            {
                m_breakTimer -= Time.deltaTime;
            }
            else
            {
                m_breakTimer += Time.deltaTime;
            }
            m_breakTimer = Mathf.Clamp(m_breakTimer, 0.0f, GlobalSettings.PlayerSettings.HandBreakTimeout + ExcessBreakTimer);
            m_interactor.enabled = m_breakTimer < GlobalSettings.PlayerSettings.HandBreakTimeout + ExcessBreakTimer && m_connected;
            if (!GetHandEnabled())
            {
                Rigidbody.position = m_handAnchor.position;
                Rigidbody.rotation = m_handAnchor.rotation;
                if (m_connected)
                    OnHandDisconnected();
                return;
            }
            if (!m_connected)
                OnHandConnected();
        }

        public void ForceDropHeldItem()
        {
            if (!GlobalSettings.PlayerSettings.AllowForcedHandBreak)
                return;
            m_breakTimer = GlobalSettings.PlayerSettings.HandBreakTimeout + ExcessBreakTimer;
        }

        private void FixedUpdate()
        {
            UpdateDrive();
            SyncPositions();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + transform.rotation * m_jointAnchor, .01f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + transform.rotation * m_jointAnchor, transform.rotation * Quaternion.Euler(m_jointAnchorRotation) * Vector3.forward);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + transform.rotation * m_jointAnchor, transform.rotation * Quaternion.Euler(m_jointAnchorRotation) * Vector3.right);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position + transform.rotation * m_jointAnchor, transform.rotation * Quaternion.Euler(m_jointAnchorRotation) * Vector3.up);
        }
    }
}