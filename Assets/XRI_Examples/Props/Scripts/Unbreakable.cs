using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Rewinds positional changes made this object and its children to restore it back to a 'complete' object
    /// </summary>
    public class Unbreakable : MonoBehaviour
    {
        [Serializable] public class RestoreEvent : UnityEvent<GameObject> { }

        [SerializeField]
        [Tooltip("How long to wait before rewinding the object's motion.")]
        float m_RestTime = 1.0f;

        [SerializeField]
        [Tooltip("How long to spend restoring the object.")]
        float m_RestoreTime = 2.0f;

        [SerializeField]
        [Tooltip("A 'non broken' object to replace this object with when motion rewinding is complete.")]
        GameObject m_RestoredVersion;

        [SerializeField]
        [Tooltip("Events to fire when the 'non broken' object is restored.")]
        RestoreEvent m_OnRestore = new RestoreEvent();

        bool m_Resting = true;
        float m_Timer = 0.0f;
        bool m_Restored = false;

        struct ChildPoses
        {
            internal Pose m_StartPose;
            internal Pose m_EndPose;
        }

        Dictionary<Transform, ChildPoses> m_ChildPoses = new Dictionary<Transform, ChildPoses>();
        List<Transform> m_ChildTransforms = new List<Transform>();

        /// <summary>
        /// Events to fire when the 'non broken' object is restored.
        /// </summary>
        public RestoreEvent onRestore => m_OnRestore;

        void Start()
        {
            // Go through all children
            GetComponentsInChildren(m_ChildTransforms);

            // Cache their start positions
            foreach (var child in m_ChildTransforms)
            {
                m_ChildPoses.Add(child, new ChildPoses { m_StartPose = new Pose(child.position, child.rotation) });
            }
        }

        void Update()
        {
            if (m_Restored)
                return;

            // Phase 1 - wait to rewind
            // Phase 2 - rewind all positions, using a an inverse quadratic curve
            // Phase 3 - replace object, destroy this one

            m_Timer += Time.deltaTime;

            if (m_Resting)
            {
                if (m_Timer > m_RestTime)
                {
                    m_Timer = 0.0f;
                    m_Resting = false;

                    foreach (var child in m_ChildTransforms)
                    {
                        if (child == null)
                            continue;

                        var poses = m_ChildPoses[child];
                        poses.m_EndPose = new Pose(child.position, child.rotation);
                        m_ChildPoses[child] = poses;
                    }
                }
            }
            else
            {
                var timePercent = m_Timer / m_RestoreTime;
                if (timePercent > 1.0f)
                {
                    m_Restored = true;
                    var restoredVersion = Instantiate(m_RestoredVersion, transform.position, transform.rotation);
                    m_OnRestore.Invoke(restoredVersion);
                    Destroy(gameObject);
                }
                else
                {
                    timePercent = 1.0f - ((1.0f - timePercent) * (1.0f - timePercent));

                    foreach (var child in m_ChildTransforms)
                    {
                        if (child == null)
                            continue;

                        var poses = m_ChildPoses[child];
                        var lerpedPosition = Vector3.Lerp(poses.m_EndPose.position, poses.m_StartPose.position, timePercent);
                        var lerpedRotation = Quaternion.Slerp(poses.m_EndPose.rotation, poses.m_StartPose.rotation, timePercent);
                        child.position = lerpedPosition;
                        child.rotation = lerpedRotation;
                    }
                }
            }
        }
    }
}
