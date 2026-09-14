#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using Kannko.Laptop;
using Kannko.Scenario2;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Kannko.Scenario2.Editor
{
    public static class BuildScenario2ExitSystem
    {
        [MenuItem("Scenario2/Build Exit & Decision Panels")]
        public static void Build()
        {
            var worldUI = GameObject.Find("WorldUI");
            if (worldUI == null)
            {
                Debug.LogError("[BuildScenario2ExitSystem] WorldUI not found!");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Scenarios/Scenario2/Textures/UI_DarkCard_Bg.png");
            var pillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Scenarios/Scenario2/Textures/UI_Pill_Bg.png");

            var floatingLaptopTrans = worldUI.transform.Find("FloatingLaptopPanel");
            if (floatingLaptopTrans == null)
            {
                Debug.LogError("[BuildScenario2ExitSystem] FloatingLaptopPanel not found under WorldUI!");
                return;
            }

            var screenContainer = floatingLaptopTrans.Find("ScreenContainer");
            if (screenContainer == null)
            {
                Debug.LogError("[BuildScenario2ExitSystem] ScreenContainer not found under FloatingLaptopPanel!");
                return;
            }

            var enlargedWifiPanel = screenContainer.Find("EnlargedWifiPanel")?.gameObject;
            var laptopBrowserPanel = screenContainer.Find("LaptopBrowserPanel")?.gameObject;
            var browserController = laptopBrowserPanel != null ? laptopBrowserPanel.GetComponent<JakkoFreeWifiBrowserController>() : null;
            var captivePortalCtrl = laptopBrowserPanel != null ? laptopBrowserPanel.GetComponent<CaptivePortalController>() : null;
            var revealWorldSpace = worldUI.transform.Find("Scenario2RevealWorldSpace")?.gameObject;

            // =========================================================
            // 1. BUILD 'STOP PLAYING?' DECISION PANEL ON LAPTOP SCREEN
            // =========================================================
            Transform decisionTrans = screenContainer.Find("StopPlayingDecisionPanel");
            GameObject decisionGo;
            if (decisionTrans == null)
            {
                decisionGo = new GameObject("StopPlayingDecisionPanel", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(CanvasGroup));
                decisionGo.transform.SetParent(screenContainer, false);
            }
            else
            {
                decisionGo = decisionTrans.gameObject;
            }

            var decisionRt = decisionGo.GetComponent<RectTransform>();
            decisionRt.anchorMin = new Vector2(0.5f, 0.5f);
            decisionRt.anchorMax = new Vector2(0.5f, 0.5f);
            decisionRt.pivot = new Vector2(0.5f, 0.5f);
            decisionRt.sizeDelta = new Vector2(620f, 360f);
            decisionRt.anchoredPosition = new Vector2(0f, 0f);

            var decisionImg = decisionGo.GetComponent<Image>();
            if (cardSprite != null)
            {
                decisionImg.sprite = cardSprite;
                decisionImg.type = Image.Type.Sliced;
            }
            decisionImg.color = new Color(0.11f, 0.12f, 0.16f, 0.98f);

            var decisionOutline = decisionGo.GetComponent<Outline>();
            if (decisionOutline == null) decisionOutline = decisionGo.AddComponent<Outline>();
            decisionOutline.effectColor = new Color(0.24f, 0.28f, 0.38f, 0.8f);
            decisionOutline.effectDistance = new Vector2(2f, -2f);

            // Title
            var titleGo = GetOrCreateChild(decisionGo.transform, "TitleText");
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -32f);
            titleRt.sizeDelta = new Vector2(-40f, 48f);
            var titleTmp = GetOrAddComponent<TextMeshProUGUI>(titleGo);
            if (font != null) titleTmp.font = font;
            titleTmp.text = "<b>STOP PLAYING?</b>";
            titleTmp.fontSize = 36f;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;

            // Subtitle
            var subGo = GetOrCreateChild(decisionGo.transform, "SubtitleText");
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 1f);
            subRt.anchorMax = new Vector2(1f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -88f);
            subRt.sizeDelta = new Vector2(-50f, 40f);
            var subTmp = GetOrAddComponent<TextMeshProUGUI>(subGo);
            if (font != null) subTmp.font = font;
            subTmp.text = "Do you want to stop this scenario and leave the cafe?";
            subTmp.fontSize = 21f;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.85f, 0.88f, 0.94f, 1f);

            // Description / Note
            var noteGo = GetOrCreateChild(decisionGo.transform, "NoteText");
            var noteRt = noteGo.GetComponent<RectTransform>();
            noteRt.anchorMin = new Vector2(0f, 1f);
            noteRt.anchorMax = new Vector2(1f, 1f);
            noteRt.pivot = new Vector2(0.5f, 1f);
            noteRt.anchoredPosition = new Vector2(0f, -135f);
            noteRt.sizeDelta = new Vector2(-60f, 48f);
            var noteTmp = GetOrAddComponent<TextMeshProUGUI>(noteGo);
            if (font != null) noteTmp.font = font;
            noteTmp.text = "You can return to the Wi-Fi list to continue the investigation, or exit the scenario now.";
            noteTmp.fontSize = 17f;
            noteTmp.alignment = TextAlignmentOptions.Center;
            noteTmp.color = new Color(0.58f, 0.62f, 0.72f, 1f);

            // Button container
            var btnRowGo = GetOrCreateChild(decisionGo.transform, "ActionButtons");
            var btnRowRt = btnRowGo.GetComponent<RectTransform>();
            btnRowRt.anchorMin = new Vector2(0f, 0f);
            btnRowRt.anchorMax = new Vector2(1f, 0f);
            btnRowRt.pivot = new Vector2(0.5f, 0f);
            btnRowRt.anchoredPosition = new Vector2(0f, 28f);
            btnRowRt.sizeDelta = new Vector2(-40f, 64f);

            // Continue Scenario Button
            var continueBtnGo = GetOrCreateChild(btnRowGo.transform, "ContinueScenarioButton");
            var continueRt = continueBtnGo.GetComponent<RectTransform>();
            continueRt.anchorMin = new Vector2(0f, 0.5f);
            continueRt.anchorMax = new Vector2(0f, 0.5f);
            continueRt.pivot = new Vector2(0f, 0.5f);
            continueRt.anchoredPosition = new Vector2(10f, 0f);
            continueRt.sizeDelta = new Vector2(265f, 56f);
            var continueImg = GetOrAddComponent<Image>(continueBtnGo);
            if (pillSprite != null) { continueImg.sprite = pillSprite; continueImg.type = Image.Type.Sliced; }
            continueImg.color = new Color(0.20f, 0.23f, 0.32f, 1f);
            var continueBtn = GetOrAddComponent<Button>(continueBtnGo);
            continueBtn.targetGraphic = continueImg;
            var continueCol = continueBtn.colors;
            continueCol.highlightedColor = new Color(0.28f, 0.33f, 0.45f, 1f);
            continueCol.pressedColor = new Color(0.14f, 0.16f, 0.22f, 1f);
            continueBtn.colors = continueCol;

            var continueTxtGo = GetOrCreateChild(continueBtnGo.transform, "Text");
            var continueTxtRt = continueTxtGo.GetComponent<RectTransform>();
            continueTxtRt.anchorMin = Vector2.zero;
            continueTxtRt.anchorMax = Vector2.one;
            continueTxtRt.offsetMin = Vector2.zero;
            continueTxtRt.offsetMax = Vector2.zero;
            var continueTmp = GetOrAddComponent<TextMeshProUGUI>(continueTxtGo);
            if (font != null) continueTmp.font = font;
            continueTmp.text = "<b>Continue Scenario</b>";
            continueTmp.fontSize = 20f;
            continueTmp.alignment = TextAlignmentOptions.Center;
            continueTmp.color = Color.white;

            var continueCol3D = GetOrAddComponent<BoxCollider>(continueBtnGo);
            continueCol3D.center = Vector3.zero;
            continueCol3D.size = new Vector3(265f, 56f, 50f);
            GetOrAddComponent<XRSimpleInteractable>(continueBtnGo);

            // Get Out of This Cafe Button
            var getOutBtnGo = GetOrCreateChild(btnRowGo.transform, "GetOutOfCafeButton");
            var getOutRt = getOutBtnGo.GetComponent<RectTransform>();
            getOutRt.anchorMin = new Vector2(1f, 0.5f);
            getOutRt.anchorMax = new Vector2(1f, 0.5f);
            getOutRt.pivot = new Vector2(1f, 0.5f);
            getOutRt.anchoredPosition = new Vector2(-10f, 0f);
            getOutRt.sizeDelta = new Vector2(265f, 56f);
            var getOutImg = GetOrAddComponent<Image>(getOutBtnGo);
            if (pillSprite != null) { getOutImg.sprite = pillSprite; getOutImg.type = Image.Type.Sliced; }
            getOutImg.color = new Color(0.72f, 0.18f, 0.18f, 1f); // Distinct danger/warning accent
            var getOutBtn = GetOrAddComponent<Button>(getOutBtnGo);
            getOutBtn.targetGraphic = getOutImg;
            var getOutCol = getOutBtn.colors;
            getOutCol.highlightedColor = new Color(0.85f, 0.25f, 0.25f, 1f);
            getOutCol.pressedColor = new Color(0.50f, 0.12f, 0.12f, 1f);
            getOutBtn.colors = getOutCol;

            var getOutTxtGo = GetOrCreateChild(getOutBtnGo.transform, "Text");
            var getOutTxtRt = getOutTxtGo.GetComponent<RectTransform>();
            getOutTxtRt.anchorMin = Vector2.zero;
            getOutTxtRt.anchorMax = Vector2.one;
            getOutTxtRt.offsetMin = Vector2.zero;
            getOutTxtRt.offsetMax = Vector2.zero;
            var getOutTmp = GetOrAddComponent<TextMeshProUGUI>(getOutTxtGo);
            if (font != null) getOutTmp.font = font;
            getOutTmp.text = "<b>Get Out of This Cafe</b>";
            getOutTmp.fontSize = 20f;
            getOutTmp.alignment = TextAlignmentOptions.Center;
            getOutTmp.color = Color.white;

            var getOutCol3D = GetOrAddComponent<BoxCollider>(getOutBtnGo);
            getOutCol3D.center = Vector3.zero;
            getOutCol3D.size = new Vector3(265f, 56f, 50f);
            GetOrAddComponent<XRSimpleInteractable>(getOutBtnGo);

            // Decision panel initially inactive
            decisionGo.SetActive(false);


            // =========================================================
            // 2. BUILD CAMERA-FACING EXIT CONFIRMATION WORLD SPACE PANEL
            // =========================================================
            Transform exitTrans = worldUI.transform.Find("Scenario2ExitConfirmationPanel");
            GameObject exitGo;
            if (exitTrans == null)
            {
                exitGo = new GameObject("Scenario2ExitConfirmationPanel",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster),
                    typeof(UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster),
                    typeof(CanvasGroup),
                    typeof(Image),
                    typeof(Outline),
                    typeof(Scenario2ExitController));
                exitGo.transform.SetParent(worldUI.transform, false);
            }
            else
            {
                exitGo = exitTrans.gameObject;
            }

            var exitCanvas = GetOrAddComponent<Canvas>(exitGo);
            exitCanvas.renderMode = RenderMode.WorldSpace;
            exitCanvas.sortingOrder = 25;
            exitCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

            var exitScaler = GetOrAddComponent<CanvasScaler>(exitGo);
            exitScaler.dynamicPixelsPerUnit = 10f;

            GetOrAddComponent<GraphicRaycaster>(exitGo);
            GetOrAddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>(exitGo);
            GetOrAddComponent<CanvasGroup>(exitGo);

            var exitRt = exitGo.GetComponent<RectTransform>();
            // Camera pos is (0.69, 1.32, -0.71). Player looks in -X direction.
            // Position panel 0.69m in front of camera at (0.00, 1.32, -0.71), rotation (0, 270, 0)
            exitRt.position = new Vector3(0.00f, 1.32f, -0.71f);
            exitRt.eulerAngles = new Vector3(0f, 270f, 0f);
            exitRt.localScale = new Vector3(0.0008f, 0.0008f, 0.0008f);
            exitRt.sizeDelta = new Vector2(760f, 440f);
            exitRt.pivot = new Vector2(0.5f, 0.5f);

            var exitImg = GetOrAddComponent<Image>(exitGo);
            if (cardSprite != null) { exitImg.sprite = cardSprite; exitImg.type = Image.Type.Sliced; }
            exitImg.color = new Color(0.08f, 0.10f, 0.15f, 0.98f);

            var exitOutline = GetOrAddComponent<Outline>(exitGo);
            exitOutline.effectColor = new Color(0.24f, 0.35f, 0.55f, 0.85f);
            exitOutline.effectDistance = new Vector2(3f, -3f);

            // Badge at top
            var badgeGo = GetOrCreateChild(exitGo.transform, "WarningBadge");
            var badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.5f, 1f);
            badgeRt.anchorMax = new Vector2(0.5f, 1f);
            badgeRt.pivot = new Vector2(0.5f, 1f);
            badgeRt.anchoredPosition = new Vector2(0f, -28f);
            badgeRt.sizeDelta = new Vector2(240f, 32f);
            var badgeTmp = GetOrAddComponent<TextMeshProUGUI>(badgeGo);
            if (font != null) badgeTmp.font = font;
            badgeTmp.text = "<color=#FF453A><b>LEAVE SCENARIO</b></color>";
            badgeTmp.fontSize = 18f;
            badgeTmp.alignment = TextAlignmentOptions.Center;

            // Title
            var exitTitleGo = GetOrCreateChild(exitGo.transform, "ExitTitle");
            var exitTitleRt = exitTitleGo.GetComponent<RectTransform>();
            exitTitleRt.anchorMin = new Vector2(0f, 1f);
            exitTitleRt.anchorMax = new Vector2(1f, 1f);
            exitTitleRt.pivot = new Vector2(0.5f, 1f);
            exitTitleRt.anchoredPosition = new Vector2(0f, -68f);
            exitTitleRt.sizeDelta = new Vector2(-40f, 54f);
            var exitTitleTmp = GetOrAddComponent<TextMeshProUGUI>(exitTitleGo);
            if (font != null) exitTitleTmp.font = font;
            exitTitleTmp.text = "<b>EXIT THIS SCENARIO?</b>";
            exitTitleTmp.fontSize = 42f;
            exitTitleTmp.alignment = TextAlignmentOptions.Center;
            exitTitleTmp.color = Color.white;

            // Subtitle
            var exitSubGo = GetOrCreateChild(exitGo.transform, "ExitSubtitle");
            var exitSubRt = exitSubGo.GetComponent<RectTransform>();
            exitSubRt.anchorMin = new Vector2(0f, 1f);
            exitSubRt.anchorMax = new Vector2(1f, 1f);
            exitSubRt.pivot = new Vector2(0.5f, 1f);
            exitSubRt.anchoredPosition = new Vector2(0f, -128f);
            exitSubRt.sizeDelta = new Vector2(-40f, 36f);
            var exitSubTmp = GetOrAddComponent<TextMeshProUGUI>(exitSubGo);
            if (font != null) exitSubTmp.font = font;
            exitSubTmp.text = "Are you sure you want to leave Scenario 2?";
            exitSubTmp.fontSize = 24f;
            exitSubTmp.alignment = TextAlignmentOptions.Center;
            exitSubTmp.color = new Color(0.85f, 0.88f, 0.94f, 1f);

            // Notice Card
            var noticeGo = GetOrCreateChild(exitGo.transform, "NoticeCard");
            var noticeRt = noticeGo.GetComponent<RectTransform>();
            noticeRt.anchorMin = new Vector2(0.5f, 1f);
            noticeRt.anchorMax = new Vector2(0.5f, 1f);
            noticeRt.pivot = new Vector2(0.5f, 1f);
            noticeRt.anchoredPosition = new Vector2(0f, -180f);
            noticeRt.sizeDelta = new Vector2(660f, 85f);
            var noticeImg = GetOrAddComponent<Image>(noticeGo);
            if (cardSprite != null) { noticeImg.sprite = cardSprite; noticeImg.type = Image.Type.Sliced; }
            noticeImg.color = new Color(0.14f, 0.17f, 0.24f, 0.95f);

            var noticeTxtGo = GetOrCreateChild(noticeGo.transform, "NoticeText");
            var noticeTxtRt = noticeTxtGo.GetComponent<RectTransform>();
            noticeTxtRt.anchorMin = Vector2.zero;
            noticeTxtRt.anchorMax = Vector2.one;
            noticeTxtRt.offsetMin = new Vector2(20f, 10f);
            noticeTxtRt.offsetMax = new Vector2(-20f, -10f);
            var noticeTmp = GetOrAddComponent<TextMeshProUGUI>(noticeTxtGo);
            if (font != null) noticeTmp.font = font;
            noticeTmp.text = "<color=#FFA500><b>Notice:</b></color> Any unsaved scenario progress will be lost.\nYou will return to the scenario selection menu.";
            noticeTmp.fontSize = 19f;
            noticeTmp.alignment = TextAlignmentOptions.Center;
            noticeTmp.color = new Color(0.70f, 0.75f, 0.85f, 1f);

            // Exit Action Buttons Row
            var exitBtnRow = GetOrCreateChild(exitGo.transform, "ActionButtons");
            var exitBtnRowRt = exitBtnRow.GetComponent<RectTransform>();
            exitBtnRowRt.anchorMin = new Vector2(0f, 0f);
            exitBtnRowRt.anchorMax = new Vector2(1f, 0f);
            exitBtnRowRt.pivot = new Vector2(0.5f, 0f);
            exitBtnRowRt.anchoredPosition = new Vector2(0f, 32f);
            exitBtnRowRt.sizeDelta = new Vector2(-50f, 66f);

            // Back to Scenario Button
            var backBtnGo = GetOrCreateChild(exitBtnRow.transform, "BackToScenarioButton");
            var backRt = backBtnGo.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0f, 0.5f);
            backRt.anchorMax = new Vector2(0f, 0.5f);
            backRt.pivot = new Vector2(0f, 0.5f);
            backRt.anchoredPosition = new Vector2(15f, 0f);
            backRt.sizeDelta = new Vector2(320f, 60f);
            var backImg = GetOrAddComponent<Image>(backBtnGo);
            if (pillSprite != null) { backImg.sprite = pillSprite; backImg.type = Image.Type.Sliced; }
            backImg.color = new Color(0.20f, 0.23f, 0.32f, 1f);
            var backBtn = GetOrAddComponent<Button>(backBtnGo);
            backBtn.targetGraphic = backImg;
            var backCol = backBtn.colors;
            backCol.highlightedColor = new Color(0.28f, 0.33f, 0.45f, 1f);
            backCol.pressedColor = new Color(0.14f, 0.16f, 0.22f, 1f);
            backBtn.colors = backCol;

            var backTxtGo = GetOrCreateChild(backBtnGo.transform, "Text");
            var backTxtRt = backTxtGo.GetComponent<RectTransform>();
            backTxtRt.anchorMin = Vector2.zero;
            backTxtRt.anchorMax = Vector2.one;
            backTxtRt.offsetMin = Vector2.zero;
            backTxtRt.offsetMax = Vector2.zero;
            var backTmp = GetOrAddComponent<TextMeshProUGUI>(backTxtGo);
            if (font != null) backTmp.font = font;
            backTmp.text = "<b>Back to Scenario</b>";
            backTmp.fontSize = 22f;
            backTmp.alignment = TextAlignmentOptions.Center;
            backTmp.color = Color.white;

            var backCol3D = GetOrAddComponent<BoxCollider>(backBtnGo);
            backCol3D.center = Vector3.zero;
            backCol3D.size = new Vector3(320f, 60f, 50f);
            GetOrAddComponent<XRSimpleInteractable>(backBtnGo);

            // Exit Scenario Button
            var exitBtnElemGo = GetOrCreateChild(exitBtnRow.transform, "ExitScenarioButton");
            var exitBtnRt = exitBtnElemGo.GetComponent<RectTransform>();
            exitBtnRt.anchorMin = new Vector2(1f, 0.5f);
            exitBtnRt.anchorMax = new Vector2(1f, 0.5f);
            exitBtnRt.pivot = new Vector2(1f, 0.5f);
            exitBtnRt.anchoredPosition = new Vector2(-15f, 0f);
            exitBtnRt.sizeDelta = new Vector2(320f, 60f);
            var exitBtnElemImg = GetOrAddComponent<Image>(exitBtnElemGo);
            if (pillSprite != null) { exitBtnElemImg.sprite = pillSprite; exitBtnElemImg.type = Image.Type.Sliced; }
            exitBtnElemImg.color = new Color(0.78f, 0.18f, 0.18f, 1f); // Distinct danger accent
            var exitConfirmBtn = GetOrAddComponent<Button>(exitBtnElemGo);
            exitConfirmBtn.targetGraphic = exitBtnElemImg;
            var exitConfirmCol = exitConfirmBtn.colors;
            exitConfirmCol.highlightedColor = new Color(0.92f, 0.25f, 0.25f, 1f);
            exitConfirmCol.pressedColor = new Color(0.55f, 0.12f, 0.12f, 1f);
            exitConfirmBtn.colors = exitConfirmCol;

            var exitConfirmTxtGo = GetOrCreateChild(exitBtnElemGo.transform, "Text");
            var exitConfirmTxtRt = exitConfirmTxtGo.GetComponent<RectTransform>();
            exitConfirmTxtRt.anchorMin = Vector2.zero;
            exitConfirmTxtRt.anchorMax = Vector2.one;
            exitConfirmTxtRt.offsetMin = Vector2.zero;
            exitConfirmTxtRt.offsetMax = Vector2.zero;
            var exitConfirmTmp = GetOrAddComponent<TextMeshProUGUI>(exitConfirmTxtGo);
            if (font != null) exitConfirmTmp.font = font;
            exitConfirmTmp.text = "<b>Exit Scenario</b>";
            exitConfirmTmp.fontSize = 22f;
            exitConfirmTmp.alignment = TextAlignmentOptions.Center;
            exitConfirmTmp.color = Color.white;

            var exitCol3D = GetOrAddComponent<BoxCollider>(exitBtnElemGo);
            exitCol3D.center = Vector3.zero;
            exitCol3D.size = new Vector3(320f, 60f, 50f);
            GetOrAddComponent<XRSimpleInteractable>(exitBtnElemGo);

            var exitCtrl = GetOrAddComponent<Scenario2ExitController>(exitGo);

            // Exit confirmation initially inactive
            exitGo.SetActive(false);


            // =========================================================
            // 3. WIRE UP SERIALIZED REFERENCES IN INSPECTORS
            // =========================================================
            var serializedExit = new SerializedObject(exitCtrl);
            serializedExit.FindProperty("m_ExitConfirmationPanel").objectReferenceValue = exitGo;
            serializedExit.FindProperty("m_FloatingLaptopPanel").objectReferenceValue = floatingLaptopTrans.gameObject;
            serializedExit.FindProperty("m_EnlargedWifiPanel").objectReferenceValue = enlargedWifiPanel;
            serializedExit.FindProperty("m_BackToScenarioButton").objectReferenceValue = backBtn;
            serializedExit.FindProperty("m_ExitScenarioButton").objectReferenceValue = exitConfirmBtn;
            serializedExit.ApplyModifiedProperties();

            if (browserController != null)
            {
                var serializedBrowser = new SerializedObject(browserController);
                serializedBrowser.FindProperty("m_StopPlayingDecisionPanel").objectReferenceValue = decisionGo;
                serializedBrowser.FindProperty("m_ContinueScenarioButton").objectReferenceValue = continueBtn;
                serializedBrowser.FindProperty("m_GetOutOfCafeButton").objectReferenceValue = getOutBtn;
                serializedBrowser.FindProperty("m_ExitController").objectReferenceValue = exitCtrl;
                serializedBrowser.ApplyModifiedProperties();
                browserController.EnsureCollidersAndInteractables();
                browserController.SetupButtonListeners();
            }

            if (captivePortalCtrl != null)
            {
                var serializedPortal = new SerializedObject(captivePortalCtrl);
                serializedPortal.FindProperty("m_FloatingLaptopPanel").objectReferenceValue = floatingLaptopTrans.gameObject;
                serializedPortal.FindProperty("m_RevealWorldSpaceCanvas").objectReferenceValue = revealWorldSpace;
                serializedPortal.ApplyModifiedProperties();
                captivePortalCtrl.FindReferencesIfNull();
            }

            exitCtrl.EnsureCollidersAndInteractables();
            exitCtrl.SetupButtonListeners();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("[BuildScenario2ExitSystem] SUCCESS! 'STOP PLAYING?' and 'EXIT THIS SCENARIO?' panels built and configured cleanly.");
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
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
