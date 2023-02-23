using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEngine.XR.Content.Interaction
{
    public class DistanceCalculationToggle : MonoBehaviour
    {
        [SerializeField]
        Toggle m_Toggle;

        [SerializeField]
        XRBaseInteractable[] m_Interactables;

        void Start()
        {
            if (m_Toggle == null)
                return;

            OnToggleValueChanged(m_Toggle.isOn);
            m_Toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        void OnToggleValueChanged(bool value)
        {
            var distanceCalculationMode = value
                ? XRBaseInteractable.DistanceCalculationMode.ColliderVolume
                : XRBaseInteractable.DistanceCalculationMode.ColliderPosition;

            foreach (var interactable in m_Interactables)
            {
                if (interactable != null)
                    interactable.distanceCalculationMode = distanceCalculationMode;
            }
        }
    }
}
