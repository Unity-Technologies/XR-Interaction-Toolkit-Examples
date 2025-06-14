// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using Meta.Utilities;
using Meta.Utilities.Ropes;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace NorthStar
{
    /// <summary>
    /// Controller for the harpoon interaction
    /// </summary>
    public class Harpoon : MonoBehaviour
    {
        [Space(5), SerializeField]
        private BaseJointInteractable<float> m_reloadInteractable;
        [SerializeField, Range(0, 1)]
        private float m_reloadThreshold = 1;
        public UnityEvent OnReload;

        [Space(5), SerializeField]
        private BaseJointInteractable<float> m_rearmInteractable;
        [SerializeField, Range(0, 1)]
        private float m_rearmThreshold = 1;
        public UnityEvent OnRearm;

        [Space(5), SerializeField]
        private AutomaticButton m_fireButton;
        public UnityEvent OnFire;

        [Space(5), SerializeField]
        private PhysicsTransformer m_jointTransformer;
        public UnityEvent OnGrabbed;

        [Space(10), SerializeField]
        private Transform m_lockingTransform; // Transform that moves when locking the harpoon
        [SerializeField] private Transform m_stringBone;
        [SerializeField]
        private GameObject m_harpoonPrefab;
        [SerializeField]
        public Transform BoltSpawnPoint;
        [SerializeField]
        private Rigidbody m_baseRigidbody;

        private HarpoonBolt m_bolt;

        private static List<HarpoonTarget> s_targets;
        public delegate void HarpoonTargetDelegate(List<HarpoonTarget> targets);
        public static HarpoonTargetDelegate FindTargets;

        private enum HarpoonState
        {
            Loading,    // Waiting for the user to reel in the harpoon using the crank
            Locking,    // Waiting for the user to pull back the locking bar ready to fire
            Armed,      // Waiting for the user to press the fire button
            Airborne    // Waiting for the harpoon bolt to hit something or time out
        }

        [Space(10), SerializeField]
        private HarpoonState m_state = HarpoonState.Locking;

        private bool m_loaded = false;
        private bool m_resetLocked = false;

        [Space(10), SerializeField]
        private Vector3 m_harpoonModelDisarmedPos;
        [SerializeField]
        private Vector3 m_harpoonModelArmedPos;
        [SerializeField] private float m_stringBoneArmedPos, m_stringBoneDisarmedPos;
        [Space(10), SerializeField]
        private float m_firingForce = 2500;

        [Header("Aiming Line"), SerializeField]
        private LineRenderer m_lineRenderer;
        [Space(5), SerializeField]
        private bool m_showLineRenderer;
        [SerializeField] private float m_simulationTime = 10f;

        private Vector3[] m_points = new Vector3[100];

        [SerializeField] private Transform m_ropeWheel;
        [SerializeField] private BurstRope m_reelRope;
        [SerializeField] private RopeTransformBinder m_fakeArrowBinder;
        [SerializeField] private float m_baseNodeDistance = 0.05f;

        [SerializeField] private float m_ropeWheelRotationDegreesPerSecond = -720;

        [SerializeField] private bool m_skipReload = false;
        [SerializeField] private bool m_useAimAssist = false;
        [SerializeField, Range(0, 360)] private float m_minAimAsstDirectionValue = .75f;

        private float m_ropeWheelReeledRotation;
        private float m_ropeWheelHitRotation;

        private int m_holdCount = 0;
        private void Start()
        {
            if (s_targets != null)
            {
                s_targets.Clear();
            }
            m_ropeWheelHitRotation = m_ropeWheelReeledRotation = m_ropeWheel.localEulerAngles.y;

            //m_reelRope.transform.parent = null;
            // Depending on how the harpoon is set on start, lock certain levers
            switch (m_state)
            {
                case HarpoonState.Airborne:
                    m_loaded = false;

                    m_lockingTransform.gameObject.SetActive(false);
                    m_lockingTransform.localPosition = m_harpoonModelDisarmedPos;

                    m_reloadInteractable.Lock();
                    m_rearmInteractable.Lock();
                    m_fireButton.Lock();
                    break;
                case HarpoonState.Loading:
                    m_loaded = false;

                    m_lockingTransform.gameObject.SetActive(false);
                    m_lockingTransform.localPosition = m_harpoonModelDisarmedPos;

                    m_reloadInteractable.Unlock();
                    m_rearmInteractable.Lock();
                    m_fireButton.Lock();
                    break;
                case HarpoonState.Locking:
                    m_loaded = true;

                    m_lockingTransform.gameObject.SetActive(true);
                    m_lockingTransform.localPosition = Vector3.Lerp(m_harpoonModelDisarmedPos, m_harpoonModelArmedPos, m_rearmInteractable.Value);

                    m_reloadInteractable.Lock();
                    m_rearmInteractable.Unlock();
                    m_fireButton.Lock();
                    break;
                case HarpoonState.Armed:
                    m_loaded = true;

                    m_lockingTransform.gameObject.SetActive(true);
                    m_lockingTransform.localPosition = m_harpoonModelArmedPos;

                    m_reloadInteractable.Lock();
                    m_rearmInteractable.Lock();
                    m_fireButton.Unlock();
                    break;
            }

            m_jointTransformer.OnInteraction += Grabbed;
            m_jointTransformer.OnEndInteraction += Released;
            Released(null); //Assume the harpoon starts not grabbed

            m_lineRenderer.positionCount = m_points.Length;
        }

        private void Update()
        {
            var boatExists = false;

            switch (m_state)
            {
                case HarpoonState.Loading:
                    m_ropeWheel.localRotation = Quaternion.AngleAxis(m_reloadInteractable.Value.ClampedMap(0, 1, m_ropeWheelHitRotation, m_ropeWheelReeledRotation), Vector3.up);
                    break;
                case HarpoonState.Locking:
                case HarpoonState.Armed:
                    m_ropeWheelHitRotation = m_ropeWheelReeledRotation;
                    m_ropeWheel.localRotation = Quaternion.AngleAxis(m_ropeWheelReeledRotation, Vector3.up);

                    break;
                case HarpoonState.Airborne:
                    // if for some reason we have no bolt, we should assume that it's not airborne
                    m_stringBone.localPosition = new Vector3(m_stringBone.localPosition.x, m_stringBone.localPosition.y, m_stringBoneDisarmedPos);
                    if (!m_bolt || !m_bolt.IsAirborne)
                    {
                        m_state = HarpoonState.Loading;
                        m_lockingTransform.gameObject.SetActive(false);
                        m_lockingTransform.localPosition = m_harpoonModelDisarmedPos;
                        m_reloadInteractable.Unlock();
                        m_rearmInteractable.Lock();
                        m_fireButton.Lock();

                        if (m_skipReload)
                        {
                            DoReload();
                        }
                    }
                    else
                    {
                        m_ropeWheelHitRotation += m_ropeWheelRotationDegreesPerSecond * Time.deltaTime;
                        m_ropeWheel.localRotation = Quaternion.AngleAxis(m_ropeWheelHitRotation, Vector3.up);
                    }

                    break;
            }

            if (m_lineRenderer && m_lineRenderer.enabled)
            {
                var start = BoltSpawnPoint.position;
               
                var u = BoltSpawnPoint.forward * m_firingForce;
                var a = Physics.gravity;
                for (var i = 0; i < m_points.Length; i++)
                {
                    var progress = (float)i / m_points.Length;
                    var t = m_simulationTime * progress;
                    // S = ut + 1/2 a t ^2
                    var newPos = start + (t * u + .5f * Mathf.Pow(t, 2) * a);
                    m_points[i] = newPos;
                }
                m_lineRenderer.SetPositions(m_points);
            }
        }

        private void Grabbed(GameObject interactor)
        {
            if (m_holdCount == 0)
            {
                m_baseRigidbody.isKinematic = false;
                m_lineRenderer.enabled = m_showLineRenderer;
            }
            m_holdCount++;
            OnGrabbed.Invoke();
        }
        private void Released(GameObject interactor)
        {
            m_holdCount--;
            if (m_holdCount <= 0)
            {
                m_holdCount = 0;
                m_baseRigidbody.isKinematic = true;
                m_lineRenderer.enabled = false;
            }

        }

        public void ReloadInput()
        {
            if (m_state != HarpoonState.Loading)
            {
                return;
            }

            var value = m_reloadInteractable.Value;

            if (m_bolt != null)
            {
                m_bolt.ReelBolt(value);
                m_reelRope.NodeDistance = Vector3.Distance(m_fakeArrowBinder.transform.position, m_bolt.transform.position) / m_reelRope.NodeCount;
                if (value >= 0.95f)
                {
                    m_reelRope.NodeDistance = m_baseNodeDistance;
                    m_fakeArrowBinder.enabled = true;
                }
            }

            if (value >= m_reloadThreshold)
            {
                DoReload();
            }
        }

        private List<HarpoonTarget> GetTargets()
        {
            s_targets ??= new List<HarpoonTarget>();
            FindTargets?.Invoke(s_targets);
            return s_targets;
        }

        private void DoReload()
        {
            m_lockingTransform.gameObject.SetActive(true);
            m_lockingTransform.localPosition = m_harpoonModelDisarmedPos;

            m_loaded = true;

            m_reloadInteractable.Lock();
            m_rearmInteractable.Unlock();

            m_state = HarpoonState.Locking;

            OnReload.Invoke();
        }

        public void RearmInput()
        {
            if (m_state != HarpoonState.Locking)
            {
                return;
            }

            var value = m_rearmInteractable.Value;
            m_stringBone.localPosition = new Vector3(m_stringBone.localPosition.x, m_stringBone.localPosition.y, Mathf.Lerp(m_stringBoneDisarmedPos, m_stringBoneArmedPos, value));
            // The user has to reset the handle to 0 to continue
            if (!m_resetLocked)
            {
                if (value <= (1 - m_rearmThreshold))
                {
                    m_resetLocked = true;
                }
                else
                {
                    return;
                }
            }

            m_lockingTransform.localPosition = Vector3.Lerp(m_harpoonModelDisarmedPos, m_harpoonModelArmedPos, m_rearmInteractable.Value);
            if (value >= m_rearmThreshold)
            {
                m_resetLocked = false;

                m_lockingTransform.localPosition = m_harpoonModelArmedPos;

                m_rearmInteractable.Lock();
                m_fireButton.Unlock();
                m_fireButton.Release();

                m_state = HarpoonState.Armed;

                OnRearm.Invoke();
            }
        }

        public void FireInput()
        {
            if (m_state != HarpoonState.Armed)
            {
                return;
            }

            if (m_loaded && m_fireButton.Pressed)
            {
                FireHarpoon();
                m_loaded = false;

                m_fireButton.Lock();
                m_reloadInteractable.Lock();
                // m_reloadInteractable.Unlock();

                m_state = HarpoonState.Airborne;

                OnFire.Invoke();
            }
            // The user has to reset the handle to 0 to continue
            //if (!m_loaded && value <= (1 - m_fireThreshold))
            //{
            //
            //}
        }

        private Vector3 GetAimAssistDir(Vector3 position, Vector3 dir)
        {
            var targets = GetTargets();
            var bestTarget = dir;
            var bestVal = 10000f;

            foreach (var target in targets)
            {
                var toTarget = target.transform.position - position;
                toTarget.Normalize();
                var val = Vector3.Angle(toTarget, dir);
                if (val > m_minAimAsstDirectionValue)
                    continue;
                if (val < bestVal)
                {
                    bestTarget = toTarget;
                    bestVal = val;
                }
            }

            return bestTarget;
        }

        private Vector3 GetTargetVector(Vector3 position, Vector3 dir, bool aimAssist)
        {
            if (aimAssist)
            {
                dir = GetAimAssistDir(position, dir);
            }

            if (!Physics.Raycast(position, dir, out var hit))
                return dir;
            if (!hit.collider.TryGetComponent(out UntargetableObject target))
            {
                Debug.LogWarning(hit.collider.gameObject, hit.collider.gameObject);
                return dir;
            }


            var offset = Vector3.Cross(dir, Vector3.up);
            offset *= Mathf.Sign(Vector3.Dot(hit.point - position, offset)) * target.Radius;
            var newPos = hit.point + offset;
            return Vector3.Normalize(newPos - position);
        }

        [ContextMenu("Fire")] //Debug button for firing harpoon
        private void FireHarpoon()
        {
            Debug.Log("Fired");
            m_lockingTransform.gameObject.SetActive(false);

            //TODO: get offset with boat rather than using the boat as a parent
            //Script dictates the boat will be still so potentially don't need to get boat details

            var boatExists = false;
            Vector3 spawnPosition;
            Quaternion spawnRotation;
            GameObject bolt;

            spawnPosition = BoltSpawnPoint.position;
            spawnRotation = BoltSpawnPoint.rotation;
            bolt = Instantiate(m_harpoonPrefab, spawnPosition, spawnRotation);
            var binder = bolt.GetComponentInChildren<RopeTransformBinder>();
            binder.Rope = m_reelRope;
            binder.NodeIndex = m_reelRope.NodeCount - 1;
            binder.BindIndex = 1;
            binder.Enable();
            m_fakeArrowBinder.enabled = false;

            m_bolt = bolt.GetComponent<HarpoonBolt>();

            var spawnDir = GetTargetVector(spawnPosition, spawnRotation * Vector3.forward, m_useAimAssist);

            var estimatedHitTime = 0.0f;
            if (Physics.Raycast(spawnPosition, spawnDir, out var hit))
            {
                estimatedHitTime = hit.distance / m_firingForce;
            }


            m_bolt.Fire(spawnDir, spawnRotation * Vector3.forward, m_firingForce, estimatedHitTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(BoltSpawnPoint.position, .1f);

            Gizmos.color = Color.red;

            var boatExists = false;
            Vector3 spawnPosition;
            Quaternion spawnRotation;

            if (boatExists)
            { //FIX THIS LATER
                spawnPosition = BoltSpawnPoint.position; //BoatController.WorldToBoatSpace(BoltSpawnPoint.position);//(BoatController.Instance.MovementSource.CurrentRotation * (BoltSpawnPoint.position - BoatController.Instance.transform.position)) + BoatController.Instance.MovementSource.CurrentPosition;
                spawnRotation = BoltSpawnPoint.rotation;//BoatController.WorldToBoatSpace(BoltSpawnPoint.rotation); //BoltSpawnPoint.rotation * Quaternion.Inverse(BoatController.Instance.transform.rotation) * BoatController.Instance.MovementSource.CurrentRotation;
                //TODO: convert to object pooling
            }
            else
            {
                spawnPosition = BoltSpawnPoint.position;
                spawnRotation = BoltSpawnPoint.rotation;
            }
            m_fakeArrowBinder.enabled = false;
            _ = GetTargetVector(spawnPosition, spawnRotation * Vector3.forward, m_useAimAssist);

            for (var i = 0; i < 100; i++)
            {
                var dir = BoltSpawnPoint.rotation * Quaternion.Euler(0, 0, (float)i / 100 * 360) * Quaternion.Euler(m_minAimAsstDirectionValue, 0, 0) * Vector3.forward;
                Gizmos.DrawRay(spawnPosition, dir * 100);
            }
        }

    }
}
