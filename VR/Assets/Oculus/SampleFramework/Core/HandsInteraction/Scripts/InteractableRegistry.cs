/************************************************************************************

Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.  

See SampleFramework license.txt for license terms.  Unless required by applicable law 
or agreed to in writing, the sample code is provided “AS IS” WITHOUT WARRANTIES OR 
CONDITIONS OF ANY KIND, either express or implied.  See the license for specific 
language governing permissions and limitations under the license.

************************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace OculusSampleFramework
{
	/// <summary>
	/// In case someone wants to know about all interactables in a scene,
	/// this registry is the easiest way to access that information.
	/// </summary>
	public class InteractableRegistry : MonoBehaviour
	{
		public static HashSet<Interactable> _interactables = new HashSet<Interactable>();

		public static HashSet<Interactable> Interactables
		{
			get
			{
				return _interactables;
			}
		}

		public static void RegisterInteractable(Interactable interactable)
		{
			Interactables.Add(interactable);
		}

		public static void UnregisterInteractable(Interactable interactable)
		{
			Interactables.Remove(interactable);
		}
	}
}
