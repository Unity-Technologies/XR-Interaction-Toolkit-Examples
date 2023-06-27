using UnityEngine;

/// <summary>
/// Set the rotation of an object
/// </summary>
public class RotateObject : MonoBehaviour
{
    [Tooltip("The value at which the speed is applied")]
    [Range(0, 1)] public float sensitivity = 1.0f;

    [Tooltip("The max speed of the rotation")]
    public float speed = 10.0f;

    private bool isRotating = false;

    public void SetIsRotating(bool value)
    {
        if(value)
        {
            Begin();
        }
        else
        {
            End();
        }
    }

    public void Begin()
    {
        isRotating = true;
    }

    public void End()
    {
        isRotating = false;
    }

    public void ToggleRotate()
    {
        isRotating = !isRotating;
    }


    public void SetSpeed(float value)
    {
        sensitivity = Mathf.Clamp(value, 0, 1);
        isRotating = (sensitivity * speed) != 0.0f;
    }

    private void Update()
    {
        if (isRotating)
            Rotate();
    }

    private void Rotate()
    {
        transform.Rotate(transform.up, (sensitivity * speed) * Time.deltaTime);
    }
}
