using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BNG {

    /// <summary>
    /// This script will adjust the player's virtual height to match the 'DesiredPlayerHeight' property.
    /// For example, if the player's height is 1.5 meters tall, but the DesiredPlayerHeight = 1.6, then the player's virtual height will be increased by 0.1.
    /// </summary>
    public class CalibratePlayerHeight : MonoBehaviour {

        [Tooltip("Desired height of the player in meters. The player's presence in vr will be adjusted based on their physical height. 1.65 meters = 5.41 feet")]
        public float DesiredPlayerHeight = 1.65f;

        [Tooltip("Adjust the CharacterControllerYOffset property of this playerController. If not specified one will be found using GetComponentInChildren()")]
        public BNGPlayerController PlayerController;

        [Header("Startup")]
        [Tooltip("If true, the player's virtual height will be adjusted to match DesiredPlayerHeight on Start()")]
        public bool CalibrateOnStart = true;

        [Header("Input :")]
        [Tooltip("If specified, pressing this button / action will activate the calibration")]
        public InputAction CalibrateHeightAction;

        private float _initialOffset = 0;

        void Start() {

            if(CalibrateHeightAction != null) {
                CalibrateHeightAction.Enable();
                CalibrateHeightAction.performed += context => { CalibrateHeight(); };
            }

            if(PlayerController == null) {
                PlayerController = GetComponentInChildren<BNGPlayerController>();
            }

            if(PlayerController) {
                _initialOffset = PlayerController.CharacterControllerYOffset;
            }

            if(CalibrateOnStart) {
                CalibrateHeight();
            }
        }

        public void CalibrateHeight() {
            CalibrateHeight(DesiredPlayerHeight);
        }
        
        public void CalibrateHeight(float calibrateHeight) {

            float physicalHeight = GetCurrentPlayerHeight();

            PlayerController.CharacterControllerYOffset = calibrateHeight - physicalHeight;
        }

        public void ResetPlayerHeight() {
            PlayerController.CharacterControllerYOffset = _initialOffset;
        }

        public float GetCurrentPlayerHeight() {
            if(PlayerController != null) {
                return PlayerController.CameraHeight;
            }

            return 0;
        }
    }
}

