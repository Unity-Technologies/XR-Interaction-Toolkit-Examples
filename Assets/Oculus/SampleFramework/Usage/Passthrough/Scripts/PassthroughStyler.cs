using System.Collections;
using UnityEngine;

public class PassthroughStyler : MonoBehaviour
{
    private const float FadeDuration = 0.2f;

    [SerializeField]
    private OVRInput.Controller _controllerHand = OVRInput.Controller.None;

    [SerializeField]
    private OVRPassthroughLayer _passthroughLayer;

    [SerializeField]
    private RectTransform _colorWheel;

    [SerializeField]
    private Texture2D _colorTexture;

    [SerializeField]
    private Texture2D _colorLutTexture;

    [SerializeField]
    private CanvasGroup _mainCanvas;

    [SerializeField]
    private GameObject[] _compactObjects;

    [SerializeField]
    private GameObject[] _objectsToHideForColorPassthrough;

    private Vector3 _cursorPosition = Vector3.zero;
    private bool _settingColor = false;
    private Color _savedColor = Color.white;
    private float _savedBrightness = 0.0f;
    private float _savedContrast = 0.0f;
    private float _savedSaturation = 0.0f;

    private OVRPassthroughLayer.ColorMapEditorType _currentStyle =
        OVRPassthroughLayer.ColorMapEditorType.ColorAdjustment;

    private float _savedBlend = 1;
    private OVRPassthroughColorLut _passthroughColorLut;
    private IEnumerator _fade;

    private void Start()
    {
        if (TryGetComponent<GrabObject>(out var grabOject))
        {
            grabOject.GrabbedObjectDelegate += Grab;
            grabOject.ReleasedObjectDelegate += Release;
            grabOject.CursorPositionDelegate += Cursor;
        }

        _savedColor = new Color(1, 1, 1, 0);
        ShowFullMenu(false);
        _mainCanvas.interactable = false;
        _passthroughColorLut = new OVRPassthroughColorLut(_colorLutTexture);

        if (!OVRManager.GetPassthroughCapabilities().SupportsColorPassthrough)
        {
            if (_objectsToHideForColorPassthrough != null)
            {
                for (int i = 0; i < _objectsToHideForColorPassthrough.Length; i++)
                {
                    _objectsToHideForColorPassthrough[i].SetActive(false);
                }
            }
        }
    }

    private void Update()
    {
        if (_controllerHand == OVRInput.Controller.None)
        {
            return;
        }

        if (_settingColor)
        {
            GetColorFromWheel();
        }
    }

    /// <summary>
    /// Called from editor
    /// </summary>
    public void OnBrightnessChanged(float newValue)
    {
        _savedBrightness = newValue;
        UpdateBrighnessContrastSaturation();
    }

    /// <summary>
    /// Called from editor
    /// </summary>
    public void OnContrastChanged(float newValue)
    {
        _savedContrast = newValue;
        UpdateBrighnessContrastSaturation();
    }

    /// <summary>
    /// Called from editor
    /// </summary>
    public void OnSaturationChanged(float newValue)
    {
        _savedSaturation = newValue;
        UpdateBrighnessContrastSaturation();
    }

    /// <summary>
    /// Called from editor
    /// </summary>
    public void OnAlphaChanged(float newValue)
    {
        _savedColor = new Color(_savedColor.r, _savedColor.g, _savedColor.b, newValue);
        _passthroughLayer.edgeColor = _savedColor;
    }

    /// <summary>
    /// Called from editor
    /// </summary>
    public void OnBlendChange(float newValue)
    {
        _savedBlend = newValue;
        _passthroughLayer.SetColorLut(_passthroughColorLut, _savedBlend);
    }

    /// <summary>
    /// Called from editor
    /// </summary>
    public void DoColorDrag(bool doDrag)
    {
        _settingColor = doDrag;
    }

    /// <summary>
    /// Called from editor
    /// </summary>
    public void SetPassthroughStyleToColorAdjustment(bool isOn)
    {
        if (isOn)
        {
            SetPassthroughStyle(OVRPassthroughLayer.ColorMapEditorType.ColorAdjustment);
        }
    }

    /// <summary>
    /// Called from editor
    /// </summary>
    public void SetPassthroughStyleToColorLut(bool isOn)
    {
        if (isOn)
        {
            SetPassthroughStyle(OVRPassthroughLayer.ColorMapEditorType.ColorLut);
        }
    }

