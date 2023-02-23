using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Allows for actions to 'lock' the input controls they use so that others actions using the same controls will not receive input at the same time.
    /// InputMediator works by injecting input processors on every binding in active action maps.
    /// Usage is via <see cref="ConsumeControl"/> and <see cref="ReleaseControl"/>.
    /// </summary>
    public static class InputMediator
    {
        /// <summary>
        /// Substring of the names that all of the input processors that are injected have.
        /// </summary>
        /// <seealso cref="Initialize"/>
        const string k_ConsumeKey = "Consume";
        static bool s_Updating;

        // Data associated with each control, storing if an action has locked it,
        // and other actions that are allowed to make use of this control at the same time
        class ConsumptionState
        {
            public int m_LockedAction = -1;
            public int m_AllowedAction1 = -1;
            public int m_AllowedAction2 = -1;

            public bool m_Automatic;
        }

        /// <summary>
        /// Generic Consumption processor - handles all the aspects of looking up actions that have locked a control
        /// Implementations merely need to implement the methods to determine if a control has returned to rest (and thus should reset)
        /// And the 'identity' value of a control, which is the value it should have when the control is locked
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        abstract class ConsumeProcessor<TValue> : InputProcessor<TValue> where TValue : struct
        {
            public int m_ActionIndex = -1;

            public override TValue Process(TValue value, InputControl control)
            {
                // Check the dictionary for this control
                // If it does not exist, proceed unhindered
                if (control == null || !s_ConsumedControls.TryGetValue(control, out var currentState))
                    return value;

                // If there is no locked action, also proceed unhindered
                if (currentState.m_LockedAction == -1)
                    return value;

                // Check for an action match
                var actionMatched = (currentState.m_LockedAction == m_ActionIndex) || (currentState.m_AllowedAction1 == m_ActionIndex) || (currentState.m_AllowedAction2 == m_ActionIndex);

                // Check if we should automatically release
                if (actionMatched)
                {
                    if (currentState.m_Automatic && ValueNearZero(value))
                        currentState.m_LockedAction = -1;

                    return value;
                }

                return IdentityValue();
            }

            public abstract bool ValueNearZero(TValue value);

            public abstract TValue IdentityValue();
        }

        class ConsumeFloat : ConsumeProcessor<float>
        {
            public override bool ValueNearZero(float value)
            {
                return value < float.Epsilon;
            }

            public override float IdentityValue()
            {
                return 0.0f;
            }
        }

        class ConsumeVector2 : ConsumeProcessor<Vector2>
        {
            public override bool ValueNearZero(Vector2 value)
            {
                return value.sqrMagnitude < float.Epsilon;
            }

            public override Vector2 IdentityValue()
            {
                return Vector2.zero;
            }
        }

        class ConsumeVector3 : ConsumeProcessor<Vector3>
        {
            public override bool ValueNearZero(Vector3 value)
            {
                return value.sqrMagnitude < float.Epsilon;
            }

            public override Vector3 IdentityValue()
            {
                return Vector3.zero;
            }
        }

        class ConsumeQuaternion : ConsumeProcessor<Quaternion>
        {
            public override bool ValueNearZero(Quaternion value)
            {
                return Quaternion.Angle(value, Quaternion.identity) < float.Epsilon;
            }

            public override Quaternion IdentityValue()
            {
                return Quaternion.identity;
            }
        }

        static Dictionary<InputControl, ConsumptionState> s_ConsumedControls = new Dictionary<InputControl, ConsumptionState>();
        static Dictionary<InputAction, int> s_ActionIndices = new Dictionary<InputAction, int>();
        static HashSet<InputAction> s_InitializedActions = new HashSet<InputAction>();

        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            InputSystem.InputSystem.RegisterProcessor<ConsumeFloat>(nameof(ConsumeFloat));
            InputSystem.InputSystem.RegisterProcessor<ConsumeVector2>(nameof(ConsumeVector2));
            InputSystem.InputSystem.RegisterProcessor<ConsumeVector3>(nameof(ConsumeVector3));
            InputSystem.InputSystem.RegisterProcessor<ConsumeQuaternion>(nameof(ConsumeQuaternion));
            Application.quitting += OnApplicationQuitting;
            InputSystem.InputSystem.onActionChange += OnActionChange;
            InitializeConsumeProcessors();
        }

        static void OnApplicationQuitting()
        {
            InputSystem.InputSystem.onActionChange -= OnActionChange;
        }

        /// <summary>
        /// Attempts to 'lock' the controls belonging to an action - which means other actions using the same control will only get zero/identity values during this time
        /// </summary>
        /// <param name="source">The action that should lock their controls</param>
        /// <param name="automaticRelease">If the control lock should release automatically when the controls go to a resting state</param>
        /// <param name="force">If the action should forcefully take a lock from another consuming action</param>
        /// <param name="friendAction1">An additional action that can access these controls at this time</param>
        /// <param name="friendAction2">An additional action that can access these controls at this time</param>
        /// <returns>False if _any_ of the associated controls were unable to be locked </returns>
        public static bool ConsumeControl(InputAction source, bool automaticRelease, bool force = false, InputAction friendAction1 = null, InputAction friendAction2 = null)
        {
            if (source == null)
                return false;

            var actionIndex1 = GetActionIndex(source);
            var actionIndex2 = GetActionIndex(friendAction1);
            var actionIndex3 = GetActionIndex(friendAction2);
            var lockCount = 0;

            var sourceControls = source.controls;
            foreach (var currentControl in sourceControls)
            {
                // Check to see if it is in the list already
                // If not, make an entry for it
                if (!s_ConsumedControls.TryGetValue(currentControl, out var controlState))
                {
                    var parent = currentControl.parent;
                    if (currentControl is AxisControl && parent is Vector2Control)
                    {
                        if (!s_ConsumedControls.TryGetValue(parent, out controlState))
                        {
                            controlState = new ConsumptionState { m_Automatic = automaticRelease };
                            s_ConsumedControls.Add(parent, controlState);
                        }
                    }
                    else
                    {
                        controlState = new ConsumptionState { m_Automatic = automaticRelease };
                    }

                    s_ConsumedControls.Add(currentControl, controlState);
                }

                if (force || controlState.m_LockedAction == -1)
                {
                    controlState.m_LockedAction = actionIndex1;
                    controlState.m_AllowedAction1 = actionIndex2;
                    controlState.m_AllowedAction2 = actionIndex3;
                    lockCount++;
                }
            }
            return (lockCount == sourceControls.Count);
        }

        /// <summary>
        /// Releases an action's lock over its associated controls. Other actions using the same controls will begin receiving input again
        /// </summary>
        /// <param name="source">The action that is attempting to release its lock</param>
        /// <param name="force">If this input lock should be released regardless of requesting action</param>
        /// <returns>False if _any_ of the associated controls were unable to be released </returns>
        public static bool ReleaseControl(InputAction source, bool force = false)
        {
            if (source == null)
                return false;

            var actionIndex = GetActionIndex(source);
            var lockCount = 0;

            var sourceControls = source.controls;
            foreach (var currentControl in sourceControls)
            {
                // Check to see if it is in the list already
                // If not, nothing to release
                if (!s_ConsumedControls.TryGetValue(currentControl, out var controlState))
                {
                    lockCount++;
                    continue;
                }

                if (force || controlState.m_LockedAction == actionIndex)
                {
                    controlState.m_LockedAction = -1;
                    lockCount++;
                }
            }

            return (lockCount == sourceControls.Count);
        }

        static void InitializeConsumeProcessors()
        {
            s_Updating = true;
            var actionList = InputSystem.InputSystem.ListEnabledActions();
            foreach (var action in actionList)
            {
                EnsureConsumeProcessorAdded(action);

                // Since this list only contains currently enabled actions,
                // any actions that are enabled later will need to
                // have the consume processor added. Since those actions may not
                // trigger a BoundControlsChanged change, the OnActionChange event handler
                // will check against this list and append to it as actions are enabled.
                // This set is checked against for performance reasons
                // to avoid the more costly EnsureConsumeProcessorAdded(InputAction) method.
                s_InitializedActions.Add(action);
            }
            s_Updating = false;
        }

        static void OnActionChange(object actionSource, InputActionChange change)
        {
            if (s_Updating)
                return;

            s_Updating = true;

            if (change == InputActionChange.ActionEnabled)
            {
                var action = (InputAction)actionSource;
                if (s_InitializedActions.Add(action))
                    EnsureConsumeProcessorAdded(action);
            }
            else if (change == InputActionChange.ActionMapEnabled)
            {
                var actionMap = (InputActionMap)actionSource;
                foreach (var action in actionMap.actions)
                {
                    if (s_InitializedActions.Add(action))
                        EnsureConsumeProcessorAdded(action);
                }
            }
            else if (change == InputActionChange.BoundControlsChanged)
            {
                // We skip pure actions here as they can get into an invalid state if bindings were changed
                if (actionSource is InputActionMap actionMap)
                {
                    EnsureConsumeProcessorAdded(actionMap);
                }
                else if (actionSource is InputActionAsset actionAsset)
                {
                    EnsureConsumeProcessorAdded(actionAsset);
                }
            }

            s_Updating = false;
        }

        static string ControlTypeToConsumeType(string controlType)
        {
            switch (controlType)
            {
                case "Single":
                case "Button":
                case "float":
                    return nameof(ConsumeFloat);
                case "Vector2":
                    return nameof(ConsumeVector2);
                case "Vector3":
                    return nameof(ConsumeVector3);
                case "Quaternion":
                    return nameof(ConsumeQuaternion);
            }
            return "";
        }

        static string ProcessBindingControl(string bindingPath)
        {
            var control = InputSystem.InputSystem.FindControl(bindingPath);
            var consumeType = "";

            if (control != null)
                consumeType = ControlTypeToConsumeType(control.valueType.Name);
            else
            {
                // Try to fall back based on path keywords
                var bindingLower = bindingPath.ToLower();
                if (bindingLower.EndsWith("position"))
                    consumeType = ControlTypeToConsumeType("Vector3");

                if (bindingLower.EndsWith("rotation"))
                    consumeType = ControlTypeToConsumeType("Quaternion");

                if (bindingLower.EndsWith("x"))
                    consumeType = ControlTypeToConsumeType("float");

                if (bindingLower.EndsWith("y"))
                    consumeType = ControlTypeToConsumeType("float");

                if (bindingLower.EndsWith("axis"))
                    consumeType = ControlTypeToConsumeType("Vector2");
            }

            if (string.IsNullOrEmpty(consumeType))
                return "";

            return consumeType;
        }

        static void EnsureConsumeProcessorAdded(InputAction action)
        {
            var bindingCount = action.bindings.Count;
            for (var i = 0; i < bindingCount; i++)
            {
                var currentBinding = action.bindings[i];

                // Ignore composites, but not parts of composites
                if (currentBinding.isComposite)
                    continue;

                // Ignore bindings that aren't ready yet
                if (currentBinding.effectiveProcessors == null)
                    continue;

                var actionIndex = GetActionIndex(action);
                if (!currentBinding.effectiveProcessors.Contains(k_ConsumeKey))
                {
                    // Ignore unused bindings
                    if (string.IsNullOrEmpty(currentBinding.path))
                        continue;

                    // Get the binding's control type and cache it in the control lookup
                    var bindingType = ProcessBindingControl(currentBinding.path);

                    // If the composite can't figure out its type, then skip it
                    if (string.IsNullOrEmpty(bindingType))
                    {
                        //Debug.LogWarning($"Could not add consume processor for binding { currentBinding.path }, in {action.name}");
                        continue;
                    }

                    if (currentBinding.processors.Length > 0)
                        action.ApplyBindingOverride(i, new InputBinding { overrideProcessors = $"{bindingType}(m_ActionIndex={actionIndex}), {currentBinding.processors}" });
                    else
                        action.ApplyBindingOverride(i, new InputBinding { overrideProcessors = $"{bindingType}(m_ActionIndex={actionIndex})" });
                }
            }
        }

        static void EnsureConsumeProcessorAdded(InputActionMap actionMap)
        {
            foreach (var action in actionMap.actions)
            {
                EnsureConsumeProcessorAdded(action);
            }
        }

        static void EnsureConsumeProcessorAdded(InputActionAsset actionAsset)
        {
            foreach (var map in actionAsset.actionMaps)
            {
                EnsureConsumeProcessorAdded(map);
            }
        }

        static int GetActionIndex(InputAction source)
        {
            if (source == null)
                return -1;

            if (!s_ActionIndices.TryGetValue(source, out var actionIndex))
            {
                actionIndex = s_ActionIndices.Count;
                s_ActionIndices.Add(source, actionIndex);
            }

            return actionIndex;
        }
    }
}
