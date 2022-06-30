using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {

    public class HandPhysics : MonoBehaviour {

        /// <summary>
        /// This is the object our physical hand should try to follow / match
        /// </summary>
        [Tooltip("This is the object our physical hand should try to follow / match. Should typically be an object on the controller Transform")]
        public Transform AttachTo;

        [Tooltip("Amount of Velocity to apply to hands when trying to reach anchor point")]
        public float HandVelocity = 1500f;

        [Tooltip("If true, Hand COlliders will be disabled while grabbing an object")]
        public bool DisableHandCollidersOnGrab = true;

        [Tooltip("If the hand exceeds this distance from it's origin it will snap back to the original position. Specified in meters.")]
        public float SnapBackDistance = 1f;

        [Tooltip("This is the Grabber to use when this hand is active.")]
        public Grabber ThisGrabber;

        [Tooltip("Disable this Grabber when this hand is active. (Optional)")]
        public Grabber DisableGrabber;

        [Tooltip("This is the RemoteGrabber to use when this hand is active.")]
        public RemoteGrabber ThisRemoteGrabber;

        [Tooltip("Disable this RemoteGrabber when this hand is active. (Optional)")]
        public RemoteGrabber DisableRemoteGrabber;

        [Tooltip("Assign Hand Colliders this material if provided")]
        public PhysicMaterial ColliderMaterial;

        public Transform HandModel;
        public Transform HandModelOffset;

        public bool HoldingObject {
            get {
                return ThisGrabber != null && ThisGrabber.HeldGrabbable != null;
            }
        }

        // Colliders that live in the hand model
        List<Collider> handColliders;
        Rigidbody rigid;
        ConfigurableJoint configJoint;
        Grabbable heldGrabbable;

        List<Collider> collisions = new List<Collider>();
        LineRenderer line;

        Vector3 localHandOffset;
        Vector3 localHandOffsetRotation;

        bool wasHoldingObject = false;

        void Start() {

            rigid = GetComponent<Rigidbody>();
            configJoint = GetComponent<ConfigurableJoint>();
            line = GetComponent<LineRenderer>();

            // Create Attach Point based on current position and rotation
            if(AttachTo == null) {
                AttachTo = new GameObject("AttachToTransform").transform;
            }
            
            AttachTo.parent = transform.parent;
            AttachTo.SetPositionAndRotation(transform.position, transform.rotation);

            // Connect config joint to our AttachPoint's Rigidbody
            Rigidbody attachRB = AttachTo.gameObject.AddComponent<Rigidbody>();
            attachRB.useGravity = false;
            attachRB.isKinematic = true;
            attachRB.constraints = RigidbodyConstraints.FreezeAll;
            // configJoint.connectedBody = attachRB;
            Destroy(configJoint);

            localHandOffset = HandModel.localPosition;
            localHandOffsetRotation = HandModel.localEulerAngles;

            initHandColliders();

            _priorParent = transform.parent;
            // Physics Hands typically want to have no parent at all
            transform.parent = null;
        }

        void Update() {
            updateHandGraphics();

            // Line indicating our object is far away
            drawDistanceLine();

            // Check if we should ignore collision with an object that is being remotely pulled towards us
            checkRemoteCollision();

            // Check if hand has gotten too far away
            checkBreakDistance();

            // Our root object is disabled
            if (!AttachTo.gameObject.activeSelf) {
                transform.parent = AttachTo;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                return;
            }

            // If we are holding something, move the hands in Update, ignoring physics. 
            if (HoldingObject) {

                // Call On Grabbed Event if our first grab
                if (!wasHoldingObject) {
                    OnGrabbedObject(ThisGrabber.HeldGrabbable);
                }

                // If we are holding something, move the hands in Update, ignoring physics. 
                //transform.position = AttachTo.position;
                //transform.rotation = AttachTo.rotation;
            }
            else {
                if (wasHoldingObject) {
                    OnReleasedObject(heldGrabbable);
                }
            }

            wasHoldingObject = HoldingObject;
        }

        void FixedUpdate() {

            // Move object directly to our hand since the hand joint is controlling movement now
            if (HoldingObject && ThisGrabber.HeldGrabbable.DidParentHands) {
                rigid.MovePosition(AttachTo.position);
                rigid.MoveRotation(AttachTo.rotation);
            }
            else {
                // Move using Velocity
                Vector3 positionDelta = AttachTo.position - transform.position;
                rigid.velocity = Vector3.MoveTowards(rigid.velocity, (positionDelta * HandVelocity) * Time.fixedDeltaTime, 5f);

                // Rotate using angular velocity
                float angle;
                Vector3 axis;
                Quaternion rotationDelta = AttachTo.rotation * Quaternion.Inverse(transform.rotation);
                rotationDelta.ToAngleAxis(out angle, out axis);

                // Fix rotation angle
                if (angle > 180) {
                    angle -= 360;
                }

                if (angle != 0) {
                    Vector3 angularTarget = angle * axis;
                    angularTarget = (angularTarget * 60f) * Time.fixedDeltaTime;
                    rigid.angularVelocity = Vector3.MoveTowards(rigid.angularVelocity, angularTarget, 20f);
                }
            }

            // Reset Collisions every physics update
            collisions = new List<Collider>();
        }

        void initHandColliders() {
            handColliders = new List<Collider>();

            // Only accept non-trigger colliders.
            var tempColliders = GetComponentsInChildren<Collider>(false);
            for (int x = 0; x < tempColliders.Length; x++) {
                Collider c = tempColliders[x];
                if (!c.isTrigger && c.enabled) {
                    if (ColliderMaterial) {
                        c.material = ColliderMaterial;
                    }

                    handColliders.Add(c);
                }
            }

            // Ignore all other hand collider
            for (int x = 0; x < handColliders.Count; x++) {
                Collider thisCollider = handColliders[x];

                for (int y = 0; y < handColliders.Count; y++) {
                    Physics.IgnoreCollision(thisCollider, handColliders[y], true);
                }
            }
        }

        Grabbable remoteIgnoredGrabbable;
        void checkRemoteCollision() {
            // Should we unignore this object if we are no longer pulling it towards us?
            if(remoteIgnoredGrabbable != null && ThisGrabber.RemoteGrabbingGrabbable != remoteIgnoredGrabbable) {
                // If we are holding this object then let the settings take care of it
                if(ThisGrabber.HeldGrabbable == remoteIgnoredGrabbable) {
                    remoteIgnoredGrabbable = null;
                }
                // Otherwise we dropped it mid flight and should unignore it
                else {
                    IgnoreGrabbableCollisions(remoteIgnoredGrabbable, false);
                    remoteIgnoredGrabbable = null;
                }
            }

            // Ignore collision with object we started pulling towards us
            if(ThisGrabber.RemoteGrabbingGrabbable != null && ThisGrabber.RemoteGrabbingGrabbable != remoteIgnoredGrabbable) {
                remoteIgnoredGrabbable = ThisGrabber.RemoteGrabbingGrabbable;
                IgnoreGrabbableCollisions(remoteIgnoredGrabbable, true);
            }
        }

        // Line indicating our object is far away
        void drawDistanceLine() {
            if (line) {
                if (Vector3.Distance(transform.position, AttachTo.position) > 0.05f) {
                    line.enabled = true;
                    line.SetPosition(0, transform.position);
                    line.SetPosition(1, AttachTo.position);
                }
                else {
                    line.enabled = false;
                }
            }
        }        

        void checkBreakDistance() {
            if (SnapBackDistance > 0 && Vector3.Distance(transform.position, AttachTo.position) > SnapBackDistance) {
                transform.position = AttachTo.position;
            }
        }

        void updateHandGraphics() {

            bool holdingObject = ThisGrabber.HeldGrabbable != null;
            if (!holdingObject) {
                if (HandModelOffset) {
                    HandModelOffset.parent = HandModel;
                    HandModelOffset.localPosition = Vector3.zero;
                    HandModelOffset.localEulerAngles = Vector3.zero;
                }

                return;
            }

            // Position Hand Model
            if (HandModelOffset && ThisGrabber.HandsGraphics) {
                HandModelOffset.parent = ThisGrabber.HandsGraphics;
                HandModelOffset.localPosition = localHandOffset;
                HandModelOffset.localEulerAngles = localHandOffsetRotation;
            }
        }

        IEnumerator UnignoreAllCollisions() {

            var thisGrabbable = heldGrabbable;
            heldGrabbable = null;

            // Delay briefly so any held objects don't automatically clip
            yield return new WaitForSeconds(0.1f);

            IgnoreGrabbableCollisions(thisGrabbable, false);
        }

        public void IgnoreGrabbableCollisions(Grabbable grab, bool ignorePhysics) {

            var grabColliders = grab.GetComponentsInChildren<Collider>();

            // Ignore all other hand collider
            for (int x = 0; x < grabColliders.Length; x++) {
                Collider thisGrabCollider = grabColliders[x];

                for (int y = 0; y < handColliders.Count; y++) {
                    Physics.IgnoreCollision(thisGrabCollider, handColliders[y], ignorePhysics);
                }
            }
        }

        public void DisableHandColliders() {
            for (int x = 0; x < handColliders.Count; x++) {
                if(handColliders[x] != null && handColliders[x].enabled) {
                    handColliders[x].enabled = false;
                }
            }
        }

        public void EnableHandColliders() {
            for (int x = 0; x < handColliders.Count; x++) {
                if (handColliders[x] != null && handColliders[x].enabled == false) {
                    handColliders[x].enabled = true;
                }
            }
        }

        public virtual void OnGrabbedObject(Grabbable grabbedObject) {
            heldGrabbable = grabbedObject;

            if (DisableHandCollidersOnGrab) {
                DisableHandColliders();
            }
            // Make the hand ignore the grabbable's colliders
            else {

                IgnoreGrabbableCollisions(heldGrabbable, true);
            }
        }

        Transform _priorParent;

        public virtual void LockLocalPosition() {
            _priorParent = transform.parent;
            transform.parent = AttachTo;
        }

        public virtual void UnlockLocalPosition() {
            transform.parent = _priorParent;
        }

        public virtual void OnReleasedObject(Grabbable grabbedObject) {

            if (heldGrabbable != null) {
                // Make sure hand colliders come back
                if (DisableHandCollidersOnGrab) {
                    EnableHandColliders();
                }
                // Unignore the grabbable's colliders
                else {
                    StartCoroutine(UnignoreAllCollisions());
                }
            }

            heldGrabbable = null;
        }

        void OnEnable() {
            if(DisableGrabber) {
                DisableGrabber.enabled = false;
            }

            if (ThisGrabber) {
                ThisGrabber.enabled = true;
            }

            if (ThisRemoteGrabber) {
                ThisRemoteGrabber.enabled = true;
                DisableRemoteGrabber.enabled = false;
            }

            // Move events
            PlayerTeleport.OnBeforeTeleport += LockLocalPosition;
            PlayerTeleport.OnAfterTeleport += UnlockLocalPosition;

            PlayerRotation.OnBeforeRotate += LockLocalPosition;
            PlayerRotation.OnAfterRotate += UnlockLocalPosition;

            SmoothLocomotion.OnBeforeMove += LockOffset;
            SmoothLocomotion.OnAfterMove += UnlockOffset;
        }

        Vector3 _priorLocalOffsetPosition;

        public virtual void LockOffset() {
            _priorLocalOffsetPosition = AttachTo.InverseTransformPoint(transform.position);
        }

        public virtual void UnlockOffset() {
            Vector3 dest = AttachTo.TransformPoint(_priorLocalOffsetPosition);
            float dist = Vector3.Distance(transform.position, dest);
            // Only move if gone far enough
            if (dist > 0.0005f) {
                transform.position = dest;
            }
        }

        void OnDisable() {
            if (ThisGrabber) {
                ThisGrabber.enabled = false;
            }

            if (DisableGrabber) {
                DisableGrabber.enabled = true;
            }

            if (ThisRemoteGrabber) {
                ThisRemoteGrabber.enabled = false;
            }

            if (DisableRemoteGrabber) {
                DisableRemoteGrabber.enabled = true;
            }

            // Move events
            PlayerTeleport.OnBeforeTeleport -= LockLocalPosition;
            PlayerTeleport.OnAfterTeleport -= UnlockLocalPosition;

            PlayerRotation.OnBeforeRotate -= LockLocalPosition;
            PlayerRotation.OnAfterRotate -= UnlockLocalPosition;

            SmoothLocomotion.OnBeforeMove -= LockOffset;
            SmoothLocomotion.OnAfterMove -= UnlockOffset;
        }

        void OnCollisionStay(Collision collision) {
            for (int x = 0; x < collision.contacts.Length; x++) {
                ContactPoint contact = collision.contacts[x];
                // Keep track of how many objects we are colliding with
                if (IsValidCollision(contact.otherCollider) && !collisions.Contains(contact.otherCollider)) {
                    collisions.Add(contact.otherCollider);
                }
            }
        }

        public bool IsValidCollision(Collider col) {
            return true;
        }
    }
}