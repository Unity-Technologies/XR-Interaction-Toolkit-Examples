using System;

namespace UnityEngine.XR.Content.Walkthrough
{
    /// <summary>
    /// Defines a walkthrough - a series of steps gated by triggers
    /// </summary>
    public class Walkthrough : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        [Tooltip("The name of this walkthrough - for reference by UI")]
        string m_WalkthroughName;

        [SerializeField]
        [Tooltip("All of the steps this walkthrough requires, in order")]
        WalkthroughStep[] m_Steps;

        [SerializeField]
        GameObject m_Waypoint;

        [SerializeField]
        GameObject m_WaypointLink;

        [SerializeField]
        bool m_LoopOnComplete = false;
#pragma warning restore 649

        /// <summary>
        /// The name of the walkthrough experience
        /// </summary>
        public string walkthroughName => m_WalkthroughName;

        /// <summary>
        /// All of the steps this walkthrough requires, in order
        /// </summary>
        public WalkthroughStep[] steps => m_Steps;

        /// <summary>
        /// The currently active step of the walkthrough
        /// </summary>
        public int currentStep { get; private set; } = 0;

        /// <summary>
        /// Event that is raised whenever the state of the walkthrough has changed.
        /// </summary>
        public Action walkthroughChangedCallback;

        /// <summary>
        /// Shifts to another step of the walkthrough
        /// </summary>
        /// <param name="stepIndex">The step to make active</param>
        /// <param name="autoProgressIfComplete">If true, allows for skipping to the subsequent step if the current one is already complete.</param>
        public void SkipToStep(int stepIndex, bool autoProgressIfComplete)
        {
            // Ignore invalid indices and no-ops
            if (stepIndex < 0 || stepIndex >= m_Steps.Length || stepIndex == currentStep)
                return;

            // If any steps between our current step and the next are incomplete and block progression, we do not allow skipping to occur. This prevents
            // problems like skipping to a step where relocalization has not yet occurred.
            if (stepIndex > currentStep)
            {
                for (var testStepIndex = currentStep; testStepIndex < stepIndex; testStepIndex++)
                {
                    var testStep = m_Steps[testStepIndex];
                    if (!testStep.canSkip)
                    {
                        Debug.LogWarning($"Can't skip past incomplete step {testStep.name}");
                        walkthroughChangedCallback?.Invoke();
                        return;
                    }
                }
            }

            // If a valid step is already being displayed, set it back to inactive now
            if (currentStep >= 0 && currentStep < m_Steps.Length)
                m_Steps[currentStep].CancelStep();

            currentStep = stepIndex;
            m_Steps[currentStep].StartStep(OnStepComplete, autoProgressIfComplete);

            walkthroughChangedCallback?.Invoke();
        }

        public void SkipToStep(int stepIndex)
        {
            SkipToStep(stepIndex, false);
        }

        void Awake()
        {
            int stepIndex = 1;
            // We ensure each walkthrough step is ready to work (as we can't ensure components are waking in a determined order), then start the first step
            foreach (var step in m_Steps)
            {
                step.Initialize();
                var waypoint = Instantiate(m_Waypoint, step.gameObject.transform);
                waypoint.transform.localPosition = Vector3.zero;
                waypoint.transform.rotation = Quaternion.identity;
                var waypointText = waypoint.GetComponentInChildren<TMPro.TMP_Text>();
                waypointText.text = stepIndex.ToString();
                step.waypoint = waypoint;

                if (stepIndex > 1)
                {
                    var link = Instantiate(m_WaypointLink, step.gameObject.transform);
                    link.transform.localPosition = Vector3.zero;
                    link.transform.rotation = Quaternion.identity;
                    var linkCurve = link.GetComponentInChildren<WaypointCurve>();
                    linkCurve.start = m_Steps[stepIndex - 2].gameObject.transform.position;
                    linkCurve.end = m_Steps[stepIndex - 1].gameObject.transform.position;
                    step.link = link;
                }

                stepIndex++;
            }
            if (m_Steps != null && m_Steps.Length > 0)
                m_Steps[currentStep].StartStep(OnStepComplete);

            walkthroughChangedCallback?.Invoke();
        }

        void OnStepComplete(bool autoProgress)
        {
            // We still call the changed callback even if we are not auto-progressing, as some UI may want to update labels or controls
            if (!autoProgress)
            {
                walkthroughChangedCallback?.Invoke();
                return;
            }

            // If we are auto-progressing, increment the step index and start the process again
            currentStep++;

            if (m_LoopOnComplete && currentStep >= m_Steps.Length)
                currentStep = 0;

            if (m_Steps == null || currentStep >= m_Steps.Length)
                return;

            m_Steps[currentStep].StartStep(OnStepComplete);

            walkthroughChangedCallback?.Invoke();
        }
    }
}
