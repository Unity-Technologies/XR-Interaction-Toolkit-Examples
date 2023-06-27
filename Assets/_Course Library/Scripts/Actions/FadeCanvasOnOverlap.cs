using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FadeCanvasOnOverlap : MonoBehaviour
{
    [Tooltip("Used for the radius of overlap sphere")]
    public float checkDistance = 0.5f;

    [Tooltip("What layers trigger the overlap?")]
    public LayerMask overlapMask = 0;

    [Serializable] public class ValueChangeEvent : UnityEvent<float> { }
    // Updates when the position of the head changes
    public ValueChangeEvent OnValueChange = new ValueChangeEvent();

    private void Update()
    {
        CheckOverlap();
    }

    private void CheckOverlap()
    {
        Collider[] results = Physics.OverlapSphere(transform.position, checkDistance, overlapMask, QueryTriggerInteraction.Ignore);

        // Get bounds of each, find the nearest
        List<Vector3> bounds = GetColliderBounds(results);
        float closest = FindClosestPoint(bounds);

        // What's the percentage of the overlapping object?
        float percentage = Mathf.InverseLerp(checkDistance / 2.0f, 0.0f, closest);
        OnValueChange.Invoke(percentage);
    }

    private List<Vector3> GetColliderBounds(Collider[] colliders)
    {
        List<Vector3> allBounds = new List<Vector3>();

        foreach (Collider collider in colliders)
        {
            Bounds bounds = collider.bounds;
            allBounds.Add(bounds.ClosestPoint(transform.position));
        }

        return allBounds;
    }

    private float FindClosestPoint(List<Vector3> closestPoints)
    {
        float smallestDistance = float.MaxValue;

        foreach (Vector3 point in closestPoints)
        {
            float distance = (transform.position - point).magnitude;

            if (distance < smallestDistance)
                smallestDistance = distance;
        }

        return smallestDistance;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, checkDistance);
    }
}