using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScenarioSelectManager : MonoBehaviour
{
    [Header("Scenario Data & Prefab")]
    public ScenarioData[] scenarios;
    public GameObject scenarioCardPrefab;
    public Transform cardContainer;

    [Header("Navigation Buttons")]
    public Button backToMenuButton;

    private void Start()
    {
        // Auto-find references if unassigned
        if (cardContainer == null) cardContainer = transform.Find("ScenarioSelectPanel/CardsContainer");
        if (cardContainer == null) cardContainer = transform.Find("CardsContainer");
        if (backToMenuButton == null) backToMenuButton = transform.Find("ScenarioSelectPanel/BackButton")?.GetComponent<Button>();
        if (backToMenuButton == null) backToMenuButton = transform.Find("BackButton")?.GetComponent<Button>();

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }

        PopulateScenarioCards();

        // Ensure Main Menu is shown first by default if present in the scene
        GameObject mainMenuCanvas = GameObject.Find("MainMenuCanvas");
        if (mainMenuCanvas != null)
        {
            Transform scenarioPanelTrans = transform.Find("ScenarioSelectPanel");
            if (scenarioPanelTrans != null)
            {
                scenarioPanelTrans.gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void PopulateScenarioCards()
    {
        if (cardContainer == null || scenarioCardPrefab == null || scenarios == null) return;

        // Clear existing children
        for (int i = cardContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(cardContainer.GetChild(i).gameObject);
        }

        foreach (ScenarioData data in scenarios)
        {
            if (data == null) continue;

            GameObject cardGO = Instantiate(scenarioCardPrefab, cardContainer);
            cardGO.name = $"Card_{data.name}";

            ScenarioCardUI cardUI = cardGO.GetComponent<ScenarioCardUI>();
            if (cardUI == null)
            {
                cardUI = cardGO.AddComponent<ScenarioCardUI>();
            }

            cardUI.Bind(data, OnScenarioSelected);
        }
    }

    private void OnScenarioSelected(ScenarioData selectedData)
    {
        Debug.Log($"<b>[ScenarioSelectManager]</b> Selected Scenario: '{selectedData.title}' -> Loading Scene: '{selectedData.sceneToLoad}'");

        // 1. Store selected scenario number in GameManager singleton (if parseable from its name)
        if (GameManager.Instance != null)
        {
            string digits = "";
            foreach (char ch in selectedData.name)
            {
                if (char.IsDigit(ch)) digits += ch;
            }
            int scenarioNumber;
            if (int.TryParse(digits, out scenarioNumber))
            {
                GameManager.Instance.SelectScenario(scenarioNumber);
            }
        }

        // 2. Load the scenario's scene if scene exists in build
        if (!string.IsNullOrEmpty(selectedData.sceneToLoad))
        {
            if (Application.CanStreamedLevelBeLoaded(selectedData.sceneToLoad))
            {
                SceneManager.LoadScene(selectedData.sceneToLoad);
            }
            else
            {
                Debug.LogWarning($"[ScenarioSelectManager] Scene '{selectedData.sceneToLoad}' is not in Build Settings yet.");
            }
        }
    }

    public void OnBackToMenuClicked()
    {
        Debug.Log("[ScenarioSelectManager] Navigating Back to Main Menu...");

        // If MainMenuCanvas is present in the current scene, seamlessly switch panels
        GameObject mainMenuCanvas = GameObject.Find("MainMenuCanvas");
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(true);

            // Enable MainMenuPanel
            Transform mainPanelTrans = mainMenuCanvas.transform.Find("MainMenuPanel");
            if (mainPanelTrans != null)
            {
                CanvasGroup mainCG = mainPanelTrans.GetComponent<CanvasGroup>();
                if (mainCG != null)
                {
                    mainCG.alpha = 1f;
                    mainCG.interactable = true;
                    mainCG.blocksRaycasts = true;
                    mainPanelTrans.gameObject.SetActive(true);
                }
            }

            // Hide ScenarioSelectPanel
            Transform scenarioPanelTrans = transform.Find("ScenarioSelectPanel");
            if (scenarioPanelTrans != null)
            {
                scenarioPanelTrans.gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
            return;
        }

        // Otherwise load MainMenu scene if scene exists in build settings
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            Debug.LogWarning("[ScenarioSelectManager] Scene 'MainMenu' is not in Build Settings.");
        }
    }
}
