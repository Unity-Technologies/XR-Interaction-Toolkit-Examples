using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BNG {

    [RequireComponent(typeof(HandPoser))]
    [ExecuteInEditMode]
    public class AutoPoser : MonoBehaviour {

        [Header("Auto Pose Settings")]
        [Tooltip("(Required) A HandPose in the fully open position. AutoPose will lerp each finger joint between OpenHandPose and ClosedHandPose until contact is made at each finger tip.")]
        public HandPose OpenHandPose;
        
        [Tooltip("(Required) A HandPose in the fully closed position. AutoPose will lerp each finger between OpenHandPose and ClosedHandPose until contact is made at each finger tip.")]
        public HandPose ClosedHandPose;

        [Header("Finger Tip Collision")]
        [Tooltip("Radius (in meters) of the fingertips to use when checking for collisions during auto-posing. Only used if a FingerTipCollider is not defined. (Default : 0.00875)")]
        [Range(0.0f, 0.02f)]
        public float FingerTipRadius = 0.00875f;

        [Tooltip("(Optional) Index Finger Offset - Use this to manually position and scale your finger tip collider")]
        public FingerTipCollider ThumbCollider;

        [Tooltip("(Optional) Index Finger Offset - Use this to manually position and scale your finger tip collider")]
        public FingerTipCollider IndexFingerCollider;
        
        [Tooltip("(Optional) Index Finger Offset - Use this to manually position and scale your finger tip collider")]
        public FingerTipCollider MiddleFingerCollider;
        
        [Tooltip("(Optional) Index Finger Offset - Use this to manually position and scale your finger tip collider")]
        public FingerTipCollider RingFingerCollider;
        
        [Tooltip("(Optional) Index Finger Offset - Use this to manually position and scale your finger tip collider")]
        public FingerTipCollider PinkyFingerCollider;

        [Header("Continuous Update")]
        [Tooltip("If true the hand will auto pose in Update(). Also works in the editor.")]
        public bool UpdateContinuously = false;

        [Tooltip("(Optional) The HandPose to use when UpdateContinuously = true and no collisions have been detected. If not specified, the hand will make a ClosedHandPose shape when no collisions have been found.")]
        public HandPose IdleHandPose;

        public LayerMask CollisionLayerMask = ~0;

        // Gizmo Customization
        [Header("Editor Gizmos")]
        public bool ShowGizmos = true;

        public GizmoDisplayType GizmoType = GizmoDisplayType.Wire;
        public Color GizmoColor = Color.white;

        public HandPoser InspectedPose;

        // Where our hand currently is this frame
        HandPoseDefinition currentPose;
        // Used to store temp poses and prevent GC
        HandPoseDefinition tempPose;

        // Where our hand would after checking for collisions
        public HandPoseDefinition CollisionPose {
            get {
                return collisionPose;
            }
        }
        HandPoseDefinition collisionPose;

        public bool CollisionDetected {
            get {
                return _thumbHit || _indexHit || _middleHit || _ringHit || _pinkyHit;
            }
        }

        #region private variables        
        private int  _count = 0;
        private bool _thumbHit = false;
        private bool _indexHit = false;
        private bool _middleHit = false;
        private bool _ringHit = false;
        private bool _pinkyHit = false;
        #endregion

        void Start() {
            if(InspectedPose == null) {
                InspectedPose = GetComponent<HandPoser>();
            }
        }

        void OnEnable() {
            if(Application.isEditor && OpenHandPose == null && ClosedHandPose == null) {
                // Try to auto fill open / closed hand pose
                OpenHandPose = (HandPose)Resources.Load("Default", typeof(HandPose));
                ClosedHandPose = (HandPose)Resources.Load("Fist", typeof(HandPose));
            }
        }

        // Update is called once per frame
        void Update() {
            // Auto Update Auto Pose
            if (UpdateContinuously) {

                bool useIdlePose = CollisionDetected == false && IdleHandPose != null && InspectedPose != null;
                if(useIdlePose) {
                    collisionPose = GetAutoPose();
                    InspectedPose.UpdateHandPose(IdleHandPose.Joints, true);
                }
                else {
                    UpdateAutoPose(true);
                }
            }
        }

        public virtual void UpdateAutoPose(bool lerp) {
            collisionPose = GetAutoPose();

            // Lerp to collision hand pose
            if (collisionPose != null) {
                InspectedPose.UpdateHandPose(collisionPose, lerp);
            }
        }

        /// <summary>
        /// Runs UpdateContinuously for one second
        /// </summary>
        public virtual void UpdateAutoPoseOnce() {
            StartCoroutine(updateAutoPoseRoutine());
        }

        IEnumerator updateAutoPoseRoutine() {
            
            UpdateContinuously = true;

            yield return new WaitForSeconds(0.2f);

            UpdateContinuously = false;
        }

        public HandPoseDefinition GetAutoPose() {
            if (InspectedPose == null || OpenHandPose == null || ClosedHandPose == null) {
                return null;
            }

            // Store our current hand state so we can snap back to it after we determine a collision
            currentPose = CopyHandDefinition(InspectedPose.GetHandPoseDefinition());

            // Start in open pose before 
            InspectedPose.UpdateHandPose(OpenHandPose.Joints, false);

            // Lerp between Open and Closed hand position, stopping if we hit something
            _count = 0;
            _thumbHit = false;
            _indexHit = false;
            _middleHit = false;
            _ringHit = false;
            _pinkyHit = false;

            while (_count < 300) {

                // Check Tip Collision
                if (!_thumbHit) { _thumbHit = GetThumbHit(InspectedPose); }
                if (!_indexHit) { _indexHit = GetIndexHit(InspectedPose); }
                if (!_middleHit) { _middleHit = GetMiddleHit(InspectedPose); }
                if (!_ringHit) { _ringHit = GetRingHit(InspectedPose); }
                if (!_pinkyHit) { _pinkyHit = GetPinkyHit(InspectedPose); }

                // Can bail if everything is touching
                if (_thumbHit && _indexHit && _middleHit && _ringHit && _pinkyHit) {
                    break;
                }

                _count++;
            }

            // Store the collision pose so we can return it after we've reset out hand back to it's original position
            tempPose = CopyHandDefinition(InspectedPose.GetHandPoseDefinition());

           

            // Switch back to the original hand pose we were in
            InspectedPose.UpdateHandPose(currentPose, false);

            return tempPose;
        }

        public HandPoseDefinition CopyHandDefinition(HandPoseDefinition ToCopy) {
            return new HandPoseDefinition() {
                IndexJoints = GetJointsCopy(ToCopy.IndexJoints),
                MiddleJoints = GetJointsCopy(ToCopy.MiddleJoints),
                OtherJoints = GetJointsCopy(ToCopy.OtherJoints),
                PinkyJoints = GetJointsCopy(ToCopy.PinkyJoints),
                RingJoints = GetJointsCopy(ToCopy.RingJoints),
                ThumbJoints = GetJointsCopy(ToCopy.ThumbJoints),
                WristJoint  = GetJointCopy(ToCopy.WristJoint),
            };
        }

        public FingerJoint GetJointCopy(FingerJoint ToClone) {
            
            // Null check
            if(ToClone == null) {
                return null;
            }

            return new FingerJoint() {
                LocalPosition = ToClone.LocalPosition,
                LocalRotation = ToClone.LocalRotation,
                TransformName = ToClone.TransformName
            };
        }

        public List<FingerJoint> GetJointsCopy(List<FingerJoint> ToClone) {
            List<FingerJoint> joints = new List<FingerJoint>();

            for (int x = 0; x < ToClone.Count; x++) {
                joints.Add(GetJointCopy(ToClone[x]));
            }

            return joints;
        }

        public bool GetThumbHit(HandPoser poser) {
            if (ThumbCollider != null) {
                return LoopThroughJoints(poser.ThumbJoints, ClosedHandPose.Joints.ThumbJoints, ThumbCollider.transform.position, ThumbCollider.Radius);
            }
            else {
                return LoopThroughJoints(poser.ThumbJoints, ClosedHandPose.Joints.ThumbJoints, HandPoser.GetFingerTipPositionWithOffset(poser.ThumbJoints, FingerTipRadius), FingerTipRadius);
            }
        }

        public bool GetIndexHit(HandPoser poser) {
            if (IndexFingerCollider != null) {
                return LoopThroughJoints(poser.IndexJoints, ClosedHandPose.Joints.IndexJoints, IndexFingerCollider.transform.position, IndexFingerCollider.Radius);
            }
            else {
                return LoopThroughJoints(poser.IndexJoints, ClosedHandPose.Joints.IndexJoints, HandPoser.GetFingerTipPositionWithOffset(poser.IndexJoints, FingerTipRadius), FingerTipRadius);
            }
        }

        public bool GetMiddleHit(HandPoser poser) {
            if (MiddleFingerCollider != null) {
                return LoopThroughJoints(poser.MiddleJoints, ClosedHandPose.Joints.MiddleJoints, MiddleFingerCollider.transform.position, MiddleFingerCollider.Radius);
            }
            else {
                return LoopThroughJoints(poser.MiddleJoints, ClosedHandPose.Joints.MiddleJoints, HandPoser.GetFingerTipPositionWithOffset(poser.MiddleJoints, FingerTipRadius), FingerTipRadius);
            }
        }

        public bool GetRingHit(HandPoser poser) {
            if (RingFingerCollider != null) {
                return LoopThroughJoints(poser.RingJoints, ClosedHandPose.Joints.RingJoints, RingFingerCollider.transform.position, RingFingerCollider.Radius);
            }
            else {
                return LoopThroughJoints(poser.RingJoints, ClosedHandPose.Joints.RingJoints, HandPoser.GetFingerTipPositionWithOffset(poser.RingJoints, FingerTipRadius), FingerTipRadius);
            }
        }

        public bool GetPinkyHit(HandPoser poser) {
            if (PinkyFingerCollider != null) {
                return LoopThroughJoints(poser.PinkyJoints, ClosedHandPose.Joints.PinkyJoints, PinkyFingerCollider.transform.position, PinkyFingerCollider.Radius);
            }
            else {
                return LoopThroughJoints(poser.PinkyJoints, ClosedHandPose.Joints.PinkyJoints, HandPoser.GetFingerTipPositionWithOffset(poser.PinkyJoints, FingerTipRadius), FingerTipRadius);
            }
        }

        public virtual bool LoopThroughJoints(List<Transform> fromJoints, List<FingerJoint> toJoints, Vector3 tipPosition, float tipRadius) {

            // Not a complete set
            if(fromJoints == null || toJoints == null || toJoints.Count == 0) {
                return false;
            }

            int fromLength = fromJoints.Count;
            int toLength = toJoints.Count;
            float movementAmount = 1f;

            for (int x = 0; x < fromLength; x++) {

                // move towards destination
                Transform thisJoint = fromJoints[x];

                // Bone count mismatch
                if(x >= toLength) {
                    return false;
                }

                thisJoint.localPosition = Vector3.MoveTowards(thisJoint.localPosition, toJoints[x].LocalPosition, movementAmount);
                thisJoint.localRotation = Quaternion.RotateTowards(thisJoint.localRotation, toJoints[x].LocalRotation, movementAmount);

                // Do Sphere cast at tip of finger
                bool isFingerTip = x == fromLength - 1;
                if (isFingerTip) {
                    Collider[] hitColliders = Physics.OverlapSphere(tipPosition, tipRadius, CollisionLayerMask, QueryTriggerInteraction.Ignore);
                    if (hitColliders != null && hitColliders.Length > 0) {
                        for(int y = 0; y < hitColliders.Length; y++) {
                            if(IsValidCollision(hitColliders[y])) {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// You can overrride this method to add your own collision validation logic
        /// </summary>
        public virtual bool IsValidCollision(Collider col) {

            // Ignore Triggers
            if (col == null || col.isTrigger) {
                return false;
            }

            // Default to a valid collisions
            return true;
        }

        #region EditorGizmos

        void OnDrawGizmos() {

            // Don't draw gizmos if this component has been disabled
            if(!this.isActiveAndEnabled) {
                return;
            }

            if(InspectedPose == null) {
                InspectedPose = GetComponent<HandPoser>();
            }

            if (ShowGizmos && InspectedPose != null) {
                Gizmos.color = GizmoColor;
                DrawJointGizmo(ThumbCollider, HandPoser.GetFingerTipPositionWithOffset(InspectedPose.ThumbJoints, FingerTipRadius), FingerTipRadius, GizmoType);
                DrawJointGizmo(IndexFingerCollider, HandPoser.GetFingerTipPositionWithOffset(InspectedPose.IndexJoints, FingerTipRadius), FingerTipRadius, GizmoType);
                DrawJointGizmo(MiddleFingerCollider, HandPoser.GetFingerTipPositionWithOffset(InspectedPose.MiddleJoints, FingerTipRadius), FingerTipRadius, GizmoType);
                DrawJointGizmo(RingFingerCollider, HandPoser.GetFingerTipPositionWithOffset(InspectedPose.RingJoints, FingerTipRadius), FingerTipRadius, GizmoType);
                DrawJointGizmo(PinkyFingerCollider, HandPoser.GetFingerTipPositionWithOffset(InspectedPose.PinkyJoints, FingerTipRadius), FingerTipRadius, GizmoType);
            }
        }

        public void DrawJointGizmo(FingerTipCollider tipCollider, Vector3 defaultPosition, float radius, GizmoDisplayType gizmoType) {
            if(tipCollider != null) {
                defaultPosition = tipCollider.transform.position;
                radius = tipCollider.Radius;
            }

            if (gizmoType == GizmoDisplayType.Wire) {
                Gizmos.DrawWireSphere(defaultPosition, radius);
            }
            else if (GizmoType == GizmoDisplayType.Solid) {
                Gizmos.DrawSphere(defaultPosition, radius);
            }
        }

        #endregion
    }

    public enum GizmoDisplayType {
        Wire,
        Solid,
        None
    }
}

