/************************************************************************************

Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.  

See SampleFramework license.txt for license terms.  Unless required by applicable law 
or agreed to in writing, the sample code is provided “AS IS” WITHOUT WARRANTIES OR 
CONDITIONS OF ANY KIND, either express or implied.  See the license for specific 
language governing permissions and limitations under the license.

************************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OculusSampleFramework
{
    public class DistanceGrabberSample : MonoBehaviour
    {

        bool useSpherecast = false;
        bool allowGrabThroughWalls = false;

        public bool UseSpherecast
        {
            get { return useSpherecast; }
            set
            {
                useSpherecast = value;
                for (int i = 0; i < m_grabbers.Length; ++i)
                {
                    m_grabbers[i].UseSpherecast = useSpherecast;
                }
            }
        }

        public bool AllowGrabThroughWalls
        {
            get { return allowGrabThroughWalls; }
            set
            {
                allowGrabThroughWalls = value;
                for (int i = 0; i < m_grabbers.Length; ++i)
                {
                    m_grabbers[i].m_preventGrabThroughWalls = !allowGrabThroughWalls;
                }
            }
        }

        [SerializeField]
        DistanceGrabber[] m_grabbers = null;

        // Use this for initialization
        void Start()
        {
            DebugUIBuilder.instance.AddLabel("Distance Grab Sample");
            DebugUIBuilder.instance.AddToggle("Use Spherecasting", ToggleSphereCasting, useSpherecast);
            DebugUIBuilder.instance.AddToggle("Grab Through Walls", ToggleGrabThroughWalls, allowGrabThroughWalls);
            DebugUIBuilder.instance.Show();

			// Forcing physics tick rate to match game frame rate, for improved physics in this sample.
			// See comment in OVRGrabber.Update for more information.
			float freq = OVRManager.display.displayFrequency;
			if(freq > 0.1f)
			{
				Debug.Log("Setting Time.fixedDeltaTime to: " + (1.0f / freq));
				Time.fixedDeltaTime = 1.0f / freq;
			}
        }

        public void ToggleSphereCasting(Toggle t)
        {
            UseSpherecast = !UseSpherecast;
        }

        public void ToggleGrabThroughWalls(Toggle t)
        {
            AllowGrabThroughWalls = !AllowGrabThroughWalls;
        }
    }
}
