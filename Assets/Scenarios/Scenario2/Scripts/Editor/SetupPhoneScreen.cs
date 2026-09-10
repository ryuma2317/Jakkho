using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Kannko.Phone;

namespace Kannko.Editor
{
    /// <summary>
    /// Builds the visual phone screen experience using a dedicated RenderTexture architecture:
    /// - Creates/configures RT_PhoneScreen (700x1460) and M_PhoneScreen_RenderTexture material
    /// - Creates a physical display quad attached directly to Smartphone (0.070m x 0.146m)
    /// - Creates an isolated PhoneUI_RenderRig at (1000, 1000, 1000) with a dedicated PhoneUI Camera
    /// - Builds the Gen-Z home screen layout (Wallpaper, Status bar, Clock widget, 6 App slots, Dock, Home bar)
    /// - Configures PhoneScreenManager to drive the display quad and camera based on phone states
    /// - Updates Assets/Prefabs/Interactive/Smartphone.prefab and Scenario2_Cafe.unity
    /// </summary>
    public static class SetupPhoneScreen
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";
        private const string PrefabPath = "Assets/Prefabs/Interactive/Smartphone.prefab";
        private const string RenderTexturePath = "Assets/Textures/RT_PhoneScreen.renderTexture";
        private const string MaterialPath = "Assets/Materials/M_PhoneScreen_RenderTexture.mat";

