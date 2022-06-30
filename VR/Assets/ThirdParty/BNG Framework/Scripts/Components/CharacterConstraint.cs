using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

	/// <summary>
	/// This CharacterConstraint will keep the size the Character Capsule along with the camera if not colliding with anything
	/// </summary>
	public class CharacterConstraint : MonoBehaviour {

		BNGPlayerController bngController;
		CharacterController character;

		void Awake() {
			character = GetComponent<CharacterController>();
			bngController = transform.GetComponentInParent<BNGPlayerController>();
		}

		private void Update() {
			CheckCharacterCollisionMove();
		}

		public virtual void CheckCharacterCollisionMove() {

			var initialCameraRigPosition = bngController.CameraRig.transform.position;
			var cameraPosition = bngController.CenterEyeAnchor.position;
			var delta = cameraPosition - transform.position;

			// Ignore Y position
			delta.y = 0;

			// Move Character Controller and Camera Rig to Camera's delta
			if (delta.magnitude > 0) {
				character.Move(delta);

				// Move Camera Rig back into position
				bngController.CameraRig.transform.position = initialCameraRigPosition;
			}
		}
	}
}