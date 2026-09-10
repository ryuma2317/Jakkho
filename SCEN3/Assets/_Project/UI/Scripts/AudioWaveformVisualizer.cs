using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioWaveformVisualizer : MonoBehaviour
{
    [Header("Waveform Settings")]
    [SerializeField] private int barCount = 14;
    [SerializeField] private float minHeight = 4f;
    [SerializeField] private float maxHeight = 48f;
    [SerializeField] private float animSpeed = 9f;
    [SerializeField] private Color activeColor = new Color(0.25f, 0.90f, 0.55f, 1f); // Vibrant emerald
    [SerializeField] private Color idleColor = new Color(0.40f, 0.50f, 0.60f, 0.6f);   // Muted slate

    [Header("UI References")]
    [SerializeField] private Transform barsContainer;
    [SerializeField] private TextMeshProUGUI statusLabel;

    private RectTransform[] barTransforms;
    private Image[] barImages;
    private float[] targetHeights;
    private bool isCallActive = false;
    private float timeOffset = 0f;

    private void Awake()
    {
        InitializeBars();
    }

    private void InitializeBars()
    {
        if (barsContainer == null) barsContainer = transform;

        // Clean up any existing children
        for (int i = barsContainer.childCount - 1; i >= 0; i--)
        {
            var child = barsContainer.GetChild(i);
            if (child.name.StartsWith("WaveBar_"))
            {
                DestroyImmediate(child.gameObject);
            }
        }

        barTransforms = new RectTransform[barCount];
        barImages = new Image[barCount];
        targetHeights = new float[barCount];

        float containerWidth = 260f;
        float barWidth = 10f;
        float spacing = (containerWidth - (barCount * barWidth)) / (barCount - 1);

        for (int i = 0; i < barCount; i++)
        {
            GameObject barObj = new GameObject($"WaveBar_{i}");
            barObj.transform.SetParent(barsContainer, false);

            RectTransform rt = barObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float xPos = -(containerWidth * 0.5f) + (i * (barWidth + spacing)) + (barWidth * 0.5f);
            rt.sizeDelta = new Vector2(barWidth, minHeight);
            rt.anchoredPosition = new Vector2(xPos, 0f);

            Image img = barObj.AddComponent<Image>();
            img.color = idleColor;

            barTransforms[i] = rt;
            barImages[i] = img;
            targetHeights[i] = minHeight;
        }
    }

    public void SetCallActive(bool active, string statusText = "")
    {
        isCallActive = active;
        if (statusLabel != null && !string.IsNullOrEmpty(statusText))
        {
            statusLabel.text = statusText;
        }

        Color targetCol = active ? activeColor : idleColor;
        if (barImages != null)
        {
            foreach (var img in barImages)
            {
                if (img != null) img.color = targetCol;
            }
        }
    }

    private void Update()
    {
        if (barTransforms == null || barTransforms.Length == 0) return;

        timeOffset += Time.deltaTime * animSpeed;

        for (int i = 0; i < barCount; i++)
        {
            if (barTransforms[i] == null) continue;

            if (isCallActive)
            {
                // Multi-frequency harmonic wave simulation mimicking voice cadence
                float wave1 = Mathf.Sin(timeOffset + (i * 0.45f));
                float wave2 = Mathf.Cos((timeOffset * 1.6f) + (i * 0.8f));
                float wave3 = Mathf.Sin((timeOffset * 0.7f) - (i * 0.3f));
                float combined = Mathf.Clamp01((wave1 + wave2 + wave3 + 1.5f) / 3.0f);

                // Add center weighting (human voice peak in middle frequencies)
                float centerWeight = 1.0f - Mathf.Abs((i - (barCount * 0.5f)) / (barCount * 0.5f)) * 0.35f;
                targetHeights[i] = Mathf.Lerp(minHeight, maxHeight * centerWeight, combined);
            }
            else
            {
                targetHeights[i] = minHeight;
            }

            Vector2 sz = barTransforms[i].sizeDelta;
            sz.y = Mathf.Lerp(sz.y, targetHeights[i], Time.deltaTime * 18f);
            barTransforms[i].sizeDelta = sz;
        }
    }
}
