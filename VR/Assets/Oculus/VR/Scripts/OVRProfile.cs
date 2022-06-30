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
using System.Collections;
using System.Threading;

/// <summary>
/// (Deprecated) Contains information about the user's preferences and body dimensions.
/// </summary>
public class OVRProfile : Object
{
	[System.Obsolete]
	public enum State
	{
		NOT_TRIGGERED,
		LOADING,
		READY,
		ERROR
	};

	[System.Obsolete]
	public string id { get { return "000abc123def"; } }
	[System.Obsolete]
	public string userName { get { return "Oculus User"; } }
	[System.Obsolete]
	public string locale { get { return "en_US"; } }

	public float ipd { get { return Vector3.Distance (OVRPlugin.GetNodePose (OVRPlugin.Node.EyeLeft, OVRPlugin.Step.Render).ToOVRPose ().position, OVRPlugin.GetNodePose (OVRPlugin.Node.EyeRight, OVRPlugin.Step.Render).ToOVRPose ().position); } }
	public float eyeHeight { get { return OVRPlugin.eyeHeight; } }
	public float eyeDepth { get { return OVRPlugin.eyeDepth; } }
	public float neckHeight { get { return eyeHeight - 0.075f; } }

	[System.Obsolete]
	public State state { get { return State.READY; } }
}
