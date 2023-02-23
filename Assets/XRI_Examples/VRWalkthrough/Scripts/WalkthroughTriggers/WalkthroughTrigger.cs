namespace UnityEngine.XR.Content.Walkthrough
{
    /// <summary>
    /// Base class for all triggers used by walkthrough steps.
    /// </summary>
    abstract public class WalkthroughTrigger : MonoBehaviour
    {
        /// <summary>
        /// Attempts to return a trigger to a state where it can be activated again
        /// </summary>
        /// <returns>False if this trigger cannot be reset or would automatically fire</returns>
        public abstract bool ResetTrigger();

        /// <summary>
        /// Checks if this trigger's pass/fail condition is active
        /// </summary>
        /// <returns>True if this trigger is no longer blocking the current walkthrough step</returns>
        public abstract bool Check();
    }
}
