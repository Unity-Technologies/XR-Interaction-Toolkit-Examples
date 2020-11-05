using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine.InputSystem;

namespace UnityEngine.XR.Interaction.Toolkit.Examples
{
    /// <summary>
    /// Use this class as a central manager to configure locomotion control schemes and configuration preferences.
    /// </summary>
    /// <remarks>
    /// Input bindings will often overlap between different locomotion methods, and this class can be used to
    /// set binding masks which are used to determine which bindings of an action to enable and which to ignore.
    /// 
    /// <example>
    /// Teleport  (Input Action)
    /// (1) Binding &lt;XRController&gt;{LeftHand}/PrimaryButton (Use in control scheme "Generic XR")
    /// (2) Binding &lt;XRController&gt;{LeftHand}/Primary2DAxis (Use in control scheme "Noncontinuous Move")
    /// Move (Input Action)
    /// (3) Binding &lt;XRController&gt;{LeftHand}/Primary2DAxis (Use in control scheme "Continuous Move")
    ///
    /// Set <see cref="baseControlScheme"/>="Generic XR"
    /// Set <see cref="noncontinuousControlScheme"/>="Noncontinuous Move"
    /// Set <see cref="continuousControlScheme"/>="Continuous Move"
    /// Set <see cref="actions"/> to be both input actions (Teleport and Move).
    ///
    /// When <see cref="moveScheme"/>=<see cref="MoveScheme.Noncontinuous"/>,
    /// bindings (1) and (2) will be enabled, but binding (3) will be disabled.
    ///
    /// When <see cref="moveScheme"/>=<see cref="MoveScheme.Continuous"/>,
    /// bindings (1) and (3) will be enabled, but binding (2) will be disabled.
    /// </example>
    /// </remarks>
    public class LocomotionSchemeManager : MonoBehaviour
    {
        /// <summary>
        /// Sets which movement control scheme to use.
        /// </summary>
        /// <seealso cref="moveScheme"/>
        public enum MoveScheme
        {
            /// <summary>
            /// Use noncontinuous movement control scheme.
            /// </summary>
            Noncontinuous,

            /// <summary>
            /// Use continuous movement control scheme.
            /// </summary>
            Continuous,
        }

        /// <summary>
        /// Sets which turn style of locomotion to use.
        /// </summary>
        /// <seealso cref="turnStyle"/>
        public enum TurnStyle
        {
            /// <summary>
            /// Use snap turning to rotate the direction you are facing by snapping by a specified angle.
            /// </summary>
            Snap,

            /// <summary>
            /// Use continuous turning to smoothly rotate the direction you are facing by a specified speed.
            /// </summary>
            Continuous,
        }

        /// <summary>
        /// Sets which orientation the forward direction of continuous movement is relative to.
        /// </summary>
        /// <seealso cref="moveForwardSource"/>
        /// <seealso cref="ContinuousMoveProviderBase.forwardSource"/>
        public enum MoveForwardSource
        {
            /// <summary>
            /// Use to continuously move in a direction based on the head orientation.
            /// </summary>
            Head,

            /// <summary>
            /// Use to continuously move in a direction based on the left hand orientation.
            /// </summary>
            LeftHand,

            /// <summary>
            /// Use to continuously move in a direction based on the right hand orientation.
            /// </summary>
            RightHand,
        }

        [SerializeField]
        [Tooltip("Controls which movement control scheme to use.")]
        MoveScheme m_MoveScheme;
        /// <summary>
        /// Controls which movement control scheme to use.
        /// </summary>
        /// <seealso cref="MoveScheme"/>
        public MoveScheme moveScheme
        {
            get => m_MoveScheme;
            set
            {
                SetMoveScheme(value);
                m_MoveScheme = value;
            }
        }

        [SerializeField]
        [Tooltip("Controls which turn style of locomotion to use.")]
        TurnStyle m_TurnStyle;
        /// <summary>
        /// Controls which turn style of locomotion to use.
        /// </summary>
        /// <seealso cref="TurnStyle"/>
        public TurnStyle turnStyle
        {
            get => m_TurnStyle;
            set
            {
                SetTurnStyle(value);
                m_TurnStyle = value;
            }
        }

        [SerializeField]
        [Tooltip("Controls which orientation the forward direction of continuous movement is relative to.")]
        MoveForwardSource m_MoveForwardSource;
        /// <summary>
        /// Controls which orientation the forward direction of continuous movement is relative to.
        /// </summary>
        /// <seealso cref="MoveForwardSource"/>
        public MoveForwardSource moveForwardSource
        {
            get => m_MoveForwardSource;
            set
            {
                SetMoveForwardSource(value);
                m_MoveForwardSource = value;
            }
        }

