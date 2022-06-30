using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayinAnimation : MonoBehaviour
{
    //[SerializeField] public Animation myAnimationController;
    public Animator myAnimationController;
    public AnimationClip AnimationToPlay;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            myAnimationController.SetFloat("Sculpt Clay", 1);
            //AnimationToPlay.SampleAnimation(GameObject go, 1 deltatime)
            //AnimationToPlay = true; m_PlayAutomatically m_Animations
           
            Debug.Log("Touched");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            myAnimationController.SetFloat("ClaySculpt", 0);
        }
    }
}


