using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEngine.XR.Content.Interaction.Analytics
{
    /// <summary>
    /// Class that connects the 3DUI Interaction station scene objects with their respective analytics events.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    class Xrc3DUIInteractionStationAnalytics : MonoBehaviour
    {
        [Header("3DUI Simple Controls Substation")]
        [SerializeField]
        XRLever m_Lever;

        [SerializeField]
        XRJoystick m_Joystick;

        [SerializeField]
        XRKnob m_Dial;

        [SerializeField]
        XRKnob m_Wheel;

        [SerializeField]
        XRSlider m_Slider;

        [SerializeField]
        XRGripButton m_GripButton;

        [SerializeField]
        XRPushButton m_PushButton;

        [Header("Claw Machine Substation")]
        [SerializeField]
        XRJoystick m_ClawMachineJoystick;

        [SerializeField]
        XRPushButton m_ClawMachinePushButton;

        [SerializeField]
        XRSocketInteractor m_UfoGrabberSocket;

        [SerializeField]
        XRBaseInteractable[] m_PrizeInteractables;

        void Awake()
        {
            XrcAnalyticsUtils.Register(m_Lever, new LeverInteraction());
            XrcAnalyticsUtils.Register(m_Joystick, new JoystickInteraction());
            XrcAnalyticsUtils.Register(m_Dial, new DialInteraction());
            XrcAnalyticsUtils.Register(m_Wheel, new WheelInteraction());
            XrcAnalyticsUtils.Register(m_Slider, new SliderInteraction());
            XrcAnalyticsUtils.Register(m_GripButton, new GripButtonPressed());
            XrcAnalyticsUtils.Register(m_PushButton, new PushButtonPressed());

            XrcAnalyticsUtils.Register(m_ClawMachineJoystick, new ClawMachineJoystickInteraction());
            XrcAnalyticsUtils.Register(m_ClawMachinePushButton, new ClawMachinePushButtonPressed());
            XrcAnalyticsUtils.Register(m_UfoGrabberSocket, new ConnectClawMachineToPrize());
            XrcAnalyticsUtils.Register(m_PrizeInteractables, new GrabClawMachinePrize());
        }
    }
}
