using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("XP & Stats Display")]
    [SerializeField] private TextMeshProUGUI totalXPText;
    [SerializeField] private TextMeshProUGUI scenariosCompletedText;

    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private void Start()
    {
        ShowMainPanel();
        UpdateStatsDisplay();

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
    }

    private void OnEnable()
    {
        UpdateStatsDisplay();
    }

    public void UpdateStatsDisplay()
    {
        if (ScoreTracker.Instance != null)
        {
            if (totalXPText != null) 
                totalXPText.text = $"Total XP: {ScoreTracker.Instance.TotalXP}";
            if (scenariosCompletedText != null) 
                scenariosCompletedText.text = $"Completed: {ScoreTracker.Instance.ScenariosPlayed}";
        }
        else
        {
            if (totalXPText != null) totalXPText.text = "Total XP: 0";
            if (scenariosCompletedText != null) scenariosCompletedText.text = "Completed: 0";
        }
    }

    public void OnPlayClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadScenarioSelection();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("ScenarioSelection");
        }
    }

    public void OnScenarioSelectClicked()
    {
        OnPlayClicked();
    }

    public void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void ShowSettingsPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void ShowCreditsPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    public void OnQuitClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
        else
        {
            Application.Quit();
        }
    }
}
