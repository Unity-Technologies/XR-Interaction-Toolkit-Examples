// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections;
using Oculus.Interaction.Input;
using UnityEngine;
//using UnityEngine.Animations.Rigging;

namespace NorthStar
{
    /// <summary>
    /// Manages logic and visuals relating to body tracking
    /// 
    /// A custom leg IK solution is used here to support seated and standing play with an offset rig
    /// </summary>
    [DefaultExecutionOrder(9999)] // Must be higher than FakeMovement to keep player upright
    public class BodyPositions : MonoBehaviour
    {
        //public bool BodyTrackingActive => OVRPlugin.bodyTrackingEnabled && m_body is not null && m_body.GetBodyTrackingCalibrationStatus() != OVRPlugin.BodyTrackingCalibrationState.Invalid;

        [SerializeField] private Animator m_animator;
        [SerializeField] public Transform Head;

        [SerializeField, Range(-1, 1)] private float m_breakCutoff = .5f;
        [SerializeField, Min(0)] private float m_distanceCutoff = 1.0f;
        [field: SerializeField] public Transform[] WristAnchors { get; private set; }
        [field: SerializeField] public SyntheticHand[] SyntheticHands { get; private set; }
        [field: SerializeField] public Transform[] HandVisuals { get; private set; }
        [field: SerializeField] public Transform CameraRig { get; private set; }

        public static BodyPositions Instance { get; private set; }

        public enum BodySide
        {
            Left,
            Right
        }

        public enum LegState
        {
            Planted,
            Stepping
        }

        [Serializable]
        public class LocomotionSettings
        {
            public float StepThreshold = 0.3f;
            public float StepDuration = 0.5f;
            public float StepHeight = 0.2f;
            public float StepPlantedPause = 0.25f;
            public float ExclusionRadius = 0.2f;
            public AnimationCurve StepHeightCurve;
        }

        [Serializable]
        public class LegSettings
        {
            public BodySide Side;
           // public TwoBoneIKConstraint Constraint;
            [NonSerialized] public LegState State;
            [NonSerialized] public float StepInterp;
            [NonSerialized] public Vector3 TargetPositionOffset;
            [NonSerialized] public Quaternion TargetRotationOffset;
            [NonSerialized] public Vector3 LastPlantedPosition;
            [NonSerialized] public Quaternion LastPlantedRotation;
        }

        [SerializeField] private LegSettings m_leftLegSettings;
        [SerializeField] private LegSettings m_rightLegSettings;
        [SerializeField] private LocomotionSettings m_locomotionSettings;

        private const float SHOULDER_SPAN_RATIO = .25f;
        private const float SHOULDER_HEIGHT_RATIO = 0.875f;
        private float m_elbowToWristLength;

        private Transform m_leftShoulderBone, m_rightShoulderBone;
       // private OVRBody m_body;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            //m_body = GetComponentInChildren<OVRBody>(); // Attempt to get the OVRBody for more tracking info
            m_leftShoulderBone = m_animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            m_rightShoulderBone = m_animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            m_elbowToWristLength = Vector3.Distance(m_animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position, m_animator.GetBoneTransform(HumanBodyBones.LeftHand).position);


          //  OVRManager.HMDMounted += OVRManager_HMDMounted;
        }

        private void OnDestroy()
        {
         //   OVRManager.HMDMounted -= OVRManager_HMDMounted;
        }

        private IEnumerator ResetTracking()
        {
            yield return new WaitForSeconds(1.0f);
           // m_body.enabled = false;
            yield return null;
          //  m_body.enabled = true;
            yield return null;
          //  FindObjectOfType<RetargetingAnimationConstraint>()?.RegenerateData();
        }

        
      
        private Vector3 EstimateShoulderPosition(bool right)
        {
            var toSide = Vector3.Scale(Head.right * (right ? 1 : -1), new Vector3(1, 0, 1)).normalized;
            var heightInUnits = GlobalSettings.PlayerSettings.Height / 100;
            var neckRoot = Head.TransformPoint(new Vector3(0, -heightInUnits * (1 - SHOULDER_HEIGHT_RATIO), 0));
            return neckRoot + toSide * (heightInUnits * (SHOULDER_SPAN_RATIO / 2));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(EstimateShoulderPosition(false), 0.05f);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(EstimateShoulderPosition(true), 0.05f);
        }
        
        public static Transform GetLeftHand()
        {
            return Instance.SyntheticHands[0].transform;
        }

        public static Transform GetRightHand()
        {
            return Instance.SyntheticHands[1].transform;
        }

        public Vector3 GetRelativeHandAngles(HumanBodyBones bodyBone)
        {
            var hand = m_animator.GetBoneTransform(bodyBone);
            var elbow = m_animator.GetBoneTransform(bodyBone == HumanBodyBones.LeftHand ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);

            return GetRelativeHandAngles(elbow, hand);
        }

        public Vector3 GetRelativeHandAngles(Transform elbow, Transform hand)
        {
            var twist = Vector3.SignedAngle(elbow.up, hand.up, elbow.right);
            var upDown = Vector3.SignedAngle(elbow.right, hand.right, hand.up);
            var sideToSide = Vector3.SignedAngle(elbow.up, hand.up, elbow.forward);

            return new Vector3(twist, upDown, sideToSide);
        }

        public bool IsHandWithinLimits(HumanBodyBones bodyBone)
        {
            if (bodyBone is not HumanBodyBones.LeftHand and not HumanBodyBones.RightHand) return false;
            Transform anchor;
            Transform hand;
            if (HumanBodyBones.LeftHand == bodyBone)
            {
                anchor = WristAnchors[0];
                hand = SyntheticHands[0].transform;
            }
            else
            {
                anchor = WristAnchors[1];
                hand = SyntheticHands[1].transform;
            }

            if (Vector3.Dot(anchor.forward, hand.forward) < m_breakCutoff)
                return false;
            if (Vector3.Dot(anchor.up, hand.up) < m_breakCutoff)
                return false;
            if (Vector3.Dot(anchor.right, hand.right) < m_breakCutoff)
                return false;
            if (Vector3.Distance(anchor.position, hand.position) > m_distanceCutoff)
                return false;
            
            
            var handBone = m_animator.GetBoneTransform(bodyBone);
            var elbow = m_animator.GetBoneTransform(bodyBone == HumanBodyBones.LeftHand ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);

            var elbowToWristLength = Vector3.Distance(handBone.position, elbow.position) / m_animator.transform.localScale.x;
            var stretch = elbowToWristLength - m_elbowToWristLength;

            return stretch < GlobalSettings.PlayerSettings.ArmStretchLimit;
        }
    }
}
