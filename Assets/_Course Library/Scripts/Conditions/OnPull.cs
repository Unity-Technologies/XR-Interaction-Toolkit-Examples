using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Tracks the distance of an interactor with begin, and end threshold
/// </summary>
public class OnPull : MonoBehaviour
{
    [Tooltip("Distance threshold the interactor must break")]
    public float threshold = 1.0f;

    // Once the threshold has been broken
    public UnityEvent OnBegin = new UnityEvent();

    // Once the threshold is no longer broken
    public UnityEvent OnEnd = new UnityEvent();

    private XRBaseInteractor pullInteractor = null;
    private Vector3 startPosition = Vector3.zero;
    private bool withinThreshold = false;

    public void BeginCheck(XRBaseInteractor interactor)
    {
        pullInteractor = interactor;
        startPosition = pullInteractor.transform.position;
    }

    public void EndCheck(XRBaseInteractor interactor)
    {
        pullInteractor = null;
        startPosition = Vector3.zero;
        OnEnd.Invoke();
    }

    private void Update()
    {
        CheckPull();
    }

    private void CheckPull()
    {
        if (pullInteractor)
        {
            bool thresholdCheck = CheckThreshold();

            if (thresholdCheck != withinThreshold)
            {
                withinThreshold = thresholdCheck;

                if (withinThreshold)
                {
                    OnBegin.Invoke();
                }
                else
                {
                    OnEnd.Invoke();
                }
            }
        }
    }

    private bool CheckThreshold()
    {
        Vector3 handDifference = pullInteractor.transform.position - startPosition;
        float distanceSqr = handDifference.magnitude;
        return (distanceSqr > threshold);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, threshold);
    }
}
