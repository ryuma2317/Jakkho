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
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "Scenario2")
            {
                activeScene = EditorSceneManager.OpenScene("Assets/Scenarios/Scenario2/Scenes/Scenario2.unity", OpenSceneMode.Single);
            }

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
                attackRevealGo = CreateSubCard("AttackRevealPanel", revealSpaceGo.transform, new Vector2(0f, -136f), new Vector2(1700f, 820f), cardSprite, new Color(0.12f, 0.12f, 0.14f, 0.98f));
                var art = attackRevealGo.GetComponent<RectTransform>();
                art.anchorMin = new Vector2(0.5f, 0.5f);
                art.anchorMax = new Vector2(0.5f, 0.5f);
                art.anchoredPosition = new Vector2(0f, -136f);
                art.sizeDelta = new Vector2(1700f, 820f);
                art.localEulerAngles = new Vector3(16.695f, 0f, 0f);
                art.localPosition = new Vector3(0f, 0f, -256f);

                // Warning Header
                var wh = CreateSubCard("WarningHeader", attackRevealGo.transform, new Vector2(0, 310), new Vector2(1000, 120), pillSprite, new Color(1f, 0f, 0f, 1f));
                CreateLabel("WarningTitle", wh.transform, Vector2.zero, new Vector2(960, 48), "WARNING", 26, Color.white, TextAlignmentOptions.Center, true, font);

                // Trapped Headline
                CreateLabel("TrappedHeadline", attackRevealGo.transform, new Vector2(0, 213), new Vector2(780, 50), "<color=#FF3B30>YOU WERE TRAPPED</color>", 32, Color.white, TextAlignmentOptions.Center, true, font);

                // Explanation Body
                var ebCard = CreateSubCard("ExplanationCard", attackRevealGo.transform, new Vector2(0, -15), new Vector2(1100, 350), cardSprite, new Color(0.18f, 0.18f, 0.22f, 0.95f));
                CreateLabel("ExplanationBody", ebCard.transform, Vector2.zero, new Vector2(1040, 300),
                    "<b>This was NOT the legitimate Jakko-Cooffee Wi-Fi.</b>\n\n" +
                    "You connected to a <b>simulated fake access point</b>.\n" +
                    "The information you entered was submitted to the <b>simulated attacker dashboard</b>.\n\n" +
                    "<size=80%><color=#8E8E93><i>Because this is an educational simulation, the attacker dashboard is only an in-game representation. No real credentials were transmitted or stored.</i></color></size>",
                    20, new Color(0.90f, 0.90f, 0.95f, 1f), TextAlignmentOptions.Left, false, font);

                attackRevealGo.SetActive(false);
            }
            else
            {
                attackRevealGo = attackRevealTrans.gameObject;
            }

            // Ensure ActionButtons on AttackRevealPanel has both ViewAttackerInfoButton and ReturnToLaptopButton
            Transform actButtonsTrans = attackRevealGo.transform.Find("ActionButtons");
            GameObject actRow;
            if (actButtonsTrans == null)
            {
                actRow = new GameObject("ActionButtons", typeof(RectTransform));
                actRow.transform.SetParent(attackRevealGo.transform, false);
                var rt = actRow.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -288f);
                rt.sizeDelta = new Vector2(780f, 60f);
            }
            else
            {
                actRow = actButtonsTrans.gameObject;
                var rt = actRow.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, -288f);
                rt.sizeDelta = new Vector2(780f, 60f);
            }

            // ViewAttackerInfoButton
            Transform viewInfoTrans = actRow.transform.Find("ViewAttackerInfoButton");
            if (viewInfoTrans != null) Undo.DestroyObjectImmediate(viewInfoTrans.gameObject);
            CreateButton("ViewAttackerInfoButton", actRow.transform, new Vector2(-190f, 0f), new Vector2(360f, 52f), "View Attacker Telemetry", new Color(0.85f, 0.15f, 0.15f, 1.0f), Color.white, font, pillSprite, 19);

            // ReturnToLaptopButton
            Transform returnTrans = actRow.transform.Find("ReturnToLaptopButton");
            if (returnTrans != null) Undo.DestroyObjectImmediate(returnTrans.gameObject);
            CreateButton("ReturnToLaptopButton", actRow.transform, new Vector2(190f, 0f), new Vector2(340f, 52f), "Return to Laptop", new Color(0.24f, 0.24f, 0.28f, 1.0f), Color.white, font, pillSprite, 19);

            // -------------------------------------------------------------
            // 2. AttackerTelemetryPanel (Rogue Wi-Fi Attacker Telemetry)
            // -------------------------------------------------------------
            Transform attackerPanelTrans = revealSpaceGo.transform.Find("AttackerTelemetryPanel");
            if (attackerPanelTrans != null) Undo.DestroyObjectImmediate(attackerPanelTrans.gameObject);

            GameObject attackerPanelGo = CreateSubCard("AttackerTelemetryPanel", revealSpaceGo.transform, new Vector2(0f, -136f), new Vector2(1700f, 820f), cardSprite, new Color(0.08f, 0.08f, 0.10f, 0.98f));
            var apRt = attackerPanelGo.GetComponent<RectTransform>();
            apRt.anchorMin = new Vector2(0.5f, 0.5f);
            apRt.anchorMax = new Vector2(0.5f, 0.5f);
            apRt.anchoredPosition = new Vector2(0f, -136f);
            apRt.sizeDelta = new Vector2(1700f, 820f);
            apRt.localEulerAngles = new Vector3(16.695f, 0f, 0f);
            apRt.localPosition = new Vector3(0f, 0f, -256f);

            // C2 Hacker outline styling matching Scenario 1
            Outline apOutline = attackerPanelGo.AddComponent<Outline>();
            apOutline.effectColor = new Color(0.94f, 0.27f, 0.27f, 0.95f);
            apOutline.effectDistance = new Vector2(3f, -3f);

            // Terminal Warning Header
            var apHeaderCard = CreateSubCard("WarningHeader", attackerPanelGo.transform, new Vector2(0f, 310f), new Vector2(1000f, 100f), pillSprite, new Color(0.20f, 0.05f, 0.05f, 0.95f));
            CreateLabel("HeaderTitle", apHeaderCard.transform, new Vector2(0f, 16f), new Vector2(960f, 44f), "⚠️ <color=#EF4444><b>ROGUE Wi-Fi</b></color>", 28, Color.white, TextAlignmentOptions.Center, true, font);
            CreateLabel("StatusBadge", apHeaderCard.transform, new Vector2(0f, -22f), new Vector2(960f, 32f), "<color=#94A3B8>Status:</color> <color=#22C55E><b>ACTIVE</b></color>", 20, Color.white, TextAlignmentOptions.Center, true, font);

            // Telemetry Content Card
            var apContentCard = CreateSubCard("TelemetryContentCard", attackerPanelGo.transform, new Vector2(0f, 10f), new Vector2(1100f, 470f), cardSprite, new Color(0.05f, 0.07f, 0.10f, 0.95f));
            Outline contentOutline = apContentCard.AddComponent<Outline>();
            contentOutline.effectColor = new Color(0.25f, 0.30f, 0.40f, 0.70f);
            contentOutline.effectDistance = new Vector2(1.5f, -1.5f);

            string telemetryText =
                "<b><size=26>👤 <color=#38BDF8>VICTIM CONNECTED</color></size></b>\n\n" +
                "<size=21><b>Device:</b> <color=#F8FAFC>Laptop-07</color>\n" +
                "<b>Network:</b> <color=#F8FAFC>Free_Public_WiFi</color>\n" +
                "<b>IP:</b> <color=#F8FAFC>192.168.50.23</color></size>\n\n" +
                "<color=#334155>─────────────────────────────────────────────────────────────────</color>\n\n" +
                "<b><size=24><color=#F59E0B>NETWORK ACTIVITY</color></size></b>\n\n" +
                "<size=21><color=#10B981>✓ Device detected</color>\n" +
                "<color=#10B981>✓ DNS request observed</color>\n" +
                "<color=#10B981>✓ Website connection detected</color></size>\n\n" +
                "<b><size=24><color=#38BDF8>🔒 HTTPS</color></size></b>\n" +
                "<size=19><color=#94A3B8>Encrypted connection</color></size>\n\n" +
                "<size=24><color=#EF4444><b>⚠️ WAITING FOR USER...</b></color></size>";

            CreateLabel("TelemetryText", apContentCard.transform, Vector2.zero, new Vector2(1040f, 440f), telemetryText, 20, Color.white, TextAlignmentOptions.TopLeft, false, font);

            // AttackerTelemetryPanel ActionButtons
            var apActRow = new GameObject("ActionButtons", typeof(RectTransform));
            apActRow.transform.SetParent(attackerPanelGo.transform, false);
            var apActRt = apActRow.GetComponent<RectTransform>();
            apActRt.anchorMin = new Vector2(0.5f, 0.5f);
            apActRt.anchorMax = new Vector2(0.5f, 0.5f);
            apActRt.anchoredPosition = new Vector2(0f, -288f);
            apActRt.sizeDelta = new Vector2(1100f, 60f);

            CreateButton("BackFromAttackerInfoButton", apActRow.transform, new Vector2(-380f, 0f), new Vector2(320f, 52f), "← Back to Warning", new Color(0.18f, 0.20f, 0.26f, 1.0f), Color.white, font, pillSprite, 19);
            CreateButton("ViewLessonLearnedButton", apActRow.transform, new Vector2(0f, 0f), new Vector2(340f, 52f), "🛡️ Lessons Learned →", new Color(0.05f, 0.55f, 0.40f, 1.0f), Color.white, font, pillSprite, 19);
            CreateButton("ReturnToLaptopButton", apActRow.transform, new Vector2(380f, 0f), new Vector2(320f, 52f), "Return to Laptop", new Color(0.24f, 0.24f, 0.28f, 1.0f), Color.white, font, pillSprite, 19);

            attackerPanelGo.SetActive(false);

            // -------------------------------------------------------------
            // 3. LessonLearnedPanel (Public Wi-Fi & Fake AP Defense Checklist)
            // -------------------------------------------------------------
            Transform lessonPanelTrans = revealSpaceGo.transform.Find("LessonLearnedPanel");
            if (lessonPanelTrans != null) Undo.DestroyObjectImmediate(lessonPanelTrans.gameObject);

            GameObject lessonPanelGo = CreateSubCard("LessonLearnedPanel", revealSpaceGo.transform, new Vector2(0f, -136f), new Vector2(1700f, 820f), cardSprite, new Color(0.08f, 0.09f, 0.11f, 0.98f));
            var lpRt = lessonPanelGo.GetComponent<RectTransform>();
            lpRt.anchorMin = new Vector2(0.5f, 0.5f);
            lpRt.anchorMax = new Vector2(0.5f, 0.5f);
            lpRt.anchoredPosition = new Vector2(0f, -136f);
            lpRt.sizeDelta = new Vector2(1700f, 820f);
            lpRt.localEulerAngles = new Vector3(16.695f, 0f, 0f);
            lpRt.localPosition = new Vector3(0f, 0f, -256f);

            // Emerald outline styling
            Outline lpOutline = lessonPanelGo.AddComponent<Outline>();
            lpOutline.effectColor = new Color(0.06f, 0.72f, 0.50f, 0.95f);
            lpOutline.effectDistance = new Vector2(3f, -3f);

            // Header Card
            var lpHeaderCard = CreateSubCard("WarningHeader", lessonPanelGo.transform, new Vector2(0f, 310f), new Vector2(1000f, 100f), pillSprite, new Color(0.04f, 0.18f, 0.14f, 0.95f));
            CreateLabel("HeaderTitle", lpHeaderCard.transform, new Vector2(0f, 16f), new Vector2(960f, 44f), "🛡️ <color=#10B981><b>LESSONS LEARNED</b></color>", 28, Color.white, TextAlignmentOptions.Center, true, font);
            CreateLabel("StatusBadge", lpHeaderCard.transform, new Vector2(0f, -22f), new Vector2(960f, 32f), "<color=#94A3B8>Public Wi-Fi & Fake AP Defense Checklist</color>", 20, Color.white, TextAlignmentOptions.Center, true, font);

            // Content Card
            var lpContentCard = CreateSubCard("LessonContentCard", lessonPanelGo.transform, new Vector2(0f, 10f), new Vector2(1160f, 490f), cardSprite, new Color(0.05f, 0.08f, 0.09f, 0.95f));
            Outline lpContentOutline = lpContentCard.AddComponent<Outline>();
            lpContentOutline.effectColor = new Color(0.15f, 0.45f, 0.35f, 0.70f);
            lpContentOutline.effectDistance = new Vector2(1.5f, -1.5f);

            string lessonText =
                "<b><color=#34D399>☐ 1. 📶 Check the Wi-Fi</color></b>\n" +
                "   <color=#E2E8F0>Verify the network with the legitimate provider.</color>\n\n" +
                "<b><color=#34D399>☐ 2. 🌐 Check the Website</color></b>\n" +
                "   <color=#E2E8F0>Make sure you're visiting the intended website.</color>\n\n" +
                "<b><color=#34D399>☐ 3. 🔎 Check the Domain</color></b>\n" +
                "   <color=#E2E8F0>Look for misspelled or suspicious domains.</color>\n\n" +
                "<b><color=#34D399>☐ 4. 🔒 Check HTTPS</color></b>\n" +
                "   <color=#E2E8F0>HTTPS encrypts the connection,\n   but doesn't prove the website is legitimate.</color>\n\n" +
                "<b><color=#34D399>☐ 5. ⚠️ Check Browser Warnings</color></b>\n" +
                "   <color=#E2E8F0>Stop if the browser reports a security warning.</color>\n\n" +
                "<b><color=#34D399>☐ 6. 🛡 Use a Trusted VPN</color></b>\n" +
                "   <color=#E2E8F0>A VPN can add protection on untrusted networks.</color>\n\n" +
                "<b><color=#34D399>☐ 7. 🔑 Protect Your Account</color></b>\n" +
                "   <color=#E2E8F0>Use MFA or a passkey when available.</color>";

            var lbl = CreateLabel("LessonText", lpContentCard.transform, Vector2.zero, new Vector2(1080f, 450f), lessonText, 17.5f, Color.white, TextAlignmentOptions.TopLeft, false, font);
            var lblTmp = lbl.GetComponent<TextMeshProUGUI>();
            if (lblTmp != null)
            {
                lblTmp.lineSpacing = 4f;
            }

            // LessonLearnedPanel ActionButtons
            var lpActRow = new GameObject("ActionButtons", typeof(RectTransform));
            lpActRow.transform.SetParent(lessonPanelGo.transform, false);
            var lpActRt = lpActRow.GetComponent<RectTransform>();
            lpActRt.anchorMin = new Vector2(0.5f, 0.5f);
            lpActRt.anchorMax = new Vector2(0.5f, 0.5f);
            lpActRt.anchoredPosition = new Vector2(0f, -288f);
            lpActRt.sizeDelta = new Vector2(780f, 60f);

            CreateButton("BackFromLessonLearnedButton", lpActRow.transform, new Vector2(-190f, 0f), new Vector2(340f, 52f), "← Back to Telemetry", new Color(0.18f, 0.20f, 0.26f, 1.0f), Color.white, font, pillSprite, 19);
            CreateButton("ReturnToLaptopButton", lpActRow.transform, new Vector2(190f, 0f), new Vector2(340f, 52f), "Return to Laptop", new Color(0.24f, 0.24f, 0.28f, 1.0f), Color.white, font, pillSprite, 19);

            lessonPanelGo.SetActive(false);

            // -------------------------------------------------------------
            // BIND & REFRESH CaptivePortalController
            // -------------------------------------------------------------
            var controller = browserPanel.GetComponent<CaptivePortalController>();
            if (controller == null)
            {
                controller = browserPanel.AddComponent<CaptivePortalController>();
            }

            // Assign serialized references via SerializedObject to guarantee scene persistence
            SerializedObject so = new SerializedObject(controller);
            so.Update();
            so.FindProperty("m_AttackRevealPanel").objectReferenceValue = attackRevealGo;
            so.FindProperty("m_AttackerTelemetryPanel").objectReferenceValue = attackerPanelGo;
            so.FindProperty("m_LessonLearnedPanel").objectReferenceValue = lessonPanelGo;
            so.FindProperty("m_ViewAttackerInfoButton").objectReferenceValue = actRow.transform.Find("ViewAttackerInfoButton")?.GetComponent<Button>();
            so.FindProperty("m_ReturnToLaptopButton").objectReferenceValue = actRow.transform.Find("ReturnToLaptopButton")?.GetComponent<Button>();
            so.FindProperty("m_ViewLessonLearnedButton").objectReferenceValue = apActRow.transform.Find("ViewLessonLearnedButton")?.GetComponent<Button>();
            so.FindProperty("m_BackFromAttackerInfoButton").objectReferenceValue = apActRow.transform.Find("BackFromAttackerInfoButton")?.GetComponent<Button>();
            so.FindProperty("m_AttackerInfoReturnToLaptopButton").objectReferenceValue = apActRow.transform.Find("ReturnToLaptopButton")?.GetComponent<Button>();
            so.FindProperty("m_BackFromLessonLearnedButton").objectReferenceValue = lpActRow.transform.Find("BackFromLessonLearnedButton")?.GetComponent<Button>();
            so.FindProperty("m_LessonLearnedReturnToLaptopButton").objectReferenceValue = lpActRow.transform.Find("ReturnToLaptopButton")?.GetComponent<Button>();
            so.FindProperty("m_RevealWorldSpaceCanvas").objectReferenceValue = revealSpaceGo;
            so.ApplyModifiedProperties();

            controller.FindReferencesIfNull();
            controller.SetupButtonListeners();
            controller.EnsureCollidersAndInteractables();

            // Ensure external World Space Canvas is inactive initially
            revealSpaceGo.SetActive(false);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(browserPanel);
            EditorUtility.SetDirty(revealSpaceGo);
            EditorSceneManager.MarkSceneDirty(browserPanel.scene);
            EditorSceneManager.SaveScene(browserPanel.scene);

            Debug.Log($"[CaptivePortalBuilder] Successfully constructed Attacker Telemetry Panel and wired navigation in scene: {browserPanel.scene.name}");
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
