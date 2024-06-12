using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Grid Socket interactor that only selects and hovers interactables with a <see cref="Keychain"/> component containing specific keys.
    /// </summary>
    public class XRLockGridSocketInteractor : XRGridSocketInteractor
    {
        [Space]
        [SerializeField]
        [Tooltip("The required keys to interact with this socket.")]
        Lock m_Lock;

        /// <summary>
        /// The required keys to interact with this socket.
        /// </summary>
        public Lock keychainLock
        {
            get => m_Lock;
            set => m_Lock = value;
        }

        /// <inheritdoc />
        public override bool CanHover(IXRHoverInteractable interactable)
        {
            if (!base.CanHover(interactable))
                return false;

            var keyChain = interactable.transform.GetComponent<IKeychain>();
            return m_Lock.CanUnlock(keyChain);
        }

        /// <inheritdoc />
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            if (!base.CanSelect(interactable))
                return false;

            var keyChain = interactable.transform.GetComponent<IKeychain>();
            return m_Lock.CanUnlock(keyChain);
        }
    }
}
