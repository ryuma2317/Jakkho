using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

public class ScenarioUIOverlay : MonoBehaviour
{
    [Header("Scene Names")]
    public string scenarioSelectionScene = "MainMenu";
    public string mainMenuScene = "MainMenu";

    [Header("Style")]
    public TMP_FontAsset font;
    public Color panelColor = new Color(0.06f, 0.08f, 0.13f, 0.97f);
    public Color outlineColor = new Color(0.35f, 0.8f, 1f, 0.65f);
    public Color titleColor = new Color(0.35f, 0.8f, 1f, 1f);
    public Color bodyColor = new Color(0.9f, 0.93f, 0.97f, 1f);
    public Color positiveColor = new Color(0.18f, 0.6f, 0.38f, 1f);
    public Color neutralColor = new Color(0.28f, 0.36f, 0.48f, 1f);
    public Color dangerColor = new Color(0.65f, 0.22f, 0.22f, 1f);
    public Color exitButtonColor = new Color(0.28f, 0.12f, 0.14f, 0.85f);

    [Header("Behaviour")]
    [Tooltip("Whether to build and display the floating ScenarioEndPanel (disabled when rich Lessons Learned panel is used)")]
    public bool enableEndPanel = false;
    public float endPanelDelay = 1.0f;

    private GameObject confirmPanel;
    private GameObject endPanel;

    private const float CanvasDistance = 1.3f;
    private const float CanvasScale = 0.0011f;

    private void Awake()
    {
        if (font == null)
        {
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        var canvasGO = BuildCanvas();
        BuildExitButton(canvasGO.transform);
        confirmPanel = BuildConfirmPanel(canvasGO.transform);
        if (enableEndPanel)
        {
            endPanel = BuildEndPanel(canvasGO.transform);
            endPanel.SetActive(false);
        }
        confirmPanel.SetActive(false);
    }

    private void Start()
    {
        HookScenarioEvents();
    }

    private void HookScenarioEvents()
    {
        var sm = UnityEngine.Object.FindFirstObjectByType<ScenarioManager>();
        if (sm != null)
        {
            sm.onScenarioEnded.AddListener(OnScenarioEndedWithOutcome);
        }

        var t = Type.GetType("Scenario2_EvilTwinManager");
        if (t != null)
        {
            var comp = UnityEngine.Object.FindFirstObjectByType(t) as MonoBehaviour;
            if (comp != null)
            {
                var f = t.GetField("OnScenarioTimeExpired");
                var ev = f != null ? f.GetValue(comp) as UnityEvent : null;
                if (ev != null) ev.AddListener(OnScenarioEnded);
            }
        }
    }

    private void OnScenarioEndedWithOutcome(ScenarioManager.OutcomeType outcome, int xp)
    {
        OnScenarioEnded();
    }

    public void OnScenarioEnded()
    {
        StartCoroutine(ShowEndPanelDelayed());
    }

    private IEnumerator ShowEndPanelDelayed()
    {
        if (!enableEndPanel || endPanel == null) yield break;
        if (endPanelDelay > 0f) yield return new WaitForSeconds(endPanelDelay);
        if (endPanel != null) endPanel.SetActive(true);
    }

    public void ShowConfirmPanel() { if (confirmPanel != null) confirmPanel.SetActive(true); }
    public void HideConfirmPanel() { if (confirmPanel != null) confirmPanel.SetActive(false); }
    public void ShowEndPanel() { if (enableEndPanel && endPanel != null) endPanel.SetActive(true); }

    public void ExitToScenarioSelection()
    {
        VRSetup.VRCardNavigationManager.navigateToScenarioSelectionDirectly = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadScenarioSelection();
        }
        else
        {
            SceneManager.LoadScene(mainMenuScene);
        }
    }

