#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using CADT.VR.Scenarios.Scenario1;

namespace CADT.VR.Scenarios.Scenario1.Editor
{
    public static class BuildScenario1DialogueIntro
    {
        [MenuItem("CADT VR/Scenarios/Build Scenario 1 Dialogue Intro", priority = 32)]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Scenario1")
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenarios/Scenario1/Scenes/Scenario1.unity", OpenSceneMode.Single);
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGo = GameObject.Find("Main Camera");
                if (camGo != null) mainCam = camGo.GetComponent<Camera>();
            }

            // Find existing Scenario1IntroPanel
            GameObject introPanelGo = GameObject.Find("Scenario1IntroPanel");
            if (introPanelGo == null)
            {
                if (mainCam != null)
                {
                    Transform t = mainCam.transform.Find("Scenario1IntroPanel");
                    if (t != null) introPanelGo = t.gameObject;
                }
            }

            if (introPanelGo == null)
            {
                introPanelGo = new GameObject("Scenario1IntroPanel");
            }

            // Parent to Main Camera so it moves with the player's view, exactly like ScenarioUI_Canvas & the Exit button!
            if (mainCam != null)
            {
                introPanelGo.transform.SetParent(mainCam.transform, false);
            }
            introPanelGo.transform.localPosition = new Vector3(0f, 0f, 1.25f);
            introPanelGo.transform.localRotation = Quaternion.identity;
            introPanelGo.transform.localScale = Vector3.one * 0.0011f;

            // Canvas & Raycasters
            var canvas = GetOrAddComponent<Canvas>(introPanelGo);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCam;
            canvas.sortingOrder = 95;

            var scaler = GetOrAddComponent<CanvasScaler>(introPanelGo);
            scaler.dynamicPixelsPerUnit = 10f;

            var canvasGroup = GetOrAddComponent<CanvasGroup>(introPanelGo);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            GetOrAddComponent<GraphicRaycaster>(introPanelGo);
            var tracked = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (tracked != null)
            {
                if (introPanelGo.GetComponent(tracked) == null) introPanelGo.AddComponent(tracked);
            }

            var rootRt = introPanelGo.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(1000f, 640f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);

            var rootCol = GetOrAddComponent<BoxCollider>(introPanelGo);
            rootCol.size = new Vector3(1000f, 640f, 2f);
            rootCol.center = Vector3.zero;

            // Clear old children to build clean dialogue system
            while (introPanelGo.transform.childCount > 0)
            {
                Undo.DestroyObjectImmediate(introPanelGo.transform.GetChild(0).gameObject);
            }

            // =========================================================
            // 1. BUILD DIALOGUE CARD AT BOTTOM-LEFT OF MAIN CAMERA
            // Position: x in [-460, 0], y in [-270, -85] (opposite top-right EXIT button at x: [+355, +485], y: [+243, +304])
            // =========================================================
            GameObject cardGo = new GameObject("DialogueCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BoxCollider));
            cardGo.transform.SetParent(introPanelGo.transform, false);

            float cardWidth = 1377.1f;
            float cardHeight = 240.8f;

            RectTransform cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0f, 0f);
            cardRt.anchoredPosition = new Vector2(-460f, -270f);
            cardRt.sizeDelta = new Vector2(cardWidth, cardHeight);

            Image cardImg = cardGo.GetComponent<Image>();
            cardImg.color = new Color(0f, 0f, 0f, 0f);
            cardImg.raycastTarget = false;

            BoxCollider cardCol = cardGo.GetComponent<BoxCollider>();
            cardCol.size = new Vector3(cardWidth, cardHeight, 5f);
            cardCol.center = new Vector3(cardWidth * 0.5f, cardHeight * 0.5f, 0f);

            // Border Frame (clean hollow cyan border without background fill)
            GameObject borderFrameGo = new GameObject("BorderFrame", typeof(RectTransform));
            borderFrameGo.transform.SetParent(cardGo.transform, false);
            borderFrameGo.transform.SetAsFirstSibling();
            RectTransform bfRt = borderFrameGo.GetComponent<RectTransform>();
            bfRt.anchorMin = Vector2.zero;
            bfRt.anchorMax = Vector2.one;
            bfRt.offsetMin = Vector2.zero;
            bfRt.offsetMax = Vector2.zero;

