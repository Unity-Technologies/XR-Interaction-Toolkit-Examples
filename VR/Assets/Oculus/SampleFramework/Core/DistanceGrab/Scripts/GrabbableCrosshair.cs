/************************************************************************************

Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.  

See SampleFramework license.txt for license terms.  Unless required by applicable law 
or agreed to in writing, the sample code is provided “AS IS” WITHOUT WARRANTIES OR 
CONDITIONS OF ANY KIND, either express or implied.  See the license for specific 
language governing permissions and limitations under the license.

************************************************************************************/
using UnityEngine;
using System.Collections;

namespace OculusSampleFramework
{
    public class GrabbableCrosshair : MonoBehaviour
    {
        public enum CrosshairState { Disabled, Enabled, Targeted }

        CrosshairState m_state = CrosshairState.Disabled;
        Transform m_centerEyeAnchor;

        [SerializeField]
        GameObject m_targetedCrosshair = null;
        [SerializeField]
        GameObject m_enabledCrosshair = null;

        private void Start()
        {
            m_centerEyeAnchor = GameObject.Find("CenterEyeAnchor").transform;
        }

        public void SetState(CrosshairState cs)
        {
            m_state = cs;
            if (cs == CrosshairState.Disabled)
            {
                m_targetedCrosshair.SetActive(false);
                m_enabledCrosshair.SetActive(false);
            }
            else if (cs == CrosshairState.Enabled)
            {
                m_targetedCrosshair.SetActive(false);
                m_enabledCrosshair.SetActive(true);
            }
            else if (cs == CrosshairState.Targeted)
            {
                m_targetedCrosshair.SetActive(true);
                m_enabledCrosshair.SetActive(false);
            }
        }

        private void Update()
        {
            if (m_state != CrosshairState.Disabled)
            {
                transform.LookAt(m_centerEyeAnchor);
            }
        }
    }
}
