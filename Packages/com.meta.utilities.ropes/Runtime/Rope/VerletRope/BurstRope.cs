// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Meta.Utilities.Ropes
{
    /// <summary>
    /// Struct for binding part of the rope target is in rope space
    /// </summary>
    [Serializable]
    public struct BindingPoint
    {
        public int Index;
        public Vector3 Target;
        public bool Bound;
        public float Strength;
    }

    /// <summary>
    /// A single verlet node within a rope simulation
    /// </summary>
    [Serializable]
    internal struct VerletNode
    {
        public Vector3 Position;
        public Vector3 OldPosition;

        public VerletNode(Vector3 startPosition)
        {
            Position = startPosition;
            OldPosition = startPosition;
        }

        public VerletNode(Vector3 position, Vector3 oldPosition)
        {
            Position = position;
            OldPosition = oldPosition;
        }
    }

    public struct BurstRopeCollision
    {
        public int Index;
        public Vector3 Point;
        public Vector3 Normal;
        public Collider Collider;
    }

    /// <summary>
    /// Rope simulation Level of Detail (LOD), used to define simulation quality based on player distance
    /// </summary>
    [Serializable]
    public struct BurstRopeSimulationLod
    {
        public float Distance;
        public int Iterations;
        public int SubstepCount;
        public bool UseCollision;
    }

    /// <summary>
    /// This component simulates a verlet rope using burst jobs
    /// The rope also supports basic one-way collision with static colliders in the scene
    /// 
    /// Rope nodes can be pinned to specific locations via binding points allowing them to 
    /// be hung and drapped or interacted with by the player
    /// 
    /// The rope simulation is calculated in local space
    /// </summary>
    [BurstCompile]
    public class BurstRope : MonoBehaviour
    {
        public List<BindingPoint> Binds = new();
        public int NodeCount => m_nodeCount;
        public int SubstepCount => m_substepCount;
        public int WarmupSubstepsRemaining { get; private set; }
        public event Action<BurstRopeCollision> OnCollision;

        [SerializeField, Tooltip("The number of iterations to use for solving constraints")] private int m_iterations = 80;
        [SerializeField, Tooltip("The number of substeps, note that each substep will run a full number of iterations and collision detection")] private int m_substepCount = 1;
        [SerializeField] private int m_nodeCount = 40;
        [SerializeField, Tooltip("How many substeps to use to 'warm up' the simulation at the start")] private int m_warmupSubsteps = 1;

        [SerializeField] private bool m_useSimulationLods;
        [SerializeField, Tooltip("The transform to use for determining player distance from the rope for simulation lod purposes")] private Transform m_lodProxy;
        [SerializeField, Tooltip("The simulation lods settings to use")] private BurstRopeSimulationLod[] m_simulationLods;

        /// <summary>
        /// A copy of the current state of the verlet nodes, safe to access even while burst jobs are running
        /// </summary>
        internal VerletNode[] ReadableNodes
        {
            get
            {
                if (m_readableNodes == null || m_readableNodes.Length != m_nodeCount)
                    m_readableNodes = new VerletNode[m_nodeCount];
                return m_readableNodes;
            }
        }

        /// <summary>
        /// The radius used for collisions with the rope
        /// </summary>
        public float CollisionRadius
        {
            get => m_collider.radius;
            set
            {
                if (m_collider.radius != value) m_collider.radius = value;
            }
        }

        [Tooltip("The resting distance between nodes that the constraint solver will try to maintain")]
        public float NodeDistance = 0.1f;
        [SerializeField, Tooltip("The layer mask used for collision detection (if enabled)")] private LayerMask m_collisionLayers;
        [SerializeField] private bool m_useCollision;
        [SerializeField] private bool m_renderOnUpdate = true;

        [SerializeField] private float m_damping = 0.25f;
        [SerializeField] private float m_friction = 0.9f;

        [SerializeField, AutoSet] private TubeRenderer m_renderer;

        #region Constants

        private const int MAXCOLLISIONS = 128;
        private const int MAXBINDINGS = 128;
        private const float COLLISIONRADIUS = .05f;
        private const int COLLISIONBUFFERSIZE = 8;
        private const int INNER_LOOP_BATCH_COUNT = 64;

        #endregion

        private NativeArray<BindingPoint> m_bindings;
        private NativeArray<VerletNode> m_nodes;
        private NativeArray<Vector3> m_translations;
        private NativeArray<ColliderHit> m_collisionResults;
        private NativeArray<OverlapSphereCommand> m_overlapSphereCommands;
        private JobHandle m_handle = new();
        [SerializeField] private VerletNode[] m_readableNodes;
        private SphereCollider m_collider;
        private int m_fixedUpdateCount;
        private int m_substepsPerformedThisFrame;

        private void Awake()
        {
            // Dummy collider used for depenetration calls (keep disabled)
            m_collider = gameObject.AddComponent<SphereCollider>();
            m_collider.radius = .01f;
            m_collider.enabled = false;
            m_collider.center = Vector3.zero;
            WarmupSubstepsRemaining = m_warmupSubsteps;

            Setup();
        }

        /// <summary>
        /// Perform simulation in editor (useful for setting up ropes in the editor)
        /// </summary>
        [ContextMenu("Simulate")]
        private void SimulateInEditor()
        {
            Simulate(1000);
        }

        /// <summary>
        /// Simulate a fixed number of iterations all at once
        /// </summary>
        /// <param name="iterations"></param>
        public void Simulate(int iterations)
        {
            Setup();
            for (var i = 0; i < iterations; i++)
            {
                Execute(Time.fixedDeltaTime);
                Finish(Time.fixedDeltaTime);
            }

            UpdateRenderer();
            Dispose();
        }

        /// <summary>
        /// Render the rope's current state to a mesh (via the tube renderer component)
        /// </summary>
        public void UpdateRenderer()
        {
            SetRenderer();
            m_renderer.RunOnce();
        }

        private void Setup()
        {
            if (m_readableNodes == null || m_readableNodes.Length != m_nodeCount)
            {
                m_readableNodes = new VerletNode[m_nodeCount];
                // Initialize sensible defaults to reduce rope snapping on awake
                if (Binds.Count > 0)
                {
                    for (var i = 0; i < m_readableNodes.Length; i++)
                    {
                        var bindF = (float)Binds.Count * i / m_readableNodes.Length;
                        var bind0 = Mathf.FloorToInt(bindF);
                        var bind1 = Mathf.Min(bind0 + 1, Binds.Count - 1);
                        var pos = Vector3.Lerp(Binds[bind0].Target, Binds[bind1].Target,
                            (bindF - bind0) / Mathf.Max(bind1 - bind0, 1));
                        m_readableNodes[i] = new() { Position = pos, OldPosition = pos };
                    }
                }
            }
            m_nodes = new NativeArray<VerletNode>(m_nodeCount, Allocator.Persistent);
            m_nodes.CopyFrom(m_readableNodes);

            m_bindings = new NativeArray<BindingPoint>(m_nodeCount, Allocator.Persistent);
            Binds.Capacity = MAXBINDINGS;

            m_translations = new NativeArray<Vector3>(m_nodeCount, Allocator.Persistent);

            m_renderer = GetComponent<TubeRenderer>();
            if (m_renderOnUpdate)
                m_renderer.ResizeTube(NodeCount);

            m_collisionResults = new NativeArray<ColliderHit>(m_nodeCount * COLLISIONBUFFERSIZE, Allocator.Persistent);
            m_overlapSphereCommands = new NativeArray<OverlapSphereCommand>(m_nodeCount, Allocator.Persistent);
        }

        private void Dispose()
        {
            m_handle.Complete();
            m_nodes.Dispose();
            m_translations.Dispose();
            m_bindings.Dispose();
            m_collisionResults.Dispose();
            m_overlapSphereCommands.Dispose();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void Execute(float deltaTime)
        {
            if (m_fixedUpdateCount == 0)
            {
                // Only update bindings once since they may be used by jobs until LateUpdate() is finished
                for (var j = 0; j < m_bindings.Length; j++)
                {
                    m_bindings[j] = new BindingPoint();
                }
                foreach (var bind in Binds)
                {
                    if (bind.Index < m_bindings.Length)
                        m_bindings[bind.Index] = bind;
                }
            }

            SimulateNodesJob simulate = new()
            {
                Nodes = m_nodes,
                Bindings = m_bindings,
                Gravity = transform.InverseTransformDirection(Physics.gravity),
                Damping = m_damping,
                DeltaTime = deltaTime,
            };

            m_handle = simulate.Schedule(m_nodeCount, INNER_LOOP_BATCH_COUNT, dependsOn: m_handle);

            for (var i = 0; i < m_iterations; i++)
            {
                DistanceConstraintJob distanceConstraintJob = new()
                {
                    Nodes = m_nodes,
                    TranslateValues = m_translations,
                    NodeDistance = NodeDistance,
                };
                m_handle = distanceConstraintJob.Schedule(m_nodeCount - 1, INNER_LOOP_BATCH_COUNT, dependsOn: m_handle);

                ApplyConstraintsJob applyConstraintsJob1 = new()
                {
                    Nodes = m_nodes,
                    TranslateValues = m_translations,
                    Bindings = m_bindings
                };
                m_handle = applyConstraintsJob1.Schedule(m_nodeCount, INNER_LOOP_BATCH_COUNT, dependsOn: m_handle);
            }

            if (m_useCollision)
            {
                SetupOverlapSphereCommands setupSphereCommands = new()
                {
                    Nodes = m_nodes,
                    Commands = m_overlapSphereCommands,
                    QueryParams = new QueryParameters
                    {
                        layerMask = m_collisionLayers,
                        hitTriggers = QueryTriggerInteraction.Ignore
                    },
                    TransformMatrix = transform.localToWorldMatrix,
                };
                m_handle = setupSphereCommands.Schedule(m_nodeCount, INNER_LOOP_BATCH_COUNT, m_handle);
                m_handle = OverlapSphereCommand.ScheduleBatch(m_overlapSphereCommands, m_collisionResults, 1, COLLISIONBUFFERSIZE, m_handle);
            }
            JobHandle.ScheduleBatchedJobs();
        }

        private void Finish(float dt, bool copy = true)
        {
            m_handle.Complete();
            if (m_useCollision) HandleCollisions(dt);
            if (copy) m_nodes.CopyTo(m_readableNodes);
        }

        private void HandleCollisions(float dt)
        {
            if (m_collider == null) return;
            m_collider.enabled = true;

            for (var i = 0; i < NodeCount; i++)
            {
                if (m_bindings[i].Bound || m_collisionResults[i * COLLISIONBUFFERSIZE].collider == null) continue;

                var node = m_nodes[i];

                node.Position = FromRopeSpace(node.Position);
                node.OldPosition = FromRopeSpace(node.OldPosition);
                var velocity = node.Position - node.OldPosition;

                for (var j = 0; j < COLLISIONBUFFERSIZE; j++)
                {
                    var hitResult = m_collisionResults[i * COLLISIONBUFFERSIZE + j];
                    if (hitResult.collider == null) break;

                    var hitCollider = hitResult.collider;
                    if (hitCollider == null || hitCollider == m_collider)
                        continue;

                    var dynamicFriction = m_friction;

                    if (Physics.ComputePenetration(m_collider, node.Position, Quaternion.identity, hitCollider, hitCollider.transform.position, hitCollider.transform.rotation, out var direction, out var distance))
                    {
                        var normalVelocity = Vector3.Dot(direction, velocity);

                        if (m_friction > 0 && velocity.sqrMagnitude > Vector3.kEpsilon)
                        {
                            var nv = velocity.normalized;
                            if (Mathf.Abs(Vector3.Dot(direction, nv)) < 1)
                            {
                                var tangent = Vector3.Cross(direction, nv).normalized;
                                var bitangent = Vector3.Cross(tangent, direction).normalized;

                                var tangentVelocity = Vector3.Dot(tangent, velocity);
                                var bitangentVelocity = Vector3.Dot(bitangent, velocity);

                                velocity -= tangent * (tangentVelocity * dynamicFriction);
                                velocity -= bitangent * (bitangentVelocity * dynamicFriction);
                            }
                        }

                        velocity -= direction * normalVelocity;

                        node.Position += direction * distance;
                        node.OldPosition = node.Position - velocity;

                        OnCollision?.Invoke(new BurstRopeCollision
                        {
                            Index = i,
                            Point = node.Position,
                            Normal = direction,
                            Collider = hitCollider,
                        });
                    }
                }

                node.Position = ToRopeSpace(node.Position);
                node.OldPosition = ToRopeSpace(node.OldPosition);
                m_nodes[i] = node;
            }

            m_collider.enabled = false;
        }

        private void FixedUpdate()
        {
            m_substepsPerformedThisFrame = 0;

            // Adjust simulation settings based on simulation lods
            if (m_useSimulationLods)
            {
                var measurePoint = m_lodProxy ? m_lodProxy : transform;
                var cameraDistance = (Camera.main.transform.position - measurePoint.position).magnitude;
                for (var i = 0; i < m_simulationLods.Length; i++)
                {
                    if (i == m_simulationLods.Length - 1 || cameraDistance < m_simulationLods[i].Distance)
                    {
                        m_iterations = m_simulationLods[i].Iterations;
                        m_substepCount = m_simulationLods[i].SubstepCount;
                        m_useCollision = m_simulationLods[i].UseCollision;
                        break;
                    }
                }
            }

            var substepsThisFrame = Mathf.Max(m_substepCount, WarmupSubstepsRemaining);

            for (var i = 0; i < substepsThisFrame; i++)
            {
                Execute(Time.fixedDeltaTime / substepsThisFrame);
                if (i != m_substepCount - 1)
                    Finish(Time.fixedDeltaTime, false);
                m_substepsPerformedThisFrame++;
            }
            if (WarmupSubstepsRemaining > 0) WarmupSubstepsRemaining -= m_substepsPerformedThisFrame;

            m_fixedUpdateCount++;
        }

        private void LateUpdate()
        {
            if (m_substepsPerformedThisFrame > 0)
            {
                Finish(Time.fixedDeltaTime);

                if (m_renderOnUpdate)
                {
                    SetRenderer();
                }
            }

            m_fixedUpdateCount = 0;
        }

        private void SetRenderer()
        {
            for (var i = 0; i < m_nodeCount; i++)
            {
                var point = m_readableNodes[i].Position;
                foreach (var bind in Binds)
                {
                    if (!bind.Bound || bind.Index != i) continue;
                    point = bind.Target;
                }
                m_renderer.SetPoint(point, i);
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct SimulateNodesJob : IJobParallelFor
        {
            public NativeArray<VerletNode> Nodes;
            [ReadOnly] public NativeArray<BindingPoint> Bindings;
            public Vector3 Gravity;
            public float DeltaTime;
            public float Damping;

            public void Execute(int index)
            {
                var node = Nodes[index];

                node.Position += (node.Position - node.OldPosition) * (1 - Damping * DeltaTime) + Gravity * (DeltaTime * DeltaTime);
                node.OldPosition = Nodes[index].Position;

                Nodes[index] = node;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct DistanceConstraintJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<VerletNode> Nodes;
            [WriteOnly] public NativeArray<Vector3> TranslateValues;
            public float NodeDistance;

            public void Execute(int i)
            {
                var node1 = Nodes[i];
                var node2 = Nodes[i + 1];

                var diff = node1.Position - node2.Position;
                var distance = diff.magnitude;
                float difference = 0;

                if (distance > 0)
                {
                    difference = (NodeDistance - distance) / distance;
                }

                TranslateValues[i] = diff * (0.5f * difference);
            }
        }

        /// <summary>
        /// Experimental angular constraint (not currently used)
        /// </summary>
        [BurstCompile(CompileSynchronously = true)]
        private struct AngleConstraintJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<VerletNode> Nodes;
            [WriteOnly] public NativeArray<Vector3> TranslateValues;
            public float NodeDistance;

            public void Execute(int i)
            {
                if (i < 2)
                {
                    TranslateValues[0] = Vector3.zero;
                    return;
                }

                var node0 = Nodes[i - 2];
                var node1 = Nodes[i - 1];
                var node2 = Nodes[i];

                var dir1 = node1.Position - node0.Position;
                var dir2 = node2.Position - node1.Position;

                var goal = node1.Position + dir1.normalized * dir2.magnitude;

                TranslateValues[i] = (goal - node2.Position) * 0.001f;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct ApplyConstraintsJob : IJobParallelFor
        {
            public NativeArray<VerletNode> Nodes;
            [ReadOnly] public NativeArray<Vector3> TranslateValues;
            [ReadOnly] public NativeArray<BindingPoint> Bindings;
            [ReadOnly] public int Substeps;

            public void Execute(int i)
            {
                var node = Nodes[i];

                if (Bindings[i].Bound)
                {
                    node.Position = Bindings[i].Target;
                }
                else
                {
                    if (i < TranslateValues.Length - 1 && Bindings[i + 1].Bound && Bindings[i + 1].Strength > 0)
                    {
                        node.Position += Vector3.ClampMagnitude(TranslateValues[i], Bindings[i + 1].Strength);
                    }
                    else if (i < TranslateValues.Length)
                    {
                        node.Position += TranslateValues[i];
                    }

                    if (i > 0)
                    {
                        if (Bindings[i - 1].Bound && Bindings[i - 1].Strength > 0)
                        {
                            node.Position -= Vector3.ClampMagnitude(TranslateValues[i - 1], Bindings[i - 1].Strength);
                        }
                        else
                        {
                            node.Position -= TranslateValues[i - 1];
                        }
                    }
                }

                Nodes[i] = node;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct SetupOverlapSphereCommands : IJobParallelFor
        {
            [ReadOnly] public NativeArray<VerletNode> Nodes;
            public NativeArray<OverlapSphereCommand> Commands;
            [ReadOnly] public QueryParameters QueryParams;
            [ReadOnly] public Matrix4x4 TransformMatrix;

            public void Execute(int i)
            {
                var position = Nodes[i].Position;
                Commands[i] = new OverlapSphereCommand(TransformMatrix * new Vector4(position.x, position.y, position.z, 1.0f), COLLISIONRADIUS, QueryParams);
            }
        }

        public int ClosestIndexToPoint(Vector3 point)
        {
            point = transform.InverseTransformPoint(point);
            var nodes = m_readableNodes;
            var bestindex = 0;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                var distance = (point - node.Position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestindex = i;
                    bestDistance = distance;
                }
            }
            return bestindex;
        }

        public Vector3 ClosestPointOnRope(Vector3 point)
        {
            point = transform.InverseTransformPoint(point);
            var nodes = m_readableNodes;
            var bestPoint = Vector3.zero;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < nodes.Length - 1; i++)
            {
                var node1 = nodes[i];
                var node2 = nodes[i + 1];
                var pointOnLineSegment = LineSegment.ClosestPoint(node1.Position, node2.Position, point);
                var distance = (point - pointOnLineSegment).sqrMagnitude;

                if (distance < bestDistance)
                {
                    bestPoint = pointOnLineSegment;
                    bestDistance = distance;
                }
            }

            return transform.TransformPoint(bestPoint);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            foreach (var binding in Binds)
            {
                Gizmos.color = binding.Bound ? Color.yellow : Color.yellow * .5f;
                Gizmos.DrawSphere(transform.TransformPoint(binding.Target), 0.01f);
            }
            if (!Application.isPlaying) return;
            for (var i = 0; i < m_readableNodes.Length - 1; i++)
            {
                Gizmos.color = i % 2 == 0 ? Color.blue : Color.red;
                Gizmos.DrawLine(FromRopeSpace(m_readableNodes[i].Position), FromRopeSpace(m_readableNodes[i + 1].Position));
            }
        }

        public float AverageLinkDistance(int minIndex, int maxIndexInclusive)
        {
            if (minIndex < 0 || maxIndexInclusive >= m_readableNodes.Length)
            {
                Debug.LogError("Trying to read nodes that dont exist");
                return 0f;
            }
            float distances = 0;
            for (var i = minIndex; i < maxIndexInclusive; i++)
            {
                var node1Pos = m_readableNodes[i].Position;
                var node2Pos = m_readableNodes[i + 1].Position;
                distances += Vector3.Distance(node1Pos, node2Pos);
            }
            distances /= maxIndexInclusive - minIndex;
            return distances;
        }

        public void MirrorTightness(BurstRope target, int from, int to)
        {
            var targetLength = target.AverageLinkDistance(from, to) * (to - from);
            var length = NodeCount * NodeDistance;
            var percent = targetLength / length;
            NodeDistance *= percent;
        }

        /// <summary>
        /// Convert world point to rope space (i.e. local space)
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector3 ToRopeSpace(Vector3 point)
        {
            return transform.InverseTransformPoint(point);
        }

        /// <summary>
        /// Convert rope point (i.e. local space) to world space
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector3 FromRopeSpace(Vector3 point)
        {
            return transform.TransformPoint(point);
        }
    }
}
