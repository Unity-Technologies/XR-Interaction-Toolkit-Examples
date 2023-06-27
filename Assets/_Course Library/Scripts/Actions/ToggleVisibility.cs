using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script toggles the visibility of a GameObject.
/// </summary>

public class ToggleVisibility : MonoBehaviour
{
    private Renderer currentRenderer = null;

    private void Awake()
    {
        currentRenderer = GetComponent<Renderer>();
    }

    public void Toggle()
    {
        bool isEnabled = !currentRenderer.enabled;
        currentRenderer.enabled = isEnabled;
    }
}
