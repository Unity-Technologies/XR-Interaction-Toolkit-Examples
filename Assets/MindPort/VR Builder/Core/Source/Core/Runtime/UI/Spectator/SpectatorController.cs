using System;
using VRBuilder.Core.Input;

namespace VRBuilder.UX
{
    /// <summary>
    /// Controller for a spectator to toggle UI visibility.
    /// </summary>
    public class SpectatorController : InputActionListener
    {
        /// <summary>
        /// Event fired when UI visibility is toggled.
        /// </summary>
        public event EventHandler ToggleUIOverlayVisibility;

        protected void OnEnable()
        {
            RegisterInputEvent(ToggleOverlay);
        }
        
        protected void OnDisable()
        {
            UnregisterInputEvent(ToggleOverlay);
        }

        protected void ToggleOverlay(InputController.InputEventArgs args)
        {
            ToggleUIOverlayVisibility?.Invoke(this, new EventArgs());
        }
    }
}
