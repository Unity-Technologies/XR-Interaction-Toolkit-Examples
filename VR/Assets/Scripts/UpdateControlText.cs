using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class UpdateControlText : MonoBehaviour
{
    [SerializeField]
    TextMesh m_TextMesh = new TextMesh();

    public void OnSliderEvent(float value)
    {
        if (m_TextMesh)
            m_TextMesh.text = string.Format("{0:0.#}", value);
    }

    public void OnKnobEvent(float value)
    {
        if (m_TextMesh)
            m_TextMesh.text = string.Format("{0:0.#}", value);
    }

    IEnumerator StartTextMeshFade(TextMesh textMesh)
    {
        float duration = 0.5f;
        float t = 0f;
        while (t <= duration)
        {
            t += Time.deltaTime;
            textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b,
                Mathf.Lerp(1.0f, 0.0f, Mathf.Min(Mathf.SmoothStep(0f, 1f, t / duration), 1.0f)));
            yield return null;
        }
    }

    public void OnButtonEvent()
    {
        if (m_TextMesh)
            StartCoroutine(StartTextMeshFade(m_TextMesh));
    }
}
