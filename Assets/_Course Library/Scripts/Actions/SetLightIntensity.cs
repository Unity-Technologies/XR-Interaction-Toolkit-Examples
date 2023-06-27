using UnityEngine;

/// <summary>
/// Sets the intensity of a light
/// </summary>
[RequireComponent(typeof(Light))]
public class SetLightIntensity : MonoBehaviour
{
    private Light currentLight = null;

    private void Awake()
    {
        currentLight = GetComponent<Light>();
    }

    public void SetIntensity(float value)
    {
        currentLight.intensity = value;
    }
}
