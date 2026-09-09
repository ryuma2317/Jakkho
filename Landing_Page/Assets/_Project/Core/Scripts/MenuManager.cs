using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("Layer Panels")]
    [SerializeField] private CanvasGroup mainMenuPanel;
    [SerializeField] private CanvasGroup characterSelectPanel;

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.3f;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button settingsButton; // optional

    private Coroutine transitionCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (mainMenuPanel      == null) mainMenuPanel      = GameObject.Find("MainMenuPanel")?.GetComponent<CanvasGroup>();
        if (characterSelectPanel==null) characterSelectPanel= GameObject.Find("CharacterSelectPanel")?.GetComponent<CanvasGroup>();
        if (playButton         == null) playButton         = GameObject.Find("PlayButton")?.GetComponent<Button>();
        if (exitButton         == null) exitButton         = GameObject.Find("ExitButton")?.GetComponent<Button>();
        if (backButton         == null) backButton         = GameObject.Find("BackButton")?.GetComponent<Button>();
        if (settingsButton     == null) settingsButton     = GameObject.Find("SettingsButton")?.GetComponent<Button>();

        BindButtonListeners();
        SetCanvasGroupState(mainMenuPanel, true);
        SetCanvasGroupState(characterSelectPanel, false);
    }

    // Original signature preserved (backward compatible)
    public void SetupReferences(CanvasGroup mainPanel, CanvasGroup charPanel,
        Button playBtn, Button exitBtn, Button backBtn, Button settingsBtn = null)
    {
        mainMenuPanel        = mainPanel;
        characterSelectPanel = charPanel;
        playButton           = playBtn;
        exitButton           = exitBtn;
        backButton           = backBtn;
        settingsButton       = settingsBtn;
        BindButtonListeners();
    }

    private void BindButtonListeners()
    {
        if (playButton     != null) { playButton.onClick.RemoveListener(OnPlayClicked);     playButton.onClick.AddListener(OnPlayClicked); }
        if (exitButton     != null) { exitButton.onClick.RemoveListener(OnExitClicked);     exitButton.onClick.AddListener(OnExitClicked); }
        if (backButton     != null) { backButton.onClick.RemoveListener(OnBackClicked);     backButton.onClick.AddListener(OnBackClicked); }
        if (settingsButton != null) { settingsButton.onClick.RemoveListener(OnSettingsClicked); settingsButton.onClick.AddListener(OnSettingsClicked); }
    }

    public void OnPlayClicked()
    {
        Debug.Log("[MenuManager] PLAY / NEW GAME Button Clicked.");

        // Try ScenarioSelectCanvas first
        GameObject scenarioCanvas = GameObject.Find("ScenarioSelectCanvas");
        if (scenarioCanvas != null)
        {
            scenarioCanvas.SetActive(true);
            scenarioCanvas.transform.Find("ScenarioSelectPanel")?.gameObject.SetActive(true);
            if (mainMenuPanel != null) SetCanvasGroupState(mainMenuPanel, false);
            return;
        }

        // Fallback: Character Select Panel
        if (characterSelectPanel != null)
            TransitionPanels(mainMenuPanel, characterSelectPanel);
    }

    public void OnBackClicked()
    {
        Debug.Log("[MenuManager] BACK Button Clicked.");
        TransitionPanels(characterSelectPanel, mainMenuPanel);
    }

    public void OnExitClicked()
    {
        Debug.Log("[MenuManager] EXIT Button Clicked.");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnSettingsClicked()
    {
        Debug.Log("[MenuManager] SETTINGS Button Clicked. (Hook your settings panel here.)");
    }

    public void OnCharacterSelected(string characterName)
    {
        Debug.Log($"[Character Select] Selected: {characterName}");
    }

    private void TransitionPanels(CanvasGroup fromPanel, CanvasGroup toPanel)
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(FadeTransitionRoutine(fromPanel, toPanel));
    }

    private IEnumerator FadeTransitionRoutine(CanvasGroup fromPanel, CanvasGroup toPanel)
    {
        toPanel.gameObject.SetActive(true);
        toPanel.interactable = false;
        toPanel.blocksRaycasts = false;
        fromPanel.interactable = false;
        fromPanel.blocksRaycasts = false;

        float elapsed = 0f;
        float fromStart = fromPanel.alpha;
        float toStart   = toPanel.alpha;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            fromPanel.alpha = Mathf.Lerp(fromStart, 0f, t);
            toPanel.alpha   = Mathf.Lerp(toStart,   1f, t);
            yield return null;
        }

        SetCanvasGroupState(fromPanel, false);
        SetCanvasGroupState(toPanel, true);
    }

    private void SetCanvasGroupState(CanvasGroup group, bool active)
    {
        if (group == null) return;
        group.alpha = active ? 1f : 0f;
        group.interactable = active;
        group.blocksRaycasts = active;
        group.gameObject.SetActive(active);
    }
}
