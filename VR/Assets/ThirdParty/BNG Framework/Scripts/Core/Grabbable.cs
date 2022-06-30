using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BNG {
    
    /// <summary>
    /// An object that can be picked up by a Grabber
    /// </summary>
    public class Grabbable : MonoBehaviour {

        /// <summary>
        /// Is this object currently being held by a Grabber
        /// </summary>
        public bool BeingHeld = false;

        /// <summary>
        /// Is this object currently being held by more than one Grabber
        /// </summary>
        public bool BeingHeldWithTwoHands {
            get {
                if (heldByGrabbers != null && heldByGrabbers.Count > 1 && SecondaryGrabBehavior == OtherGrabBehavior.DualGrab) {
                    return true;
                }
                // Being Held and a defined SecondaryGrabbable is also being held
                else if (BeingHeld && SecondaryGrabbable != null && SecondaryGrabbable.BeingHeld == true) {
                    return true;
                }

                return false;
            }
        }

        List<Grabber> validGrabbers;

        /// <summary>
        /// The grabber that is currently holding us. Null if not being held
        /// </summary>        
        protected List<Grabber> heldByGrabbers;

        public List<Grabber> HeldByGrabbers {
            get {
                return heldByGrabbers;
            }
        }

        /// <summary>
        /// Save whether or not the RigidBody was kinematic on Start.
        /// </summary>
        protected bool wasKinematic;
        protected bool usedGravity;
        protected CollisionDetectionMode initialCollisionMode;
        protected RigidbodyInterpolation initialInterpolationMode;

        /// <summary>
        /// Is the object being pulled towards the Grabber
        /// </summary>
        public bool RemoteGrabbing {
            get {
                return remoteGrabbing;
            }
        }        

        protected bool remoteGrabbing;

        [Header("Grab Settings")]
        /// <summary>
        /// Configure which button is used to initiate the grab
        /// </summary>
        [Tooltip("Configure which button is used to initiate the grab")]
        public GrabButton GrabButton = GrabButton.Inherit;

        /// <summary>
        /// 'Inherit' will inherit this setting from the Grabber. 'Hold' requires the user to hold the GrabButton down. 'Toggle' will drop / release the Grabbable on button activation.
        /// </summary>
        [Tooltip("'Inherit' will inherit this setting from the Grabber. 'Hold' requires the user to hold the GrabButton down. 'Toggle' will drop / release the Grabbable on button activation.")]
        public HoldType Grabtype = HoldType.Inherit;

        /// <summary>
        /// Kinematic Physics locks the object in place on the hand / grabber. PhysicsJoint allows collisions with the environment.
        /// </summary>
        [Tooltip("Kinematic Physics locks the object in place on the hand / grabber. Physics Joint and Velocity types allow collisions with the environment.")]
        public GrabPhysics GrabPhysics = GrabPhysics.Velocity;

        /// <summary>
        /// Snap to a location or grab anywhere on the object
        /// </summary>
        [Tooltip("Snap to a location or grab anywhere on the object")]
        public GrabType GrabMechanic = GrabType.Precise;

        /// <summary>
        /// How fast to Lerp the object to the hand
        /// </summary>
        [Tooltip("How fast to Lerp the object to the hand")]
        public float GrabSpeed = 15f;

        /// <summary>
        /// Can the object be picked up from far away. Must be within RemoteGrabber Trigger
        /// </summary>
        [Header("Remote Grab")]
        [Tooltip("Can the object be picked up from far away. Must be within RemoteGrabber Trigger")]
        public bool RemoteGrabbable = false;

        public RemoteGrabMovement RemoteGrabMechanic = RemoteGrabMovement.Linear;

        /// <summary>
        /// Max Distance Object can be Remote Grabbed. Not applicable if RemoteGrabbable is false
        /// </summary>
        [Tooltip("Max Distance Object can be Remote Grabbed. Not applicable if RemoteGrabbable is false")]
        public float RemoteGrabDistance = 2f;

        /// <summary>
        /// Multiply controller's velocity times this when throwing
        /// </summary>
        [Header("Throwing")]
        [Tooltip("Multiply controller's velocity times this when throwing")]
        public float ThrowForceMultiplier = 2f;

        /// <summary>
        /// Multiply controller's angular velocity times this when throwing
        /// </summary>
        [Tooltip("Multiply controller's angular velocity times this when throwing")]
        public float ThrowForceMultiplierAngular = 1.5f; // Multiply Angular Velocity times this

        /// <summary>
        /// Drop the item if object's center travels this far from the Grabber's Center (in meters). Set to 0 to disable distance break.
        /// </summary>
        [Tooltip("Drop the item if object's center travels this far from the Grabber's Center (in meters). Set to 0 to disable distance break.")]
        public float BreakDistance = 0;

        /// <summary>
        /// Enabling this will hide the Transform specified in the Grabber's HandGraphics property
        /// </summary>
        [Header("Hand Options")]
        [Tooltip("Enabling this will hide the Transform specified in the Grabber's HandGraphics property")]
        public bool HideHandGraphics = false;

        /// <summary>
        ///  Parent this object to the hands for better stability.
        ///  Not recommended for child grabbers
        /// </summary>
        [Tooltip("Parent this object to the hands for instantaneous movement. Object will travel 1:1 with the controller but may have trouble detecting fast moving collisions.")]
        public bool ParentToHands = false;

        /// <summary>
        /// If true, the hand model will be attached to the grabbed object
        /// </summary>
        [Tooltip("If true, the hand model will be attached to the grabbed object. This separates it from a 1:1 match with the controller, but may look more realistic.")]
        public bool ParentHandModel = true;

        [Tooltip("If true, the hand model will snap to the nearest GrabPoint. Otherwise the hand model will stay with the Grabber.")]
        public bool SnapHandModel = true;

        /// <summary>
        /// Set to false to disable dropping. If false, will be permanently attached to whatever grabs this.
        /// </summary>
        [Header("Misc")]
        [Tooltip("Set to false to disable dropping. If false, will be permanently attached to whatever grabs this.")]
        public bool CanBeDropped = true;

        /// <summary>
        /// Can this object be snapped to snap zones? Set to false if you never want this to be snappable. Further filtering can be done on the SnapZones
        /// </summary>
        [Tooltip("Can this object be snapped to snap zones? Set to false if you never want this to be snappable. Further filtering can be done on the SnapZones")]
        public bool CanBeSnappedToSnapZone = true;

        [Tooltip("If true, the object will always have kinematic disabled when dropped, even if it was initially kinematic.")]
        public bool ForceDisableKinematicOnDrop = false;

        [Tooltip("If true, the object will instantly position / rotate to the grabber instead of using velocity / force. This will only happen if no collisions have recently occurred. When using this method, the Grabbable's Rigidbody willbe instantly rotated / moved, taking in to account the interpolation settings. May clip through objects if moving fast enough.")]
        public bool InstantMovement = false;

        [Tooltip("If true, all child colliders will be considered Grabbable. If false, you will need to add the 'GrabbableChild' component to any child colliders that you wish to also be considered grabbable.")]
        public bool MakeChildCollidersGrabbable = false;

        [Header("Default Hand Pose")]
        [Tooltip("A hand controller can read this value to determine how to animate when grabbing this object. 'AnimatorID' = specify an Animator ID to be set on the hand animator after grabbing this object. 'HandPose' = use a HandPose scriptable object. 'AutoPoseOnce' = DO an auto pose one time upon grabbing this object. 'AutoPoseContinuous' = Keep running attempting an autopose while grabbing this item.")]
        public HandPoseType handPoseType = HandPoseType.HandPose;
        protected HandPoseType initialHandPoseType;

        [Tooltip("If HandPoseType = 'HandPose', this HandPose object will be applied to the hand on pickup")]
        public HandPose SelectedHandPose;
        protected HandPose initialHandPose;

        /// <summary>
        /// Animator ID of the Hand Pose to use
        /// </summary>
        [Tooltip("This HandPose Id will be passed to the Hand Animator when equipped. You can add new hand poses in the HandPoseDefinitions.cs file.")]
        public HandPoseId CustomHandPose = HandPoseId.Default;
        protected HandPoseId initialHandPoseId;

        /// <summary>
        /// What to do if another grabber grabs this while equipped. DualGrab is currently unsupported.
        /// </summary>
        [Header("Two-Handed Grab Behavior")]
        [Tooltip("What to do if another grabber grabs this while equipped.")]
        public OtherGrabBehavior SecondaryGrabBehavior = OtherGrabBehavior.None;

        [Tooltip("How to behave when two hands are grabbing this object. LookAt = Have the primary Grabber 'LookAt' the secondary grabber. For example, holding a rifle in the right controller will have it rotate towards the left controller. AveragePositionRotation = Use a point and rotation in space that is half-way between both grabbers.")]
        public TwoHandedPositionType TwoHandedPosition = TwoHandedPositionType.Lerp;

        [Tooltip("How far to lerp between grabber positions. For example, 0.5 = halfway between the primary and secondary grabber. 0 = use the primary grabber's position, 1 = use the secondary grabber's position.")]
        [Range(0.0f, 1f)]
        public float TwoHandedPostionLerpAmount = 0.5f;

        [Tooltip("How to behave when two hands are grabbing this object. LookAt = Have the primary Grabber 'LookAt' the secondary grabber. For example, holding a rifle in the right controller will have it rotate towards the left controller. AveragePositionRotation = Use a point and rotation in space that is half-way between both grabbers.")]
        public TwoHandedRotationType TwoHandedRotation = TwoHandedRotationType.Slerp;
        
        [Tooltip("How far to lerp / slerp between grabber rotation. For example, 0.5 = halfway between the primary and secondary grabber. 0 = use the primary grabber's rotation, 1 = use the secondary grabber's position.")]
        [Range(0.0f, 1f)]
        public float TwoHandedRotationLerpAmount = 0.5f;

        [Tooltip("How to repond if you are holding an object with two hands, and then drop the primary grabber. For example, you may want to drop the object, transfer it to the second hand, or do nothing at all.")]
        public TwoHandedDropMechanic TwoHandedDropBehavior = TwoHandedDropMechanic.Drop;

        [Tooltip("Which vector to use when TwoHandedRotation = LookAtSecondary. Ex : Horizontal = A rifle type setup where you want to aim down the sites; Vertical = A melee type setup where the object is vertical")]
        public TwoHandedLookDirection TwoHandedLookVector = TwoHandedLookDirection.Horizontal;        

        [Tooltip("How quickly to Lerp towards the SecondaryGrabbable if TwoHandedGrabBehavior = LookAt")]
        public float SecondHandLookSpeed = 40f;

        [Header("Secondary Grabbale Object")]
        [Tooltip("If specified, this object will be used as a secondary grabbable instead of relying on grab points on this object. If 'TwoHandedGrabBehavior' is specified as LookAt, this is the object the grabber will be rotated towards. If 'TwoHandedGrabBehavior' is specified as AveragePositionRotation, this is the object the grabber use to calculate position.")]
        public Grabbable SecondaryGrabbable;        

        /// <summary>
        /// The Grabbable can only be grabbed if this grabbable is being held.
        /// Example : If you only want a weapon part to be grabbable if the weapon itself is being held.
        /// </summary>
        [Header("Grab Restrictions")]
        [Tooltip("The Grabbable can only be grabbed if this grabbable is being held. Example : If you only want a weapon part to be grabbable if the weapon itself is being held.")]
        public Grabbable OtherGrabbableMustBeGrabbed = null;

        [Header("Physics Joint Settings")]
        /// <summary>
        /// How much Spring Force to apply to the joint when something comes in contact with the grabbable
        /// A higher Spring Force will make the Grabbable more rigid
        /// </summary>
        [Tooltip("A higher Spring Force will make the Grabbable more rigid")]
        public float CollisionSpring = 3000;

        /// <summary>
        /// How much Slerp Force to apply to the joint when something is in contact with the grabbable
        /// </summary>
        [Tooltip("How much Slerp Force to apply to the joint when something is in contact with the grabbable")]
        public float CollisionSlerp = 500;

        [Tooltip("How to restrict the Configurable Joint's xMotion when colliding with an object. Position can be free, completely locked, or limited.")]
        public ConfigurableJointMotion CollisionLinearMotionX = ConfigurableJointMotion.Free;

        [Tooltip("How to restrict the Configurable Joint's yMotion when colliding with an object. Position can be free, completely locked, or limited.")]
        public ConfigurableJointMotion CollisionLinearMotionY = ConfigurableJointMotion.Free;

        [Tooltip("How to restrict the Configurable Joint's zMotion when colliding with an object. Position can be free, completely locked, or limited.")]
        public ConfigurableJointMotion CollisionLinearMotionZ = ConfigurableJointMotion.Free;

        [Tooltip("Restrict the rotation around the X axes to be Free, completely Locked, or Limited when colliding with an object.")]
        public ConfigurableJointMotion CollisionAngularMotionX = ConfigurableJointMotion.Free;

        [Tooltip("Restrict the rotation around the Y axes to be Free, completely Locked, or Limited when colliding with an object.")]
        public ConfigurableJointMotion CollisionAngularMotionY = ConfigurableJointMotion.Free;

        [Tooltip("Restrict the rotation around Z axes to be Free, completely Locked, or Limited when colliding with an object.")]
        public ConfigurableJointMotion CollisionAngularMotionZ = ConfigurableJointMotion.Free;


        [Tooltip("If true, the object's velocity will be adjusted to match the grabber. This is in addition to any forces added by the configurable joint.")]
        public bool ApplyCorrectiveForce = true;

        [Header("Velocity Grab Settings")]
        public float MoveVelocityForce = 3000f;
        public float MoveAngularVelocityForce = 90f;

        /// <summary>
        /// Time in seconds (Time.time) when we last grabbed this item
        /// </summary>
        [HideInInspector]
        public float LastGrabTime;

        /// <summary>
        /// Time in seconds (Time.time) when we last dropped this item
        /// </summary>
        [HideInInspector]
        public float LastDropTime;

        /// <summary>
        /// Set to True to throw the Grabbable by applying the controller velocity to the grabbable on drop. 
        /// Set False if you don't want the object to be throwable, or want to apply your own force manually
        /// </summary>
        [HideInInspector]
        public bool AddControllerVelocityOnDrop = true;

        // Total distance between the Grabber and Grabbable.
        float journeyLength;

        public Vector3 OriginalScale { get; private set; }

        // Keep track of objects that are colliding with us
        [Header("Shown for Debug : ")]
        [SerializeField]
        public List<Collider> collisions;

        // Last time in seconds (Time.time) since we had a valid collision
        public float lastCollisionSeconds { get; protected set; }

        /// <summary>
        /// How many seconds we've gone without collisions
        /// </summary>
        public float lastNoCollisionSeconds { get; protected set; }

        /// <summary>
        /// Have we recently collided with an object
        /// </summary>
        public bool RecentlyCollided { 
            get {
                if(Time.time - lastCollisionSeconds <= 0.1f) {
                    return true;
                }

                if(collisions != null && collisions.Count > 0) {
                    return true;
                }
                return false;
            } 
        }

        // If Time.time < requestSpringTime, force joint to be springy
        public float requestSpringTime { get; protected set; }

        /// <summary>
        /// If Grab Mechanic is set to Snap, set position and rotation to this Transform on the primary Grabber
        /// </summary>
        protected Transform primaryGrabOffset;
        protected Transform secondaryGrabOffset;

        /// <summary>
        /// Returns the active GrabPoint component if object is held and a GrabPoint has been assigneed
        /// </summary>
        [HideInInspector]
        public GrabPoint ActiveGrabPoint;        

        [HideInInspector]
        public Vector3 SecondaryLookOffset;

        [HideInInspector]
        public Transform SecondaryLookAtTransform;

        [HideInInspector]
        public Transform LocalOffsetTransform;

        Vector3 grabPosition {
            get {
                if (primaryGrabOffset != null) {
                    return primaryGrabOffset.position;
                }
                else {
                    return transform.position;
                }
            }
        }

        [HideInInspector]
        public Vector3 GrabPositionOffset {
            get {
                if (primaryGrabOffset) {
                    return primaryGrabOffset.transform.localPosition;
                }

                return Vector3.zero;
            }
        }

        [HideInInspector]
        public Vector3 GrabRotationOffset {
            get {
                if (primaryGrabOffset) {
                    return primaryGrabOffset.transform.localEulerAngles;
                }
                return Vector3.zero;
            }
        }

        private Transform _grabTransform;

        // Position this on the grabber to get a precise location
        public Transform grabTransform {
            get {
                if (_grabTransform != null) {
                    return _grabTransform;
                }

                _grabTransform = new GameObject().transform;
                _grabTransform.parent = this.transform;
                _grabTransform.name = "Grab Transform";
                _grabTransform.localPosition = Vector3.zero;
                // _grabTransform.hideFlags = HideFlags.HideInHierarchy;

                return _grabTransform;
            }
        }

        private Transform _grabTransformSecondary;

        // Position this on the grabber to get a precise location
        public Transform grabTransformSecondary {
            get {
                if (_grabTransformSecondary != null) {
                    return _grabTransformSecondary;
                }

                _grabTransformSecondary = new GameObject().transform;
                _grabTransformSecondary.parent = this.transform;
                _grabTransformSecondary.name = "Grab Transform Secondary";
                _grabTransformSecondary.localPosition = Vector3.zero;
                _grabTransformSecondary.hideFlags = HideFlags.HideInHierarchy;

                return _grabTransformSecondary;
            }
        }

        [Header("Grab Points")]
        /// <summary>
        /// If Grab Mechanic is set to Snap, the closest GrabPoint will be used. Add a SnapPoint Component to a GrabPoint to specify custom hand poses and rotation.
        /// </summary>
        [Tooltip("If Grab Mechanic is set to Snap, the closest GrabPoint will be used. Add a SnapPoint Component to a GrabPoint to specify custom hand poses and rotation.")]
        public List<Transform> GrabPoints;

        /// <summary>
        /// Can the object be moved towards a Grabber. 
        /// Levers, buttons, doorknobs, and other types of objects cannot be moved because they are attached to another object or are static.
        /// </summary>
        public bool CanBeMoved {
            get {
                return _canBeMoved;
            }
        }
        private bool _canBeMoved;

        protected Transform originalParent;
        protected InputBridge input;
        protected ConfigurableJoint connectedJoint;
        protected Vector3 previousPosition;
        protected float lastItemTeleportTime;
        protected bool recentlyTeleported;

        /// <summary>
        /// Set this to false if you need to see Debug field or don't want to use the custom inspector
        /// </summary>
        [HideInInspector]
        public bool UseCustomInspector = true;

        /// <summary>
        /// If a BNGPlayerController is provided we can check for player movements and make certain adjustments to physics.
        /// </summary>
        protected BNGPlayerController player {
            get {
                return GetBNGPlayerController();
            }
        }
        private BNGPlayerController _player;
        protected Collider col;
        protected Rigidbody rigid;

        public Grabber FlyingToGrabber {
            get {
                return flyingTo;
            }
        }
        protected Grabber flyingTo;

        protected List<GrabbableEvents> events;

        public bool DidParentHands {
            get {
                return didParentHands;
            }
        }
        protected bool didParentHands = false;

        protected void Awake() {
            col = GetComponent<Collider>();
            rigid = GetComponent<Rigidbody>();
            input = InputBridge.Instance;

            events = GetComponents<GrabbableEvents>().ToList();
            collisions = new List<Collider>();

            // Try parent if no rigid found here
            if (rigid == null && transform.parent != null) {
                rigid = transform.parent.GetComponent<Rigidbody>();
            }

            // Store initial rigidbody properties so we can reset them later as needed
            if (rigid) {
                initialCollisionMode = rigid.collisionDetectionMode;
                initialInterpolationMode = rigid.interpolation;
                wasKinematic = rigid.isKinematic;
                usedGravity = rigid.useGravity;

                // Allow our rigidbody to rotate quickly
                rigid.maxAngularVelocity = 25f;
            }

            // Store initial parent so we can reset later if needed
            UpdateOriginalParent(transform.parent);

            validGrabbers = new List<Grabber>();

            // Set Original Scale based in World coordinates if available
            if (transform.parent != null) {
                OriginalScale = transform.parent.TransformVector(transform.localScale);
            }
            else {
                OriginalScale = transform.localScale;
            }

            initialHandPoseId = CustomHandPose;
            initialHandPose = SelectedHandPose;
            initialHandPoseType = handPoseType;

            // Store movement status
            _canBeMoved = canBeMoved();

            // Set up any Child Grabbable Objects
            if(MakeChildCollidersGrabbable) {
                Collider[] cols = GetComponentsInChildren<Collider>();
                for(int x = 0; x < cols.Length; x++) {
                    // Make child Grabbable if it isn't already
                    if (cols[x].GetComponent<Grabbable>() == null && cols[x].GetComponent<GrabbableChild>() == null) {
                        var gc = cols[x].gameObject.AddComponent<GrabbableChild>();
                        gc.ParentGrabbable = this;
                    }
                }
            }
        }        

        public virtual void Update() {

            if (BeingHeld) {

                // ResetLockResets();

                // Something happened to our Grabber. Drop the item
                if (heldByGrabbers == null) {
                    DropItem(null, true, true);
                    return;
                }

                // Make sure all collisions are valid
                filterCollisions();

                // Cache PrimaryGrabber designation
                _priorPrimaryGrabber = GetPrimaryGrabber();

                // Update collision time
                if (collisions != null && collisions.Count > 0) {
                    lastCollisionSeconds = Time.time;
                    lastNoCollisionSeconds = 0;
                }
                else if (collisions != null && collisions.Count <= 0) {
                    lastNoCollisionSeconds += Time.deltaTime;
                }

                // Update item recently teleported time
                if (Vector3.Distance(transform.position, previousPosition) > 0.1f) {
                    lastItemTeleportTime = Time.time;
                }
                recentlyTeleported = Time.time - lastItemTeleportTime < 0.2f;

                // Loop through held grabbers and see if we need to drop the item, fire off events, etc.
                for (int x = 0; x < heldByGrabbers.Count; x++) {
                    Grabber g = heldByGrabbers[x];

                    // Should we drop the item if it's too far away?
                    if (!recentlyTeleported && BreakDistance > 0 && Vector3.Distance(grabPosition, g.transform.position) > BreakDistance) {
                        Debug.Log("Break Distance Exceeded. Dropping item.");
                        DropItem(g, true, true);
                        break;
                    }

                    // Should we drop the item if no longer holding the required Grabbable?
                    if (OtherGrabbableMustBeGrabbed != null && !OtherGrabbableMustBeGrabbed.BeingHeld) {
                        // Fixed joints work ok. Configurable Joints have issues
                        if (GetComponent<ConfigurableJoint>() != null) {
                            DropItem(g, true, true);
                            break;
                        }
                    }

                    // Fire off any relevant events
                    callEvents(g);
                }

                // Check to parent the hand models to the Grabbable
                if(ParentHandModel && !didParentHands) {
                    checkParentHands(GetPrimaryGrabber());
                }

                // Position Hands in proper place
                positionHandGraphics(GetPrimaryGrabber());

                // Rotate the grabber to look at our secondary object
                // JPTODO : Move this to physics updates
                if(TwoHandedRotation == TwoHandedRotationType.LookAtSecondary && GrabPhysics == GrabPhysics.PhysicsJoint) {
                    checkSecondaryLook();
                }

                // Keep track of where we were each frame
                previousPosition = transform.position;
            }
        }        

        public virtual void FixedUpdate() {

            if (remoteGrabbing) {
                UpdateRemoteGrab();
            }

            if (BeingHeld) {

                // Reset all collisions every physics update
                // These are then populated in OnCollisionEnter / OnCollisionStay to make sure we have the most up to date collision info
                // This can create garbage so only do this if we are holding the object
                if (BeingHeld && collisions != null && collisions.Count > 0) {
                    collisions = new List<Collider>();
                }

                // Update any physics properties here
                if (GrabPhysics == GrabPhysics.PhysicsJoint) {
                    UpdatePhysicsJoints();
                }
                else if (GrabPhysics == GrabPhysics.FixedJoint) {
                    UpdateFixedJoints();
                }
                else if (GrabPhysics == GrabPhysics.Kinematic) {
                    UpdateKinematicPhysics();
                }
                else if (GrabPhysics == GrabPhysics.Velocity) {
                    UpdateVelocityPhysics();
                }
            }
        }        

        public virtual Vector3 GetGrabberWithGrabPointOffset(Grabber grabber, Transform grabPoint) {
            // Sanity check
            if(grabber == null) {
                return Vector3.zero;
            }

            // Get the Grabber's position, offset by a grab point
            Vector3 grabberPosition = grabber.transform.position;
            if (grabPoint != null) {
                grabberPosition += transform.position - grabPoint.position;
            }

            return grabberPosition;

        }

        public virtual Quaternion GetGrabberWithOffsetWorldRotation(Grabber grabber) {

            if(grabber != null) {
                return grabber.transform.rotation;
            }

            return Quaternion.identity;
        }

        protected void positionHandGraphics(Grabber g) {
            if (ParentHandModel && didParentHands) {
                if (GrabMechanic == GrabType.Snap) {                    
                    if(g != null) {
                        g.HandsGraphics.localPosition = g.handsGraphicsGrabberOffset;
                        g.HandsGraphics.localEulerAngles = Vector3.zero;
                    }
                }
            }
        }

        /// <summary>
        /// Is this object able to be grabbed. Does not check for valid Grabbers, only if it isn't being held, is active, etc.
        /// </summary>
        /// <returns></returns>
        public virtual bool IsGrabbable() {

            // Not valid if not active
            if (!isActiveAndEnabled) {
                return false;
            }

            // Not valid if being held and the object has no secondary grab behavior
            if (BeingHeld == true && SecondaryGrabBehavior == OtherGrabBehavior.None) {
                return false;
            }

            // Not Grabbable if set as DualGrab, but secondary grabbable has been specified. This means we can't use a grab point on this object
            if (BeingHeld == true && SecondaryGrabBehavior == OtherGrabBehavior.DualGrab && SecondaryGrabbable != null) {
                return false;
            }

            // Make sure grabbed conditions are met
            if (OtherGrabbableMustBeGrabbed != null && !OtherGrabbableMustBeGrabbed.BeingHeld) {
                return false;
            }

            return true;
        }

        public virtual void UpdateRemoteGrab() {
            
            // Linear Movement
            if(RemoteGrabMechanic == RemoteGrabMovement.Linear) {
                CheckRemoteGrabLinear();
            }
            else if (RemoteGrabMechanic == RemoteGrabMovement.Velocity) {
                CheckRemoteGrabVelocity();
            }
            else if (RemoteGrabMechanic == RemoteGrabMovement.Flick) {
                CheckRemoteGrabFlick();
            }
        }

        public virtual void CheckRemoteGrabLinear() {
            // Bail early if we're not remote grabbing this item
            if (!remoteGrabbing) {
                return;
            }

            // Move the object linearly as a kinematic rigidbody
            if (rigid && !rigid.isKinematic) {
                rigid.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rigid.isKinematic = true;
            }

            Vector3 grabberPosition = GetGrabberWithGrabPointOffset(flyingTo, GetClosestGrabPoint(flyingTo));
            Quaternion remoteRotation = getRemoteRotation(flyingTo);
            float distance = Vector3.Distance(transform.position, grabberPosition);

            // reached destination, snap to final transform position
            // Typically this won't be hit as the Grabber trigger will pick it up first
            if (distance <= 0.002f) {
                movePosition(grabberPosition);
                moveRotation(grabTransform.rotation);

                if (rigid) {
                    rigid.velocity = Vector3.zero;
                }

                if (flyingTo != null) {
                    flyingTo.GrabGrabbable(this);
                }
            }
            // Getting close so speed up
            else if (distance < 0.03f) {
                movePosition(Vector3.MoveTowards(transform.position, grabberPosition, Time.fixedDeltaTime * GrabSpeed * 2f));
                moveRotation(Quaternion.Slerp(transform.rotation, remoteRotation, Time.fixedDeltaTime * GrabSpeed * 2f));
            }
            // Normal Lerp
            else {
                movePosition(Vector3.Lerp(transform.position, grabberPosition, Time.fixedDeltaTime * GrabSpeed));
                moveRotation(Quaternion.Slerp(transform.rotation, remoteRotation, Time.fixedDeltaTime * GrabSpeed));
            }
        }

        public virtual void CheckRemoteGrabVelocity() {
            if (remoteGrabbing) {

                Vector3 grabberPosition = GetGrabberWithGrabPointOffset(flyingTo, GetClosestGrabPoint(flyingTo));
                Quaternion remoteRotation = getRemoteRotation(flyingTo);
                float distance = Vector3.Distance(transform.position, grabberPosition);

                // Move the object with velocity, without using gravity
                if (rigid && rigid.useGravity) {
                    rigid.useGravity = false;

                    // Snap rotation once
                    // transform.rotation = remoteRotation;
                }

                // reached destination, snap to final transform position
                // Typically this won't be hit as the Grabber trigger will pick it up first
                if (distance <= 0.0025f) {
                    movePosition(grabberPosition);
                    moveRotation(grabTransform.rotation);

                    if (rigid) {
                        rigid.velocity = Vector3.zero;
                    }

                    if (flyingTo != null) {
                        flyingTo.GrabGrabbable(this);
                    }
                }
                else {
                    // Move with velocity
                    Vector3 positionDelta = grabberPosition - transform.position;

                    // Move towards hand using velocity
                    rigid.velocity = Vector3.MoveTowards(rigid.velocity, (positionDelta * MoveVelocityForce) * Time.fixedDeltaTime, 1f);

                    rigid.MoveRotation(Quaternion.Slerp(rigid.rotation, GetGrabbersAveragedRotation(), Time.fixedDeltaTime * GrabSpeed));
                    //rigid.angularVelocity = Vector3.zero;
                    //moveRotation(Quaternion.Slerp(transform.rotation, remoteRotation, Time.fixedDeltaTime * GrabSpeed));
                }
            }
        }


        bool initiatedFlick = false;
        // Angular Velocity required to start the flick force
        float flickStartVelocity = 1.5f;

        /// <summary>
        /// How long in seconds the object should take to jump to the grabber when using the Flick remote grab type
        /// </summary>
        float FlickSpeed = 0.5f;

        public float lastFlickTime;

        public virtual void InitiateFlick() {

            initiatedFlick = true;

            lastFlickTime = Time.time;

            Vector3 grabberPosition = flyingTo.transform.position;// GetGrabberWithGrabPointOffset(flyingTo, GetClosestGrabPoint(flyingTo));
            Quaternion remoteRotation = getRemoteRotation(flyingTo);
            float distance = Vector3.Distance(transform.position, grabberPosition);

            // Defauult to 1, but speed up if close
            float timeToGrab = FlickSpeed;
            if (distance < 1f) {
                timeToGrab = FlickSpeed / 1.5f;
            }
            else if (distance < 0.5f) {
                timeToGrab = FlickSpeed / 3;
            }

            Vector3 vel = GetVelocityToHitTargetByTime(transform.position, grabberPosition, Physics.gravity * 1.1f, timeToGrab);

            rigid.velocity = vel;
            // rigid.AddForce(vel, ForceMode.VelocityChange);

            // No longer initiated flick
            initiatedFlick = false;
        }

        public Vector3 GetVelocityToHitTargetByTime(Vector3 startPosition, Vector3 targetPosition, Vector3 gravityBase, float timeToTarget) {

            Vector3 direction = targetPosition - startPosition;
            Vector3 horizontal = Vector3.Project(direction, Vector3.Cross(gravityBase, Vector3.Cross(direction, gravityBase)));
            
            float horizontalDistance = horizontal.magnitude;
            float horizontalSpeed = horizontalDistance / timeToTarget;

            Vector3 vertical = Vector3.Project(direction, gravityBase);
            float verticalDistance = vertical.magnitude * Mathf.Sign(Vector3.Dot(vertical, -gravityBase));
            float verticalSpeed = (verticalDistance + ((0.5f * gravityBase.magnitude) * (timeToTarget * timeToTarget))) / timeToTarget;

            return (horizontal.normalized * horizontalSpeed) - (gravityBase.normalized * verticalSpeed);
        }

        public virtual void CheckRemoteGrabFlick() {
            if(remoteGrabbing) {

                // Have we initiated a flick yet?
                if(!initiatedFlick) {
                    // Get angular velocity from controller
                    if(InputBridge.Instance.GetControllerAngularVelocity(flyingTo.HandSide).magnitude >= flickStartVelocity) {
                        // Must be at least some time between flicks
                        if(Time.time - lastFlickTime >= 0.1f) {
                            InitiateFlick();
                        }
                    }
                }
            }
            else {
                initiatedFlick = false;
            }
        }

        public float FlickForce = 1f;

        public virtual void UpdateFixedJoints() {
            // Set to continuous dynamic while being held
            if (rigid != null && rigid.isKinematic) {
                rigid.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }
            else {
                rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            // Adjust item velocity. This smooths out forces while becoming rigid
            if (ApplyCorrectiveForce) {
                moveWithVelocity();
            }           
        }

        public virtual void UpdatePhysicsJoints() {

            // Bail if no joint connected
            if (connectedJoint == null || rigid == null) {
                return;
            }

            // Set to continuous dynamic while being held
            if (rigid.isKinematic) {
                rigid.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }
            else {
                rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            // Update Joint poisition in real time
            if (GrabMechanic == GrabType.Snap) {
                connectedJoint.anchor = Vector3.zero;
                connectedJoint.connectedAnchor = GrabPositionOffset;
            }

            // Check if something is requesting a springy joint
            // For example, a gun may wish to make the joint springy in order to apply recoil to a weapon via AddForce
            bool forceSpring = Time.time < requestSpringTime;

            // Only snap to a rigid grip if it's been a short delay after our last collision
            // This prevents the joint from rapidly becoming stiff / springy which will cause jittery behaviour
            bool afterCollision = collisions.Count == 0 && lastNoCollisionSeconds >= 0.1f;

            // Nothing touching it so we can stick to hand rigidly
            // Two-Handed weapons currently react much more smoothly if the joint is rigid, due to how the LookAt system works
            if ((BeingHeldWithTwoHands || afterCollision) && !forceSpring) {
                // Lock Angular, XYZ Motion
                // Make joint very rigid
                connectedJoint.rotationDriveMode = RotationDriveMode.Slerp;
                connectedJoint.xMotion = ConfigurableJointMotion.Limited;
                connectedJoint.yMotion = ConfigurableJointMotion.Limited;
                connectedJoint.zMotion = ConfigurableJointMotion.Limited;
                connectedJoint.angularXMotion = ConfigurableJointMotion.Limited;
                connectedJoint.angularYMotion = ConfigurableJointMotion.Limited;
                connectedJoint.angularZMotion = ConfigurableJointMotion.Limited;

                SoftJointLimit sjl = connectedJoint.linearLimit;
                sjl.limit = 15f;

                SoftJointLimitSpring sjlsp = connectedJoint.linearLimitSpring;
                sjlsp.spring = 3000;
                sjlsp.damper = 10f;

                // Set X,Y, and Z drive to our values
                // Set X,Y, and Z drive to our values
                setPositionSpring(CollisionSpring, 10f);

                // Slerp drive used for rotation
                setSlerpDrive(CollisionSlerp, 10f);

                // Adjust item velocity. This smooths out forces while becoming rigid
                if (ApplyCorrectiveForce) {
                    moveWithVelocity();
                }
            }
            else {
                // Make Springy
                connectedJoint.rotationDriveMode = RotationDriveMode.Slerp;
                connectedJoint.xMotion = CollisionLinearMotionX;
                connectedJoint.yMotion = CollisionLinearMotionY;
                connectedJoint.zMotion = CollisionLinearMotionZ;
                connectedJoint.angularXMotion = CollisionAngularMotionX;
                connectedJoint.angularYMotion = CollisionAngularMotionY;
                connectedJoint.angularZMotion = CollisionAngularMotionZ;

                SoftJointLimitSpring sp = connectedJoint.linearLimitSpring;
                sp.spring = 5000;
                sp.damper = 5;

                // Set X,Y, and Z drive to our values
                setPositionSpring(CollisionSpring, 5f);

                // Slerp drive used for rotation
                setSlerpDrive(CollisionSlerp, 5f);
            }

            if(BeingHeldWithTwoHands && SecondaryLookAtTransform != null) {
                connectedJoint.angularXMotion = ConfigurableJointMotion.Free;

                setSlerpDrive(1000f, 2f);
                connectedJoint.angularYMotion = ConfigurableJointMotion.Limited;


                connectedJoint.angularZMotion = ConfigurableJointMotion.Limited;

                if (TwoHandedRotation == TwoHandedRotationType.LookAtSecondary) {
                    checkSecondaryLook();
                }
            }
        }

        void setPositionSpring(float spring, float damper) {

            if(connectedJoint == null) {
                return;
            }

            JointDrive xDrive = connectedJoint.xDrive;
            xDrive.positionSpring = spring;
            xDrive.positionDamper = damper;
            connectedJoint.xDrive = xDrive;

            JointDrive yDrive = connectedJoint.yDrive;
            yDrive.positionSpring = spring;
            yDrive.positionDamper = damper;
            connectedJoint.yDrive = yDrive;

            JointDrive zDrive = connectedJoint.zDrive;
            zDrive.positionSpring = spring;
            zDrive.positionDamper = damper;
            connectedJoint.zDrive = zDrive;
        }

        void setSlerpDrive(float slerp, float damper) {
            if(connectedJoint) {
                JointDrive slerpDrive = connectedJoint.slerpDrive;
                slerpDrive.positionSpring = slerp;
                slerpDrive.positionDamper = damper;
                connectedJoint.slerpDrive = slerpDrive;
            }
        }
        
        public virtual Vector3 GetGrabberVector3(Grabber grabber, bool isSecondary) {
            // Snap
            if (GrabMechanic == GrabType.Snap) {
                return GetGrabberWithGrabPointOffset(grabber, isSecondary ? secondaryGrabOffset : primaryGrabOffset);
            }
            // Precise
            else {
                if (isSecondary) {
                    return grabTransformSecondary.position;
                }

                return grabTransform.position;
            }
        }

        public virtual Quaternion GetGrabberQuaternion(Grabber grabber, bool isSecondary) {

            if (GrabMechanic == GrabType.Snap) {
                return GetGrabberWithOffsetWorldRotation(grabber);
            }
            else {
                if (isSecondary) {
                    return grabTransformSecondary.rotation;
                }

                return grabTransform.rotation;
            }
        }

        /// <summary>
        /// Apply a velocity on our Grabbable towards our Grabber
        /// </summary>
        void moveWithVelocity() {

            if(rigid == null) { return; }
            
            Vector3 destination = GetGrabbersAveragedPosition();

            float distance = Vector3.Distance(transform.position, destination);

            if (distance > 0.002f) {
                Vector3 positionDelta = destination - transform.position;

                // Move towards hand using velocity
                rigid.velocity = Vector3.MoveTowards(rigid.velocity, (positionDelta * MoveVelocityForce) * Time.fixedDeltaTime, 1f);
            }
            else {
                // Very close - just move object right where it needs to be and set velocity to 0 so it doesn't overshoot
                rigid.MovePosition(destination);
                rigid.velocity = Vector3.zero;
            }            
        }

        float angle;
        Vector3 axis, angularTarget, angularMovement;

        void rotateWithVelocity() {

            if(rigid == null) {
                return;
            }

            bool noRecentCollisions = collisions != null && collisions.Count == 0 && lastNoCollisionSeconds >= 0.5f;
            bool moveInstantlyOneHand = InstantMovement; // MoveAngularVelocityForce >= 200f;
            bool moveInstantlyTwoHands = BeingHeldWithTwoHands && InstantMovement; // TwoHandedRotation == TwoHandedRotationType.LookAtSecondary && SecondHandLookSpeed > 20;

            if (InstantMovement == true && noRecentCollisions && (moveInstantlyOneHand || moveInstantlyTwoHands)) {
                //rigid.rotation = GetGrabbersAveragedRotation();
                rigid.MoveRotation(Quaternion.Slerp(rigid.rotation, GetGrabbersAveragedRotation(), Time.fixedDeltaTime * SecondHandLookSpeed));

                // Can exit immediately
                return;
            }

            Quaternion rotationDelta = GetGrabbersAveragedRotation() * Quaternion.Inverse(transform.rotation);
            rotationDelta.ToAngleAxis(out angle, out axis);

            // Use closest rotation. If over 180 degrees, rotate the other way
            if (angle > 180) {
                angle -= 360;
            }

            if (angle != 0) {
                angularTarget = angle * axis;
                angularTarget = (angularTarget * MoveAngularVelocityForce) * Time.fixedDeltaTime;

                angularMovement = Vector3.MoveTowards(rigid.angularVelocity, angularTarget, MoveAngularVelocityForce);

                if (angularMovement.magnitude > 0.05f) {
                    // rigid.centerOfMass = transform.InverseTransformPoint(GetGrabbersAveragedPosition());
                    rigid.angularVelocity = angularMovement;
                }

                // Snap in place if very close
                if(angle < 1) {
                    rigid.MoveRotation(GetGrabbersAveragedRotation());
                    rigid.angularVelocity = Vector3.zero;
                }
            }
        }

        /// <summary>
        /// Get the estimated world position of the grabber(s) holding this object. Position factors in 2-Handed grabbing options
        /// </summary>
        /// <returns>World position og the grabber, with two handed behavior factored in.</returns>
        public Vector3 GetGrabbersAveragedPosition() {
            // Start with our primary Grabber
            Vector3 destination = GetGrabberVector3(GetPrimaryGrabber(), false);

            // Add secondary grabber position
            if (SecondaryGrabBehavior == OtherGrabBehavior.DualGrab && TwoHandedPosition == TwoHandedPositionType.Lerp) {
                // Check Secondary Grabbable first
                if (SecondaryGrabbable != null && SecondaryGrabbable.BeingHeld) {
                    // Add secondary grab position
                    destination = Vector3.Lerp(destination, SecondaryGrabbable.GetGrabberVector3(SecondaryGrabbable.GetPrimaryGrabber(), false), TwoHandedPostionLerpAmount);
                }
                // Check if a grabber is holding this object
                else if (heldByGrabbers != null && heldByGrabbers.Count > 1) {
                    destination = Vector3.Lerp(destination, GetGrabberVector3(heldByGrabbers[1], true), TwoHandedPostionLerpAmount);
                }
            }

            // Return primary grabber position as default
            return destination;
        }

        public Quaternion GetGrabbersAveragedRotation() {
            // Start with our primary Grabber's rotation
            Quaternion destination = GetGrabberQuaternion(GetPrimaryGrabber(), false);

            // Add secondary grabber position
            // Check Lerp / Slerp Setting
            if (SecondaryGrabBehavior == OtherGrabBehavior.DualGrab && TwoHandedRotation == TwoHandedRotationType.Lerp || TwoHandedRotation == TwoHandedRotationType.Slerp) {
                // Check Secondary Grabbable first
                if (SecondaryGrabbable != null && SecondaryGrabbable.BeingHeld) {
                    if (TwoHandedRotation == TwoHandedRotationType.Lerp) {
                        destination = Quaternion.Lerp(destination, SecondaryGrabbable.GetGrabberQuaternion(SecondaryGrabbable.GetPrimaryGrabber(), false), TwoHandedRotationLerpAmount);
                    }
                    else {
                        destination = Quaternion.Slerp(destination, SecondaryGrabbable.GetGrabberQuaternion(SecondaryGrabbable.GetPrimaryGrabber(), false), TwoHandedRotationLerpAmount);
                    }
                }
                // Check if a grabber is holding this object
                else if (heldByGrabbers != null && heldByGrabbers.Count > 1) {
                    if (TwoHandedRotation == TwoHandedRotationType.Lerp) {
                        destination = Quaternion.Lerp(destination, GetGrabberQuaternion(heldByGrabbers[1], true), TwoHandedRotationLerpAmount);
                    }
                    else {
                        destination = Quaternion.Slerp(destination, GetGrabberQuaternion(heldByGrabbers[1], true), TwoHandedRotationLerpAmount);
                    }
                }
            }
            // LookAt type
            else if (SecondaryGrabBehavior == OtherGrabBehavior.DualGrab && TwoHandedRotation == TwoHandedRotationType.LookAtSecondary) {
                // Rotate our primary grabber towards our secondary grabber
                // Check Secondary Grabbable first
                if (SecondaryGrabbable != null && SecondaryGrabbable.BeingHeld) {
                    
                    Vector3 targetVector = GetGrabberVector3(SecondaryGrabbable.GetPrimaryGrabber(), false) - GetGrabberVector3(GetPrimaryGrabber(), false);

                    // Forward Direction
                    if(TwoHandedLookVector == TwoHandedLookDirection.Horizontal) {
                        destination = Quaternion.LookRotation(targetVector, -GetPrimaryGrabber().transform.up) * Quaternion.AngleAxis(180f, Vector3.up) * Quaternion.AngleAxis(180f, Vector3.forward);
                    }
                    // Do up / down
                    else if(TwoHandedLookVector == TwoHandedLookDirection.Vertical) {
                        destination = Quaternion.LookRotation(targetVector, -GetPrimaryGrabber().transform.right) * Quaternion.AngleAxis(90f, Vector3.right) * Quaternion.AngleAxis(180f, Vector3.forward) * Quaternion.AngleAxis(-90f, Vector3.up);
                    }
                }
                // Check if a grabber is holding this object
                else if (heldByGrabbers != null && heldByGrabbers.Count > 1) {
                    // destination = Quaternion.Lerp(destination, GetGrabberQuaternion(heldByGrabbers[1], true), TwoHandedRotationLerpAmount);
                }
            }

            return destination;
        }

        public virtual void UpdateKinematicPhysics() {

            // Distance moved equals elapsed time times speed.
            float distCovered = (Time.time - LastGrabTime) * GrabSpeed;

            // How far along have we traveled
            float fractionOfJourney = distCovered / journeyLength;

            Vector3 destination = GetGrabbersAveragedPosition();
            Quaternion destRotation = grabTransform.rotation;

            // Realtime update position to make it easier to preview grab transforms
            bool realtime = Application.isEditor;
            if(realtime) {
                destination = getRemotePosition(GetPrimaryGrabber());
                //destRotation = getRemoteRotation(GetPrimaryGrabber());
                rotateGrabber(false);
            }

            if (GrabMechanic == GrabType.Snap) {
                // Set our position as a fraction of the distance between the markers.
                Grabber g = GetPrimaryGrabber();

                // Update local transform in real time
                if (g != null) {
                    if (ParentToHands) {
                        transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero - GrabPositionOffset, fractionOfJourney);
                        transform.localRotation = Quaternion.Lerp(transform.localRotation, grabTransform.localRotation, Time.deltaTime * 10);
                    }
                    // Position the object in world space using physics
                    else {
                        movePosition(Vector3.Lerp(transform.position, destination, fractionOfJourney));
                        moveRotation(Quaternion.Lerp(transform.rotation, destRotation, Time.deltaTime * 20));
                    }
                }
                else {
                    movePosition(destination);
                    transform.localRotation = grabTransform.localRotation;
                }
            }
            else if (GrabMechanic == GrabType.Precise) {
                movePosition(grabTransform.position);
                moveRotation(grabTransform.rotation);
            }
        }

        public virtual void UpdateVelocityPhysics() {

            // Make sure rotation is always free
            if(connectedJoint != null) {
                connectedJoint.xMotion = ConfigurableJointMotion.Free;
                connectedJoint.yMotion = ConfigurableJointMotion.Free;
                connectedJoint.zMotion = ConfigurableJointMotion.Free;
                connectedJoint.angularXMotion = ConfigurableJointMotion.Free;
                connectedJoint.angularYMotion = ConfigurableJointMotion.Free;
                connectedJoint.angularZMotion = ConfigurableJointMotion.Free;
            }

            // Make sure linear spring is off
            // Set X,Y, and Z drive to our values
            setPositionSpring(0, 0.5f);

            // Slerp drive used for rotation
            setSlerpDrive(5, 0.5f);

            // Update collision detection mode to ContinuousDynamic while being held
            if (rigid && rigid.isKinematic) {
                rigid.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }
            else if(rigid) {
                rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            moveWithVelocity();
            rotateWithVelocity();

            //Parent to our hands if no colliisions present
            // This makes our object move 1:1 with our controller
            if (ParentToHands) {
                // Parent to hands if no collisions
                bool afterCollision = collisions.Count == 0 && lastNoCollisionSeconds >= 0.2f;
                // Set parent to us to keep movement smoothed
                if (afterCollision) {
                    Grabber g = GetPrimaryGrabber();
                    transform.parent = g.transform;
                }
                else {
                    transform.parent = null;
                }
            }
        }

        void checkParentHands(Grabber g) {            

            if (ParentHandModel && g != null) {

                // Precise - Go ahead and parent hands model immediately 
                if (GrabMechanic == GrabType.Precise) {
                    parentHandGraphics(g);
                }
                // Snap - Hand Models if close enough
                else {
                    // Vector3 grabberPosition = g.transform.position;
                    Vector3 grabberPosition = grabTransform.position;
                    Vector3 grabbablePosition = transform.position;

                    float distance = Vector3.Distance(grabbablePosition, grabberPosition);

                    // If object can be moved towards the grabber, wait until the item is close before snapping hand to it
                    if (CanBeMoved) {
                        // Close enough to snap hand graphics
                        if (distance < 0.001f ) {
                            // Snap position
                            parentHandGraphics(g);

                            // Snap Hand Model Position
                            if (g.HandsGraphics != null) {
                                g.HandsGraphics.localEulerAngles = Vector3.zero;
                                g.HandsGraphics.localPosition = g.handsGraphicsGrabberOffset;
                            }
                        }
                    }
                    else {
                        // Can't be moved so go ahead and snap
                        if (grabTransform != null && distance < 0.1f) {

                            // Snap position
                            parentHandGraphics(g);
                            positionHandGraphics(g); 

                            if (g.HandsGraphics != null) {
                                g.HandsGraphics.localEulerAngles = Vector3.zero;
                                g.HandsGraphics.localPosition = g.handsGraphicsGrabberOffset;
                            }
                        }
                    }
                }
            }
        }

        // Can this object be moved towards an object, or is it fixed in place / attached to something else
        bool canBeMoved() {

            if (GetComponent<Rigidbody>() == null) {
                return false;
            }

            if (GetComponent<Joint>() != null) {
                return false;
            }

            return true;
        }

        void checkSecondaryLook() {

            // Create transform to look at if we are looking at a precise grab
            if (BeingHeldWithTwoHands) {
                if (SecondaryLookAtTransform == null) {
                    Grabber thisGrabber = GetPrimaryGrabber();
                    Grabber secondaryGrabber = SecondaryGrabbable.GetPrimaryGrabber();

                    GameObject o = new GameObject();
                    SecondaryLookAtTransform = o.transform;
                    SecondaryLookAtTransform.name = "LookAtTransformTemp";
                    // Precise grab can use current grabber position
                    if (SecondaryGrabbable.GrabMechanic == GrabType.Precise) {
                        SecondaryLookAtTransform.position = secondaryGrabber.transform.position;
                    }
                    // Otherwise use snap point
                    else {
                        Transform grabPoint = SecondaryGrabbable.GetGrabPoint();
                        if (grabPoint) {
                            SecondaryLookAtTransform.position = grabPoint.position;
                        }
                        else {
                            SecondaryLookAtTransform.position = SecondaryGrabbable.transform.position;
                        }

                        SecondaryLookAtTransform.position = SecondaryGrabbable.transform.position;
                    }

                    if (SecondaryLookAtTransform && thisGrabber) {
                        SecondaryLookAtTransform.parent = thisGrabber.transform;
                        SecondaryLookAtTransform.localEulerAngles = Vector3.zero;
                        SecondaryLookAtTransform.localPosition = new Vector3(0, 0, SecondaryLookAtTransform.localPosition.z);

                        // Move parent back to grabber
                        SecondaryLookAtTransform.parent = secondaryGrabber.transform;
                    }
                }
            }

            // We should not be aiming at anything if a Grabbable was specified
            if (SecondaryGrabbable != null && !SecondaryGrabbable.BeingHeld && SecondaryLookAtTransform != null) {
                clearLookAtTransform();
            }

            Grabber heldBy = GetPrimaryGrabber();
            if (heldBy) {
                Transform grabberTransform = heldBy.transform;

                if (SecondaryLookAtTransform != null) {
                    Vector3 initialRotation = grabberTransform.localEulerAngles;

                    Quaternion dest = Quaternion.LookRotation(SecondaryLookAtTransform.position - grabberTransform.position, Vector3.up);
                    grabberTransform.rotation = Quaternion.Slerp(grabberTransform.rotation, dest, Time.deltaTime * SecondHandLookSpeed);

                    // Exclude rotations to only x and y
                    grabberTransform.localEulerAngles = new Vector3(grabberTransform.localEulerAngles.x, grabberTransform.localEulerAngles.y, initialRotation.z);
                }
                else {
                    rotateGrabber(true);
                }
            }
        }

        void rotateGrabber(bool lerp = false) {
            Grabber heldBy = GetPrimaryGrabber();
            if (heldBy != null) {
                Transform grabberTransform = heldBy.transform;

                if (lerp) {
                    grabberTransform.localRotation = Quaternion.Slerp(grabberTransform.localRotation, Quaternion.Inverse(Quaternion.Euler(GrabRotationOffset)), Time.deltaTime * 20);
                }
                else {
                    grabberTransform.localRotation = Quaternion.Inverse(Quaternion.Euler(GrabRotationOffset));
                }
            }
        }

        public Transform GetGrabPoint() {
            return primaryGrabOffset;
        }

        public virtual void GrabItem(Grabber grabbedBy) {

            // Make sure we release this item
            if (BeingHeld && SecondaryGrabBehavior != OtherGrabBehavior.DualGrab) {
                DropItem(false, true);
            }

            bool isPrimaryGrab = !BeingHeld;
            bool isSecondaryGrab = BeingHeld && SecondaryGrabBehavior == OtherGrabBehavior.DualGrab;

            // Officially being held
            BeingHeld = true;
            LastGrabTime = Time.time;

            // Primary Grabber just grabbed this item
            if (isPrimaryGrab) {
                // Make sure all values are reset first
                ResetGrabbing();

                // Set where the item will move to on the grabber
                primaryGrabOffset = GetClosestGrabPoint(grabbedBy);
                secondaryGrabOffset = null;

                // Set the active Grab Point that we will be using
                if (primaryGrabOffset) {
                    ActiveGrabPoint = primaryGrabOffset.GetComponent<GrabPoint>();
                }
                else {
                    ActiveGrabPoint = null;
                }

                // Update Hand Pose Id
                if (primaryGrabOffset != null && ActiveGrabPoint != null) {
                    CustomHandPose = primaryGrabOffset.GetComponent<GrabPoint>().HandPose;
                    SelectedHandPose = primaryGrabOffset.GetComponent<GrabPoint>().SelectedHandPose;
                    handPoseType = primaryGrabOffset.GetComponent<GrabPoint>().handPoseType;
                }
                else {
                    CustomHandPose = initialHandPoseId;
                    SelectedHandPose = initialHandPose;
                    handPoseType = initialHandPoseType;
                }

                // Update held by properties
                addGrabber(grabbedBy);
                grabTransform.parent = grabbedBy.transform;
                rotateGrabber(false);

                // Use center of grabber if snapping
                if (GrabMechanic == GrabType.Snap) {
                    grabTransform.localEulerAngles = Vector3.zero;
                    grabTransform.localPosition = -GrabPositionOffset;
                }
                // Precision hold can use position of what we're grabbing
                else if (GrabMechanic == GrabType.Precise) {
                    grabTransform.position = transform.position;
                    grabTransform.rotation = transform.rotation;
                }

                // First remove any connected joints if necessary
                var projectile = GetComponent<Projectile>();
                if (projectile) {
                    var fj = GetComponent<FixedJoint>();
                    if (fj) {
                        Destroy(fj);
                    }
                }

                // Setup any relevant joints or required components
                if (GrabPhysics == GrabPhysics.PhysicsJoint) {
                    setupConfigJointGrab(grabbedBy, GrabMechanic);
                }
                else if (GrabPhysics == GrabPhysics.Velocity) {
                    setupVelocityGrab(grabbedBy, GrabMechanic);
                }
                else if (GrabPhysics == GrabPhysics.FixedJoint) {
                    setupFixedJointGrab(grabbedBy, GrabMechanic);
                }
                else if (GrabPhysics == GrabPhysics.Kinematic) {
                    setupKinematicGrab(grabbedBy, GrabMechanic);
                }

                // Stop our object on initial grab
                if(rigid) {
                    rigid.velocity = Vector3.zero;
                    rigid.angularVelocity = Vector3.zero;
                }
                

                // Let events know we were grabbed
                for (int x = 0; x < events.Count; x++) {
                    events[x].OnGrab(grabbedBy);
                }

                checkParentHands(grabbedBy);

                // Move Hand Model
                if (GrabMechanic == GrabType.Precise && SnapHandModel && primaryGrabOffset != null && grabbedBy.HandsGraphics != null) {
                    grabbedBy.HandsGraphics.transform.parent = primaryGrabOffset;
                    grabbedBy.HandsGraphics.localPosition = grabbedBy.handsGraphicsGrabberOffset;
                    grabbedBy.HandsGraphics.localEulerAngles = grabbedBy.handsGraphicsGrabberOffsetRotation;
                }

                SubscribeToMoveEvents();

            }
            else if (isSecondaryGrab) {
                // Set where the item will move to on the grabber
                secondaryGrabOffset = GetClosestGrabPoint(grabbedBy);

                // Update held by properties
                addGrabber(grabbedBy);

                grabTransformSecondary.parent = grabbedBy.transform;

                // Use center of grabber if snapping
                if (GrabMechanic == GrabType.Snap) {
                    grabTransformSecondary.localEulerAngles = Vector3.zero;
                    grabTransformSecondary.localPosition = GrabPositionOffset;
                }
                // Precision hold can use position of what we're grabbing
                else if (GrabMechanic == GrabType.Precise) {
                    grabTransformSecondary.position = transform.position;
                    grabTransformSecondary.rotation = transform.rotation;
                }

                checkParentHands(grabbedBy);

                // Move Hand Model if snap hands and precise
                if (GrabMechanic == GrabType.Precise && SnapHandModel && secondaryGrabOffset != null && grabbedBy.HandsGraphics != null) {
                    grabbedBy.HandsGraphics.transform.parent = secondaryGrabOffset;
                    grabbedBy.HandsGraphics.localPosition = grabbedBy.handsGraphicsGrabberOffset;
                    grabbedBy.HandsGraphics.localEulerAngles = grabbedBy.handsGraphicsGrabberOffsetRotation;
                }
            }

            // Hide the hand graphics if necessary
            if (HideHandGraphics) {
                grabbedBy.HideHandGraphics();
            }

            journeyLength = Vector3.Distance(grabPosition, grabbedBy.transform.position);
        }

        protected virtual void setupConfigJointGrab(Grabber grabbedBy, GrabType grabType) {
            // Set up the new connected joint
            if (GrabMechanic == GrabType.Precise) {
                connectedJoint = grabbedBy.GetComponent<ConfigurableJoint>();
                connectedJoint.connectedBody = rigid;
                // Just let the autoconfigure handle the calculations for us
                connectedJoint.autoConfigureConnectedAnchor = true;
            }

            // Set up the physics joint for snapping
            else if (GrabMechanic == GrabType.Snap) {
                // Need to Fix Rotation on Snap Physics when close by
                transform.rotation = grabTransform.rotation;

                // Setup joint
                setupConfigJoint(grabbedBy);

                rigid.MoveRotation(grabTransform.rotation);
            }
        }

        protected virtual void setupFixedJointGrab(Grabber grabbedBy, GrabType grabType) {
            FixedJoint joint = grabbedBy.gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = rigid;

            // Setup Fixed Joint in place
            if (GrabMechanic == GrabType.Precise) {
                // Just let the autoconfigure handle the calculations for us
                joint.autoConfigureConnectedAnchor = true;
            }
            // Setup the snap point manually
            else if (GrabMechanic == GrabType.Snap) {
                joint.autoConfigureConnectedAnchor = false;
                joint.anchor = Vector3.zero;
                joint.connectedAnchor = GrabPositionOffset;
            }
        }

        protected virtual void setupKinematicGrab(Grabber grabbedBy, GrabType grabType) {
            if (ParentToHands) {
                transform.parent = grabbedBy.transform;
            }

            if (rigid != null) {
                
                // Update detection mode if necessary
                if (rigid.collisionDetectionMode == CollisionDetectionMode.Continuous || rigid.collisionDetectionMode == CollisionDetectionMode.ContinuousDynamic) {
                    rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                }
                rigid.isKinematic = true;
            }
        }

        protected virtual void setupVelocityGrab(Grabber grabbedBy, GrabType grabType) {
            // Setup joint to be used when moving with velocity
            bool addJointToVelocityGrabbable = false;
            if(addJointToVelocityGrabbable) {
                if (GrabMechanic == GrabType.Precise) {
                    connectedJoint = grabbedBy.GetComponent<ConfigurableJoint>();
                    connectedJoint.connectedBody = rigid;
                    // Just let the autoconfigure handle the calculations for us
                    connectedJoint.autoConfigureConnectedAnchor = true;
                }
                // Set up the connected joint for snapping
                else if (GrabMechanic == GrabType.Snap) {
                    transform.rotation = grabTransform.rotation;
                    // Setup joint
                    setupConfigJoint(grabbedBy);
                    rigid.MoveRotation(grabTransform.rotation);
                }
            }

            // Disable Gravity to prevent fighting physics with the hand object
            rigid.useGravity = false;            
        }

        public virtual void GrabRemoteItem(Grabber grabbedBy) {
            flyingTo = grabbedBy;
            grabTransform.parent = grabbedBy.transform;
            grabTransform.localEulerAngles = Vector3.zero;
            grabTransform.localPosition = -GrabPositionOffset;

            grabTransform.localEulerAngles = GrabRotationOffset;            

            remoteGrabbing = true;
        }

        public virtual void ResetGrabbing() {
            if (rigid) {
                rigid.isKinematic = wasKinematic;
            }

            flyingTo = null;

            remoteGrabbing = false;

            collisions = new List<Collider>();
        }        

        public virtual void DropItem(Grabber droppedBy, bool resetVelocity, bool resetParent) {

            // Nothing holding us
            if (heldByGrabbers == null) {
                BeingHeld = false;
                return;
            }

            bool isPrimaryGrabber = droppedBy == GetPrimaryGrabber();
            bool isSecondaryGrabber = !isPrimaryGrabber && heldByGrabbers.Count > 1;

            if(isPrimaryGrabber) {

                // Keep track of if we were being held with two hands or not before dropping the item
                bool wasHeldWithTwoHands = BeingHeldWithTwoHands;
                // Should we release this item
                bool releaseItem = true;

                if (resetParent) {
                    ResetParent();
                }

                // disconnect all joints and set the connected object to null
                removeConfigJoint();

                // Remove Fixed Joint
                if (GrabPhysics == GrabPhysics.FixedJoint && droppedBy != null) {
                    FixedJoint joint = droppedBy.gameObject.GetComponent<FixedJoint>();
                    if (joint) {
                        GameObject.Destroy(joint);
                    }
                }

                //  If something called drop on this item we want to make sure the parent knows about it
                // Reset's Grabber position, grabbable state, etc.
                if (droppedBy) {
                    droppedBy.DidDrop();
                }

                // No longer need move events
                UnsubscribeFromMoveEvents();

                // No longer have a primary Grab Offset set
                primaryGrabOffset = null;

                // No longer looking at a 2h object
                clearLookAtTransform();

                removeGrabber(droppedBy);

                didParentHands = false;

                // This object is being held by another grabber. Should we drop the item, transfer it over, or do nothing.
                if (wasHeldWithTwoHands) {

                    // Force Release
                    if(TwoHandedDropBehavior == TwoHandedDropMechanic.Drop) {
                        // Drop Secondary Object
                        if(SecondaryGrabbable != null && SecondaryGrabbable.BeingHeld) {
                            SecondaryGrabbable.DropItem(false, false);
                        }
                        else {
                            // Drop our own object
                            DropItem(heldByGrabbers[0]);                            
                        }
                    }
                    // Swap To other Hand Side
                    else if (TwoHandedDropBehavior == TwoHandedDropMechanic.Transfer) {

                        // We are going to transfer this item, so no need to release
                        releaseItem = false;

                        // Swap to new grabber
                        var newGrabber = heldByGrabbers[0];
                        Vector3 localHandsPos = Vector3.zero;
                        Vector3 localHandsRot = Vector3.zero;

                        if (newGrabber.HandsGraphics != null) {
                            Transform prev = newGrabber.HandsGraphics.parent;
                            newGrabber.HandsGraphics.parent = transform;
                            localHandsPos = newGrabber.HandsGraphics.localPosition;
                            localHandsRot = newGrabber.HandsGraphics.localEulerAngles;
                            newGrabber.HandsGraphics.parent = prev;
                        }

                        DropItem(newGrabber);
                        newGrabber.GrabGrabbable(this);

                        // Call Transfer Events
                        // OnTransferGrabber(Grabber from, Grabber to);

                        // Fix Hands position
                        if (newGrabber.HandsGraphics != null && ParentHandModel == true && GrabMechanic == GrabType.Precise) {
                            Transform prev = newGrabber.HandsGraphics.parent;
                            newGrabber.HandsGraphics.parent = transform;
                            newGrabber.HandsGraphics.localPosition = localHandsPos;
                            newGrabber.HandsGraphics.localEulerAngles = localHandsRot;
                            newGrabber.HandsGraphics.parent = prev;
                        }
                    }
                }
                // Release the object
                if(releaseItem) {

                    LastDropTime = Time.time;

                    // Release item and apply physics force to it
                    if (rigid != null && GrabPhysics != GrabPhysics.None) {
                        rigid.isKinematic = wasKinematic;
                        rigid.useGravity = usedGravity;
                        rigid.interpolation = initialInterpolationMode;
                        rigid.collisionDetectionMode = initialCollisionMode;
                    }

                    // Override Kinematic status if specified
                    if (ForceDisableKinematicOnDrop) {
                        rigid.isKinematic = false;
                        // Free of constraints if they were set
                        if (rigid.constraints == RigidbodyConstraints.FreezeAll) {
                            rigid.constraints = RigidbodyConstraints.None;
                        }
                    }

                    // On release event
                    if (events != null) {
                        for (int x = 0; x < events.Count; x++) {
                            events[x].OnRelease();
                        }
                    }

                    // Reset hand pose
                    CustomHandPose = initialHandPoseId;
                    SelectedHandPose = initialHandPose;
                    handPoseType = initialHandPoseType;

                    // Apply velocity last
                    if (rigid && resetVelocity && droppedBy && AddControllerVelocityOnDrop&& GrabPhysics != GrabPhysics.None) {
                        // Make sure velocity is passed on
                        Vector3 velocity = droppedBy.GetGrabberAveragedVelocity() + droppedBy.GetComponent<Rigidbody>().velocity;
                        Vector3 angularVelocity = droppedBy.GetGrabberAveragedAngularVelocity() + droppedBy.GetComponent<Rigidbody>().angularVelocity;

                        if (gameObject.activeSelf) {
                            Release(velocity, angularVelocity);
                        }
                    }
                }
            }
            else if (isSecondaryGrabber) {
                //  If something called drop on this item we want to make sure the parent knows about it
                // Reset's Grabber position, grabbable state, etc.
                if (droppedBy) {
                    droppedBy.DidDrop();
                }

                removeGrabber(droppedBy);

                secondaryGrabOffset = null;

                // didParentHands = false;
            }

            BeingHeld = heldByGrabbers != null && heldByGrabbers.Count > 0;
        }

        void clearLookAtTransform() {
            if (SecondaryLookAtTransform != null && SecondaryLookAtTransform.transform.name == "LookAtTransformTemp") {
                GameObject.Destroy(SecondaryLookAtTransform.gameObject);
            }

            SecondaryLookAtTransform = null;
        }

        void callEvents(Grabber g) {
            if (events.Any()) {
                ControllerHand hand = g.HandSide;

                // Right Hand Controls
                if (hand == ControllerHand.Right) {
                    foreach (var e in events) {
                        e.OnGrip(input.RightGrip);
                        e.OnTrigger(input.RightTrigger);

                        if (input.RightTriggerUp) {
                            e.OnTriggerUp();
                        }
                        if (input.RightTriggerDown) {
                            e.OnTriggerDown();
                        }
                        if (input.AButton) {
                            e.OnButton1();
                        }
                        if (input.AButtonDown) {
                            e.OnButton1Down();
                        }
                        if (input.AButtonUp) {
                            e.OnButton1Up();
                        }
                        if (input.BButton) {
                            e.OnButton2();
                        }
                        if (input.BButtonDown) {
                            e.OnButton2Down();
                        }
                        if (input.BButtonUp) {
                            e.OnButton2Up();
                        }
                    }
                }

                // Left Hand Controls
                if (hand == ControllerHand.Left) {
                    for (int x = 0; x < events.Count; x++) {
                        GrabbableEvents e = events[x];
                        e.OnGrip(input.LeftGrip);
                        e.OnTrigger(input.LeftTrigger);

                        if (input.LeftTriggerUp) {
                            e.OnTriggerUp();
                        }
                        if (input.LeftTriggerDown) {
                            e.OnTriggerDown();
                        }
                        if (input.XButton) {
                            e.OnButton1();
                        }
                        if (input.XButtonDown) {
                            e.OnButton1Down();
                        }
                        if (input.XButtonUp) {
                            e.OnButton1Up();
                        }
                        if (input.YButton) {
                            e.OnButton2();
                        }
                        if (input.YButtonDown) {
                            e.OnButton2Down();
                        }
                        if (input.YButtonUp) {
                            e.OnButton2Up();
                        }
                    }
                }
            }
        }       

        public virtual void DropItem(Grabber droppedBy) {
            DropItem(droppedBy, true, true);
        }

        public virtual void DropItem(bool resetVelocity, bool resetParent) {
            DropItem(GetPrimaryGrabber(), resetVelocity, resetParent);
        }

        public void ResetScale() {
            transform.localScale = OriginalScale;
        }

        public void ResetParent() {
            transform.parent = originalParent;
        }

        public void UpdateOriginalParent(Transform newOriginalParent) {
            originalParent = newOriginalParent;
        }

        public void UpdateOriginalParent() {
            UpdateOriginalParent(transform.parent);
        }

        public ControllerHand GetControllerHand(Grabber g) {
            if(g != null) {
                return g.HandSide;
            }

            return ControllerHand.None;
        }
        
        /// <summary>
        /// Returns the Grabber that first grabbed this item. Return null if not being held.
        /// </summary>
        /// <returns></returns>
        public virtual Grabber GetPrimaryGrabber() {
            if(heldByGrabbers != null) {
                for (int x = 0; x < heldByGrabbers.Count; x++) {
                    if (heldByGrabbers[x] != null && heldByGrabbers[x].HeldGrabbable == this) {
                        return heldByGrabbers[x];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get the closest valid grabber. 
        /// </summary>
        /// <returns>Returns null if no valid Grabbers in range</returns>
        public virtual Grabber GetClosestGrabber() {

            Grabber closestGrabber = null;
            float lastDistance = 9999;

            if (validGrabbers != null) {

                for (int x = 0; x < validGrabbers.Count; x++) {
                    Grabber g = validGrabbers[x];
                    if (g != null) {
                        float dist = Vector3.Distance(grabPosition, g.transform.position);
                        if(dist < lastDistance) {
                            closestGrabber = g;
                        }
                    }
                }
            }

            return closestGrabber;
        }

        public virtual Transform GetClosestGrabPoint(Grabber grabber) {
            Transform grabPoint = null;
            float lastDistance = 9999;
            float lastAngle = 360;
            if(GrabPoints != null) {
                int grabCount = GrabPoints.Count;
                for (int x = 0; x < grabCount; x++) {
                    Transform g = GrabPoints[x];

                    // Transform may have been destroyed
                    if (g == null) {
                        continue;
                    }

                    float thisDist = Vector3.Distance(g.transform.position, grabber.transform.position);
                    if (thisDist <= lastDistance) {

                        // Check for GrabPoint component that may override some values
                        GrabPoint gp = g.GetComponent<GrabPoint>();
                        if (gp) {

                            // Not valid for this hand side
                            if((grabber.HandSide == ControllerHand.Left && !gp.LeftHandIsValid) || (grabber.HandSide == ControllerHand.Right && !gp.RightHandIsValid)) {
                                continue;
                            }

                            // Angle is too great
                            float currentAngle = Quaternion.Angle(grabber.transform.rotation, g.transform.rotation);
                            if (currentAngle > gp.MaxDegreeDifferenceAllowed) {
                                continue;
                            }

                            // Last angle was better, don't use this one
                            if (currentAngle > lastAngle && gp.MaxDegreeDifferenceAllowed != 360) {
                                continue;
                            }

                            lastAngle = currentAngle;
                        }

                        grabPoint = g;
                        lastDistance = thisDist;
                    }
                }
            }

            return grabPoint;
        }

        /// <summary>
        /// Throw the object by applying velocity
        /// </summary>
        /// <param name="velocity">How much velocity to apply to the grabbable. Multiplied by ThrowForceMultiplier</param>
        /// <param name="angularVelocity">How much angular velocity to apply to the grabbable.</param>
        public virtual void Release(Vector3 velocity, Vector3 angularVelocity) {
            Vector3 releaseVelocity = velocity * ThrowForceMultiplier;

            // Make sure this is a valid velocity
            if (float.IsInfinity(releaseVelocity.x) || float.IsNaN(releaseVelocity.x)) {
                return;
            }

            rigid.velocity = releaseVelocity;
            rigid.angularVelocity = angularVelocity;
        }

        public virtual bool IsValidCollision(Collision collision) {
            return IsValidCollision(collision.collider);
        }

        public virtual bool IsValidCollision(Collider col) {

            // Ignore Projectiles from grabbable collision
            // This way our grabbable stays rigid when projectils come in contact
            string transformName = col.transform.name;
            if (transformName.Contains("Projectile") || transformName.Contains("Bullet") || transformName.Contains("Clip")) {
                return false;
            }

            // Ignore Character Joints as these cause jittery issues
            if (transformName.Contains("Joint")) {
                return false;
            }

            // Ignore Character Controllers
            CharacterController cc = col.gameObject.GetComponent<CharacterController>();
            if (cc && col) {
                Physics.IgnoreCollision(col, cc, true);
                return false;
            }

            return true;
        }

        public virtual void parentHandGraphics(Grabber g) {
            if (g.HandsGraphics != null) {
                // Set to specified Grab Transform
                if (primaryGrabOffset != null) {
                    g.HandsGraphics.transform.parent = primaryGrabOffset;
                    didParentHands = true;
                }
                else {
                    g.HandsGraphics.transform.parent = transform;
                    didParentHands = true;
                }
            }
        }

        void setupConfigJoint(Grabber g) {
            connectedJoint = g.GetComponent<ConfigurableJoint>();
            connectedJoint.autoConfigureConnectedAnchor = false;
            connectedJoint.connectedBody = rigid;
            connectedJoint.anchor = Vector3.zero;
            connectedJoint.connectedAnchor = GrabPositionOffset;
        }

        void removeConfigJoint() {
            if (connectedJoint != null) {
                connectedJoint.anchor = Vector3.zero;
                connectedJoint.connectedBody = null;
            }
        }

        void addGrabber(Grabber g) {
            if (heldByGrabbers == null) {
                heldByGrabbers = new List<Grabber>();
            }

            if (!heldByGrabbers.Contains(g)) {
                heldByGrabbers.Add(g);
            }
        }

        void removeGrabber(Grabber g) {
            if (heldByGrabbers == null) {
                heldByGrabbers = new List<Grabber>();
            }
            else if (heldByGrabbers.Contains(g)) {
                heldByGrabbers.Remove(g);
            }

            Grabber removeGrabber = null;
            // Clean up any other latent grabbers
            for (int x = 0; x < heldByGrabbers.Count; x++) {
                Grabber grab = heldByGrabbers[x];
                if (grab.HeldGrabbable == null || grab.HeldGrabbable != this) {
                    removeGrabber = grab;
                }
            }

            if (removeGrabber) {
                heldByGrabbers.Remove(removeGrabber);
            }
        }

        /// <summary>
        /// Moves the Grabbable using MovePosition if rigidbody present. Otherwise use transform.position
        /// </summary>
        void movePosition(Vector3 worldPosition) {
            if (rigid) {
                rigid.MovePosition(worldPosition);
            }
            else {
                transform.position = worldPosition;
            }
        }

        /// <summary>
        /// Rotates the Grabbable using MoveRotation if rigidbody present. Otherwise use transform.rotation
        /// </summary>
        void moveRotation(Quaternion worldRotation) {
            if (rigid) {
                rigid.MoveRotation(worldRotation);
            }
            else {
                transform.rotation = worldRotation;
            }
        }

        protected Vector3 getRemotePosition(Grabber toGrabber) {

            return GetGrabberWithGrabPointOffset(toGrabber, GetClosestGrabPoint(toGrabber));

            //if (toGrabber != null) {
            //    Transform pointPosition = GetClosestGrabPoint(toGrabber);

            //    if(pointPosition) {
            //        Vector3 grabberPosition = toGrabber.transform.position;

            //        if (pointPosition != null) {
            //            grabberPosition += transform.position - pointPosition.position;
            //            //Vector3 offset = toGrabber.transform.InverseTransformPoint(pointPosition.position);
            //            //grabberPosition += offset;
            //        }

            //        return grabberPosition;
            //    }

            //    return grabTransform.position;
            //}

            //return grabTransform.position;
        }

        protected Quaternion getRemoteRotation(Grabber grabber) {

            if (grabber != null) {
                Transform point = GetClosestGrabPoint(grabber);
                if (point) {
                    Quaternion originalRot = grabTransform.rotation;
                    grabTransform.localRotation *= Quaternion.Inverse(point.localRotation);
                    Quaternion result = grabTransform.rotation;

                    grabTransform.rotation = originalRot;

                    return result;
                }
            }

            return grabTransform.rotation;
        }

        void filterCollisions() {
            for (int x = 0; x < collisions.Count; x++) {
                if (collisions[x] == null || !collisions[x].enabled || !collisions[x].gameObject.activeSelf) {
                    collisions.Remove(collisions[x]);
                    break;
                }
            }
        }

        /// <summary>
        /// A BNGPlayerController is optional, but if one is available we can check the last moved time in order to strengthen the physics joint during quick movements. This helps prevent jitter or flying objects in certain situations.
        /// </summary>
        /// <returns></returns>
        public virtual BNGPlayerController GetBNGPlayerController() {

            if (_player != null) {
                return _player;
            }

            // The player object can be used to determine if the object is about to move rapidly
            if (GameObject.FindGameObjectWithTag("Player")) {
                return _player = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<BNGPlayerController>();
            }
            else {
                return _player = FindObjectOfType<BNGPlayerController>();
            }
        }

        /// <summary>
        /// Request the Grabbable to use a springy joint for the next X seconds
        /// </summary>
        /// <param name="seconds">How many seconds to make the Grabbable springy.</param>
        public virtual void RequestSpringTime(float seconds) {
            float requested = Time.time + seconds;

            // Only apply if our request is longer than the current request
            if(requested > requestSpringTime) {
                requestSpringTime = requested;
            }
        }

        public virtual void AddValidGrabber(Grabber grabber) {

            if (validGrabbers == null) {
                validGrabbers = new List<Grabber>();
            }

            if (!validGrabbers.Contains(grabber)) {
                validGrabbers.Add(grabber);
            }
        }

        public virtual void RemoveValidGrabber(Grabber grabber) {
            if (validGrabbers != null && validGrabbers.Contains(grabber)) {
                validGrabbers.Remove(grabber);
            }
        }

        bool subscribedToEvents = false;
        bool grabbableIsLocked = false;

        /// <summary>
        /// Subscribe to any movement-related events that might cause our Grabbable to suddenly move far away.
        /// By subscribing to these events before they occur we can then respond better to these positional updates
        /// </summary>
        public virtual void SubscribeToMoveEvents() {

            // Object can't be moved, so no need for subscription
            if(!CanBeMoved || subscribedToEvents == true || GrabPhysics == GrabPhysics.None) {
                return;
            }

            // Lock the slide in place when teleporting or snap turning
            PlayerTeleport.OnBeforeTeleport += LockGrabbableWithRotation;
            PlayerTeleport.OnAfterTeleport += UnlockGrabbable;

            PlayerRotation.OnBeforeRotate += LockGrabbableWithRotation;
            PlayerRotation.OnAfterRotate += UnlockGrabbable;

            // Only needed for velocity and physics type movement
            if(GrabPhysics == GrabPhysics.Velocity || GrabPhysics == GrabPhysics.PhysicsJoint) {
                SmoothLocomotion.OnBeforeMove += LockGrabbable;
                SmoothLocomotion.OnAfterMove += UnlockGrabbable;
            }

            // Kinematic can use parenting
            if (GrabPhysics == GrabPhysics.Kinematic && ParentToHands == true) {
                SmoothLocomotion.OnBeforeMove += LockGrabbableWithRotation;
                SmoothLocomotion.OnAfterMove += UnlockGrabbable;
            }
            else if (GrabPhysics == GrabPhysics.Kinematic && ParentToHands == false) {
                SmoothLocomotion.OnBeforeMove += LockGrabbable;
                SmoothLocomotion.OnAfterMove += UnlockGrabbable;
            }

            subscribedToEvents = true;
        }

        public virtual void UnsubscribeFromMoveEvents() {
            if(subscribedToEvents) {
                PlayerTeleport.OnBeforeTeleport -= LockGrabbableWithRotation;
                PlayerTeleport.OnAfterTeleport -= UnlockGrabbable;

                PlayerRotation.OnBeforeRotate -= LockGrabbableWithRotation;
                PlayerRotation.OnAfterRotate -= UnlockGrabbable;

                // Specific lock types
                if (GrabPhysics == GrabPhysics.Velocity || GrabPhysics == GrabPhysics.PhysicsJoint) {
                    SmoothLocomotion.OnBeforeMove -= LockGrabbable;
                    SmoothLocomotion.OnAfterMove -= UnlockGrabbable;
                }

                // Kinematic can use parenting
                if (GrabPhysics == GrabPhysics.Kinematic && ParentToHands == true) {
                    SmoothLocomotion.OnBeforeMove -= LockGrabbableWithRotation;
                    SmoothLocomotion.OnAfterMove -= UnlockGrabbable;
                }
                else if (GrabPhysics == GrabPhysics.Kinematic && ParentToHands == false) {
                    SmoothLocomotion.OnBeforeMove -= LockGrabbable;
                    SmoothLocomotion.OnAfterMove -= UnlockGrabbable;
                }

                // Reset Lock Events
                lockRequests = 0;

                subscribedToEvents = false;
            }
        }

        private Transform _priorParent;

        private Vector3 _priorLocalOffsetPosition;
        private Quaternion _priorLocalOffsetRotation;

        private Grabber _priorPrimaryGrabber;
        bool lockPos, lockRot;
        int lockRequests = 0;

        public virtual void LockGrabbable() {
            // By default only lock position
            LockGrabbable(true, false, false);
        }

        // Lock both position and rotation
        public virtual void LockGrabbableWithRotation() {
            LockGrabbable(true, true, true);
        }

        public virtual void RequestLockGrabbable() {

            // Don't do anything if recent collision
            if(RecentlyCollided) {
                return;
            }

            lockRequests++;

            if (lockRequests == 1) {
                if (_priorPrimaryGrabber != null) {
                    // Lock via parenting
                    // Store position as well as parenting
                    _priorParent = transform.parent;
                    transform.parent = _priorPrimaryGrabber.transform;
                }
            }

            if (lockRequests > 0) {
                if (_priorPrimaryGrabber != null) {

                    _priorParent = transform.parent;
                    transform.parent = _priorPrimaryGrabber.transform;

                    // Store latest position offset
                    _priorLocalOffsetPosition = _priorPrimaryGrabber.transform.InverseTransformPoint(transform.position);
                }
            }
        }

        public virtual void RequestUnlockGrabbable() {

            // Don't do anything if recent collision
            if (RecentlyCollided) {
                return;
            }

            ResetLockResets();
        }

        public virtual void ResetLockResets() {
            if (lockRequests > 0) {

                if (transform.parent != _priorParent) {
                    transform.parent = _priorParent;
                }

                lockRequests = 0;
            }
        }

        /// <summary>
        /// Keep the Grabbable's position and /or rotation in place
        /// </summary>
        public virtual void LockGrabbable(bool lockPosition, bool lockRotation, bool overridePriorLock) {

            if (BeingHeld && (!grabbableIsLocked || overridePriorLock)) {

                if (_priorPrimaryGrabber != null) {

                    lockPos = lockPosition;
                    lockRot = lockRotation;

                    // Lock via parenting
                    if (lockPosition && lockRotation) {
                        // Store position as well as parenting
                        _priorLocalOffsetPosition = _priorPrimaryGrabber.transform.InverseTransformPoint(transform.position);

                        _priorParent = transform.parent;
                        transform.parent = _priorPrimaryGrabber.transform;
                    }
                   //  Individual locking
                    else {
                        if (lockPos) {
                            _priorLocalOffsetPosition = _priorPrimaryGrabber.transform.InverseTransformPoint(transform.position);
                        }

                        if (lockRot) {
                            _priorLocalOffsetRotation = Quaternion.FromToRotation(transform.forward, _priorPrimaryGrabber.transform.forward);
                        }
                    }

                    grabbableIsLocked = true;
                }
            }
        }

        /// <summary>
        /// Allow the Grabbable to move
        /// </summary>
        public virtual void UnlockGrabbable() {
            if (BeingHeld && grabbableIsLocked) {
                // Use parenting if both position and rotation are to be locked
                if(lockPos && lockRot) {
                    Vector3 dest = _priorPrimaryGrabber.transform.TransformPoint(_priorLocalOffsetPosition);
                    float dist = Vector3.Distance(transform.position, dest);
                    // Only move if gone far enough
                    if (dist > 0.001f) {
                        transform.position = _priorPrimaryGrabber.transform.TransformPoint(_priorLocalOffsetPosition);
                    }

                    // Only reparent if necessary
                    if(transform.parent != _priorParent) {
                        transform.parent = _priorParent;
                    }
                }
                else {
                    if (lockPos) {
                        Vector3 dest = _priorPrimaryGrabber.transform.TransformPoint(_priorLocalOffsetPosition);
                        float dist = Vector3.Distance(transform.position, dest);
                        // Only move if gone far enough
                        if (dist > 0.0005f) {
                            transform.position = dest;
                        }
                    }

                    if (lockRot) {
                        transform.rotation = _priorPrimaryGrabber.transform.rotation * _priorLocalOffsetRotation;
                    }
                }

                grabbableIsLocked = false;
            }
        }

        /// <summary>
        /// You can comment this function out if you don't need precise contacts. Otherwise this is necessary to check for world collisions while being held
        /// </summary>
        /// <param name="collision"></param>
        private void OnCollisionStay(Collision collision) {

            // Can bail early
            if (!BeingHeld) {
                return;
            }

            for (int x = 0; x < collision.contacts.Length; x++) {
                ContactPoint contact = collision.contacts[x];
                // Keep track of how many objects we are colliding with
                if (BeingHeld && IsValidCollision(contact.otherCollider) && !collisions.Contains(contact.otherCollider)) {
                    collisions.Add(contact.otherCollider);
                }
            }
        }

        private void OnCollisionEnter(Collision collision) {
            // Keep track of how many objects we are colliding with
            if (BeingHeld && IsValidCollision(collision) && !collisions.Contains(collision.collider)) {
                collisions.Add(collision.collider);
            }
        }

        private void OnCollisionExit(Collision collision) {
            // We only care about collisions when being held, so we can skip this check otherwise
            if (BeingHeld && collisions.Contains(collision.collider)) {
                collisions.Remove(collision.collider);
            }
        }

        bool quitting = false;
        void OnApplicationQuit() {
            quitting = true;
        }

        void OnDestroy() {
            if(BeingHeld && !quitting) {
                DropItem(false, false);
            }
        }

        void OnDrawGizmosSelected() {
            // Show Grip Points
            Gizmos.color = new Color(0, 1, 0, 0.5f);

            if (GrabPoints != null && GrabPoints.Count > 0) {
                for (int i = 0; i < GrabPoints.Count; i++) {
                    Transform p = GrabPoints[i];
                    if (p != null) {
                        Gizmos.DrawSphere(p.position, 0.02f);
                    }
                }
            }
            else {
                Gizmos.DrawSphere(transform.position, 0.02f);
            }
        }     
    }

    #region enums
    public enum GrabType {
        Snap,
        Precise
    }

    public enum RemoteGrabMovement {
        Linear,
        Velocity,
        Flick
    }

    public enum GrabPhysics {
        None = 2,
        PhysicsJoint = 0,
        FixedJoint = 3,
        Velocity = 4,
        Kinematic = 1
    }

    public enum OtherGrabBehavior {
        None,
        SwapHands,
        DualGrab
    }

    public enum TwoHandedPositionType {
        Lerp,
        None
    }

    public enum TwoHandedRotationType {
        Lerp,
        Slerp,
        LookAtSecondary,
        None
    }

    public enum TwoHandedDropMechanic {
        Drop,
        Transfer,
        None
    }

    public enum TwoHandedLookDirection {
        Horizontal,
        Vertical        
    }

    public enum HandPoseType {
        AnimatorID,
        HandPose,
        AutoPoseOnce,
        AutoPoseContinuous,
        None
    }

    #endregion
}