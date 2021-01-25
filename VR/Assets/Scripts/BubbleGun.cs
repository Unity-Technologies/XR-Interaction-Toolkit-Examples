using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class BubbleGun : MonoBehaviour
{
    XRGrabInteractable m_InteractableBase;
    Animator m_Animator;
    
    [SerializeField] ParticleSystem m_BubbleParticleSystem = null;

    const string k_AnimTriggerDown = "TriggerDown";
    const string k_AnimTriggerUp = "TriggerUp";
    const float k_HeldThreshold = 0.1f;

    float m_TriggerHeldTime;
    bool m_TriggerDown;

    protected void Start()
    {
        m_InteractableBase = GetComponent<XRGrabInteractable>();
        m_Animator = GetComponent<Animator>();
        m_InteractableBase.selectExited.AddListener(DroppedGun);
        m_InteractableBase.activated.AddListener(TriggerPulled);
        m_InteractableBase.deactivated.AddListener(TriggerReleased);
    }

    protected void Update()
    {
        if (m_TriggerDown)
        {
            m_TriggerHeldTime += Time.deltaTime;

            if (m_TriggerHeldTime >= k_HeldThreshold)
            {
                if (!m_BubbleParticleSystem.isPlaying)
                {
                    m_BubbleParticleSystem.Play();    
                }
            }
        }
    }

    void TriggerReleased(DeactivateEventArgs args)
    {
        m_Animator.SetTrigger(k_AnimTriggerUp);
        m_TriggerDown = false;
        m_TriggerHeldTime = 0f;
        m_BubbleParticleSystem.Stop();
    }

    void TriggerPulled(ActivateEventArgs args)
    {
        m_Animator.SetTrigger(k_AnimTriggerDown);
        m_TriggerDown = true;
    }

    void DroppedGun(SelectExitEventArgs args)
    {
        // In case the gun is dropped while in use.
        m_Animator.SetTrigger(k_AnimTriggerUp);

        m_TriggerDown = false;
        m_TriggerHeldTime = 0f;
        m_BubbleParticleSystem.Stop();
    }

    public void ShootEvent()
    {
        m_BubbleParticleSystem.Emit(1);
    }
}
