using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class IKDummy : MonoBehaviour {

        public Transform ThisEyeBone;

        public Transform PlayerTransform;

        public Transform HeadFollow;
        public Transform RightHandFollow;
        public Transform LeftHandFollow;

        public Vector3 HandRotationOffset = Vector3.zero;

        Animator animator;
        Transform headBone;

        Transform leftHandDummy;
        Transform rightHandDummy;


        Transform leftHandOnPlayer;
        Transform rightHandOnPlayer;


        Transform lookatDummy;

        Vector3 localPos;
        Quaternion localRot;

        Transform cam;

        // Start is called before the first frame update
        void Start() {
            cam = Camera.main.transform;
            animator = GetComponent<Animator>();
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);

            if (PlayerTransform == null) {
                PlayerTransform = GameObject.FindGameObjectWithTag("Player").transform;
                HeadFollow = GameObject.Find("CenterEyeAnchor").transform;
                LeftHandFollow = GameObject.Find("LeftControllerAnchor").transform;
                RightHandFollow = GameObject.Find("RightControllerAnchor").transform;
            }

            leftHandDummy = SetParentAndLocalPosRot("leftDummy", transform);
            rightHandDummy = SetParentAndLocalPosRot("rightHandDummy", transform);

            leftHandOnPlayer = SetParentAndLocalPosRot("leftHandOnPlayer", PlayerTransform);
            rightHandOnPlayer = SetParentAndLocalPosRot("rightHandOnPlayer", PlayerTransform);

            lookatDummy = SetParentAndLocalPosRot("lookatDummy", transform);
        }

        public Transform SetParentAndLocalPosRot(string transformName, Transform parentToSet) {
            Transform theTransform = new GameObject(transformName).transform;
            theTransform.parent = parentToSet;
            theTransform.localPosition = Vector3.zero;
            theTransform.localRotation = Quaternion.identity;

            return theTransform;
        }

        Vector3 leftHandLocalPos, rightHandLocalPos;
        Quaternion leftHandLocalRot, rightHandLocalRot;

        // Update is called once per frame
        void LateUpdate() {

            if(HeadFollow == null || PlayerTransform == null) {
                return;
            }

            // Rotate body Y position to look at the player
            transform.LookAt(cam);
            transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);

            // Eye Helper
            lookatDummy.position = cam.position;
            lookatDummy.rotation = transform.rotation;

            // Left Hand
            leftHandOnPlayer.position = LeftHandFollow.position;
            leftHandOnPlayer.rotation = LeftHandFollow.rotation;

            rightHandOnPlayer.position = RightHandFollow.position;
            rightHandOnPlayer.rotation = RightHandFollow.rotation;

            leftHandDummy.localPosition = leftHandOnPlayer.localPosition;
            leftHandDummy.localRotation = leftHandOnPlayer.localRotation;

            rightHandDummy.localPosition = rightHandOnPlayer.localPosition;
            rightHandDummy.localRotation = rightHandOnPlayer.localRotation;


            leftHandLocalPos = leftHandDummy.localPosition;
            leftHandLocalRot = leftHandDummy.localRotation;

            rightHandLocalPos = rightHandDummy.localPosition;
            rightHandLocalRot = rightHandDummy.localRotation;
        }

        public Vector3 LeftHandsOffset = Vector3.zero;
        public Vector3 RightHandsOffset = Vector3.zero;

        void OnAnimatorIK() {
            if(animator == null) {
                return;
            }

            // Set weights for all IK goals
            animator.SetLookAtWeight(1);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);

            animator.SetLookAtPosition(cam.position);

            animator.SetIKPosition(AvatarIKGoal.LeftHand, ThisEyeBone.TransformPoint(leftHandLocalPos + LeftHandsOffset)) ;
            animator.SetIKRotation(AvatarIKGoal.LeftHand, transform.rotation * leftHandDummy.localRotation);

            animator.SetIKPosition(AvatarIKGoal.RightHand, ThisEyeBone.TransformPoint(rightHandLocalPos + RightHandsOffset));
            animator.SetIKRotation(AvatarIKGoal.RightHand, transform.rotation * rightHandDummy.localRotation);
        }
    }
}

