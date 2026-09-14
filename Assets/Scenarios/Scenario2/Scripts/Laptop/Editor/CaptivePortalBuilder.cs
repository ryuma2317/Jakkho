#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using Kannko.Laptop;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Kannko.Laptop.Editor
{
    public static class CaptivePortalBuilder
    {
        [MenuItem("Scenario2/Build Captive Portal")]
        public static void Build()
        {
            var browserPanel = GameObject.Find("LaptopBrowserPanel");
            if (browserPanel == null)
            {
                foreach (var g in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (g.name == "LaptopBrowserPanel" && !EditorUtility.IsPersistent(g))
                    {
                        browserPanel = g;
                        break;
                    }
                }
            }

            if (browserPanel == null)
            {
                Debug.LogError("[CaptivePortalBuilder] LaptopBrowserPanel not found in current scene!");
                return;
            }

            var pageContent = browserPanel.transform.Find("PageContent");
            if (pageContent == null)
            {
                Debug.LogError("[CaptivePortalBuilder] PageContent not found under LaptopBrowserPanel!");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Scenarios/Scenario2/Textures/UI_DarkCard_Bg.png");
            var pillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Scenarios/Scenario2/Textures/UI_Pill_Bg.png");

            // -------------------------------------------------------------
            // EXTERNAL WORLD SPACE CANVAS (Scenario2RevealWorldSpace)
            // -------------------------------------------------------------
            var worldUI = GameObject.Find("WorldUI");
            if (worldUI == null)
            {
                Debug.LogError("[CaptivePortalBuilder] WorldUI not found in current scene!");
                return;
            }

            Transform revealSpaceTrans = worldUI.transform.Find("Scenario2RevealWorldSpace");
            GameObject revealSpaceGo;
            if (revealSpaceTrans == null)
            {
                revealSpaceGo = new GameObject("Scenario2RevealWorldSpace",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster),
                    typeof(UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster),
                    typeof(CanvasGroup));
                revealSpaceGo.transform.SetParent(worldUI.transform, false);
            }
            else
            {
                revealSpaceGo = revealSpaceTrans.gameObject;
            }

            var revealCanvas = revealSpaceGo.GetComponent<Canvas>();
            if (revealCanvas == null) revealCanvas = revealSpaceGo.AddComponent<Canvas>();
            revealCanvas.renderMode = RenderMode.WorldSpace;
            revealCanvas.sortingOrder = 10;
            revealCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

            var revealScaler = revealSpaceGo.GetComponent<CanvasScaler>();
            if (revealScaler == null) revealScaler = revealSpaceGo.AddComponent<CanvasScaler>();
            revealScaler.dynamicPixelsPerUnit = 10f;

            if (revealSpaceGo.GetComponent<GraphicRaycaster>() == null) revealSpaceGo.AddComponent<GraphicRaycaster>();
            if (revealSpaceGo.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() == null) revealSpaceGo.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
            if (revealSpaceGo.GetComponent<CanvasGroup>() == null) revealSpaceGo.AddComponent<CanvasGroup>();

            var revealRt = revealSpaceGo.GetComponent<RectTransform>();
            revealRt.position = new Vector3(-0.14f, 1.56f, -0.69f);
            revealRt.eulerAngles = new Vector3(-14f, 270f, 0f);
            revealRt.localScale = new Vector3(0.0006f, 0.0006f, 0.0006f);
            revealRt.sizeDelta = new Vector2(960f, 840f);
            revealRt.pivot = new Vector2(0.5f, 0.5f);

            // -------------------------------------------------------------
            // REPARENT EXISTING REVEAL PANELS TO Scenario2RevealWorldSpace
            // -------------------------------------------------------------
            // 1. AttackRevealPanel under Scenario2RevealWorldSpace
            // -------------------------------------------------------------
            var origArp = pageContent.Find("AttackRevealPanel");
            if (origArp != null) origArp.gameObject.SetActive(false);

            Transform attackRevealTrans = revealSpaceGo.transform.Find("AttackRevealPanel");
            GameObject attackRevealGo;
            if (attackRevealTrans == null)
            {
                attackRevealGo = CreateSubCard("AttackRevealPanel", revealSpaceGo.transform, Vector2.zero, new Vector2(860f, 820f), cardSprite, new Color(0.12f, 0.12f, 0.14f, 0.98f));
                var art = attackRevealGo.GetComponent<RectTransform>();
                art.anchorMin = new Vector2(0.5f, 0.5f);
                art.anchorMax = new Vector2(0.5f, 0.5f);
                art.anchoredPosition = Vector2.zero;
                art.sizeDelta = new Vector2(860f, 820f);

                // Warning Header
                var wh = CreateSubCard("WarningHeader", attackRevealGo.transform, new Vector2(0, 310), new Vector2(800, 48), pillSprite, new Color(0.95f, 0.55f, 0.10f, 0.95f));
                CreateLabel("WarningTitle", wh.transform, Vector2.zero, new Vector2(760, 36), "⚠ SECURITY WARNING", 18, Color.black, TextAlignmentOptions.Center, true, font);

                // Trapped Headline
                CreateLabel("TrappedHeadline", attackRevealGo.transform, new Vector2(0, 230), new Vector2(780, 50), "<color=#FF3B30>YOU WERE TRAPPED</color>", 32, Color.white, TextAlignmentOptions.Center, true, font);

                // Explanation Body
                var ebCard = CreateSubCard("ExplanationCard", attackRevealGo.transform, new Vector2(0, 70), new Vector2(780, 220), cardSprite, new Color(0.18f, 0.18f, 0.22f, 0.95f));
                CreateLabel("ExplanationBody", ebCard.transform, Vector2.zero, new Vector2(740, 190),
                    "<b>This was NOT the legitimate Jakko-Cooffee Wi-Fi.</b>\n\n" +
                    "You connected to a <b>simulated fake access point</b>.\n" +
                    "The information you entered was submitted to the <b>simulated attacker dashboard</b>.\n\n" +
                    "<size=80%><color=#8E8E93><i>Because this is an educational simulation, the attacker dashboard is only an in-game representation. No real credentials were transmitted or stored.</i></color></size>",
                    16, new Color(0.90f, 0.90f, 0.95f, 1f), TextAlignmentOptions.Left, false, font);

                // Action Buttons
                var actRow = new GameObject("ActionButtons", typeof(RectTransform));
                actRow.transform.SetParent(attackRevealGo.transform, false);
                var revealActRt = actRow.GetComponent<RectTransform>();
                revealActRt.anchoredPosition = new Vector2(0, -180);
                revealActRt.sizeDelta = new Vector2(780, 60);

                CreateButton("SeeWhatHappenedButton", actRow.transform, new Vector2(-180, 0), new Vector2(340, 52), "See What Happened →", new Color(0.00f, 0.48f, 1.00f, 1.0f), Color.white, font, pillSprite, 19);
                CreateButton("ReturnToLaptopButton", actRow.transform, new Vector2(180, 0), new Vector2(340, 52), "Return to Laptop", new Color(0.24f, 0.24f, 0.28f, 1.0f), Color.white, font, pillSprite, 19);

                attackRevealGo.SetActive(false);
            }
            else
            {
                attackRevealGo = attackRevealTrans.gameObject;
            }

            // 2. AttackerDashboardPanel
            Transform dashboardTrans = revealSpaceGo.transform.Find("AttackerDashboardPanel");
            if (dashboardTrans == null) dashboardTrans = pageContent.Find("AttackerDashboardPanel");
            if (dashboardTrans != null)
            {
                dashboardTrans.SetParent(revealSpaceGo.transform, false);
                var drt = dashboardTrans.GetComponent<RectTransform>();
                drt.anchorMin = new Vector2(0.5f, 0.5f);
                drt.anchorMax = new Vector2(0.5f, 0.5f);
                drt.anchoredPosition = Vector2.zero;
                drt.sizeDelta = new Vector2(860f, 820f);
                dashboardTrans.gameObject.SetActive(false);
            }

            // 3. AttackSummaryPanel
            Transform summaryTrans = revealSpaceGo.transform.Find("AttackSummaryPanel");
            if (summaryTrans == null) summaryTrans = pageContent.Find("AttackSummaryPanel");
            if (summaryTrans != null)
            {
                summaryTrans.SetParent(revealSpaceGo.transform, false);
                var srt = summaryTrans.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.5f, 0.5f);
                srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.anchoredPosition = Vector2.zero;
                srt.sizeDelta = new Vector2(860f, 820f);
                summaryTrans.gameObject.SetActive(false);
            }

            // -------------------------------------------------------------
            // EDUCATIONAL REWIND PANEL (6 Scenes Progression)
            // -------------------------------------------------------------
            Transform rewindTrans = revealSpaceGo.transform.Find("EducationalRewindPanel");
            if (rewindTrans == null) rewindTrans = pageContent.Find("EducationalRewindPanel");
            GameObject rewindGo;
            if (rewindTrans == null)
            {
                rewindGo = new GameObject("EducationalRewindPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                rewindGo.transform.SetParent(revealSpaceGo.transform, false);
                var rewRt = rewindGo.GetComponent<RectTransform>();
                rewRt.anchorMin = new Vector2(0.5f, 0.5f);
                rewRt.anchorMax = new Vector2(0.5f, 0.5f);
                rewRt.anchoredPosition = Vector2.zero;
                rewRt.sizeDelta = new Vector2(860f, 820f);

                var rewImg = rewindGo.GetComponent<Image>();
                rewImg.sprite = cardSprite;
                rewImg.type = Image.Type.Sliced;
                rewImg.color = new Color(0.12f, 0.12f, 0.14f, 0.98f);
            }
            else
            {
                rewindGo = rewindTrans.gameObject;
                rewindGo.transform.SetParent(revealSpaceGo.transform, false);
                var rewRt = rewindGo.GetComponent<RectTransform>();
                rewRt.anchorMin = new Vector2(0.5f, 0.5f);
                rewRt.anchorMax = new Vector2(0.5f, 0.5f);
                rewRt.anchoredPosition = Vector2.zero;
                rewRt.sizeDelta = new Vector2(860f, 820f);
            }

            // Clean existing placeholder children under EducationalRewindPanel to construct the true 6 scenes
            // (Only children under EducationalRewindPanel are replaced; PortalCard, CaptivePortalPanel, etc. remain untouched!)
            var childrenToDestroy = new List<GameObject>();
            for (int i = 0; i < rewindGo.transform.childCount; i++)
            {
                childrenToDestroy.Add(rewindGo.transform.GetChild(i).gameObject);
            }
            foreach (var child in childrenToDestroy)
            {
                GameObject.DestroyImmediate(child);
            }

            // Add or retrieve EducationalRewindController component
            var rewindController = rewindGo.GetComponent<EducationalRewindController>();
            if (rewindController == null)
            {
                rewindController = rewindGo.AddComponent<EducationalRewindController>();
            }

            // Top Simulation Header Banner on rewindGo
            var simBannerGo = new GameObject("SimBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            simBannerGo.transform.SetParent(rewindGo.transform, false);
            var simBannerRt = simBannerGo.GetComponent<RectTransform>();
            simBannerRt.anchoredPosition = new Vector2(0, 375);
            simBannerRt.sizeDelta = new Vector2(800, 30);
            var simBannerImg = simBannerGo.GetComponent<Image>();
            simBannerImg.sprite = pillSprite;
            simBannerImg.type = Image.Type.Sliced;
            simBannerImg.color = new Color(0.95f, 0.60f, 0.05f, 0.95f);
            CreateLabel("SimBannerText", simBannerGo.transform, Vector2.zero, new Vector2(780, 26), "EDUCATIONAL SIMULATION — NO REAL CREDENTIALS USED", 15, Color.black, TextAlignmentOptions.Center, true, font);

            // =================================================================
            // SCENE 7: REWIND TO WI-FI (Network Selection Analysis)
            // =================================================================
            var scene7Go = new GameObject("Scene7_RewindToWifi", typeof(RectTransform));
            scene7Go.transform.SetParent(rewindGo.transform, false);
            var sc7Rt = scene7Go.GetComponent<RectTransform>();
            sc7Rt.anchorMin = Vector2.zero;
            sc7Rt.anchorMax = Vector2.one;
            sc7Rt.sizeDelta = Vector2.zero;

            CreateLabel("Scene7Header", scene7Go.transform, new Vector2(0, 325), new Vector2(800, 36), "REWIND: WI-FI SELECTION", 26, new Color(1.0f, 0.60f, 0.0f, 1.0f), TextAlignmentOptions.Center, true, font);
            CreateLabel("Scene7Sub", scene7Go.transform, new Vector2(0, 298), new Vector2(800, 24), "Analyzing what happened when you chose a network in the coffee shop", 16, new Color(0.70f, 0.70f, 0.75f, 1.0f), TextAlignmentOptions.Center, false, font);

            // Card 1: Network Comparison Card
            var s7NetCard = CreateSubCard("WifiListOverlayCard", scene7Go.transform, new Vector2(0, 160), new Vector2(780, 210), cardSprite, new Color(0.16f, 0.16f, 0.19f, 0.98f));
            CreateLabel("NetTitle", s7NetCard.transform, new Vector2(0, 80), new Vector2(740, 26), "AVAILABLE NETWORKS DETECTED", 18, Color.white, TextAlignmentOptions.Left, true, font);

            // Network item 1 (Legitimate)
            var net1 = CreateSubCard("LegitNetworkRow", s7NetCard.transform, new Vector2(0, 35), new Vector2(740, 48), cardSprite, new Color(0.10f, 0.20f, 0.12f, 0.95f));
            CreateLabel("N1Name", net1.transform, new Vector2(-150, 0), new Vector2(380, 30), "📶 <b>Jakko-Coffee_5G</b>  <color=#34C759>[Legitimate]</color>", 16, Color.white, TextAlignmentOptions.Left, false, font);
            CreateLabel("N1Sec", net1.transform, new Vector2(230, 0), new Vector2(240, 30), "🔒 WPA2 Protected (Staff pass)", 15, new Color(0.6f, 0.9f, 0.6f, 1f), TextAlignmentOptions.Right, false, font);

            // Network item 2 (Fake Twin)
            var net2 = CreateSubCard("FakeNetworkRow", s7NetCard.transform, new Vector2(0, -22), new Vector2(740, 48), cardSprite, new Color(0.28f, 0.10f, 0.10f, 0.95f));
            CreateLabel("N2Name", net2.transform, new Vector2(-150, 0), new Vector2(380, 30), "📶 <b>Jakko-cooffee_Free_WiFi</b>  <color=#FF3B30>[You Selected]</color>", 16, Color.white, TextAlignmentOptions.Left, false, font);
            CreateLabel("N2Sec", net2.transform, new Vector2(230, 0), new Vector2(240, 30), "⚠ Open / Unsecured", 15, new Color(1.0f, 0.4f, 0.4f, 1f), TextAlignmentOptions.Right, false, font);

            CreateLabel("N2Note", s7NetCard.transform, new Vector2(0, -75), new Vector2(740, 36), "Notice the subtle typos and suffix addition: '<b>Jakko-cooffee</b>' (double o) and '<b>_Free_WiFi</b>'.", 14, new Color(1.0f, 0.70f, 0.40f, 1.0f), TextAlignmentOptions.Center, false, font);

            // Card 2: Educational Warning
            var s7LessonCard = CreateSubCard("LessonCard", scene7Go.transform, new Vector2(0, -85), new Vector2(780, 210), cardSprite, new Color(0.16f, 0.16f, 0.19f, 0.98f));
            CreateLabel("LTitle", s7LessonCard.transform, new Vector2(0, 80), new Vector2(740, 26), "RED FLAG #1: A FAMILIAR NAME IS NOT PROOF OF LEGITIMACY", 17, new Color(1.0f, 0.35f, 0.35f, 1.0f), TextAlignmentOptions.Left, true, font);
            CreateLabel("LBody", s7LessonCard.transform, new Vector2(0, -10), new Vector2(740, 145),
                "• <b>SSID Spoofing:</b> Anyone can name their portable router or hotspot anything they want.\n" +
                "• <b>Evil Twin Attack:</b> Attackers clone legitimate shop names to deceive nearby visitors.\n" +
                "• <b>Nuanced Caution:</b> Open Wi-Fi is <i>not automatically malicious</i>, but unsecured networks offer zero identity verification or eavesdropping protection.\n" +
                "• <b>Best Practice:</b> Always verify the exact network name and password with shop staff.",
                15, new Color(0.85f, 0.85f, 0.90f, 1.0f), TextAlignmentOptions.Left, false, font);

            CreateButton("Scene7_InspectWifiBtn", scene7Go.transform, new Vector2(0, -260), new Vector2(360, 54), "Inspect Network Details →", new Color(0.00f, 0.48f, 1.00f, 1.0f), Color.white, font, pillSprite, 20);

            // =================================================================
            // SCENE 8: INSPECT WI-FI DETAILS (Network Properties Card)
            // =================================================================
            var scene8Go = new GameObject("Scene8_InspectWifiDetails", typeof(RectTransform));
            scene8Go.transform.SetParent(rewindGo.transform, false);
            var sc8Rt = scene8Go.GetComponent<RectTransform>();
            sc8Rt.anchorMin = Vector2.zero;
            sc8Rt.anchorMax = Vector2.one;
            sc8Rt.sizeDelta = Vector2.zero;

            CreateLabel("Scene8Header", scene8Go.transform, new Vector2(0, 325), new Vector2(800, 36), "INSPECT WI-FI PROPERTIES", 26, new Color(0.04f, 0.52f, 1.0f, 1.0f), TextAlignmentOptions.Center, true, font);
            CreateLabel("Scene8Sub", scene8Go.transform, new Vector2(0, 298), new Vector2(800, 24), "Examining technical connection properties reveals hidden risks", 16, new Color(0.70f, 0.70f, 0.75f, 1.0f), TextAlignmentOptions.Center, false, font);

            // Card 1: Network Technical Properties
            var s8PropCard = CreateSubCard("TechPropsCard", scene8Go.transform, new Vector2(0, 160), new Vector2(780, 210), cardSprite, new Color(0.16f, 0.16f, 0.19f, 0.98f));
            CreateLabel("PropTitle", s8PropCard.transform, new Vector2(0, 80), new Vector2(740, 26), "CONNECTION PROPERTIES (SIMULATED)", 18, Color.white, TextAlignmentOptions.Left, true, font);

            string[] propKeys = { "SSID:", "Security Type:", "Signal Strength:", "BSSID (Router MAC):", "IP Assignment:" };
            string[] propVals = { "Jakko-cooffee_Free_WiFi", "<color=#FF3B30><b>None (Open / Unencrypted)</b></color>", "98% (Very Close — Attacker device inside cafe)", "02:00:00:7E:11:AB (Locally Administered)", "DHCP (192.168.50.1)" };
            for (int i = 0; i < propKeys.Length; i++)
            {
                float rowY = 48 - (i * 27);
                CreateLabel($"Key_{i}", s8PropCard.transform, new Vector2(-200, rowY), new Vector2(300, 24), propKeys[i], 15, new Color(0.7f, 0.7f, 0.75f, 1f), TextAlignmentOptions.Left, false, font);
                CreateLabel($"Val_{i}", s8PropCard.transform, new Vector2(150, rowY), new Vector2(400, 24), propVals[i], 15, Color.white, TextAlignmentOptions.Left, false, font);
            }

            // Card 2: 4-Step Rule Card
            var s8RuleCard = CreateSubCard("FormulaCard", scene8Go.transform, new Vector2(0, -85), new Vector2(780, 210), cardSprite, new Color(0.16f, 0.16f, 0.19f, 0.98f));
            CreateLabel("RuleTitle", s8RuleCard.transform, new Vector2(0, 80), new Vector2(740, 26), "THE GOLDEN FORMULA FOR PUBLIC WI-FI", 17, new Color(1.0f, 0.85f, 0.20f, 1.0f), TextAlignmentOptions.Left, true, font);

            var formulaBanner = CreateSubCard("FormulaBanner", s8RuleCard.transform, new Vector2(0, 35), new Vector2(740, 42), cardSprite, new Color(0.25f, 0.18f, 0.05f, 1.0f));
            CreateLabel("FormulaText", formulaBanner.transform, Vector2.zero, new Vector2(720, 36), "<b>Open Wi-Fi + Unexpected Login + Credential Request = STOP AND VERIFY</b>", 16, new Color(1.0f, 0.85f, 0.20f, 1.0f), TextAlignmentOptions.Center, true, font);

            CreateLabel("RuleBody", s8RuleCard.transform, new Vector2(0, -45), new Vector2(740, 95),
                "1. <b>Open Wi-Fi:</b> Unencrypted traffic allows rogue devices to inject redirects.\n" +
                "2. <b>Unexpected Portal:</b> Browser opens without your command directing to an unfamiliar page.\n" +
                "3. <b>Social/Email Demand:</b> Legitimate venues rarely require your personal account password.\n" +
                "4. <b>Action:</b> Disconnect immediately and ask staff for official network details.",
                14, new Color(0.85f, 0.85f, 0.90f, 1.0f), TextAlignmentOptions.Left, false, font);

            CreateButton("Scene8_InspectLoginBtn", scene8Go.transform, new Vector2(0, -260), new Vector2(360, 54), "Inspect Login Page →", new Color(0.00f, 0.48f, 1.00f, 1.0f), Color.white, font, pillSprite, 20);

            // =================================================================
            // SCENE 9: REWIND TO LOGIN PAGE (Red Flag Inspection)
            // =================================================================
            var scene9Go = new GameObject("Scene9_RewindToLoginPage", typeof(RectTransform));
            scene9Go.transform.SetParent(rewindGo.transform, false);
            var sc9Rt = scene9Go.GetComponent<RectTransform>();
            sc9Rt.anchorMin = Vector2.zero;
            sc9Rt.anchorMax = Vector2.one;
            sc9Rt.sizeDelta = Vector2.zero;

            CreateLabel("Scene9Header", scene9Go.transform, new Vector2(0, 325), new Vector2(800, 36), "REWIND: LOGIN PAGE RED FLAGS", 26, new Color(1.0f, 0.25f, 0.25f, 1.0f), TextAlignmentOptions.Center, true, font);
            CreateLabel("Scene9Sub", scene9Go.transform, new Vector2(0, 298), new Vector2(800, 24), "Three glaring warning signs that proved this portal was fraudulent", 16, new Color(0.70f, 0.70f, 0.75f, 1.0f), TextAlignmentOptions.Center, false, font);

            (string title, string indicator, string desc, Color col)[] flags = new (string, string, string, Color)[]
            {
                ("FLAG #1: Account Password Requested", "Field: 'Enter your email account password'", "Legitimate captive portals ask for Terms acceptance or a guest code — NEVER your personal Google/Facebook password.", new Color(1.0f, 0.35f, 0.35f, 1.0f)),
                ("FLAG #2: Suspicious Address Bar Domain", "Domain: 'coffeeshop-free-login.example'", "Attackers use disposable lookalike domains. The real shop domain was 'jakko-coffee.wifi', not an external '.example' site.", new Color(1.0f, 0.60f, 0.0f, 1.0f)),
                ("FLAG #3: Browser Warning & Misleading HTTPS", "Warning: '⚠ Your connection is not private'", "The portal triggered an invalid certificate warning. HTTPS only encrypts the tunnel — it does NOT prove the server is trustworthy!", new Color(1.0f, 0.85f, 0.20f, 1.0f))
            };

            float[] flagY = { 180f, 60f, -60f };
            for (int i = 0; i < flags.Length; i++)
            {
                var fCard = CreateSubCard($"FlagCard_{i + 1}", scene9Go.transform, new Vector2(0, flagY[i]), new Vector2(780, 105), cardSprite, new Color(0.16f, 0.16f, 0.19f, 0.98f));
                CreateLabel("FTitle", fCard.transform, new Vector2(0, 32), new Vector2(740, 24), flags[i].title, 16, flags[i].col, TextAlignmentOptions.Left, true, font);
                CreateLabel("FInd", fCard.transform, new Vector2(0, 8), new Vector2(740, 22), flags[i].indicator, 14, Color.white, TextAlignmentOptions.Left, false, font);
                CreateLabel("FDesc", fCard.transform, new Vector2(0, -22), new Vector2(740, 38), flags[i].desc, 13, new Color(0.80f, 0.80f, 0.85f, 1.0f), TextAlignmentOptions.Left, false, font);
            }

            CreateLabel("Scene9Note", scene9Go.transform, new Vector2(0, -150), new Vector2(780, 48),
                "<b>Key Takeaway:</b> Browsers display security indicators for a reason. Ignoring invalid certificate warnings or entering passwords on unknown domains hands your credentials directly to attackers.",
                14, new Color(0.85f, 0.85f, 0.90f, 1.0f), TextAlignmentOptions.Center, false, font);

            CreateButton("Scene9_TestYourselfBtn", scene9Go.transform, new Vector2(0, -240), new Vector2(360, 54), "Take the Red-Flag Challenge →", new Color(0.00f, 0.48f, 1.00f, 1.0f), Color.white, font, pillSprite, 20);

            // =================================================================
            // SCENE 10: INTERACTIVE RED-FLAG CHALLENGE
            // =================================================================
            var scene10Go = new GameObject("Scene10_InteractiveChallenge", typeof(RectTransform));
            scene10Go.transform.SetParent(rewindGo.transform, false);
            var sc10Rt = scene10Go.GetComponent<RectTransform>();
            sc10Rt.anchorMin = Vector2.zero;
            sc10Rt.anchorMax = Vector2.one;
            sc10Rt.sizeDelta = Vector2.zero;

            CreateLabel("Scene10Header", scene10Go.transform, new Vector2(0, 325), new Vector2(800, 36), "CHALLENGE: IDENTIFY 4 RED FLAGS", 26, new Color(1.0f, 0.60f, 0.0f, 1.0f), TextAlignmentOptions.Center, true, font);
            CreateLabel("Scene10Sub", scene10Go.transform, new Vector2(0, 298), new Vector2(800, 24), "Click on the 4 suspicious elements in the simulated portal below", 16, new Color(0.70f, 0.70f, 0.75f, 1.0f), TextAlignmentOptions.Center, false, font);

            // Counter Badge
            var counterBadge = CreateSubCard("CounterBadge", scene10Go.transform, new Vector2(0, 255), new Vector2(240, 36), pillSprite, new Color(0.18f, 0.18f, 0.22f, 1.0f));
            CreateLabel("CounterText", counterBadge.transform, Vector2.zero, new Vector2(220, 30), "Discovered: <b>0 / 4</b>", 17, new Color(1.0f, 0.85f, 0.20f, 1.0f), TextAlignmentOptions.Center, true, font);

            // Simulated Portal View Container
            var portalSimCard = CreateSubCard("SimulatedPortalView", scene10Go.transform, new Vector2(0, 60), new Vector2(780, 320), cardSprite, new Color(0.16f, 0.16f, 0.20f, 0.98f));

            // 4 Clickable Targets (Hotspots)
            // Target 1: Domain / URL bar
            var tDomain = CreateButton("Target_Domain", portalSimCard.transform, new Vector2(0, 115), new Vector2(720, 44), "🌐 Address: https://coffeeshop-free-login.example [Click to inspect]", new Color(0.22f, 0.22f, 0.26f, 1.0f), Color.white, font, pillSprite, 15);
            // Target 2: Browser Warning Banner
            var tWarning = CreateButton("Target_Warning", portalSimCard.transform, new Vector2(0, 58), new Vector2(720, 44), "⚠ Warning: Security certificate could not be verified! [Click to inspect]", new Color(0.35f, 0.18f, 0.05f, 1.0f), new Color(1f, 0.85f, 0.3f, 1f), font, pillSprite, 15);
            // Target 3: Password Input Area
            var tPassword = CreateButton("Target_Password", portalSimCard.transform, new Vector2(0, 0), new Vector2(720, 44), "🔑 Email Password: [••••••••••••] (Demanded by Wi-Fi portal) [Click]", new Color(0.35f, 0.12f, 0.12f, 1.0f), new Color(1f, 0.5f, 0.5f, 1f), font, pillSprite, 15);
            // Target 4: Open Wi-Fi Network Name
            var tOpenWifi = CreateButton("Target_OpenWifi", portalSimCard.transform, new Vector2(0, -58), new Vector2(720, 44), "📶 Connected to: Jakko-cooffee_Free_WiFi (Open / No encryption) [Click]", new Color(0.20f, 0.20f, 0.28f, 1.0f), new Color(0.7f, 0.8f, 1f, 1f), font, pillSprite, 15);

            // Educational Feedback Text Box
            var fbCard = CreateSubCard("FeedbackCard", scene10Go.transform, new Vector2(0, -150), new Vector2(780, 80), cardSprite, new Color(0.12f, 0.12f, 0.15f, 0.95f));
            CreateLabel("FeedbackText", fbCard.transform, Vector2.zero, new Vector2(740, 70), "Click any of the 4 elements above to identify the security flaw.", 15, new Color(0.80f, 0.80f, 0.85f, 1.0f), TextAlignmentOptions.Center, false, font);

            // Unlockable Proceed Button (initially disabled)
            var proceedBtn = CreateButton("Scene10_ProceedBtn", scene10Go.transform, new Vector2(0, -240), new Vector2(380, 54), "Make the Safe Decision →", new Color(0.00f, 0.48f, 1.00f, 1.0f), Color.white, font, pillSprite, 20);
            proceedBtn.GetComponent<Button>().interactable = false;

            // =================================================================
            // SCENE 11: SAFE DECISION (Meaningful Choice Branching)
            // =================================================================
            var scene11Go = new GameObject("Scene11_SafeDecision", typeof(RectTransform));
            scene11Go.transform.SetParent(rewindGo.transform, false);
            var sc11Rt = scene11Go.GetComponent<RectTransform>();
            sc11Rt.anchorMin = Vector2.zero;
            sc11Rt.anchorMax = Vector2.one;
            sc11Rt.sizeDelta = Vector2.zero;

            CreateLabel("Scene11Header", scene11Go.transform, new Vector2(0, 325), new Vector2(800, 36), "DECISION TIME: WHAT WOULD YOU DO?", 26, new Color(1.0f, 0.85f, 0.20f, 1.0f), TextAlignmentOptions.Center, true, font);
            CreateLabel("Scene11Sub", scene11Go.transform, new Vector2(0, 298), new Vector2(800, 24), "You are sitting in the cafe and this portal appears. Choose your response:", 16, new Color(0.70f, 0.70f, 0.75f, 1.0f), TextAlignmentOptions.Center, false, font);

            // Choices Card
            var choicesCard = CreateSubCard("ChoicesCard", scene11Go.transform, new Vector2(0, 100), new Vector2(780, 320), cardSprite, new Color(0.16f, 0.16f, 0.19f, 0.98f));
            CreateLabel("ChoicePrompt", choicesCard.transform, new Vector2(0, 120), new Vector2(740, 40), "Select the action you would take in real life:", 18, Color.white, TextAlignmentOptions.Center, true, font);

            // Choice 1: Continue Button
            CreateButton("Scene11_ContinueBtn", choicesCard.transform, new Vector2(0, 45), new Vector2(680, 58), "A) Click 'Connect' & enter credentials to get quick internet", new Color(0.35f, 0.15f, 0.15f, 1.0f), new Color(1.0f, 0.6f, 0.6f, 1.0f), font, pillSprite, 16);

            // Choice 2: Stop & Verify Button
            CreateButton("Scene11_StopVerifyBtn", choicesCard.transform, new Vector2(0, -45), new Vector2(680, 58), "B) STOP & VERIFY: Disconnect Wi-Fi & confirm official network with staff", new Color(0.12f, 0.35f, 0.18f, 1.0f), new Color(0.6f, 1.0f, 0.6f, 1.0f), font, pillSprite, 16);

            // Sub-branch A: Attacker Warning Card (Revealed if Continue chosen)
            var warningBranchCard = CreateSubCard("AttackerWarningCard", scene11Go.transform, new Vector2(0, -100), new Vector2(780, 220), cardSprite, new Color(0.28f, 0.10f, 0.10f, 0.98f));
            CreateLabel("WarnBranchTitle", warningBranchCard.transform, new Vector2(0, 80), new Vector2(740, 26), "⚠ ATTACK SUCCEEDS — ATTACKER OBJECTIVE ACHIEVED", 17, new Color(1f, 0.35f, 0.35f, 1f), TextAlignmentOptions.Center, true, font);
            CreateLabel("WarnBranchBody", warningBranchCard.transform, new Vector2(0, 10), new Vector2(740, 95),
                "The attacker counted on urgency and convenience overriding security vigilance.\n" +
                "By entering your password, your account credentials were submitted into the attacker's harvesting log.\n" +
                "In real life, attackers immediately change passwords or access sensitive personal emails.",
                14, new Color(0.95f, 0.85f, 0.85f, 1.0f), TextAlignmentOptions.Left, false, font);
            CreateButton("Scene11_TryAgainBtn", warningBranchCard.transform, new Vector2(0, -65), new Vector2(260, 44), "Try Safe Path ↺", new Color(0.80f, 0.25f, 0.25f, 1.0f), Color.white, font, pillSprite, 16);
            warningBranchCard.SetActive(false);

            // Sub-branch B: Attack Avoided Card (Revealed if Stop & Verify chosen)
            var avoidedBranchCard = CreateSubCard("AttackAvoidedCard", scene11Go.transform, new Vector2(0, -100), new Vector2(780, 220), cardSprite, new Color(0.10f, 0.25f, 0.14f, 0.98f));
            CreateLabel("AvoidBranchTitle", avoidedBranchCard.transform, new Vector2(0, 80), new Vector2(740, 26), "🟢 ATTACK AVOIDED — YOU STAYED SAFE!", 18, new Color(0.35f, 1.0f, 0.45f, 1f), TextAlignmentOptions.Center, true, font);
            CreateLabel("AvoidBranchBody", avoidedBranchCard.transform, new Vector2(0, 15), new Vector2(740, 90),
                "✓ You recognized that personal passwords should NEVER be entered to join public Wi-Fi.\n" +
                "✓ You checked the network name and avoided the rogue access point.\n" +
                "✓ Zero credentials compromised. Attacker received no data.",
                14, new Color(0.85f, 0.95f, 0.88f, 1.0f), TextAlignmentOptions.Left, false, font);
            CreateButton("Scene11_SeeComparisonBtn", avoidedBranchCard.transform, new Vector2(0, -65), new Vector2(320, 46), "See Before & After →", new Color(0.15f, 0.55f, 0.25f, 1.0f), Color.white, font, pillSprite, 17);
            avoidedBranchCard.SetActive(false);

            // =================================================================
            // SCENE 12: FINAL COMPARISON & CLOSING
            // =================================================================
            var scene12Go = new GameObject("Scene12_FinalComparison", typeof(RectTransform));
            scene12Go.transform.SetParent(rewindGo.transform, false);
            var sc12Rt = scene12Go.GetComponent<RectTransform>();
            sc12Rt.anchorMin = Vector2.zero;
            sc12Rt.anchorMax = Vector2.one;
            sc12Rt.sizeDelta = Vector2.zero;

            CreateLabel("Scene12Header", scene12Go.transform, new Vector2(0, 325), new Vector2(800, 36), "SECURITY AWARENESS: BEFORE & AFTER", 26, new Color(0.34f, 0.78f, 0.35f, 1.0f), TextAlignmentOptions.Center, true, font);
            CreateLabel("Scene12Sub", scene12Go.transform, new Vector2(0, 298), new Vector2(800, 24), "Comparing the two paths: Careless connection vs. Security-aware verification", 16, new Color(0.70f, 0.70f, 0.75f, 1.0f), TextAlignmentOptions.Center, false, font);

            // Before Card (Left Column)
            var beforeCard = CreateSubCard("BeforeCard", scene12Go.transform, new Vector2(-205, 120), new Vector2(380, 280), cardSprite, new Color(0.24f, 0.10f, 0.10f, 0.98f));
            CreateLabel("BeforeTitle", beforeCard.transform, new Vector2(0, 115), new Vector2(350, 26), "🔴 PATH A: UNNOTICED ATTACK", 16, new Color(1.0f, 0.35f, 0.35f, 1.0f), TextAlignmentOptions.Left, true, font);
            CreateLabel("BeforeBody", beforeCard.transform, new Vector2(0, -10), new Vector2(350, 205),
                "• Joined first Wi-Fi with matching name without checking security.\n" +
                "• Ignored browser certificate warning.\n" +
                "• Provided account password on captive portal.\n" +
                "• <b>Outcome:</b> Simulated attacker captured email, password, and IP.",
                14, new Color(0.95f, 0.85f, 0.85f, 1.0f), TextAlignmentOptions.Left, false, font);

            // After Card (Right Column)
            var afterCard = CreateSubCard("AfterCard", scene12Go.transform, new Vector2(205, 120), new Vector2(380, 280), cardSprite, new Color(0.10f, 0.22f, 0.12f, 0.98f));
            CreateLabel("AfterTitle", afterCard.transform, new Vector2(0, 115), new Vector2(350, 26), "🟢 PATH B: VIGILANT VERIFICATION", 16, new Color(0.35f, 1.0f, 0.45f, 1.0f), TextAlignmentOptions.Left, true, font);
            CreateLabel("AfterBody", afterCard.transform, new Vector2(0, -10), new Vector2(350, 205),
                "• Verified exact network name & security with coffee shop staff.\n" +
                "• Refused to submit credentials on HTTP/invalid portal.\n" +
                "• Relied on cellular mobile data or VPN for sensitive tasks.\n" +
                "• <b>Outcome:</b> Attacker harvested nothing; accounts safe.",
                14, new Color(0.85f, 0.95f, 0.88f, 1.0f), TextAlignmentOptions.Left, false, font);

            // Key Takeaway Card (Bottom)
            var takeawayCard = CreateSubCard("KeyTakeawayCard", scene12Go.transform, new Vector2(0, -85), new Vector2(780, 100), cardSprite, new Color(0.16f, 0.16f, 0.20f, 0.98f));
            CreateLabel("TakeawayTitle", takeawayCard.transform, new Vector2(0, 28), new Vector2(740, 24), "CORE TAKEAWAY FOR PUBLIC WI-FI SAFETY", 16, new Color(1.0f, 0.85f, 0.20f, 1.0f), TextAlignmentOptions.Center, true, font);
            CreateLabel("TakeawayBody", takeawayCard.transform, new Vector2(0, -12), new Vector2(740, 48),
                "Never enter private account credentials to access public Wi-Fi.\nWhen in doubt, ask staff, use mobile data, or activate a trustworthy VPN.",
                15, Color.white, TextAlignmentOptions.Center, false, font);

            // Final Action Buttons
            var actionRow = new GameObject("FinalActionButtons", typeof(RectTransform));
            actionRow.transform.SetParent(scene12Go.transform, false);
            var actRt = actionRow.GetComponent<RectTransform>();
            actRt.anchoredPosition = new Vector2(0, -185);
            actRt.sizeDelta = new Vector2(780, 54);

            CreateButton("FinishReturnButton", actionRow.transform, new Vector2(-195, 0), new Vector2(360, 52), "Finish & Return to Laptop", new Color(0.00f, 0.48f, 1.00f, 1.0f), Color.white, font, pillSprite, 19);
            CreateButton("ReplaySimulationButton", actionRow.transform, new Vector2(195, 0), new Vector2(360, 52), "Replay Simulation ↺", new Color(0.24f, 0.24f, 0.28f, 1.0f), Color.white, font, pillSprite, 19);

            // Default: Hide all scenes initially (EducationalRewindController.StartRewindSequence activates Scene 7)
            scene7Go.SetActive(false);
            scene8Go.SetActive(false);
            scene9Go.SetActive(false);
            scene10Go.SetActive(false);
            scene11Go.SetActive(false);
            scene12Go.SetActive(false);
            rewindGo.SetActive(false);

            // Connect EducationalRewindController references
            rewindController.FindReferencesIfNull();

            // -------------------------------------------------------------
            // BIND & REFRESH CaptivePortalController
            // -------------------------------------------------------------
            var controller = browserPanel.GetComponent<CaptivePortalController>();
            if (controller == null)
            {
                controller = browserPanel.AddComponent<CaptivePortalController>();
            }

            controller.FindReferencesIfNull();
            controller.SetupButtonListeners();
            controller.EnsureCollidersAndInteractables();

            // Ensure external World Space Canvas is inactive initially
            revealSpaceGo.SetActive(false);

            EditorUtility.SetDirty(browserPanel);
            EditorUtility.SetDirty(revealSpaceGo);
            EditorSceneManager.MarkSceneDirty(browserPanel.scene);
            EditorSceneManager.SaveScene(browserPanel.scene);

            Debug.Log($"[CaptivePortalBuilder] Successfully constructed external World Space Reveal Canvas and all 6 Educational Rewind scenes! Scene saved: {browserPanel.scene.name}");
        }

        private static GameObject CreateSubCard(string name, Transform parent, Vector2 pos, Vector2 size, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            img.color = color;
            return go;
        }

        private static GameObject CreateLabel(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, Color color, TextAlignmentOptions align, bool bold, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.text = text;
            tmp.richText = true;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            return go;
        }

        private static GameObject CreateButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Color bgColor, Color textColor, TMP_FontAsset font, Sprite pillSprite, float fontSize = 22)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(BoxCollider), typeof(XRSimpleInteractable));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            if (pillSprite != null)
            {
                img.sprite = pillSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = bgColor;

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.94f, 0.96f, 1f);
            colors.pressedColor = new Color(0.85f, 0.88f, 0.90f, 1f);
            colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
            btn.colors = colors;

            var col = go.GetComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 50f);
            col.center = Vector3.zero;

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = text;

            return go;
        }
    }
}
#endif
