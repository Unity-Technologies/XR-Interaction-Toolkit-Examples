using UnityEngine;

/// <summary>
/// On collision align the object with the normal of the surface that was hit
/// </summary>
public class StickToObject : MonoBehaviour
{
    public void AlignWithSurface(Collision collision)
    {
        ContactPoint contactPoint = collision.GetContact(0);
        transform.up = -contactPoint.normal;
    }
}
