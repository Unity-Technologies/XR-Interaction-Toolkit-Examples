using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class MovingPlatform : MonoBehaviour {
        [Tooltip("If set to ParentToPlatform the CharacterController will be parented to any MovingPlatform object below it each frame. If set to PositionDifference the movement will be read from the PositionDifference property of the MoveToWaypoint object below it. ")]
        public MovingPlatformMethod MovementMethod = MovingPlatformMethod.ParentToPlatform;

        [HideInInspector]
        public Vector3 PositionDelta;
        [HideInInspector]
        public Quaternion RotationDelta;

        protected Vector3 previousPosition;
        protected Quaternion previousRotation;

        protected void Update() {
            PositionDelta = transform.position - previousPosition;
            RotationDelta = transform.rotation * Quaternion.Inverse(previousRotation);

            previousPosition = transform.position;
            previousRotation = transform.rotation;
        }
    }

    public enum MovingPlatformMethod {
        ParentToPlatform,
        PositionDifference
    }
}

