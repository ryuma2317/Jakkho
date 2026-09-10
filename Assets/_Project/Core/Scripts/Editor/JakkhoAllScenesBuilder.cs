#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JakkhoAllScenesBuilder : MonoBehaviour
{
    private static TMP_FontAsset defaultFont;

    private static void EnsureFont()
    {
        if (defaultFont == null)
        {
            defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont == null)
            {
                defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }
        }
    }

    [MenuItem("Jakkho/Build Main Menu Scene & Prefab", false, 2)]
    public static void BuildMainMenu()
    {
        EnsureFont();
        string scenePath = "Assets/_Project/Core/Scenes/MainMenu.unity";
        if (File.Exists(scenePath))
        {
            EditorSceneManager.OpenScene(scenePath);
        }

        // Core Managers
        GameObject coreManagers = GameObject.Find("[Core_Managers]");
        if (coreManagers == null) coreManagers = new GameObject("[Core_Managers]");
        if (!coreManagers.GetComponent<GameManager>()) coreManagers.AddComponent<GameManager>();
        if (!coreManagers.GetComponent<ScoreTracker>()) coreManagers.AddComponent<ScoreTracker>();
        if (!coreManagers.GetComponent<VRInteractionHandler>()) coreManagers.AddComponent<VRInteractionHandler>();

        // Recreate Canvas
        GameObject existingCanvas = GameObject.Find("MainMenu_Canvas");
        if (existingCanvas != null) DestroyImmediate(existingCanvas);

        GameObject canvasObj = new GameObject("MainMenu_Canvas");
        canvasObj.transform.position = new Vector3(0, 1.15f, 0.95f);
        canvasObj.transform.rotation = Quaternion.Euler(4f, 0, 0);
        canvasObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1200, 800);

        canvasObj.AddComponent<GraphicRaycaster>();
        Type trackedRaycasterType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (trackedRaycasterType != null) canvasObj.AddComponent(trackedRaycasterType);

        MainMenuUI menuUI = canvasObj.AddComponent<MainMenuUI>();

        // Background
        GameObject bgObj = CreateUIObject("Background", canvasObj.transform);
        SetRect(bgObj, Vector2.zero, Vector2.one);
        bgObj.AddComponent<Image>().color = new Color(0.07f, 0.10f, 0.16f, 0.98f);

        // Header Title
        TextMeshProUGUI titleText = CreateTMP("Title", bgObj.transform, "<b>JAKKHO VR</b>", 52, TextAlignmentOptions.Center, new Color(0.35f, 0.8f, 1.0f));
        SetRect(titleText.gameObject, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.96f));

        TextMeshProUGUI subtitleText = CreateTMP("Subtitle", bgObj.transform, "\"Experience. Decide. Understand. Respond.\"", 26, TextAlignmentOptions.Center, new Color(0.85f, 0.9f, 0.95f));
        SetRect(subtitleText.gameObject, new Vector2(0.1f, 0.68f), new Vector2(0.9f, 0.78f));

        // Stats Banner
        TextMeshProUGUI xpText = CreateTMP("TotalXPText", bgObj.transform, "<b>Total XP: 0</b>", 28, TextAlignmentOptions.Left, new Color(0.3f, 0.95f, 0.55f));
        SetRect(xpText.gameObject, new Vector2(0.1f, 0.58f), new Vector2(0.5f, 0.68f));

        TextMeshProUGUI completedText = CreateTMP("CompletedText", bgObj.transform, "<b>Completed: 0</b>", 28, TextAlignmentOptions.Right, new Color(1f, 0.85f, 0.4f));
        SetRect(completedText.gameObject, new Vector2(0.5f, 0.58f), new Vector2(0.9f, 0.68f));

        // Panels
        GameObject mainPanel = CreateUIObject("MainPanel", bgObj.transform);
        SetRect(mainPanel, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.56f));

        GameObject settingsPanel = CreateUIObject("SettingsPanel", bgObj.transform);
        SetRect(settingsPanel, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.56f));
        settingsPanel.SetActive(false);

        GameObject creditsPanel = CreateUIObject("CreditsPanel", bgObj.transform);
        SetRect(creditsPanel, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.56f));
        creditsPanel.SetActive(false);

        // Main Panel Buttons
        GameObject playBtn = CreateButton("PlayBtn", mainPanel.transform, "<b>[ PLAY / SELECT SCENARIO ]</b>", new Color(0.18f, 0.6f, 0.38f), 32);
        SetRect(playBtn, new Vector2(0.15f, 0.68f), new Vector2(0.85f, 0.96f));
        playBtn.GetComponent<Button>().onClick.AddListener(menuUI.OnPlayClicked);

        GameObject settingsBtn = CreateButton("SettingsBtn", mainPanel.transform, "<b>[ SETTINGS ]</b>", new Color(0.25f, 0.45f, 0.65f), 32);
        SetRect(settingsBtn, new Vector2(0.15f, 0.36f), new Vector2(0.85f, 0.64f));
        settingsBtn.GetComponent<Button>().onClick.AddListener(menuUI.ShowSettingsPanel);

        GameObject quitBtn = CreateButton("QuitBtn", mainPanel.transform, "<b>[ QUIT ]</b>", new Color(0.65f, 0.22f, 0.22f), 32);
        SetRect(quitBtn, new Vector2(0.15f, 0.04f), new Vector2(0.85f, 0.32f));
        quitBtn.GetComponent<Button>().onClick.AddListener(menuUI.OnQuitClicked);

        // Settings Panel Contents
        TextMeshProUGUI setHeader = CreateTMP("SettingsHeader", settingsPanel.transform, "<b>AUDIO SETTINGS</b>", 32, TextAlignmentOptions.Center, Color.white);
        SetRect(setHeader.gameObject, new Vector2(0.1f, 0.7f), new Vector2(0.9f, 0.95f));

        GameObject backFromSetBtn = CreateButton("BackFromSettings", settingsPanel.transform, "<b>[ BACK ]</b>", new Color(0.4f, 0.45f, 0.55f), 30);
        SetRect(backFromSetBtn, new Vector2(0.25f, 0.1f), new Vector2(0.75f, 0.35f));
        backFromSetBtn.GetComponent<Button>().onClick.AddListener(menuUI.ShowMainPanel);

        // Connect references
        SerializedObject so = new SerializedObject(menuUI);
        so.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.FindProperty("creditsPanel").objectReferenceValue = creditsPanel;
        so.FindProperty("totalXPText").objectReferenceValue = xpText;
        so.FindProperty("scenariosCompletedText").objectReferenceValue = completedText;
        so.ApplyModifiedProperties();

        // Save Prefab
        if (!Directory.Exists("Assets/_Project/Core/Prefabs")) Directory.CreateDirectory("Assets/_Project/Core/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(canvasObj, "Assets/_Project/Core/Prefabs/MainMenu_Canvas.prefab");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[JakkhoAllScenesBuilder] Built Main Menu Scene & Prefab!");
    }

    [MenuItem("Jakkho/Build Scenario Selection Scene & Prefab", false, 3)]
    public static void BuildScenarioSelection()
    {
        EnsureFont();
        string scenePath = "Assets/_Project/Core/Scenes/ScenarioSelection.unity";
        if (File.Exists(scenePath))
        {
            EditorSceneManager.OpenScene(scenePath);
        }

        // Core Managers
        GameObject coreManagers = GameObject.Find("[Core_Managers]");
        if (coreManagers == null) coreManagers = new GameObject("[Core_Managers]");
        if (!coreManagers.GetComponent<GameManager>()) coreManagers.AddComponent<GameManager>();
        if (!coreManagers.GetComponent<ScoreTracker>()) coreManagers.AddComponent<ScoreTracker>();
        if (!coreManagers.GetComponent<VRInteractionHandler>()) coreManagers.AddComponent<VRInteractionHandler>();

        // Canvas
        GameObject existingCanvas = GameObject.Find("ScenarioSelection_Canvas");
        if (existingCanvas != null) DestroyImmediate(existingCanvas);

        GameObject canvasObj = new GameObject("ScenarioSelection_Canvas");
        canvasObj.transform.position = new Vector3(0, 1.15f, 0.95f);
        canvasObj.transform.rotation = Quaternion.Euler(4f, 0, 0);
        canvasObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1200, 800);

        canvasObj.AddComponent<GraphicRaycaster>();
        Type trackedRaycasterType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (trackedRaycasterType != null) canvasObj.AddComponent(trackedRaycasterType);

        CarouselController carousel = canvasObj.AddComponent<CarouselController>();

        // Background
        GameObject bgObj = CreateUIObject("Background", canvasObj.transform);
        SetRect(bgObj, Vector2.zero, Vector2.one);
        bgObj.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.15f, 0.98f);

        // Header Title
        TextMeshProUGUI titleText = CreateTMP("Header", bgObj.transform, "<b>SELECT TRAINING SCENARIO</b>", 42, TextAlignmentOptions.Center, new Color(0.4f, 0.85f, 1.0f));
        SetRect(titleText.gameObject, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f));

        // Card Container
        GameObject cardBox = CreateUIObject("ScenarioCard", bgObj.transform);
        SetRect(cardBox, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.84f));
        cardBox.AddComponent<Image>().color = new Color(0.14f, 0.18f, 0.26f, 1f);

        TextMeshProUGUI cardTitle = CreateTMP("CardTitle", cardBox.transform, "<b>Scenario 3: The Fake Link Hijacking</b>", 36, TextAlignmentOptions.TopLeft, Color.white);
        SetRect(cardTitle.gameObject, new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.95f));

        TextMeshProUGUI cardDesc = CreateTMP("CardDesc", cardBox.transform, "A friend's hacked Telegram sends an urgent survey link for their NextGen project. Inspect the URL, refuse QR device linking, and enable 2FA.", 30, TextAlignmentOptions.TopLeft, new Color(0.90f, 0.92f, 0.96f));
        SetRect(cardDesc.gameObject, new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.74f));

        TextMeshProUGUI settingTxt = CreateTMP("SettingText", cardBox.transform, "<b>Location:</b> Home Study Room", 26, TextAlignmentOptions.Left, new Color(0.5f, 0.85f, 1f));
        SetRect(settingTxt.gameObject, new Vector2(0.05f, 0.16f), new Vector2(0.5f, 0.30f));

        TextMeshProUGUI threatTxt = CreateTMP("ThreatText", cardBox.transform, "<b>Threat:</b> Telegram QR Hijack", 26, TextAlignmentOptions.Left, new Color(1f, 0.65f, 0.4f));
        SetRect(threatTxt.gameObject, new Vector2(0.05f, 0.04f), new Vector2(0.5f, 0.16f));

        TextMeshProUGUI xpTxt = CreateTMP("MaxXPText", cardBox.transform, "<b>Max Reward: +200 XP</b>", 28, TextAlignmentOptions.Right, new Color(0.3f, 0.95f, 0.55f));
        SetRect(xpTxt.gameObject, new Vector2(0.5f, 0.04f), new Vector2(0.95f, 0.20f));

        TextMeshProUGUI pageIndicator = CreateTMP("PageIndicator", bgObj.transform, "<b>3 / 3</b>", 26, TextAlignmentOptions.Center, Color.white);
        SetRect(pageIndicator.gameObject, new Vector2(0.4f, 0.19f), new Vector2(0.6f, 0.27f));

        // Navigation Buttons
        GameObject prevBtn = CreateButton("PrevBtn", bgObj.transform, "<b>< PREV</b>", new Color(0.28f, 0.38f, 0.50f), 26);
        SetRect(prevBtn, new Vector2(0.06f, 0.18f), new Vector2(0.26f, 0.27f));

        GameObject nextBtn = CreateButton("NextBtn", bgObj.transform, "<b>NEXT ></b>", new Color(0.28f, 0.38f, 0.50f), 26);
        SetRect(nextBtn, new Vector2(0.74f, 0.18f), new Vector2(0.94f, 0.27f));

        // Start Scenario & Back Buttons
        GameObject startBtn = CreateButton("StartScenarioBtn", bgObj.transform, "<b>[ START SCENARIO ]</b>", new Color(0.18f, 0.65f, 0.38f), 32);
        SetRect(startBtn, new Vector2(0.25f, 0.04f), new Vector2(0.75f, 0.15f));

        GameObject backMenuBtn = CreateButton("BackMenuBtn", bgObj.transform, "<b>[ MENU ]</b>", new Color(0.4f, 0.45f, 0.55f), 26);
        SetRect(backMenuBtn, new Vector2(0.06f, 0.04f), new Vector2(0.20f, 0.15f));
        backMenuBtn.GetComponent<Button>().onClick.AddListener(carousel.BackToMainMenu);

        // Connect references
        SerializedObject so = new SerializedObject(carousel);
        so.FindProperty("titleText").objectReferenceValue = cardTitle;
        so.FindProperty("descriptionText").objectReferenceValue = cardDesc;
        so.FindProperty("settingText").objectReferenceValue = settingTxt;
        so.FindProperty("threatTypeText").objectReferenceValue = threatTxt;
        so.FindProperty("maxXPText").objectReferenceValue = xpTxt;
        so.FindProperty("pageIndicatorText").objectReferenceValue = pageIndicator;
        so.FindProperty("startScenarioButton").objectReferenceValue = startBtn.GetComponent<Button>();
        so.FindProperty("startButtonText").objectReferenceValue = startBtn.GetComponentInChildren<TextMeshProUGUI>();
        so.FindProperty("prevButton").objectReferenceValue = prevBtn.GetComponent<Button>();
        so.FindProperty("nextButton").objectReferenceValue = nextBtn.GetComponent<Button>();
        so.ApplyModifiedProperties();

        // Save Prefab
        if (!Directory.Exists("Assets/_Project/Core/Prefabs")) Directory.CreateDirectory("Assets/_Project/Core/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(canvasObj, "Assets/_Project/Core/Prefabs/ScenarioSelection_Canvas.prefab");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[JakkhoAllScenesBuilder] Built Scenario Selection Scene & Prefab with Large Crisp Typography!");
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private static void SetRect(GameObject obj, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateTMP(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObj = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) tmp.font = defaultFont;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.extraPadding = true;
        return tmp;
    }

    private static GameObject CreateButton(string name, Transform parent, string buttonText, Color buttonColor, float fontSize = 28)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        Image img = btnObj.AddComponent<Image>();
        img.color = buttonColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = Color.Lerp(buttonColor, Color.white, 0.40f);
        colors.pressedColor = Color.Lerp(buttonColor, Color.black, 0.35f);
        colors.selectedColor = buttonColor;
        btn.colors = colors;

        TextMeshProUGUI txt = CreateTMP("Text", btnObj.transform, buttonText, fontSize, TextAlignmentOptions.Center, Color.white);
        SetRect(txt.gameObject, Vector2.zero, Vector2.one);

        return btnObj;
    }
}
#endif
