using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BNG {
    /// <summary>
    /// A marker for valid teleport destinations
    /// </summary>
    public class TeleportDestination : MonoBehaviour {

        /// <summary>
        /// Where the player will be teleported to
        /// </summary>
        [Tooltip("Where the player will be teleported to")]
        public Transform DestinationTransform;

        /// <summary>
        /// Snap player to this rotation?
        /// </summary>+
        [Tooltip("Snap player to this rotation?")]
        public bool ForcePlayerRotation = false;

        [Tooltip("Called when a player uses the teleporter to enter this destination.")]
        public UnityEvent OnPlayerTeleported;
    }
}