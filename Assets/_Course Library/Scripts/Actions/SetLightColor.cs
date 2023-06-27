using UnityEngine;

/// <summary>
/// Sets the color of a light using color or HUE
/// </summary>
[RequireComponent(typeof(Light))]
public class SetLightColor : MonoBehaviour
{
    private Light currentLight = null;

    private void Awake()
    {
        currentLight = GetComponent<Light>();
    }

    public void SetColor(Color color)
    {
        currentLight.color = color;
    }

    public void SetHue(float value)
    {
        Color.RGBToHSV(currentLight.color, out _, out float s, out float v);

        value = Mathf.Clamp(value, 0, 1);
        Color newColor = Color.HSVToRGB(value, s, v);

        currentLight.color = newColor;
    }

    public void ResetColor()
    {
        currentLight.color = Color.white;
    }
}
