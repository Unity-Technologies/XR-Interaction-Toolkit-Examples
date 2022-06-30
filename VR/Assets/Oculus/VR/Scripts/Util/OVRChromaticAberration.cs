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
/// Allows you to toggle chromatic aberration correction with a gamepad button press.
/// </summary>
public class OVRChromaticAberration : MonoBehaviour
{
	/// <summary>
	/// The button that will toggle chromatic aberration correction.
	/// </summary>
	public OVRInput.RawButton			toggleButton = OVRInput.RawButton.X;

	private bool								chromatic = false;

	void Start ()
	{
		// Enable/Disable Chromatic Aberration Correction.
		// NOTE: Enabling Chromatic Aberration for mobile has a large performance cost.
		OVRManager.instance.chromatic = chromatic;
	}

	void Update()
	{
		// NOTE: some of the buttons defined in OVRInput.RawButton are not available on the Android game pad controller
		if (OVRInput.GetDown(toggleButton))
		{
			//*************************
			// toggle chromatic aberration correction
			//*************************
			chromatic = !chromatic;
			OVRManager.instance.chromatic = chromatic;
		}
	}

}
