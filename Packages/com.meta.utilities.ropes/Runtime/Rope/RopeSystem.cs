// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using SplineEditor;
using UnityEngine;
using UnityEngine.Events;
using Oculus.Interaction.Input;





#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.Utilities.Ropes
{
    /// <summary>
    /// The rope system, used for all interactable ropes in the game. This script works in conjunction with BurstRope
    /// to provide generally a stable, interactive rope simulation with the ability to wrap the rope around cleats and
    /// other objects
    /// 
    /// The system itself uses spherecasts and depenetration calculations to determine how and where the rope should
    /// snag and wrap around various objects. This is mostly smoke and mirrors but neccessary since the player's hands
    /// exert quite a bit of force, which the verlet rope cannot handle. Joint-based constraints are used to limit how
    /// far the player can pull the rope when it gets caught on or wraps around physical objects in the game
    /// </summary>
    public class RopeSystem : MonoBehaviour
    {
        #region Types

        public enum AnchorType
        {
            Start, // The start of the rope
            Dynamic, // Any dynamic objects attached to the rope (hands, the buouy, etc...)
            Bend, // A bend in the rope (whenever wrapping around objects)
            End // The end of the rope, which is almost always hanging loose
        }

        /// <summary>
        /// Anchors are artificial restrictions on the rope's movement that bind it to certain locations. All wrapping and tying is based on this concept
        /// </summary>
        [Serializable]
        public class Anchor
        {
            public AnchorType Type;
            public Vector3 Position;
            public float Mass = 0.5f;
            public float Proportion;
            public bool Fixed;
            public bool HasCollision;
            public Rigidbody PinToRigidbody;

            [NonSerialized] public bool UseExistingObject;
            [NonSerialized] public GameObject GameObject;
            [NonSerialized] public Rigidbody Rigidbody;
            [NonSerialized] public SphereCollider Collider;
            [NonSerialized] public ConfigurableJoint Constraint;

            public int RopeBindIndex;
            public float BindDistance;
            public Vector3 BindAxis;
            public Collider BendCollider;
            public Vector3 BendNormal;
            public Vector3 BendAxis;
        }

        #endregion

        #region Properties

        public BurstRope RopeSimulation => m_ropeSimulation;

        /// <summary>
        /// The total amount of rope spooled/dispensed so far normalized between [0,1]
        /// </summary>
        public float TotalAmountSpooled => m_maximumSpooledLength > 0 ? Mathf.Clamp01(1 - m_spooledLength / m_maximumSpooledLength) : 0;

        /// <summary>
        /// The amount of rope that is current slack (i.e. not as tight as it could be) as a percentage
        /// </summary>
        public float TotalSlackPercentage
        {
            get
            {
                float currentTotalLength = 0;
                for (var i = 0; i < m_anchors.Count - 1; i++)
                {
                    var length = Vector3.Distance(m_anchors[i].Position, m_anchors[i + 1].Position);
                    currentTotalLength += length;
                }

                return Mathf.Clamp01(1 - currentTotalLength / (m_totalLength - m_spooledLength));
            }
        }

        /// <summary>
        /// The total number of bend anchors current present
        /// </summary>
        public int TotalBendAnchors
        {
            get
            {
                var count = 0;
                foreach (var anchor in m_anchors)
                {
                    if (anchor.Type == AnchorType.Bend) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// The amount of revolutions present in the rope in degrees
        /// </summary>
        public float TotalBendRevolutions
        {
            get
            {
                var total = 0.0f;
                for (var i = 1; i < m_anchors.Count - 1; i++)
                {
                    var prevAnchor = m_anchors[i - 1];
                    var anchor = m_anchors[i];
                    var nextAnchor = m_anchors[i + 1];

                    var dir1 = (anchor.Position - prevAnchor.Position).normalized;
                    var dir2 = (nextAnchor.Position - anchor.Position).normalized;
                    total += Vector3.Angle(dir1, dir2);
                }
                return total / 360.0f;
            }
        }

        /// <summary>
        /// Does the rope think it's tied?
        /// </summary>
        public bool Tied { get; private set; }

        #endregion

        [Header("Rope Behavior"), Space]

        [SerializeField, Tooltip("The current anchors that make up the rope's constraints")] private List<Anchor> m_anchors = new();

        [SerializeField, Tooltip("The radius of the rope")] private float m_radius = 0.05f;

        [SerializeField, Tooltip("The total length of the rope, including any spooling")] private float m_totalLength = 1;

        public float TotalLength => m_totalLength;

        [SerializeField, Tooltip("How much of the rope is allowed to be 'spooled' (i.e. hidden from view)")] private float m_maximumSpooledLength;

        [SerializeField, Tooltip("How much of the rope is currently spooled")] private float m_spooledLength = 5;

        [SerializeField, Tooltip("How quickly the rope will retract back into the spool when not being interacted with")] private float m_spoolRetractRate = 0.25f;

        [SerializeField, Tooltip("How often spooling will trigger an event (used for sound fx mostly)")] private float m_distancePerSpoolEvent = 0.2f;

        [SerializeField, Tooltip("The force threshold for when spooling and hand slipping occurs")] private float m_slipForce = 100;
        [SerializeField, Tooltip("The speed at which the rope with spool or hands will sleep when force is exceeded")] private float m_slipSpeed = 0.01f;

        [SerializeField, Tooltip("The minimum range of angles that a rope bend can occur at")] private float m_minimumBendAngle = 25;
        [SerializeField, Tooltip("The maximum range of angles that a rope bend can occur at")] private float m_maximumBendAngle = 65;
        [SerializeField, Tooltip("Prevents new bends from being created")] private bool m_disableNewBends = false;

        [SerializeField, Tooltip("Alters the burst rope node distance constraint relative to how many nodes would normally be needed (lower = tighter rope)")] private float m_nodeDistanceMultiplier = 0.75f;

        [SerializeField, Tooltip("The default physics material for dynamic anchors")] private PhysicsMaterial m_defaultPhysicsMaterial;

        [SerializeField] private bool m_autoGenerateAnchors;

        [Space]
        [Header("Layers"), Space]
        [SerializeField, Tooltip("The layer mask for detecing snags / bends")] private LayerMask m_layerMask;
        [SerializeField, Tooltip("The layer for dynamic anchors")] private int m_ropeLayer;

        [Space]
        [Header("Tying"), Space]

        [SerializeField, Tooltip("The minimum amount of slack for the rope to be considered tied")] private float m_tiedSlackMin;

        [SerializeField, Tooltip("The maximum amount of slack for the rope to be considered tied")] private float m_tiedSlackMax;

        [SerializeField, Tooltip("The minimum number of revolutions (turns) required for the rope to be considered tied")] private float m_tiedRevolutionsMin;

        [SerializeField, Tooltip("The maximum number of revolutions (turns) required for the rope to be considered tied")] private float m_tiedRevolutionsMax;

        [SerializeField, Tooltip("The threshold required for the rope to be considered 'spooled'")] private float m_spooledThreshold;

        [Space]
        [Header("References"), Space]
        [SerializeField, Tooltip("The burst rope simulation to use")] private BurstRope m_ropeSimulation;

        [SerializeField, Tooltip("The tube renderer to use for drawing this rope")] private TubeRenderer m_tubeRenderer;

        [SerializeField, AutoSet] private BezierSpline m_spline;

        [SerializeField, AutoSet] private Rigidbody m_rigidbody;

        [SerializeField] protected RopeGrabAnchor[] m_grabAnchors;

        #region Events

        [Space]                     //Dylan - Changed these events to public so that the narrative system can interface with them
        [Header("Events"), Space]

        [Tooltip("Triggered when the rope is wrapped around something")]
        [SerializeField] private UnityEvent m_onRopeTied;

        [Tooltip("Triggered when the rope is unwrapped from something")]
        [SerializeField] private UnityEvent m_onRopeUntied;

        [Tooltip("Triggered when the rope is grabbed by the player")]
        [SerializeField] private UnityEvent m_onRopeGrabbed;

        [Tooltip("Triggered when the rope is released by the player")]
        [SerializeField] private UnityEvent m_onRopeReleased;

        [Tooltip("Triggered when more rope is pulled out, the value is how much rope has been pulled out in total (from zero to one)")]
        [SerializeField] private UnityEvent<float> m_onRopeSpooled;

        [Tooltip("Triggered when the rope slips through the players hand, the value is the amount of slipping in meters this frame")]
        [SerializeField] private UnityEvent<float> m_onRopeSlipped;

        [Tooltip("Triggered when more rope is spooled past the specified threshold and also tied")]
        [SerializeField] private UnityEvent m_onRopeSpooledAndTied;

        [Tooltip("Triggered when more rope is pulled out, but only every distancePerSpooledEvent units")]
        [SerializeField] private UnityEvent<float> m_onRopeSpooledChunk;

        [Tooltip("Triggered when the rope a rope is bent around a corner")]
        [SerializeField] private UnityEvent m_onRopeBendCreated;

        #endregion

        #region Private Fields


        private LineRenderer m_lineRenderer;
        private List<Anchor> m_anchorsCopy = new();
        private CapsuleCollider m_dummyCapsule;
        private SphereCollider m_dummySphere;
        private float m_spoolDistanceAccumulator;

        #endregion

        [SerializeField] protected SyntheticHand m_leftHand, m_rightHand;
        protected virtual void SetupHandRefs()
        {
            // Set the references to the left and right hands its easier to define these elsewhere but this is also fine
            m_grabAnchors[0].Hand = m_leftHand;
            m_grabAnchors[1].Hand = m_rightHand;
        }

        private void Start()
        {
            m_lineRenderer = GetComponent<LineRenderer>();

            var i = 0;
            foreach (var anchor in m_anchors)
            {
                SetupAnchor(anchor);
                i++;
            }
            SetupConstraints();

            var test = new GameObject("DummyCapsule");
            test.transform.parent = transform;
            m_dummyCapsule = test.AddComponent<CapsuleCollider>();
            m_dummyCapsule.enabled = false;
            m_dummyCapsule.direction = 2;

            m_dummySphere = test.AddComponent<SphereCollider>();
            m_dummySphere.enabled = false;

            m_tubeRenderer.ResizeTube(m_ropeSimulation.NodeCount);

            SetupHandRefs();

            var nodeDistance = m_totalLength / m_ropeSimulation.NodeCount;
            m_ropeSimulation.NodeDistance = nodeDistance * m_nodeDistanceMultiplier;
        }

        /// <summary>
        /// Simulates the rope in editor and updates the renderer mesh
        /// </summary>
        [ContextMenu("Setup (Burst Rope + Mesh)")]
        private void SetupInEditorBurstMesh()
        {
            UpdateBurstRope();
            m_ropeSimulation.Simulate(1000);
            m_ropeSimulation.UpdateRenderer();
        }

        /// <summary>
        /// Sets up the rope using the attached spline component, detecting where any bends are required as well as calculating the correct total length and node distance
        /// </summary>
        [ContextMenu("Setup")]
        private void SetupInEditor()
        {
            if (m_anchors.Count >= 2 && m_autoGenerateAnchors && m_spline)
            {
                m_anchors.Clear();

                m_anchors.Add(new Anchor
                {
                    Position = m_spline.GetPoint(0, false),
                    Type = AnchorType.Start,
                    RopeBindIndex = Mathf.FloorToInt(m_spooledLength / m_totalLength * (m_ropeSimulation.NodeCount - 1)),
                });

                var colliders = new Collider[64];

                var splineLength = m_spline.GetQuadraticLength();

                m_totalLength = m_maximumSpooledLength + splineLength;

                var nodeDistance = m_totalLength / m_ropeSimulation.NodeCount;

                m_ropeSimulation.NodeDistance = nodeDistance * m_nodeDistanceMultiplier;

                SetStaticRopePositions();

                var prevAnchor = m_anchors[0];

                var tempCollider = gameObject.AddComponent<SphereCollider>();
                tempCollider.radius = m_radius;
                tempCollider.center = Vector3.zero;
                tempCollider.enabled = false;

                var index = 0;
                foreach (var node in m_ropeSimulation.ReadableNodes)
                {
                    var count = Physics.OverlapSphereNonAlloc(transform.TransformPoint(node.Position), m_radius * 1.5f, colliders, m_layerMask);
                    if (count > 0)
                    {
                        var nearestPoint = colliders[0].ClosestPoint(transform.TransformPoint(node.Position));

                        tempCollider.enabled = true;
                        if (Physics.ComputePenetration(tempCollider, nearestPoint, Quaternion.identity, colliders[0], colliders[0].transform.position, colliders[0].transform.rotation, out var dir, out var dist))
                        {
                            var point = transform.InverseTransformPoint(nearestPoint + dir * dist);
                            var distanceToPrev = Vector3.Distance(prevAnchor.Position, point);

                            if (distanceToPrev >= nodeDistance)
                            {
                                m_anchors.Add(new Anchor
                                {
                                    Position = point,
                                    Type = AnchorType.Bend,
                                    RopeBindIndex = index,
                                    BendCollider = colliders[0],
                                    BendNormal = dir,
                                });
                                prevAnchor = m_anchors[^1];
                            }
                        }
                        tempCollider.enabled = false;
                        // TODO: generate other required bend data
                    }
                    index++;
                }

                DestroyImmediate(tempCollider);

                m_anchors.Add(new Anchor
                {
                    Position = m_spline.GetPoint(1, false),
                    Type = AnchorType.End,
                    Mass = 0.1f,
                    RopeBindIndex = m_ropeSimulation.NodeCount - 1,
                });

                var exposedProportion = (m_totalLength - m_spooledLength) / m_totalLength;
                var totalProportions = 0f;

                for (var i = 0; i < m_anchors.Count - 1; i++)
                {
                    var anchor = m_anchors[i];
                    var nextAnchor = m_anchors[i + 1];
                    var proportion = (nextAnchor.RopeBindIndex - anchor.RopeBindIndex) / (float)(m_ropeSimulation.NodeCount - 1);
                    anchor.Proportion = proportion;

                    if (anchor.Type == AnchorType.Bend)
                    {
                        prevAnchor = m_anchors[i - 1];
                        var dir1 = (anchor.Position - prevAnchor.Position).normalized;
                        var dir2 = (nextAnchor.Position - anchor.Position).normalized;
                        anchor.BendAxis = Vector3.Cross(dir1, dir2).normalized;
                    }

                    totalProportions += anchor.Proportion;
                }

                foreach (var anchor in m_anchors)
                {
                    anchor.Proportion = anchor.Proportion / totalProportions * exposedProportion;
                }
            }

            UpdateBurstRope();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(m_ropeSimulation);
            EditorUtility.SetDirty(m_tubeRenderer);
#endif

            //m_ropeSimulation.Simulate(1000);

            m_ropeSimulation.UpdateRenderer();
        }

        /// <summary>
        /// Updates the mesh renderer but does not do any simulation or setup
        /// </summary>
        [ContextMenu("UpdateRenderer")]
        private void UpdateRenderer()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(m_ropeSimulation);
            EditorUtility.SetDirty(m_tubeRenderer);
#endif
            m_ropeSimulation.UpdateRenderer();
        }

        private void OnEnable()
        {
            m_ropeSimulation.enabled = true;
            m_tubeRenderer.enabled = true;
            foreach (var grabAnchor in m_grabAnchors) grabAnchor.enabled = true;
            foreach (var anchor in m_anchors)
            {
                if (anchor.Type == AnchorType.Dynamic && !anchor.UseExistingObject && anchor.Rigidbody != null)
                {
                    anchor.Rigidbody.isKinematic = false;
                    anchor.Collider.enabled = anchor.HasCollision;
                }
            }
        }

        private void OnDisable()
        {
            m_ropeSimulation.enabled = false;
            m_tubeRenderer.enabled = false;
            foreach (var grabAnchor in m_grabAnchors) grabAnchor.enabled = false;
            foreach (var anchor in m_anchors)
            {
                if (anchor.Type == AnchorType.Dynamic && !anchor.UseExistingObject && anchor.Rigidbody != null)
                {
                    anchor.Rigidbody.isKinematic = true;
                    anchor.Collider.enabled = false;
                }
            }
        }

        public void OnRopeGrabbed()
        {
            m_onRopeGrabbed.Invoke();
        }

        public void OnRopeReleased()
        {
            m_onRopeReleased.Invoke();
        }

        private void TransferAnchorProportions(Anchor anchorA, Anchor anchorB, float amount)
        {
            amount /= m_totalLength;
            amount = Mathf.Min(amount, anchorB.Proportion);

            anchorA.Proportion += amount;
            anchorB.Proportion -= amount;
            if (anchorA.Constraint is not null)
                anchorA.Constraint.linearLimit = new SoftJointLimit { bounciness = 0, contactDistance = 0, limit = m_totalLength * anchorA.Proportion };

            if (anchorB.Constraint is not null)
                anchorB.Constraint.linearLimit = new SoftJointLimit { bounciness = 0, contactDistance = 0, limit = m_totalLength * anchorB.Proportion };
        }

        private void SetupAnchor(Anchor anchor)
        {
            if (anchor.UseExistingObject) return;

            switch (anchor.Type)
            {
                case AnchorType.Start:
                    anchor.Rigidbody = m_rigidbody;
                    break;
                case AnchorType.Dynamic:
                    anchor.GameObject = new GameObject($"Anchor_{m_anchors.IndexOf(anchor)}_(Dynamic)");
                    anchor.Collider = anchor.GameObject.AddComponent<SphereCollider>();
                    anchor.Collider.enabled = anchor.HasCollision;
                    anchor.Collider.radius = m_radius * 1.5f;
                    anchor.Collider.sharedMaterial = m_defaultPhysicsMaterial;
                    anchor.Rigidbody = anchor.GameObject.AddComponent<Rigidbody>();
                    anchor.Rigidbody.mass = anchor.Mass;
                    anchor.Rigidbody.linearDamping = 1.0f;
                    anchor.Rigidbody.angularDamping = 10.0f;
                    anchor.Rigidbody.freezeRotation = true;
                    anchor.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    anchor.GameObject.transform.parent = transform;
                    anchor.GameObject.transform.position = transform.TransformPoint(anchor.Position);
                    anchor.GameObject.layer = m_ropeLayer;

                    if (anchor.PinToRigidbody)
                    {
                        var fixedJoint = anchor.GameObject.AddComponent<FixedJoint>();
                        fixedJoint.connectedBody = anchor.PinToRigidbody;
                        anchor.Rigidbody.freezeRotation = false;
                    }

                    break;
                case AnchorType.Bend:
                    anchor.Rigidbody = m_rigidbody;
                    break;
                default:
                    break;
            }
        }

        private void SetupDynamicConstraint(Anchor anchorA, Anchor anchorB, float length)
        {
            // Reconfigure existing constraint
            if (anchorA.Constraint)
            {
                anchorA.Constraint.connectedBody = anchorB.Rigidbody;
                anchorA.Constraint.linearLimit = new SoftJointLimit { bounciness = 0, contactDistance = 0, limit = m_totalLength * anchorA.Proportion };
            }
            else
            {
                anchorA.Constraint = (anchorA.GameObject ? anchorA.GameObject : gameObject).AddComponent<ConfigurableJoint>();
                anchorA.Constraint.autoConfigureConnectedAnchor = false;
                anchorA.Constraint.connectedBody = anchorB.Rigidbody;
                anchorA.Constraint.linearLimit = new SoftJointLimit { bounciness = 0, contactDistance = 0, limit = m_totalLength * anchorA.Proportion };
                anchorA.Constraint.linearLimitSpring = new SoftJointLimitSpring { spring = 10000, damper = 100 };
                anchorA.Constraint.xMotion = ConfigurableJointMotion.Limited;
                anchorA.Constraint.yMotion = ConfigurableJointMotion.Limited;
                anchorA.Constraint.zMotion = ConfigurableJointMotion.Limited;
            }

            anchorA.Constraint.anchor = anchorA.Type != AnchorType.Dynamic ? anchorA.Position : Vector3.zero;
            anchorA.Constraint.connectedAnchor = anchorB.Type != AnchorType.Dynamic ? anchorB.Position : Vector3.zero;
        }

        private void SetupConstraint(Anchor anchorA, Anchor anchorB, float length = -1)
        {
            if (anchorA.Type == AnchorType.Start && anchorB.Type == AnchorType.Dynamic)
            {
                SetupDynamicConstraint(anchorA, anchorB, length);
            }
            else if (anchorA.Type == AnchorType.Dynamic && anchorB.Type == AnchorType.Dynamic)
            {
                SetupDynamicConstraint(anchorA, anchorB, length);
            }
            else if (anchorA.Type == AnchorType.Dynamic && anchorB.Type == AnchorType.Bend)
            {
                SetupDynamicConstraint(anchorA, anchorB, length);
            }
            else if (anchorA.Type == AnchorType.Bend && anchorB.Type == AnchorType.Dynamic)
            {
                SetupDynamicConstraint(anchorA, anchorB, length);
            }
            else
            {
                // Remove existing constraint
                if (anchorA.Constraint)
                {
                    Destroy(anchorA.Constraint);
                    anchorA.Constraint = null;
                }
            }
        }

        private void SetupConstraints()
        {
            for (var i = 0; i < m_anchors.Count - 1; i++)
            {
                SetupConstraint(m_anchors[i], m_anchors[i + 1]);
            }
        }

        public Anchor CreateAnchor(AnchorType type, Vector3 point, int index, GameObject existingObject = null)
        {
            var previousAnchor = m_anchors[index];
            var nextAnchor = m_anchors[index + 1];

            var lengthA = Vector3.Distance(previousAnchor.Position, point);
            var lengthB = Vector3.Distance(point, nextAnchor.Position);
            var total = lengthA + lengthB;

            // Normalise lengths
            lengthA /= total;
            lengthB /= total;

            var originalProportion = previousAnchor.Proportion;

            previousAnchor.Proportion = originalProportion * lengthA;

            var anchor = new Anchor
            {
                Type = type,
                Position = point,
                Proportion = originalProportion * lengthB
            };

            if (existingObject is not null)
            {
                anchor.UseExistingObject = true;
                anchor.GameObject = existingObject;
                anchor.Collider = existingObject.GetComponent<SphereCollider>();
                anchor.Rigidbody = existingObject.GetComponent<Rigidbody>();
            }

            m_anchors.Insert(index + 1, anchor);

            SetupAnchor(anchor);
            SetupConstraint(previousAnchor, anchor);
            SetupConstraint(anchor, nextAnchor);

            return anchor;
        }

        public Anchor CreateAnchorViaRopeSim(AnchorType type, Vector3 point, int index, GameObject existingObject = null)
        {
            var anchorIndex = -1;
            for (var i = 0; i < m_anchors.Count - 1; i++)
            {
                if (m_anchors[i].RopeBindIndex <= index && m_anchors[i + 1].RopeBindIndex >= index)
                {
                    anchorIndex = i;
                    break;
                }
            }

            // Unable to find anchor index
            if (anchorIndex == -1) return null;

            var previousAnchor = m_anchors[anchorIndex];
            var nextAnchor = m_anchors[anchorIndex + 1];

            if (previousAnchor.RopeBindIndex == nextAnchor.RopeBindIndex) return null;

            var lengthA = (float)(index - previousAnchor.RopeBindIndex);
            var lengthB = (float)(nextAnchor.RopeBindIndex - index);
            var total = lengthA + lengthB;

            // Normalise lengths
            lengthA /= total;
            lengthB /= total;

            var originalProportion = previousAnchor.Proportion;

            previousAnchor.Proportion = originalProportion * lengthA;

            var anchor = new Anchor
            {
                Type = type,
                Position = point,
                Proportion = originalProportion * lengthB,
                RopeBindIndex = index,
            };

            if (existingObject is not null)
            {
                anchor.UseExistingObject = true;
                anchor.GameObject = existingObject;
                anchor.Collider = existingObject.GetComponent<SphereCollider>();
                anchor.Rigidbody = existingObject.GetComponent<Rigidbody>();
            }

            m_anchors.Insert(anchorIndex + 1, anchor);

            SetupAnchor(anchor);
            SetupConstraint(previousAnchor, anchor);
            SetupConstraint(anchor, nextAnchor);

            return anchor;
        }

        public Anchor CreateAnchor(Ray ray, AnchorType type)
        {
            return ClosestPointToRay(ray, out var point, out var _, out var index) ? CreateAnchor(type, point, index) : null;
        }

        public Anchor CreateAnchorViaRopeSim(Vector3 point, AnchorType type, GameObject existingObject = null)
        {
            return CreateAnchorViaRopeSim(type, transform.InverseTransformPoint(point), m_ropeSimulation.ClosestIndexToPoint(point), existingObject);
        }

        public bool GetPrevAndNextAnchors(Anchor anchor, out Anchor prevAnchor, out Anchor nextAnchor)
        {
            var index = m_anchors.IndexOf(anchor);
            if (index == -1)
            {
                prevAnchor = null;
                nextAnchor = null;
                return false;
            }

            prevAnchor = index > 0 ? m_anchors[index - 1] : null;
            nextAnchor = index < m_anchors.Count - 1 ? m_anchors[index + 1] : null;
            return true;
        }

        public void DestroyAnchor(Anchor anchor)
        {
            // Sanity check
            var index = m_anchors.IndexOf(anchor);

            if (anchor.Type != AnchorType.Start && index > 0 && index < m_anchors.Count - 1)
            {
                m_anchors.RemoveAt(index);

                if (anchor.Constraint is not null) Destroy(anchor.Constraint);
                if (!anchor.UseExistingObject && anchor.GameObject) Destroy(anchor.GameObject); // TODO: cache gameobjects and constraints to prevent GC alloc                

                var previousAnchor = m_anchors[index - 1];
                var nextAnchor = m_anchors[index];

                // Restore original proportion (relative rope length
                previousAnchor.Proportion += anchor.Proportion;

                SetupConstraint(previousAnchor, nextAnchor);
            }
        }

        private bool CheckCollisionBetween(Vector3 point1, Vector3 point2, int index)
        {
            var direction = (point2 - point1).normalized;
            var length = Vector3.Distance(point2, point1);
            if (Physics.SphereCast(transform.TransformPoint(point1), m_radius - 0.01f, transform.TransformDirection(direction), out var hit, length, m_layerMask))
            {
                m_dummyCapsule.height = length;
                m_dummyCapsule.radius = m_radius;

                var p1 = transform.TransformPoint(point1);
                var p2 = transform.TransformPoint(point2);

                var midPoint = (p1 + p2) / 2;
                var rotation = Quaternion.LookRotation(transform.TransformDirection(direction));

                m_dummyCapsule.transform.position = midPoint;
                m_dummyCapsule.transform.rotation = rotation;
                m_dummyCapsule.enabled = true;
                if (Physics.ComputePenetration(hit.collider,
                                               hit.transform.position,
                                               hit.transform.rotation,
                                               m_dummyCapsule,
                                               midPoint,
                                               rotation, out var dir, out var dist))
                {

                    if (Physics.CapsuleCast(p1 - dir * (dist + 0.01f), p2 - dir * (dist + 0.01f), m_radius, dir, out var hit2, dist + 0.02f, m_layerMask))
                    {
                        if (hit2.collider == hit.collider)
                        {
                            var bendPoint = transform.InverseTransformPoint(hit2.point + hit2.normal * m_radius);

                            var dir1 = (bendPoint - point1).normalized;
                            var dir2 = (point2 - bendPoint).normalized;
                            var bendAngle = Vector3.Angle(dir1, dir2);
                            var normal = transform.InverseTransformDirection(hit2.normal);

                            // Bends should curve around the normal (i.e. they must be convex)
                            if (Vector3.Dot(dir1, normal) < 0 || Vector3.Dot(dir2, normal) > 0) return false;

                            if (bendAngle >= m_minimumBendAngle && bendAngle < m_maximumBendAngle)
                            {
                                var bendNormalDot = Vector3.Dot(Vector3.Cross(dir1, dir2).normalized, normal);

                                if (bendNormalDot < 0.25f)
                                {
                                    var newAnchor = CreateAnchor(AnchorType.Bend, bendPoint, index);
                                    newAnchor.BendAxis = Vector3.Cross(dir1, dir2).normalized;
                                    newAnchor.BendCollider = hit.collider;
                                    newAnchor.BendNormal = normal;
                                    m_dummyCapsule.enabled = false;
                                    m_onRopeBendCreated?.Invoke();
                                    return true;
                                }
                            }
                        }
                    }
                }
                m_dummyCapsule.enabled = false;
            }
            return false;
        }

        private bool CheckCollisionBetween(Anchor anchorA, Anchor anchorB, int index)
        {
            for (var i = anchorA.RopeBindIndex; i < anchorB.RopeBindIndex; i++)
            {
                var worldPoint = m_ropeSimulation.FromRopeSpace(m_ropeSimulation.ReadableNodes[i].Position);
                var results = Physics.OverlapSphere(worldPoint, m_radius + 0.01f, m_layerMask, QueryTriggerInteraction.Ignore);
                foreach (var result in results)
                {
                    var closest = result.ClosestPoint(worldPoint);
                    var normal = (worldPoint - closest).normalized;

                    var bendPoint = transform.InverseTransformPoint(worldPoint);

                    var dir1 = (bendPoint - anchorA.Position).normalized;
                    var dir2 = (anchorB.Position - bendPoint).normalized;
                    var bendAngle = Vector3.Angle(dir1, dir2);
                    normal = transform.InverseTransformDirection(normal);

                    // Bends should curve around the normal (i.e. they must be convex)
                    if (Vector3.Dot(dir1, normal) < 0 || Vector3.Dot(dir2, normal) > 0) return false;

                    if (bendAngle >= m_minimumBendAngle && bendAngle < m_maximumBendAngle)
                    {
                        var bendNormalDot = Vector3.Dot(Vector3.Cross(dir1, dir2).normalized, normal);

                        if (bendNormalDot < 0.25f)
                        {
                            var newAnchor = CreateAnchor(AnchorType.Bend, bendPoint, index);
                            newAnchor.BendAxis = Vector3.Cross(dir1, dir2).normalized;
                            newAnchor.BendCollider = result;
                            newAnchor.BendNormal = normal;
                            m_dummyCapsule.enabled = false;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check for bends between two anchors
        /// </summary>
        /// <param name="anchor">The anchor to check</param>
        /// <param name="prevAnchor">The next anchor in the system (if there is one)</param>
        /// <param name="nextAnchor">The previous anchor in the system (if there is one)</param>
        /// <param name="i">The index of the anchor</param>
        /// <returns></returns>
        private bool CheckForBends(Anchor anchor, Anchor prevAnchor, Anchor nextAnchor, int i)
        {
            if (!m_disableNewBends && nextAnchor.Type == AnchorType.Dynamic)
            {
                var length = anchor.Proportion * m_totalLength;
                var currentLength = Vector3.Distance(anchor.Position, nextAnchor.Position);
                var tightness = currentLength / length;

                if (tightness > 0.9f)
                {
                    if (CheckCollisionBetween(anchor.Position, nextAnchor.Position, i))
                    {
                        return true;
                    }
                }
                else
                {
                    var midPoint = m_ropeSimulation.ReadableNodes[(anchor.RopeBindIndex + nextAnchor.RopeBindIndex) / 2].Position;

                    if (CheckCollisionBetween(anchor.Position, midPoint, i) || CheckCollisionBetween(midPoint, nextAnchor.Position, i))
                    {
                        return true;
                    }
                }
            }

            if (anchor.Fixed)
            {
                return false;
            }

            NudgeAnchor(anchor, Vector3.zero);

            if (anchor.Type == AnchorType.Bend && prevAnchor is not null && nextAnchor is not null && (prevAnchor.Type == AnchorType.Dynamic || nextAnchor.Type == AnchorType.Dynamic || nextAnchor.Type == AnchorType.End || prevAnchor.Type == AnchorType.Start))
            {
                var p1 = transform.TransformPoint(prevAnchor.Position);
                var p2 = transform.TransformPoint(anchor.Position);
                var p3 = transform.TransformPoint(nextAnchor.Position);

                var dir1 = (p2 - p1).normalized;
                var dir2 = (p3 - p2).normalized;
                var bendAxis = Vector3.Cross(dir1, dir2).normalized;
                var bendNormalDot = Vector3.Dot(bendAxis, transform.TransformDirection(anchor.BendNormal));

                if (prevAnchor.Type == AnchorType.Bend && prevAnchor.BendCollider == anchor.BendCollider)
                {
                    var bendAxisDot = Vector3.Dot(bendAxis, transform.TransformDirection(prevAnchor.BendAxis));
                    if (bendAxisDot < 0.0f)
                    {
                        DestroyAnchor(anchor);
                        return true;
                    }
                }

                if (Vector3.Dot(transform.TransformDirection(anchor.BendAxis), bendAxis) < 0 || bendNormalDot >= 0.5f)
                {
                    DestroyAnchor(anchor);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Nudges an anchor's position while keeping it stuck to it's current surface
        /// </summary>
        /// <param name="anchor">The anchor to nudge</param>
        /// <param name="offset">The amount of nudge the anchor</param>
        private void NudgeAnchor(Anchor anchor, Vector3 offset)
        {
            // Only applicable to bend anchors
            if (anchor.Type != AnchorType.Bend) return;

            var point = anchor.BendCollider.ClosestPoint(transform.TransformPoint(anchor.Position + offset));
            m_dummySphere.enabled = true;
            m_dummySphere.center = Vector3.zero;
            m_dummySphere.radius = m_radius;
            if (Physics.ComputePenetration(m_dummySphere,
                                           point,
                                           Quaternion.identity,
                                           anchor.BendCollider,
                                           anchor.BendCollider.gameObject.transform.position,
                                           anchor.BendCollider.gameObject.transform.rotation,
                                           out var dir,
                                           out var dist))
            {
                point += dir * dist;

                anchor.Position = transform.InverseTransformPoint(point);
                if (anchor.Constraint is not null)
                {
                    anchor.Constraint.anchor = anchor.Position;
                }
                anchor.BendNormal = transform.InverseTransformDirection(dir);
            }
            m_dummySphere.enabled = false;
        }


        private void UpdateAnchors(float dt)
        {
            m_anchorsCopy.Clear();
            m_anchorsCopy.AddRange(m_anchors);

            foreach (var anchor in m_anchorsCopy)
            {
                if (anchor.Type == AnchorType.Dynamic)
                {
                    anchor.Position = transform.InverseTransformPoint(anchor.GameObject.transform.position);
                    anchor.Rigidbody.WakeUp();
                }
                else if (anchor.Type == AnchorType.End)
                {
                    anchor.Position = m_ropeSimulation.ReadableNodes[^1].Position;
                }
            }

            for (var k = 0; k < m_anchors.Count; k++)
            {
                for (var l = k + 1; l < m_anchors.Count; l++)
                {
                    if (m_anchors[k].Type == AnchorType.Bend || m_anchors[l].Type == AnchorType.Bend)
                    {
                        var dir = m_anchors[k].Position - m_anchors[l].Position;
                        var dist = dir.magnitude;

                        if (dist < m_radius * 2)
                        {
                            NudgeAnchor(m_anchors[k], dir * 0.05f);
                            NudgeAnchor(m_anchors[l], dir * -0.05f);
                        }
                    }
                }
            }


            var i = 0;
            var breakLoop = false;
            foreach (var anchor in m_anchorsCopy)
            {
                var prevAnchor = i > 0 ? m_anchorsCopy[i - 1] : null;
                var nextAnchor = i < m_anchorsCopy.Count - 1 ? m_anchorsCopy[i + 1] : null;

                switch (anchor.Type)
                {
                    case AnchorType.Start:
                    case AnchorType.Bend:
                    case AnchorType.Dynamic:

                        if (nextAnchor is not null && CheckForBends(anchor, prevAnchor, nextAnchor, i))
                        {
                            i++;
                            breakLoop = true;
                            break;
                        }

                        if (nextAnchor is not null && anchor.Constraint is not null)
                        {
                            var dir = transform.TransformDirection(nextAnchor.Position - anchor.Position).normalized;
                            var force = -Vector3.Dot(anchor.Constraint.currentForce, dir);

                            // Additional force for when anchor sits between two constraints
                            if (nextAnchor.Constraint is not null)
                            {
                                var nextNextAnchor = i < m_anchors.Count - 2 ? m_anchorsCopy[i + 2] : null;
                                if (nextNextAnchor is not null)
                                {
                                    var dir2 = transform.TransformDirection(nextNextAnchor.Position - nextAnchor.Position).normalized;
                                    force -= Vector3.Dot(nextAnchor.Constraint.currentForce, -dir2);
                                }
                            }

                            // Slipping logic:
                            // When enough force (resistence to pulling the rope) is encountered, resolve in this order
                            // 1. Tighten any slack on the rope (traverse the rope backwards to find any slack) OR
                            // 2. Spool some additional rope (if there is any unspooled rope left) OR
                            // 3. Slip the hand up or down the rope (adjust proportions and constraints on the fly) OR
                            // 4. Slip off the end of the rope (ran out of rope)

                            if (m_slipForce > 0 && Mathf.Abs(force) > m_slipForce)
                            {
                                var lengthChange = -Mathf.Clamp((force - m_slipForce * Mathf.Sign(force)) / m_slipForce, -2.0f, 2.0f) * m_slipSpeed * dt;

                                // Resolve slack
                                if (i > 0 && lengthChange > 0)
                                {
                                    for (var j = i - 1; j >= 0; j--)
                                    {
                                        var currentLength = Vector3.Distance(m_anchors[j].Position, m_anchors[j + 1].Position);
                                        var length = m_anchors[j].Proportion * m_totalLength;

                                        if (length > currentLength)
                                        {
                                            var amount = Mathf.Min(lengthChange, length - currentLength) / m_totalLength;
                                            m_anchors[j].Proportion -= amount;
                                            anchor.Proportion += amount;
                                            anchor.Constraint.linearLimit = new SoftJointLimit { bounciness = 0, contactDistance = 0, limit = m_totalLength * anchor.Proportion };
                                            lengthChange = 0;
                                            break;
                                        }
                                    }
                                }

                                // Resolve spool
                                if (lengthChange != 0 && m_spooledLength > 0)
                                {
                                    lengthChange = Mathf.Min(lengthChange, m_spooledLength);
                                    m_spooledLength -= lengthChange;

                                    m_anchors[0].Proportion += lengthChange / m_totalLength;
                                    if (m_anchors[0].Constraint is not null)
                                    {
                                        m_anchors[0].Constraint.linearLimit = new SoftJointLimit { bounciness = 0, contactDistance = 0, limit = m_totalLength * m_anchors[0].Proportion };
                                    }

                                    m_onRopeSpooled.Invoke(TotalAmountSpooled);

                                    m_spoolDistanceAccumulator += lengthChange;
                                    if (m_spoolDistanceAccumulator > m_distancePerSpoolEvent)
                                    {
                                        m_spoolDistanceAccumulator = 0;
                                        m_onRopeSpooledChunk?.Invoke(TotalAmountSpooled);
                                    }

                                    if (TotalAmountSpooled > m_spooledThreshold && Tied)
                                    {
                                        m_onRopeSpooledAndTied.Invoke();
                                    }

                                    lengthChange = 0;
                                }

                                // Resolve slip
                                if (lengthChange != 0)
                                {
                                    m_onRopeSlipped.Invoke(lengthChange);

                                    TransferAnchorProportions(anchor, nextAnchor, lengthChange);
                                    // Ran out of rope, drop it
                                    if (nextAnchor.Proportion <= Mathf.Epsilon)
                                    {
                                        m_onRopeReleased.Invoke();
                                        DestroyAnchor(nextAnchor);
                                        breakLoop = true;
                                    }
                                }
                            }
                        }

                        break;
                }

                if (breakLoop) break;
                i++;
            }
        }

        private void FixedUpdate()
        {
            if (m_ropeSimulation.SubstepCount == 0 && m_ropeSimulation.WarmupSubstepsRemaining <= 0) return;

            m_ropeSimulation.CollisionRadius = m_radius;
            UpdateAnchors(Time.fixedDeltaTime);

            if (m_maximumSpooledLength > 0 && m_spoolRetractRate > 0 && m_spooledLength < m_maximumSpooledLength && m_anchors.Count == 2)
            {
                var lengthChange = m_spoolRetractRate * Time.fixedDeltaTime;
                m_anchors[0].Proportion -= lengthChange / m_totalLength;
                m_spooledLength += lengthChange;

                if (m_anchors[0].Constraint is not null)
                {
                    m_anchors[0].Constraint.linearLimit = new SoftJointLimit { bounciness = 0, contactDistance = 0, limit = m_totalLength * m_anchors[0].Proportion };
                }

                m_onRopeSpooled.Invoke(TotalAmountSpooled);

                m_spoolDistanceAccumulator += lengthChange;
                if (m_spoolDistanceAccumulator > m_distancePerSpoolEvent)
                {
                    m_spoolDistanceAccumulator = 0;
                    m_onRopeSpooledChunk?.Invoke(TotalAmountSpooled);
                }
            }

            UpdateBurstRope();
            UpdateLineRenderer();
        }

        private void Update()
        {
            if (m_ropeSimulation.SubstepCount == 0 && m_ropeSimulation.WarmupSubstepsRemaining <= 0) return;

            if (!Tied && TotalSlackPercentage < m_tiedSlackMin && TotalBendRevolutions > m_tiedRevolutionsMax)
            {
                Tied = true;
                m_onRopeTied.Invoke();
                if (TotalAmountSpooled > m_spooledThreshold)
                {
                    m_onRopeSpooledAndTied.Invoke();
                }
            }
            else if (Tied && (TotalSlackPercentage > m_tiedSlackMax || TotalBendRevolutions < m_tiedRevolutionsMin))
            {
                Tied = false;
                m_onRopeUntied.Invoke();
            }

            m_tubeRenderer.ClearPoints();

            for (int i = 0, j = 0; i < m_ropeSimulation.NodeCount; i++)
            {
                var node = m_ropeSimulation.ReadableNodes[i];
                var point = node.Position;

                var distance = (float)i / (m_ropeSimulation.NodeCount - 1) * m_totalLength;
                if (distance < m_spooledLength)
                {
                    m_tubeRenderer.UvIndexOffset = i;
                    continue;
                }
                else
                {
                    foreach (var bind in m_ropeSimulation.Binds)
                    {
                        if (!bind.Bound || bind.Index != i) continue;
                        point = bind.Target;
                    }
                }

                m_tubeRenderer.SetPoint(point, j++);
            }
        }

        private void SetStaticRopePositions()
        {
            var curveCount = m_spline.CurvesCount;
            var curveLengths = new float[curveCount];
            var lengthSum = 0f;

            for (var i = 0; i < curveCount; i++)
            {
                var p0 = m_spline.Points[i * 3].Position;
                var p1 = m_spline.Points[i * 3 + 1].Position;
                var p2 = m_spline.Points[i * 3 + 2].Position;
                var p3 = m_spline.Points[i * 3 + 3].Position;
                curveLengths[i] = BezierUtils.GetCubicLength(p0, p1, p2, p3);
                lengthSum += BezierUtils.GetCubicLength(p0, p1, p2, p3);
            }

            for (var i = 0; i < m_ropeSimulation.NodeCount; i++)
            {
                var node = m_ropeSimulation.ReadableNodes[i];

                var distance = (float)i / (m_ropeSimulation.NodeCount - 1) * m_totalLength;
                if (distance < m_spooledLength)
                {
                    node.Position = m_anchors[0].Position;
                }
                else
                {
                    distance -= m_spooledLength;

                    var accum = 0f;
                    for (var j = 0; j < curveCount; j++)
                    {
                        if (distance <= accum + curveLengths[j] || j == curveCount - 1)
                        {
                            var t = (distance - accum) / curveLengths[j];
                            var k = j * 3;
                            node.Position = BezierUtils.GetPoint(m_spline.Points[k].Position, m_spline.Points[k + 1].Position, m_spline.Points[k + 2].Position, m_spline.Points[k + 3].Position, t);
                            break;
                        }

                        accum += curveLengths[j];

                    }
                }

                node.OldPosition = node.Position;
                m_ropeSimulation.ReadableNodes[i] = node;
            }
        }

        private bool Sample(float d, float threshold, out Vector3 point, out Anchor anchorA, out Anchor anchorB, out float strength)
        {
            var distance = m_spooledLength / m_totalLength;
            strength = 0;

            for (var i = 0; i < m_anchors.Count - 1; i++)
            {
                var anchor = m_anchors[i];
                var nextAnchor = m_anchors[i + 1];

                if (d > distance && d < distance + anchor.Proportion)
                {
                    if (d < distance + (threshold + anchor.BindDistance) || d > distance + anchor.Proportion - (threshold + nextAnchor.BindDistance))
                    {
                        var p1 = anchor.Position;
                        var p2 = nextAnchor.Position;
                        anchorA = null;
                        anchorB = null;

                        if (d < distance + (threshold + anchor.BindDistance))
                        {
                            anchorA = anchor;

                            if (anchor.BindDistance > 0)
                            {
                                var t = (d - distance) / (threshold + anchor.BindDistance);
                                point = Vector3.Lerp(p1, p1 - anchor.BindAxis * (threshold + anchor.BindDistance) * m_totalLength, t);
                                strength = 0.001f;
                                return true;
                            }
                        }

                        if (d > distance + anchor.Proportion - (threshold + nextAnchor.BindDistance))
                        {
                            anchorB = nextAnchor;

                            if (nextAnchor.BindDistance > 0)
                            {
                                var t = (d - (distance + anchor.Proportion - (threshold + nextAnchor.BindDistance))) / (threshold + nextAnchor.BindDistance);
                                point = Vector3.Lerp(p2, p2 + nextAnchor.BindAxis * (threshold + nextAnchor.BindDistance) * m_totalLength, 1 - t);
                                strength = 0.001f;
                                return true;
                            }
                        }

                        point = Vector3.Lerp(p1, p2, (d - distance) / anchor.Proportion);
                        return true;
                    }
                }
                distance += anchor.Proportion;
            }

            point = Vector3.zero;
            anchorA = null;
            anchorB = null;
            return false;
        }

        private void UpdateBurstRope()
        {
            m_ropeSimulation.Binds.Clear();

            var distance = m_spooledLength / m_totalLength;
            var nodeDistance = m_totalLength / m_ropeSimulation.NodeCount;

            var startIndex = 0;

            if (m_spooledLength > 0)
            {
                var value = (m_ropeSimulation.NodeCount - 1) * distance;
                var index = Mathf.Min(Mathf.FloorToInt(value), m_ropeSimulation.NodeCount - 1);

                startIndex = index;

                for (var j = 0; j < index; j++)
                {
                    var bind = new BindingPoint
                    {
                        Bound = true,
                        Index = j,
                        Target = m_anchors[0].Position
                    };
                    m_ropeSimulation.Binds.Add(bind);
                }
            }

            m_ropeSimulation.Binds.Add(new BindingPoint
            {
                Bound = true,
                Index = startIndex,
                Target = m_anchors[0].Position
            });
            m_anchors[0].RopeBindIndex = startIndex;

            var threshold = nodeDistance / m_totalLength;

            for (var i = startIndex + 1; i < m_ropeSimulation.NodeCount - 1; i++)
            {
                var d = (float)i / (m_ropeSimulation.NodeCount - 1);
                if (Sample(d, threshold, out var point, out var a, out var b, out _))
                {
                    m_ropeSimulation.Binds.Add(new BindingPoint
                    {
                        Bound = true,
                        Index = i,
                        Target = point,
                        Strength = 0
                    });
                    if (a is not null) a.RopeBindIndex = i;
                    if (b is not null) b.RopeBindIndex = i;
                }
            }

            m_ropeSimulation.Binds.Add(new BindingPoint
            {
                Bound = m_anchors[^1].Type == AnchorType.Dynamic,
                Index = m_ropeSimulation.NodeCount - 1,
                Target = m_anchors[^1].Position
            });
            m_anchors[^1].RopeBindIndex = m_ropeSimulation.NodeCount - 1;
        }

        private void UpdateLineRenderer()
        {
            if (!m_lineRenderer.enabled) return;

            m_lineRenderer.positionCount = m_anchors.Count;

            var i = 0;
            foreach (var anchor in m_anchors)
            {
                m_lineRenderer.SetPosition(i, m_anchors[i].Position);
                m_lineRenderer.startWidth = m_radius * 2.0f;
                m_lineRenderer.endWidth = m_radius * 2.0f;
                i++;
            }
        }

        private float RayToLineDistance(Ray ray, Vector3 p1, Vector3 p2, out Vector3 point)
        {
            var r1 = transform.InverseTransformPoint(ray.origin);
            var r2 = p1;
            var e1 = transform.InverseTransformDirection(ray.direction);
            var e2 = p2 - p1;
            var n = Vector3.Cross(e1, e2);

            var t1 = Vector3.Dot(Vector3.Cross(e2, n), r2 - r1) / Vector3.Dot(n, n);
            var rayPoint = r1 + t1 * e1;

            var t2 = Mathf.Clamp(Vector3.Dot(Vector3.Cross(e1, n), r2 - r1) / Vector3.Dot(n, n), 0, e2.magnitude);
            point = r2 + t2 * e2;

            return Vector3.Distance(rayPoint, point);
        }

        private float PointToLineDistance(Vector3 point, Vector3 p1, Vector3 p2, out Vector3 closestPoint)
        {
            point = transform.InverseTransformPoint(point);
            var dir = p2 - p1;
            var length = dir.magnitude;
            dir /= length;
            var projected = Mathf.Clamp(Vector3.Dot(point - p1, dir), 0, length);
            closestPoint = p1 + dir * projected;
            return Vector3.Distance(closestPoint, point);
        }

        public bool ClosestPointToRay(Ray ray, out Vector3 point, out float distance, out int index)
        {
            point = Vector3.zero;
            distance = float.MaxValue;
            index = -1;

            for (var i = 0; i < m_anchors.Count - 1; i++)
            {
                var d = RayToLineDistance(ray, m_anchors[i].Position, m_anchors[i + 1].Position, out var p);
                if (d < distance)
                {
                    distance = d;
                    point = p;
                    index = i;
                }
            }

            return index != -1;
        }

        public bool ClosestPoint(Vector3 point, out Vector3 closestPoint, out float distance, out int index)
        {
            closestPoint = Vector3.zero;
            distance = float.MaxValue;
            index = -1;

            for (var i = 0; i < m_anchors.Count - 1; i++)
            {
                var d = PointToLineDistance(point, m_anchors[i].Position, m_anchors[i + 1].Position, out var p);
                if (d < distance)
                {
                    distance = d;
                    closestPoint = p;
                    index = i;
                }
            }

            return index != -1;
        }

        private void OnDrawGizmos()
        {
            for (var i = 0; i < m_anchors.Count; i++)
            {
                var anchor = m_anchors[i];
                switch (anchor.Type)
                {
                    case AnchorType.Start:
                        Gizmos.color = Color.yellow;
                        break;
                    case AnchorType.Bend:
                        Gizmos.color = Color.blue;
                        break;
                    case AnchorType.Dynamic:
                        Gizmos.color = Color.green;
                        break;
                }
                Gizmos.DrawSphere(transform.TransformPoint(anchor.Position), m_radius);

                if (i < m_anchors.Count - 1)
                {

                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(transform.TransformPoint(anchor.Position), transform.TransformPoint(m_anchors[i + 1].Position));
                }
            }
        }
    }
}