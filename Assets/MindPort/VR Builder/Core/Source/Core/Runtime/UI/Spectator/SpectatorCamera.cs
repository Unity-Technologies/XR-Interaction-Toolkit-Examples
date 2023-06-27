using System;
using VRBuilder.Core.Configuration;
using UnityEngine;

namespace VRBuilder.UX
{
    /// <summary>
    /// Spectator camera which sets its viewpoint to the one of the user.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SpectatorCamera : MonoBehaviour
    {
        private GameObject user;

        protected virtual void Start()
        {
            user = RuntimeConfigurator.Configuration.User.GameObject;
        }

        protected virtual void Update()
        {
            UpdateCameraPositionAndRotation();
        }

        /// <summary>
        /// Sets the position and rotation of the spectator camera to the one of the user.
        /// </summary>
        protected virtual void UpdateCameraPositionAndRotation()
        {
            if (user == null)
            {
                try
                {
                    user = RuntimeConfigurator.Configuration.User.GameObject;
                }
                catch (NullReferenceException)
                {
                    return;
                }
            }

            transform.SetPositionAndRotation(user.transform.position, user.transform.rotation);
        }
    }
}
