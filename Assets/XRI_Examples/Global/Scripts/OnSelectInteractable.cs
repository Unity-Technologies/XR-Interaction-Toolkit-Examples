using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Triggers a Unity Event once when an interactable is selected by an interactor.
    /// </summary>
    public class OnSelectInteractable : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The interactable that is checked for selection.")]
        XRBaseInteractable m_TargetInteractable;

        [SerializeField]
        [Tooltip("The function to call when the interactable is selected.")]
        SelectEnterEvent m_OnSelected;

        void Start()
        {
            if (m_TargetInteractable != null)
                m_TargetInteractable.selectEntered.AddListener(OnSelected);
        }

        void OnSelected(SelectEnterEventArgs args)
        {
            m_OnSelected.Invoke(args);

            if (m_TargetInteractable != null)
                m_TargetInteractable.selectEntered.RemoveListener(OnSelected);
        }
    }
}
