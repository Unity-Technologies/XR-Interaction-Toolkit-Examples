using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace UnityEngine.XR.Content.Walkthrough
{
    /// <summary>
    /// Contains information needed to process one step of a walkthrough.
    /// </summary>
    public class WalkthroughStep : MonoBehaviour
    {
        /// Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<WalkthroughTrigger> s_TriggersToRemove = new List<WalkthroughTrigger>();

        [SerializeField]
        [Tooltip("Camera target to reposition user.")]
        GameObject m_CameraTarget;

        [SerializeField]
        [Tooltip("The Teleportation Provider used to reposition the user. Usually a component on the XR Origin.")]
        TeleportationProvider m_TeleportationProvider;

        [SerializeField]
        [Tooltip("Optional audio source for voiceover")]
        AudioSource m_AudioSource;

        [SerializeField]
        [Tooltip("Objects to enable when this step is active.")]
        List<GameObject> m_Visuals = new List<GameObject>();

#pragma warning disable 649
        [SerializeField]
        [Tooltip("Actions to call when the step starts.")]
        UnityEvent m_OnStepBegin;

        [SerializeField]
        [Tooltip("Actions to call when the step completes.")]
        UnityEvent m_OnStepComplete;

        [SerializeField]
        [Tooltip("The purpose of this step.")]
        string m_Description;
#pragma warning restore 649

        [SerializeField]
        [Tooltip("If true, this step cannot be skipped until completed at least once.")]
        bool m_BlockUntilComplete;

        [SerializeField]
        [Tooltip("If true, this step will automatically progress when complete - unless explicitly skipped to.")]
        bool m_AutoProgressOnComplete = true;

        bool m_Started;
        bool m_AutoProgressEnabled = true;
        bool m_StepInvoked;

        Action<bool> m_OnComplete;
        GameObject m_Waypoint;
        GameObject m_Link;

        List<WalkthroughTrigger> m_Triggers = new List<WalkthroughTrigger>();
        List<WalkthroughTrigger> m_RemainingTriggers = new List<WalkthroughTrigger>();

        /// <summary>
        /// The purpose of this step. Appends a (Complete) if complete and normally has triggers.
        /// </summary>
        public string description => $"{m_Description}{(completed && m_Triggers.Count > 0 ? " (Complete)" : "") }";


        public GameObject waypoint
        {
            get => m_Waypoint;
            set => m_Waypoint = value;
        }

        public GameObject link
        {
            get => m_Link;
            set => m_Link = value;
        }

        /// <summary>
        /// Ensures the step visuals are hidden until active and that all triggers are accounted for
        /// </summary>
        public void Initialize()
        {
            if (!m_Started)
                SetVisualsState(false);

            GetComponents(m_Triggers);
        }

        /// <summary>
        /// Returns true if this step does not currently have any triggers remaining to fire
        /// </summary>
        public bool canProgress => (!m_BlockUntilComplete || (m_RemainingTriggers.Count == 0));

        /// <summary>
        /// Returns true if this step does not block, or has been completed at least once.
        /// </summary>
        public bool canSkip => (!m_BlockUntilComplete || completed);

        /// <summary>
        /// True if this step's triggers have been activated at least once
        /// </summary>
        public bool completed { get; private set; }

        /// <summary>
        /// Makes this step and its triggers the active focus of a walkthrough
        /// </summary>
        /// <param name="onComplete">Callback to fire when this step's triggers are complete</param>
        /// <param name="allowAutoProgress">If this step is allow to auto-progress during this activation</param>
        public void StartStep(Action<bool> onComplete, bool allowAutoProgress = true)
        {
            if (m_Started)
                return;

            // Autoprogression is enabled only if the step AND walkthrough allow it
            m_AutoProgressEnabled = allowAutoProgress && m_AutoProgressOnComplete;

            SetVisualsState(true);
            SetAudioSource(true);
            if (m_Waypoint != null)
            {
                m_Waypoint.SetActive(false);
            }

            if (m_CameraTarget != null && m_TeleportationProvider != null)
            {
                SetCameraPosition();
            }

            m_OnComplete = onComplete;

            m_Started = true;

            if (m_Triggers.Count == 0 && m_AutoProgressEnabled)
            {
                CompleteStep();
                return;
            }

            foreach (var currentTrigger in m_Triggers)
            {
                if (currentTrigger.ResetTrigger())
                    m_RemainingTriggers.Add(currentTrigger);
            }

            if (m_RemainingTriggers.Count == 0)
            {
                CompleteStep();
                return;
            }

            if (m_RemainingTriggers.Count > 0)
                m_AutoProgressEnabled = m_AutoProgressOnComplete;
        }

        /// <summary>
        /// Ends this step being the focus of the current walkthrough
        /// </summary>
        public void CancelStep()
        {
            SetVisualsState(false);
            SetAudioSource(false);
            if (m_Waypoint != null)
            {
                m_Waypoint.SetActive(true);
            }

            if (!m_Started)
                return;

            m_OnComplete = null;
            m_Started = false;

            m_RemainingTriggers.Clear();
        }

        void CompleteStep()
        {
            if (!m_Started)
                return;

            completed = true;

            m_OnComplete?.Invoke(m_AutoProgressEnabled);
            m_OnComplete = null;
            m_Started = false;

            // We disable visuals if the the next step is being activated
            if (m_AutoProgressEnabled)
            {
                SetVisualsState(false);
                SetAudioSource(false);
                if (m_Waypoint != null)
                {
                    m_Waypoint.SetActive(true);
                }
            }

            m_RemainingTriggers.Clear();
        }

        void Update()
        {
            // If this step is running, check remaining triggers.  Any triggers that are now met get removed.
            // If there are no triggers left, then the step is complete.
            if (!m_Started)
                return;

            if (m_RemainingTriggers.Count == 0)
                return;

            s_TriggersToRemove.Clear();
            foreach (var currentTrigger in m_RemainingTriggers)
            {
                if (currentTrigger.Check())
                    s_TriggersToRemove.Add(currentTrigger);
            }

            foreach (var toRemove in s_TriggersToRemove)
            {
                m_RemainingTriggers.Remove(toRemove);
            }
            s_TriggersToRemove.Clear();

            if (m_RemainingTriggers.Count == 0)
            {
                CompleteStep();
                return;
            }
        }

        void SetVisualsState(bool enabled)
        {
            if (m_Visuals != null)
            {
                foreach (var currentVisual in m_Visuals)
                {
                    if (currentVisual != null)
                        currentVisual.SetActive(enabled);
                }
            }

            if (m_StepInvoked == enabled)
                return;

            m_StepInvoked = enabled;

            if (enabled && m_OnStepBegin != null)
                m_OnStepBegin.Invoke();

            if (!enabled && m_OnStepComplete != null)
                m_OnStepComplete.Invoke();
        }

        void SetAudioSource(bool enabled)
        {
            if (m_AudioSource != null)
            {
                if (enabled)
                {
                    m_AudioSource.Play();
                }
                else
                {
                    m_AudioSource.Stop();
                }
            }
        }

        void SetCameraPosition()
        {
            TeleportRequest request = new TeleportRequest()
            {
                requestTime = Time.time,
                matchOrientation = MatchOrientation.TargetUpAndForward,

                destinationPosition = m_CameraTarget.transform.position,
                destinationRotation = m_CameraTarget.transform.rotation
            };

            m_TeleportationProvider.QueueTeleportRequest(request);
        }
    }
}
