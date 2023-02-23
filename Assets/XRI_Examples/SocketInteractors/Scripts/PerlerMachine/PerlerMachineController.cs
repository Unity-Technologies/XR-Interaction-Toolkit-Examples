namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// This class is responsible for creating the perler sockets grid and turning on/off the machine.
    /// </summary>
    public class PerlerMachineController : MonoBehaviour
    {
        static readonly string k_EmissionKeyword = "_EMISSION";

        [SerializeField]
        [Tooltip("The emissive materials that will change state whenever the machine is turned on/off")]
        Material[] m_EmissiveMaterials;

        bool m_MachineActive;

        void Awake()
        {
            DisableEmissiveMaterials();
        }

#if UNITY_EDITOR
        void OnDestroy()
        {
            EnableEmissiveMaterials();
        }
#endif

        void DisableEmissiveMaterials()
        {
            foreach (var material in m_EmissiveMaterials)
                material.DisableKeyword(k_EmissionKeyword);
        }

        void EnableEmissiveMaterials()
        {
            foreach (var material in m_EmissiveMaterials)
                material.EnableKeyword(k_EmissionKeyword);
        }

        /// <summary>
        /// Call this method to activate or deactivate the machine. This will also turn on/off its lights.
        /// Used by the BatterySlot GameObject socket.
        /// </summary>
        /// <param name="active">Value of <see langword="true"/> to activate the machine; <see langword="false"/> otherwise.</param>
        public void SetMachineActive(bool active)
        {
            // It's the same state?
            if (active == m_MachineActive)
                return;

            // Change the machine light state
            m_MachineActive = active;
            if (m_MachineActive)
                EnableEmissiveMaterials();
            else
                DisableEmissiveMaterials();
        }
    }
}
