// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using System.Linq;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace NorthStar
{
    /// <summary>
    /// A base class for objects that will be manipulated by an XR user which will trigger a response in external classes.
    /// </summary>
    /// <typeparam name="T">The expected type of value the inherited interactable class will output when manipulated</typeparam>
    public abstract class BaseJointInteractable<T> : MonoBehaviour
    {
        [SerializeField] private UnityEvent<T> m_onValueChange;
        [SerializeField] private UnityEvent<bool> m_onGrabChange;

        [SerializeField] private UnityEvent m_onGrabbed;
        [SerializeField] private UnityEvent m_onReleased;

        [SerializeField]
        protected ConfigurableJoint m_joint;

        [SerializeField, HideInInspector]
        protected Rigidbody m_jointRigidbody;

        [SerializeField]
        protected bool m_clampValues = false;

        [SerializeField, Obsolete("Add elements to m_grabbables instead.")]
        protected Grabbable m_grabbable;

        [SerializeField]
        protected List<Grabbable> m_grabbables;

        [SerializeField]
        protected bool m_requireGrabbing;

        [Space(10)]

        protected Vector3 m_tertiaryAxis;

        //[HideInInspector]
        private T m_value;
        public T Value
        {
            get => m_value;
            protected set
            {
                m_value = value;
                OnValueChange();
            }
        }

        protected virtual void OnValueChange() => m_onValueChange.Invoke(m_value);

        private bool m_wasGrabbed;

        public bool IsLocked => m_locked;
        private bool m_locked = false;

        #region Unity Methods
        protected void Start()
        {
            // TODO: determine if this belongs in Start or OnValidate
            m_tertiaryAxis = Vector3.Cross(m_joint.axis, m_joint.secondaryAxis);
            UpdateKinematicLock();

            m_onGrabChange.AddListener(isGrabbed => (isGrabbed ? m_onGrabbed : m_onReleased)?.Invoke());
        }

        protected virtual void OnGrabChange(bool isGrabbed) => m_onGrabChange?.Invoke(isGrabbed);

        protected virtual void Update()
        {
            var isGrabbing = IsGrabbing();
            if (isGrabbing && !m_wasGrabbed)
            {
                m_wasGrabbed = true;
                UpdateKinematicLock();
                OnGrabChange(true);
            }
            else if (!isGrabbing && m_wasGrabbed)
            {
                m_wasGrabbed = false;
                UpdateKinematicLock();
                OnGrabChange(false);
            }
        }

        // This is to ensure that these parameters are on the same object as m_joint
        private void OnValidate()
        {
            if (m_joint != null)
            {
                if (m_jointRigidbody == null || m_jointRigidbody.transform != m_joint.transform)
                {
                    m_jointRigidbody = m_joint.GetComponent<Rigidbody>();
                }
            }
        }
        #endregion

        #region Member Methods

#pragma warning disable CS0618 // Type or member is obsolete
        public bool IsGrabbing() =>
            (m_grabbable && m_grabbable.SelectingPointsCount > 0) ||
            m_grabbables.Any(x => x.SelectingPointsCount > 0);
#pragma warning restore CS0618 // Type or member is obsolete

        // Ensure we do not double handle the locking and unlocking of rigidbodies by keep track of its state while also using the RigidbodyKinematicLocker helper
        public virtual void Lock()
        {
            if (!m_locked)
            {
                m_locked = true;
                UpdateKinematicLock();
            }
            else
            {
                Debug.LogWarning("This object is already locked!", this);
            }
        }

        public virtual void Unlock()
        {
            if (m_locked)
            {
                m_locked = false;
                UpdateKinematicLock();
            }
            else
            {
                Debug.LogWarning("This object is already unlocked!", this);
            }
        }

        private void UpdateKinematicLock()
        {
            m_jointRigidbody.isKinematic = m_locked || (m_requireGrabbing && !IsGrabbing());
        }

        #endregion
    }
}