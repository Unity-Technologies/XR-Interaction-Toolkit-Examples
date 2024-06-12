namespace UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard
{
    /// <summary>
    /// Manage the reuse and updating of data for each child <see cref="XRKeyboardKey"/> button. 
    /// </summary>
    public class XRKeyboardLayout : MonoBehaviour
    {
        [SerializeField]
        XRKeyboardConfig m_DefaultKeyMapping;

        /// <summary>
        /// Default key mapping for resetting key layout.
        /// </summary>
        public XRKeyboardConfig defaultKeyMapping
        {
            get => m_DefaultKeyMapping;
            set => m_DefaultKeyMapping = value;
        }

        [SerializeField]
        XRKeyboardConfig m_ActiveKeyMapping;

        /// <summary>
        /// Active <see cref="XRKeyboardConfig"/> which data is populated in these keys.
        /// </summary>
        public XRKeyboardConfig activeKeyMapping
        {
            get => m_ActiveKeyMapping;
            set
            {
                m_ActiveKeyMapping = value;
                PopulateKeys();
            }
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void Start()
        {
            PopulateKeys();
        }

        /// <summary>
        /// Sets the active key mapping to the default key mapping.
        /// </summary>
        /// <seealso cref="activeKeyMapping"/>
        /// <seealso cref="defaultKeyMapping"/>
        public void SetDefaultLayout()
        {
            if (m_DefaultKeyMapping != null)
                activeKeyMapping = m_DefaultKeyMapping;
        }

        /// <summary>
        /// Updates all child <see cref="XRKeyboardKey"/> buttons with the data from the <see cref="activeKeyMapping"/>. 
        /// </summary>
        /// <remarks>
        /// This function returns without changing the keys if the number of child <see cref="XRKeyboardKey"/> buttons is less than
        /// the number of mappings in the <see cref="activeKeyMapping"/>.
        /// </remarks>
        public void PopulateKeys()
        {
            if (m_ActiveKeyMapping == null)
                return;

            var keyMappings = m_ActiveKeyMapping.keyMappings;

            var keys = GetComponentsInChildren<XRKeyboardKey>();
            if (keys.Length < keyMappings.Count)

        	{
        		Debug.LogWarning("Keyboard layout update failed: There are fewer keys than key mappings in the current config. Ensure there is a correct number of keys and key mappings.", this);
        		return;
            }

            for (var i = 0; i < keyMappings.Count; ++i)
            {
                var mapping = keyMappings[i];
                var key = keys[i];

                key.character = mapping.character;
                key.displayCharacter = mapping.displayCharacter;

                key.shiftCharacter = mapping.shiftCharacter;
                key.shiftDisplayCharacter = mapping.shiftDisplayCharacter;

                key.keyFunction = mapping.overrideDefaultKeyFunction ? mapping.keyFunction : m_ActiveKeyMapping.defaultKeyFunction;
                key.keyCode = mapping.keyCode;

                key.displayIcon = mapping.displayIcon;
                key.shiftDisplayIcon = mapping.shiftDisplayIcon;

                key.SetButtonInteractable(!mapping.disabled);
            }
        }
    }
}
