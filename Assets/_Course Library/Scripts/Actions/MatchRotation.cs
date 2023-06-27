using UnityEngine;

/// <summary>
/// Match the rotation of the target transform
/// </summary>
public class MatchRotation : MonoBehaviour
{
    [Tooltip("Match x rotation")]
    public bool matchX = false;

    [Tooltip("Match y rotation")]
    public bool matchY = false;

    [Tooltip("Match z rotation")]
    public bool matchZ = false;

    [Tooltip("The transform this object will match")]
    public Transform targetTransform = null;
    private Vector3 originalRotation = Vector3.zero;

    private void Awake()
    {
        originalRotation = transform.eulerAngles;
    }

    public void FollowRotation()
    {
        Vector3 newRotation = targetTransform.eulerAngles;

        newRotation.x = matchX ? newRotation.x : originalRotation.x;
        newRotation.y = matchY ? newRotation.y : originalRotation.y;
        newRotation.z = matchZ ? newRotation.z : originalRotation.z;

        transform.rotation = Quaternion.Euler(newRotation);
    }
}
