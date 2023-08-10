using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;

namespace UnityEngine.XR.Hands.Samples.VisualizerSample
{
    public class HandVisualizer : MonoBehaviour
    {
        public enum VelocityType
        {
            Linear,
            Angular,
            None,
        }

        [SerializeField]
        [Tooltip("If this is enabled, this component will enable the Input System internal feature flag 'USE_OPTIMIZED_CONTROLS'. You must have at least version 1.5.0 of the Input System and have its backend enabled for this to take effect.")]
        bool m_UseOptimizedControls;

        [SerializeField]
        XROrigin m_Origin;

        [SerializeField]
        GameObject m_LeftHandMesh;

        [SerializeField]
        GameObject m_RightHandMesh;

        [SerializeField]
        Material m_HandMeshMaterial;

        public bool drawMeshes
        {
            get => m_DrawMeshes;
            set => m_DrawMeshes = value;
        }

        [SerializeField]
        bool m_DrawMeshes;
        bool m_PreviousDrawMeshes;

        [SerializeField]
        GameObject m_DebugDrawPrefab;

        public bool debugDrawJoints
        {
            get => m_DebugDrawJoints;
            set => m_DebugDrawJoints = value;
        }

        [SerializeField]
        bool m_DebugDrawJoints;
        bool m_PreviousDebugDrawJoints;

        [SerializeField]
        GameObject m_VelocityPrefab;

        public VelocityType velocityType
        {
            get => m_VelocityType;
            set => m_VelocityType = value;
        }

        [SerializeField]
        VelocityType m_VelocityType;
        VelocityType m_PreviousVelocityType;

        XRHandSubsystem m_Subsystem;
        HandGameObjects m_LeftHandGameObjects;
        HandGameObjects m_RightHandGameObjects;

        static readonly List<XRHandSubsystem> s_SubsystemsReuse = new List<XRHandSubsystem>();

        protected void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            if (m_UseOptimizedControls)
                InputSystem.InputSystem.settings.SetInternalFeatureFlag("USE_OPTIMIZED_CONTROLS", true);
#endif // ENABLE_INPUT_SYSTEM
        }

        protected void OnEnable()
        {
            if (m_Subsystem == null)
                return;

            UpdateRenderingVisibility(m_LeftHandGameObjects, m_Subsystem.leftHand.isTracked);
            UpdateRenderingVisibility(m_RightHandGameObjects, m_Subsystem.rightHand.isTracked);
        }

        protected void OnDisable()
        {
            if (m_Subsystem != null)
            {
                m_Subsystem.trackingAcquired -= OnTrackingAcquired;
                m_Subsystem.trackingLost -= OnTrackingLost;
                m_Subsystem.updatedHands -= OnUpdatedHands;
                m_Subsystem = null;
            }

            UpdateRenderingVisibility(m_LeftHandGameObjects, false);
            UpdateRenderingVisibility(m_RightHandGameObjects, false);
        }

        protected void OnDestroy()
        {
            if (m_LeftHandGameObjects != null)
            {
                m_LeftHandGameObjects.OnDestroy();
                m_LeftHandGameObjects = null;
            }

            if (m_RightHandGameObjects != null)
            {
                m_RightHandGameObjects.OnDestroy();
                m_RightHandGameObjects = null;
            }
        }

        protected void Update()
        {
            if (m_Subsystem != null)
                return;

            SubsystemManager.GetSubsystems(s_SubsystemsReuse);
            if (s_SubsystemsReuse.Count == 0)
                return;

            m_Subsystem = s_SubsystemsReuse[0];

            if (m_LeftHandGameObjects == null)
            {
                m_LeftHandGameObjects = new HandGameObjects(
                    Handedness.Left,
                    transform,
                    m_LeftHandMesh,
                    m_HandMeshMaterial,
                    m_DebugDrawPrefab,
                    m_VelocityPrefab);
            }

            if (m_RightHandGameObjects == null)
            {
                m_RightHandGameObjects = new HandGameObjects(
                    Handedness.Right,
                    transform,
                    m_RightHandMesh,
                    m_HandMeshMaterial,
                    m_DebugDrawPrefab,
                    m_VelocityPrefab);
            }

            UpdateRenderingVisibility(m_LeftHandGameObjects, m_Subsystem.leftHand.isTracked);
            UpdateRenderingVisibility(m_RightHandGameObjects, m_Subsystem.rightHand.isTracked);

            m_PreviousDrawMeshes = m_DrawMeshes;
            m_PreviousDebugDrawJoints = m_DebugDrawJoints;
            m_PreviousVelocityType = m_VelocityType;

            m_Subsystem.trackingAcquired += OnTrackingAcquired;
            m_Subsystem.trackingLost += OnTrackingLost;
            m_Subsystem.updatedHands += OnUpdatedHands;
        }

