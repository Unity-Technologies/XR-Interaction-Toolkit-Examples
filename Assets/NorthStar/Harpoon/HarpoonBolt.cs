// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
//using Meta.Utilities.Environment;
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// The projectile launched from the harpoon
    /// </summary>
    public class HarpoonBolt : MonoBehaviour
    {
        [SerializeField] private Rigidbody m_rigidbody;
        [SerializeField] private LineRenderer m_lineRenderer;
        [SerializeField] private float m_maxAirTime = 20;
        [SerializeField] private float m_colliderDelay = 0.2f;
        [SerializeField] private Collider m_collider;
        public bool UseVisualOffset;
        [SerializeField] private bool m_hideOnHit = false;
        [SerializeField] private Transform m_visual;
        [SerializeField] private Transform m_tip, m_end;
        [SerializeField] private float m_visualYOffset = .01f;
        [SerializeField] private AnimationCurve m_distanceOffsetCurve;
        [SerializeField] private LayerMask m_layerMask = int.MaxValue;
       // [SerializeField] private EffectAsset m_hitParticles;
        [SerializeField] private ParticleSystem m_trailParticle;

        private List<Vector3> m_points = new();

        private Vector3 m_firedPosition;
        private Vector3 m_landedPosition;
        private Vector3 m_visualDirection;
        private float m_timeOfLaunch;
        private float m_hitTime;

        private bool m_airborne = true;
        public bool IsAirborne => m_airborne;
        private float m_airTimeRemaining;
        private float m_colliderDelayTimeRemaining;

        private HarpoonTarget m_hitTarget;

        public void Fire(Vector3 actualDir, Vector3 intendedDir, float force, float estimatedHitTime)
        {
            m_rigidbody.AddForce(actualDir * force, ForceMode.VelocityChange);
            m_visualDirection = intendedDir * force;
            m_timeOfLaunch = Time.time;
            m_hitTime = estimatedHitTime;
        }

        private Vector3 GetVisualPosition()
        {
            var dt = Time.time - m_timeOfLaunch;
            var offset = m_visualDirection * dt + .5f * Physics.gravity * dt * dt;
            return m_firedPosition + offset;
        }

        private void Start()
        {
            m_firedPosition = transform.position;
            m_points.Add(transform.position);
            m_airTimeRemaining = m_maxAirTime;
            m_colliderDelayTimeRemaining = m_colliderDelay;
            m_airborne = true;
            m_rigidbody.isKinematic = false;
            if (m_trailParticle != null)
            {
                var main = m_trailParticle.main;
               // main.customSimulationSpace = BoatController.Instance.transform;
            }
            if (m_collider) m_collider.enabled = false;
        }

        private void Update()
        {
            var dt = Time.time - m_timeOfLaunch;
            var t = dt / m_hitTime;
            m_visual.transform.position = Vector3.Lerp(GetVisualPosition(), transform.position, t);
            if (m_airborne)
            {
                m_points.Add(transform.position);

                m_lineRenderer.positionCount = m_points.Count;
                m_lineRenderer.SetPositions(m_points.ToArray());
                transform.forward = m_rigidbody.linearVelocity.normalized;

                // enable the collider after a certain time has passed
                if (m_collider && !m_collider.enabled && m_colliderDelayTimeRemaining > 0)
                {
                    m_colliderDelayTimeRemaining -= Time.deltaTime;
                    if (m_colliderDelayTimeRemaining <= 0)
                    {
                        m_collider.enabled = true;
                    }
                }

                //After too much time has passed, force the projectile to stop and be reeled in
                m_airTimeRemaining -= Time.deltaTime;
                if (m_airTimeRemaining < 0)
                {
                    m_airborne = false;
                    m_rigidbody.isKinematic = true;
                    m_landedPosition = transform.position;
                }
            }
            else
            {
                if (!UseVisualOffset)
                    return;
                var tipHeight = SampleHeight(m_tip.position);
                var endHeight = SampleHeight(m_end.position);

                var centreHeight = (tipHeight + endHeight) / 2;
                var pos = transform.position;
                pos.y = centreHeight + m_visualYOffset;
                m_visual.position = pos;

                var tipPos = m_tip.position;
                tipPos.y = tipHeight;

                var endPos = m_end.position;
                endPos.y = endHeight;

                m_visual.forward = tipPos - endPos;

                if (m_hitTarget != null)
                {
                    m_hitTarget.transform.position = tipPos;
                }
            }
        }

        private float SampleHeight(Vector3 pos)
        {
            var t = m_distanceOffsetCurve.Evaluate(HorizontalDistance(pos, m_firedPosition));
            if (Physics.Raycast(pos, Vector3.down, out var hit, float.PositiveInfinity, m_layerMask))
            {
                return Mathf.Lerp(hit.point.y, pos.y, t);
            }
            else
            {
               
            }
            return pos.y;

        }

        private float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0;
            b.y = 0;
            return Vector3.Distance(a, b);
        }

        public void ReelBolt(float value)
        {
            if (!m_airborne)
            {
                transform.position = Vector3.Lerp(m_landedPosition, m_firedPosition, value);
            }

            if (value >= 0.95f)
            {
                if (m_hitTarget != null)
                {
                    if (m_hitTarget.TryGetComponent(out Rigidbody body))
                    {
                        body.isKinematic = false;
                    }
                    m_hitTarget.Reeled();
                }
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (m_airborne)
            {
                m_airborne = false;
                m_rigidbody.isKinematic = true;
                m_landedPosition = transform.position;

                if (collision.collider.attachedRigidbody != null)
                {
                    if (collision.collider.attachedRigidbody.TryGetComponent(out HarpoonTarget target))
                    {
                        target.Hit();
                        m_hitTarget = target;
                        if (target.Reelable)
                        {
                            if (m_hitTarget.TryGetComponent(out Rigidbody body))
                            {
                                body.isKinematic = true;
                            }
                        }
                    }
                }
                

                if (m_hideOnHit)
                {
                    m_visual.GetComponent<Renderer>().enabled = false;

                    //gameObject.SetActive(false);
                }
                if (m_visual.TryGetComponent(out HarpoonTrail trail))
                    trail.RecordPositions = false;
            }
        }
    }
}
