using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using Kannko.Phone;

namespace Kannko.Editor
{
    public static class BuildPhoneLockscreenUI
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";
        private const string PrefabPath = "Assets/Scenarios/Scenario2/Prefabs/Interactive/Smartphone.prefab";
        private const string TexturesFolder = "Assets/Scenarios/Scenario2/Textures/";

        [MenuItem("VR Prototype/Build Phone Lockscreen UI", priority = 8)]
        public static void BuildUI()
        {
            Debug.Log("[PhoneLockscreen] Starting Phone Lockscreen UI construction...");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[PhoneLockscreen] Scene not found: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Locate Smartphone
            GameObject smartphoneGo = GameObject.Find("Smartphone");
            if (smartphoneGo == null)
            {
                Debug.LogError("[PhoneLockscreen] Smartphone object not found in scene!");
                return;
            }

            // 2. Generate or load textures
            Sprite wallpaperSprite = GetOrCreateWallpaperSprite();
            Sprite darkCardSprite = GetOrCreateDarkCardSprite();
            Sprite pillSprite = GetOrCreatePillSprite();
            Sprite thumbnailSprite = GetOrCreateThumbnailSprite();
            Sprite telegramIcon = AssetDatabase.LoadAssetAtPath<Sprite>(TexturesFolder + "Telegram_Icon.png");
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            // 3. Clean up any existing screen canvas, displays, or floating billboard on smartphone
            Transform oldCanvas = smartphoneGo.transform.Find("PhoneScreenCanvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas.gameObject);

            Transform oldDisplay = smartphoneGo.transform.Find("Phone_Screen_Display");
            if (oldDisplay != null) Object.DestroyImmediate(oldDisplay.gameObject);

            Transform floatingPopup = smartphoneGo.transform.Find("TelegramMessagePopup");
            if (floatingPopup != null) floatingPopup.gameObject.SetActive(false);

            // 4. Create World Space Canvas attached directly to the phone's physical screen
            GameObject canvasGo = new GameObject("PhoneScreenCanvas");
            canvasGo.transform.SetParent(smartphoneGo.transform, false);

            // Position right on top of the phone screen mesh (Y = 0.0052m)
            // Rotating X = 90 makes Canvas top (+Y) align with phone top (+Z / Notch) and face upwards (+Y)
            canvasGo.transform.localPosition = new Vector3(0f, 0.0052f, 0f);
            canvasGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Physical screen size: 0.070m wide by 0.150m tall
            // Resolution: 720 x 1560 px
            float scaleX = 0.070f / 720f;
            float scaleY = 0.150f / 1560f;
            canvasGo.transform.localScale = new Vector3(scaleX, scaleY, scaleX);

            RectTransform canvasRect = canvasGo.AddComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(720f, 1560f);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            canvasGo.AddComponent<GraphicRaycaster>();
            
            // Add TrackedDeviceGraphicRaycaster for VR pointer / poke if available
            var trackedRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (trackedRaycasterType != null)
            {
                canvasGo.AddComponent(trackedRaycasterType);
            }

            CanvasGroup canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.ignoreParentGroups = true; // Stay visible regardless of phone resting state

            // 5. Build UI Elements

            // 5a. Wallpaper Background
            GameObject bgGo = new GameObject("Wallpaper");
            bgGo.transform.SetParent(canvasGo.transform, false);
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

            // 5b. Status Bar (Top: Carrier & Icons)
            GameObject statusBarGo = new GameObject("StatusBar");
            statusBarGo.transform.SetParent(canvasGo.transform, false);
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

            // 5c. Time & Date Section
            GameObject timeGo = new GameObject("TimeDateSection");
            timeGo.transform.SetParent(canvasGo.transform, false);
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

            // 5d. Notification Card
            GameObject notifGo = new GameObject("NotificationCard");
            notifGo.transform.SetParent(canvasGo.transform, false);
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

            // 5e. Bottom Control Buttons
            // Left Button: "Notification settings"
            GameObject notifSetBtnGo = new GameObject("NotificationSettingsButton");
            notifSetBtnGo.transform.SetParent(canvasGo.transform, false);
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
            clearBtnGo.transform.SetParent(canvasGo.transform, false);
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

            // 5f. Home Swipe Indicator
            GameObject homeGo = new GameObject("HomeIndicator");
            homeGo.transform.SetParent(canvasGo.transform, false);
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

            // 6. Save Scene and update Prefab
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            PrefabUtility.SaveAsPrefabAssetAndConnect(smartphoneGo, PrefabPath, InteractionMode.AutomatedAction);

            Debug.Log("[PhoneLockscreen] Phone Lockscreen UI successfully built and saved!");
        }

        private struct BokehLight
        {
            public float x, y, r;
            public Color color;
            public BokehLight(float _x, float _y, float _r, Color _c) { x = _x; y = _y; r = _r; color = _c; }
        }

        private static Sprite GetOrCreateWallpaperSprite()
        {
            string path = TexturesFolder + "T_PhoneLockscreen_Wallpaper.png";
            if (File.Exists(path))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null) return s;
            }

