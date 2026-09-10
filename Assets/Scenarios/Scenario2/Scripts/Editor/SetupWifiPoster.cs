using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;

namespace Kannko.Editor
{
    /// <summary>
    /// Editor helper to generate or update the Jakko_Cooffee Wi-Fi wall poster.
    /// Preserves custom user positioning & rotation in the Scene view and avoids Play Mode errors.
    /// </summary>
    public static class SetupWifiPoster
    {
        [MenuItem("Kannko/Setup Wifi Poster")]
        public static void CreateWifiPoster()
        {
            // Do NOT execute during Play Mode or state transition
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "Scenario2_Cafe")
            {
                return;
            }

            // 1. Configure TextureImporter for logo
            string logoPath = "Assets/Textures/Scenario2/WifiCoffeeLogo.png";
            if (File.Exists(logoPath))
            {
                var importer = AssetImporter.GetAtPath(logoPath) as TextureImporter;
                if (importer != null && (importer.textureType != TextureImporterType.Sprite || !importer.alphaIsTransparency))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }

            Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);

            // Find wall parent
            var coffeeRoom = GameObject.Find("CoffeeShopRoom");
            Transform parentTransform = coffeeRoom != null ? coffeeRoom.transform : null;

            // Preserve existing user position & rotation if poster already exists in scene
            Vector3 savedPosition = new Vector3(0.05f, 2.15f, 3.25f);
            Quaternion savedRotation = Quaternion.Euler(0f, 180f, 0f);
            Vector3 savedScale = new Vector3(0.0016f, 0.0016f, 0.0016f);

            var existingPoster = GameObject.Find("WifiPoster_JakkoCoffee");
            if (existingPoster != null)
            {
                savedPosition = existingPoster.transform.position;
                savedRotation = existingPoster.transform.rotation;
                savedScale = existingPoster.transform.localScale;
                if (existingPoster.transform.parent != null)
                {
                    parentTransform = existingPoster.transform.parent;
                }
                Object.DestroyImmediate(existingPoster);
            }

            // 2. Create Poster Frame GameObject
            var posterGO = new GameObject("WifiPoster_JakkoCoffee");
            if (parentTransform != null)
            {
                posterGO.transform.SetParent(parentTransform, true);
            }

            // Restore user's custom position and rotation
            posterGO.transform.position = savedPosition;
            posterGO.transform.rotation = savedRotation;

            // Add World Space Canvas
            var canvas = posterGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var scaler = posterGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            var rectTransform = posterGO.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(560, 840);
            rectTransform.localScale = savedScale;

            System.Action<Graphic> disableRaycast = (graphic) => {
                if (graphic != null) graphic.raycastTarget = false;
            };

            // 3. Dark Wood Frame Outer
            var frameGO = new GameObject("FrameOuter", typeof(Image));
            frameGO.transform.SetParent(posterGO.transform, false);
            var frameRT = frameGO.GetComponent<RectTransform>();
            frameRT.anchorMin = Vector2.zero;
            frameRT.anchorMax = Vector2.one;
            frameRT.offsetMin = Vector2.zero;
            frameRT.offsetMax = Vector2.zero;
            var frameImg = frameGO.GetComponent<Image>();
            frameImg.color = new Color(0.20f, 0.12f, 0.06f, 1f);
            disableRaycast(frameImg);

            // 4. Cream Matting Border
            var mattingGO = new GameObject("MattingBorder", typeof(Image));
            mattingGO.transform.SetParent(frameGO.transform, false);
            var mattingRT = mattingGO.GetComponent<RectTransform>();
            mattingRT.anchorMin = Vector2.zero;
            mattingRT.anchorMax = Vector2.one;
            mattingRT.offsetMin = new Vector2(18, 18);
            mattingRT.offsetMax = new Vector2(-18, -18);
            var mattingImg = mattingGO.GetComponent<Image>();
            mattingImg.color = new Color(0.97f, 0.96f, 0.93f, 1f);
            disableRaycast(mattingImg);

            // 5. Crisp White Paper Content Area
            var contentGO = new GameObject("PosterContent", typeof(Image));
            contentGO.transform.SetParent(mattingGO.transform, false);
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = Vector2.zero;
            contentRT.anchorMax = Vector2.one;
            contentRT.offsetMin = new Vector2(14, 14);
            contentRT.offsetMax = new Vector2(-14, -14);
            var contentImg = contentGO.GetComponent<Image>();
            contentImg.color = Color.white;
            disableRaycast(contentImg);

            // 6. TOP: Coffee Wi-Fi Pic ABOVE Jakko_Cooffee
            var logoGO = new GameObject("LogoImage", typeof(Image));
            logoGO.transform.SetParent(contentGO.transform, false);
            var logoRT = logoGO.GetComponent<RectTransform>();
            logoRT.anchorMin = new Vector2(0.5f, 1f);
            logoRT.anchorMax = new Vector2(0.5f, 1f);
            logoRT.pivot = new Vector2(0.5f, 1f);
            logoRT.anchoredPosition = new Vector2(0, -25);
            logoRT.sizeDelta = new Vector2(300, 270);
            
