using UnityEngine.Playables;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Component that when paired with an interactable will drive an associated timeline with the activate button
    /// Must be used with an action-based controller
    /// </summary>
    public class InteractionAnimator : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The timeline to drive with the activation button.")]
        PlayableDirector m_ToAnimate;

        bool m_Animating;
        XRBaseController m_Controller;

        void Start()
        {
            // We want to hook up to the Select events so we can read data about the interacting controller
            var interactable = GetComponent<IXRSelectInteractable>();
            if (interactable == null || interactable as Object == null)
            {
                Debug.LogWarning($"No interactable on {name} - no animation will be played.", this);
                enabled = false;
                return;
            }

            if (m_ToAnimate == null)
            {
                Debug.LogWarning($"No timeline configured on {name} - no animation will be played.", this);
                enabled = false;
                return;
            }

            interactable.selectEntered.AddListener(OnSelect);
            interactable.selectExited.AddListener(OnDeselect);
        }

        void Update()
        {
            if (m_Animating && m_Controller != null)
            {
                var floatValue = m_Controller.activateInteractionState.value;
                m_ToAnimate.time = floatValue;
            }
        }

        void OnSelect(SelectEnterEventArgs args)
        {
            // Get the controller from the interactor, and then the activation control from there
            var controllerInteractor = args.interactorObject as XRBaseControllerInteractor;
            if (controllerInteractor == null)
            {
                Debug.LogWarning($"Selected by {args.interactorObject.transform.name}, which is not an XRBaseControllerInteractor", this);
                return;
            }

            m_Controller = controllerInteractor.xrController;
            if (m_Controller == null)
            {
                Debug.LogWarning($"Selected by {controllerInteractor.name}, which does not have a valid XRBaseController", this);
                return;
            }

            // Ready to animate
            m_ToAnimate.Play();
            m_Animating = true;
        }

        void OnDeselect(SelectExitEventArgs args)
        {
            m_Animating = false;
            m_ToAnimate.Stop();
            m_Controller = null;
        }
    }
}
