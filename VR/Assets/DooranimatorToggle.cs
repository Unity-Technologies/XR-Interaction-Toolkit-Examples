using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DooranimatorToggle : MonoBehaviour
{
    private Animator DoorAnimator;
    private bool IsOpen;
    private void Awake()
    {
        DoorAnimator = GetComponent<Animator>();
    }

    public void OnButtonClick()
    {
        IsOpen = !IsOpen;
        DoorAnimator.SetBool("DoorClose", !IsOpen);
        DoorAnimator.SetTrigger("DoorPress");
    }
   // public void OnButtonExit()
    //{
      //  DoorAnimator.SetBool("DoorClose", IsOpen);
      //    DoorAnimator.ResetTrigger("DoorPress");
    //}
}
