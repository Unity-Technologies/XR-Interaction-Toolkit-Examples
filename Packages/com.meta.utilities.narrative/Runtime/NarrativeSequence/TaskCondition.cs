// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using UnityEngine;

namespace Meta.Utilities.Narrative
{
    // NOTE: Since task conditions are serialized by reference in TaskHandler,
    // renaming any of these types will break existing conditions of that type serialized in scenes

    /// <summary>
    /// The base class for defining new task conditions used to advance task sequences in the narrative
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "NorthStar", sourceAssembly: "Assembly-CSharp")]
    public class TaskCondition
    {
        /// <summary>
        /// Can be added to task condition classes to indicate in the inspector
        /// that it may be expensive to use at runtime.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        public class SlowAttribute : Attribute { }

        [HideInInspector][SerializeField] private int m_index;

        /// <summary>
        /// Describes the condition's current configuration (including settable parameters).
        /// </summary>
        public virtual string Description => "";

        /// <summary>
        /// An editor-UI-friendly rich label including a description of the condition setup.
        /// </summary>
        public string RichLabel
        {
            get
            {
                var hex = ColorUtility.ToHtmlStringRGBA(Color.grey * GUI.color);
                var typeName = NiceTypeName(GetType());
                return $"<b>{typeName}:</b>  <size=11><color=#{hex}>{Description}</color></size>";
            }
        }

        /// <summary>
        /// Called once each frame while the task is running to check for completion.
        /// Avoid expensive logic here if possible.
        /// </summary>
        /// <param name="handler">The host task handler.</param>
        /// <returns>Whether the condition has been met.</returns>
        public virtual bool IsComplete(TaskHandler handler) => true;


        /// <summary>
        /// Called when the host handler component is being destroyed.
        /// Should be used for cleanup of resources held by the condition,
        /// e.g. unsubscribing from events.
        /// </summary>
        /// <param name="handler">The host task handler.</param>
        public virtual void OnHandlerDestroy(TaskHandler handler) { }

        /// <summary>
        /// Called when the task is started.
        /// </summary>
        /// <param name="handler">The host task handler.</param>
        public virtual void OnTaskStarted(TaskHandler handler) { }

        /// <summary>
        /// Called when the handler is created (Different to the task starting)
        /// </summary>
        /// <param name="handler"></param>
        public virtual void OnHandlerStart(TaskHandler handler) { }

        /// <summary>
        /// Called during the host task handler's OnValidate(). 
        /// </summary>
        /// <param name="handler">The host task handler.</param>
        public virtual void OnValidate(TaskHandler handler) { }

        /// <summary>
        /// Called when Skip() is called on the task handler.
        /// Should be used to ensure game state is consistent
        /// with this condition having been completed.
        /// </summary>
        /// <param name="handler">The host task handler.</param>
        public virtual void ForceComplete(TaskHandler handler) { }

        /// <param name="conditionType">The task condition type.</param>
        /// <returns>Formatted type name in Unity inspector style,
        /// minus a trailing "Condition".</returns>
        public static string NiceTypeName(Type conditionType)
        {
#if UNITY_EDITOR
            var niceTypeName = UnityEditor.ObjectNames.NicifyVariableName(conditionType.Name);
            if (niceTypeName.EndsWith(" Condition")) { niceTypeName = niceTypeName[..^10]; }
#else
            var niceTypeName = conditionType.Name;
            if (niceTypeName.EndsWith("Condition")) { niceTypeName = niceTypeName[..^9]; }
#endif

            return niceTypeName;
        }

        public override string ToString() => $"{NiceTypeName(GetType())} ({Description})";
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "NorthStar", sourceAssembly: "Assembly-CSharp")]
    public class ProximityCondition : TaskCondition
    {
        public Transform Target;
        public float MaxPlayerDistance = 3f;

        public override string Description
            => $"{MaxPlayerDistance:0.0}m from {(Target ? Target.name : "unset target")}";


        public override bool IsComplete(TaskHandler handler)
        {
            if (!handler.PlayerTransform || !Target) { return true; } // failsafe

            var squareDistance = Vector3.SqrMagnitude(handler.PlayerTransform.transform.position
                                                      - Target.position);

            return squareDistance < MaxPlayerDistance * MaxPlayerDistance;
        }
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "NorthStar", sourceAssembly: "Assembly-CSharp")]
    public class LookAtTargetCondition : TaskCondition
    {
        public Transform Target;
        public float ToleranceAngle = 15f;

        public override string Description
            => $"{(Target ? Target.name : "unset target")}, within {ToleranceAngle}°";

        public override bool IsComplete(TaskHandler handler)
        {
            if (!handler.PlayerGazeCamera || !Target) { return true; } // failsafe

            var vecToTarget = Target.position - handler.PlayerGazeCamera.transform.position;

            return Vector3.Angle(vecToTarget, handler.PlayerGazeCamera.transform.forward)
                   < ToleranceAngle;
        }
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "NorthStar", sourceAssembly: "Assembly-CSharp")]
    public class TimeDelayCondition : TaskCondition
    {
        public float DelayInSeconds;

        private float m_startTime = Mathf.Infinity;

        public override string Description => $"{DelayInSeconds} seconds";

        public override void OnTaskStarted(TaskHandler handler)
        {
            m_startTime = Time.time;
        }

        public override bool IsComplete(TaskHandler handler)
            => Time.time - m_startTime > DelayInSeconds;
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "NorthStar", sourceAssembly: "Assembly-CSharp")]
    public class WaitForEventCondition : TaskCondition
    {
        public EventLink EventLink;

        private bool m_completed;

        public override string Description
        {
            get
            {
                return !EventLink.TargetObject || string.IsNullOrWhiteSpace(EventLink.FieldName)
                    ? "No target"
                    : $"{EventLink.TargetObject.GetType().Name}.{EventLink.FieldName} "
                       + $"on {EventLink.TargetObject.name}";
            }
        }

        public override bool IsComplete(TaskHandler handler) => m_completed;

        public override void OnHandlerDestroy(TaskHandler handler) => EventLink.Unsubscribe();

        public override void OnTaskStarted(TaskHandler handler)
        {
            m_completed = false;
            EventLink.Subscribe(EventHandler);

            if (TaskManager.DebugLogs)
            {
                Debug.Log("WaitForEventCondition.OnTaskStarted(); "
                          + "condition is set incomplete; event link subscribed");
            }
        }

        private void EventHandler()
        {
            m_completed = true;
            EventLink.Unsubscribe();

            if (TaskManager.DebugLogs)
            {
                Debug.Log("WaitForEventCondition.EventHandler(); "
                          + "condition is now completed; event link unsubscribed");
            }
        }
    }

    [Serializable, Slow]
    public class ValueIsTrueCondition : TaskCondition
    {
        public MemberLink<bool> MemberLink;

        [LabelWidth(112)] public bool StaysSafisfied;

        private bool m_completed;

        public override string Description
        {
            get
            {
                return !MemberLink.TargetObject || string.IsNullOrWhiteSpace(MemberLink.MemberName)
                    ? "No target"
                    : $"{MemberLink.TargetObject.GetType().Name}.{MemberLink.MemberName} "
                       + $"on {MemberLink.TargetObject.name}";
            }
        }

        public override bool IsComplete(TaskHandler handler)
        {
            return (m_completed && StaysSafisfied) || (m_completed = MemberLink.GetValue());
        }

        public override void OnTaskStarted(TaskHandler handler) => m_completed = false;
    }

    public class LabelWidthAttribute : PropertyAttribute
    {
        public readonly float Width;

        public LabelWidthAttribute(float width) => Width = width;
    }
}