        void UpdateRenderingVisibility(HandGameObjects handGameObjects, bool isTracked)
        {
            if (handGameObjects == null)
                return;

            handGameObjects.ToggleDrawMesh(m_DrawMeshes && isTracked);
            handGameObjects.ToggleDebugDrawJoints(m_DebugDrawJoints && isTracked);
            handGameObjects.SetVelocityType(isTracked ? m_VelocityType : VelocityType.None);
        }

        void OnTrackingAcquired(XRHand hand)
        {
            switch (hand.handedness)
            {
                case Handedness.Left:
                    UpdateRenderingVisibility(m_LeftHandGameObjects, true);
                    break;

                case Handedness.Right:
                    UpdateRenderingVisibility(m_RightHandGameObjects, true);
                    break;
            }
        }

        void OnTrackingLost(XRHand hand)
        {
            switch (hand.handedness)
            {
                case Handedness.Left:
                    UpdateRenderingVisibility(m_LeftHandGameObjects, false);
                    break;

                case Handedness.Right:
                    UpdateRenderingVisibility(m_RightHandGameObjects, false);
                    break;
            }
        }

        void OnUpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, XRHandSubsystem.UpdateType updateType)
        {
            // We have no game logic depending on the Transforms, so early out here
            // (add game logic before this return here, directly querying from
            // subsystem.leftHand and subsystem.rightHand using GetJoint on each hand)
            if (updateType == XRHandSubsystem.UpdateType.Dynamic)
                return;

            bool leftHandTracked = subsystem.leftHand.isTracked;
            bool rightHandTracked = subsystem.rightHand.isTracked;

            if (m_PreviousDrawMeshes != m_DrawMeshes)
            {
                m_LeftHandGameObjects.ToggleDrawMesh(m_DrawMeshes && leftHandTracked);
                m_RightHandGameObjects.ToggleDrawMesh(m_DrawMeshes && rightHandTracked);
                m_PreviousDrawMeshes = m_DrawMeshes;
            }

            if (m_PreviousDebugDrawJoints != m_DebugDrawJoints)
            {
                m_LeftHandGameObjects.ToggleDebugDrawJoints(m_DebugDrawJoints && leftHandTracked);
                m_RightHandGameObjects.ToggleDebugDrawJoints(m_DebugDrawJoints && rightHandTracked);
                m_PreviousDebugDrawJoints = m_DebugDrawJoints;
            }

            if (m_PreviousVelocityType != m_VelocityType)
            {
                m_LeftHandGameObjects.SetVelocityType(leftHandTracked ? m_VelocityType : VelocityType.None);
                m_RightHandGameObjects.SetVelocityType(rightHandTracked ? m_VelocityType : VelocityType.None);
                m_PreviousVelocityType = m_VelocityType;
            }

            m_LeftHandGameObjects.UpdateJoints(
                m_Origin,
                subsystem.leftHand,
                (updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) != 0,
                m_DrawMeshes,
                m_DebugDrawJoints,
                m_VelocityType);

            if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose) != 0)
                m_LeftHandGameObjects.UpdateRootPose(subsystem.leftHand);

            m_RightHandGameObjects.UpdateJoints(
                m_Origin,
                subsystem.rightHand,
                (updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints) != 0,
                m_DrawMeshes,
                m_DebugDrawJoints,
                m_VelocityType);
            
            if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose) != 0)
                m_RightHandGameObjects.UpdateRootPose(subsystem.rightHand);
        }

        class HandGameObjects
        {
            GameObject m_HandRoot;
            GameObject m_DrawJointsParent;

            Transform[] m_JointXforms = new Transform[XRHandJointID.EndMarker.ToIndex()];
            GameObject[] m_DrawJoints = new GameObject[XRHandJointID.EndMarker.ToIndex()];
            GameObject[] m_VelocityParents = new GameObject[XRHandJointID.EndMarker.ToIndex()];
            LineRenderer[] m_Lines = new LineRenderer[XRHandJointID.EndMarker.ToIndex()];
            bool m_IsTracked;

            static Vector3[] s_LinePointsReuse = new Vector3[2];
            const float k_LineWidth = 0.005f;

            public HandGameObjects(
                Handedness handedness,
                Transform parent,
                GameObject meshPrefab,
                Material meshMaterial,
                GameObject debugDrawPrefab,
                GameObject velocityPrefab)
            {
                void AssignJoint(
                    XRHandJointID jointId,
                    Transform jointXform,
                    Transform drawJointsParent)
                {
                    int jointIndex = jointId.ToIndex();
                    m_JointXforms[jointIndex] = jointXform;

                    m_DrawJoints[jointIndex] = Instantiate(debugDrawPrefab);
                    m_DrawJoints[jointIndex].transform.parent = drawJointsParent;
                    m_DrawJoints[jointIndex].name = jointId.ToString();

                    m_VelocityParents[jointIndex] = Instantiate(velocityPrefab);
                    m_VelocityParents[jointIndex].transform.parent = jointXform;

                    m_Lines[jointIndex] = m_DrawJoints[jointIndex].GetComponent<LineRenderer>();
                    m_Lines[jointIndex].startWidth = m_Lines[jointIndex].endWidth = k_LineWidth;
                    s_LinePointsReuse[0] = s_LinePointsReuse[1] = jointXform.position;
                    m_Lines[jointIndex].SetPositions(s_LinePointsReuse);
                }

                m_HandRoot = Instantiate(meshPrefab, parent);
                m_HandRoot.transform.localPosition = Vector3.zero;
                m_HandRoot.transform.localRotation = Quaternion.identity;

                Transform wristRootXform = null;
                for (int childIndex = 0; childIndex < m_HandRoot.transform.childCount; ++childIndex)
                {
                    var child = m_HandRoot.transform.GetChild(childIndex);
                    if (child.gameObject.name.EndsWith(XRHandJointID.Wrist.ToString()))
                        wristRootXform = child;
                    else if (child.gameObject.name.EndsWith("Hand") && meshMaterial != null && child.TryGetComponent<SkinnedMeshRenderer>(out var renderer))
                        renderer.sharedMaterial = meshMaterial;
                }

                m_DrawJointsParent = new GameObject();
                m_DrawJointsParent.transform.parent = parent;
                m_DrawJointsParent.transform.localPosition = Vector3.zero;
                m_DrawJointsParent.transform.localRotation = Quaternion.identity;
                m_DrawJointsParent.name = handedness + " Hand Debug Draw Joints";

                if (wristRootXform == null)
                {
                    Debug.LogWarning("Hand transform hierarchy not set correctly - couldn't find Wrist joint!");
                }
                else
                {
                    AssignJoint(XRHandJointID.Wrist, wristRootXform, m_DrawJointsParent.transform);
                    for (int childIndex = 0; childIndex < wristRootXform.childCount; ++childIndex)
                    {
                        var child = wristRootXform.GetChild(childIndex);

                        if (child.name.EndsWith(XRHandJointID.Palm.ToString()))
                        {
                            AssignJoint(XRHandJointID.Palm, child, m_DrawJointsParent.transform);
                            continue;
                        }

                        for (int fingerIndex = (int)XRHandFingerID.Thumb;
                             fingerIndex <= (int)XRHandFingerID.Little;
                             ++fingerIndex)
                        {
                            var fingerId = (XRHandFingerID)fingerIndex;

                            var jointIdFront = fingerId.GetFrontJointID();
                            if (!child.name.EndsWith(jointIdFront.ToString()))
                                continue;

                            AssignJoint(jointIdFront, child, m_DrawJointsParent.transform);
                            var lastChild = child;

                            int jointIndexBack = fingerId.GetBackJointID().ToIndex();
                            for (int jointIndex = jointIdFront.ToIndex() + 1;
                                 jointIndex <= jointIndexBack;
                                 ++jointIndex)
                            {
                                for (int nextChildIndex = 0; nextChildIndex < lastChild.childCount; ++nextChildIndex)
                                {
                                    var nextChild = lastChild.GetChild(nextChildIndex);
                                    if (nextChild.name.EndsWith(XRHandJointIDUtility.FromIndex(jointIndex).ToString()))
                                    {
                                        lastChild = nextChild;
                                        break;
                                    }
                                }

                                if (!lastChild.name.EndsWith(XRHandJointIDUtility.FromIndex(jointIndex).ToString()))
                                    throw new InvalidOperationException("Hand transform hierarchy not set correctly - couldn't find " + XRHandJointIDUtility.FromIndex(jointIndex) + " joint!");

                                var jointId = XRHandJointIDUtility.FromIndex(jointIndex);
                                AssignJoint(jointId, lastChild, m_DrawJointsParent.transform);
                            }
                        }
                    }
                }

                for (int fingerIndex = (int)XRHandFingerID.Thumb;
                     fingerIndex <= (int)XRHandFingerID.Little;
                     ++fingerIndex)
                {
                    var fingerId = (XRHandFingerID)fingerIndex;

                    var jointId = fingerId.GetFrontJointID();
                    if (m_JointXforms[jointId.ToIndex()] == null)
                        Debug.LogWarning("Hand transform hierarchy not set correctly - couldn't find " + jointId + " joint!");
                }
            }

            public void OnDestroy()
            {
                Destroy(m_HandRoot);
                m_HandRoot = null;

                for (int jointIndex = 0; jointIndex < m_DrawJoints.Length; ++jointIndex)
                {
                    Destroy(m_DrawJoints[jointIndex]);
                    m_DrawJoints[jointIndex] = null;
                }

                for (int jointIndex = 0; jointIndex < m_VelocityParents.Length; ++jointIndex)
                {
                    Destroy(m_VelocityParents[jointIndex]);
                    m_VelocityParents[jointIndex] = null;
                }

                Destroy(m_DrawJointsParent);
                m_DrawJointsParent = null;
            }

            public void ToggleDrawMesh(bool drawMesh)
            {
                for (int childIndex = 0; childIndex < m_HandRoot.transform.childCount; ++childIndex)
                {
                    var xform = m_HandRoot.transform.GetChild(childIndex);
                    if (xform.TryGetComponent<SkinnedMeshRenderer>(out var renderer))
                        renderer.enabled = drawMesh;
                }
            }

            public void ToggleDebugDrawJoints(bool debugDrawJoints)
            {
                for (int jointIndex = 0; jointIndex < m_DrawJoints.Length; ++jointIndex)
                {
                    ToggleRenderers<MeshRenderer>(debugDrawJoints, m_DrawJoints[jointIndex].transform);
                    m_Lines[jointIndex].enabled = debugDrawJoints;
                }

                m_Lines[0].enabled = false;
            }

            public void SetVelocityType(VelocityType velocityType)
            {
                for (int jointIndex = 0; jointIndex < m_VelocityParents.Length; ++jointIndex)
                    ToggleRenderers<LineRenderer>(velocityType != VelocityType.None, m_VelocityParents[jointIndex].transform);
            }

            public void UpdateRootPose(XRHand hand)
            {
                var xform = m_JointXforms[XRHandJointID.Wrist.ToIndex()];
                xform.localPosition = hand.rootPose.position;
                xform.localRotation = hand.rootPose.rotation;
            }

            public void UpdateJoints(
                XROrigin xrOrigin,
                XRHand hand,
                bool areJointsTracked,
                bool drawMeshes,
                bool debugDrawJoints,
                VelocityType velocityType)
            {
                if (m_IsTracked != areJointsTracked)
                {
                    ToggleDrawMesh(areJointsTracked && drawMeshes);
                    ToggleDebugDrawJoints(areJointsTracked && debugDrawJoints);
                    SetVelocityType(areJointsTracked ? velocityType : VelocityType.None);
                    m_IsTracked = areJointsTracked;
                }

                if (!m_IsTracked)
                    return;

                var originTransform = xrOrigin.Origin.transform;
                var originPose = new Pose(originTransform.position, originTransform.rotation);

                var wristPose = Pose.identity;
                UpdateJoint(debugDrawJoints, velocityType, originPose, hand.GetJoint(XRHandJointID.Wrist), ref wristPose);
                UpdateJoint(debugDrawJoints, velocityType, originPose, hand.GetJoint(XRHandJointID.Palm), ref wristPose, false);

                for (int fingerIndex = (int)XRHandFingerID.Thumb;
                    fingerIndex <= (int)XRHandFingerID.Little;
                    ++fingerIndex)
                {
                    var parentPose = wristPose;
                    var fingerId = (XRHandFingerID)fingerIndex;

                    int jointIndexBack = fingerId.GetBackJointID().ToIndex();
                    for (int jointIndex = fingerId.GetFrontJointID().ToIndex();
                        jointIndex <= jointIndexBack;
                        ++jointIndex)
                    {
                        if (m_JointXforms[jointIndex] != null)
                            UpdateJoint(debugDrawJoints, velocityType, originPose, hand.GetJoint(XRHandJointIDUtility.FromIndex(jointIndex)), ref parentPose);
                    }
                }
            }

            void UpdateJoint(
                bool debugDrawJoints,
                VelocityType velocityType,
                Pose originPose,
                XRHandJoint joint,
                ref Pose parentPose,
                bool cacheParentPose = true)
            {
                int jointIndex = joint.id.ToIndex();
                var xform = m_JointXforms[jointIndex];
                if (xform == null || !joint.TryGetPose(out var pose))
                    return;

                m_DrawJoints[jointIndex].transform.localPosition = pose.position;
                m_DrawJoints[jointIndex].transform.localRotation = pose.rotation;

                if (debugDrawJoints && joint.id != XRHandJointID.Wrist)
                {
                    s_LinePointsReuse[0] = parentPose.GetTransformedBy(originPose).position;
                    s_LinePointsReuse[1] = pose.GetTransformedBy(originPose).position;
                    m_Lines[jointIndex].SetPositions(s_LinePointsReuse);
                }

                var inverseParentRotation = Quaternion.Inverse(parentPose.rotation);
                xform.localPosition = inverseParentRotation * (pose.position - parentPose.position);
                xform.localRotation = inverseParentRotation * pose.rotation;
                if (cacheParentPose)
                    parentPose = pose;

                if (velocityType != VelocityType.None && m_VelocityParents[jointIndex].TryGetComponent<LineRenderer>(out var renderer))
                {
                    m_VelocityParents[jointIndex].transform.localPosition = Vector3.zero;
                    m_VelocityParents[jointIndex].transform.localRotation = Quaternion.identity;

                    s_LinePointsReuse[0] = s_LinePointsReuse[1] = m_VelocityParents[jointIndex].transform.position;
                    if (velocityType == VelocityType.Linear)
                    {
                        if (joint.TryGetLinearVelocity(out var velocity))
                            s_LinePointsReuse[1] += velocity;
                    }
                    else if (velocityType == VelocityType.Angular)
                    {
                        if (joint.TryGetAngularVelocity(out var velocity))
                            s_LinePointsReuse[1] += 0.05f * velocity.normalized;
                    }

                    renderer.SetPositions(s_LinePointsReuse);
                }
            }

            static void ToggleRenderers<TRenderer>(bool toggle, Transform xform)
                where TRenderer : Renderer
            {
                if (xform.TryGetComponent<TRenderer>(out var renderer))
                    renderer.enabled = toggle;

                for (int childIndex = 0; childIndex < xform.childCount; ++childIndex)
                    ToggleRenderers<TRenderer>(toggle, xform.GetChild(childIndex));
            }
        }
    }
}
