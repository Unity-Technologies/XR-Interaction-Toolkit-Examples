using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// This class is responsible to update the claw position and its state.
    /// The 3 states (NoPrize, TryGrabPrize and ReleasePrize) are coroutines and they update
    /// the claw speed, particles and the UfoAbductionForce
    /// <seealso cref="XRJoystick"/> and <seealso cref="XRPushButton"/>
    /// </summary>
    public class ClawMachine : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The claw transform that will be updated and translated")]
        Transform m_ClawTransform;

        [SerializeField]
        [Tooltip("The claw socket used to get the prizes")]
        XRSocketInteractor m_ClawSocket;

        [SerializeField]
        [Tooltip("The component used to apply a force on the prizes")]
        UfoAbductionForce m_UfoAbductionForce;

        [SerializeField]
        [Tooltip("The claw speed when not carrying a prize")]
        float m_ClawWithoutPrizeSpeed;

        [SerializeField]
        [Tooltip("The claw speed when carrying a prize")]
        float m_ClawWithPrizeSpeed;

        [SerializeField]
        [Tooltip("The claw speed when the UfoAbductionForce is enabled")]
        float m_ClawAbductionSpeed;

        [SerializeField]
        [Tooltip("The claw's minimum local position. Used to clamp the claw position")]
        Vector2 m_MinClawPosition;

        [SerializeField]
        [Tooltip("The claw's maximum local position. Used to clamp the claw position")]
        Vector2 m_MaxClawPosition;

        [SerializeField]
        [Tooltip("The Sparklies particle. This particle is activated while the UfoAbductionForce is enabled")]
        ParticleSystem m_SparkliesParticle;

        [SerializeField]
        [Tooltip("The UfoBeam particle. This particle is activated while the PushButton is held down")]
        ParticleSystem m_UfoBeamParticle;

        bool m_ButtonPressed;
        Vector2 m_JoystickValue;

        void Start()
        {
            StartCoroutine(NoPrizeState());
        }

        void UpdateClawPosition(float speed)
        {
            // Get current claw position
            var clawPosition = m_ClawTransform.localPosition;

            // Calculate claw velocity and new position
            clawPosition += new Vector3(m_JoystickValue.x * speed * Time.deltaTime, 0f,
                m_JoystickValue.y * speed * Time.deltaTime);

            // Clamp claw position
            clawPosition.x = Mathf.Clamp(clawPosition.x, m_MinClawPosition.x, m_MaxClawPosition.x);
            clawPosition.z = Mathf.Clamp(clawPosition.z, m_MinClawPosition.y, m_MaxClawPosition.y);

            // Update claw position
            m_ClawTransform.localPosition = clawPosition;
        }

        IEnumerator NoPrizeState()
        {
            // Move the claw
            while (!m_ButtonPressed)
            {
                UpdateClawPosition(m_ClawWithoutPrizeSpeed);
                yield return null;
            }

            StartCoroutine(TryGrabPrizeState());
        }

        IEnumerator TryGrabPrizeState()
        {
            // Start particles, activate the Socket and the UfoAbductionForce
            m_SparkliesParticle.Play();
            m_UfoBeamParticle.Play();
            m_ClawSocket.socketActive = true;
            m_UfoAbductionForce.enabled = true;

            // Try get a prize, the claw can still move
            while (m_ButtonPressed && !m_ClawSocket.hasSelection)
            {
                UpdateClawPosition(m_ClawAbductionSpeed);
                yield return null;
            }

            // Disable abduction force and the Sparklies particle
            m_UfoAbductionForce.enabled = false;
            m_SparkliesParticle.Stop();

            StartCoroutine(ReleasePrizeState());
        }

        IEnumerator ReleasePrizeState()
        {
            // Move the claw
            while (m_ButtonPressed)
            {
                UpdateClawPosition(m_ClawWithPrizeSpeed);
                yield return null;
            }

            // Release the prize and stop the last particle
            m_ClawSocket.socketActive = false;
            m_UfoBeamParticle.Stop();

            StartCoroutine(NoPrizeState());
        }

        /// <summary>
        /// Updates the internal state of the push button used by this class.
        /// Called by the <c>XRPushButton.OnPress</c> event.
        /// </summary>
        public void OnButtonPress()
        {
            m_ButtonPressed = true;
        }

        /// <summary>
        /// Updates the internal state of the push button used by this class.
        /// Called by the <c>XRPushButton.OnRelease</c> event.
        /// </summary>
        public void OnButtonRelease()
        {
            m_ButtonPressed = false;
        }

        /// <summary>
        /// Gets the X value of the joystick. Called by the <c>XRJoystick.OnValueChangeX</c> event.
        /// </summary>
        /// <param name="x">The joystick's X value</param>
        public void OnJoystickValueChangeX(float x)
        {
            m_JoystickValue.x = x;
        }

        /// <summary>
        /// Gets the Y value of the joystick. Called by the <c>XRJoystick.OnValueChangeY</c> event.
        /// </summary>
        /// <param name="y">The joystick's Y value</param>
        public void OnJoystickValueChangeY(float y)
        {
            m_JoystickValue.y = y;
        }
    }
}
