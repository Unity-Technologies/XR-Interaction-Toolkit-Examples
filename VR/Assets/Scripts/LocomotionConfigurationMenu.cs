using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace UnityEngine.XR.Interaction.Toolkit.Examples
{
    /// <summary>
    /// Use this class to present locomotion control schemes and configuration preferences,
    /// and respond to player input in the UI to set them.
    /// </summary>
    /// <seealso cref="LocomotionSchemeManager"/>
    public class LocomotionConfigurationMenu : MonoBehaviour
    {
        class EnumDropdownCache<T> where T : Enum
        {
            public List<Dropdown.OptionData> options { get; } = new List<Dropdown.OptionData>();

            readonly List<T> m_Values = new List<T>();

            public EnumDropdownCache()
            {
                foreach (var name in Enum.GetNames(typeof(T)))
                {
                    options.Add(new Dropdown.OptionData(name));
                }

                foreach (var value in Enum.GetValues(typeof(T)))
                {
                    m_Values.Add((T)value);
                }
            }

            public T GetValue(int index)
            {
                return m_Values[index];
            }

            public int FindIndex(T value)
            {
                // This will not distinguish between enums with duplicate values,
                // and will always select the first equal value.
                var comparer = EqualityComparer<T>.Default;
                for (var index = 0; index < m_Values.Count; ++index)
                {
                    if (comparer.Equals(m_Values[index], value))
                    {
                        return index;
                    }
                }

                return -1;
            }
        }

        [SerializeField]
        [Tooltip("Stores the Toggle used to enable or disable continuous movement locomotion.")]
        Toggle m_ContinuousMoveToggle;
        /// <summary>
        /// Stores the <see cref="Toggle"/> used to enable or disable continuous movement locomotion.
        /// </summary>
        public Toggle continuousMoveToggle
        {
            get => m_ContinuousMoveToggle;
            set
            {
                UnsubscribeContinuousMove(m_ContinuousMoveToggle);
                m_ContinuousMoveToggle = value;
                SubscribeContinuousMove(m_ContinuousMoveToggle);
            }
        }

        [SerializeField]
        [Tooltip("Stores the Slider used to set the move speed of continuous movement.")]
        Slider m_MoveSpeedSlider;
        /// <summary>
        /// Stores the <see cref="Slider"/> used to set the move speed of continuous movement.
        /// </summary>
        public Slider moveSpeedSlider
        {
            get => m_MoveSpeedSlider;
            set
            {
                UnsubscribeMoveSpeed(m_MoveSpeedSlider);
                m_MoveSpeedSlider = value;
                SubscribeMoveSpeed(m_MoveSpeedSlider, m_MoveSpeedValueText);
                RefreshMoveDependentInteractable();
            }
        }

        [SerializeField]
        [Tooltip("Stores the Text used to display the current move speed of continuous movement.")]
        Text m_MoveSpeedValueText;
        /// <summary>
        /// Stores the <see cref="Text"/> used to display the current move speed of continuous movement.
        /// </summary>
        public Text moveSpeedValueText
        {
            get => m_MoveSpeedValueText;
            set
            {
                UnsubscribeMoveSpeed(m_MoveSpeedSlider);
                m_MoveSpeedValueText = value;
                SubscribeMoveSpeed(m_MoveSpeedSlider, m_MoveSpeedValueText);
            }
        }

        [SerializeField]
        [Tooltip("Stores the Toggle used to enable or disable strafing (sideways movement) of continuous movement.")]
        Toggle m_EnableStrafeToggle;
        /// <summary>
        /// Stores the <see cref="Toggle"/> used to enable or disable strafing (sideways movement) of continuous movement.
        /// </summary>
        public Toggle enableStrafeToggle
        {
            get => m_EnableStrafeToggle;
            set
            {
                UnsubscribeEnableStrafe(m_EnableStrafeToggle);
                m_EnableStrafeToggle = value;
                SubscribeEnableStrafe(m_EnableStrafeToggle);
                RefreshMoveDependentInteractable();
            }
        }

        [SerializeField]
        [Tooltip("Stores the Toggle used to enable or disable gravity on continuous movement.")]
        Toggle m_UseGravityToggle;
        /// <summary>
        /// Stores the <see cref="Toggle"/> used to enable or disable gravity on continuous movement.
        /// </summary>
        public Toggle useGravityToggle
        {
            get => m_UseGravityToggle;
            set
            {
                UnsubscribeUseGravity(m_UseGravityToggle);
                m_UseGravityToggle = value;
                SubscribeUseGravity(m_UseGravityToggle);
                RefreshMoveDependentInteractable();
            }
        }

        [SerializeField]
        [Tooltip("Stores the Dropdown used to select when gravity is applied with continuous movement.")]
        Dropdown m_GravityApplicationModeDropdown;
        /// <summary>
        /// Stores the <see cref="Dropdown"/> used to select when gravity is applied with continuous movement.
        /// </summary>
        public Dropdown gravityApplicationModeDropdown
        {
            get => m_GravityApplicationModeDropdown;
            set
            {
                UnsubscribeGravityApplicationMode(m_GravityApplicationModeDropdown);
                m_GravityApplicationModeDropdown = value;
                SubscribeGravityApplicationMode(m_GravityApplicationModeDropdown);
                RefreshMoveDependentInteractable();
            }
        }

        [SerializeField]
        [Tooltip("Stores the Dropdown used to select the source Transform to define the forward direction of continuous movement.")]
        Dropdown m_ForwardSourceDropdown;
        /// <summary>
        /// Stores the <see cref="Dropdown"/> used to select the source <see cref="Transform"/> to define the forward direction of continuous movement.
        /// </summary>
        public Dropdown forwardSourceDropdown
        {
            get => m_ForwardSourceDropdown;
            set
            {
                UnsubscribeForwardSource(m_ForwardSourceDropdown);
                m_ForwardSourceDropdown = value;
                SubscribeForwardSource(m_ForwardSourceDropdown);
                RefreshMoveDependentInteractable();
            }
        }

        [SerializeField]
        [Tooltip("Stores the Toggle used to enable or disable continuous turn locomotion.")]
        Toggle m_ContinuousTurnToggle;
        /// <summary>
        /// Stores the <see cref="Toggle"/> used to enable or disable continuous turn locomotion.
        /// </summary>
        public Toggle continuousTurnToggle
        {
            get => m_ContinuousTurnToggle;
            set
            {
                UnsubscribeContinuousTurn(m_ContinuousTurnToggle);
                m_ContinuousTurnToggle = value;
                SubscribeContinuousTurn(m_ContinuousTurnToggle);
            }
        }

        [SerializeField]
        [Tooltip("Stores the Slider used to set the turn speed of continuous turning.")]
        Slider m_TurnSpeedSlider;
        /// <summary>
        /// Stores the <see cref="Slider"/> used to set the turn speed of continuous turning.
        /// </summary>
        public Slider turnSpeedSlider
        {
            get => m_TurnSpeedSlider;
            set
            {
                UnsubscribeTurnSpeed(m_TurnSpeedSlider);
                m_TurnSpeedSlider = value;
                SubscribeTurnSpeed(m_TurnSpeedSlider, m_TurnSpeedValueText);
                RefreshTurnDependentInteractable();
            }
        }

        [SerializeField]
        [Tooltip("Stores the Text used to display the current turn speed of continuous turning.")]
        Text m_TurnSpeedValueText;
        /// <summary>
        /// Stores the <see cref="Text"/> used to display the current turn speed of continuous turning.
        /// </summary>
        public Text turnSpeedValueText
        {
            get => m_TurnSpeedValueText;
            set
            {
                UnsubscribeTurnSpeed(m_TurnSpeedSlider);
                m_TurnSpeedValueText = value;
                SubscribeTurnSpeed(m_TurnSpeedSlider, m_TurnSpeedValueText);
            }
        }

        [SerializeField]
        [Tooltip("Stores the Dropdown used to select the number of degrees to rotate for snap turning.")]
        Dropdown m_SnapTurnAmountDropdown;
        /// <summary>
        /// Stores the <see cref="Dropdown"/> used to select the number of degrees to rotate for snap turning.
        /// </summary>
        public Dropdown snapTurnAmountDropdown
        {
            get => m_SnapTurnAmountDropdown;
            set
            {
                UnsubscribeSnapTurnAmount(m_SnapTurnAmountDropdown);
                m_SnapTurnAmountDropdown = value;
                SubscribeSnapTurnAmount(m_SnapTurnAmountDropdown);
                RefreshTurnDependentInteractable();
            }
        }

        [SerializeField]
        [Tooltip("Stores the Toggle used to enable or disable 180° snap turns.")]
        Toggle m_EnableTurnAroundToggle;
        /// <summary>
        /// Stores the <see cref="Toggle"/> used to enable or disable 180° snap turns.
        /// </summary>
        public Toggle enableTurnAroundToggle
        {
            get => m_EnableTurnAroundToggle;
            set
            {
                UnsubscribeEnableTurnAround(m_EnableTurnAroundToggle);
                m_EnableTurnAroundToggle = value;
                SubscribeEnableTurnAround(m_EnableTurnAroundToggle);
            }
        }

        [SerializeField]
        [Tooltip("Stores the behavior that will be used to configure locomotion control schemes and configuration preferences.")]
        LocomotionSchemeManager m_Manager;
        /// <summary>
        /// Stores the behavior that will be used to configure locomotion control schemes and configuration preferences.
        /// </summary>
        public LocomotionSchemeManager manager
        {
            get => m_Manager;
            set
            {
                UnsubscribeAll();
                m_Manager = value;
                if (m_Manager != null)
                {
                    SubscribeAll();
                    RefreshInteractable();
                }
            }
        }

        static readonly List<float> k_SnapTurnAmounts = new List<float> { 15f, 30f, 45f, 60f, 75f, 90f };

        static readonly List<Dropdown.OptionData> k_SnapTurnAmountOptions = new List<Dropdown.OptionData>(k_SnapTurnAmounts.Count);

        static EnumDropdownCache<ContinuousMoveProviderBase.GravityApplicationMode> s_GravityApplicationModeDropdownCache;

        static EnumDropdownCache<LocomotionSchemeManager.MoveForwardSource> s_ForwardSourceDropdownCache;

        protected void Awake()
        {
            // Initialize Dropdown options for Snap Turn Amount
            if (k_SnapTurnAmountOptions.Count != k_SnapTurnAmounts.Count)
            {
                foreach (var value in k_SnapTurnAmounts)
                {
                    k_SnapTurnAmountOptions.Add(new Dropdown.OptionData($"{value}°"));
                }
            }

            // Initialize Dropdown options for Gravity Application Mode
            if (s_GravityApplicationModeDropdownCache == null)
            {
                s_GravityApplicationModeDropdownCache = new EnumDropdownCache<ContinuousMoveProviderBase.GravityApplicationMode>();
            }

            // Initialize Dropdown options for Forward Source
            if (s_ForwardSourceDropdownCache == null)
            {
                s_ForwardSourceDropdownCache = new EnumDropdownCache<LocomotionSchemeManager.MoveForwardSource>();
            }
        }

        protected void OnEnable()
        {
            if (!ValidateManager())
                return;

            SubscribeAll();
            RefreshInteractable();
        }

        protected void OnDisable()
        {
            UnsubscribeAll();
        }

        void SubscribeAll()
        {
            SubscribeContinuousMove(m_ContinuousMoveToggle);
            SubscribeMoveSpeed(m_MoveSpeedSlider, m_MoveSpeedValueText);
            SubscribeEnableStrafe(m_EnableStrafeToggle);
            SubscribeUseGravity(m_UseGravityToggle);
            SubscribeGravityApplicationMode(m_GravityApplicationModeDropdown);
            SubscribeForwardSource(m_ForwardSourceDropdown);
            SubscribeContinuousTurn(m_ContinuousTurnToggle);
            SubscribeTurnSpeed(m_TurnSpeedSlider, m_TurnSpeedValueText);
            SubscribeSnapTurnAmount(m_SnapTurnAmountDropdown);
            SubscribeEnableTurnAround(m_EnableTurnAroundToggle);
        }

        void UnsubscribeAll()
        {
            UnsubscribeContinuousMove(m_ContinuousMoveToggle);
            UnsubscribeMoveSpeed(m_MoveSpeedSlider);
            UnsubscribeEnableStrafe(m_EnableStrafeToggle);
            UnsubscribeUseGravity(m_UseGravityToggle);
            UnsubscribeGravityApplicationMode(m_GravityApplicationModeDropdown);
            UnsubscribeForwardSource(m_ForwardSourceDropdown);
            UnsubscribeContinuousTurn(m_ContinuousTurnToggle);
            UnsubscribeTurnSpeed(m_TurnSpeedSlider);
            UnsubscribeSnapTurnAmount(m_SnapTurnAmountDropdown);
            UnsubscribeEnableTurnAround(m_EnableTurnAroundToggle);
        }

        /// <summary>
        /// Grey out input options that don't apply to the current control scheme.
        /// </summary>
        void RefreshInteractable()
        {
            if (!ValidateManager())
                return;

            RefreshMoveDependentInteractable();
            RefreshTurnDependentInteractable();
        }

        void RefreshMoveDependentInteractable()
        {
            if (!ValidateManager())
                return;

            var continuousMove = m_Manager.moveScheme == LocomotionSchemeManager.MoveScheme.Continuous;
            RefreshMoveDependentInteractable(continuousMove);
        }

        void RefreshMoveDependentInteractable(bool continuous)
        {
            if (m_MoveSpeedSlider != null)
                m_MoveSpeedSlider.interactable = continuous;
            if (m_EnableStrafeToggle != null)
                m_EnableStrafeToggle.interactable = continuous;
            if (m_UseGravityToggle != null)
                m_UseGravityToggle.interactable = continuous;
            if (m_GravityApplicationModeDropdown != null)
                m_GravityApplicationModeDropdown.interactable = continuous;
            if (m_ForwardSourceDropdown != null)
                m_ForwardSourceDropdown.interactable = continuous;
        }

        void RefreshTurnDependentInteractable()
        {
            if (!ValidateManager())
                return;

            var continuousTurn = m_Manager.turnStyle == LocomotionSchemeManager.TurnStyle.Continuous;
            RefreshTurnDependentInteractable(continuousTurn);
        }

        void RefreshTurnDependentInteractable(bool continuous)
        {
            if (m_TurnSpeedSlider != null)
                m_TurnSpeedSlider.interactable = continuous;
            if (m_SnapTurnAmountDropdown != null)
                m_SnapTurnAmountDropdown.interactable = !continuous;
        }

        bool ValidateManager()
        {
            if (m_Manager == null)
            {
                Debug.LogError($"Reference to the {nameof(LocomotionSchemeManager)} is not set or the object has been destroyed," +
                    " configuring locomotion settings from the menu will not be possible." +
                    " Ensure the value has been set in the Inspector.", this);
                return false;
            }

            if (m_Manager.continuousMoveProvider == null)
            {
                Debug.LogError($"Reference to the {nameof(ContinuousMoveProviderBase)} is not set or the object has been destroyed," +
                    " configuring locomotion settings from the menu will not be possible." +
                    $" Ensure the value has been set in the Inspector on {m_Manager}.", this);
                return false;
            }

            if (m_Manager.continuousTurnProvider == null)
            {
                Debug.LogError($"Reference to the {nameof(ContinuousTurnProviderBase)} is not set or the object has been destroyed," +
                    " configuring locomotion settings from the menu will not be possible." +
                    $" Ensure the value has been set in the Inspector on {m_Manager}.", this);
                return false;
            }

            if (m_Manager.snapTurnProvider == null)
            {
                Debug.LogError($"Reference to the {nameof(SnapTurnProviderBase)} is not set or the object has been destroyed," +
                    " configuring locomotion settings from the menu will not be possible." +
                    $" Ensure the value has been set in the Inspector on {m_Manager}.", this);
                return false;
            }

            return true;
        }

        void SubscribeContinuousMove(Toggle toggle)
        {
            if (toggle == null)
                return;
            if (!ValidateManager())
                return;

            var continuousMove = m_Manager.moveScheme == LocomotionSchemeManager.MoveScheme.Continuous;
            toggle.isOn = continuousMove;
            toggle.onValueChanged.AddListener(OnContinuousMoveToggleValueChanged);
        }

        void SubscribeMoveSpeed(Slider slider, Text valueText)
        {
            if (slider == null)
                return;
            if (!ValidateManager())
                return;

            var currentMoveSpeed = m_Manager.continuousMoveProvider.moveSpeed;
            if (currentMoveSpeed < slider.minValue || currentMoveSpeed > slider.maxValue)
            {
                Debug.LogError($"Move speed {currentMoveSpeed} is outside [{slider.minValue}, {slider.maxValue}] range of slider.");
            }

            slider.value = currentMoveSpeed;
            if (valueText != null)
            {
                valueText.text = $"{currentMoveSpeed:F2}";
            }

            slider.onValueChanged.AddListener(OnMoveSpeedSliderValueChanged);
        }

        void SubscribeEnableStrafe(Toggle toggle)
        {
            if (toggle == null)
                return;
            if (!ValidateManager())
                return;

            toggle.isOn = m_Manager.continuousMoveProvider.enableStrafe;
            toggle.onValueChanged.AddListener(OnEnableStrafeToggleValueChanged);
        }

        void SubscribeUseGravity(Toggle toggle)
        {
            if (toggle == null)
                return;
            if (!ValidateManager())
                return;

            toggle.isOn = m_Manager.continuousMoveProvider.useGravity;
            toggle.onValueChanged.AddListener(OnUseGravityToggleValueChanged);
        }

        void SubscribeGravityApplicationMode(Dropdown dropdown)
        {
            if (dropdown == null)
                return;
            if (!ValidateManager())
                return;

            dropdown.options = s_GravityApplicationModeDropdownCache.options;
            dropdown.value = s_GravityApplicationModeDropdownCache.FindIndex(m_Manager.continuousMoveProvider.gravityApplicationMode);
            dropdown.onValueChanged.AddListener(OnGravityApplicationModeDropdownValueChanged);
        }

        void SubscribeForwardSource(Dropdown dropdown)
        {
            if (dropdown == null)
                return;
            if (!ValidateManager())
                return;

            dropdown.options = s_ForwardSourceDropdownCache.options;
            dropdown.value = s_ForwardSourceDropdownCache.FindIndex(m_Manager.moveForwardSource);
            dropdown.onValueChanged.AddListener(OnForwardSourceDropdownValueChanged);
        }

        void SubscribeContinuousTurn(Toggle toggle)
        {
            if (toggle == null)
                return;
            if (!ValidateManager())
                return;

            var continuousTurn = m_Manager.turnStyle == LocomotionSchemeManager.TurnStyle.Continuous;
            toggle.isOn = continuousTurn;
            toggle.onValueChanged.AddListener(OnContinuousTurnToggleValueChanged);
        }

        void SubscribeTurnSpeed(Slider slider, Text valueText)
        {
            if (slider == null)
                return;
            if (!ValidateManager())
                return;

            var currentTurnSpeed = m_Manager.continuousTurnProvider.turnSpeed;
            if (currentTurnSpeed < slider.minValue || currentTurnSpeed > slider.maxValue)
            {
                Debug.LogError($"Turn speed {currentTurnSpeed} is outside [{slider.minValue}, {slider.maxValue}] range of slider.");
            }

            slider.value = currentTurnSpeed;
            if (valueText != null)
            {
                valueText.text = currentTurnSpeed.ToString();
            }

            slider.onValueChanged.AddListener(OnTurnSpeedSliderValueChanged);
        }

        void SubscribeSnapTurnAmount(Dropdown dropdown)
        {
            if (dropdown == null)
                return;
            if (!ValidateManager())
                return;

            dropdown.options = k_SnapTurnAmountOptions;
            var currentTurnAmount = m_Manager.snapTurnProvider.turnAmount;

            // Find the index of the current turn amount within the options list of the Dropdown
            var snapTurnIndex = -1;
            for (var index = 0; index < k_SnapTurnAmounts.Count; ++index)
            {
                var value = k_SnapTurnAmounts[index];
                if (Mathf.Approximately(value, currentTurnAmount))
                {
                    snapTurnIndex = index;
                    break;
                }
            }

            if (snapTurnIndex < 0)
            {
                Debug.LogError($"Turn amount {currentTurnAmount} is not contained within options list {{{string.Join(", ", k_SnapTurnAmounts)}}}", this);
            }
            else
            {
                dropdown.value = snapTurnIndex;
            }

            dropdown.onValueChanged.AddListener(OnSnapTurnAmountDropdownValueChanged);
        }

        void SubscribeEnableTurnAround(Toggle toggle)
        {
            if (toggle == null)
                return;
            if (!ValidateManager())
                return;

            toggle.isOn = m_Manager.snapTurnProvider.enableTurnAround;
            toggle.onValueChanged.AddListener(OnEnableTurnAroundToggleValueChanged);
        }

        void UnsubscribeContinuousMove(Toggle toggle)
        {
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(OnContinuousMoveToggleValueChanged);
        }

        void UnsubscribeMoveSpeed(Slider slider)
        {
            if (slider != null)
                slider.onValueChanged.RemoveListener(OnMoveSpeedSliderValueChanged);
        }

        void UnsubscribeEnableStrafe(Toggle toggle)
        {
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(OnEnableStrafeToggleValueChanged);
        }

        void UnsubscribeUseGravity(Toggle toggle)
        {
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(OnUseGravityToggleValueChanged);
        }

        void UnsubscribeGravityApplicationMode(Dropdown dropdown)
        {
            if (dropdown != null)
                dropdown.onValueChanged.RemoveListener(OnGravityApplicationModeDropdownValueChanged);
        }

        void UnsubscribeForwardSource(Dropdown dropdown)
        {
            if (dropdown != null)
                dropdown.onValueChanged.RemoveListener(OnForwardSourceDropdownValueChanged);
        }

        void UnsubscribeContinuousTurn(Toggle toggle)
        {
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(OnContinuousTurnToggleValueChanged);
        }

        void UnsubscribeTurnSpeed(Slider slider)
        {
            if (slider != null)
                slider.onValueChanged.RemoveListener(OnTurnSpeedSliderValueChanged);
        }

        void UnsubscribeSnapTurnAmount(Dropdown dropdown)
        {
            if (dropdown != null)
                dropdown.onValueChanged.RemoveListener(OnSnapTurnAmountDropdownValueChanged);
        }

        void UnsubscribeEnableTurnAround(Toggle toggle)
        {
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(OnEnableTurnAroundToggleValueChanged);
        }

        void OnContinuousMoveToggleValueChanged(bool value)
        {
            m_Manager.moveScheme = value
                ? LocomotionSchemeManager.MoveScheme.Continuous
                : LocomotionSchemeManager.MoveScheme.Noncontinuous;

            RefreshMoveDependentInteractable(value);
        }

        void OnMoveSpeedSliderValueChanged(float value)
        {
            m_Manager.continuousMoveProvider.moveSpeed = value;
            m_MoveSpeedValueText.text = $"{value:F2}";
        }

        void OnEnableStrafeToggleValueChanged(bool value)
        {
            m_Manager.continuousMoveProvider.enableStrafe = value;
        }

        void OnUseGravityToggleValueChanged(bool value)
        {
            m_Manager.continuousMoveProvider.useGravity = value;
        }

        void OnGravityApplicationModeDropdownValueChanged(int index)
        {
            m_Manager.continuousMoveProvider.gravityApplicationMode = s_GravityApplicationModeDropdownCache.GetValue(index);
        }

        void OnForwardSourceDropdownValueChanged(int index)
        {
            m_Manager.moveForwardSource = s_ForwardSourceDropdownCache.GetValue(index);
        }

        void OnContinuousTurnToggleValueChanged(bool value)
        {
            m_Manager.turnStyle = value
                ? LocomotionSchemeManager.TurnStyle.Continuous
                : LocomotionSchemeManager.TurnStyle.Snap;

            RefreshTurnDependentInteractable(value);
        }

        void OnTurnSpeedSliderValueChanged(float value)
        {
            m_Manager.continuousTurnProvider.turnSpeed = value;
            m_TurnSpeedValueText.text = value.ToString();
        }

        void OnSnapTurnAmountDropdownValueChanged(int index)
        {
            var turnAmount = k_SnapTurnAmounts[index];
            m_Manager.snapTurnProvider.turnAmount = turnAmount;
        }

        void OnEnableTurnAroundToggleValueChanged(bool value)
        {
            m_Manager.snapTurnProvider.enableTurnAround = value;
        }
    }
}
