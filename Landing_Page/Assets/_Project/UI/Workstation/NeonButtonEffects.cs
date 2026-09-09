using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to any World Space neon button for:
/// - Idle emissive pulse
/// - Hover: stacked glowing outline layers (3D depth card effect)
/// - Hover: scale + Z-shift (works in conjunction with HoverScaler)
/// </summary>
[RequireComponent(typeof(Image))]
public class NeonButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Idle Pulse")]
    public Color baseColor      = new Color(1f, 0.18f, 0.47f, 1f); // neon pink
    public float pulseSpeed     = 1.4f;
    public float pulseMinAlpha  = 0.55f;
    public float pulseMaxAlpha  = 1.00f;

    [Header("Hover Stack")]
    public int   stackLayers    = 3;
    public float stackOffset    = 6f;    // pixels per layer in UI space
    public float stackFadeDuration = 0.18f;
    public Color stackColor     = new Color(1f, 0.18f, 0.47f, 0.28f);

    private Image        _image;
    private bool         _hovered;
    private GameObject[] _stackClones;
    private Coroutine    _stackRoutine;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.color = baseColor;
    }

    private void Update()
    {
        // Idle alpha pulse
        if (!_hovered)
        {
            float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha,
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            _image.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
        else
        {
            _image.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        if (_stackRoutine != null) StopCoroutine(_stackRoutine);
        _stackRoutine = StartCoroutine(ShowStack(true));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        if (_stackRoutine != null) StopCoroutine(_stackRoutine);
        _stackRoutine = StartCoroutine(ShowStack(false));
    }

    private IEnumerator ShowStack(bool show)
    {
        // Create clones on first hover
        if (_stackClones == null)
        {
            _stackClones = new GameObject[stackLayers];
            for (int i = 0; i < stackLayers; i++)
            {
                GameObject clone = Instantiate(gameObject, transform.parent);
                clone.name = $"{gameObject.name}_StackLayer_{i + 1}";

                // Remove this script from clone to avoid recursion
                Destroy(clone.GetComponent<NeonButtonEffects>());
                Destroy(clone.GetComponent<HoverScaler>());
                Destroy(clone.GetComponent<Button>());

                Image cloneImg = clone.GetComponent<Image>();
                if (cloneImg != null) cloneImg.color = new Color(stackColor.r, stackColor.g, stackColor.b, 0f);

                RectTransform cloneRT = clone.GetComponent<RectTransform>();
                RectTransform srcRT   = GetComponent<RectTransform>();
                cloneRT.sizeDelta = srcRT.sizeDelta;

                // Offset behind (positive Z = away from camera in world space canvas)
                Vector3 localOff = new Vector3(stackOffset * (i + 1) * 0.5f,
                                              -stackOffset * (i + 1) * 0.5f,
                                               stackOffset * (i + 1));
                cloneRT.localPosition = srcRT.localPosition + localOff;
                clone.transform.SetSiblingIndex(transform.GetSiblingIndex()); // behind original

                _stackClones[i] = clone;
            }
        }

        float elapsed = 0f;
        float[] startAlphas = new float[stackLayers];
        float[] targetAlphas = new float[stackLayers];

        for (int i = 0; i < stackLayers; i++)
        {
            Image img = _stackClones[i]?.GetComponent<Image>();
            if (img == null) continue;
            startAlphas[i]  = img.color.a;
            targetAlphas[i] = show ? stackColor.a * (1f - i * 0.25f) : 0f;
        }

        while (elapsed < stackFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / stackFadeDuration);
            for (int i = 0; i < stackLayers; i++)
            {
                Image img = _stackClones[i]?.GetComponent<Image>();
                if (img == null) continue;
                float a = Mathf.Lerp(startAlphas[i], targetAlphas[i], t);
                img.color = new Color(stackColor.r, stackColor.g, stackColor.b, a);
            }
            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (_stackClones == null) return;
        foreach (var c in _stackClones)
            if (c != null) Destroy(c);
    }
}
