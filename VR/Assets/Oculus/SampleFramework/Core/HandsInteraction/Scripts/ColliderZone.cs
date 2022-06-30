/************************************************************************************

Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.  

See SampleFramework license.txt for license terms.  Unless required by applicable law 
or agreed to in writing, the sample code is provided “AS IS” WITHOUT WARRANTIES OR 
CONDITIONS OF ANY KIND, either express or implied.  See the license for specific 
language governing permissions and limitations under the license.

************************************************************************************/

using System;
using UnityEngine;

namespace OculusSampleFramework
{
	/// <summary>
	/// Zone that can be collided with in example code.
	/// </summary>
	public interface ColliderZone
	{
		Collider Collider { get; }
		// Which interactable do we belong to?
		Interactable ParentInteractable { get; }
		InteractableCollisionDepth CollisionDepth { get; }
	}

	/// <summary>
	/// Arguments for object interacting with collider zone.
	/// </summary>
	public class ColliderZoneArgs : EventArgs
	{
		public readonly ColliderZone Collider;
		public readonly float FrameTime;
		public readonly InteractableTool CollidingTool;
		public readonly InteractionType InteractionT;

		public ColliderZoneArgs(ColliderZone collider, float frameTime,
		  InteractableTool collidingTool, InteractionType interactionType)
		{
			Collider = collider;
			FrameTime = frameTime;
			CollidingTool = collidingTool;
			InteractionT = interactionType;
		}
	}

	public enum InteractionType
	{
		Enter = 0,
		Stay,
		Exit
	}
}
