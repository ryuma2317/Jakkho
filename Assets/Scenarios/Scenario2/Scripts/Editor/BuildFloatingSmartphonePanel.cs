using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

namespace Kannko.Editor
{
    public static class BuildFloatingSmartphonePanel
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";
        private const string PrefabPath = "Assets/Scenarios/Scenario2/Prefabs/UI/FloatingSmartphonePanel.prefab";
        private const string TexturesFolder = "Assets/Scenarios/Scenario2/Textures/";

        [MenuItem("VR Prototype/Build Floating Smartphone Panel", priority = 9)]
        public static void BuildPanel()
        {
            Debug.Log("[FloatingPanel] Building standalone floating smartphone UI panel...");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[FloatingPanel] Scene not found: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Locate WorldUI parent (or scene root)
            GameObject worldUiGo = GameObject.Find("WorldUI");
            Transform parentTransform = worldUiGo != null ? worldUiGo.transform : null;

            // Clean up old panel if re-running
            GameObject existingPanel = GameObject.Find("FloatingSmartphonePanel");
            if (existingPanel != null) Object.DestroyImmediate(existingPanel);

            // Load assets
            Sprite wallpaperSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "T_PhoneLockscreen_Wallpaper.png");
            Sprite darkCardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "UI_DarkCard_Bg.png");
            Sprite pillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "UI_Pill_Bg.png");
            Sprite thumbnailSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "UI_ThumbnailAttachment.png");
            Sprite telegramIcon = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "Telegram_Icon.png");
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            // 1. Create Root Floating Panel GameObject (Standalone World Space Canvas)
            GameObject panelGo = new GameObject("FloatingSmartphonePanel");
            if (parentTransform != null) panelGo.transform.SetParent(parentTransform, true);

            // Position comfortably in front of seated player (Player at ~0.74, 1.30, -1.54 looking West towards -X)
            // Panel floating naturally at (0.05, 1.15, -1.82), angled comfortably towards player eyes
            Vector3 panelPos = new Vector3(0.05f, 1.15f, -1.82f);
            panelGo.transform.position = panelPos;

            Vector3 playerEyes = new Vector3(0.741f, 1.305f, -1.542f);
            Vector3 toPlayer = (playerEyes - panelPos).normalized;
            // Face the player
            panelGo.transform.rotation = Quaternion.LookRotation(toPlayer, Vector3.up);

            // Dimensions: 720 x 1560 at comfortable VR handheld/floating scale (0.28m wide by 0.60m tall)
            float vrScale = 0.28f / 720f;
            panelGo.transform.localScale = new Vector3(vrScale, vrScale, vrScale);

            RectTransform rootRect = panelGo.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(720f, 1560f);

            Canvas canvas = panelGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 5;

            CanvasScaler scaler = panelGo.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            panelGo.AddComponent<GraphicRaycaster>();

            // Add TrackedDeviceGraphicRaycaster for VR interaction
            var trackedRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (trackedRaycasterType != null)
            {
                panelGo.AddComponent(trackedRaycasterType);
            }

            CanvasGroup canvasGroup = panelGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.ignoreParentGroups = true;

            // 2. Outer Phone Chassis / Bezel (Rounded dark border)
            GameObject chassisGo = new GameObject("PhoneChassis");
            chassisGo.transform.SetParent(panelGo.transform, false);
            RectTransform chassisRect = chassisGo.AddComponent<RectTransform>();
            chassisRect.anchorMin = Vector2.zero;
            chassisRect.anchorMax = Vector2.one;
            chassisRect.offsetMin = new Vector2(-16f, -16f);
            chassisRect.offsetMax = new Vector2(16f, 16f);

            Image chassisImg = chassisGo.AddComponent<Image>();
            chassisImg.sprite = darkCardSprite;
            chassisImg.type = Image.Type.Sliced;
            chassisImg.color = new Color(0.12f, 0.13f, 0.16f, 0.98f);
            chassisImg.raycastTarget = false;

            // 3. Screen Container (Masked with rounded corners)
            GameObject screenGo = new GameObject("ScreenContainer");
            screenGo.transform.SetParent(panelGo.transform, false);
            RectTransform screenRect = screenGo.AddComponent<RectTransform>();
            screenRect.anchorMin = Vector2.zero;
            screenRect.anchorMax = Vector2.one;
            screenRect.offsetMin = Vector2.zero;
            screenRect.offsetMax = Vector2.zero;

            Image screenMaskImg = screenGo.AddComponent<Image>();
            screenMaskImg.sprite = darkCardSprite;
            screenMaskImg.type = Image.Type.Sliced;
            screenMaskImg.color = Color.white;

            Mask mask = screenGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // 4. Wallpaper Background
            GameObject bgGo = new GameObject("Wallpaper");
            bgGo.transform.SetParent(screenGo.transform, false);
            RectTransform bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = wallpaperSprite;
            bgImg.type = Image.Type.Simple;
            bgImg.color = Color.white;
            bgImg.raycastTarget = false;

            // 5. Status Bar
            GameObject statusBarGo = new GameObject("StatusBar");
            statusBarGo.transform.SetParent(screenGo.transform, false);
            RectTransform statusRect = statusBarGo.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0f, -20f);
            statusRect.sizeDelta = new Vector2(0f, 50f);

            // Carrier: "Cellcard"
            GameObject carrierGo = new GameObject("CarrierText");
            carrierGo.transform.SetParent(statusBarGo.transform, false);
            RectTransform carrierRect = carrierGo.AddComponent<RectTransform>();
            carrierRect.anchorMin = new Vector2(0f, 0.5f);
            carrierRect.anchorMax = new Vector2(0f, 0.5f);
            carrierRect.pivot = new Vector2(0f, 0.5f);
            carrierRect.anchoredPosition = new Vector2(36f, 0f);
            carrierRect.sizeDelta = new Vector2(200f, 40f);

            TextMeshProUGUI carrierText = carrierGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) carrierText.font = fontAsset;
            carrierText.text = "Cellcard";
            carrierText.fontSize = 28f;
            carrierText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            carrierText.alignment = TextAlignmentOptions.MidlineLeft;
            carrierText.raycastTarget = false;

            // Status Icons: NFC, Wi-Fi, Signal, Battery "87"
            GameObject iconsGo = new GameObject("StatusIcons");
            iconsGo.transform.SetParent(statusBarGo.transform, false);
            RectTransform iconsRect = iconsGo.AddComponent<RectTransform>();
            iconsRect.anchorMin = new Vector2(1f, 0.5f);
            iconsRect.anchorMax = new Vector2(1f, 0.5f);
            iconsRect.pivot = new Vector2(1f, 0.5f);
            iconsRect.anchoredPosition = new Vector2(-36f, 0f);
            iconsRect.sizeDelta = new Vector2(260f, 40f);

            TextMeshProUGUI iconsText = iconsGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) iconsText.font = fontAsset;
            iconsText.text = "<b>N</b>   <size=22>●))</size>   <size=22>▲</size>   <size=24>[ 87 ]</size>";
            iconsText.fontSize = 24f;
            iconsText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            iconsText.alignment = TextAlignmentOptions.MidlineRight;
            iconsText.raycastTarget = false;

            // 6. Time & Date Section
            GameObject timeGo = new GameObject("TimeDateSection");
            timeGo.transform.SetParent(screenGo.transform, false);
            RectTransform timeRect = timeGo.AddComponent<RectTransform>();
            timeRect.anchorMin = new Vector2(0f, 1f);
            timeRect.anchorMax = new Vector2(1f, 1f);
            timeRect.pivot = new Vector2(0f, 1f);
            timeRect.anchoredPosition = new Vector2(36f, -110f);
            timeRect.sizeDelta = new Vector2(648f, 90f);

            GameObject clockGo = new GameObject("ClockText");
            clockGo.transform.SetParent(timeGo.transform, false);
            RectTransform clockRect = clockGo.AddComponent<RectTransform>();
            clockRect.anchorMin = new Vector2(0f, 0.5f);
            clockRect.anchorMax = new Vector2(0f, 0.5f);
            clockRect.pivot = new Vector2(0f, 0.5f);
            clockRect.anchoredPosition = new Vector2(0f, 0f);
            clockRect.sizeDelta = new Vector2(210f, 80f);

            TextMeshProUGUI clockText = clockGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) clockText.font = fontAsset;
            clockText.text = "21:07";
            clockText.fontSize = 72f;
            clockText.fontStyle = FontStyles.Bold;
            clockText.color = Color.white;
            clockText.alignment = TextAlignmentOptions.MidlineLeft;
            clockText.raycastTarget = false;

            GameObject dateGo = new GameObject("DateText");
            dateGo.transform.SetParent(timeGo.transform, false);
            RectTransform dateRect = dateGo.AddComponent<RectTransform>();
            dateRect.anchorMin = new Vector2(0f, 0.5f);
            dateRect.anchorMax = new Vector2(0f, 0.5f);
            dateRect.pivot = new Vector2(0f, 0.5f);
            dateRect.anchoredPosition = new Vector2(220f, -4f);
            dateRect.sizeDelta = new Vector2(300f, 60f);

            TextMeshProUGUI dateText = dateGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) dateText.font = fontAsset;
            dateText.text = "Mon 7 Sept";
            dateText.fontSize = 36f;
            dateText.fontStyle = FontStyles.Normal;
            dateText.color = new Color(0.90f, 0.90f, 0.92f, 1f);
            dateText.alignment = TextAlignmentOptions.MidlineLeft;
            dateText.raycastTarget = false;

            // 7. Notification Card
            GameObject notifGo = new GameObject("NotificationCard");
            notifGo.transform.SetParent(screenGo.transform, false);
            RectTransform notifRect = notifGo.AddComponent<RectTransform>();
            notifRect.anchorMin = new Vector2(0.5f, 1f);
            notifRect.anchorMax = new Vector2(0.5f, 1f);
            notifRect.pivot = new Vector2(0.5f, 1f);
            notifRect.anchoredPosition = new Vector2(0f, -230f);
            notifRect.sizeDelta = new Vector2(660f, 150f);

            Image notifImg = notifGo.AddComponent<Image>();
            notifImg.sprite = darkCardSprite;
            notifImg.type = Image.Type.Sliced;
            notifImg.color = new Color(0.18f, 0.20f, 0.24f, 0.92f);

            Button notifBtn = notifGo.AddComponent<Button>();
            ColorBlock cb = notifBtn.colors;
            cb.highlightedColor = new Color(0.24f, 0.27f, 0.32f, 0.95f);
            cb.pressedColor = new Color(0.14f, 0.16f, 0.20f, 0.98f);
            notifBtn.colors = cb;

            // Notification App Icon (Telegram)
            GameObject appIconGo = new GameObject("AppIcon");
            appIconGo.transform.SetParent(notifGo.transform, false);
            RectTransform appIconRect = appIconGo.AddComponent<RectTransform>();
            appIconRect.anchorMin = new Vector2(0f, 0.5f);
            appIconRect.anchorMax = new Vector2(0f, 0.5f);
            appIconRect.pivot = new Vector2(0f, 0.5f);
            appIconRect.anchoredPosition = new Vector2(22f, 0f);
            appIconRect.sizeDelta = new Vector2(80f, 80f);

            Image appIconImg = appIconGo.AddComponent<Image>();
            if (telegramIcon != null) appIconImg.sprite = telegramIcon;
            appIconImg.preserveAspect = true;
            appIconImg.raycastTarget = false;

            // Notification Content Area
            GameObject contentGo = new GameObject("NotifContent");
            contentGo.transform.SetParent(notifGo.transform, false);
            RectTransform contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(118f, 16f);
            contentRect.offsetMax = new Vector2(-120f, -16f);

            // Title + Timestamp
            GameObject titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(contentGo.transform, false);
            RectTransform titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, 0f);
            titleRect.sizeDelta = new Vector2(0f, 40f);

            TextMeshProUGUI titleText = titleGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) titleText.font = fontAsset;
            titleText.text = "<b>Telegram Update</b>   <size=22><color=#8E8E93>18:07</color></size>";
            titleText.fontSize = 28f;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.overflowMode = TextOverflowModes.Ellipsis;
            titleText.raycastTarget = false;

            // Message Preview
            GameObject msgGo = new GameObject("PreviewText");
            msgGo.transform.SetParent(contentGo.transform, false);
            RectTransform msgRect = msgGo.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0f, 0f);
            msgRect.anchorMax = new Vector2(1f, 1f);
            msgRect.offsetMin = new Vector2(0f, 0f);
            msgRect.offsetMax = new Vector2(0f, -44f);

            TextMeshProUGUI msgText = msgGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) msgText.font = fontAsset;
            msgText.text = "Please send me the report file within 20 mins...";
            msgText.fontSize = 25f;
            msgText.color = new Color(0.80f, 0.82f, 0.86f, 1f);
            msgText.alignment = TextAlignmentOptions.TopLeft;
            msgText.overflowMode = TextOverflowModes.Ellipsis;
            msgText.raycastTarget = false;

            // Attachment Thumbnail
            GameObject thumbGo = new GameObject("Thumbnail");
            thumbGo.transform.SetParent(notifGo.transform, false);
            RectTransform thumbRect = thumbGo.AddComponent<RectTransform>();
            thumbRect.anchorMin = new Vector2(1f, 0.5f);
            thumbRect.anchorMax = new Vector2(1f, 0.5f);
            thumbRect.pivot = new Vector2(1f, 0.5f);
            thumbRect.anchoredPosition = new Vector2(-54f, 0f);
            thumbRect.sizeDelta = new Vector2(58f, 72f);

            Image thumbImg = thumbGo.AddComponent<Image>();
            thumbImg.sprite = thumbnailSprite;
            thumbImg.type = Image.Type.Simple;
            thumbImg.raycastTarget = false;

            // Expand Chevron
            GameObject chevGo = new GameObject("Chevron");
            chevGo.transform.SetParent(notifGo.transform, false);
            RectTransform chevRect = chevGo.AddComponent<RectTransform>();
            chevRect.anchorMin = new Vector2(1f, 0.5f);
            chevRect.anchorMax = new Vector2(1f, 0.5f);
            chevRect.pivot = new Vector2(1f, 0.5f);
            chevRect.anchoredPosition = new Vector2(-16f, 0f);
            chevRect.sizeDelta = new Vector2(30f, 30f);

            TextMeshProUGUI chevText = chevGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) chevText.font = fontAsset;
            chevText.text = "<b>v</b>";
            chevText.fontSize = 24f;
            chevText.color = new Color(0.60f, 0.62f, 0.66f, 1f);
            chevText.alignment = TextAlignmentOptions.Center;
            chevText.raycastTarget = false;

            // 8. Bottom Control Buttons
            // Left Button: "Notification settings"
            GameObject notifSetBtnGo = new GameObject("NotificationSettingsButton");
            notifSetBtnGo.transform.SetParent(screenGo.transform, false);
            RectTransform setRect = notifSetBtnGo.AddComponent<RectTransform>();
            setRect.anchorMin = new Vector2(0f, 0f);
            setRect.anchorMax = new Vector2(0f, 0f);
            setRect.pivot = new Vector2(0f, 0f);
            setRect.anchoredPosition = new Vector2(36f, 80f);
            setRect.sizeDelta = new Vector2(330f, 74f);

            Image setImg = notifSetBtnGo.AddComponent<Image>();
            setImg.sprite = pillSprite;
            setImg.type = Image.Type.Sliced;
            setImg.color = new Color(0.16f, 0.18f, 0.22f, 0.90f);

            Button setBtn = notifSetBtnGo.AddComponent<Button>();
            setBtn.colors = cb;

            GameObject setTextGo = new GameObject("Text");
            setTextGo.transform.SetParent(notifSetBtnGo.transform, false);
            RectTransform setTextRect = setTextGo.AddComponent<RectTransform>();
            setTextRect.anchorMin = Vector2.zero;
            setTextRect.anchorMax = Vector2.one;
            setTextRect.offsetMin = Vector2.zero;
            setTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI setTmp = setTextGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) setTmp.font = fontAsset;
            setTmp.text = "Notification settings";
            setTmp.fontSize = 26f;
            setTmp.color = new Color(0.92f, 0.92f, 0.95f, 1f);
            setTmp.alignment = TextAlignmentOptions.Center;
            setTmp.raycastTarget = false;

            // Right Button: "Clear"
            GameObject clearBtnGo = new GameObject("ClearButton");
            clearBtnGo.transform.SetParent(screenGo.transform, false);
            RectTransform clearRect = clearBtnGo.AddComponent<RectTransform>();
            clearRect.anchorMin = new Vector2(1f, 0f);
            clearRect.anchorMax = new Vector2(1f, 0f);
            clearRect.pivot = new Vector2(1f, 0f);
            clearRect.anchoredPosition = new Vector2(-36f, 80f);
            clearRect.sizeDelta = new Vector2(170f, 74f);

            Image clearImg = clearBtnGo.AddComponent<Image>();
            clearImg.sprite = pillSprite;
            clearImg.type = Image.Type.Sliced;
            clearImg.color = new Color(0.16f, 0.18f, 0.22f, 0.90f);

            Button clearBtn = clearBtnGo.AddComponent<Button>();
            clearBtn.colors = cb;

            GameObject clearTextGo = new GameObject("Text");
            clearTextGo.transform.SetParent(clearBtnGo.transform, false);
            RectTransform clearTextRect = clearTextGo.AddComponent<RectTransform>();
            clearTextRect.anchorMin = Vector2.zero;
            clearTextRect.anchorMax = Vector2.one;
            clearTextRect.offsetMin = Vector2.zero;
            clearTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI clearTmp = clearTextGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) clearTmp.font = fontAsset;
            clearTmp.text = "Clear";
            clearTmp.fontSize = 26f;
            clearTmp.color = new Color(0.92f, 0.92f, 0.95f, 1f);
            clearTmp.alignment = TextAlignmentOptions.Center;
            clearTmp.raycastTarget = false;

            // 9. Home Swipe Indicator
            GameObject homeGo = new GameObject("HomeIndicator");
            homeGo.transform.SetParent(screenGo.transform, false);
            RectTransform homeRect = homeGo.AddComponent<RectTransform>();
            homeRect.anchorMin = new Vector2(0.5f, 0f);
            homeRect.anchorMax = new Vector2(0.5f, 0f);
            homeRect.pivot = new Vector2(0.5f, 0f);
            homeRect.anchoredPosition = new Vector2(0f, 16f);
            homeRect.sizeDelta = new Vector2(240f, 8f);

            Image homeImg = homeGo.AddComponent<Image>();
            homeImg.sprite = pillSprite;
            homeImg.type = Image.Type.Sliced;
            homeImg.color = new Color(0.90f, 0.90f, 0.95f, 0.55f);
            homeImg.raycastTarget = false;

            // 10. Save Scene and Prefab
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            PrefabUtility.SaveAsPrefabAssetAndConnect(panelGo, PrefabPath, InteractionMode.AutomatedAction);

            Debug.Log("[FloatingPanel] Standalone Floating Smartphone Panel successfully built and saved!");
        }
    }
}
