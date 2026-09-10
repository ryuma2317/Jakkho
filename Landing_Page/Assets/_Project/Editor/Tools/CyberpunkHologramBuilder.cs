using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;
using System.IO;

namespace VRSetup.Editor
{
    public class CyberpunkHologramBuilder : UnityEditor.Editor
    {
        [MenuItem("Tools/Build Cyberpunk Hologram & Void Environment")]
        public static void BuildCyberpunkScene()
        {
            // 1. Clean up legacy rooms, walls, and old cards
            string[] legacyObjects = {
                "VR_EnhancedRoomEnvironment", "VR_SciFiCommandRoom", "RoomEnvironment", 
                "WallWindow4m", "Wall4m", "Wall1m", "Wall4m (1)", "WallWindowTall4m", 
                "Floor2m", "RugRectangleMedium", "ObsidianMirrorFloor", "VoidAmbientParticles"
            };

            foreach (var name in legacyObjects)
            {
                var go = GameObject.Find(name);
                if (go != null) DestroyImmediate(go);
            }

            // 2. Configure Camera for Infinite Dark Void
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0.005f, 0.008f, 0.015f, 1f); // Deep pitch black void
                mainCam.nearClipPlane = 0.01f;
                mainCam.farClipPlane = 1000f;
            }

            // 3. Create Glossy Obsidian Mirror Floor Plane
            var floorGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorGO.name = "ObsidianMirrorFloor";
            floorGO.transform.position = new Vector3(0f, -0.05f, 2.5f);
            floorGO.transform.localScale = new Vector3(30f, 0.1f, 30f);

            var floorMat = GetOrCreateFloorMaterial();
            floorGO.GetComponent<Renderer>().sharedMaterial = floorMat;

            var floorCol = floorGO.GetComponent<BoxCollider>();
            if (floorCol == null) floorCol = floorGO.AddComponent<BoxCollider>();
            floorCol.size = new Vector3(30f, 0.1f, 30f);

