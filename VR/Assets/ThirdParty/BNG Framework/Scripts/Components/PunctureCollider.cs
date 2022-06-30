using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {

    public class PunctureCollider : MonoBehaviour {

        [Header("Puncture properties : ")]

        [Tooltip("Minimum distance (in meters) an object must be attached once punctured. Upon initial puncture the object will be inserted this distance from the puncture point.")]
        public float FRequiredPenetrationForce = 150f;

        [Tooltip("Minimum distance (in meters) an object must be attached once punctured. Upon initial puncture the object will be inserted this distance from the puncture point.")]
        public float MinPenetration = 0.01f;

        [Tooltip("Minimum distance the object can be penetrated (in meters).")]
        public float MaxPenetration = 0.2f;

        [Tooltip("How far away the object must be from it's entry point to consider breaking the joint. Set to 0 if you do not want to break the joint based on distance.")]
        public float BreakDistance = 0.2f;

        [Tooltip("How far away the object must be from it's entry point to consider breaking the joint. Set to 0 if you do not want to break the joint based on distance.")]
        public List<Collider> PunctureColliders;

        [Header("Shown for Debug : ")]
        [Tooltip("Is the object currently embedded in another object?")]
        public bool HasPunctured = false;

        [Tooltip("The object currently embedded in")]
        public GameObject PuncturedObject;

        [Tooltip("How far (in meters) our object has been embedded into")]
        public float PunctureValue;
        float previousPunctureValue;

        Collider col;
        Collider hitCollilder;
        Collider[] ignoreColliders;
        Rigidbody rigid;
        GameObject jointHelper;
        Rigidbody jointHelperRigid;
        ConfigurableJoint jointHelperJoint;
        Grabbable thisGrabbable;
        FixedJoint fj;

        // Used to store min / max puncture values
        float yPuncture, yPunctureMin, yPunctureMax;

        void Start() {
            col = GetComponent<Collider>();
            rigid = col.attachedRigidbody;
            ignoreColliders = GetComponentsInChildren<Collider>();
            thisGrabbable = GetComponent<Grabbable>();
        }

        public float TargetDistance;

        public void FixedUpdate() {
            UpdatePunctureValue();
            CheckBreakDistance();
            CheckPunctureRelease();
            AdjustJointMass();
            ApplyResistanceForce();

            if(jointHelperJoint) {
                TargetDistance = Vector3.Distance(jointHelperJoint.targetPosition, jointHelperJoint.transform.position);
            }
        }
                
        // Get distance of puncture and move up / down if possible
        public virtual void UpdatePunctureValue() {

            if (HasPunctured && PuncturedObject != null && jointHelper != null) {
                // How far away from the pouncture point we are on the Y axis
                PunctureValue = transform.InverseTransformVector(jointHelper.transform.position - PuncturedObject.transform.position).y * -1;
                if (PunctureValue > 0 && PunctureValue < 0.0001f) {
                    PunctureValue = 0;
                }
                if (PunctureValue < 0 && PunctureValue > -0.0001f) {
                    PunctureValue = 0;
                }

                if (PunctureValue > 0.001f) {
                    MovePunctureUp();
                }
                else if (PunctureValue < -0.001f) {
                    MovePunctureDown();
                }
            }
            else {
                PunctureValue = 0;
            }
        }

        public virtual void MovePunctureUp() {

            jointHelperJoint.autoConfigureConnectedAnchor = false;

            float updatedYValue = jointHelperJoint.connectedAnchor.y + (Time.deltaTime);

            // Set min / max
            if (updatedYValue > yPunctureMin) {
                updatedYValue = yPunctureMin;
            }
            else if (updatedYValue < yPunctureMax) {
                updatedYValue = yPunctureMax;
            }

            // Apply the changes
            jointHelperJoint.connectedAnchor = new Vector3(jointHelperJoint.connectedAnchor.x, updatedYValue, jointHelperJoint.connectedAnchor.z);
        }

        public virtual void MovePunctureDown() {
            jointHelperJoint.autoConfigureConnectedAnchor = false;

            float updatedYValue = jointHelperJoint.connectedAnchor.y - (Time.deltaTime);

            if (updatedYValue > yPunctureMin) {
                updatedYValue = yPunctureMin;
            }
            else if (updatedYValue < yPunctureMax) {
                updatedYValue = yPunctureMax;
            }

            // Apply the changes
            jointHelperJoint.connectedAnchor = new Vector3(jointHelperJoint.connectedAnchor.x, updatedYValue, jointHelperJoint.connectedAnchor.z);
        }

        public virtual void CheckBreakDistance() {
            if (BreakDistance != 0 && HasPunctured && PuncturedObject != null && jointHelper != null) {
                if (PunctureValue > BreakDistance) {
                    ReleasePuncture();
                }
            }
        }

        public virtual void CheckPunctureRelease() {
            // Did an object get updated?
            if (HasPunctured && (PuncturedObject == null || jointHelper == null)) {
                ReleasePuncture();
            }
        }

        public virtual void AdjustJointMass() {
            // If this is a grabbable object we can adjust the physics a bit to make this smoother while being held
            if (thisGrabbable != null && jointHelperJoint != null) {
                // If being held, fix the mass scale so our mass is greater
                if (HasPunctured && thisGrabbable.BeingHeld) {
                    jointHelperJoint.massScale = 1f;
                    jointHelperJoint.connectedMassScale = 0.0001f;
                }
                // Otherwise use the default
                else {
                    jointHelperJoint.massScale = 1f;
                    jointHelperJoint.connectedMassScale = 1f;
                }
            }
        }

        // Apply a resistance force to the object if currently inserted
        public virtual void ApplyResistanceForce() {
            if (HasPunctured) {

                // Currently only apply resistance if holding the object
                if(thisGrabbable != null && thisGrabbable.BeingHeld) {
                    float punctureDifference = previousPunctureValue - PunctureValue;
                    // Apply opposing force
                    if (punctureDifference != 0 && Mathf.Abs(punctureDifference) > 0.0001f) {
                        rigid.AddRelativeForce(rigid.transform.up * punctureDifference, ForceMode.VelocityChange);
                    }
                }

                // Store our previous puncture value so we can compare it later
                previousPunctureValue = PunctureValue;
            }
            else {
                previousPunctureValue = 0;
            }
        }

        public virtual void DoPuncture(Collider colliderHit, Vector3 connectPosition) {

            // Bail early if no rigidbody is present
            if(colliderHit == null || colliderHit.attachedRigidbody == null) {
                return;
            }

            hitCollilder = colliderHit;
            PuncturedObject = hitCollilder.attachedRigidbody.gameObject;

            // Ignore physics with this collider            
            for (int x = 0; x < ignoreColliders.Length; x++) {
                Physics.IgnoreCollision(ignoreColliders[x], hitCollilder, true);
            }

            // Set up the joint helpter
            if (jointHelper == null) {
                // Set up config joint
                jointHelper = new GameObject("JointHelper");
                jointHelper.transform.parent = null;
                jointHelperRigid = jointHelper.AddComponent<Rigidbody>();

                jointHelper.transform.position = PuncturedObject.transform.position;
                jointHelper.transform.rotation = transform.rotation;

                jointHelperJoint = jointHelper.AddComponent<ConfigurableJoint>();
                jointHelperJoint.connectedBody = rigid;
                jointHelperJoint.autoConfigureConnectedAnchor = true;

                jointHelperJoint.xMotion = ConfigurableJointMotion.Locked;
                jointHelperJoint.yMotion = ConfigurableJointMotion.Limited;
                jointHelperJoint.zMotion = ConfigurableJointMotion.Locked;

                jointHelperJoint.angularXMotion = ConfigurableJointMotion.Locked;
                jointHelperJoint.angularYMotion = ConfigurableJointMotion.Locked;
                jointHelperJoint.angularZMotion = ConfigurableJointMotion.Locked;

                // Set out current puncture state. This is our 0-based location
                yPuncture = jointHelperJoint.connectedAnchor.y;
                yPunctureMin = yPuncture - MinPenetration;
                yPunctureMax = yPuncture - MaxPenetration;

                // Start the object punctured in a bit
                SetPenetration(MinPenetration);
            }

            // Attach fixed joint to our helper
            fj = PuncturedObject.AddComponent<FixedJoint>();
            fj.connectedBody = jointHelperRigid;
            //fj.massScale = 1;
            //fj.connectedMassScale = 100;

            HasPunctured = true;
        }

        /// <summary>
        /// Set penetration amount between MinPenetration and MaxPenetration
        /// </summary>
        /// <param name="penetrationAmount"></param>
        public void SetPenetration(float penetrationAmount) {

            float minPenVal = yPuncture - MinPenetration;
            float maxPenVal = yPuncture - MaxPenetration;
            float currentPenVal = yPuncture - penetrationAmount;

            float formattedPenVal = Mathf.Clamp(currentPenVal, maxPenVal, minPenVal);

            if (jointHelperJoint != null && jointHelperJoint.connectedAnchor != null) {

                // Make sure we aren't still in auto config mode
                jointHelperJoint.autoConfigureConnectedAnchor = false;
                
                jointHelperJoint.connectedAnchor = new Vector3(jointHelperJoint.connectedAnchor.x, formattedPenVal, jointHelperJoint.connectedAnchor.z);
            }
        }

        public void ReleasePuncture() {

            if(HasPunctured) {

                // Unignore the colliders
                for (int x = 0; x < ignoreColliders.Length; x++) {
                    // Colliders may have changed, make sure they are still valid before unignoring
                    if(ignoreColliders[x] != null && hitCollilder != null) {
                        Physics.IgnoreCollision(ignoreColliders[x], hitCollilder, false);
                    }
                }

                // Disconnect the jointHelper
                if(jointHelperJoint) {
                    jointHelperJoint.connectedBody = null;
                    GameObject.Destroy(jointHelper);
                }

                if(fj) {
                    fj.connectedBody = null;
                }

                // Disconnect FixedJoint
                GameObject.Destroy(fj);
            }

            PuncturedObject = null;
            HasPunctured = false;
        }

        public virtual bool CanPunctureObject(GameObject go) {
            // Override this method if you have custom puncture logic
            Rigidbody rigid = go.GetComponent<Rigidbody>();

            // Don't currently support kinematic objects
            if(rigid != null && rigid.isKinematic) {
                return false;
            }

            // Don't support static objects since joint can't be moved
            if(go.isStatic) {
                return false;
            }

            return true;
        }

        void OnCollisionEnter(Collision collision) {

            ContactPoint contact = collision.contacts[0];
            Vector3 hitPosition = contact.point;
            Quaternion hitRotation = Quaternion.FromToRotation(Vector3.up, contact.normal);
            float collisionForce = collision.impulse.magnitude / Time.fixedDeltaTime;

            // Debug.Log("Collision Force : " + collisionForce);

            // Do puncture
            if (collisionForce > FRequiredPenetrationForce && CanPunctureObject(collision.collider.gameObject) && !HasPunctured) {
                DoPuncture(collision.collider, hitPosition);
            }
        }
    }
}

