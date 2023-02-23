namespace UnityEngine.XR.Content.Animation
{
    /// <summary>
    /// Enables a component to react to the 'ActionBegin' animation event.
    /// </summary>
    /// <seealso cref="IAnimationEventActionFinished"/>
    public interface IAnimationEventActionBegin
    {
        void ActionBegin(string label);
    }

    /// <summary>
    /// Calls the 'ActionBegin' function on any supported component when the target animation begins.
    /// </summary>
    /// <seealso cref="AnimationEventActionFinished"/>
    public class AnimationEventActionBegin : StateMachineBehaviour
    {
        [SerializeField]
        [Tooltip("A label identifying the animation that has started.")]
        string m_Label;

        /// <inheritdoc />
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var eventReceiver = animator.GetComponentInParent<IAnimationEventActionBegin>();
            eventReceiver?.ActionBegin(m_Label);
        }
    }
}
