using System;
using UnityEngine.UI;

namespace UnityEngine.XR.Content.Walkthrough
{
    /// <summary>
    /// Trigger that, when active, waits for a UI button to be pressed
    /// </summary>
    internal class ButtonPressTrigger : WalkthroughTrigger
    {
#pragma warning disable 649
        [SerializeField]
        [Tooltip("The UI button that when pressed, will allow this trigger to pass.")]
        Button m_ButtonToPress;

        [SerializeField]
        [Tooltip("Allow pressing of the button switch the step of the tutorial")]
        bool m_SwitchContext = true;

#pragma warning restore 649
        bool m_Triggered = false;

        void Start()
        {
            if (m_ButtonToPress == null)
                return;

            m_ButtonToPress.onClick.RemoveListener(ButtonPressHandler);
            m_ButtonToPress.onClick.AddListener(ButtonPressHandler);
        }

        public override bool ResetTrigger()
        {
            m_Triggered = false;
            if (m_ButtonToPress == null)
                return false;

            m_ButtonToPress.onClick.RemoveListener(ButtonPressHandler);
            m_ButtonToPress.onClick.AddListener(ButtonPressHandler);
            return true;
        }

        public override bool Check()
        {
            return m_Triggered;
        }

        void ButtonPressHandler()
        {
            // Attempt to switch to this step if this button is not part of the current step
            if (m_SwitchContext)
            {
                var parent = GetComponentInParent<WalkthroughStep>();
                var walkthrough = GetComponentInParent<Walkthrough>();
                if (parent != null && walkthrough != null)
                {
                    var steps = walkthrough.steps;
                    var stepIndex = Array.IndexOf(steps, parent);
                    if (stepIndex != walkthrough.currentStep)
                        walkthrough.SkipToStep(stepIndex);
                }
            }

            m_Triggered = true;
        }
    }
}
