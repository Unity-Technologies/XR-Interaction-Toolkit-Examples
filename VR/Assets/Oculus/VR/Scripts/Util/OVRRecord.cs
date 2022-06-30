/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.OVR.Scripts
{
	public class Record
	{
		public string category;
		public string message;
		public Record(string cat, string msg)
		{
			category = cat;
			message = msg;
		}
	}

	public class RangedRecord : Record
	{
		public float value;
		public float min;
		public float max;
		public RangedRecord(string cat, string msg, float val, float minVal, float maxVal)
			: base(cat, msg)
		{
			value = val;
			min = minVal;
			max = maxVal;
		}
	}

	public delegate void FixMethodDelegate(UnityEngine.Object obj, bool isLastInSet, int selectedIndex);

	public class FixRecord : Record
	{
		public FixMethodDelegate fixMethod;
		public UnityEngine.Object targetObject;
		public string[] buttonNames;
		public bool editModeRequired;
		public bool complete;

		public FixRecord(string cat, string msg, FixMethodDelegate fix, UnityEngine.Object target, bool editRequired, string[] buttons)
			: base(cat, msg)
		{
			buttonNames = buttons;
			fixMethod = fix;
			targetObject = target;
			editModeRequired = editRequired;
			complete = false;
		}
	}
}