            int w = 720;
            int h = 1560;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            BokehLight[] bokehs = new BokehLight[]
            {
                new BokehLight(110f, 420f, 110f, new Color(0.85f, 0.88f, 0.95f, 0.55f)), // White/silver
                new BokehLight(270f, 420f, 115f, new Color(0.88f, 0.22f, 0.25f, 0.50f)), // Red
                new BokehLight(450f, 420f, 120f, new Color(0.55f, 0.12f, 0.85f, 0.50f)), // Purple
                new BokehLight(610f, 420f, 110f, new Color(0.20f, 0.65f, 0.88f, 0.45f)), // Cyan
                new BokehLight(110f, 210f, 110f, new Color(0.12f, 0.72f, 0.32f, 0.50f)), // Green
                new BokehLight(270f, 210f, 110f, new Color(0.22f, 0.45f, 0.85f, 0.45f)), // Soft Blue
                new BokehLight(450f, 210f, 115f, new Color(0.35f, 0.38f, 0.78f, 0.45f)), // Indigo
                new BokehLight(610f, 210f, 110f, new Color(0.18f, 0.24f, 0.42f, 0.45f))  // Dark Teal
            };

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / h;
                // Base moody gradient from top to bottom
                Color baseCol = Color.Lerp(new Color(0.08f, 0.09f, 0.11f), new Color(0.12f, 0.13f, 0.16f), 1f - t);

                for (int x = 0; x < w; x++)
                {
                    Color pixel = baseCol;

                    // Add bokeh light contributions
                    for (int b = 0; b < bokehs.Length; b++)
                    {
                        float dx = x - bokehs[b].x;
                        float dy = y - bokehs[b].y;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist < bokehs[b].r)
                        {
                            float factor = Mathf.SmoothStep(1f, 0f, dist / bokehs[b].r);
                            pixel += bokehs[b].color * factor * bokehs[b].color.a;
                        }
                    }

                    pixel.a = 1f;
                    tex.SetPixel(x, y, pixel);
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

        private static Sprite GetOrCreateDarkCardSprite()
        {
            string path = TexturesFolder + "UI_DarkCard_Bg.png";
            if (File.Exists(path))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null) return s;
            }

            int size = 256;
            float radius = 48f;
            Texture2D tex = CreateRoundedTexture(size, size, radius, true, true, true, true);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(48, 48, 48, 48);
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite GetOrCreatePillSprite()
        {
            string path = TexturesFolder + "UI_Pill_Bg.png";
            if (File.Exists(path))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null) return s;
            }

            int w = 256;
            int h = 128;
            float radius = 64f;
            Texture2D tex = CreateRoundedTexture(w, h, radius, true, true, true, true);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(64, 32, 64, 32);
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite GetOrCreateThumbnailSprite()
        {
            string path = TexturesFolder + "UI_ThumbnailAttachment.png";
            if (File.Exists(path))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null) return s;
            }

            int w = 64;
            int h = 80;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Border
                    if (x == 0 || x == w - 1 || y == 0 || y == h - 1)
                    {
                        tex.SetPixel(x, y, new Color(0.7f, 0.7f, 0.75f, 1f));
                    }
                    else
                    {
                        // Faint horizontal text lines
                        if ((y % 8 == 0 || y % 8 == 1) && y > 16 && y < h - 10 && x > 6 && x < w - 6)
                        {
                            tex.SetPixel(x, y, new Color(0.6f, 0.65f, 0.7f, 1f));
                        }
                        else
                        {
                            tex.SetPixel(x, y, new Color(0.95f, 0.96f, 0.98f, 1f)); // Paper white
                        }
                    }

                    // Red stamp at bottom right
                    float dx = x - (w - 14);
                    float dy = y - 14;
                    if (Mathf.Sqrt(dx * dx + dy * dy) < 8f)
                    {
                        tex.SetPixel(x, y, new Color(0.85f, 0.15f, 0.15f, 0.9f));
                    }
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

        private static Texture2D CreateRoundedTexture(int width, int height, float radius, bool roundTL, bool roundTR, bool roundBL, bool roundBR)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = 0f;
                    bool inCorner = false;

                    if (x < radius && y >= height - radius && roundTL)
                    {
                        inCorner = true;
                        float dx = radius - x;
                        float dy = y - (height - radius - 1);
                        dist = Mathf.Sqrt(dx * dx + dy * dy);
                    }
                    else if (x >= width - radius && y >= height - radius && roundTR)
                    {
                        inCorner = true;
                        float dx = x - (width - radius - 1);
                        float dy = y - (height - radius - 1);
                        dist = Mathf.Sqrt(dx * dx + dy * dy);
                    }
                    else if (x < radius && y < radius && roundBL)
                    {
                        inCorner = true;
                        float dx = radius - x;
                        float dy = radius - y;
                        dist = Mathf.Sqrt(dx * dx + dy * dy);
                    }
                    else if (x >= width - radius && y < radius && roundBR)
                    {
                        inCorner = true;
                        float dx = x - (width - radius - 1);
                        float dy = radius - y;
                        dist = Mathf.Sqrt(dx * dx + dy * dy);
                    }

                    float alpha = 1f;
                    if (inCorner)
                    {
                        float edgeDist = dist - radius;
                        alpha = Mathf.Clamp01(0.5f - edgeDist);
                    }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
