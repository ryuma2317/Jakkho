using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int SelectedScenario { get; private set; }
    public bool TutorialComplete { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectScenario(int scenarioNumber)
    {
        SelectedScenario = scenarioNumber;
        Debug.Log($"[GameManager] Scenario {scenarioNumber} selected.");
    }

    public void SetTutorialComplete() { TutorialComplete = true; }

    public void LoadScenario()
    {
        switch (SelectedScenario)
        {
            case 1: SceneManager.LoadScene("Scenario1"); break;
            case 2: SceneManager.LoadScene("Scenario2"); break;
            case 3: SceneManager.LoadScene("Scenario3"); break;
            default: Debug.LogError("[GameManager] Invalid scenario number!"); break;
        }
    }

    public bool openScenarioSelectionDirectly = false;

    public void LoadMainMenu()
    {
        openScenarioSelectionDirectly = true;
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadScenarioSelection()
    {
        openScenarioSelectionDirectly = true;
        SceneManager.LoadScene("MainMenu");
    }
    public void CompleteScenario(int xpEarned = 0) { Debug.Log($"[GameManager] Scenario completed with {xpEarned} XP"); }
    public void QuitGame() { Application.Quit(); }
}
