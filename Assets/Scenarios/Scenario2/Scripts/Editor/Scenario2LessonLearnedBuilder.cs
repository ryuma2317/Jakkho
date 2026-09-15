using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Kannko.Scenario2.Editor
{
    public static class Scenario2LessonLearnedBuilder
    {
        [MenuItem("Tools/Jakkho VR/Build Scenario 2 Lesson Learned Panel")]
        public static void BuildPanel()
        {
            var worldUI = GameObject.Find("WorldUI");
            if (worldUI == null)
            {
                worldUI = new GameObject("WorldUI");
            }

            // Remove previous panel if exists
            Transform existing = worldUI.transform.Find("Scenario2_Lesson_Learned_Panel");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            // Find Inter-Bold SDF and Inter-Regular SDF font assets
            TMP_FontAsset boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter/Inter-Bold SDF.asset");
            TMP_FontAsset regFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter/Inter-Regular SDF.asset");
            if (boldFont == null)
            {
                boldFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
            if (regFont == null)
            {
                regFont = boldFont;
            }

            // Root Canvas
            GameObject root = new GameObject("Scenario2_Lesson_Learned_Panel",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster),
                typeof(CanvasGroup),
                typeof(Image),
                typeof(Outline),
                typeof(BoxCollider),
                typeof(Scenario2_LessonLearnedController));

            root.transform.SetParent(worldUI.transform, false);

            // Position floating right in front of the seated player at eye level in the cafe booth
            // Player is at (0.41, 1.24, -0.71).
            root.transform.position = new Vector3(-0.05f, 1.34f, -0.70f);
            root.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
            root.transform.localScale = new Vector3(0.00085f, 0.00085f, 0.00085f);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            if (canvas.worldCamera == null) canvas.worldCamera = Camera.main;

            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(1080f, 660f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);

            // Root Background styling (Dark Obsidian with Emerald Cyber Glow)
            Image rootImg = root.GetComponent<Image>();
            rootImg.color = new Color(0.03f, 0.05f, 0.08f, 0.98f);

            Outline rootOutline = root.GetComponent<Outline>();
            rootOutline.effectColor = new Color(0.12f, 0.85f, 0.50f, 0.95f); // Emerald victory glow
            rootOutline.effectDistance = new Vector2(2.5f, -2.5f);

            BoxCollider rootCol = root.GetComponent<BoxCollider>();
            rootCol.size = new Vector3(1080f, 660f, 5f);

            Scenario2_LessonLearnedController controller = root.GetComponent<Scenario2_LessonLearnedController>();

            // -------------------------------------------------------------
            // 1. TABS CONTAINER (Top Row)
            // -------------------------------------------------------------
            GameObject tabsContainer = new GameObject("Tabs_Container", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabsContainer.transform.SetParent(root.transform, false);
            RectTransform tabsRt = tabsContainer.GetComponent<RectTransform>();
            tabsRt.anchorMin = new Vector2(0f, 1f);
            tabsRt.anchorMax = new Vector2(1f, 1f);
            tabsRt.pivot = new Vector2(0.5f, 1f);
            tabsRt.anchoredPosition = new Vector2(0f, -14f);
            tabsRt.sizeDelta = new Vector2(-28f, 44f);

            HorizontalLayoutGroup tabsHlg = tabsContainer.GetComponent<HorizontalLayoutGroup>();
            tabsHlg.spacing = 8f;
            tabsHlg.childAlignment = TextAnchor.MiddleCenter;
            tabsHlg.childControlWidth = true;
            tabsHlg.childControlHeight = true;
            tabsHlg.childForceExpandWidth = true;
            tabsHlg.childForceExpandHeight = true;

            string[] tabLabels = new string[]
            {
                "1. ASSETS PROTECTED",
                "2. ATTACK DEBRIEF",
                "3. RED FLAGS",
                "4. VICTORY (200 XP)"
            };

            Button[] tabButtons = new Button[4];

            for (int i = 0; i < 4; i++)
            {
                GameObject tabBtnObj = new GameObject($"Tab_Button_{i}",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
                tabBtnObj.transform.SetParent(tabsContainer.transform, false);

                Image tImg = tabBtnObj.GetComponent<Image>();
                tImg.color = new Color(0.10f, 0.14f, 0.20f, 0.85f);

                Outline tOutline = tabBtnObj.GetComponent<Outline>();
                tOutline.effectColor = new Color(0.25f, 0.32f, 0.42f, 0.6f);
                tOutline.effectDistance = new Vector2(1f, -1f);

                Button tBtn = tabBtnObj.GetComponent<Button>();
                tabButtons[i] = tBtn;

                GameObject tTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                tTextObj.transform.SetParent(tabBtnObj.transform, false);
                RectTransform ttRt = tTextObj.GetComponent<RectTransform>();
                ttRt.anchorMin = Vector2.zero;
                ttRt.anchorMax = Vector2.one;
                ttRt.offsetMin = Vector2.zero;
                ttRt.offsetMax = Vector2.zero;

                TextMeshProUGUI ttTMP = tTextObj.GetComponent<TextMeshProUGUI>();
                if (boldFont != null) ttTMP.font = boldFont;
                ttTMP.text = tabLabels[i];
                ttTMP.fontSize = 13.5f;
                ttTMP.enableWordWrapping = false;
                ttTMP.fontStyle = FontStyles.Bold;
                ttTMP.alignment = TextAlignmentOptions.Center;
                ttTMP.color = Color.white;
            }

            // -------------------------------------------------------------
            // 2. CARDS CONTAINER (Middle Area)
            // -------------------------------------------------------------
            GameObject cardsContainer = new GameObject("Cards_Container", typeof(RectTransform));
            cardsContainer.transform.SetParent(root.transform, false);
            RectTransform cardsRt = cardsContainer.GetComponent<RectTransform>();
            cardsRt.anchorMin = new Vector2(0f, 0f);
            cardsRt.anchorMax = new Vector2(1f, 1f);
            cardsRt.pivot = new Vector2(0.5f, 0.5f);
            cardsRt.offsetMin = new Vector2(24f, 74f);
            cardsRt.offsetMax = new Vector2(-24f, -68f);

            GameObject[] cards = new GameObject[4];

            // --- CARD 0: ASSETS PROTECTED ---
            cards[0] = CreateCard(cardsContainer.transform, "Card_0_AssetsProtected",
                "<color=#34D399><b>★ THREAT AVOIDED — CRITICAL ASSETS SAFEGUARDED ★</b></color>",
                "<line-height=150%><size=22>" +
                "<color=#10B981>●</color>  <b>Email & Password Credentials:</b>  <color=#34D399><b>NOT SURRENDERED [SAFE]</b></color>\n" +
                "<color=#10B981>●</color>  <b>Social & Corporate Accounts:</b>  <color=#34D399><b>FULLY PROTECTED</b></color>\n" +
                "<color=#10B981>●</color>  <b>Q3 Financial Report Documents:</b>  <color=#34D399><b>PRESERVED</b></color>\n" +
                "<color=#10B981>●</color>  <b>Fake Captive Portal Phishing:</b>  <color=#34D399><b>FOILED IN TIME</b></color>\n" +
                "<color=#10B981>●</color>  <b>Man-in-the-Middle Packet Sniffing:</b>  <color=#34D399><b>BLOCKED</b></color>\n" +
                "<color=#10B981>●</color>  <b>Unencrypted HTTP Packet Intercept:</b>  <color=#34D399><b>PREVENTED</b></color>\n" +
                "<color=#10B981>●</color>  <b>Rogue Access Point Beacon:</b>  <color=#34D399><b>DISCONNECTED</b></color>\n" +
                "<color=#10B981>●</color>  <b>Identity Theft & Impersonation:</b>  <color=#34D399><b>AVOIDED</b></color>\n" +
                "<color=#10B981>●</color>  <b>Keystroke / Session Hijacking:</b>  <color=#34D399><b>ZERO DATA EXFILTRATED</b></color>\n" +
                "<color=#10B981>●</color>  <b>Workstation Cybersecurity Hygiene:</b>  <color=#34D399><b>PERFECT DISCIPLINE</b></color>" +
                "</size></line-height>",
                boldFont, new Color(0.12f, 0.85f, 0.50f, 0.95f), "Assets_Body");

            // --- CARD 1: ATTACK DEBRIEF ---
            cards[1] = CreateCard(cardsContainer.transform, "Card_1_AttackDebrief",
                "<color=#38BDF8><b>[!] ATTACK DEBRIEF: THE EVIL TWIN TRAP</b></color>",
                "<line-height=170%><size=26>" +
                "<color=#10B981>●</color>  <b>Rogue Access Point Setup:</b>\n" +
                "<color=#94A3B8>A threat actor broadcasted a duplicate Wi-Fi SSID mimicking the cafe's network name.</color>\n\n" +
                "<color=#38BDF8>●</color>  <b>The Passwordless Decoy:</b>\n" +
                "<color=#94A3B8>By offering an 'open' unencrypted network, attackers lure users in a hurry to connect.</color>\n\n" +
                "<color=#FBBF24>●</color>  <b>The Deceptive Captive Portal:</b>\n" +
                "<color=#94A3B8>All traffic was hijacked to a fake login domain demanding your personal email and password.</color>\n\n" +
                "<color=#10B981>●</color>  <b>Why You Won:</b>\n" +
                "<color=#94A3B8>You recognized that public Wi-Fi never requires personal passwords and closed the browser!</color>" +
                "</size></line-height>",
                boldFont, new Color(0.20f, 0.70f, 0.98f, 0.95f), "Debrief_Body");

            // --- CARD 2: RED FLAGS IDENTIFIED ---
            cards[2] = CreateCard(cardsContainer.transform, "Card_2_RedFlags",
                "<color=#FBBF24><b>★ 4 RED FLAGS YOU SUCCESSFULLY IDENTIFIED ★</b></color>",
                "<line-height=165%><size=26>" +
                "<color=#F59E0B><b>1. Open / Unsecured Network:</b></color>\n" +
                "<color=#94A3B8>The authentic cafe Wi-Fi has a password on the wall sign; the rogue twin required no password.</color>\n\n" +
                "<color=#F59E0B><b>2. Mismatched Website Domain:</b></color>\n" +
                "<color=#94A3B8>The captive portal URL ('coffeeshop-free-login.example') did not belong to the business.</color>\n\n" +
                "<color=#F59E0B><b>3. Demanding Personal Credentials:</b></color>\n" +
                "<color=#94A3B8>Legitimate Wi-Fi portals never demand personal email passwords or social media credentials.</color>\n\n" +
                "<color=#F59E0B><b>4. SSL Stripping & Traffic Interception:</b></color>\n" +
                "<color=#94A3B8>The login was intercepted over unencrypted HTTP to harvest plain-text form data.</color>" +
                "</size></line-height>",
                boldFont, new Color(0.95f, 0.65f, 0.15f, 0.95f), "Flags_Body");

            // --- CARD 3: VICTORY SCORECARD ---
            cards[3] = CreateCard(cardsContainer.transform, "Card_3_VictoryScorecard",
                "<color=#34D399><b>★ SCENARIO 2 COMPLETE — VICTORY! ★</b></color>",
                "<line-height=150%><size=27>" +
                "<b>Red Flags Addressed:</b>  <color=#34D399><b>4 / 4 (Full Awareness)</b></color>\n" +
                "<b>Choice Made:</b>  <color=#34D399><b>Safe (Refused to Enter Credentials & Closed Browser)</b></color>\n" +
                "<b>XP Earned:</b>  <color=#FBBF24><b>200 / 200 XP (PERFECT SCORE!)</b></color>\n\n" +
                "<color=#38BDF8><b>SECURITY BADGE UNLOCKED:</b></color>  <color=#FBBF24><b>★ Rogue Wi-Fi Hunter ★</b></color>\n\n" +
                "<color=#FBBF24><b>KEY RULES TO REMEMBER:</b></color>\n" +
                "<b>1. Never enter personal passwords on public Wi-Fi portals</b>\n" +
                "<b>2. Always verify official Wi-Fi SSID and password with cafe signage</b>\n" +
                "<b>3. Use cellular hotspots or trusted VPNs for sensitive work files</b>" +
                "</size></line-height>",
                boldFont, new Color(0.15f, 0.85f, 0.50f, 0.95f), "Summary_Body");

            // -------------------------------------------------------------
            // 3. BOTTOM ACTION BAR (Navigation & Actions)
            // -------------------------------------------------------------
            GameObject bottomBar = new GameObject("Bottom_Action_Bar", typeof(RectTransform));
            bottomBar.transform.SetParent(root.transform, false);
            RectTransform bbRt = bottomBar.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0f, 0f);
            bbRt.anchorMax = new Vector2(1f, 0f);
            bbRt.pivot = new Vector2(0.5f, 0f);
            bbRt.anchoredPosition = new Vector2(0f, 14f);
            bbRt.sizeDelta = new Vector2(-48f, 50f);

            // Replay Button (Left)
            Button replayBtn = CreateNavButton(bottomBar.transform, "Replay_Button", "Replay Scenario",
                new Vector2(0f, 0.5f), new Vector2(15f, 0f), new Vector2(160f, 44f),
                new Color(0.10f, 0.16f, 0.22f, 0.95f), new Color(0.20f, 0.70f, 0.98f, 0.8f), boldFont);

            // Prev Button
            Button prevBtn = CreateNavButton(bottomBar.transform, "Prev_Button", "\u2190 Previous",
                new Vector2(0.5f, 0.5f), new Vector2(-150f, 0f), new Vector2(130f, 44f),
                new Color(0.12f, 0.16f, 0.23f, 0.95f), new Color(0.35f, 0.45f, 0.60f, 0.6f), boldFont);

            // Page Indicator Text (Center)
            GameObject pageIndObj = new GameObject("Page_Indicator_TMP", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            pageIndObj.transform.SetParent(bottomBar.transform, false);
            RectTransform piRt = pageIndObj.GetComponent<RectTransform>();
            piRt.anchorMin = new Vector2(0.5f, 0.5f);
            piRt.anchorMax = new Vector2(0.5f, 0.5f);
            piRt.pivot = new Vector2(0.5f, 0.5f);
            piRt.anchoredPosition = new Vector2(0f, 0f);
            piRt.sizeDelta = new Vector2(140f, 44f);
            TextMeshProUGUI piTMP = pageIndObj.GetComponent<TextMeshProUGUI>();
            if (boldFont != null) piTMP.font = boldFont;
            piTMP.text = "Card 1 of 4";
            piTMP.fontSize = 18;
            piTMP.fontStyle = FontStyles.Bold;
            piTMP.alignment = TextAlignmentOptions.Center;
            piTMP.color = new Color(0.85f, 0.90f, 0.95f, 1f);

            // Next Button
            Button nextBtn = CreateNavButton(bottomBar.transform, "Next_Button", "Next \u2192",
                new Vector2(0.5f, 0.5f), new Vector2(150f, 0f), new Vector2(130f, 44f),
                new Color(0.10f, 0.35f, 0.75f, 0.95f), new Color(0.35f, 0.65f, 0.98f, 0.9f), boldFont);

            // Return to Menu Button (Right)
            Button menuBtn = CreateNavButton(bottomBar.transform, "Menu_Button", "Return to Menu",
                new Vector2(1f, 0.5f), new Vector2(-15f, 0f), new Vector2(160f, 44f),
                new Color(0.15f, 0.12f, 0.25f, 0.95f), new Color(0.65f, 0.35f, 0.98f, 0.8f), boldFont);

            // -------------------------------------------------------------
            // 4. WIRE SERIALIZED PROPERTIES ON CONTROLLER
            // -------------------------------------------------------------
            SerializedObject so = new SerializedObject(controller);
            SerializedProperty lessonCardsProp = so.FindProperty("lessonCards");
            lessonCardsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                lessonCardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
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
            so.FindProperty("pageIndicatorTMP").objectReferenceValue = piTMP;

            so.ApplyModifiedProperties();

            controller.HookButtons();
            controller.ShowLesson(0);

            // Start hidden during Play mode so it only appears upon winning
            so.FindProperty("hideOnStart").boolValue = true;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(root);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);

            Debug.Log("<color=#34D399><b>[Scenario2LessonLearnedBuilder]</b> Successfully created Scenario 2 Lesson Learned Panel!</color>");
        }

        private static GameObject CreateCard(Transform parent, string name, string headerText, string bodyText, TMP_FontAsset font, Color outlineColor, string bodyName)
        {
            GameObject card = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            card.transform.SetParent(parent, false);

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = card.GetComponent<Image>();
            img.color = new Color(0.02f, 0.08f, 0.05f, 0.98f);

            Outline outline = card.GetComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Header Text
            GameObject headerObj = new GameObject("Card_Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(card.transform, false);
            RectTransform hrt = headerObj.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0f, 1f);
            hrt.anchoredPosition = new Vector2(30f, -20f);
            hrt.sizeDelta = new Vector2(-60f, 42f);

            TextMeshProUGUI headerTMP = headerObj.GetComponent<TextMeshProUGUI>();
            if (font != null) headerTMP.font = font;
            headerTMP.text = headerText;
            headerTMP.fontSize = 25;
            headerTMP.fontStyle = FontStyles.Bold;
            headerTMP.color = Color.white;

            // Divider line
            GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            divider.transform.SetParent(card.transform, false);
            RectTransform divRt = divider.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0f, 1f);
            divRt.anchorMax = new Vector2(1f, 1f);
            divRt.pivot = new Vector2(0.5f, 1f);
            divRt.anchoredPosition = new Vector2(0f, -64f);
            divRt.sizeDelta = new Vector2(-60f, 2f);
            Image divImg = divider.GetComponent<Image>();
            divImg.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.5f);

            // Body Text
            GameObject bodyObj = new GameObject(bodyName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            bodyObj.transform.SetParent(card.transform, false);
            RectTransform brt = bodyObj.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            brt.offsetMin = new Vector2(30f, 20f);
            brt.offsetMax = new Vector2(-30f, -74f);

            TextMeshProUGUI bodyTMP = bodyObj.GetComponent<TextMeshProUGUI>();
            if (font != null) bodyTMP.font = font;
            bodyTMP.text = bodyText;
            bodyTMP.fontSize = 20;
            bodyTMP.color = new Color(0.92f, 0.95f, 0.98f, 1f);

            return card;
        }

        private static Button CreateNavButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color bgColor, Color outlineColor, TMP_FontAsset font)
        {
            GameObject btnObj = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
            img.color = bgColor;

            Outline outline = btnObj.GetComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnObj.GetComponent<Button>();

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = label;
            tmp.fontSize = 17;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }
    }
}
