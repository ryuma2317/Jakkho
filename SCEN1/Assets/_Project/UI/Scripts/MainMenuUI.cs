using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CADT.VR.Core;

namespace CADT.VR.UI
{
    /// <summary>
    /// Manages the Main Menu UI interactions, scenario launching,
    /// settings modal, and persistent player XP stats.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuUI : MonoBehaviour
    {
        [Header("--- Buttons ---")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button scenarioSelectButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("--- Profile Display ---")]
        [SerializeField] private TextMeshProUGUI playerNameTMP;
        [SerializeField] private TextMeshProUGUI totalXpTMP;

        [Header("--- Settings Panel ---")]
        [SerializeField] private GameObject settingsModal;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Button closeSettingsButton;

        private void Start()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (scenarioSelectButton != null) scenarioSelectButton.onClick.AddListener(OnScenarioSelectClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                if (GameManager.Instance != null) masterVolumeSlider.value = GameManager.Instance.MasterVolume;
            }

            UpdateProfileDisplay();
        }

        public void UpdateProfileDisplay()
        {
            if (GameManager.Instance != null)
            {
                if (playerNameTMP != null) playerNameTMP.text = GameManager.Instance.PlayerName;
                if (totalXpTMP != null) totalXpTMP.text = $"{GameManager.Instance.TotalXP} XP";
            }
        }

        private void OnStartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScenario("Scenario1");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Scenario1");
            }
        }

        private void OnScenarioSelectClicked()
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

        private void OpenSettings()
        {
            if (settingsModal != null) settingsModal.SetActive(true);
        }

        private void CloseSettings()
        {
            if (settingsModal != null) settingsModal.SetActive(false);
        }

        private void OnVolumeChanged(float val)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetVolumes(val, val, val);
            }
            else
            {
                AudioListener.volume = val;
            }
        }

        private void OnExitClicked()
        {
            Application.Quit();
        }
    }
}
