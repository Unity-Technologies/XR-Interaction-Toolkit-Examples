using UnityEngine;

/// <summary>
/// This script makes it easier to toggle physics on a rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ApplyPhysics : MonoBehaviour
{
    private Rigidbody rigidBody = null;
    private CollisionDetectionMode originalMode = CollisionDetectionMode.Discrete;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        originalMode = rigidBody.collisionDetectionMode;
    }

    public void EnablePhysics()
    {
        rigidBody.collisionDetectionMode = originalMode;
        rigidBody.useGravity = true;
        rigidBody.isKinematic = false;
    }

    public void DisablePhysics()
    {
        rigidBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rigidBody.useGravity = false;
        rigidBody.isKinematic = true;
    }
}
