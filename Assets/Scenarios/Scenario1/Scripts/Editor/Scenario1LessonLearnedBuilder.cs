using System;
using CADT.VR.Cybersecurity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CADT.VR.Scenarios.Scenario1.Editor
{
    public static class Scenario1LessonLearnedBuilder
    {
        [MenuItem("CADT VR/Scenarios/Build Scenario 1 Lesson Learned Panel", priority = 32)]
        public static void BuildLessonLearnedPanel()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "Scenario1")
            {
                activeScene = EditorSceneManager.OpenScene("Assets/Scenarios/Scenario1/Scenes/Scenario1.unity", OpenSceneMode.Single);
            }

            Debug.Log("<color=#00F0FF><b>[CADT VR]</b> Building Overhead Lesson Learned Panel (Above PC Screen)...</color>");

            // 1. Find Workstation Setup & Monitor Screen Canvas
            GameObject workstationSetup = GameObject.Find("Workstation_Setup");
            if (workstationSetup == null)
            {
                Debug.LogError("Workstation_Setup not found in scene!");
                return;
            }

            GameObject monitorCanvas = GameObject.Find("Monitor_Screen_Canvas");
            if (monitorCanvas == null)
            {
                Debug.LogError("Monitor_Screen_Canvas not found in scene!");
                return;
            }

            Transform cmdScreenTrans = monitorCanvas.transform.Find("Command_Prompt_Screen");
            if (cmdScreenTrans == null)
            {
                Debug.LogError("Command_Prompt_Screen not found under Monitor_Screen_Canvas!");
                return;
            }

            CommandPromptBreachSimulator breachSim = cmdScreenTrans.GetComponent<CommandPromptBreachSimulator>();
            if (breachSim == null)
            {
                Debug.LogError("CommandPromptBreachSimulator component missing on Command_Prompt_Screen!");
                return;
            }

            // 2. Load Fonts & Audio Assets
            TMP_FontAsset boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter/Inter-Bold SDF.asset");
            if (boldFont == null)
            {
                boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }
            TMP_FontAsset regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter/Inter-Regular SDF.asset");
            if (regularFont == null) regularFont = boldFont;

            AudioClip clickSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/DemoAssets/Audio/ButtonClick.wav");
            AudioClip popSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/DemoAssets/Audio/TeleportSelection.wav");
            if (popSfx == null)
            {
                popSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/DemoAssets/Audio/Button Pop.wav");
            }

            // 3. Remove previous panel instances (under Canvas or Workstation)
            Transform oldPanelCanvas = monitorCanvas.transform.Find("Lesson_Learned_Panel");
            if (oldPanelCanvas != null)
            {
                Undo.DestroyObjectImmediate(oldPanelCanvas.gameObject);
            }
            Transform oldPanelWorkstation = workstationSetup.transform.Find("Lesson_Learned_Panel");
            if (oldPanelWorkstation != null)
            {
                Undo.DestroyObjectImmediate(oldPanelWorkstation.gameObject);
            }

            // 4. Create Standalone World Space Canvas ABOVE the PC Screen
            GameObject panelRoot = new GameObject("Lesson_Learned_Panel",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster), typeof(Image),
                typeof(Outline), typeof(Scenario1_LessonLearnedController), typeof(AudioSource));
            panelRoot.transform.SetParent(workstationSetup.transform, false);

            // World Space Canvas Setup
            Canvas canvasComp = panelRoot.GetComponent<Canvas>();
            canvasComp.renderMode = RenderMode.WorldSpace;
            canvasComp.worldCamera = Camera.main;

            CanvasScaler scaler = panelRoot.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 1f;
            scaler.referencePixelsPerUnit = 100f;

            TrackedDeviceGraphicRaycaster xrRaycaster = panelRoot.GetComponent<TrackedDeviceGraphicRaycaster>();
            xrRaycaster.checkFor2DOcclusion = false;
            xrRaycaster.checkFor3DOcclusion = false;

            // Positioning: ABOVE the PC screen (Monitor top bezel is at y = 1.87, z = 5.12)
            // Position panel at y = 2.12, z = 5.48 tilted down by 18 degrees directly facing the seated player
            RectTransform rootRt = panelRoot.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(1360f, 900f);
            rootRt.position = new Vector3(-1.58f, 2.12f, 5.48f);
            rootRt.eulerAngles = new Vector3(-18f, 180f, 0f);
            rootRt.localScale = new Vector3(0.00075f, 0.00075f, 0.00075f);

            // Sleek cyber floating glass background
            Image rootBg = panelRoot.GetComponent<Image>();
            rootBg.color = new Color(0.03f, 0.05f, 0.08f, 0.98f);

            Outline rootOutline = panelRoot.GetComponent<Outline>();
            rootOutline.effectColor = new Color(0.35f, 0.60f, 0.95f, 0.85f);
            rootOutline.effectDistance = new Vector2(3f, -3f);

            AudioSource audioSource = panelRoot.GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 0.5f;
            audioSource.maxDistance = 10f;

            Scenario1_LessonLearnedController controller = panelRoot.GetComponent<Scenario1_LessonLearnedController>();

            // -------------------------------------------------------------
            // 5. TOP TAB NAVIGATION BAR (Attached to top of floating panel)
            // -------------------------------------------------------------
            GameObject tabContainer = CreateUIObject("Tabs_Container", panelRoot.transform);
            RectTransform tcRt = tabContainer.GetComponent<RectTransform>();
            tcRt.anchorMin = new Vector2(0.5f, 1f);
            tcRt.anchorMax = new Vector2(0.5f, 1f);
            tcRt.pivot = new Vector2(0.5f, 1f);
            tcRt.anchoredPosition = new Vector2(0f, -20f);
            tcRt.sizeDelta = new Vector2(1300f, 54f);

            string[] tabLabels = new string[]
            {
                "1. YOUR LOSSES",
                "2. SECURITY ALERT",
                "3. OUT-OF-BAND",
                "4. SCENARIO COMPLETE"
            };

            Button[] tabButtons = new Button[4];
            float tabWidth = 308f;
            float tabGap = 16f;
            float startX = -((4 * tabWidth + 3 * tabGap) / 2f) + (tabWidth / 2f);

            Color tab0ActiveColor = new Color(0.85f, 0.15f, 0.15f, 0.95f);
            Color tabInactiveColor = new Color(0.10f, 0.14f, 0.20f, 0.85f);

            for (int i = 0; i < 4; i++)
            {
                Color bg = (i == 0) ? tab0ActiveColor : tabInactiveColor;
                Color border = (i == 0) ? Color.white : new Color(0.25f, 0.32f, 0.42f, 0.60f);

                GameObject tabObj = CreateButton($"Tab_Button_{i}", tabContainer.transform, tabLabels[i], 20, boldFont, bg, border);
                RectTransform trt = tabObj.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.5f, 0.5f);
                trt.anchorMax = new Vector2(0.5f, 0.5f);
                trt.pivot = new Vector2(0.5f, 0.5f);
                trt.anchoredPosition = new Vector2(startX + i * (tabWidth + tabGap), 0f);
                trt.sizeDelta = new Vector2(tabWidth, 50f);
                tabButtons[i] = tabObj.GetComponent<Button>();
            }

            // -------------------------------------------------------------
            // 6. CARDS CONTAINER & THE 4 INDIVIDUAL CARDS (Center of floating panel)
            // -------------------------------------------------------------
            GameObject cardsContainer = CreateUIObject("Cards_Container", panelRoot.transform);
            RectTransform ccRt = cardsContainer.GetComponent<RectTransform>();
            ccRt.anchorMin = new Vector2(0.5f, 0.5f);
            ccRt.anchorMax = new Vector2(0.5f, 0.5f);
            ccRt.pivot = new Vector2(0.5f, 0.5f);
            ccRt.anchoredPosition = new Vector2(0f, 8f);
            ccRt.sizeDelta = new Vector2(1300f, 720f);

            GameObject[] lessonCards = new GameObject[4];

            // -----------------------------
            // CARD 0: YOUR LOSSES
            // -----------------------------
            GameObject card0 = CreateCard("Card_0_Losses", cardsContainer.transform,
                new Color(0.08f, 0.03f, 0.04f, 0.98f), new Color(0.95f, 0.20f, 0.20f, 0.95f));
            SetAnchors(card0.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            CreateCardHeader(card0.transform, "<b>YOUR LOSSES:</b>", 36, new Color(1.0f, 0.25f, 0.25f), boldFont, new Color(0.85f, 0.20f, 0.20f, 0.5f));

            string lossesContent =
                "<line-height=145%>" +
                "<color=#EF4444><b>✗</b></color>  <b>Bank login credentials (ABA Bank)</b>\n" +
                "<color=#EF4444><b>✗</b></color>  <b>Work email password</b>\n" +
                "<color=#EF4444><b>✗</b></color>  <b>Telegram account session</b>\n" +
                "<color=#EF4444><b>✗</b></color>  <b>PayPal account access</b>\n" +
                "<color=#EF4444><b>✗</b></color>  <b>Webcam photo of you</b>\n" +
                "<color=#EF4444><b>✗</b></color>  <b>847 personal contacts exposed</b>\n" +
                "<color=#EF4444><b>✗</b></color>  <b>234 corporate contacts exposed</b>\n" +
                "<color=#EF4444><b>✗</b></color>  <b>Your identity used to spam 10+ people</b>\n" +
                "<color=#EF4444><b>✗</b></color>  <b>Potential financial theft</b>\n" +
                "<color=#EF4444><b>✗</b></color>  <b>Potential identity theft</b>" +
                "</line-height>";

            CreateCardBody(card0.transform, "Losses_Body", lossesContent, 26, boldFont);
            lessonCards[0] = card0;

            // -----------------------------
            // CARD 1: SECURITY ALERT
            // -----------------------------
            GameObject card1 = CreateCard("Card_1_Alert", cardsContainer.transform,
                new Color(0.10f, 0.03f, 0.03f, 0.98f), new Color(0.95f, 0.35f, 0.15f, 0.95f));
            SetAnchors(card1.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            CreateCardHeader(card1.transform, "<b>[!] SECURITY ALERT</b>", 36, new Color(1.0f, 0.35f, 0.25f), boldFont, new Color(0.95f, 0.35f, 0.15f, 0.5f));

            string alertContent =
                "<line-height=180%><size=30>" +
                "<color=#EF4444>●</color>  <b>Workstation Network Isolated</b>\n\n" +
                "<color=#F59E0B>●</color>  <b>Active InfoStealer Detected</b>\n\n" +
                "<color=#EF4444>●</color>  <b>All files and credentials may be compromised</b>\n\n" +
                "<color=#38BDF8>●</color>  <b>Contact IT Security immediately</b>" +
                "</size></line-height>";

            CreateCardBody(card1.transform, "Alert_Body", alertContent, 30, boldFont);
            lessonCards[1] = card1;

            // -----------------------------
            // CARD 2: OUT-OF-BAND VERIFICATION
            // -----------------------------
            GameObject card2 = CreateCard("Card_2_Verification", cardsContainer.transform,
                new Color(0.03f, 0.06f, 0.12f, 0.98f), new Color(0.20f, 0.70f, 0.98f, 0.95f));
            SetAnchors(card2.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            CreateCardHeader(card2.transform, "<b>OUT-OF-BAND VERIFICATION</b>", 36, new Color(0.25f, 0.80f, 1.0f), boldFont, new Color(0.20f, 0.70f, 0.98f, 0.5f));

            string oobContent =
                "<line-height=160%><size=28>" +
                "<b>\"Out-of-band\"</b> means verifying through a <b>DIFFERENT</b> communication channel.\n\n" +
                "If you receive a suspicious file via chat,\n" +
                "<color=#38BDF8><b>verify by calling the sender.</b></color>\n\n" +
                "If you receive a suspicious email,\n" +
                "<color=#38BDF8><b>verify by calling the company.</b></color>\n\n" +
                "<color=#34D399><b>This single habit stops 90% of phishing attacks.</b></color>" +
                "</size></line-height>";

            CreateCardBody(card2.transform, "OOB_Body", oobContent, 28, boldFont);
            lessonCards[2] = card2;

            // -----------------------------
            // CARD 3: SCENARIO 1 COMPLETE
            // -----------------------------
            GameObject card3 = CreateCard("Card_3_Summary", cardsContainer.transform,
                new Color(0.03f, 0.08f, 0.06f, 0.98f), new Color(0.15f, 0.85f, 0.50f, 0.95f));
            SetAnchors(card3.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            CreateCardHeader(card3.transform, "<b>SCENARIO 1 COMPLETE</b>", 36, new Color(0.25f, 0.95f, 0.60f), boldFont, new Color(0.15f, 0.85f, 0.50f, 0.5f));

            string summaryContent =
                "<line-height=150%><size=27>" +
                "<b>Red Flags Detected:</b>  <color=#F87171><b>0 / 3</b></color>\n" +
                "<b>Choice Made:</b>  <color=#EF4444><b>Unsafe (Executed Payload)</b></color>\n" +
                "<b>XP Earned:</b>  <color=#F87171><b>0 / 100 XP</b></color>\n\n" +
                "<color=#FBBF24><b>KEY RULES TO REMEMBER:</b></color>\n" +
                "<b>1. Documents are never executables</b>\n" +
                "<b>2. Password-protected archives = suspicious</b>\n" +
                "<b>3. Verify out-of-band through a friend</b>" +
                "</size></line-height>";

            CreateCardBody(card3.transform, "Summary_Body", summaryContent, 27, boldFont);

            // In-Card "RETURN TO MENU" button matching the ASCII diagram with larger font
            GameObject inCardMenuBtnObj = CreateButton("InCard_ReturnToMenu_Button", card3.transform,
                "RETURN TO MENU", 25, boldFont, new Color(0.15f, 0.45f, 0.35f, 0.95f), new Color(0.25f, 0.95f, 0.60f, 0.95f));
            RectTransform imRt = inCardMenuBtnObj.GetComponent<RectTransform>();
            imRt.anchorMin = new Vector2(0.5f, 0f);
            imRt.anchorMax = new Vector2(0.5f, 0f);
            imRt.pivot = new Vector2(0.5f, 0f);
            imRt.anchoredPosition = new Vector2(0f, 32f);
            imRt.sizeDelta = new Vector2(440f, 64f);
            Button inCardMenuBtn = inCardMenuBtnObj.GetComponent<Button>();

            lessonCards[3] = card3;

            // Set initial active state: only card 0 visible
            for (int i = 0; i < 4; i++)
            {
                lessonCards[i].SetActive(i == 0);
            }

            // -------------------------------------------------------------
            // 7. BOTTOM ACTION BAR (Attached directly to bottom of floating panel)
            // -------------------------------------------------------------
            GameObject bottomBar = CreateUIObject("Bottom_Action_Bar", panelRoot.transform);
            RectTransform bbRt = bottomBar.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0.5f, 0f);
            bbRt.anchorMax = new Vector2(0.5f, 0f);
            bbRt.pivot = new Vector2(0.5f, 0f);
            bbRt.anchoredPosition = new Vector2(0f, 20f);
            bbRt.sizeDelta = new Vector2(1300f, 64f);

            // Replay Button (Far Left)
            GameObject replayBtnObj = CreateButton("Replay_Button", bottomBar.transform, "Replay Scenario", 20, boldFont,
                new Color(0.08f, 0.38f, 0.28f, 0.95f), new Color(0.16f, 0.90f, 0.55f, 0.9f));
            RectTransform rRt = replayBtnObj.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.5f, 0.5f);
            rRt.anchorMax = new Vector2(0.5f, 0.5f);
            rRt.pivot = new Vector2(0.5f, 0.5f);
            rRt.anchoredPosition = new Vector2(-525f, 0f);
            rRt.sizeDelta = new Vector2(230f, 58f);
            Button replayBtn = replayBtnObj.GetComponent<Button>();

            // Previous Card Button (Center-Left)
            GameObject prevBtnObj = CreateButton("Prev_Button", bottomBar.transform, "← Previous", 21, boldFont,
                new Color(0.15f, 0.22f, 0.32f, 0.95f), new Color(0.35f, 0.55f, 0.85f, 0.9f));
            RectTransform pRt = prevBtnObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.anchoredPosition = new Vector2(-215f, 0f);
            pRt.sizeDelta = new Vector2(180f, 58f);
            Button prevBtn = prevBtnObj.GetComponent<Button>();

            // Page Indicator Text (Center)
            TextMeshProUGUI pageIndicatorTMP = CreateTMPText("Page_Indicator_TMP", bottomBar.transform,
                "Card 1 of 4", 23, TextAlignmentOptions.Center, new Color(0.85f, 0.90f, 0.98f), boldFont);
            RectTransform piRt = pageIndicatorTMP.GetComponent<RectTransform>();
            piRt.anchorMin = new Vector2(0.5f, 0.5f);
            piRt.anchorMax = new Vector2(0.5f, 0.5f);
            piRt.pivot = new Vector2(0.5f, 0.5f);
            piRt.anchoredPosition = new Vector2(0f, 0f);
            piRt.sizeDelta = new Vector2(170f, 44f);

            // Next Card Button (Center-Right)
            GameObject nextBtnObj = CreateButton("Next_Button", bottomBar.transform, "Next →", 21, boldFont,
                new Color(0.12f, 0.35f, 0.60f, 0.95f), new Color(0.25f, 0.65f, 1.0f, 0.9f));
            RectTransform nRt = nextBtnObj.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0.5f, 0.5f);
            nRt.anchorMax = new Vector2(0.5f, 0.5f);
            nRt.pivot = new Vector2(0.5f, 0.5f);
            nRt.anchoredPosition = new Vector2(215f, 0f);
            nRt.sizeDelta = new Vector2(180f, 58f);
            Button nextBtn = nextBtnObj.GetComponent<Button>();

            // Return to Menu Button (Far Right)
            GameObject menuBtnObj = CreateButton("Menu_Button", bottomBar.transform, "Return to Menu", 20, boldFont,
                new Color(0.25f, 0.15f, 0.38f, 0.95f), new Color(0.65f, 0.45f, 0.95f, 0.9f));
            RectTransform mRt = menuBtnObj.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.5f, 0.5f);
            mRt.anchorMax = new Vector2(0.5f, 0.5f);
            mRt.pivot = new Vector2(0.5f, 0.5f);
            mRt.anchoredPosition = new Vector2(525f, 0f);
            mRt.sizeDelta = new Vector2(230f, 58f);
            Button menuBtn = menuBtnObj.GetComponent<Button>();

            // -------------------------------------------------------------
            // 8. WIRE CONTROLLER
            // -------------------------------------------------------------
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("headerTitleTMP").objectReferenceValue = null;
            so.FindProperty("headerSubtitleTMP").objectReferenceValue = null;
            so.FindProperty("statusBadgeTMP").objectReferenceValue = null;

            SerializedProperty cardsProp = so.FindProperty("lessonCards");
            cardsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = lessonCards[i];
            }

            SerializedProperty tabsProp = so.FindProperty("tabPillButtons");
            tabsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                tabsProp.GetArrayElementAtIndex(i).objectReferenceValue = tabButtons[i];
            }

            so.FindProperty("prevButton").objectReferenceValue = prevBtn;
            so.FindProperty("nextButton").objectReferenceValue = nextBtn;
            so.FindProperty("replayButton").objectReferenceValue = replayBtn;
            so.FindProperty("returnMenuButton").objectReferenceValue = menuBtn;
            so.FindProperty("cardCompleteReturnButton").objectReferenceValue = inCardMenuBtn;
            so.FindProperty("pageIndicatorTMP").objectReferenceValue = pageIndicatorTMP;

            so.FindProperty("audioSource").objectReferenceValue = audioSource;
            if (clickSfx != null) so.FindProperty("buttonClickSFX").objectReferenceValue = clickSfx;
            if (popSfx != null) so.FindProperty("panelOpenedSFX").objectReferenceValue = popSfx;
            so.FindProperty("hideOnStart").boolValue = true;
            so.FindProperty("animatePopOnEnable").boolValue = true;
            so.FindProperty("popDuration").floatValue = 0.45f;

            so.ApplyModifiedProperties();

            // Hook controller listeners
            controller.HookButtons();

            // -------------------------------------------------------------
            // 9. WIRE COMMAND PROMPT BREACH SIMULATOR
            // -------------------------------------------------------------
            SerializedObject simSo = new SerializedObject(breachSim);
            simSo.FindProperty("breachDuration").floatValue = 10.0f;
            simSo.FindProperty("autoCloseAfterDuration").boolValue = true;
            simSo.FindProperty("lessonLearnedPanel").objectReferenceValue = panelRoot;
            simSo.ApplyModifiedProperties();

            // Keep panel active in Edit Mode so the user can see and inspect it immediately in the Editor.
            // Scenario1_LessonLearnedController.Start() will automatically hide it when Play Mode starts.
            panelRoot.SetActive(true);
            Selection.activeGameObject = panelRoot;

            // Frame SceneView camera to show both the PC monitor and the floating panel above it
            var sv = SceneView.lastActiveSceneView;
            if (sv != null)
            {
                sv.LookAtDirect(new Vector3(-1.58f, 1.70f, 5.25f), Quaternion.Euler(-6f, 180f, 0f), 0.75f);
                sv.Repaint();
            }

            // Mark Scene Dirty and Save
            EditorUtility.SetDirty(panelRoot);
            EditorUtility.SetDirty(cmdScreenTrans.gameObject);
            EditorUtility.SetDirty(monitorCanvas);
            EditorUtility.SetDirty(workstationSetup);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=#22C55E><b>[CADT VR]</b> Overhead Lesson Learned Panel (Above PC Screen) built successfully!</color>");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreateCard(string name, Transform parent, Color bgColor, Color outlineColor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = bgColor;
            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(3f, -3f);
            return go;
        }

        private static void CreateCardHeader(Transform cardParent, string title, float fontSize, Color titleColor, TMP_FontAsset font, Color dividerColor)
        {
            TextMeshProUGUI headerTMP = CreateTMPText("Card_Header", cardParent, title, fontSize, TextAlignmentOptions.Left, titleColor, font);
            RectTransform hRt = headerTMP.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0f, 1f);
            hRt.anchoredPosition = new Vector2(50f, -24f);
            hRt.sizeDelta = new Vector2(1100f, 50f);

            GameObject divider = CreateUIObject("Divider", cardParent);
            Image divImg = divider.AddComponent<Image>();
            divImg.color = dividerColor;
            RectTransform divRt = divider.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0f, 1f);
            divRt.anchorMax = new Vector2(1f, 1f);
            divRt.pivot = new Vector2(0.5f, 1f);
            divRt.offsetMin = new Vector2(35f, -80f);
            divRt.offsetMax = new Vector2(-35f, -78f);
        }

        private static void CreateCardBody(Transform cardParent, string name, string text, float fontSize, TMP_FontAsset font)
        {
            TextMeshProUGUI bodyTMP = CreateTMPText(name, cardParent, text, fontSize, TextAlignmentOptions.TopLeft, new Color(0.96f, 0.94f, 0.95f), font);
            RectTransform bRt = bodyTMP.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0f);
            bRt.anchorMax = new Vector2(1f, 1f);
            bRt.offsetMin = new Vector2(50f, 20f);
            bRt.offsetMax = new Vector2(-35f, -96f);
        }

        private static GameObject CreateButton(string name, Transform parent, string label, float fontSize, TMP_FontAsset font, Color bgColor, Color outlineColor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = bgColor;

            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
            cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.3f);
            cb.selectedColor = bgColor;
            cb.disabledColor = new Color(0.08f, 0.10f, 0.14f, 0.45f);
            btn.colors = cb;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(go.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.font = font;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return go;
        }

        private static TextMeshProUGUI CreateTMPText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions align, Color color, TMP_FontAsset font)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            if (font != null) tmp.font = font;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