        [SerializeField]
        [Tooltip("Input action assets associated with locomotion to affect when the active movement control scheme is set." +
            " Can use this list by itself or together with the Action Maps list to set control scheme masks by Asset or Map.")]
        List<InputActionAsset> m_ActionAssets;
        /// <summary>
        /// Input action assets associated with locomotion to affect when the active movement control scheme is set.
        /// Can use this list by itself or together with the Action Maps list to set control scheme masks by Asset or Map.
        /// </summary>
        /// <seealso cref="actionMaps"/>
        public List<InputActionAsset> actionAssets
        {
            get => m_ActionAssets;
            set => m_ActionAssets = value;
        }

        [SerializeField]
        [Tooltip("Input action maps associated with locomotion to affect when the active movement control scheme is set." +
            " Can use this list together with the Action Assets list to set control scheme masks by Map instead of the whole Asset.")]
        List<string> m_ActionMaps;
        /// <summary>
        /// Input action maps associated with locomotion to affect when the active movement control scheme is set.
        /// Can use this list together with the Action Assets list to set control scheme masks by Map instead of the whole Asset.
        /// </summary>
        /// <seealso cref="actionAssets"/>
        public List<string> actionMaps
        {
            get => m_ActionMaps;
            set => m_ActionMaps = value;
        }

        [SerializeField]
        [Tooltip("Input actions associated with locomotion to affect when the active movement control scheme is set." +
            " Can use this list to select exactly the actions to affect instead of setting control scheme masks by Asset or Map.")]
        List<InputActionReference> m_Actions;
        /// <summary>
        /// Input actions associated with locomotion that are affected by the active movement control scheme.
        /// Can use this list to select exactly the actions to affect instead of setting control scheme masks by Asset or Map.
        /// </summary>
        /// <seealso cref="actionAssets"/>
        /// <seealso cref="actionMaps"/>
        public List<InputActionReference> actions
        {
            get => m_Actions;
            set => m_Actions = value;
        }

        [SerializeField]
        [Tooltip("Name of an input control scheme that defines the grouping of bindings that should remain enabled when applying any movement control scheme." +
            " Control schemes are created and named in the Input Actions window. The other movement control schemes are applied additively to this scheme." +
            " Can be an empty string, which means only bindings that match the specified movement control scheme will be enabled.")]
        string m_BaseControlScheme;
        /// <summary>
        /// Name of an input control scheme that defines the grouping of bindings that should remain enabled when applying any movement control scheme.
        /// Control schemes are created and named in the Input Actions window. The other movement control schemes are applied additively to this scheme.
        /// Can be an empty string, which means only bindings that match the specified movement control scheme will be enabled.
        /// </summary>
        public string baseControlScheme
        {
            get => m_BaseControlScheme;
            set => m_BaseControlScheme = value;
        }

        [SerializeField]
        [Tooltip("Name of an input control scheme that defines the grouping of bindings that should remain enabled when applying the noncontinuous movement control scheme." +
            " Control schemes are created and named in the Input Actions window. Can be an empty string, which means only bindings that match the" +
            " base control scheme will be enabled.")]
        string m_NoncontinuousControlScheme;
        /// <summary>
        /// Name of an input control scheme that defines the grouping of bindings that should remain enabled when applying the noncontinuous movement control scheme.
        /// Control schemes are created and named in the Input Actions window. Can be an empty string, which means only bindings that match the
        /// base control scheme will be enabled.
        /// </summary>
        public string noncontinuousControlScheme
        {
            get => m_NoncontinuousControlScheme;
            set => m_NoncontinuousControlScheme = value;
        }

        [SerializeField]
        [Tooltip("Name of an input control scheme that defines the grouping of bindings that should remain enabled when applying the continuous movement control scheme." +
            " Control schemes are created and named in the Input Actions window. Can be an empty string, which means only bindings that match the" +
            " base control scheme will be enabled.")]
        string m_ContinuousControlScheme;
        /// <summary>
        /// Name of an input control scheme that defines the grouping of bindings that should remain enabled when applying the continuous movement control scheme.
        /// Control schemes are created and named in the Input Actions window. Can be an empty string, which means only bindings that match the
        /// base control scheme will be enabled.
        /// </summary>
        public string continuousControlScheme
        {
            get => m_ContinuousControlScheme;
            set => m_ContinuousControlScheme = value;
        }

