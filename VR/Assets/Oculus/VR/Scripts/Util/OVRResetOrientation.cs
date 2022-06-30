/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using UnityEngine;

/// <summary>
/// Allows you to reset VR input tracking with a gamepad button press.
/// </summary>
public class OVRResetOrientation : MonoBehaviour
{
	/// <summary>
	/// The gamepad button that will reset VR input tracking.
	/// </summary>
	public OVRInput.RawButton resetButton = OVRInput.RawButton.Y;

	/// <summary>
	/// Check input and reset orientation if necessary
	/// See the input mapping setup in the Unity Integration guide
	/// </summary>
	void Update()
	{
		// NOTE: some of the buttons defined in OVRInput.RawButton are not available on the Android game pad controller
		if (OVRInput.GetDown(resetButton))
		{
			//*************************
			// reset orientation
			//*************************
			OVRManager.display.RecenterPose();
		}
	}
}
