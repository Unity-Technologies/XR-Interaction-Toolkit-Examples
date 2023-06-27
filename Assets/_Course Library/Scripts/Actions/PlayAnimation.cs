using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayAnimation : MonoBehaviour
{
    private Animator animator = null;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void SetBoolTrue(string parameter)
    {
        animator.SetBool(parameter, true);
    }

    public void SetBoolFalse(string parameter)
    {
        animator.SetBool(parameter, false);
    }

    public void SetTrigger(string parameter)
    {
        animator.SetTrigger(parameter);
    }
}
