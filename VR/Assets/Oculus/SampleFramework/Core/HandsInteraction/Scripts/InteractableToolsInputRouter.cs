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
	/// Routes all collisions from interactable tools to the interactables themselves.
	/// We want to do this in a top-down fashion, because we might want to disable
	/// far-field interactions if near-field interactions take precendence (for instance).
	/// </summary>
	public class InteractableToolsInputRouter : MonoBehaviour
	{
		private static InteractableToolsInputRouter _instance;
		private bool _leftPinch, _rightPinch;

		public static InteractableToolsInputRouter Instance
		{
			get
			{
				if (_instance == null)
				{
					var instances = FindObjectsOfType<InteractableToolsInputRouter>();
					if (instances.Length > 0)
					{
						_instance = instances[0];
						// remove extras, if any
						for (int i = 1; i < instances.Length; i++)
						{
							GameObject.Destroy(instances[i].gameObject);
						}
					}
				}

				return _instance;
			}
		}

		private HashSet<InteractableTool> _leftHandNearTools = new HashSet<InteractableTool>();
		private HashSet<InteractableTool> _leftHandFarTools = new HashSet<InteractableTool>();
		private HashSet<InteractableTool> _rightHandNearTools = new HashSet<InteractableTool>();
		private HashSet<InteractableTool> _rightHandFarTools = new HashSet<InteractableTool>();

		public void RegisterInteractableTool(InteractableTool interactableTool)
		{
			if (interactableTool.IsRightHandedTool)
			{
				if (interactableTool.IsFarFieldTool)
				{
					_rightHandFarTools.Add(interactableTool);
				}
				else
				{
					_rightHandNearTools.Add(interactableTool);
				}
			}
			else
			{
				if (interactableTool.IsFarFieldTool)
				{
					_leftHandFarTools.Add(interactableTool);
				}
				else
				{
					_leftHandNearTools.Add(interactableTool);
				}
			}
		}

		public void UnregisterInteractableTool(InteractableTool interactableTool)
		{
			if (interactableTool.IsRightHandedTool)
			{
				if (interactableTool.IsFarFieldTool)
				{
					_rightHandFarTools.Remove(interactableTool);
				}
				else
				{
					_rightHandNearTools.Remove(interactableTool);
				}
			}
			else
			{
				if (interactableTool.IsFarFieldTool)
				{
					_leftHandFarTools.Remove(interactableTool);
				}
				else
				{
					_leftHandNearTools.Remove(interactableTool);
				}
			}
		}

		private void Update()
		{
			if (!HandsManager.Instance.IsInitialized())
			{
				return;
			}

			bool leftHandIsReliable = HandsManager.Instance.LeftHand.IsTracked &&
				HandsManager.Instance.LeftHand.HandConfidence == OVRHand.TrackingConfidence.High;
			bool rightHandIsReliable = HandsManager.Instance.RightHand.IsTracked &&
				HandsManager.Instance.RightHand.HandConfidence == OVRHand.TrackingConfidence.High;
			bool leftHandProperlyTracked = HandsManager.Instance.LeftHand.IsPointerPoseValid;
			bool rightHandProperlyTracked = HandsManager.Instance.RightHand.IsPointerPoseValid;

			bool encounteredNearObjectsLeftHand = UpdateToolsAndEnableState(_leftHandNearTools, leftHandIsReliable);
			// don't interact with far field if near field is touching something
			UpdateToolsAndEnableState(_leftHandFarTools, !encounteredNearObjectsLeftHand && leftHandIsReliable &&
			  leftHandProperlyTracked);

			bool encounteredNearObjectsRightHand = UpdateToolsAndEnableState(_rightHandNearTools, rightHandIsReliable);
			// don't interact with far field if near field is touching something
			UpdateToolsAndEnableState(_rightHandFarTools, !encounteredNearObjectsRightHand && rightHandIsReliable &&
			  rightHandProperlyTracked);
		}

		private bool UpdateToolsAndEnableState(HashSet<InteractableTool> tools, bool toolsAreEnabledThisFrame)
		{
			bool encounteredObjects = UpdateTools(tools, resetCollisionData: !toolsAreEnabledThisFrame);
			ToggleToolsEnableState(tools, toolsAreEnabledThisFrame);
			return encounteredObjects;
		}

		/// <summary>
		/// Update tools specified based on new collisions.
		/// </summary>
		/// <param name="tools">Tools to update.</param>
		/// <param name="resetCollisionData">True if we want the tool to be disabled. This can happen
		/// if near field tools take precedence over far-field tools, for instance.</param>
		/// <returns></returns>
		private bool UpdateTools(HashSet<InteractableTool> tools, bool resetCollisionData = false)
		{
			bool toolsEncounteredObjects = false;

			foreach (InteractableTool currentInteractableTool in tools)
			{
				List<InteractableCollisionInfo> intersectingObjectsFound =
					currentInteractableTool.GetNextIntersectingObjects();

				if (intersectingObjectsFound.Count > 0 && !resetCollisionData)
				{
					if (!toolsEncounteredObjects)
					{
						toolsEncounteredObjects = intersectingObjectsFound.Count > 0;
					}

					// create map that indicates the furthest collider encountered per interactable element
					currentInteractableTool.UpdateCurrentCollisionsBasedOnDepth();

					if (currentInteractableTool.IsFarFieldTool)
					{
						var firstInteractable = currentInteractableTool.GetFirstCurrentCollisionInfo();
						// if our tool is activated, make sure depth is set to "action"
						if (currentInteractableTool.ToolInputState == ToolInputState.PrimaryInputUp)
						{
							firstInteractable.Value.InteractableCollider = firstInteractable.Key.ActionCollider;
							firstInteractable.Value.CollisionDepth = InteractableCollisionDepth.Action;
						}
						else
						{
							firstInteractable.Value.InteractableCollider = firstInteractable.Key.ContactCollider;
							firstInteractable.Value.CollisionDepth = InteractableCollisionDepth.Contact;
						}

						// far field tools only can focus elements -- pick first (for now)
						currentInteractableTool.FocusOnInteractable(firstInteractable.Key,
							firstInteractable.Value.InteractableCollider);
					}
				}
				else
				{
					currentInteractableTool.DeFocus();
					currentInteractableTool.ClearAllCurrentCollisionInfos();
				}

				currentInteractableTool.UpdateLatestCollisionData();
			}

			return toolsEncounteredObjects;
		}

		private void ToggleToolsEnableState(HashSet<InteractableTool> tools, bool enableState)
		{
			foreach (InteractableTool tool in tools)
			{
				if (tool.EnableState != enableState)
				{
					tool.EnableState = enableState;
				}
			}
		}
	}
}
