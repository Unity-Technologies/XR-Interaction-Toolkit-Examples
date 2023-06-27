using System;
using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;

namespace VRBuilder.XRInteraction
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
    /// When <see cref="moveScheme"/>=<see cref="MoveSchemeType.Noncontinuous"/>,
    /// bindings (1) and (2) will be enabled, but binding (3) will be disabled.
    ///
    /// When <see cref="moveScheme"/>=<see cref="MoveSchemeType.Continuous"/>,
    /// bindings (1) and (3) will be enabled, but binding (2) will be disabled.
    /// </example>
    /// </remarks>
    public class LocomotionSchemeManager : MonoBehaviour
    {
        /// <summary>
        /// Sets which movement control scheme to use.
        /// </summary>
        /// <seealso cref="moveScheme"/>
        public enum MoveSchemeType
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
        public enum TurnStyleType
        {
            /// <summary>
            /// Use snap turning to rotate the direction you are facing by snapping by a specified angle.
            /// </summary>
            Snap,

            /// <summary>
            /// Use continuous turning to smoothly rotate the direction you are facing by a specified speed.
            /// </summary>
            Continuous
        }

        /// <summary>
        /// Sets which orientation the forward direction of continuous movement is relative to.
        /// </summary>
        /// <seealso cref="moveForwardSource"/>
        /// <seealso cref="ContinuousMoveProviderBase.forwardSource"/>
        public enum MoveForwardSourceType
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
        private MoveSchemeType moveScheme;
        
        /// <summary>
        /// Controls which movement control scheme to use.
        /// </summary>
        /// <seealso cref="MoveSchemeType"/>
        public MoveSchemeType MoveScheme
        {
            get => moveScheme;
            set
            {
                SetMoveScheme(value);
                moveScheme = value;
            }
        }

        [SerializeField]
        [Tooltip("Controls which turn style of locomotion to use.")]
        private TurnStyleType turnStyle;
        
        /// <summary>
        /// Controls which turn style of locomotion to use.
        /// </summary>
        /// <seealso cref="TurnStyleType"/>
        public TurnStyleType TurnStyle
        {
            get => turnStyle;
            set
            {
                SetTurnStyle(value);
                turnStyle = value;
            }
        }

        [SerializeField]
        [Tooltip("Controls which orientation the forward direction of continuous movement is relative to.")]
        private MoveForwardSourceType moveForwardSource;
        
        /// <summary>
        /// Controls which orientation the forward direction of continuous movement is relative to.
        /// </summary>
        /// <seealso cref="MoveForwardSourceType"/>
        public MoveForwardSourceType MoveForwardSource
        {
            get => moveForwardSource;
            set
            {
                SetMoveForwardSource(value);
                moveForwardSource = value;
            }
        }

        [SerializeField]
        [Tooltip("Input action assets associated with locomotion to affect when the active movement control scheme is set." +
            " Can use this list by itself or together with the Action Maps list to set control scheme masks by Asset or Map.")]
        private List<InputActionAsset> actionAssets;
        
        /// <summary>
        /// Input action assets associated with locomotion to affect when the active movement control scheme is set.
        /// Can use this list by itself or together with the Action Maps list to set control scheme masks by Asset or Map.
        /// </summary>
        /// <seealso cref="actionMaps"/>
        public List<InputActionAsset> ActionAssets
        {
            get => actionAssets;
            set => actionAssets = value;
        }

        [SerializeField]
        [Tooltip("Input action maps associated with locomotion to affect when the active movement control scheme is set." +
            " Can use this list together with the Action Assets list to set control scheme masks by Map instead of the whole Asset.")]
        private List<string> actionMaps;
        
        /// <summary>
        /// Input action maps associated with locomotion to affect when the active movement control scheme is set.
        /// Can use this list together with the Action Assets list to set control scheme masks by Map instead of the whole Asset.
        /// </summary>
        /// <seealso cref="actionAssets"/>
        public List<string> ActionMaps
        {
            get => actionMaps;
            set => actionMaps = value;
        }

        [SerializeField]
        [Tooltip("Input actions associated with locomotion to affect when the active movement control scheme is set." +
            " Can use this list to select exactly the actions to affect instead of setting control scheme masks by Asset or Map.")]
        private List<InputActionReference> actions;
        
        /// <summary>
        /// Input actions associated with locomotion that are affected by the active movement control scheme.
        /// Can use this list to select exactly the actions to affect instead of setting control scheme masks by Asset or Map.
        /// </summary>
        /// <seealso cref="actionAssets"/>
        /// <seealso cref="actionMaps"/>
        public List<InputActionReference> Actions
        {
            get => actions;
            set => actions = value;
        }

        [SerializeField]
        [Tooltip("Name of an input control scheme that defines the grouping of bindings that should remain enabled when applying any movement control scheme." +
            " Control schemes are created and named in the Input Actions window. The other movement control schemes are applied additively to this scheme." +
            " Can be an empty string, which means only bindings that match the specified movement control scheme will be enabled.")]
        private string baseControlScheme;
        
        /// <summary>
        /// Name of an input control scheme that defines the grouping of bindings that should remain enabled when applying any movement control scheme.
        /// Control schemes are created and named in the Input Actions window. The other movement control schemes are applied additively to this scheme.
        /// Can be an empty string, which means only bindings that match the specified movement control scheme will be enabled.
        /// </summary>
        public string BaseControlScheme
        {
            get => baseControlScheme;
            set => baseControlScheme = value;
        }

        [SerializeField]
        [Tooltip("Name of an input control scheme that defines the grouping of bindings that should remain enabled when applying the noncontinuous movement control scheme." +
            " Control schemes are created and named in the Input Actions window. Can be an empty string, which means only bindings that match the" +
            " base control scheme will be enabled.")]
        private string noncontinuousControlScheme;
        
        /// <summary>
        /// Name of an input control scheme that defines the grouping of bindings that should remain enabled when applying the noncontinuous movement control scheme.
        /// Control schemes are created and named in the Input Actions window. Can be an empty string, which means only bindings that match the
        /// base control scheme will be enabled.
        /// </summary>
        public string NoncontinuousControlScheme
        {
            get => noncontinuousControlScheme;
            set => noncontinuousControlScheme = value;
        }

        [SerializeField]
        [Tooltip("Name of an input control scheme that defines the grouping of bindings that should remain enabled when applying the continuous movement control scheme." +
            " Control schemes are created and named in the Input Actions window. Can be an empty string, which means only bindings that match the" +
            " base control scheme will be enabled.")]
        private string continuousControlScheme;
        
        /// <summary>
        /// Name of an input control scheme that defines the grouping of bindings that should remain enabled when applying the continuous movement control scheme.
        /// Control schemes are created and named in the Input Actions window. Can be an empty string, which means only bindings that match the
        /// base control scheme will be enabled.
        /// </summary>
        public string ContinuousControlScheme
        {
            get => continuousControlScheme;
            set => continuousControlScheme = value;
        }

        [SerializeField]
        [Tooltip("Stores the locomotion provider for continuous movement.")]
        private ContinuousMoveProviderBase continuousMoveProvider;
        
        /// <summary>
        /// Stores the locomotion provider for continuous movement.
        /// </summary>
        /// <seealso cref="ContinuousMoveProviderBase"/>
        public ContinuousMoveProviderBase ContinuousMoveProvider
        {
            get => continuousMoveProvider;
            set => continuousMoveProvider = value;
        }

        [SerializeField]
        [Tooltip("Stores the locomotion provider for continuous turning.")]
        private ContinuousTurnProviderBase continuousTurnProvider;
        
        /// <summary>
        /// Stores the locomotion provider for continuous turning.
        /// </summary>
        /// <seealso cref="ContinuousTurnProviderBase"/>
        public ContinuousTurnProviderBase ContinuousTurnProvider
        {
            get => continuousTurnProvider;
            set => continuousTurnProvider = value;
        }

        [SerializeField]
        [Tooltip("Stores the locomotion provider for snap turning.")]
        private SnapTurnProviderBase snapTurnProvider;
        
        /// <summary>
        /// Stores the locomotion provider for snap turning.
        /// </summary>
        /// <seealso cref="SnapTurnProviderBase"/>
        public SnapTurnProviderBase SnapTurnProvider
        {
            get => snapTurnProvider;
            set => snapTurnProvider = value;
        }

        [SerializeField]
        [Tooltip("Stores the \"Head\" Transform used with continuous movement when inputs should be relative to head orientation (usually the main camera).")]
        private Transform headForwardSource;
        
        /// <summary>
        /// Stores the "Head" <see cref="Transform"/> used with continuous movement when inputs should be relative to head orientation (usually the main camera).
        /// </summary>
        public Transform HeadForwardSource
        {
            get => headForwardSource;
            set => headForwardSource = value;
        }

        [SerializeField]
        [Tooltip("Stores the \"Left Hand\" Transform used with continuous movement when inputs should be relative to the left hand's orientation.")]
        private Transform leftHandForwardSource;
        
        /// <summary>
        /// Stores the "Left Hand" <see cref="Transform"/> used with continuous movement when inputs should be relative to the left hand's orientation.
        /// </summary>
        public Transform LeftHandForwardSource
        {
            get => leftHandForwardSource;
            set => leftHandForwardSource = value;
        }

        [SerializeField]
        [Tooltip("Stores the \"Right Hand\" Transform used with continuous movement when inputs should be relative to the right hand's orientation.")]
        private Transform rightHandForwardSource;
        
        /// <summary>
        /// Stores the "Right Hand" <see cref="Transform"/> used with continuous movement when inputs should be relative to the right hand's orientation.
        /// </summary>
        public Transform RightHandForwardSource
        {
            get => rightHandForwardSource;
            set => rightHandForwardSource = value;
        }

        private void OnEnable()
        {
            SetMoveScheme(moveScheme);
            SetTurnStyle(turnStyle);
            SetMoveForwardSource(moveForwardSource);
        }

        private void OnDisable()
        {
            if(GameObject.FindObjectsByType<LocomotionSchemeManager>(FindObjectsSortMode.None).Count() > 1)
            {
                return;
            }

            ClearBindingMasks();
        }

        private void SetMoveScheme(MoveSchemeType scheme)
        {
            switch (scheme)
            {
                case MoveSchemeType.Noncontinuous:
                    SetBindingMasks(noncontinuousControlScheme);
                    
                    if (continuousMoveProvider != null)
                    {
                        continuousMoveProvider.enabled = false;
                    }
                    break;
                case MoveSchemeType.Continuous:
                    SetBindingMasks(continuousControlScheme);
                    
                    if (continuousMoveProvider != null)
                    {
                        continuousMoveProvider.enabled = true;
                    }
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(scheme), (int)scheme, typeof(MoveSchemeType));
            }
        }

        private void SetTurnStyle(TurnStyleType style)
        {
            if (style != TurnStyleType.Snap && style != TurnStyleType.Continuous)
            {
                throw new InvalidEnumArgumentException(nameof(style), (int)style, typeof(TurnStyleType));
            }
            
            if (continuousTurnProvider != null)
            {
                continuousTurnProvider.enabled = style != TurnStyleType.Snap;
            }
            
            if (snapTurnProvider != null)
            {
                if (style == TurnStyleType.Snap)
                {
                    // TODO: If the Continuous Turn and Snap Turn providers both use the same
                    // action, then disabling the first provider will cause the action to be
                    // disabled, so the action needs to be enabled, which is done by forcing
                    // the OnEnable() of the second provider to be called.
                    // ReSharper disable Unity.InefficientPropertyAccess
                    snapTurnProvider.enabled = false;
                    snapTurnProvider.enabled = true;
                    // ReSharper restore Unity.InefficientPropertyAccess
                }
                
                snapTurnProvider.enableTurnLeftRight = style == TurnStyleType.Snap;
            }
        }

        private void SetMoveForwardSource(MoveForwardSourceType forwardSource)
        {
            if (continuousMoveProvider == null)
            {
                Debug.LogError($"Cannot set forward source to {forwardSource}, the reference to the {nameof(ContinuousMoveProviderBase)} is missing or the object has been destroyed.", this);
                return;
            }

            switch (forwardSource)
            {
                case MoveForwardSourceType.Head:
                    continuousMoveProvider.forwardSource = headForwardSource;
                    break;
                case MoveForwardSourceType.LeftHand:
                    continuousMoveProvider.forwardSource = leftHandForwardSource;
                    break;
                case MoveForwardSourceType.RightHand:
                    continuousMoveProvider.forwardSource = rightHandForwardSource;
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(forwardSource), (int)forwardSource, typeof(MoveForwardSourceType));
            }
        }

        private void SetBindingMasks(string controlSchemeName)
        {
            foreach (InputActionReference actionReference in actions)
            {
                if (actionReference == null)
                {
                    continue;
                }

                InputAction action = actionReference.action;
                
                if (action == null)
                {
                    Debug.LogError($"Cannot set binding mask on {actionReference} since the action could not be found.", this);
                    continue;
                }

                // Get the (optional) base control scheme and the control scheme to apply on top of base
                InputControlScheme? baseInputControlScheme = FindControlScheme(baseControlScheme, actionReference);
                InputControlScheme? inputControlScheme = FindControlScheme(controlSchemeName, actionReference);

                action.bindingMask = GetBindingMask(baseInputControlScheme, inputControlScheme);
            }

            if (actionMaps.Count > 0 && actionAssets.Count == 0)
            {
                Debug.LogError($"Cannot set binding mask on action maps since no input action asset references have been set.", this);
            }

            foreach (InputActionAsset actionAsset in actionAssets)
            {
                if (actionAsset == null)
                {
                    continue;
                }

                // Get the (optional) base control scheme and the control scheme to apply on top of base
                InputControlScheme? baseInputControlScheme = FindControlScheme(baseControlScheme, actionAsset);
                InputControlScheme? inputControlScheme = FindControlScheme(controlSchemeName, actionAsset);

                if (actionMaps.Count == 0)
                {
                    actionAsset.bindingMask = GetBindingMask(baseInputControlScheme, inputControlScheme);
                    continue;
                }

                foreach (string mapName in actionMaps)
                {
                    InputActionMap actionMap = actionAsset.FindActionMap(mapName);
                    
                    if (actionMap == null)
                    {
                        Debug.LogError($"Cannot set binding mask on \"{mapName}\" since the action map not be found in '{actionAsset}'.", this);
                        continue;
                    }

                    actionMap.bindingMask = GetBindingMask(baseInputControlScheme, inputControlScheme);
                }
            }
        }

        private void ClearBindingMasks()
        {
            SetBindingMasks(string.Empty);
        }

        private InputControlScheme? FindControlScheme(string controlSchemeName, InputActionReference action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (string.IsNullOrEmpty(controlSchemeName))
            {
                return null;
            }

            InputActionAsset asset = action.asset;
            
            if (asset == null)
            {
                Debug.LogError($"Cannot find control scheme \"{controlSchemeName}\" for '{action}' since it does not belong to an {nameof(InputActionAsset)}.", this);
                return null;
            }

            return FindControlScheme(controlSchemeName, asset);
        }

        private InputControlScheme? FindControlScheme(string controlSchemeName, InputActionAsset asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            if (string.IsNullOrEmpty(controlSchemeName))
            {
                return null;
            }

            InputControlScheme? scheme = asset.FindControlScheme(controlSchemeName);
            
            if (scheme == null)
            {
                Debug.LogError($"Cannot find control scheme \"{controlSchemeName}\" in '{asset}'.", this);
                return null;
            }

            return scheme;
        }

        private static InputBinding? GetBindingMask(InputControlScheme? baseInputControlScheme, InputControlScheme? inputControlScheme)
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
