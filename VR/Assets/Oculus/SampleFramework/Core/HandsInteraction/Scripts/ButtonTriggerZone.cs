/************************************************************************************

Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.  

See SampleFramework license.txt for license terms.  Unless required by applicable law 
or agreed to in writing, the sample code is provided “AS IS” WITHOUT WARRANTIES OR 
CONDITIONS OF ANY KIND, either express or implied.  See the license for specific 
language governing permissions and limitations under the license.

************************************************************************************/

using UnityEngine;
using UnityEngine.Assertions;

namespace OculusSampleFramework
{
	/// <summary>
	/// Trigger zone of button, can be proximity, contact or action.
	/// </summary>
	public class ButtonTriggerZone : MonoBehaviour, ColliderZone
	{
		[SerializeField] private GameObject _parentInteractableObj = null;

		public Collider Collider { get; private set; }
		public Interactable ParentInteractable { get; private set; }

		public InteractableCollisionDepth CollisionDepth
		{
			get
			{
				var myColliderZone = (ColliderZone)this;
				var depth = ParentInteractable.ProximityCollider == myColliderZone ? InteractableCollisionDepth.Proximity :
				  ParentInteractable.ContactCollider == myColliderZone ? InteractableCollisionDepth.Contact :
				  ParentInteractable.ActionCollider == myColliderZone ? InteractableCollisionDepth.Action :
				  InteractableCollisionDepth.None;
				return depth;
			}
		}

		private void Awake()
		{
			Assert.IsNotNull(_parentInteractableObj);

			Collider = GetComponent<Collider>();
			ParentInteractable = _parentInteractableObj.GetComponent<Interactable>();
		}
	}
}
