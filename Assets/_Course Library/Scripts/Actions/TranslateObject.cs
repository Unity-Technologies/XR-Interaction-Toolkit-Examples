using UnityEngine;

public class TranslateObject : MonoBehaviour
{
    private Vector3 startPos;
    private Quaternion startRot;

    private void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;

    }

    public void TranslateX(float amount)
    {
        transform.position += transform.right * amount;
    }

    public void TranslateY(float amount)
    {
        transform.position += transform.up * amount;
    }

    public void TranslateZ(float amount)
    {
        transform.position += transform.forward * amount;
    }

    public void SetLocalXPosition(float newPosition)
    {
        transform.localPosition = new Vector3(newPosition, transform.localPosition.y, transform.localPosition.z);
    }

    public void SetLocalYPosition(float newPosition)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, newPosition, transform.localPosition.z);
    }

    public void SetLocalZPosition(float newPosition)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, newPosition);
    }

    public void ResetToStartingPosition()
    {
        transform.SetPositionAndRotation(startPos, startRot);

;
    }

}
