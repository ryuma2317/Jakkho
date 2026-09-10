using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEditor.SceneManagement;
using System.IO;

public class ScenarioSelectBuilder : Editor
{
    [MenuItem("Tools/Build Scenario Select VR Screen")]
    public static void BuildScenarioSelectScreen()
    {
        // 1. Create Data Assets & Thumbnails
        ScenarioData[] scenarios = CreateScenarioAssets();

        // 2. Create Card Prefab
        GameObject cardPrefab = CreateScenarioCardPrefab();

        // 3. Ensure EventSystem exists
        EnsureEventSystem();

        // 4. Ensure GameManager exists
        GameObject gmGO = GameObject.Find("GameManager");
        if (gmGO == null)
        {
            gmGO = new GameObject("GameManager");
            gmGO.AddComponent<GameManager>();
        }

        // 5. Create 3D World Space Scenario Select Canvas
        GameObject canvasGO = GameObject.Find("ScenarioSelectCanvas");
        if (canvasGO != null)
        {
            DestroyImmediate(canvasGO);
        }

        canvasGO = new GameObject("ScenarioSelectCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.position = new Vector3(0f, 1.45f, 2.2f);
        canvasRect.rotation = Quaternion.identity;
        canvasRect.sizeDelta = new Vector2(1920, 1080);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        if (Camera.main != null)
        {
            canvas.worldCamera = Camera.main;
        }

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        System.Type vrRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (vrRaycasterType != null && canvasGO.GetComponent(vrRaycasterType) == null)
        {
            canvasGO.AddComponent(vrRaycasterType);
        }

        // Main Background Panel
        GameObject mainPanelGO = CreateUIObject("ScenarioSelectPanel", canvasGO.transform);
        StretchToFill(mainPanelGO.GetComponent<RectTransform>());

        Image mainBg = mainPanelGO.AddComponent<Image>();
        mainBg.color = new Color(0.03f, 0.04f, 0.08f, 0.98f);
        mainBg.raycastTarget = true;

        // Header Title
        GameObject headerGO = CreateUIObject("HeaderTitle", mainPanelGO.transform);
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

        // Back to Menu Button
        GameObject backBtnGO = CreateStyledButton("BackButton", mainPanelGO.transform, "◄ Back to Menu", new Vector2(220, 50));
        RectTransform backRect = backBtnGO.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0f, 1f);
        backRect.anchorMax = new Vector2(0f, 1f);
        backRect.pivot = new Vector2(0f, 1f);
        backRect.anchoredPosition3D = new Vector3(50f, -50f, -15f);
        Button backBtn = backBtnGO.GetComponent<Button>();

        // Horizontal Layout Container for Cards
        GameObject containerGO = CreateUIObject("CardsContainer", mainPanelGO.transform);
        RectTransform containerRect = containerGO.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.44f);
        containerRect.anchorMax = new Vector2(0.5f, 0.44f);
        containerRect.anchoredPosition3D = new Vector3(0f, 0f, -15f);
        containerRect.sizeDelta = new Vector2(1150, 560);

        HorizontalLayoutGroup hGroup = containerGO.AddComponent<HorizontalLayoutGroup>();
        hGroup.spacing = 35;
        hGroup.childAlignment = TextAnchor.MiddleCenter;
        hGroup.childControlWidth = false;
        hGroup.childControlHeight = false;

