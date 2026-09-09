using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class CharacterCard : MonoBehaviour, IPointerClickHandler
{
    [Header("Card Data")]
    public string characterName = "MOE ROCKA";
    public string characterSubtitle = "THE MIGHTY";
    public Color accentColor = new Color(0.2f, 0.6f, 1.0f);

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Image glowingBorder;
    [SerializeField] private Image portraitImage;

    private void Start()
    {
        UpdateUI();
    }

    public void SetCharacterData(string cName, string cSubtitle, Color color, Sprite portrait = null)
    {
        characterName = cName;
        characterSubtitle = cSubtitle;
        accentColor = color;

        if (portrait != null && portraitImage != null)
        {
            portraitImage.sprite = portrait;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (nameText == null) nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        if (subtitleText == null) subtitleText = transform.Find("SubtitleText")?.GetComponent<TextMeshProUGUI>();
        if (glowingBorder == null) glowingBorder = transform.Find("GlowingBorder")?.GetComponent<Image>();
        if (portraitImage == null) portraitImage = transform.Find("GlowingBorder/PortraitImage")?.GetComponent<Image>();

        if (nameText != null) nameText.text = characterName;
        if (subtitleText != null) subtitleText.text = characterSubtitle;
        if (glowingBorder != null) glowingBorder.color = accentColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OnCharacterSelected(characterName);
        }
        else
        {
            Debug.Log($"[Character Select] Card Clicked: {characterName} ({characterSubtitle})");
        }
    }
}