            Color borderColor = new Color(0.0f, 0.88f, 1.0f, 0.92f);

            // Top Border
            GameObject topB = new GameObject("Border_Top", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            topB.transform.SetParent(borderFrameGo.transform, false);
            RectTransform tRt = topB.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.sizeDelta = new Vector2(0f, 2.5f);
            tRt.anchoredPosition = Vector2.zero;
            Image tImg = topB.GetComponent<Image>();
            tImg.color = borderColor;
            tImg.raycastTarget = false;

            // Bottom Border
            GameObject btmB = new GameObject("Border_Bottom", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            btmB.transform.SetParent(borderFrameGo.transform, false);
            RectTransform btmRt = btmB.GetComponent<RectTransform>();
            btmRt.anchorMin = new Vector2(0f, 0f);
            btmRt.anchorMax = new Vector2(1f, 0f);
            btmRt.pivot = new Vector2(0.5f, 0f);
            btmRt.sizeDelta = new Vector2(0f, 2.5f);
            btmRt.anchoredPosition = Vector2.zero;
            Image bImg = btmB.GetComponent<Image>();
            bImg.color = borderColor;
            bImg.raycastTarget = false;

            // Left Border
            GameObject leftB = new GameObject("Border_Left", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            leftB.transform.SetParent(borderFrameGo.transform, false);
            RectTransform lRt = leftB.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0f);
            lRt.anchorMax = new Vector2(0f, 1f);
            lRt.pivot = new Vector2(0f, 0.5f);
            lRt.sizeDelta = new Vector2(2.5f, 0f);
            lRt.anchoredPosition = Vector2.zero;
            Image lImg = leftB.GetComponent<Image>();
            lImg.color = borderColor;
            lImg.raycastTarget = false;

            // Right Border
            GameObject rightB = new GameObject("Border_Right", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rightB.transform.SetParent(borderFrameGo.transform, false);
            RectTransform rRt = rightB.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(1f, 0f);
            rRt.anchorMax = new Vector2(1f, 1f);
            rRt.pivot = new Vector2(1f, 0.5f);
            rRt.sizeDelta = new Vector2(2.5f, 0f);
            rRt.anchoredPosition = Vector2.zero;
            Image rImg = rightB.GetComponent<Image>();
            rImg.color = borderColor;
            rImg.raycastTarget = false;

            // ---------------------------------------------------------
            // 2. HEADER ROW (Title Badge + Step Counter)
            // ---------------------------------------------------------
            GameObject headerRowGo = new GameObject("HeaderRow", typeof(RectTransform));
            headerRowGo.transform.SetParent(cardGo.transform, false);
            RectTransform headerRt = headerRowGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -6f);
            headerRt.sizeDelta = new Vector2(-24f, 24f);

            // Title Badge
            GameObject titleGo = new GameObject("BadgeTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(headerRowGo.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0f);
            titleRt.anchorMax = new Vector2(0.7f, 1f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            TextMeshProUGUI titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            if (font != null) titleTmp.font = font;
            titleTmp.text = "<b>◆ SCENARIO 1 BRIEFING</b>";
            titleTmp.fontSize = 13.5f;
            titleTmp.color = new Color(0.0f, 0.94f, 1.0f, 1f);
            titleTmp.alignment = TextAlignmentOptions.Left;

            // Step Counter
            GameObject counterGo = new GameObject("StepCounter", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            counterGo.transform.SetParent(headerRowGo.transform, false);
            RectTransform counterRt = counterGo.GetComponent<RectTransform>();
            counterRt.anchorMin = new Vector2(0.7f, 0f);
            counterRt.anchorMax = new Vector2(1f, 1f);
            counterRt.offsetMin = Vector2.zero;
            counterRt.offsetMax = Vector2.zero;
            TextMeshProUGUI counterTmp = counterGo.GetComponent<TextMeshProUGUI>();
            if (font != null) counterTmp.font = font;
            counterTmp.text = "<b>[1/5]</b>";
            counterTmp.fontSize = 13.5f;
            counterTmp.color = new Color(0.58f, 0.65f, 0.78f, 1f);
            counterTmp.alignment = TextAlignmentOptions.Right;

            // Divider Line
            GameObject dividerGo = new GameObject("DividerLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dividerGo.transform.SetParent(cardGo.transform, false);
            RectTransform divRt = dividerGo.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0f, 1f);
            divRt.anchorMax = new Vector2(1f, 1f);
            divRt.pivot = new Vector2(0.5f, 1f);
            divRt.anchoredPosition = new Vector2(0f, -32f);
            divRt.sizeDelta = new Vector2(-24f, 1.5f);
            Image divImg = dividerGo.GetComponent<Image>();
            divImg.color = new Color(0.18f, 0.25f, 0.38f, 0.75f);

            // ---------------------------------------------------------
            // 3. DIALOGUE BODY TEXT
            // ---------------------------------------------------------
            GameObject bodyGo = new GameObject("DialogueBodyText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            bodyGo.transform.SetParent(cardGo.transform, false);
            RectTransform bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.pivot = new Vector2(0.5f, 0.5f);
            bodyRt.offsetMin = new Vector2(18f, 78f);
            bodyRt.offsetMax = new Vector2(-18f, -36f);
            TextMeshProUGUI bodyTmp = bodyGo.GetComponent<TextMeshProUGUI>();
            if (font != null) bodyTmp.font = font;
            bodyTmp.text = "Introduction to Scenario 1...";
            bodyTmp.fontSize = 40f;
            bodyTmp.enableAutoSizing = true;
            bodyTmp.fontSizeMin = 22f;
            bodyTmp.fontSizeMax = 40f;
            bodyTmp.color = new Color(0.94f, 0.96f, 0.98f, 1f);
            bodyTmp.alignment = TextAlignmentOptions.TopLeft;
            bodyTmp.enableWordWrapping = true;
            bodyTmp.lineSpacing = 5f;

            // ---------------------------------------------------------
            // 4. BOTTOM ACTION ROW ([Menu] and [Next >])
            // ---------------------------------------------------------
            GameObject actionRowGo = new GameObject("BottomRow", typeof(RectTransform));
            actionRowGo.transform.SetParent(cardGo.transform, false);
            RectTransform actionRowRt = actionRowGo.GetComponent<RectTransform>();
            actionRowRt.anchorMin = new Vector2(0f, 0f);
            actionRowRt.anchorMax = new Vector2(1f, 0f);
            actionRowRt.pivot = new Vector2(0.5f, 0f);
            actionRowRt.anchoredPosition = new Vector2(0f, 15f);
            actionRowRt.sizeDelta = new Vector2(-36f, 54f);

            // Menu Button (Left) - Zoomed to fit enlarged card
            GameObject menuBtnGo = new GameObject("MenuButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Button), typeof(BoxCollider), typeof(XRSimpleInteractable));
            menuBtnGo.transform.SetParent(actionRowGo.transform, false);
            RectTransform menuRt = menuBtnGo.GetComponent<RectTransform>();
            menuRt.anchorMin = new Vector2(0f, 0.5f);
            menuRt.anchorMax = new Vector2(0f, 0.5f);
            menuRt.pivot = new Vector2(0f, 0.5f);
            menuRt.anchoredPosition = Vector2.zero;
            menuRt.sizeDelta = new Vector2(210f, 52f);

            Image menuImg = menuBtnGo.GetComponent<Image>();
            menuImg.color = new Color(0.15f, 0.18f, 0.26f, 0.9f);

            Outline menuOutline = menuBtnGo.GetComponent<Outline>();
            menuOutline.effectColor = new Color(0.35f, 0.45f, 0.6f, 0.5f);
            menuOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Button menuBtn = menuBtnGo.GetComponent<Button>();
            menuBtn.targetGraphic = menuImg;

            BoxCollider menuCol = menuBtnGo.GetComponent<BoxCollider>();
            menuCol.size = new Vector3(210f, 52f, 10f);
            menuCol.center = Vector3.zero;

            GameObject menuTxtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            menuTxtGo.transform.SetParent(menuBtnGo.transform, false);
            RectTransform menuTxtRt = menuTxtGo.GetComponent<RectTransform>();
            menuTxtRt.anchorMin = Vector2.zero;
            menuTxtRt.anchorMax = Vector2.one;
            menuTxtRt.offsetMin = Vector2.zero;
            menuTxtRt.offsetMax = Vector2.zero;
            TextMeshProUGUI menuTmp = menuTxtGo.GetComponent<TextMeshProUGUI>();
            if (font != null) menuTmp.font = font;
            menuTmp.text = "<b>MENU</b>";
            menuTmp.fontSize = 24f;
            menuTmp.color = new Color(0.75f, 0.8f, 0.9f, 1f);
            menuTmp.alignment = TextAlignmentOptions.Center;

            // Action Button ([Next >] / [Start >]) (Right) - Zoomed to fit enlarged card
            GameObject actionBtnGo = new GameObject("ActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Button), typeof(BoxCollider), typeof(XRSimpleInteractable));
            actionBtnGo.transform.SetParent(actionRowGo.transform, false);
            RectTransform actRt = actionBtnGo.GetComponent<RectTransform>();
            actRt.anchorMin = new Vector2(1f, 0.5f);
            actRt.anchorMax = new Vector2(1f, 0.5f);
            actRt.pivot = new Vector2(1f, 0.5f);
            actRt.anchoredPosition = Vector2.zero;
            actRt.sizeDelta = new Vector2(260f, 52f);

            Image actImg = actionBtnGo.GetComponent<Image>();
            actImg.color = new Color(0.01f, 0.52f, 0.78f, 1f);

            Outline actOutline = actionBtnGo.GetComponent<Outline>();
            actOutline.effectColor = new Color(0.0f, 0.94f, 1.0f, 0.95f);
            actOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Button actBtn = actionBtnGo.GetComponent<Button>();
            actBtn.targetGraphic = actImg;

            BoxCollider actCol = actionBtnGo.GetComponent<BoxCollider>();
            actCol.size = new Vector3(260f, 52f, 10f);
            actCol.center = Vector3.zero;

            GameObject actTxtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            actTxtGo.transform.SetParent(actionBtnGo.transform, false);
            RectTransform actTxtRt = actTxtGo.GetComponent<RectTransform>();
            actTxtRt.anchorMin = Vector2.zero;
            actTxtRt.anchorMax = Vector2.one;
            actTxtRt.offsetMin = Vector2.zero;
            actTxtRt.offsetMax = Vector2.zero;
            TextMeshProUGUI actTmp = actTxtGo.GetComponent<TextMeshProUGUI>();
            if (font != null) actTmp.font = font;
            actTmp.text = "<b>NEXT ></b>";
            actTmp.fontSize = 26f;
            actTmp.color = Color.white;
            actTmp.alignment = TextAlignmentOptions.Center;

            // ---------------------------------------------------------
            // 5. WIRE CONTROLLER
            // ---------------------------------------------------------
            var controller = GetOrAddComponent<Scenario1IntroController>(introPanelGo);
            var so = new SerializedObject(controller);
            so.FindProperty("m_IntroPanel").objectReferenceValue = introPanelGo;
            so.FindProperty("m_TitleText").objectReferenceValue = titleTmp;
            so.FindProperty("m_StepCounterText").objectReferenceValue = counterTmp;
            so.FindProperty("m_StoryText").objectReferenceValue = bodyTmp;
            so.FindProperty("m_ActionButton").objectReferenceValue = actBtn;
            so.FindProperty("m_ActionButtonText").objectReferenceValue = actTmp;
            so.FindProperty("m_ActionButtonImage").objectReferenceValue = actImg;
            so.FindProperty("m_MenuButton").objectReferenceValue = menuBtn;
            so.FindProperty("m_EnableTypewriterEffect").boolValue = true;
            so.FindProperty("m_TypingSpeed").floatValue = 0.025f;
            so.FindProperty("m_PunctuationPause").floatValue = 0.12f;

            GameObject popup = GameObject.Find("Telegram_Notification_Popup");
            if (popup != null)
            {
                so.FindProperty("m_TelegramMessagePopup").objectReferenceValue = popup;
            }

            so.ApplyModifiedProperties();

            // Save Prefab
            string prefabPath = "Assets/Scenarios/Scenario1/Prefabs/Scenario1IntroPanel.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(introPanelGo, prefabPath, InteractionMode.AutomatedAction);

            // Mark Scene Dirty & Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=#10B981><b>[Scenario 1]</b> Scenario1IntroPanel rebuilt at bottom-left under Main Camera!</color>");
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }
    }
}
#endif