        // Attach ScenarioSelectManager
        ScenarioSelectManager manager = canvasGO.AddComponent<ScenarioSelectManager>();
        manager.scenarios = scenarios;
        manager.scenarioCardPrefab = cardPrefab;
        manager.cardContainer = containerGO.transform;
        manager.backToMenuButton = backBtn;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<b>[ScenarioSelectBuilder]</b> Scenario Select VR Screen successfully built with card artwork!");
    }

    private static ScenarioData[] CreateScenarioAssets()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenarios"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenarios");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Textures"))
        {
            AssetDatabase.CreateFolder("Assets", "Textures");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Textures/Thumbnails"))
        {
            AssetDatabase.CreateFolder("Assets/Textures", "Thumbnails");
        }

        // Check both .jpg and .png paths for each scenario image
        Sprite s1Sprite = GetOrCreateThumbnailSprite("Assets/Textures/Thumbnails/Scenario_SuspiciousFile.png", "Assets/Textures/Thumbnails/Scenario_SuspiciousFile.jpg", new Color(0.85f, 0.25f, 0.25f));
        Sprite s2Sprite = GetOrCreateThumbnailSprite("Assets/Textures/Thumbnails/Scenario_FreeWifi.jpg", "Assets/Textures/Thumbnails/Scenario_FreeWifi.png", new Color(0.25f, 0.55f, 0.95f));
        Sprite s3Sprite = GetOrCreateThumbnailSprite("Assets/Textures/Thumbnails/Scenario_ScanOrSkip.png", "Assets/Textures/Thumbnails/Scenario_ScanOrSkip.jpg", new Color(0.95f, 0.65f, 0.2f));

        ScenarioData s1 = GetOrCreateScenario("Assets/Scenarios/Scenario_SuspiciousFile.asset",
            "Suspicious File",
            "A 'friend' sends you a file over Telegram. Do you know what happens if you tap it?",
            new string[] { "Phishing", "Malware" },
            s1Sprite,
            "Scenario_TelegramFile");

        ScenarioData s2 = GetOrCreateScenario("Assets/Scenarios/Scenario_FreeWifi.asset",
            "Free WiFi Trap",
            "No password, no login page — is this coffee shop WiFi actually safe to join?",
            new string[] { "Network Security", "Public WiFi" },
            s2Sprite,
            "Scenario_FreeWifi");

        ScenarioData s3 = GetOrCreateScenario("Assets/Scenarios/Scenario_ScanOrSkip.asset",
            "Scan or Skip?",
            "Someone sends you a link with a message trying to convince you it's real — do you click it?",
            new string[] { "Phishing Link", "Social Engineering" },
            s3Sprite,
            "Scenario_FakeLink");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return new ScenarioData[] { s1, s2, s3 };
    }

    private static Sprite GetOrCreateThumbnailSprite(string primaryPath, string secondaryPath, Color themeColor)
    {
        Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(primaryPath);
        if (existingSprite == null)
        {
            existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(secondaryPath);
        }
        if (existingSprite != null) return existingSprite;

        // Fallback procedural texture creation
        int width = 512;
        int height = 384;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color topColor = themeColor;
        Color bottomColor = new Color(0.06f, 0.08f, 0.14f);

        for (int y = 0; y < height; y++)
        {
            float t = (float)y / height;
            Color lineCol = Color.Lerp(bottomColor, topColor, t);
            for (int x = 0; x < width; x++)
            {
                float edgeX = Mathf.Abs((x / (float)width) - 0.5f) * 2f;
                float edgeY = Mathf.Abs((y / (float)height) - 0.5f) * 2f;
                float edgeFactor = 1f - (Mathf.Pow(edgeX, 4) + Mathf.Pow(edgeY, 4)) * 0.35f;
                
                tex.SetPixel(x, y, lineCol * Mathf.Clamp01(edgeFactor));
            }
        }
        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        File.WriteAllBytes(primaryPath, pngData);
        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(primaryPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(primaryPath);
    }

    private static ScenarioData GetOrCreateScenario(string path, string title, string desc, string[] tags, Sprite thumb, string sceneToLoad)
    {
        ScenarioData asset = AssetDatabase.LoadAssetAtPath<ScenarioData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<ScenarioData>();
            AssetDatabase.CreateAsset(asset, path);
        }
        asset.title = title;
        asset.description = desc;
        asset.tags = tags;
        if (thumb != null) asset.thumbnail = thumb;
        asset.sceneToLoad = sceneToLoad;
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static GameObject CreateScenarioCardPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        string prefabPath = "Assets/Prefabs/ScenarioCard.prefab";

        GameObject cardGO = CreateUIObject("ScenarioCard", null);
        RectTransform cardRect = cardGO.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(340, 520);

        Image cardBg = cardGO.AddComponent<Image>();
        cardBg.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);
        cardBg.raycastTarget = true;

        HoverScaler cardScaler = cardGO.AddComponent<HoverScaler>();
        cardScaler.SetHoverScaleMultiplier(1.06f, -30f);

        // Thumbnail Image
        GameObject thumbGO = CreateUIObject("ThumbnailImage", cardGO.transform);
        RectTransform thumbRect = thumbGO.GetComponent<RectTransform>();
        thumbRect.anchorMin = new Vector2(0f, 0.45f);
        thumbRect.anchorMax = new Vector2(1f, 1f);
        thumbRect.offsetMin = Vector2.zero;
        thumbRect.offsetMax = Vector2.zero;
        thumbGO.AddComponent<RectMask2D>();

        Image thumbImg = thumbGO.AddComponent<Image>();
        thumbImg.color = Color.white;
        thumbImg.raycastTarget = false;

        // Gradient Overlay
        GameObject gradGO = CreateUIObject("GradientOverlay", thumbGO.transform);
        RectTransform gradRect = gradGO.GetComponent<RectTransform>();
        gradRect.anchorMin = new Vector2(0f, 0f);
        gradRect.anchorMax = new Vector2(1f, 0.4f);
        gradRect.offsetMin = Vector2.zero;
        gradRect.offsetMax = Vector2.zero;
        Image gradImg = gradGO.AddComponent<Image>();
        gradImg.color = new Color(0.05f, 0.06f, 0.1f, 0.85f);
        gradImg.raycastTarget = false;

        // Risk Badge
        GameObject badgeGO = CreateUIObject("RiskBadge", cardGO.transform);
        RectTransform badgeRect = badgeGO.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1f, 1f);
        badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(1f, 1f);
        badgeRect.anchoredPosition3D = new Vector3(-12f, -12f, -5f);
        badgeRect.sizeDelta = new Vector2(90, 32);

        Image badgeImg = badgeGO.AddComponent<Image>();
        badgeImg.color = new Color(0.85f, 0.2f, 0.25f, 0.9f);
        badgeImg.raycastTarget = false;

        GameObject badgeTextGO = CreateUIObject("Text", badgeGO.transform);
        StretchToFill(badgeTextGO.GetComponent<RectTransform>());
        TextMeshProUGUI badgeTMP = badgeTextGO.AddComponent<TextMeshProUGUI>();
        badgeTMP.text = "⚠ Risk";
        badgeTMP.fontSize = 13;
        badgeTMP.fontStyle = FontStyles.Bold;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        badgeTMP.color = Color.white;
        badgeTMP.raycastTarget = false;

        // Title Text
        GameObject titleGO = CreateUIObject("TitleText", cardGO.transform);
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.06f, 0.34f);
        titleRect.anchorMax = new Vector2(0.94f, 0.43f);
        titleRect.anchoredPosition3D = new Vector3(0f, 0f, -5f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Scenario Title";
        titleTMP.fontSize = 28;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Left;
        titleTMP.color = Color.white;
        titleTMP.raycastTarget = false;

        // Description Text
        GameObject descGO = CreateUIObject("DescriptionText", cardGO.transform);
        RectTransform descRect = descGO.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.06f, 0.19f);
        descRect.anchorMax = new Vector2(0.94f, 0.33f);
        descRect.anchoredPosition3D = new Vector3(0f, 0f, -5f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;

        TextMeshProUGUI descTMP = descGO.AddComponent<TextMeshProUGUI>();
        descTMP.text = "Scenario description text goes here...";
        descTMP.fontSize = 15;
        descTMP.fontStyle = FontStyles.Normal;
        descTMP.alignment = TextAlignmentOptions.TopLeft;
        descTMP.color = new Color(0.8f, 0.85f, 0.92f);
        descTMP.textWrappingMode = TextWrappingModes.Normal; // Updated for Unity 6 TextMeshPro
        descTMP.raycastTarget = false;

        // TagRow
        GameObject tagRowGO = CreateUIObject("TagRow", cardGO.transform);
        RectTransform tagRowRect = tagRowGO.GetComponent<RectTransform>();
        tagRowRect.anchorMin = new Vector2(0.06f, 0.12f);
        tagRowRect.anchorMax = new Vector2(0.94f, 0.18f);
        tagRowRect.anchoredPosition3D = new Vector3(0f, 0f, -5f);
        tagRowRect.offsetMin = Vector2.zero;
        tagRowRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup tagLayout = tagRowGO.AddComponent<HorizontalLayoutGroup>();
        tagLayout.spacing = 8;
        tagLayout.childAlignment = TextAnchor.MiddleLeft;
        tagLayout.childControlWidth = false;
        tagLayout.childControlHeight = false;

        // PlayButton
        GameObject btnGO = CreateUIObject("PlayButton", cardGO.transform);
        RectTransform btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.06f, 0.025f);
        btnRect.anchorMax = new Vector2(0.94f, 0.11f);
        btnRect.anchoredPosition3D = new Vector3(0f, 0f, -10f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = Color.white;
        btnImg.raycastTarget = true;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.92f, 1f);
        colors.pressedColor = new Color(0.75f, 0.78f, 0.88f);
        btn.colors = colors;

        GameObject btnTextGO = CreateUIObject("Text", btnGO.transform);
        StretchToFill(btnTextGO.GetComponent<RectTransform>());

        TextMeshProUGUI btnTMP = btnTextGO.AddComponent<TextMeshProUGUI>();
        btnTMP.text = "▶ Play Scenario";
        btnTMP.fontSize = 20;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color = new Color(0.05f, 0.08f, 0.15f);
        btnTMP.raycastTarget = false;

        ScenarioCardUI cardUI = cardGO.AddComponent<ScenarioCardUI>();
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(cardGO, prefabPath);
        DestroyImmediate(cardGO);

        return savedPrefab;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }
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

        GameObject labelGO = CreateUIObject("Text", btnGO.transform);
        StretchToFill(labelGO.GetComponent<RectTransform>());

        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = textStr;
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return btnGO;
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
        if (legacyModule != null)
        {
            Undo.DestroyObjectImmediate(legacyModule);
        }

        if (inputSystemModuleType != null)
        {
            Component inputSystemModule = es.GetComponent(inputSystemModuleType);
            if (inputSystemModule == null)
            {
                Undo.AddComponent(es.gameObject, inputSystemModuleType);
            }
        }
        else
        {
            if (es.GetComponent<StandaloneInputModule>() == null)
            {
                Undo.AddComponent<StandaloneInputModule>(es.gameObject);
            }
        }
    }
}