            // 4. Create Ambient Floor Dust/Starlight Particle System
            var partGO = new GameObject("VoidAmbientParticles");
            partGO.transform.position = new Vector3(0f, 0.05f, 2.5f);
            var ps = partGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 0.8f, 1.0f, 0.35f), new Color(0.8f, 0.95f, 1.0f, 0.6f));
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 0.2f, 10f);

            var emission = ps.emission;
            emission.rateOverTime = 15;

            var psRenderer = partGO.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default"));

            // 5. Build or Refactor Canvas
            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                canvasGO = new GameObject("Canvas");
                canvasGO.AddComponent<Canvas>();
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCam;

            var canvasRT = canvasGO.GetComponent<RectTransform>();
            canvasRT.position = new Vector3(0f, 1.40f, 2.40f);
            canvasRT.rotation = Quaternion.identity;
            canvasRT.localScale = new Vector3(0.0022f, 0.0022f, 0.0022f);
            canvasRT.sizeDelta = new Vector2(1150f, 620f);

            // Ensure Raycaster & Clicker
            System.Type vrRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (vrRaycasterType != null && canvasGO.GetComponent(vrRaycasterType) == null)
            {
                canvasGO.AddComponent(vrRaycasterType);
            }

            var clicker = canvasGO.GetComponent<VRDirectPointerClicker>();
            if (clicker == null) clicker = canvasGO.AddComponent<VRDirectPointerClicker>();

            var nav = canvasGO.GetComponent<VRCardNavigationManager>();
            if (nav == null) nav = canvasGO.AddComponent<VRCardNavigationManager>();

            // Delete any old Jakkro card or panels
            var oldPanels = new System.Collections.Generic.List<GameObject>();
            foreach (Transform t in canvasGO.transform)
            {
                if (t.name != "ScenarioSelectionScreen")
                {
                    oldPanels.Add(t.gameObject);
                }
            }
            foreach (var go in oldPanels) DestroyImmediate(go);

            // 6. Build the Holographic Cyberpunk JAKKHO Card
            var round10 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/VRTemplateAssets/Sprites/UI/Round Radius 10.png")
                ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/DemoAssets/Sprites/Round Radius 4.png");

            var jakkroCard = CreateHologramCard("JakkroCard", canvasGO.transform, false, round10, out Button playBtn, out Button quitBtn);
            nav.jakkroCard = jakkroCard;
            nav.menuButton = playBtn;

            // 7. Build Real-Time Mirrored Floor Reflection
            var reflectionCard = CreateHologramCard("JakkroCard_Reflection", canvasGO.transform, true, round10, out _, out _);
            nav.jakkroCardReflection = reflectionCard;

            // 8. Wire up persistent listeners
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(playBtn.onClick, nav.ShowScenarioCard);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(playBtn.onClick, nav.ShowScenarioCard);

            nav.SetupButtonListeners();
            nav.ShowJakkroCard(); // Set initial active state

            // 9. Mark Scene Dirty & Save
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("<b>[CyberpunkHologramBuilder]</b> Cyberpunk Holographic JAKKHO card and dark void floor reflection built successfully!");
        }

        private static GameObject CreateHologramCard(string name, Transform parent, bool isReflection, Sprite roundSprite, out Button playBtn, out Button quitBtn)
        {
            var cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);

            var cardRT = cardGO.AddComponent<RectTransform>();
            cardRT.sizeDelta = new Vector2(520f, 340f);

            if (isReflection)
            {
                // Reflected below floor plane (Canvas at Y=1.40 -> Reflection at Y=-1.40 relative to floor, inverted scale)
                cardRT.anchoredPosition = new Vector2(0f, -1270f);
                cardRT.localPosition = new Vector3(0f, -1270f, 0f);
                cardRT.localScale = new Vector3(1.0f, -0.95f, 1.0f); // Inverted Y for mirror reflection
            }
            else
            {
                cardRT.anchoredPosition = Vector2.zero;
                cardRT.localPosition = Vector3.zero;
                cardRT.localScale = Vector3.one;
            }

            float alphaMult = isReflection ? 0.38f : 1.0f;

            // Main Holographic Dark Glass Frame
            var bgImg = cardGO.AddComponent<Image>();
            if (roundSprite != null)
            {
                bgImg.sprite = roundSprite;
                bgImg.type = Image.Type.Sliced;
            }
            bgImg.color = new Color(0.03f, 0.08f, 0.16f, 0.92f * alphaMult);

            // Subtle Cyberpunk Background Grid / Code Watermark
            var codeGO = new GameObject("CodeWatermark");
            codeGO.transform.SetParent(cardGO.transform, false);
            var codeRT = codeGO.AddComponent<RectTransform>();
            codeRT.sizeDelta = new Vector2(480f, 160f);
            codeRT.anchoredPosition = new Vector2(-10f, -70f);
            codeRT.localScale = Vector3.one;

            var codeTMP = codeGO.AddComponent<TextMeshProUGUI>();
            codeTMP.text = "0x8F4A 0x22B1 NET_INIT\nSYS_PROTOCOL_SECURE_AUTH\nDECRYPT_KEY_ROTATION: OK\nRUN_SCENARIO_ROUTINE...";
            codeTMP.fontSize = 9f;
            codeTMP.color = new Color(0.0f, 0.9f, 1.0f, 0.14f * alphaMult);
            codeTMP.fontStyle = FontStyles.Normal;
            codeTMP.alignment = TextAlignmentOptions.TopLeft;
            codeTMP.raycastTarget = false;

            // 4 Glowing Corner Brackets
            Color cyanGlow = new Color(0.0f, 0.95f, 1.0f, 1.0f * alphaMult);
            CreateCornerBracket(cardGO.transform, "Bracket_TL", new Vector2(-235f, 145f), "⎡", cyanGlow);
            CreateCornerBracket(cardGO.transform, "Bracket_TR", new Vector2( 235f, 145f), "⎤", cyanGlow);
            CreateCornerBracket(cardGO.transform, "Bracket_BL", new Vector2(-235f, -145f), "⎣", cyanGlow);
            CreateCornerBracket(cardGO.transform, "Bracket_BR", new Vector2( 235f, -145f), "⎦", cyanGlow);

            // Main Title: JAKKHO
            var titleGO = new GameObject("Title_JAKKHO");
            titleGO.transform.SetParent(cardGO.transform, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.sizeDelta = new Vector2(460f, 65f);
            titleRT.anchoredPosition = new Vector2(0f, 95f);
            titleRT.localScale = Vector3.one;

            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "J A K K H O";
            titleTMP.fontSize = 42f;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = cyanGlow;
            titleTMP.characterSpacing = 8f;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.raycastTarget = false;

            // Subtitle: Experience. Decide. Understand.
            var subGO = new GameObject("Subtitle");
            subGO.transform.SetParent(cardGO.transform, false);
            var subRT = subGO.AddComponent<RectTransform>();
            subRT.sizeDelta = new Vector2(460f, 30f);
            subRT.anchoredPosition = new Vector2(0f, 50f);
            subRT.localScale = Vector3.one;

            var subTMP = subGO.AddComponent<TextMeshProUGUI>();
            subTMP.text = "Experience. Decide. Understand.";
            subTMP.fontSize = 15f;
            subTMP.fontStyle = FontStyles.Normal;
            subTMP.color = new Color(0.65f, 0.90f, 1.0f, 0.9f * alphaMult);
            subTMP.alignment = TextAlignmentOptions.Center;
            subTMP.raycastTarget = false;

            // Glowing Amber ▶ PLAY Button
            Color amberColor = new Color(1.0f, 0.65f, 0.05f, 1.0f * alphaMult);
            var playBtnGO = CreateCyberButton(cardGO.transform, "PlayButton", new Vector2(0f, -12f), new Vector2(230f, 44f), "▶  PLAY", amberColor, roundSprite, isReflection, out playBtn);

            // Glowing Cyan ✕ QUIT Button
            var quitBtnGO = CreateCyberButton(cardGO.transform, "QuitButton", new Vector2(0f, -68f), new Vector2(230f, 44f), "✕  QUIT", cyanGlow, roundSprite, isReflection, out quitBtn);

            if (!isReflection && quitBtn != null)
            {
                quitBtn.onClick.AddListener(() =>
                {
#if UNITY_EDITOR
                    EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
            }

            return cardGO;
        }

        private static GameObject CreateCyberButton(Transform parent, string name, Vector2 pos, Vector2 size, string text, Color themeColor, Sprite roundSprite, bool isReflection, out Button btn)
        {
            var btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);

            var bRT = btnGO.AddComponent<RectTransform>();
            bRT.sizeDelta = size;
            bRT.anchoredPosition = pos;
            bRT.localScale = Vector3.one;

            var bImg = btnGO.AddComponent<Image>();
            if (roundSprite != null)
            {
                bImg.sprite = roundSprite;
                bImg.type = Image.Type.Sliced;
            }
            bImg.color = new Color(themeColor.r * 0.15f, themeColor.g * 0.15f, themeColor.b * 0.15f, 0.85f * themeColor.a);

            // Outline / Border Effect
            var outline = btnGO.AddComponent<Outline>();
            outline.effectColor = themeColor;
            outline.effectDistance = new Vector2(2f, 2f);

            btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = bImg;

            if (!isReflection)
            {
                var col = btnGO.AddComponent<BoxCollider>();
                col.size = new Vector3(size.x, size.y, 10f);
                col.center = Vector3.zero;

                btnGO.AddComponent<VRCardButtonAnimator>();
            }

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;

            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = themeColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return btnGO;
        }

        private static void CreateCornerBracket(Transform parent, string name, Vector2 pos, string symbol, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(30f, 30f);
            rt.anchoredPosition = pos;
            rt.localScale = Vector3.one;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = symbol;
            tmp.fontSize = 32f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        private static Material GetOrCreateFloorMaterial()
        {
            string path = "Assets/Materials/SciFiCommandRoom/ObsidianMirrorFloor.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = new Color(0.015f, 0.02f, 0.035f, 1f); // Deep glossy obsidian black
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.96f);
            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", 0.90f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
