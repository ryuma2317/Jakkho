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
                CreateLabel("WarningTitle", wh.transform, Vector2.zero, new Vector2(760, 36), "WARNING", 18, Color.black, TextAlignmentOptions.Center, true, font);

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

                CreateButton("ReturnToLaptopButton", actRow.transform, Vector2.zero, new Vector2(340, 52), "Return to Laptop", new Color(0.24f, 0.24f, 0.28f, 1.0f), Color.white, font, pillSprite, 19);

                attackRevealGo.SetActive(false);
            }
            else
            {
                attackRevealGo = attackRevealTrans.gameObject;
            }

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
