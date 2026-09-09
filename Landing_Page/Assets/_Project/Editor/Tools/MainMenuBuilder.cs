using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEditor.SceneManagement;

public class MainMenuBuilder : Editor
{
    [MenuItem("Tools/Build 3D VR Main Menu UI")]
    public static void BuildMenuUI()
    {
        // 1. Ensure EventSystem
        EnsureEventSystem();

        // 2. Destroy any leftover old room environments (plain walls, pink walls etc.)
        DestroyOldRooms();

        // 3. Build the shared Sci-Fi Command Room environment
        //    Both the Main Menu canvas AND the Scenario Select canvas float inside this room.
        SciFiCommandRoomBuilder.BuildRoomGeometryOnly();

        // 4. Create Root Menu Manager GameObject
        GameObject menuManagerGO = GameObject.Find("MenuManager");
        if (menuManagerGO == null) menuManagerGO = new GameObject("MenuManager");
        MenuManager menuManager = menuManagerGO.GetComponent<MenuManager>();
        if (menuManager == null) menuManager = menuManagerGO.AddComponent<MenuManager>();

        // 5. Create 3D World Space Canvas (Main Menu)
        GameObject canvasGO = GameObject.Find("MainMenuCanvas");
        if (canvasGO != null) DestroyImmediate(canvasGO);
        GameObject oldCanvas = GameObject.Find("VR_WorldMenuCanvas");
        if (oldCanvas != null) DestroyImmediate(oldCanvas);

        canvasGO = new GameObject("MainMenuCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.position = new Vector3(0f, 1.45f, 2.2f);
        canvasRect.rotation = Quaternion.identity;
        canvasRect.sizeDelta = new Vector2(1920, 1080);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        Camera mainCam = Camera.main;
        if (mainCam != null) canvas.worldCamera = mainCam;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        System.Type vrRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (vrRaycasterType != null && canvasGO.GetComponent(vrRaycasterType) == null)
            canvasGO.AddComponent(vrRaycasterType);

        // -------------------------------------------------------
        // LAYER 1: Main Menu Panel
        // -------------------------------------------------------
        GameObject mainPanelGO = CreateUIObject("MainMenuPanel", canvasGO.transform);
        StretchToFill(mainPanelGO.GetComponent<RectTransform>());
        CanvasGroup mainCanvasGroup = mainPanelGO.AddComponent<CanvasGroup>();
        Image mainBg = mainPanelGO.AddComponent<Image>();
        mainBg.color = new Color(0.04f, 0.05f, 0.1f, 0.92f);
        mainBg.raycastTarget = true;

        // Title
        GameObject titleGO = CreateUIObject("TitleText", mainPanelGO.transform);
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.82f);
        titleRect.anchorMax = new Vector2(0.5f, 0.82f);
        titleRect.anchoredPosition3D = new Vector3(0f, 0f, -10f);
        titleRect.sizeDelta = new Vector2(900, 110);
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "VR SCAM AWARENESS TRAINING";
        titleTMP.fontSize = 52;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(0.9f, 0.95f, 1.0f);
        titleTMP.raycastTarget = false;

        // Subtitle
        GameObject subGO = CreateUIObject("SubtitleText", mainPanelGO.transform);
        RectTransform subRect = subGO.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 0.72f);
        subRect.anchorMax = new Vector2(0.5f, 0.72f);
        subRect.anchoredPosition3D = new Vector3(0f, 0f, -10f);
        subRect.sizeDelta = new Vector2(700, 50);
        TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.text = "Learn to spot scams before they happen";
        subTMP.fontSize = 24;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color = new Color(0.65f, 0.78f, 1.0f);
        subTMP.raycastTarget = false;

        // Button Container
        GameObject vLayoutGO = CreateUIObject("ButtonContainer", mainPanelGO.transform);
        RectTransform vLayoutRect = vLayoutGO.GetComponent<RectTransform>();
        vLayoutRect.anchorMin = new Vector2(0.5f, 0.45f);
        vLayoutRect.anchorMax = new Vector2(0.5f, 0.45f);
        vLayoutRect.anchoredPosition3D = new Vector3(0f, 0f, -15f);
        vLayoutRect.sizeDelta = new Vector2(320, 220);
        VerticalLayoutGroup vLayout = vLayoutGO.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 25;
        vLayout.childAlignment = TextAnchor.MiddleCenter;
        vLayout.childControlHeight = false;
        vLayout.childControlWidth = false;

        // START Button
        GameObject playBtnGO = CreateStyledButton("PlayButton", vLayoutGO.transform, "▶  START", new Vector2(280, 70));
        Button playBtn = playBtnGO.GetComponent<Button>();
        HoverScaler playScaler = playBtnGO.AddComponent<HoverScaler>();
        playScaler.SetHoverScaleMultiplier(1.05f, -25f);

