using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CarouselController : MonoBehaviour
{
    [Serializable]
    public class ScenarioData
    {
        public int scenarioNumber;
        public string title;
        [TextArea(2, 4)]
        public string description;
        public string setting;
        public string threatType;
        public int maxXP = 200;
        public Sprite thumbnail;
        public bool isAvailable = true;
    }

    [Header("Scenario Registry")]
    [SerializeField] private List<ScenarioData> scenarios = new List<ScenarioData>()
    {
        new ScenarioData
        {
            scenarioNumber = 1,
            title = "Scenario 1: Office Spear Phishing",
            description = "Analyze suspicious executive emails, verify attachments, and spot spoofed sender domains in a corporate office.",
            setting = "Corporate Office",
            threatType = "Email Phishing & Malware",
            maxXP = 200,
            isAvailable = true
        },
        new ScenarioData
        {
            scenarioNumber = 2,
            title = "Scenario 2: Evil Twin at the Cafe",
            description = "Detect rogue public Wi-Fi access points and SSL stripping attacks before entering sensitive credentials.",
            setting = "Public Coffee Shop",
            threatType = "Wi-Fi Man-in-the-Middle",
            maxXP = 200,
            isAvailable = true
        },
        new ScenarioData
        {
            scenarioNumber = 3,
            title = "Scenario 3: The Fake Link Hijacking",
            description = "A friend's hacked Telegram sends an urgent survey link for their NextGen project. Inspect the URL, refuse QR device linking, and enable 2FA.",
            setting = "Home Study Room",
            threatType = "Session Hijacking & QR Phishing",
            maxXP = 200,
            isAvailable = true
        }
    };

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI settingText;
    [SerializeField] private TextMeshProUGUI threatTypeText;
    [SerializeField] private TextMeshProUGUI maxXPText;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Button startScenarioButton;
    [SerializeField] private TextMeshProUGUI startButtonText;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("3D Carousel Nodes (Optional)")]
    [SerializeField] private Transform[] carouselCardAnchors;
    [SerializeField] private float transitionSpeed = 8f;

    private int currentIndex = 2; // Default to Scenario 3 for Vanmonireak's development

    private void Start()
    {
        if (prevButton != null) prevButton.onClick.AddListener(PreviousScenario);
        if (nextButton != null) nextButton.onClick.AddListener(NextScenario);
        if (startScenarioButton != null) startScenarioButton.onClick.AddListener(LaunchSelectedScenario);

        UpdateUI();
    }

    public void NextScenario()
    {
        if (scenarios.Count == 0) return;
        currentIndex = (currentIndex + 1) % scenarios.Count;
        UpdateUI();
    }

    public void PreviousScenario()
    {
        if (scenarios.Count == 0) return;
        currentIndex = (currentIndex - 1 + scenarios.Count) % scenarios.Count;
        UpdateUI();
    }

    public void SelectScenarioIndex(int index)
    {
        if (index >= 0 && index < scenarios.Count)
        {
            currentIndex = index;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (scenarios.Count == 0) return;

        ScenarioData current = scenarios[currentIndex];

        if (titleText != null) titleText.text = current.title;
        if (descriptionText != null) descriptionText.text = current.description;
        if (settingText != null) settingText.text = $"Location: {current.setting}";
        if (threatTypeText != null) threatTypeText.text = $"Threat: {current.threatType}";
        if (maxXPText != null) maxXPText.text = $"Max Reward: +{current.maxXP} XP";
        if (pageIndicatorText != null) pageIndicatorText.text = $"{currentIndex + 1} / {scenarios.Count}";

        if (thumbnailImage != null)
        {
            if (current.thumbnail != null)
            {
                thumbnailImage.sprite = current.thumbnail;
                thumbnailImage.gameObject.SetActive(true);
            }
            else
            {
                thumbnailImage.gameObject.SetActive(false);
            }
        }

        if (startScenarioButton != null)
        {
            startScenarioButton.interactable = current.isAvailable;
            if (startButtonText != null)
            {
                startButtonText.text = current.isAvailable ? "START SCENARIO" : "COMING SOON";
            }
        }
    }

    public void LaunchSelectedScenario()
    {
        if (scenarios.Count == 0) return;

        ScenarioData selected = scenarios[currentIndex];
        if (!selected.isAvailable)
        {
            Debug.LogWarning($"[CarouselController] Scenario {selected.scenarioNumber} is locked / in development.");
            return;
        }

        Debug.Log($"[CarouselController] Launching Scenario {selected.scenarioNumber}: {selected.title}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SelectScenario(selected.scenarioNumber);
            GameManager.Instance.LoadScenario();
        }
        else
        {
            // Fallback direct load
            string sceneName = selected.scenarioNumber switch
            {
                1 => "Scenario1",
                2 => "Scenario2",
                3 => "Scenario3",
                _ => "MainMenu"
            };
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

    public void BackToMainMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