    public void GoToMainMenu()
    {
        VRSetup.VRCardNavigationManager.navigateToScenarioSelectionDirectly = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadScenarioSelection();
        }
        else
        {
            SceneManager.LoadScene(mainMenuScene);
        }
    }

    public void ReplayScenario()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private GameObject BuildCanvas()
    {
        var canvasGO = new GameObject("ScenarioUI_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1000f, 640f);
        rt.localScale = Vector3.one * CanvasScale;
        canvasGO.AddComponent<GraphicRaycaster>();
        var tracked = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (tracked != null) canvasGO.AddComponent(tracked);

        var cam = Camera.main;
        Transform parent = cam != null ? cam.transform : null;
        canvasGO.transform.SetParent(parent, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0f, CanvasDistance);
        canvasGO.transform.localRotation = Quaternion.identity;
        return canvasGO;
    }

    private void BuildExitButton(Transform parent)
    {
        var btnGO = CreatePanel("ExitButton", parent, exitButtonColor, outlineColor);
        SetRect(btnGO, new Vector2(0.855f, 0.88f), new Vector2(0.985f, 0.975f));
        var btn = btnGO.AddComponent<Button>();
        var img = btnGO.GetComponent<Image>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(exitButtonColor.r + 0.2f, exitButtonColor.g + 0.1f, exitButtonColor.b + 0.1f, 0.95f);
        colors.pressedColor = new Color(0.15f, 0.05f, 0.06f, 0.95f);
        btn.colors = colors;
        btn.onClick.AddListener(ShowConfirmPanel);
        CreateText("Label", btnGO.transform, "<b>EXIT</b>", 34f, titleColor, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        AddHover(btnGO);
    }

    private GameObject BuildConfirmPanel(Transform parent)
    {
        var panel = CreatePanel("ConfirmExitPanel", parent, panelColor, outlineColor);
        SetRect(panel, new Vector2(0.2f, 0.24f), new Vector2(0.8f, 0.78f));

        CreateText("Title", panel.transform, "<b>LEAVE SCENARIO?</b>", 46f, titleColor, TextAlignmentOptions.Center, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.94f));
        CreateText("Body", panel.transform, "You can either return to this scenario or exit to the scenario selection.", 28f, bodyColor, TextAlignmentOptions.Center, new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.74f));

        var back = CreateButton("BackToScenarioButton", panel.transform, "<b>BACK TO SCENARIO</b>", neutralColor, 30f, new Vector2(0.07f, 0.12f), new Vector2(0.48f, 0.4f));
        back.onClick.AddListener(HideConfirmPanel);

        var exit = CreateButton("ExitScenarioButton", panel.transform, "<b>MENU</b>", dangerColor, 30f, new Vector2(0.52f, 0.12f), new Vector2(0.93f, 0.4f));
        exit.onClick.AddListener(ExitToScenarioSelection);

        return panel;
    }

    private GameObject BuildEndPanel(Transform parent)
    {
        var panel = CreatePanel("ScenarioEndPanel", parent, panelColor, outlineColor);
        SetRect(panel, new Vector2(0.2f, 0.24f), new Vector2(0.8f, 0.78f));

        CreateText("Title", panel.transform, "<b>SCENARIO COMPLETE</b>", 46f, titleColor, TextAlignmentOptions.Center, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.94f));
        CreateText("Body", panel.transform, "Play another scenario or replay this one.", 28f, bodyColor, TextAlignmentOptions.Center, new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.74f));

        var menu = CreateButton("MenuButton", panel.transform, "<b>MENU</b>", neutralColor, 30f, new Vector2(0.07f, 0.12f), new Vector2(0.48f, 0.4f));
        menu.onClick.AddListener(GoToMainMenu);

        var replay = CreateButton("ReplayButton", panel.transform, "<b>REPLAY SCENARIO</b>", positiveColor, 30f, new Vector2(0.52f, 0.12f), new Vector2(0.93f, 0.4f));
        replay.onClick.AddListener(ReplayScenario);

        return panel;
    }

    private GameObject CreatePanel(string name, Transform parent, Color color, Color outline)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        var ol = go.AddComponent<Outline>();
        ol.effectColor = outline;
        ol.effectDistance = new Vector2(3f, -3f);
        return go;
    }

    private Button CreateButton(string name, Transform parent, string label, Color color, float fontSize, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = CreatePanel(name, parent, color, new Color(0f, 0f, 0f, 0.35f));
        SetRect(go, anchorMin, anchorMax);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        var colors = btn.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.25f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.3f);
        colors.selectedColor = color;
        btn.colors = colors;
        CreateText("Text", go.transform, label, fontSize, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        AddHover(go);
        return btn;
    }

    private void AddHover(GameObject go)
    {
        if (go.GetComponent<VRUIHoverEffect>() == null) go.AddComponent<VRUIHoverEffect>();
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
