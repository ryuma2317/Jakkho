using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

namespace Kannko.Editor
{
    public static class BuildFloatingLaptopPanel
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";
        private const string PrefabPath = "Assets/Scenarios/Scenario2/Prefabs/UI/FloatingLaptopPanel.prefab";
        private const string TexturesFolder = "Assets/Scenarios/Scenario2/Textures/";

        [MenuItem("VR Prototype/Build Floating Laptop Panel", priority = 10)]
        public static void BuildPanel()
        {
            Debug.Log("[LaptopPanel] Starting standalone floating laptop UI panel construction (Enhanced VR Readability)...");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[LaptopPanel] Scene not found: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Locate WorldUI parent (or scene root)
            GameObject worldUiGo = GameObject.Find("WorldUI");
            Transform parentTransform = worldUiGo != null ? worldUiGo.transform : null;

            // Clean up existing panel if re-running
            GameObject existingPanel = GameObject.Find("FloatingLaptopPanel");
            if (existingPanel != null) Object.DestroyImmediate(existingPanel);

            // 2. Import / Load Texture Assets
            ImportAllTextures();

            Sprite moonArtwork = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "T_Laptop_MoonArtwork.png");
            Sprite darkCardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "UI_DarkCard_Bg.png");
            Sprite pillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "UI_Pill_Bg.png");
            Sprite wifiPanelBg = GetOrCreateWifiPanelSprite();
            Sprite reminderBg = GetOrCreateReminderSprite();
            Sprite toggleOn = GetOrCreateToggleSprite();
            Sprite wifiWave = GetOrCreateWifiWaveSprite();

            Sprite iconThisPC = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "Icon_ThisPC.png");
            Sprite iconRecycle = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "Icon_RecycleBin.png");
            Sprite iconChrome = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "Icon_Chrome.png");
            Sprite iconWord = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "Icon_Word.png");
            Sprite iconExcel = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "Icon_Excel.png");
            Sprite iconTelegram = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "Icon_Telegram.png");
            Sprite iconVSCode = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "Icon_VSCode.png");
            Sprite iconUnity = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "Icon_UnityHub.png");
            Sprite iconPostman = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "Icon_Postman.png");

            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            // 3. Create Root Floating Laptop Panel (Standalone World Space Canvas)
            GameObject panelGo = new GameObject("FloatingLaptopPanel");
            if (parentTransform != null) panelGo.transform.SetParent(parentTransform, true);

            // Position at optimal VR reading distance (~0.62m from player eyes at 0.741, 1.305, -1.542)
            Vector3 panelPos = new Vector3(0.12f, 1.22f, -1.542f);
            panelGo.transform.position = panelPos;

            Vector3 playerEyes = new Vector3(0.741f, 1.305f, -1.542f);
            Vector3 toPlayer = (playerEyes - panelPos).normalized;
            // Face the seated player directly
            panelGo.transform.rotation = Quaternion.LookRotation(-toPlayer, Vector3.up);

            // High-clarity VR scale: 1920x1080 -> 0.96m wide by 0.54m tall
            float vrScale = 0.96f / 1920f; // 0.0005f
            panelGo.transform.localScale = new Vector3(vrScale, vrScale, vrScale);

            RectTransform rootRect = panelGo.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1920f, 1080f);

            Canvas canvas = panelGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 5;

            CanvasScaler scaler = panelGo.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f; // Extra crisp text rendering

            // Purely visual: No GraphicRaycaster or interaction components attached
            CanvasGroup canvasGroup = panelGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.ignoreParentGroups = true;

            // 4. Laptop Display Chassis / Bezel
            GameObject chassisGo = new GameObject("LaptopChassis");
            chassisGo.transform.SetParent(panelGo.transform, false);
            RectTransform chassisRect = chassisGo.AddComponent<RectTransform>();
            chassisRect.anchorMin = Vector2.zero;
            chassisRect.anchorMax = Vector2.one;
            chassisRect.offsetMin = new Vector2(-28f, -32f);
            chassisRect.offsetMax = new Vector2(28f, 28f);

            Image chassisImg = chassisGo.AddComponent<Image>();
            chassisImg.sprite = darkCardSprite;
            chassisImg.type = Image.Type.Sliced;
            chassisImg.color = new Color(0.09f, 0.10f, 0.12f, 1f); // Sleek dark chassis
            chassisImg.raycastTarget = false;

            // Subtle Webcam Dot on Top Bezel
            GameObject camDotGo = new GameObject("WebcamDot");
            camDotGo.transform.SetParent(chassisGo.transform, false);
            RectTransform camDotRect = camDotGo.AddComponent<RectTransform>();
            camDotRect.anchorMin = new Vector2(0.5f, 1f);
            camDotRect.anchorMax = new Vector2(0.5f, 1f);
            camDotRect.pivot = new Vector2(0.5f, 1f);
            camDotRect.anchoredPosition = new Vector2(0f, -8f);
            camDotRect.sizeDelta = new Vector2(14f, 14f);

            Image camDotImg = camDotGo.AddComponent<Image>();
            camDotImg.sprite = pillSprite;
            camDotImg.color = new Color(0.24f, 0.26f, 0.32f, 0.9f);
            camDotImg.raycastTarget = false;

            // 5. Screen Container (1920 x 1080)
            GameObject screenGo = new GameObject("ScreenContainer");
            screenGo.transform.SetParent(panelGo.transform, false);
            RectTransform screenRect = screenGo.AddComponent<RectTransform>();
            screenRect.anchorMin = Vector2.zero;
            screenRect.anchorMax = Vector2.one;
            screenRect.offsetMin = Vector2.zero;
            screenRect.offsetMax = Vector2.zero;

            Image screenBgImg = screenGo.AddComponent<Image>();
            screenBgImg.color = new Color(0.05f, 0.06f, 0.08f, 1f); // High-contrast dark background
            screenBgImg.raycastTarget = false;

            // 6. Central Moon Artwork ("to the stars who listen")
            if (moonArtwork != null)
            {
                GameObject moonGo = new GameObject("CenterMoonArtwork");
                moonGo.transform.SetParent(screenGo.transform, false);
                RectTransform moonRect = moonGo.AddComponent<RectTransform>();
                moonRect.anchorMin = new Vector2(0.5f, 0.5f);
                moonRect.anchorMax = new Vector2(0.5f, 0.5f);
                moonRect.pivot = new Vector2(0.5f, 0.5f);
                moonRect.anchoredPosition = new Vector2(-60f, 10f);
                moonRect.sizeDelta = new Vector2(760f, 425f);

                Image moonImg = moonGo.AddComponent<Image>();
                moonImg.sprite = moonArtwork;
                moonImg.preserveAspect = true;
                moonImg.color = new Color(0.90f, 0.92f, 0.98f, 0.95f);
                moonImg.raycastTarget = false;
            }

            // 7. Curated Large High-Readability Desktop Icons
            GameObject iconsContainer = new GameObject("DesktopIcons");
            iconsContainer.transform.SetParent(screenGo.transform, false);
            RectTransform iconsContRect = iconsContainer.AddComponent<RectTransform>();
            iconsContRect.anchorMin = Vector2.zero;
            iconsContRect.anchorMax = Vector2.one;
            iconsContRect.offsetMin = Vector2.zero;
            iconsContRect.offsetMax = Vector2.zero;

            // Left Column 1 (Large 72x72 icons, 22pt bold text)
            CreateDesktopIcon(iconsContainer, "This PC", iconThisPC, 40f, -40f, fontAsset, pillSprite);
            CreateDesktopIcon(iconsContainer, "Recycle Bin", iconRecycle, 40f, -170f, fontAsset, pillSprite);
            CreateDesktopIcon(iconsContainer, "Google Chrome", iconChrome, 40f, -300f, fontAsset, pillSprite);
            CreateDesktopIcon(iconsContainer, "Word", iconWord, 40f, -430f, fontAsset, pillSprite);

            // Left Column 2
            CreateDesktopIcon(iconsContainer, "Excel", iconExcel, 180f, -40f, fontAsset, pillSprite);
            CreateDesktopIcon(iconsContainer, "Telegram", iconTelegram, 180f, -170f, fontAsset, pillSprite);

            // Right Column
            CreateDesktopIcon(iconsContainer, "Unity Hub", iconUnity, -80f, -40f, fontAsset, pillSprite, true);
            CreateDesktopIcon(iconsContainer, "VS Code", iconVSCode, -80f, -170f, fontAsset, pillSprite, true);
            CreateDesktopIcon(iconsContainer, "Postman", iconPostman, -80f, -300f, fontAsset, pillSprite, true);

            // 8. Noticeable, Large Urgent Reminder Notification Card
            GameObject reminderGo = new GameObject("UrgentReminderNotification");
            reminderGo.transform.SetParent(screenGo.transform, false);
            RectTransform remRect = reminderGo.AddComponent<RectTransform>();
            remRect.anchorMin = new Vector2(0.5f, 1f);
            remRect.anchorMax = new Vector2(0.5f, 1f);
            remRect.pivot = new Vector2(0.5f, 1f);
            remRect.anchoredPosition = new Vector2(230f, -50f);
            remRect.sizeDelta = new Vector2(600f, 190f); // Enlarged & bold!

            Image remImg = reminderGo.AddComponent<Image>();
            remImg.sprite = reminderBg;
            remImg.type = Image.Type.Sliced;
            remImg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);
            remImg.raycastTarget = false;

            // Reminder Gold Left Accent Bar
            GameObject remBarGo = new GameObject("AccentBar");
            remBarGo.transform.SetParent(reminderGo.transform, false);
            RectTransform remBarRect = remBarGo.AddComponent<RectTransform>();
            remBarRect.anchorMin = new Vector2(0f, 0f);
            remBarRect.anchorMax = new Vector2(0f, 1f);
            remBarRect.pivot = new Vector2(0f, 0.5f);
            remBarRect.anchoredPosition = new Vector2(0f, 0f);
            remBarRect.sizeDelta = new Vector2(8f, 0f);

            Image remBarImg = remBarGo.AddComponent<Image>();
            remBarImg.color = new Color(1.0f, 0.72f, 0.05f, 1f); // Glowing Gold
            remBarImg.raycastTarget = false;

            // Reminder Header
            GameObject remHeadGo = new GameObject("Header");
            remHeadGo.transform.SetParent(reminderGo.transform, false);
            RectTransform remHeadRect = remHeadGo.AddComponent<RectTransform>();
            remHeadRect.anchorMin = new Vector2(0f, 1f);
            remHeadRect.anchorMax = new Vector2(1f, 1f);
            remHeadRect.pivot = new Vector2(0f, 1f);
            remHeadRect.anchoredPosition = new Vector2(28f, -16f);
            remHeadRect.sizeDelta = new Vector2(-56f, 36f);

            TextMeshProUGUI remHeadText = remHeadGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) remHeadText.font = fontAsset;
            remHeadText.text = "<color=#FFB300><b>[ ! ]  REMINDER - URGENT</b></color>   <size=22><color=#A5A5AF>Due in 20 minutes</color></size>";
            remHeadText.fontSize = 25f;
            remHeadText.alignment = TextAlignmentOptions.MidlineLeft;
            remHeadText.raycastTarget = false;

            // Reminder Title & Body
            GameObject remTitleGo = new GameObject("Title");
            remTitleGo.transform.SetParent(reminderGo.transform, false);
            RectTransform remTitleRect = remTitleGo.AddComponent<RectTransform>();
            remTitleRect.anchorMin = new Vector2(0f, 1f);
            remTitleRect.anchorMax = new Vector2(1f, 1f);
            remTitleRect.pivot = new Vector2(0f, 1f);
            remTitleRect.anchoredPosition = new Vector2(28f, -56f);
            remTitleRect.sizeDelta = new Vector2(-56f, 40f);

            TextMeshProUGUI remTitleText = remTitleGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) remTitleText.font = fontAsset;
            remTitleText.text = "Send Report File to Manager";
            remTitleText.fontSize = 30f;
            remTitleText.fontStyle = FontStyles.Bold;
            remTitleText.color = Color.white;
            remTitleText.alignment = TextAlignmentOptions.MidlineLeft;
            remTitleText.raycastTarget = false;

            GameObject remBodyGo = new GameObject("Body");
            remBodyGo.transform.SetParent(reminderGo.transform, false);
            RectTransform remBodyRect = remBodyGo.AddComponent<RectTransform>();
            remBodyRect.anchorMin = new Vector2(0f, 0f);
            remBodyRect.anchorMax = new Vector2(1f, 1f);
            remBodyRect.offsetMin = new Vector2(28f, 14f);
            remBodyRect.offsetMax = new Vector2(-28f, -100f);

            TextMeshProUGUI remBodyText = remBodyGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) remBodyText.font = fontAsset;
            remBodyText.text = "Please send the finalized report file immediately before the scheduled meeting.";
            remBodyText.fontSize = 23f;
            remBodyText.color = new Color(0.92f, 0.93f, 0.96f, 1f);
            remBodyText.alignment = TextAlignmentOptions.TopLeft;
            remBodyText.enableWordWrapping = true;
            remBodyText.raycastTarget = false;

            // 9. Extra Large & Clear Wi-Fi Flyout Panel (Bottom-Right, above taskbar)
            GameObject wifiPanelGo = new GameObject("EnlargedWifiPanel");
            wifiPanelGo.transform.SetParent(screenGo.transform, false);
            RectTransform wifiRect = wifiPanelGo.AddComponent<RectTransform>();
            wifiRect.anchorMin = new Vector2(1f, 0f);
            wifiRect.anchorMax = new Vector2(1f, 0f);
            wifiRect.pivot = new Vector2(1f, 0f);
            wifiRect.anchoredPosition = new Vector2(-24f, 84f);
            wifiRect.sizeDelta = new Vector2(580f, 490f); // Extra large & prominent!

            Image wifiImg = wifiPanelGo.AddComponent<Image>();
            wifiImg.sprite = wifiPanelBg;
            wifiImg.type = Image.Type.Sliced;
            wifiImg.color = new Color(0.12f, 0.13f, 0.16f, 0.98f);
            wifiImg.raycastTarget = false;

            // 9a. Wi-Fi Header
            GameObject wifiHeaderGo = new GameObject("WifiHeader");
            wifiHeaderGo.transform.SetParent(wifiPanelGo.transform, false);
            RectTransform wifiHeadRect = wifiHeaderGo.AddComponent<RectTransform>();
            wifiHeadRect.anchorMin = new Vector2(0f, 1f);
            wifiHeadRect.anchorMax = new Vector2(1f, 1f);
            wifiHeadRect.pivot = new Vector2(0.5f, 1f);
            wifiHeadRect.anchoredPosition = new Vector2(0f, -18f);
            wifiHeadRect.sizeDelta = new Vector2(-44f, 56f);

            TextMeshProUGUI wifiHeadText = wifiHeaderGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) wifiHeadText.font = fontAsset;
            wifiHeadText.text = "<b>←  Wi-Fi</b>";
            wifiHeadText.fontSize = 32f;
            wifiHeadText.color = Color.white;
            wifiHeadText.alignment = TextAlignmentOptions.MidlineLeft;
            wifiHeadText.raycastTarget = false;

            // Toggle Switch (ON)
            GameObject toggleGo = new GameObject("ToggleSwitch");
            toggleGo.transform.SetParent(wifiHeaderGo.transform, false);
            RectTransform toggleRect = toggleGo.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(1f, 0.5f);
            toggleRect.anchorMax = new Vector2(1f, 0.5f);
            toggleRect.pivot = new Vector2(1f, 0.5f);
            toggleRect.anchoredPosition = new Vector2(0f, 0f);
            toggleRect.sizeDelta = new Vector2(70f, 36f);

            Image toggleImg = toggleGo.AddComponent<Image>();
            toggleImg.sprite = toggleOn;
            toggleImg.type = Image.Type.Simple;
            toggleImg.raycastTarget = false;

            // 9b. Wi-Fi Network List
            GameObject listGo = new GameObject("NetworkList");
            listGo.transform.SetParent(wifiPanelGo.transform, false);
            RectTransform listRect = listGo.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0f, 0f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.offsetMin = new Vector2(22f, 68f);
            listRect.offsetMax = new Vector2(-22f, -84f);

            // Item 1: Chanrat (Connected) - Large & Highlighted
            CreateWifiItem(listGo, "Chanrat", "Connected, secured", true, 0f, fontAsset, wifiWave, darkCardSprite);
            // Item 2: Chanrat_5G
            CreateWifiItem(listGo, "Chanrat_5G", null, false, -110f, fontAsset, wifiWave, darkCardSprite);
            // Item 3: Galaxy S22 Ultra 5905
            CreateWifiItem(listGo, "Galaxy S22 Ultra 5905", null, false, -200f, fontAsset, wifiWave, darkCardSprite);

            // 9c. Wi-Fi Footer
            GameObject wifiFootGo = new GameObject("WifiFooter");
            wifiFootGo.transform.SetParent(wifiPanelGo.transform, false);
            RectTransform wifiFootRect = wifiFootGo.AddComponent<RectTransform>();
            wifiFootRect.anchorMin = new Vector2(0f, 0f);
            wifiFootRect.anchorMax = new Vector2(1f, 0f);
            wifiFootRect.pivot = new Vector2(0.5f, 0f);
            wifiFootRect.anchoredPosition = new Vector2(0f, 16f);
            wifiFootRect.sizeDelta = new Vector2(-44f, 44f);

            TextMeshProUGUI wifiFootText = wifiFootGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) wifiFootText.font = fontAsset;
            wifiFootText.text = "<size=24><color=#A8A8B2>More Wi-Fi settings</color></size>";
            wifiFootText.alignment = TextAlignmentOptions.MidlineLeft;
            wifiFootText.raycastTarget = false;

            GameObject footIconsGo = new GameObject("FooterIcons");
            footIconsGo.transform.SetParent(wifiFootGo.transform, false);
            RectTransform footIconsRect = footIconsGo.AddComponent<RectTransform>();
            footIconsRect.anchorMin = new Vector2(1f, 0.5f);
            footIconsRect.anchorMax = new Vector2(1f, 0.5f);
            footIconsRect.pivot = new Vector2(1f, 0.5f);
            footIconsRect.anchoredPosition = new Vector2(0f, 0f);
            footIconsRect.sizeDelta = new Vector2(160f, 40f);

            TextMeshProUGUI footIconsText = footIconsGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) footIconsText.font = fontAsset;
            footIconsText.text = "Settings   Refresh";
            footIconsText.fontSize = 20f;
            footIconsText.color = new Color(0.80f, 0.82f, 0.88f, 1f);
            footIconsText.alignment = TextAlignmentOptions.MidlineRight;
            footIconsText.raycastTarget = false;

            // 10. Large High-Contrast Windows 11 Taskbar
            GameObject taskbarGo = new GameObject("WindowsTaskbar");
            taskbarGo.transform.SetParent(screenGo.transform, false);
            RectTransform taskRect = taskbarGo.AddComponent<RectTransform>();
            taskRect.anchorMin = new Vector2(0f, 0f);
            taskRect.anchorMax = new Vector2(1f, 0f);
            taskRect.pivot = new Vector2(0.5f, 0f);
            taskRect.anchoredPosition = new Vector2(0f, 0f);
            taskRect.sizeDelta = new Vector2(0f, 68f);

            Image taskImg = taskbarGo.AddComponent<Image>();
            taskImg.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);
            taskImg.raycastTarget = false;

            // Taskbar Left Widget: News / Weather
            GameObject newsGo = new GameObject("NewsWidget");
            newsGo.transform.SetParent(taskbarGo.transform, false);
            RectTransform newsRect = newsGo.AddComponent<RectTransform>();
            newsRect.anchorMin = new Vector2(0f, 0.5f);
            newsRect.anchorMax = new Vector2(0f, 0.5f);
            newsRect.pivot = new Vector2(0f, 0.5f);
            newsRect.anchoredPosition = new Vector2(24f, 0f);
            newsRect.sizeDelta = new Vector2(240f, 48f);

            TextMeshProUGUI newsText = newsGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) newsText.font = fontAsset;
            newsText.text = "<b>74°F</b>  Breaking news";
            newsText.fontSize = 22f;
            newsText.color = new Color(0.92f, 0.93f, 0.96f, 1f);
            newsText.alignment = TextAlignmentOptions.MidlineLeft;
            newsText.raycastTarget = false;

            // Taskbar Center Icons
            GameObject centerIconsGo = new GameObject("CenterIcons");
            centerIconsGo.transform.SetParent(taskbarGo.transform, false);
            RectTransform centerIconsRect = centerIconsGo.AddComponent<RectTransform>();
            centerIconsRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerIconsRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerIconsRect.pivot = new Vector2(0.5f, 0.5f);
            centerIconsRect.anchoredPosition = new Vector2(0f, 0f);
            centerIconsRect.sizeDelta = new Vector2(640f, 54f);

            // Windows Start Logo
            CreateTaskbarIcon(centerIconsGo, iconThisPC, -270f);
            // Search Bar Pill
            CreateTaskbarSearchPill(centerIconsGo, -160f, pillSprite, fontAsset);
            // App Icons in taskbar
            CreateTaskbarIcon(centerIconsGo, iconChrome, 0f);
            CreateTaskbarIcon(centerIconsGo, iconTelegram, 56f);
            CreateTaskbarIcon(centerIconsGo, iconWord, 112f);
            CreateTaskbarIcon(centerIconsGo, iconExcel, 168f);
            CreateTaskbarIcon(centerIconsGo, iconVSCode, 224f);
            CreateTaskbarIcon(centerIconsGo, iconUnity, 280f);

            // Taskbar Right System Tray
            GameObject trayGo = new GameObject("SystemTray");
            trayGo.transform.SetParent(taskbarGo.transform, false);
            RectTransform trayRect = trayGo.AddComponent<RectTransform>();
            trayRect.anchorMin = new Vector2(1f, 0.5f);
            trayRect.anchorMax = new Vector2(1f, 0.5f);
            trayRect.pivot = new Vector2(1f, 0.5f);
            trayRect.anchoredPosition = new Vector2(-24f, 0f);
            trayRect.sizeDelta = new Vector2(340f, 48f);

            TextMeshProUGUI trayText = trayGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) trayText.font = fontAsset;
            trayText.text = "^  ENG  Wi-Fi  Vol  92%  <b>1:07 AM\n9/9/2026</b>";
            trayText.fontSize = 20f;
            trayText.color = new Color(0.95f, 0.95f, 0.98f, 1f);
            trayText.alignment = TextAlignmentOptions.MidlineRight;
            trayText.lineSpacing = -15f;
            trayText.raycastTarget = false;

            // 11. Save Scene and update Prefab
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            PrefabUtility.SaveAsPrefabAssetAndConnect(panelGo, PrefabPath, InteractionMode.AutomatedAction);

            Debug.Log("[LaptopPanel] Standalone Floating Laptop Panel (Enhanced Readability) successfully built and saved!");
        }

        private static void CreateDesktopIcon(GameObject parent, string name, Sprite iconSprite, float posX, float posY, TMP_FontAsset fontAsset, Sprite bgSprite, bool rightAlign = false)
        {
            GameObject iconGo = new GameObject("Icon_" + name);
            iconGo.transform.SetParent(parent.transform, false);
            RectTransform rect = iconGo.AddComponent<RectTransform>();

            if (rightAlign)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }

            rect.anchoredPosition = new Vector2(posX, posY);
            rect.sizeDelta = new Vector2(110f, 116f);

            // Icon Image (Enlarged 72x72)
            GameObject imgGo = new GameObject("Image");
            imgGo.transform.SetParent(iconGo.transform, false);
            RectTransform imgRect = imgGo.AddComponent<RectTransform>();
            imgRect.anchorMin = new Vector2(0.5f, 1f);
            imgRect.anchorMax = new Vector2(0.5f, 1f);
            imgRect.pivot = new Vector2(0.5f, 1f);
            imgRect.anchoredPosition = new Vector2(0f, 0f);
            imgRect.sizeDelta = new Vector2(72f, 72f);

            Image img = imgGo.AddComponent<Image>();
            if (iconSprite != null) img.sprite = iconSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            // Label Container
            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(iconGo.transform, false);
            RectTransform labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 0f);
            labelRect.sizeDelta = new Vector2(0f, 40f);

            if (bgSprite != null)
            {
                Image txtBg = labelGo.AddComponent<Image>();
                txtBg.sprite = bgSprite;
                txtBg.type = Image.Type.Sliced;
                txtBg.color = new Color(0f, 0f, 0f, 0.55f); // Subtle dark backplate
                txtBg.raycastTarget = false;
            }

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(labelGo.transform, false);
            RectTransform txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(4f, 2f);
            txtRect.offsetMax = new Vector2(-4f, -2f);

            TextMeshProUGUI tmp = txtGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.text = name;
            tmp.fontSize = 19f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
        }

        private static void CreateWifiItem(GameObject parent, string name, string subtext, bool isConnected, float posY, TMP_FontAsset fontAsset, Sprite wifiWave, Sprite darkCard)
        {
            GameObject itemGo = new GameObject("Item_" + name);
            itemGo.transform.SetParent(parent.transform, false);
            RectTransform rect = itemGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, posY);
            rect.sizeDelta = new Vector2(0f, isConnected ? 96f : 80f);

            if (isConnected)
            {
                Image bg = itemGo.AddComponent<Image>();
                bg.sprite = darkCard;
                bg.type = Image.Type.Sliced;
                bg.color = new Color(0.18f, 0.24f, 0.32f, 0.95f); // Highlighted active background
                bg.raycastTarget = false;
            }

            // Wi-Fi Icon (Enlarged 40x40)
            GameObject waveGo = new GameObject("WifiIcon");
            waveGo.transform.SetParent(itemGo.transform, false);
            RectTransform waveRect = waveGo.AddComponent<RectTransform>();
            waveRect.anchorMin = new Vector2(0f, 0.5f);
            waveRect.anchorMax = new Vector2(0f, 0.5f);
            waveRect.pivot = new Vector2(0f, 0.5f);
            waveRect.anchoredPosition = new Vector2(20f, 0f);
            waveRect.sizeDelta = new Vector2(40f, 40f);

            Image waveImg = waveGo.AddComponent<Image>();
            if (wifiWave != null) waveImg.sprite = wifiWave;
            waveImg.color = isConnected ? new Color(0.35f, 0.80f, 1.0f, 1f) : Color.white;
            waveImg.preserveAspect = true;
            waveImg.raycastTarget = false;

            // Name & Subtext
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(itemGo.transform, false);
            RectTransform textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(76f, 4f);
            textRect.offsetMax = new Vector2(-18f, -4f);

            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) tmp.font = fontAsset;
            if (isConnected)
            {
                tmp.text = $"<b>{name}</b>\n<size=22><color=#62CCFF>{subtext}</color></size>";
                tmp.fontSize = 28f;
            }
            else
            {
                tmp.text = $"<b>{name}</b>";
                tmp.fontSize = 26f;
            }
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }

        private static void CreateTaskbarIcon(GameObject parent, Sprite iconSprite, float posX)
        {
            GameObject iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(parent.transform, false);
            RectTransform rect = iconGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(posX, 0f);
            rect.sizeDelta = new Vector2(44f, 44f);

            Image img = iconGo.AddComponent<Image>();
            if (iconSprite != null) img.sprite = iconSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        private static void CreateTaskbarSearchPill(GameObject parent, float posX, Sprite pillSprite, TMP_FontAsset fontAsset)
        {
            GameObject pillGo = new GameObject("SearchPill");
            pillGo.transform.SetParent(parent.transform, false);
            RectTransform rect = pillGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(posX, 0f);
            rect.sizeDelta = new Vector2(150f, 44f);

            Image img = pillGo.AddComponent<Image>();
            img.sprite = pillSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.18f, 0.20f, 0.25f, 1f);
            img.raycastTarget = false;

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(pillGo.transform, false);
            RectTransform txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(20f, 0f);
            txtRect.offsetMax = new Vector2(-16f, 0f);

            TextMeshProUGUI tmp = txtGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.text = "Search";
            tmp.fontSize = 20f;
            tmp.color = new Color(0.80f, 0.82f, 0.88f, 1f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }

        private static void ImportAllTextures()
        {
            string[] files = Directory.GetFiles(TexturesFolder, "*.png");
            foreach (var f in files)
            {
                string rel = f.Replace("\\", "/");
                var importer = AssetImporter.GetAtPath(rel) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
        }

        private static Sprite GetOrCreateWifiPanelSprite()
        {
            string path = TexturesFolder + "UI_WifiPanel_Bg.png";
            if (File.Exists(path))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null) return s;
            }

            int w = 256;
            int h = 256;
            float radius = 28f;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = 0f;
                    bool inCorner = false;
                    if (x < radius && y >= h - radius) { inCorner = true; float dx = radius - x; float dy = y - (h - radius - 1); dist = Mathf.Sqrt(dx * dx + dy * dy); }
                    else if (x >= w - radius && y >= h - radius) { inCorner = true; float dx = x - (w - radius - 1); float dy = y - (h - radius - 1); dist = Mathf.Sqrt(dx * dx + dy * dy); }
                    else if (x < radius && y < radius) { inCorner = true; float dx = radius - x; float dy = radius - y; dist = Mathf.Sqrt(dx * dx + dy * dy); }
                    else if (x >= w - radius && y < radius) { inCorner = true; float dx = x - (w - radius - 1); float dy = radius - y; dist = Mathf.Sqrt(dx * dx + dy * dy); }

                    float alpha = 1f;
                    if (inCorner) { float edgeDist = dist - radius; alpha = Mathf.Clamp01(0.5f - edgeDist); }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(28, 28, 28, 28);
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite GetOrCreateReminderSprite()
        {
            string path = TexturesFolder + "UI_Reminder_Bg.png";
            if (File.Exists(path))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null) return s;
            }

            int w = 256;
            int h = 256;
            float radius = 24f;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = 0f;
                    bool inCorner = false;
                    if (x < radius && y >= h - radius) { inCorner = true; float dx = radius - x; float dy = y - (h - radius - 1); dist = Mathf.Sqrt(dx * dx + dy * dy); }
                    else if (x >= w - radius && y >= h - radius) { inCorner = true; float dx = x - (w - radius - 1); float dy = y - (h - radius - 1); dist = Mathf.Sqrt(dx * dx + dy * dy); }
                    else if (x < radius && y < radius) { inCorner = true; float dx = radius - x; float dy = radius - y; dist = Mathf.Sqrt(dx * dx + dy * dy); }
                    else if (x >= w - radius && y < radius) { inCorner = true; float dx = x - (w - radius - 1); float dy = radius - y; dist = Mathf.Sqrt(dx * dx + dy * dy); }

                    float alpha = 1f;
                    if (inCorner) { float edgeDist = dist - radius; alpha = Mathf.Clamp01(0.5f - edgeDist); }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(24, 24, 24, 24);
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite GetOrCreateToggleSprite()
        {
            string path = TexturesFolder + "UI_Toggle_On.png";
            if (File.Exists(path))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null) return s;
            }

            int w = 128;
            int h = 64;
            float r = 32f;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Pill shape background (Blue #0078D4)
                    float dist = 0f;
                    bool inCap = false;
                    if (x < r) { inCap = true; float dx = r - x; float dy = y - r; dist = Mathf.Sqrt(dx * dx + dy * dy); }
                    else if (x >= w - r) { inCap = true; float dx = x - (w - r); float dy = y - r; dist = Mathf.Sqrt(dx * dx + dy * dy); }

                    float pillAlpha = 1f;
                    if (inCap) { float edgeDist = dist - r; pillAlpha = Mathf.Clamp01(0.5f - edgeDist); }

                    Color col = new Color(0.0f, 0.47f, 0.83f, pillAlpha); // Windows Blue

                    // Knob circle on the right (center at x = w - r, y = r, radius = 22)
                    float kdx = x - (w - r);
                    float kdy = y - r;
                    float kdist = Mathf.Sqrt(kdx * kdx + kdy * kdy);
                    if (kdist < 23f)
                    {
                        float knobAlpha = Mathf.Clamp01(23.5f - kdist);
                        col = Color.Lerp(col, Color.white, knobAlpha);
                    }

                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite GetOrCreateWifiWaveSprite()
        {
            string path = TexturesFolder + "UI_WifiWave_Icon.png";
            if (File.Exists(path))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null) return s;
            }

            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float cx = 32f;
            float cy = 12f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, Mathf.Abs(dx)) * Mathf.Rad2Deg;

                    float alpha = 0f;
                    if (angle > 20f)
                    {
                        // Dot at bottom (dist < 6)
                        if (dist < 6f) alpha = 1f;
                        // Wave 1: dist 14..20
                        else if (dist >= 14f && dist <= 20f) alpha = 1f;
                        // Wave 2: dist 28..34
                        else if (dist >= 28f && dist <= 34f) alpha = 1f;
                        // Wave 3: dist 42..48
                        else if (dist >= 42f && dist <= 48f) alpha = 1f;
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
