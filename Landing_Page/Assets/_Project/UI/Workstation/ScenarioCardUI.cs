using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScenarioCardUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TextMeshProUGUI riskBadgeText;
    [SerializeField] private Transform tagRow;
    [SerializeField] private Button playButton;
    [SerializeField] private TextMeshProUGUI playButtonText;

    private ScenarioData currentData;
    private Action<ScenarioData> onPlayCallback;

    public void Bind(ScenarioData data, Action<ScenarioData> onPlay)
    {
        currentData = data;
        onPlayCallback = onPlay;

        // Auto-find references if unassigned
        if (titleText == null) titleText = transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        if (descriptionText == null) descriptionText = transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
        if (thumbnailImage == null) thumbnailImage = transform.Find("ThumbnailImage")?.GetComponent<Image>();
        if (riskBadgeText == null) riskBadgeText = transform.Find("RiskBadge/Text")?.GetComponent<TextMeshProUGUI>();
        if (tagRow == null) tagRow = transform.Find("TagRow");
        if (playButton == null) playButton = transform.Find("PlayButton")?.GetComponent<Button>();
        if (playButtonText == null) playButtonText = transform.Find("PlayButton/Text")?.GetComponent<TextMeshProUGUI>();

        if (data != null)
        {
            if (titleText != null) titleText.text = data.title;
            if (descriptionText != null) descriptionText.text = data.description;
            if (thumbnailImage != null && data.thumbnail != null)
            {
                thumbnailImage.sprite = data.thumbnail;
                thumbnailImage.color = Color.white;
            }
            if (riskBadgeText != null) riskBadgeText.text = "⚠ Risk";
            if (playButtonText != null) playButtonText.text = "▶ Play Scenario";

            // Generate Tag Pills
            PopulateTags(data.tags);
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
    }

    private void PopulateTags(string[] tags)
    {
        if (tagRow == null) return;

        // Clear existing children
        for (int i = tagRow.childCount - 1; i >= 0; i--)
        {
            Destroy(tagRow.GetChild(i).gameObject);
        }

        if (tags == null) return;

        foreach (string tag in tags)
        {
            CreateTagPill(tag);
        }
    }

    private void CreateTagPill(string tagTextStr)
    {
        GameObject pillGO = new GameObject($"Tag_{tagTextStr}");
        pillGO.transform.SetParent(tagRow, false);

        RectTransform rect = pillGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 32);

        Image pillBg = pillGO.AddComponent<Image>();
        pillBg.color = new Color(0.2f, 0.28f, 0.42f, 0.85f);
        pillBg.raycastTarget = false;

        HorizontalLayoutGroup hGroup = pillGO.AddComponent<HorizontalLayoutGroup>();
        hGroup.padding = new RectOffset(10, 10, 4, 4);
        hGroup.childAlignment = TextAnchor.MiddleCenter;

        ContentSizeFitter fitter = pillGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(pillGO.transform, false);

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = tagTextStr;
        tmp.fontSize = 13;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.85f, 0.92f, 1.0f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private void OnPlayButtonClicked()
    {
        onPlayCallback?.Invoke(currentData);
    }
}
