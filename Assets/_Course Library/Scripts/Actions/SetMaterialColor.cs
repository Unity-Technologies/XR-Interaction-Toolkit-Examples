using UnityEngine;

/// <summary>
/// Change the color a meterial using a color, or Hue
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class SetMaterialColor : MonoBehaviour
{
    [Tooltip("The material that's used for the color change")]
    public Material material = null;

    public void SetColor(Color color)
    {
        material.color = color;
    }

    public void SetHue(float value)
    {
        Color.RGBToHSV(material.color, out _, out float s, out float v);

        value = Mathf.Clamp(value, 0, 1);
        Color newColor = Color.HSVToRGB(value, s, v);

        material.color = newColor;
    }
}
