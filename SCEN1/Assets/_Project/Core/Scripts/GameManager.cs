using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CADT.VR.Core
{
    /// <summary>
    /// Global Persistent Manager coordinating VR game state, XP tracking,
    /// and seamless scene transitions between Main Menu, Scenario Selection, and Scenarios.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("--- Player Profile ---")]
        [SerializeField] private string playerName = "Cyber Analyst";
        [SerializeField] private int totalXP = 0;
        [SerializeField] private int completedScenariosCount = 0;

        [Header("--- Current Session ---")]
        [SerializeField] private ScenarioDataSO currentScenarioData;
        [SerializeField] private string currentSceneName;

        [Header("--- Global Audio ---")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1.0f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1.0f;
        [Range(0f, 1f)]
        [SerializeField] private float voiceVolume = 1.0f;

        public event Action<int> OnXPChanged;
        public event Action<string> OnSceneLoaded;

        public string PlayerName => playerName;
        public int TotalXP => totalXP;
        public int CompletedScenariosCount => completedScenariosCount;
        public ScenarioDataSO CurrentScenarioData => currentScenarioData;
        public float MasterVolume => masterVolume;
        public float SFXVolume => sfxVolume;
        public float VoiceVolume => voiceVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            currentSceneName = scene.name;
            OnSceneLoaded?.Invoke(scene.name);
        }

        public void AddXP(int amount)
        {
            totalXP = Mathf.Max(0, totalXP + amount);
            OnXPChanged?.Invoke(totalXP);
        }

        public void SetCurrentScenario(ScenarioDataSO data)
        {
            currentScenarioData = data;
        }

        public void LoadScenario(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public void LoadMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void LoadScenarioSelection()
        {
            SceneManager.LoadScene("ScenarioSelection");
        }

        public void CompleteScenario(int xpEarned)
        {
            AddXP(xpEarned);
            completedScenariosCount++;
        }

        public void SetVolumes(float master, float sfx, float voice)
        {
            masterVolume = master;
            sfxVolume = sfx;
            voiceVolume = voice;
            AudioListener.volume = masterVolume;
        }
    }
}