        [MenuItem("VR Prototype/Setup Visual Phone Screen", priority = 7)]
        public static void BuildPhoneScreen()
        {
            Debug.Log("[PhoneScreen Setup] Starting visual screen construction (RenderTexture Architecture)...");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[PhoneScreen Setup] Scene not found: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Locate Smartphone in scene
            GameObject smartphoneGo = GameObject.Find("Smartphone");
            if (smartphoneGo == null)
            {
                Debug.LogError("[PhoneScreen Setup] Smartphone object not found in scene!");
                return;
            }

            SmartphoneController phoneController = smartphoneGo.GetComponent<SmartphoneController>();
            if (phoneController == null)
            {
                phoneController = smartphoneGo.AddComponent<SmartphoneController>();
            }

            // 2. Locate or create UI Sprites, RenderTexture, and Screen Material
            Sprite roundedSprite = GetOrCreateRoundedRectSprite();
            RenderTexture rt = GetOrCreateRenderTexture();
            Material screenMat = GetOrCreateScreenMaterial(rt);

            // 3. Clean up existing screen objects if re-running
            Transform existingDisplay = smartphoneGo.transform.Find("Phone_Screen_Display");
            if (existingDisplay != null) Object.DestroyImmediate(existingDisplay.gameObject);
            Transform existingScreen = smartphoneGo.transform.Find("PhoneScreen");
            if (existingScreen != null) Object.DestroyImmediate(existingScreen.gameObject);

            // 4. Create Physical Display Quad attached directly to Smartphone
            // Physical smartphone screen is 0.070m x 0.146m.
            GameObject displayQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            displayQuad.name = "Phone_Screen_Display";
            displayQuad.transform.SetParent(smartphoneGo.transform, false);
            // Sits 0.3mm above the 3D phone screen mesh to prevent z-fighting
            displayQuad.transform.localPosition = new Vector3(0f, 0.0051f, 0f);
            displayQuad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            displayQuad.transform.localScale = new Vector3(0.070f, 0.146f, 1f);

            // Remove mesh collider from display quad
            Collider quadCol = displayQuad.GetComponent<Collider>();
            if (quadCol != null) Object.DestroyImmediate(quadCol);

            MeshRenderer quadRenderer = displayQuad.GetComponent<MeshRenderer>();
            quadRenderer.sharedMaterial = screenMat;
            displayQuad.SetActive(false); // Hidden initially in physical state

            // 5. Create or clean up isolated PhoneUI_RenderRig at (1000, 1000, 1000)
            GameObject oldRig = GameObject.Find("PhoneUI_RenderRig");
            if (oldRig != null) Object.DestroyImmediate(oldRig);

            GameObject renderRigGo = new GameObject("PhoneUI_RenderRig");
            renderRigGo.transform.position = new Vector3(1000f, 1000f, 1000f);

            // 5a. Dedicated Phone UI Camera
            GameObject camGo = new GameObject("PhoneUICamera");
            camGo.transform.SetParent(renderRigGo.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -5f);
            Camera uiCamera = camGo.AddComponent<Camera>();
            uiCamera.clearFlags = CameraClearFlags.SolidColor;
            uiCamera.backgroundColor = new Color(0.06f, 0.09f, 0.16f, 1f);
            uiCamera.orthographic = true;
            uiCamera.orthographicSize = 7.3f;
            uiCamera.nearClipPlane = 0.1f;
            uiCamera.farClipPlane = 20f;
            uiCamera.targetTexture = rt;
            uiCamera.cullingMask = 1 << 5; // UI layer
            uiCamera.enabled = false; // Disabled until Active/Open

            // 5b. Phone UI Canvas
            GameObject canvasGo = new GameObject("PhoneUICanvas");
            canvasGo.transform.SetParent(renderRigGo.transform, false);
            canvasGo.transform.localPosition = Vector3.zero;
            canvasGo.layer = 5; // UI layer

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 5f;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(700f, 1460f);
            scaler.matchWidthOrHeight = 0.5f;

            CanvasGroup canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // 5c. Build Home Screen View Hierarchy
            GameObject homeScreenGo = new GameObject("HomeScreen");
            homeScreenGo.transform.SetParent(canvasGo.transform, false);
            homeScreenGo.layer = 5;
            RectTransform homeRect = homeScreenGo.AddComponent<RectTransform>();
            homeRect.anchorMin = Vector2.zero;
            homeRect.anchorMax = Vector2.one;
            homeRect.sizeDelta = Vector2.zero;
            homeRect.anchoredPosition = Vector2.zero;

            PhoneHomeScreen homeScreen = homeScreenGo.AddComponent<PhoneHomeScreen>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Wallpaper
            GameObject wallpaperGo = new GameObject("Wallpaper");
            wallpaperGo.transform.SetParent(homeScreenGo.transform, false);
            wallpaperGo.layer = 5;
            RectTransform wallRect = wallpaperGo.AddComponent<RectTransform>();
            wallRect.anchorMin = Vector2.zero;
            wallRect.anchorMax = Vector2.one;
            wallRect.sizeDelta = Vector2.zero;
            Image wallImg = wallpaperGo.AddComponent<Image>();
            wallImg.color = new Color(0.06f, 0.09f, 0.16f, 1f); // Deep indigo dark-mode

            // Status Bar
            GameObject statusBarGo = new GameObject("StatusBar");
            statusBarGo.transform.SetParent(homeScreenGo.transform, false);
            statusBarGo.layer = 5;
            RectTransform statusRect = statusBarGo.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.sizeDelta = new Vector2(0f, 60f);
            statusRect.anchoredPosition = new Vector2(0f, -10f);

            Text statusClock = CreateText(statusBarGo.transform, "StatusClock", "14:30", defaultFont, 24, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            RectTransform scRect = statusClock.rectTransform;
            scRect.anchorMin = new Vector2(0f, 0.5f);
            scRect.anchorMax = new Vector2(0f, 0.5f);
            scRect.pivot = new Vector2(0f, 0.5f);
            scRect.anchoredPosition = new Vector2(40f, 0f);
            scRect.sizeDelta = new Vector2(120f, 40f);

            Text statusIcons = CreateText(statusBarGo.transform, "StatusIcons", "5G   88%", defaultFont, 20, FontStyle.Normal, TextAnchor.MiddleRight, new Color(0.9f, 0.9f, 0.95f));
            RectTransform siRect = statusIcons.rectTransform;
            siRect.anchorMin = new Vector2(1f, 0.5f);
            siRect.anchorMax = new Vector2(1f, 0.5f);
            siRect.pivot = new Vector2(1f, 0.5f);
            siRect.anchoredPosition = new Vector2(-40f, 0f);
            siRect.sizeDelta = new Vector2(200f, 40f);

            // Large Digital Clock & Date Widget
            GameObject widgetGo = new GameObject("ClockWidget");
            widgetGo.transform.SetParent(homeScreenGo.transform, false);
            widgetGo.layer = 5;
            RectTransform widgetRect = widgetGo.AddComponent<RectTransform>();
            widgetRect.anchorMin = new Vector2(0.5f, 1f);
            widgetRect.anchorMax = new Vector2(0.5f, 1f);
            widgetRect.pivot = new Vector2(0.5f, 1f);
            widgetRect.anchoredPosition = new Vector2(0f, -130f);
            widgetRect.sizeDelta = new Vector2(620f, 190f);

            Text bigClock = CreateText(widgetGo.transform, "BigClock", "14:30", defaultFont, 84, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            bigClock.rectTransform.anchoredPosition = new Vector2(0f, 25f);
            bigClock.rectTransform.sizeDelta = new Vector2(600f, 110f);

            Text dateSub = CreateText(widgetGo.transform, "DateSubtitle", "Thursday, Sep 3 • 24°C", defaultFont, 24, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.7f, 0.78f, 0.88f));
            dateSub.rectTransform.anchoredPosition = new Vector2(0f, -45f);
            dateSub.rectTransform.sizeDelta = new Vector2(600f, 45f);

            // App Grid (2 rows x 3 columns of placeholder app icons)
            GameObject appGridGo = new GameObject("AppGrid");
            appGridGo.transform.SetParent(homeScreenGo.transform, false);
            appGridGo.layer = 5;
            RectTransform gridRect = appGridGo.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = new Vector2(0f, 30f);
            gridRect.sizeDelta = new Vector2(620f, 460f);

            CreateAppSlot(appGridGo.transform, "cafe", "NGEP Cafe", new Color(0.96f, 0.62f, 0.04f), roundedSprite, defaultFont, new Vector2(-190f, 110f));
            CreateAppSlot(appGridGo.transform, "messages", "Messages", new Color(0.10f, 0.75f, 0.45f), roundedSprite, defaultFont, new Vector2(0f, 110f));
            CreateAppSlot(appGridGo.transform, "photos", "Photos", new Color(0.65f, 0.35f, 0.95f), roundedSprite, defaultFont, new Vector2(190f, 110f));

            CreateAppSlot(appGridGo.transform, "music", "Music", new Color(0.95f, 0.25f, 0.55f), roundedSprite, defaultFont, new Vector2(-190f, -90f));
            CreateAppSlot(appGridGo.transform, "maps", "Maps", new Color(0.08f, 0.65f, 0.95f), roundedSprite, defaultFont, new Vector2(0f, -90f));
            CreateAppSlot(appGridGo.transform, "wallet", "Wallet", new Color(0.85f, 0.70f, 0.10f), roundedSprite, defaultFont, new Vector2(190f, -90f));

            // Bottom Dock
            GameObject dockGo = new GameObject("Dock");
            dockGo.transform.SetParent(homeScreenGo.transform, false);
            dockGo.layer = 5;
            RectTransform dockRect = dockGo.AddComponent<RectTransform>();
            dockRect.anchorMin = new Vector2(0.5f, 0f);
            dockRect.anchorMax = new Vector2(0.5f, 0f);
            dockRect.pivot = new Vector2(0.5f, 0f);
            dockRect.anchoredPosition = new Vector2(0f, 110f);
            dockRect.sizeDelta = new Vector2(620f, 150f);

            Image dockBg = dockGo.AddComponent<Image>();
            dockBg.sprite = roundedSprite;
            dockBg.type = Image.Type.Sliced;
            dockBg.color = new Color(0.15f, 0.20f, 0.30f, 0.65f); // Frosted dock

            CreateDockIcon(dockGo.transform, "phone", "Phone", new Color(0.15f, 0.80f, 0.35f), roundedSprite, new Vector2(-200f, 0f));
            CreateDockIcon(dockGo.transform, "chat", "Chat", new Color(0.10f, 0.70f, 0.80f), roundedSprite, new Vector2(-67f, 0f));
            CreateDockIcon(dockGo.transform, "safari", "Browser", new Color(0.20f, 0.65f, 0.95f), roundedSprite, new Vector2(67f, 0f));
            CreateDockIcon(dockGo.transform, "settings", "Settings", new Color(0.40f, 0.45f, 0.55f), roundedSprite, new Vector2(200f, 0f));

            // Home Indicator Bar
            GameObject homeBarGo = new GameObject("HomeIndicatorBar");
            homeBarGo.transform.SetParent(homeScreenGo.transform, false);
            homeBarGo.layer = 5;
            RectTransform barRect = homeBarGo.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 0f);
            barRect.anchorMax = new Vector2(0.5f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0f, 25f);
            barRect.sizeDelta = new Vector2(240f, 8f);
            Image barImg = homeBarGo.AddComponent<Image>();
            barImg.sprite = roundedSprite;
            barImg.type = Image.Type.Sliced;
            barImg.color = new Color(1f, 1f, 1f, 0.65f);

            // 6. Connect PhoneScreenManager on Smartphone
            PhoneScreenManager screenManager = smartphoneGo.GetComponent<PhoneScreenManager>();
            if (screenManager == null) screenManager = smartphoneGo.AddComponent<PhoneScreenManager>();
            screenManager.Configure(phoneController, canvas, canvasGroup, homeScreenGo, displayQuad, uiCamera);

            // Wire serialized fields on PhoneHomeScreen
            SerializedObject homeScreenSO = new SerializedObject(homeScreen);
            homeScreenSO.FindProperty("m_StatusClockText").objectReferenceValue = statusClock;
            homeScreenSO.FindProperty("m_StatusBatteryText").objectReferenceValue = statusIcons;
            homeScreenSO.FindProperty("m_WidgetClockText").objectReferenceValue = bigClock;
            homeScreenSO.FindProperty("m_WidgetDateText").objectReferenceValue = dateSub;
            homeScreenSO.ApplyModifiedProperties();

            // 7. Save to Prefab and Scene
            bool prefabSuccess;
            PrefabUtility.SaveAsPrefabAsset(smartphoneGo, PrefabPath, out prefabSuccess);
            if (prefabSuccess)
            {
                Debug.Log($"[PhoneScreen Setup] Prefab successfully updated: {PrefabPath}");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[PhoneScreen Setup] Visual phone-screen experience setup completed successfully (RenderTexture architecture)!");
        }

        private static GameObject CreateAppSlot(Transform parent, string id, string title, Color color, Sprite sprite, Font font, Vector2 pos)
        {
            GameObject slotGo = new GameObject("App_" + id);
            slotGo.transform.SetParent(parent, false);
            slotGo.layer = 5;
            RectTransform rt = slotGo.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(120f, 150f);

            GameObject iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(slotGo.transform, false);
            iconGo.layer = 5;
            RectTransform iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchoredPosition = new Vector2(0f, 18f);
            iconRt.sizeDelta = new Vector2(96f, 96f);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = sprite;
            iconImg.type = Image.Type.Sliced;
            iconImg.color = color;

            Text label = CreateText(slotGo.transform, "Label", title, font, 20, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            label.rectTransform.anchoredPosition = new Vector2(0f, -48f);
            label.rectTransform.sizeDelta = new Vector2(140f, 30f);

            PhoneAppSlot appSlot = slotGo.AddComponent<PhoneAppSlot>();
            appSlot.Configure(id, title, color);

            return slotGo;
        }

        private static GameObject CreateDockIcon(Transform parent, string id, string title, Color color, Sprite sprite, Vector2 pos)
        {
            GameObject iconGo = new GameObject("Dock_" + id);
            iconGo.transform.SetParent(parent, false);
            iconGo.layer = 5;
            RectTransform rt = iconGo.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(90f, 90f);
            Image img = iconGo.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            return iconGo;
        }

        private static Text CreateText(Transform parent, string name, string content, Font font, int size, FontStyle style, TextAnchor alignment, Color color)
        {
            GameObject textGo = new GameObject(name);
            textGo.transform.SetParent(parent, false);
            textGo.layer = 5;
            Text text = textGo.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            return text;
        }

        private static RenderTexture GetOrCreateRenderTexture()
        {
            if (!Directory.Exists("Assets/Textures")) Directory.CreateDirectory("Assets/Textures");
            RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (rt == null)
            {
                rt = new RenderTexture(700, 1460, 24, RenderTextureFormat.ARGB32);
                rt.name = "RT_PhoneScreen";
                rt.filterMode = FilterMode.Bilinear;
                rt.wrapMode = TextureWrapMode.Clamp;
                AssetDatabase.CreateAsset(rt, RenderTexturePath);
                AssetDatabase.SaveAssets();
            }
            return rt;
        }

        private static Material GetOrCreateScreenMaterial(RenderTexture rt)
        {
            if (!Directory.Exists("Assets/Materials")) Directory.CreateDirectory("Assets/Materials");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Unlit/Texture");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.name = "M_PhoneScreen_RenderTexture";
                mat.mainTexture = rt;
                AssetDatabase.CreateAsset(mat, MaterialPath);
                AssetDatabase.SaveAssets();
            }
            else
            {
                mat.mainTexture = rt;
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static Sprite GetOrCreateRoundedRectSprite()
        {
            const string path = "Assets/Textures/UI_RoundedRect.png";
            if (!Directory.Exists("Assets/Textures"))
            {
                Directory.CreateDirectory("Assets/Textures");
            }

            if (!File.Exists(path))
            {
                int size = 64;
                int r = 16;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color[] colors = new Color[size * size];

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int cx = x < r ? r - x : (x >= size - r ? x - (size - r - 1) : 0);
                        int cy = y < r ? r - y : (y >= size - r ? y - (size - r - 1) : 0);
                        float dist = Mathf.Sqrt(cx * cx + cy * cy);

                        if (dist <= r - 0.5f)
                        {
                            colors[y * size + x] = Color.white;
                        }
                        else if (dist <= r + 0.5f)
                        {
                            float a = Mathf.Clamp01(r + 0.5f - dist);
                            colors[y * size + x] = new Color(1f, 1f, 1f, a);
                        }
                        else
                        {
                            colors[y * size + x] = Color.clear;
                        }
                    }
                }

                tex.SetPixels(colors);
                tex.Apply();

                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteBorder = new Vector4(16, 16, 16, 16);
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
