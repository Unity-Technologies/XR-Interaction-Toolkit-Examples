using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEngine.XR.Content.Interaction
{
    public class Door : MonoBehaviour
    {
        [SerializeField]
        HingeJoint m_DoorJoint;

        [SerializeField]
        [Tooltip("Transform joint that pulls a door to follow an interactor")]
        TransformJoint m_DoorPuller;

        [SerializeField]
        GameObject m_KeyKnob;

        [SerializeField]
        float m_HandleOpenValue = 0.1f;

        [SerializeField]
        float m_HandleCloseValue = 0.5f;

        [SerializeField]
        float m_HingeCloseAngle = 5.0f;

        [SerializeField]
        float m_KeyLockValue = 0.9f;

        [SerializeField]
        float m_KeyUnlockValue = 0.1f;

        [SerializeField]
        float m_KeyPullDistance = 0.1f;

        [SerializeField]
        [Tooltip("Events to fire when the door is locked.")]
        UnityEvent m_OnLock = new UnityEvent();

        [SerializeField]
        [Tooltip("Events to fire when the door is unlocked.")]
        UnityEvent m_OnUnlock = new UnityEvent();

        JointLimits m_OpenDoorLimits;
        JointLimits m_ClosedDoorLimits;
        bool m_Closed = false;
        float m_LastHandleValue = 1.0f;

        bool m_Locked = false;

        GameObject m_KeySocket;
        IXRSelectInteractable m_Key;

        XRBaseInteractor m_KnobInteractor;
        Transform m_KnobInteractorAttachTransform;

        /// <summary>
        /// Events to fire when the door is locked.
        /// </summary>
        public UnityEvent onLock => m_OnLock;

        /// <summary>
        /// Events to fire when the door is unlocked.
        /// </summary>
        public UnityEvent onUnlock => m_OnUnlock;

        void Start()
        {
            m_OpenDoorLimits = m_DoorJoint.limits;
            m_ClosedDoorLimits = m_OpenDoorLimits;
            m_ClosedDoorLimits.min = 0.0f;
            m_ClosedDoorLimits.max = 0.0f;
            m_DoorJoint.limits = m_ClosedDoorLimits;
            m_KeyKnob.SetActive(false);
            m_Closed = true;
        }

        void Update()
        {
            // If the door is open, keep track of the hinge joint and see if it enters a state where it should close again
            if (!m_Closed)
            {
                if (m_LastHandleValue < m_HandleCloseValue)
                    return;

                if (Mathf.Abs(m_DoorJoint.angle) < m_HingeCloseAngle)
                {
                    m_DoorJoint.limits = m_ClosedDoorLimits;
                    m_Closed = true;
                }
            }

            if (m_KnobInteractor != null && m_KnobInteractorAttachTransform != null)
            {
                var distance = (m_KnobInteractorAttachTransform.position - m_KeyKnob.transform.position).magnitude;

                // If over threshold, break and grant the key back to the interactor
                if (distance > m_KeyPullDistance)
                {
                    var newKeyInteractor = m_KnobInteractor;
                    m_KeySocket.SetActive(true);
                    m_Key.transform.gameObject.SetActive(true);
                    newKeyInteractor.interactionManager.SelectEnter(newKeyInteractor, m_Key);
                    m_KeyKnob.SetActive(false);
                }
            }
        }

        public void BeginDoorPulling(SelectEnterEventArgs args)
        {
            m_DoorPuller.connectedBody = args.interactorObject.GetAttachTransform(args.interactableObject);
            m_DoorPuller.enabled = true;
        }

        public void EndDoorPulling()
        {
            m_DoorPuller.enabled = false;
            m_DoorPuller.connectedBody = null;
        }

        public void DoorHandleUpdate(float handleValue)
        {
            m_LastHandleValue = handleValue;

            if (!m_Closed || m_Locked)
                return;

            if (handleValue < m_HandleOpenValue)
            {
                m_DoorJoint.limits = m_OpenDoorLimits;
                m_Closed = false;
            }
        }

        public void KeyDropUpdate(SelectEnterEventArgs args)
        {
            m_KeySocket = args.interactorObject.transform.gameObject;
            m_Key = args.interactableObject;
            m_KeySocket.SetActive(false);
            m_Key.transform.gameObject.SetActive(false);
            m_KeyKnob.SetActive(true);
        }

        public void KeyUpdate(float keyValue)
        {
            if (!m_Locked && keyValue > m_KeyLockValue)
            {
                m_Locked = true;
                m_OnLock.Invoke();
            }

            if (m_Locked && keyValue < m_KeyUnlockValue)
            {
                m_Locked = false;
                m_OnUnlock.Invoke();
            }
        }

        public void KeyLockSelect(SelectEnterEventArgs args)
        {
            m_KnobInteractor = args.interactorObject as XRBaseInteractor;
            m_KnobInteractorAttachTransform = args.interactorObject.GetAttachTransform(args.interactableObject);
        }

        public void KeyLockDeselect(SelectExitEventArgs args)
        {
            m_KnobInteractor = null;
            m_KnobInteractorAttachTransform = null;
        }
    }
}