            var logoImg = logoGO.GetComponent<Image>();
            if (logoSprite != null)
            {
                logoImg.sprite = logoSprite;
                logoImg.color = Color.white;
            }
            disableRaycast(logoImg);

            // Load Font Asset for crisp clear typography
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (fontAsset == null)
            {
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset");
            }

            System.Action<TextMeshProUGUI, float, Color, FontStyles> formatText = (tmp, size, col, style) => {
                if (tmp == null) return;
                if (fontAsset != null) tmp.font = fontAsset;
                tmp.fontSize = size;
                tmp.color = col;
                tmp.fontStyle = style;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
            };

            // 7. TITLE DIRECTLY BELOW COFFEE PIC: "Jakko_Cooffee" (Big & Clear)
            var titleGO = new GameObject("TitleText", typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(contentGO.transform, false);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0, -315);
            titleRT.sizeDelta = new Vector2(0, 65);

            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Jakko_Cooffee";
            formatText(titleTmp, 48, new Color(0.24f, 0.14f, 0.06f, 1f), FontStyles.Bold);

            // 8. SUBTITLE: "FREE WIFI" (Big & Clear)
            var subGO = new GameObject("SubtitleText", typeof(TextMeshProUGUI));
            subGO.transform.SetParent(contentGO.transform, false);
            var subRT = subGO.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0f, 1f);
            subRT.anchorMax = new Vector2(1f, 1f);
            subRT.pivot = new Vector2(0.5f, 1f);
            subRT.anchoredPosition = new Vector2(0, -385);
            subRT.sizeDelta = new Vector2(0, 45);

            var subTmp = subGO.GetComponent<TextMeshProUGUI>();
            subTmp.text = "FREE WIFI";
            formatText(subTmp, 30, new Color(0.44f, 0.30f, 0.18f, 1f), FontStyles.Bold);

            // 9. DIVIDER LINE
            var lineGO = new GameObject("Divider", typeof(Image));
            lineGO.transform.SetParent(contentGO.transform, false);
            var lineRT = lineGO.GetComponent<RectTransform>();
            lineRT.anchorMin = new Vector2(0.5f, 1f);
            lineRT.anchorMax = new Vector2(0.5f, 1f);
            lineRT.pivot = new Vector2(0.5f, 1f);
            lineRT.anchoredPosition = new Vector2(0, -445);
            lineRT.sizeDelta = new Vector2(440, 4);
            var lineImg = lineGO.GetComponent<Image>();
            lineImg.color = new Color(0.72f, 0.65f, 0.58f, 1f);
            disableRaycast(lineImg);

            // 10. PASSWORD LABEL (Big & Clear)
            var passLabelGO = new GameObject("PasswordLabel", typeof(TextMeshProUGUI));
            passLabelGO.transform.SetParent(contentGO.transform, false);
            var passLabelRT = passLabelGO.GetComponent<RectTransform>();
            passLabelRT.anchorMin = new Vector2(0f, 1f);
            passLabelRT.anchorMax = new Vector2(1f, 1f);
            passLabelRT.pivot = new Vector2(0.5f, 1f);
            passLabelRT.anchoredPosition = new Vector2(0, -465);
            passLabelRT.sizeDelta = new Vector2(0, 40);

            var passLabelTmp = passLabelGO.GetComponent<TextMeshProUGUI>();
            passLabelTmp.text = "WIFI PASSWORD (BINARY CODE):";
            formatText(passLabelTmp, 22, new Color(0.30f, 0.18f, 0.10f, 1f), FontStyles.Bold);

            // 11. BINARY CODE PASSWORD (Big, Bold & Ultra Clear)
            var binaryGO = new GameObject("BinaryCodeText", typeof(TextMeshProUGUI));
            binaryGO.transform.SetParent(contentGO.transform, false);
            var binaryRT = binaryGO.GetComponent<RectTransform>();
            binaryRT.anchorMin = new Vector2(0f, 1f);
            binaryRT.anchorMax = new Vector2(1f, 1f);
            binaryRT.pivot = new Vector2(0.5f, 1f);
            binaryRT.anchoredPosition = new Vector2(0, -515);
            binaryRT.sizeDelta = new Vector2(480, 240);

            var binaryTmp = binaryGO.GetComponent<TextMeshProUGUI>();
            binaryTmp.text = "01001100 01000001\n01010100 01010100\n01000101";
            formatText(binaryTmp, 32, new Color(0.05f, 0.05f, 0.05f, 1f), FontStyles.Bold);
            binaryTmp.characterSpacing = 3;
            binaryTmp.lineSpacing = 12;

            // Save Prefab and Scene safely in Editor mode only
            string prefabPath = "Assets/Prefabs/Scenario2/WifiPoster_JakkoCoffee.prefab";
            if (!Directory.Exists("Assets/Prefabs/Scenario2"))
            {
                Directory.CreateDirectory("Assets/Prefabs/Scenario2");
            }
            PrefabUtility.SaveAsPrefabAssetAndConnect(posterGO, prefabPath, InteractionMode.AutomatedAction);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[Wifi Poster] Setup completed cleanly. Position preserved.");
        }
    }
}
