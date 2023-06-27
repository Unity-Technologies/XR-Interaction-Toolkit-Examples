using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Manually teleport the player to a specific anchor
/// </summary>
public class TeleportPlayer : MonoBehaviour
{
    [Tooltip("The anchor the player is teleported to")]
    public TeleportationAnchor anchor = null;

    [Tooltip("The provider used to request the teleportation")]
    public TeleportationProvider provider = null;

    public void Teleport()
    {
        if(anchor && provider)
        {
            TeleportRequest request = CreateRequest();
            provider.QueueTeleportRequest(request);
        }
    }

    private TeleportRequest CreateRequest()
    {
        Transform anchorTransform = anchor.teleportAnchorTransform;

        TeleportRequest request = new TeleportRequest()
        {
            requestTime = Time.time,
            matchOrientation = anchor.matchOrientation,

            destinationPosition = anchorTransform.position,
            destinationRotation = anchorTransform.rotation
        };

        return request;
    }
}
