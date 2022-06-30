using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// This Component allows a generic humanoid rig to follow the Player's controller's and HMD using Unity's IK system
    /// </summary>
    public class CharacterIK : MonoBehaviour {
        
        /// <summary>
        /// The Left Controller our Left Hand IK should track
        /// </summary>
        public Transform FollowLeftController;

        /// <summary>
        /// The Right Controller our Right Hand IK should track
        /// </summary>
        public Transform FollowRightController;

        public Transform FollowLeftFoot;
        public Transform FollowRightFoot;
        public Transform FollowHead;

        /// <summary>
        /// Character's IK Feet will move up to this position. In World Space.
        /// </summary>
        public float FootYPosition = 0;

        /// <summary>
        /// If false the IK layers will be deactivated
        /// </summary>
        public bool IKActive = true;

        /// <summary>
        /// Should the player's feet follow our given Y axis using IK
        /// </summary>
        public bool IKFeetActive = true;
        
        public bool HideHead = true;
        public bool HideLeftArm = false;
        public bool HideRightArm = false;
        public bool HideLeftHand = false;
        public bool HideRightHand = false;
        public bool HideLegs = false;

        /// <summary>
        /// The Hips joint of the Character. Used for hiding the legs by scaling the joint to 0
        /// </summary>
        public Transform HipsJoint;

        /// <summary>
        /// The player our Body will follow
        /// </summary>
        public CharacterController FollowPlayer;

        Transform headBone;
        Transform leftShoulderJoint;
        Transform rightShoulderJoint;
        Transform leftHandJoint;
        Transform rightHandJoint;

        Animator animator;

        public float HipOffset = 0;

        // Start is called before the first frame update
        void Start() {
            animator = GetComponent<Animator>();

            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            leftHandJoint = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightHandJoint = animator.GetBoneTransform(HumanBodyBones.RightHand);
            leftShoulderJoint = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            rightShoulderJoint = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
        }

        public Vector3 hideBoneScale = new Vector3(0.0001f, 0.0001f, 0.0001f);

        void Update() {

            // Hide Headbone
            if (headBone != null) {
                headBone.localScale = HideHead ? Vector3.zero : Vector3.one;
            }

            // Hide Left Arm
            if (leftShoulderJoint != null) {
                leftShoulderJoint.localScale = HideLeftArm ? hideBoneScale : Vector3.one;
            }
            // Hide Right Arm
            if (rightShoulderJoint != null) {
                rightShoulderJoint.localScale = HideRightArm ? hideBoneScale : Vector3.one;
            }

            // Hide Left Hand
            if (leftHandJoint != null) {
                leftHandJoint.localScale = HideLeftHand ? Vector3.zero : Vector3.one;
            }
            // Hide Right Hand
            if (rightHandJoint != null) {
                rightHandJoint.localScale = HideRightHand ? Vector3.zero : Vector3.one;
            }

            // Hide Legs
            if(HipsJoint) {
                HipsJoint.localScale = HideLegs ? Vector3.zero : Vector3.one;
            }

            Transform hipJoint = animator.GetBoneTransform(HumanBodyBones.RightShoulder);            
        }
               
        void OnAnimatorIK() {
            if (animator) {                

                //if the IK is active, set the position and rotation directly to the goal. 
                if (IKActive) {

                    // Head
                    if (FollowHead != null) {
                        animator.SetLookAtWeight(1);
                        animator.SetLookAtPosition(FollowHead.position);
                    }

                    // Left Hand
                    if (FollowLeftController != null) {
                        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                        animator.SetIKPosition(AvatarIKGoal.LeftHand, FollowLeftController.position);
                        animator.SetIKRotation(AvatarIKGoal.LeftHand, FollowLeftController.rotation);
                    }
                    // Right Hand
                    if (FollowRightController != null) {
                        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                        animator.SetIKPosition(AvatarIKGoal.RightHand, FollowRightController.position);
                        animator.SetIKRotation(AvatarIKGoal.RightHand, FollowRightController.rotation);
                    }

                    // Left Foot
                    if(IKFeetActive) {
                        // Left Foot
                        if (FollowLeftFoot != null) {
                            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);                            
                            animator.SetIKPosition(AvatarIKGoal.LeftFoot, new Vector3(FollowLeftFoot.position.x, FootYPosition, FollowLeftFoot.position.z));
                        }

                        // Right Foot
                        if (FollowRightFoot != null) {
                            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
                            animator.SetIKPosition(AvatarIKGoal.RightFoot, new Vector3(FollowRightFoot.position.x, FootYPosition, FollowRightFoot.position.z));
                        }
                        // Testing body IK
                        //animator.bodyPosition = new Vector3(animator.bodyPosition.x, animator.bodyPosition.y + HipOffset + FollowPlayer.height, animator.bodyPosition.z);
                    }
                    else {
                        // Left Foot
                        if (FollowLeftFoot != null) {
                            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
                        }

                        // Right Foot
                        if (FollowRightFoot != null) {
                            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
                        }
                    }
                }
                // IK not active, release weight for hands / head
                else {
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);

                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
                    animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);

                    animator.SetLookAtWeight(0);
                }
            }
        }
    }
}