        [SerializeField]
        [Tooltip("Stores the locomotion provider for continuous movement.")]
        ContinuousMoveProviderBase m_ContinuousMoveProvider;
        /// <summary>
        /// Stores the locomotion provider for continuous movement.
        /// </summary>
        /// <seealso cref="ContinuousMoveProviderBase"/>
        public ContinuousMoveProviderBase continuousMoveProvider
        {
            get => m_ContinuousMoveProvider;
            set => m_ContinuousMoveProvider = value;
        }

        [SerializeField]
        [Tooltip("Stores the locomotion provider for continuous turning.")]
        ContinuousTurnProviderBase m_ContinuousTurnProvider;
        /// <summary>
        /// Stores the locomotion provider for continuous turning.
        /// </summary>
        /// <seealso cref="ContinuousTurnProviderBase"/>
        public ContinuousTurnProviderBase continuousTurnProvider
        {
            get => m_ContinuousTurnProvider;
            set => m_ContinuousTurnProvider = value;
        }

        [SerializeField]
        [Tooltip("Stores the locomotion provider for snap turning.")]
        SnapTurnProviderBase m_SnapTurnProvider;
        /// <summary>
        /// Stores the locomotion provider for snap turning.
        /// </summary>
        /// <seealso cref="SnapTurnProviderBase"/>
        public SnapTurnProviderBase snapTurnProvider
        {
            get => m_SnapTurnProvider;
            set => m_SnapTurnProvider = value;
        }

        [SerializeField]
        [Tooltip("Stores the \"Head\" Transform used with continuous movement when inputs should be relative to head orientation (usually the main camera).")]
        Transform m_HeadForwardSource;
        /// <summary>
        /// Stores the "Head" <see cref="Transform"/> used with continuous movement when inputs should be relative to head orientation (usually the main camera).
        /// </summary>
        public Transform headForwardSource
        {
            get => m_HeadForwardSource;
            set => m_HeadForwardSource = value;
        }

        [SerializeField]
        [Tooltip("Stores the \"Left Hand\" Transform used with continuous movement when inputs should be relative to the left hand's orientation.")]
        Transform m_LeftHandForwardSource;
        /// <summary>
        /// Stores the "Left Hand" <see cref="Transform"/> used with continuous movement when inputs should be relative to the left hand's orientation.
        /// </summary>
        public Transform leftHandForwardSource
        {
            get => m_LeftHandForwardSource;
            set => m_LeftHandForwardSource = value;
        }

        [SerializeField]
        [Tooltip("Stores the \"Right Hand\" Transform used with continuous movement when inputs should be relative to the right hand's orientation.")]
        Transform m_RightHandForwardSource;
        /// <summary>
        /// Stores the "Right Hand" <see cref="Transform"/> used with continuous movement when inputs should be relative to the right hand's orientation.
        /// </summary>
        public Transform rightHandForwardSource
        {
            get => m_RightHandForwardSource;
            set => m_RightHandForwardSource = value;
        }

        void OnEnable()
        {
            SetMoveScheme(m_MoveScheme);
            SetTurnStyle(m_TurnStyle);
            SetMoveForwardSource(m_MoveForwardSource);
        }

        void OnDisable()
        {
            ClearBindingMasks();
        }

        void SetMoveScheme(MoveScheme scheme)
        {
            switch (scheme)
            {
                case MoveScheme.Noncontinuous:
                    SetBindingMasks(m_NoncontinuousControlScheme);
                    if (m_ContinuousMoveProvider != null)
                    {
                        m_ContinuousMoveProvider.enabled = false;
                    }

                    break;
                case MoveScheme.Continuous:
                    SetBindingMasks(m_ContinuousControlScheme);
                    if (m_ContinuousMoveProvider != null)
                    {
                        m_ContinuousMoveProvider.enabled = true;
                    }

                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(scheme), (int)scheme, typeof(MoveScheme));
            }
        }

