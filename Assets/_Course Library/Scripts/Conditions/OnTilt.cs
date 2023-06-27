using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// When an object is tilted, run some functionality
/// Used with a grabable object
/// </summary>
public class OnTilt : MonoBehaviour
{
    [Tooltip("Tilt range, 0 - 180 degrees")]
    [Range(0, 1)] public float threshold = 0.0f;

    [Serializable] public class TiltEvent : UnityEvent<MonoBehaviour> { }

    // Threshold has been broken
    public TiltEvent OnBegin = new TiltEvent();

    // Threshold is no longer broken
    public TiltEvent OnEnd = new TiltEvent();

    private bool withinThreshold = false;

    private void Update()
    {
        CheckOrientation();
    }

    private void CheckOrientation()
    {
        float similarity = Vector3.Dot(-transform.up, Vector3.up);
        similarity = Mathf.InverseLerp(-1, 1, similarity);

        bool thresholdCheck = similarity >= threshold;

        if (withinThreshold != thresholdCheck)
        {
            withinThreshold = thresholdCheck;

            if (withinThreshold)
            {
                OnBegin.Invoke(this);
            }
            else
            {
                OnEnd.Invoke(this);
            }
        }
    }
}
