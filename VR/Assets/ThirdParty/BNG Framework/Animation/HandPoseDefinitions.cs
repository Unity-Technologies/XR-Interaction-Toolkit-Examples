using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// HandPoseId's are integers set on a Hand Animator component as an integer.
    /// This enum provides a way to associate integers with an easy to understand name
    /// Add any HandPoses here that you wish to use from the Editor
    /// </summary>
    public enum HandPoseId {
        // Default = 0, Generic = 1, etc.
        Default = 0,
        Generic = 1,
        PingPongBall = 2,
        Controller = 3,
        Rock = 4,
        
        // Hand Pose ID's can be in any order
        PistolGrip = 50
    }

    public class HandPoseDefinitions : MonoBehaviour { }
}

