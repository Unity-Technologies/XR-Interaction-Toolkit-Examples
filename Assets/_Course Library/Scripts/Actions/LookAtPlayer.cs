using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Make the object look at the XR Origin's camera
/// </summary>
public class LookAtPlayer : MonoBehaviour
{
    [Tooltip("Follow x axis")]
    public bool lookX = false;

    [Tooltip("Follow y axis")]
    public bool lookY = false;

    [Tooltip("Follow z axis")]
    public bool lookZ = false;

    private GameObject cameraObject = null;
    private Vector3 originalRotation = Vector3.zero;

    private void Awake()
    {
        cameraObject = FindObjectOfType<XROrigin>().Camera.gameObject;
        originalRotation = transform.eulerAngles;
    }

    private void Update()
    {
        LookAt();
    }

    private void LookAt()
    {
        Vector3 direction = transform.position - cameraObject.transform.position;
        Vector3 newRotation =  Quaternion.LookRotation(direction, transform.up).eulerAngles;

        newRotation.x = lookX ? newRotation.x : originalRotation.x;
        newRotation.y = lookY ? newRotation.y : originalRotation.y;
        newRotation.z = lookZ ? newRotation.z : originalRotation.z;

        transform.rotation = Quaternion.Euler(newRotation);
    }
}
