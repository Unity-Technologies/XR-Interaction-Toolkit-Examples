using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace UnityEngine.XR.Content.Interaction
{
    public class TargetFilterToggle : MonoBehaviour
    {
        [SerializeField]
        Toggle m_Toggle;

        [SerializeField]
        XRTargetFilter m_TargetFilter;

        [SerializeField]
        XRBaseInteractor m_LeftInteractor;

        [SerializeField]
        XRBaseInteractor m_RightInteractor;

        void Start()
        {
            if (m_Toggle == null || m_TargetFilter == null)
                return;

            m_Toggle.isOn = m_TargetFilter != null && m_TargetFilter.enabled;
            m_Toggle.onValueChanged.AddListener(OnToggleValueChanged);

            if (m_LeftInteractor != null && m_LeftInteractor.targetFilter == null)
                m_LeftInteractor.targetFilter = m_TargetFilter;

            if (m_RightInteractor != null && m_RightInteractor.targetFilter == null)
                m_RightInteractor.targetFilter = m_TargetFilter;
        }

        void OnToggleValueChanged(bool value)
        {
            if (m_TargetFilter != null)
                m_TargetFilter.enabled = value;
        }
    }
}