        // EXIT Button
        GameObject exitBtnGO = CreateStyledButton("ExitButton", vLayoutGO.transform, "EXIT", new Vector2(280, 70));
        Button exitBtn = exitBtnGO.GetComponent<Button>();
        HoverScaler exitScaler = exitBtnGO.AddComponent<HoverScaler>();
        exitScaler.SetHoverScaleMultiplier(1.05f, -25f);

        // -------------------------------------------------------
        // LAYER 2: Scenario Select Panel (hidden by default)
        // -------------------------------------------------------
        GameObject charPanelGO = CreateUIObject("CharacterSelectPanel", canvasGO.transform);
        StretchToFill(charPanelGO.GetComponent<RectTransform>());
        CanvasGroup charCanvasGroup = charPanelGO.AddComponent<CanvasGroup>();
        charCanvasGroup.alpha = 0f;
        charCanvasGroup.interactable = false;
        charCanvasGroup.blocksRaycasts = false;
        charPanelGO.SetActive(false);

        Image charBg = charPanelGO.AddComponent<Image>();
        charBg.color = new Color(0.02f, 0.03f, 0.07f, 0.98f);
        charBg.raycastTarget = true;

        // Back Button
        GameObject backBtnGO = CreateStyledButton("BackButton", charPanelGO.transform, "◄ BACK", new Vector2(150, 50));
        RectTransform backRect = backBtnGO.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0f, 1f);
        backRect.anchorMax = new Vector2(0f, 1f);
        backRect.pivot = new Vector2(0f, 1f);
        backRect.anchoredPosition3D = new Vector2(50, -50);
        Button backBtn = backBtnGO.GetComponent<Button>();
        HoverScaler backScaler = backBtnGO.AddComponent<HoverScaler>();
        backScaler.SetHoverScaleMultiplier(1.05f, -20f);

        // Header
        GameObject headerGO = CreateUIObject("HeaderTitle", charPanelGO.transform);
        RectTransform headerRect = headerGO.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0.5f, 0.88f);
        headerRect.anchorMax = new Vector2(0.5f, 0.88f);
        headerRect.anchoredPosition3D = new Vector3(0f, 0f, -10f);
        headerRect.sizeDelta = new Vector2(850, 90);
        TextMeshProUGUI headerTMP = headerGO.AddComponent<TextMeshProUGUI>();
        headerTMP.text = "SELECT SCAM TRAINING SCENARIO";
        headerTMP.fontSize = 44;
        headerTMP.fontStyle = FontStyles.Bold;
        headerTMP.alignment = TextAlignmentOptions.Center;
        headerTMP.color = Color.white;
        headerTMP.raycastTarget = false;

        // Horizontal Cards Container
        GameObject hLayoutGO = CreateUIObject("CardsContainer", charPanelGO.transform);
        RectTransform hLayoutRect = hLayoutGO.GetComponent<RectTransform>();
        hLayoutRect.anchorMin = new Vector2(0.5f, 0.45f);
        hLayoutRect.anchorMax = new Vector2(0.5f, 0.45f);
        hLayoutRect.anchoredPosition3D = new Vector3(0f, 0f, -15f);
        hLayoutRect.sizeDelta = new Vector2(960, 540);
        HorizontalLayoutGroup hLayout = hLayoutGO.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 45;
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;

        CreateCharacterCard(hLayoutGO.transform, "MOE ROCKA",  "THE MIGHTY",       new Color(0.2f,  0.65f, 1.0f));
        CreateCharacterCard(hLayoutGO.transform, "ZAPPER",     "LIGHTNING SPEED",  new Color(0.75f, 0.35f, 1.0f));
        CreateCharacterCard(hLayoutGO.transform, "VORTEX",     "THE UNSTOPPABLE",  new Color(1.0f,  0.25f, 0.35f));

        // Wire up MenuManager
        menuManager.SetupReferences(mainCanvasGroup, charCanvasGroup, playBtn, exitBtn, backBtn);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<b>[MainMenuBuilder]</b> 3D VR Main Menu rebuilt inside Sci-Fi Command Room!");
    }

    // Destroys all old room parents created by previous builders
    private static void DestroyOldRooms()
    {
        string[] oldNames = new string[]
        {
            "VR_RoomEnvironment",
            "VR_SciFiRoom",
            "RoomEnvironment",
            "SciFiRoom",
            "Room"
        };
        foreach (string n in oldNames)
        {
            GameObject old = GameObject.Find(n);
            if (old != null) DestroyImmediate(old);
        }
    }

    // -------------------------------------------------------
    // Shared Helpers
    // -------------------------------------------------------
    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void StretchToFill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject CreateStyledButton(string name, Transform parent, string textStr, Vector2 size)
    {
        GameObject btnGO = CreateUIObject(name, parent);
        RectTransform btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.sizeDelta = size;
        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(0.12f, 0.16f, 0.26f, 0.9f);
        img.raycastTarget = true;
        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock colors = btn.colors;
        colors.normalColor      = new Color(0.12f, 0.16f, 0.26f, 0.9f);
        colors.highlightedColor = new Color(0.20f, 0.28f, 0.45f, 0.95f);
        colors.pressedColor     = new Color(0.08f, 0.10f, 0.18f, 1.0f);
        btn.colors = colors;
        GameObject labelGO = CreateUIObject("Text", btnGO.transform);
        StretchToFill(labelGO.GetComponent<RectTransform>());
        labelGO.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0f, 0f, -5f);
        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = textStr;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return btnGO;
    }

    private static void CreateCharacterCard(Transform parent, string charName, string subtitle, Color accentColor)
    {
        GameObject cardGO = CreateUIObject($"Card_{charName.Replace(" ", "")}", parent);
        RectTransform cardRect = cardGO.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(260, 500);
        Image cardBg = cardGO.AddComponent<Image>();
        cardBg.color = new Color(0.08f, 0.09f, 0.14f, 0.92f);
        cardBg.raycastTarget = true;
        HoverScaler cardScaler = cardGO.AddComponent<HoverScaler>();
        cardScaler.SetHoverScaleMultiplier(1.08f, -30f);

        GameObject borderGO = CreateUIObject("GlowingBorder", cardGO.transform);
        RectTransform borderRect = borderGO.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0.05f, 0.35f);
        borderRect.anchorMax = new Vector2(0.95f, 0.95f);
        borderRect.anchoredPosition3D = new Vector3(0f, 0f, -5f);
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        Image borderImg = borderGO.AddComponent<Image>();
        borderImg.color = accentColor;
        borderImg.raycastTarget = false;

        GameObject portraitGO = CreateUIObject("PortraitImage", borderGO.transform);
        RectTransform portraitRect = portraitGO.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0.04f, 0.04f);
        portraitRect.anchorMax = new Vector2(0.96f, 0.96f);
        portraitRect.anchoredPosition3D = new Vector3(0f, 0f, -5f);
        portraitRect.offsetMin = Vector2.zero;
        portraitRect.offsetMax = Vector2.zero;
        Image portraitImg = portraitGO.AddComponent<Image>();
        portraitImg.color = new Color(0.18f, 0.22f, 0.32f, 1f);
        portraitImg.raycastTarget = false;

        GameObject nameGO = CreateUIObject("NameText", cardGO.transform);
        RectTransform nameRect = nameGO.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.22f);
        nameRect.anchorMax = new Vector2(1f, 0.30f);
        nameRect.anchoredPosition3D = new Vector3(0f, 0f, -15f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = charName.ToUpper();
        nameTMP.fontSize = 22;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color = Color.white;
        nameTMP.raycastTarget = false;

        GameObject subGO2 = CreateUIObject("SubtitleText", cardGO.transform);
        RectTransform subRect2 = subGO2.GetComponent<RectTransform>();
        subRect2.anchorMin = new Vector2(0f, 0.14f);
        subRect2.anchorMax = new Vector2(1f, 0.21f);
        subRect2.anchoredPosition3D = new Vector3(0f, 0f, -15f);
        subRect2.offsetMin = Vector2.zero;
        subRect2.offsetMax = Vector2.zero;
        TextMeshProUGUI subTMP2 = subGO2.AddComponent<TextMeshProUGUI>();
        subTMP2.text = subtitle.ToUpper();
        subTMP2.fontSize = 13;
        subTMP2.alignment = TextAlignmentOptions.Center;
        subTMP2.color = new Color(0.75f, 0.8f, 0.9f, 0.85f);
        subTMP2.raycastTarget = false;

        GameObject tipGO = CreateUIObject("BottomTipDot", cardGO.transform);
        RectTransform tipRect = tipGO.GetComponent<RectTransform>();
        tipRect.anchorMin = new Vector2(0.5f, 0.05f);
        tipRect.anchorMax = new Vector2(0.5f, 0.05f);
        tipRect.anchoredPosition3D = new Vector3(0f, 0f, -20f);
        tipRect.sizeDelta = new Vector2(14, 14);
        Image tipImg = tipGO.AddComponent<Image>();
        tipImg.color = accentColor;
        tipImg.raycastTarget = false;

        CharacterCard cardComp = cardGO.AddComponent<CharacterCard>();
        cardComp.SetCharacterData(charName, subtitle, accentColor, null);
    }

    private static void EnsureEventSystem()
    {
        EventSystem es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            es = esGO.AddComponent<EventSystem>();
        }
        System.Type inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        StandaloneInputModule legacyModule = es.GetComponent<StandaloneInputModule>();
        if (legacyModule != null) Undo.DestroyObjectImmediate(legacyModule);
        if (inputSystemModuleType != null)
        {
            if (es.GetComponent(inputSystemModuleType) == null)
                Undo.AddComponent(es.gameObject, inputSystemModuleType);
        }
        else
        {
            if (es.GetComponent<StandaloneInputModule>() == null)
                Undo.AddComponent<StandaloneInputModule>(es.gameObject);
        }
    }
}