    private void Grab(OVRInput.Controller grabHand)
    {
        _controllerHand = grabHand;
        ShowFullMenu(true);
        if (_mainCanvas) _mainCanvas.interactable = true;

        if (_fade != null) StopCoroutine(_fade);
        _fade = FadeToCurrentStyle(FadeDuration);
        StartCoroutine(_fade);
    }

    private void Release()
    {
        _controllerHand = OVRInput.Controller.None;
        ShowFullMenu(false);
        if (_mainCanvas) _mainCanvas.interactable = false;

        if (_fade != null) StopCoroutine(_fade);
        _fade = FadeToDefaultPassthrough(FadeDuration);
        StartCoroutine(_fade);
    }

    private IEnumerator FadeToCurrentStyle(float fadeTime)
    {
        _passthroughLayer.edgeRenderingEnabled = true;
        yield return FadeTo(1, fadeTime);
    }

    private IEnumerator FadeToDefaultPassthrough(float fadeTime)
    {
        yield return FadeTo(0, fadeTime);
        _passthroughLayer.edgeRenderingEnabled = false;
    }

    private IEnumerator FadeTo(float styleValueMultiplier, float duration)
    {
        float timer = 0.0f;
        float brightness = _passthroughLayer.colorMapEditorBrightness;
        float contrast = _passthroughLayer.colorMapEditorContrast;
        float saturation = _passthroughLayer.colorMapEditorSaturation;
        Color edgeCol = _passthroughLayer.edgeColor;
        float blend = _savedBlend;
        while (timer <= duration)
        {
            timer += Time.deltaTime;
            float normTimer = Mathf.Clamp01(timer / duration);
            if (_currentStyle == OVRPassthroughLayer.ColorMapEditorType.ColorLut)
            {
                _passthroughLayer.SetColorLut(_passthroughColorLut,
                    Mathf.Lerp(blend, _savedBlend * styleValueMultiplier, normTimer));
            }
            else
            {
                _passthroughLayer.SetBrightnessContrastSaturation(
                    Mathf.Lerp(brightness, _savedBrightness * styleValueMultiplier, normTimer),
                    Mathf.Lerp(contrast, _savedContrast * styleValueMultiplier, normTimer),
                    Mathf.Lerp(saturation, _savedSaturation * styleValueMultiplier, normTimer));
            }

            _passthroughLayer.edgeColor = Color.Lerp(edgeCol,
                new Color(_savedColor.r, _savedColor.g, _savedColor.b, _savedColor.a * styleValueMultiplier),
                normTimer);
            yield return null;
        }
    }

    private void UpdateBrighnessContrastSaturation()
    {
        _passthroughLayer.SetBrightnessContrastSaturation(_savedBrightness, _savedContrast, _savedSaturation);
    }

    private void ShowFullMenu(bool doShow)
    {
        foreach (GameObject go in _compactObjects)
        {
            go.SetActive(doShow);
        }
    }

    private void Cursor(Vector3 cP)
    {
        _cursorPosition = cP;
    }

    private void GetColorFromWheel()
    {
        // convert cursor world position to UV
        var localPos = _colorWheel.transform.InverseTransformPoint(_cursorPosition);
        var toImg = new Vector2(localPos.x / _colorWheel.sizeDelta.x + 0.5f,
            localPos.y / _colorWheel.sizeDelta.y + 0.5f);
        Debug.Log("Sanctuary: " + toImg.x.ToString() + ", " + toImg.y.ToString());
        Color sampledColor = Color.black;
        if (toImg.x < 1.0 && toImg.x > 0.0f && toImg.y < 1.0 && toImg.y > 0.0f)
        {
            int Upos = Mathf.RoundToInt(toImg.x * _colorTexture.width);
            int Vpos = Mathf.RoundToInt(toImg.y * _colorTexture.height);
            sampledColor = _colorTexture.GetPixel(Upos, Vpos);
        }

        _savedColor = new Color(sampledColor.r, sampledColor.g, sampledColor.b, _savedColor.a);
        _passthroughLayer.edgeColor = _savedColor;
    }

    private void SetPassthroughStyle(OVRPassthroughLayer.ColorMapEditorType passthroughStyle)
    {
        _currentStyle = passthroughStyle;
        if (_currentStyle == OVRPassthroughLayer.ColorMapEditorType.ColorLut)
        {
            _passthroughLayer.SetColorLut(_passthroughColorLut, _savedBlend);
        }
        else
        {
            UpdateBrighnessContrastSaturation();
        }
    }
}
