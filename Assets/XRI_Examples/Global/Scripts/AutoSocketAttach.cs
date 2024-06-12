using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Initializes an <see cref="XRSocketInteractor"/> attach point to match the initial scene position of the object it is containing.
    /// </summary>
    public class AutoSocketAttach : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The Socket Interactor that controls this socket attach point.")]
        XRSocketInteractor m_ControllingInteractor;

        void Start()
        {
            // If there is an existing interactable, we match its position so the object does not move
            if (m_ControllingInteractor == null)
                m_ControllingInteractor = GetComponentInParent<XRSocketInteractor>();

            if (m_ControllingInteractor == null)
            {
                Debug.LogWarning("Script is not associated with an XRSocketInteractor and will have no effect.", this);
                return;
            }

            if (m_ControllingInteractor.startingSelectedInteractable == null)
            {
                Debug.Log("AutoSocketAttach does not have a starting selected interactable to match its position.", this);
                return;
            }

            var targetTransform = m_ControllingInteractor.startingSelectedInteractable.GetAttachTransform(m_ControllingInteractor);
            transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
        }
    }
}
