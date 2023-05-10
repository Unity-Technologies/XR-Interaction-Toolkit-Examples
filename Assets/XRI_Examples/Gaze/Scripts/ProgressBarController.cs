using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Class to control progress bar on coaching card prefabs.
    /// </summary>
    public class ProgressBarController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The projectile that's created")]
        SkinnedMeshRenderer m_Blendshape = null;

        [SerializeField]
        [Tooltip("Lenght of the blendshape progress bar.")]
        float m_BarLength = 28.0f;

        [SerializeField]
        [Tooltip("Duration to dwell and fill the progress bar.")]
        float m_Seconds = 7.5f;

        [SerializeField]
        [Tooltip("The next step GameObject to enable when this step is complete.")]
        GameObject m_NextStep = null;

        float m_SecondsCnt;
        bool m_UpdateTimer;

        void Update()
        {
            if (m_UpdateTimer)
                UpdateTimer();
        }

        /// <summary>
        /// Updates the state of the proress bar.
        /// </summary>
        /// <param name="state">When true, the progress bar will progress. When false, the progresss bar will not progress.</param>
        /// <returns></returns>
        public void UpdateTimerState(bool state)
        {
            m_UpdateTimer = state;
        }

        void UpdateTimer()
        {
            m_SecondsCnt += Time.deltaTime;
            if (m_SecondsCnt >= m_Seconds)
            {
                m_SecondsCnt = 0f;
                if (m_NextStep != null)
                    m_NextStep.SetActive(true);

                gameObject.SetActive(false);
            }

            m_Blendshape.SetBlendShapeWeight(0, m_SecondsCnt / m_Seconds * m_BarLength);
        }
    }
}