        void SetTurnStyle(TurnStyle style)
        {
            switch (style)
            {
                case TurnStyle.Snap:
                    if (m_ContinuousTurnProvider != null)
                    {
                        m_ContinuousTurnProvider.enabled = false;
                    }

                    if (m_SnapTurnProvider != null)
                    {
                        // TODO: If the Continuous Turn and Snap Turn providers both use the same
                        // action, then disabling the first provider will cause the action to be
                        // disabled, so the action needs to be enabled, which is done by forcing
                        // the OnEnable() of the second provider to be called.
                        // ReSharper disable Unity.InefficientPropertyAccess
                        m_SnapTurnProvider.enabled = false;
                        m_SnapTurnProvider.enabled = true;
                        // ReSharper restore Unity.InefficientPropertyAccess
                        m_SnapTurnProvider.enableTurnLeftRight = true;
                    }
                    break;
                case TurnStyle.Continuous:
                    if (m_SnapTurnProvider != null)
                    {
                        m_SnapTurnProvider.enableTurnLeftRight = false;
                    }

                    if (m_ContinuousTurnProvider != null)
                    {
                        m_ContinuousTurnProvider.enabled = true;
                    }
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(style), (int)style, typeof(TurnStyle));
            }
        }

        void SetMoveForwardSource(MoveForwardSource forwardSource)
        {
            if (m_ContinuousMoveProvider == null)
            {
                Debug.LogError($"Cannot set forward source to {forwardSource}," +
                    $" the reference to the {nameof(ContinuousMoveProviderBase)} is missing or the object has been destroyed.", this);
                return;
            }

            switch (forwardSource)
            {
                case MoveForwardSource.Head:
                    m_ContinuousMoveProvider.forwardSource = m_HeadForwardSource;
                    break;
                case MoveForwardSource.LeftHand:
                    m_ContinuousMoveProvider.forwardSource = m_LeftHandForwardSource;
                    break;
                case MoveForwardSource.RightHand:
                    m_ContinuousMoveProvider.forwardSource = m_RightHandForwardSource;
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(forwardSource), (int)forwardSource, typeof(MoveForwardSource));
            }
        }

        void SetBindingMasks(string controlSchemeName)
        {
            foreach (var actionReference in m_Actions)
            {
                if (actionReference == null)
                    continue;

                var action = actionReference.action;
                if (action == null)
                {
                    Debug.LogError($"Cannot set binding mask on {actionReference} since the action could not be found.", this);
                    continue;
                }

                // Get the (optional) base control scheme and the control scheme to apply on top of base
                var baseInputControlScheme = FindControlScheme(m_BaseControlScheme, actionReference);
                var inputControlScheme = FindControlScheme(controlSchemeName, actionReference);

                action.bindingMask = GetBindingMask(baseInputControlScheme, inputControlScheme);
            }

            if (m_ActionMaps.Count > 0 && m_ActionAssets.Count == 0)
            {
                Debug.LogError($"Cannot set binding mask on action maps since no input action asset references have been set.", this);
            }

            foreach (var actionAsset in m_ActionAssets)
            {
                if (actionAsset == null)
                    continue;

                // Get the (optional) base control scheme and the control scheme to apply on top of base
                var baseInputControlScheme = FindControlScheme(m_BaseControlScheme, actionAsset);
                var inputControlScheme = FindControlScheme(controlSchemeName, actionAsset);

                if (m_ActionMaps.Count == 0)
                {
                    actionAsset.bindingMask = GetBindingMask(baseInputControlScheme, inputControlScheme);
                    continue;
                }

                foreach (var mapName in m_ActionMaps)
                {
                    var actionMap = actionAsset.FindActionMap(mapName);
                    if (actionMap == null)
                    {
                        Debug.LogError($"Cannot set binding mask on \"{mapName}\" since the action map not be found in '{actionAsset}'.", this);
                        continue;
                    }

                    actionMap.bindingMask = GetBindingMask(baseInputControlScheme, inputControlScheme);
                }
            }
        }

        void ClearBindingMasks()
        {
            SetBindingMasks(string.Empty);
        }

        InputControlScheme? FindControlScheme(string controlSchemeName, InputActionReference action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (string.IsNullOrEmpty(controlSchemeName))
                return null;

            var asset = action.asset;
            if (asset == null)
            {
                Debug.LogError($"Cannot find control scheme \"{controlSchemeName}\" for '{action}' since it does not belong to an {nameof(InputActionAsset)}.", this);
                return null;
            }

            return FindControlScheme(controlSchemeName, asset);
        }

        InputControlScheme? FindControlScheme(string controlSchemeName, InputActionAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            if (string.IsNullOrEmpty(controlSchemeName))
                return null;

            var scheme = asset.FindControlScheme(controlSchemeName);
            if (scheme == null)
            {
                Debug.LogError($"Cannot find control scheme \"{controlSchemeName}\" in '{asset}'.", this);
                return null;
            }

            return scheme;
        }

        static InputBinding? GetBindingMask(InputControlScheme? baseInputControlScheme, InputControlScheme? inputControlScheme)
        {
            if (inputControlScheme.HasValue)
            {
                return baseInputControlScheme.HasValue
                    ? InputBinding.MaskByGroups(baseInputControlScheme.Value.bindingGroup, inputControlScheme.Value.bindingGroup)
                    : InputBinding.MaskByGroup(inputControlScheme.Value.bindingGroup);
            }

            return baseInputControlScheme.HasValue
                ? InputBinding.MaskByGroup(baseInputControlScheme.Value.bindingGroup)
                : (InputBinding?)null;
        }
    }
}
