/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Assets.OVR.Scripts;

public class OVRProfilerDeprecated : EditorWindow
{
	[MenuItem("Oculus/Tools/(Deprecated) OVR Profiler", false, 200000)]
	static void Init()
	{
		Debug.LogWarning("OVR Profiler has been replaced by OVR Performance Lint Tool");
		// Get existing open window or if none, make a new one:
		EditorWindow.GetWindow(typeof(OVRLint));
		OVRPlugin.SendEvent("perf_lint", "activated");
		OVRLint.RunCheck();
	}
}

#endif
