/************************************************************************************

Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.  

See SampleFramework license.txt for license terms.  Unless required by applicable law 
or agreed to in writing, the sample code is provided “AS IS” WITHOUT WARRANTIES OR 
CONDITIONS OF ANY KIND, either express or implied.  See the license for specific 
language governing permissions and limitations under the license.

************************************************************************************/

using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Visual of finger tip poke tool.
/// </summary>
namespace OculusSampleFramework
{
	public class FingerTipPokeToolView : MonoBehaviour, InteractableToolView
	{
		[SerializeField] private MeshRenderer _sphereMeshRenderer = null;

		public InteractableTool InteractableTool { get; set; }

		public bool EnableState
		{
			get
			{
				return _sphereMeshRenderer.enabled;
			}
			set
			{
				_sphereMeshRenderer.enabled = value;
			}
		}

		public bool ToolActivateState { get; set; }

		public float SphereRadius { get; private set; }

		private void Awake()
		{
			Assert.IsNotNull(_sphereMeshRenderer);
			SphereRadius = _sphereMeshRenderer.transform.localScale.z * 0.5f;
		}

		public void SetFocusedInteractable(Interactable interactable)
		{
			// nothing to see here
		}
	}
}
