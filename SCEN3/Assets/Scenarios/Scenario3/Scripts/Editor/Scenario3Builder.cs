#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Scenario3Builder : MonoBehaviour
{
    private static TMP_FontAsset defaultFont;
    private static Sprite glassCardSprite;
    private static Sprite glowingBtnSprite;
    private static Sprite pillSprite;
    private static Sprite qrCodeSprite;
    private static Sprite scannerReticleSprite;

    private static readonly Vector3 WorkstationAnchor = new Vector3(-22.0f, 0f, 4.0f);
    private static readonly float WorkstationYaw = 270f;
    private static readonly float WorkstationPitch = 0f;
    private static readonly float WorkstationDistance = 1.45f; // Focal distance of curved screen
    private static readonly float WorkstationHeight = 1.30f; // Ergonomic center height at eye level (top edge at 1.77m, bottom at 0.83m)
    private static readonly float WorkstationCanvasScale = 0.00105f; // Scale for 1600x900 across 1.68m curved arc

    private struct CurvedSections
    {
        public GameObject root;
        public GameObject left;
        public GameObject center;
        public GameObject right;
    }

    [MenuItem("Jakkho/Build Scenario 3 Scene & Environment", false, 1)]
    public static void BuildScenario3()
    {
        defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (defaultFont == null)
        {
            defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        string scenePath = "Assets/Scenarios/Scenario3/Scenes/Scenario3.unity";
        if (File.Exists(scenePath) && EditorSceneManager.GetActiveScene().path != scenePath)
        {
            EditorSceneManager.OpenScene(scenePath);
        }

        glassCardSprite = CreateGlassCardSprite();
        glowingBtnSprite = CreateGlowingButtonSprite();
        pillSprite = CreatePillSprite();
        qrCodeSprite = CreateQRTexture();
        scannerReticleSprite = CreateScannerReticleSprite();

        // 1. Clean conflicting legacy objects
        CleanConflictingObjects();

        // Core Managers
        GameObject coreManagers = GameObject.Find("[Core_Managers]");
        if (coreManagers == null)
        {
            coreManagers = new GameObject("[Core_Managers]");
            Undo.RegisterCreatedObjectUndo(coreManagers, "Create Core Managers");
        }
        if (!coreManagers.GetComponent<GameManager>()) coreManagers.AddComponent<GameManager>();
        if (!coreManagers.GetComponent<ScoreTracker>()) coreManagers.AddComponent<ScoreTracker>();

        SetupVRAudioFeedback(coreManagers);

        // 2. Upgrade Imported 3D Modular Kit Materials to URP Lit
        UpgradeModularKitMaterialsToURP();

        // 3. Setup Lighting & Ambient Fill
        SetupRoomLighting();

        // 4. Build Environment & Curved Cyber Visor Backing
        BuildEnvironmentAndCurvedChassis();

        // 5. Restore standard VR Controllers
        RestoreControllersOnXROrigin();

        // 6. Reset Player & Camera Spawn Position
        ResetPlayerSpawnPose();

        // 7. Setup Scenario 3 Manager
        GameObject scenarioMgrObj = GameObject.Find("[Scenario3_Manager]");
        if (scenarioMgrObj == null)
        {
            scenarioMgrObj = new GameObject("[Scenario3_Manager]");
            Undo.RegisterCreatedObjectUndo(scenarioMgrObj, "Create Scenario 3 Manager");
        }
        Scenario3_FakeLinkManager manager = scenarioMgrObj.GetComponent<Scenario3_FakeLinkManager>();
        if (manager == null) manager = scenarioMgrObj.AddComponent<Scenario3_FakeLinkManager>();

        // 8. Build Unified Seamless Curved Holographic Canvas (1600x900 across curved arc)
        GameObject existingCanvas = GameObject.Find("Scenario3_Canvas");
        if (existingCanvas != null)
        {
            try
            {
                var raycasters = existingCanvas.GetComponentsInChildren<UnityEngine.EventSystems.BaseRaycaster>(true);
                foreach (var r in raycasters)
                {
                    r.enabled = false;
                }
                DestroyImmediate(existingCanvas);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Scenario3Builder] Cleaned up existing canvas: " + ex.Message);
            }
        }

        GameObject canvasObj = new GameObject("Scenario3_Canvas");
        Vector3 canvasLocalOffset = new Vector3(0f, WorkstationHeight, WorkstationDistance);
        Quaternion canvasRot = Quaternion.Euler(WorkstationPitch, WorkstationYaw, 0f);
        canvasObj.transform.position = WorkstationAnchor + canvasRot * canvasLocalOffset;
        canvasObj.transform.rotation = canvasRot;
        canvasObj.transform.localScale = Vector3.one * WorkstationCanvasScale;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1600, 900);

        canvasObj.AddComponent<GraphicRaycaster>();
        Type trackedRaycasterType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (trackedRaycasterType != null) canvasObj.AddComponent(trackedRaycasterType);

        // Holographic OS Root Window
        GameObject rootPanel = CreateUIObject("RootBackground", canvasObj.transform);
        SetRectTransform(rootPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // =========================================================================
        // Phase 1: Mission Immersion & Tactical Briefing (Curved 3-Section Surface)
        // =========================================================================
        CurvedSections immSec = CreateCurvedSectionRoots("ImmersionPanel", rootPanel.transform);
        GameObject immersionPanel = immSec.root;

        Color cyberHeader = new Color(0.03f, 0.05f, 0.10f, 0.98f);

        // --- Phase 1 Continuous Top Header (Left, Center, Right) ---
        GameObject immLeftHead = CreateUIObject("ImmLeftHead", immSec.left.transform);
        SetRectTransform(immLeftHead, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image ilh = immLeftHead.AddComponent<Image>();
        ilh.color = cyberHeader;
        TextMeshProUGUI cadtTitle = CreateTMPText("CADTTitle", immLeftHead.transform, "<b>CADT CYBER DEFENSE TERMINAL</b>", 15, TextAlignmentOptions.MidlineLeft, new Color(0.3f, 0.85f, 1f));
        SetRectTransform(cadtTitle.gameObject, new Vector2(0.06f, 0f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero);

        GameObject immCenterHead = CreateUIObject("ImmCenterHead", immSec.center.transform);
        SetRectTransform(immCenterHead, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image ich = immCenterHead.AddComponent<Image>();
        ich.color = cyberHeader;
        TextMeshProUGUI subTitle = CreateTMPText("SubTitle", immCenterHead.transform, "WORKSTATION OS v2.6 // INCIDENT DISPATCH", 14, TextAlignmentOptions.Center, new Color(0.6f, 0.7f, 0.85f));
        SetRectTransform(subTitle.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject immRightHead = CreateUIObject("ImmRightHead", immSec.right.transform);
        SetRectTransform(immRightHead, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image irh = immRightHead.AddComponent<Image>();
        irh.color = cyberHeader;
        TextMeshProUGUI statusTxt = CreateTMPText("StatusTxt", immRightHead.transform, "[SECURE] [12:42 PM] <color=#00E676>CONNECTED</color>", 14, TextAlignmentOptions.MidlineRight, Color.white);
        SetRectTransform(statusTxt.gameObject, new Vector2(0.05f, 0f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero);

        // --- Phase 1 Left Section: Objectives ---
        GameObject immLeftCard = CreateGlassCard("ImmLeftCard", immSec.left.transform, Color.white, -2f);
        SetRectTransform(immLeftCard, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.91f), Vector2.zero, Vector2.zero);

        GameObject objPill = CreatePill("ObjPill", immLeftCard.transform, new Color(0.14f, 0.48f, 0.80f, 0.85f), -2f);
        SetRectTransform(objPill, new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI objPillTxt = CreateTMPText("ObjPillTxt", objPill.transform, "<b>OPERATIONAL OBJECTIVES</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(objPillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI objBody = CreateTMPText("ObjBody", immLeftCard.transform,
            "• <b>Detect Phishing Lure:</b> Analyze incoming peer messages for urgency & suspicious links.\n\n" +
            "• <b>Verify Out-of-Band:</b> Call friend to confirm account compromise.\n\n" +
            "• <b>Audit URL & Protocol:</b> Detect unencrypted HTTP and reverse-proxy domain spoofing.\n\n" +
            "• <b>Thwart Device Hijack:</b> Refuse deceptive Telegram desktop QR tokens.\n\n" +
            "• <b>Enforce Cloud 2FA:</b> Enable Two-Step Verification to block unauthorized sessions.",
            15, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.92f, 0.98f));
        SetRectTransform(objBody.gameObject, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.85f), Vector2.zero, Vector2.zero);

        // --- Phase 1 Center Section: Hero Briefing ---
        GameObject immHeroCard = CreateGlassCard("ImmHeroCard", immSec.center.transform, Color.white, -2f);
        SetRectTransform(immHeroCard, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.91f), Vector2.zero, Vector2.zero);

        GameObject heroPill = CreatePill("HeroPill", immHeroCard.transform, new Color(0.85f, 0.45f, 0.15f, 0.85f), -2f);
        SetRectTransform(heroPill, new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI heroPillTxt = CreateTMPText("HeroPillTxt", heroPill.transform, "<b>INCIDENT #S3-8820: REVERSE-PROXY PHISHING DISPATCH</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(heroPillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI immersionText = CreateTMPText("ImmersionText", immHeroCard.transform,
            "<size=24><color=#80D8FF><b>CADT Student Workstation Dispatch</b></color></size>\n\n" +
            "You are working at your CADT cyber defense workstation. Your close classmate, <b>Sophea</b>, is about to message you on Telegram asking for urgent help with a research survey for their NextGen course project.\n\n" +
            "<color=#FFCC80><b>ADVISORY:</b></color> Attackers frequently weaponize trust by compromising university peers' accounts to distribute malicious reverse-proxy session hijackers.\n\n" +
            "Inspect all details carefully before authorizing any requests.",
            18, TextAlignmentOptions.TopLeft, Color.white);
        SetRectTransform(immersionText.gameObject, new Vector2(0.06f, 0.26f), new Vector2(0.94f, 0.85f), Vector2.zero, Vector2.zero);

        GameObject skipImmersionBtn = CreateButton("SkipImmersionBtn", immHeroCard.transform, "<b>[ START SCENARIO NOW ]</b>", new Color(0.18f, 0.65f, 0.38f), 18, -15f);
        SetRectTransform(skipImmersionBtn, new Vector2(0.15f, 0.06f), new Vector2(0.85f, 0.20f), Vector2.zero, Vector2.zero);

        // --- Phase 1 Right Section: Threat Advisory ---
        GameObject immRightCard = CreateGlassCard("ImmRightCard", immSec.right.transform, Color.white, -2f);
        SetRectTransform(immRightCard, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.91f), Vector2.zero, Vector2.zero);

        GameObject threatPill = CreatePill("ThreatPill", immRightCard.transform, new Color(0.85f, 0.25f, 0.15f, 0.85f), -2f);
        SetRectTransform(threatPill, new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI threatPillTxt = CreateTMPText("ThreatPillTxt", threatPill.transform, "<b>THREAT TELEMETRY</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(threatPillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI threatBody = CreateTMPText("ThreatBody", immRightCard.transform,
            "• <b>Threat Level:</b> ELEVATED\n" +
            "• <b>Vector:</b> Social Engineering Relay\n" +
            "• <b>Target:</b> Telegram Desktop Pairing\n\n" +
            "<b>Gold Standard Response:</b>\n" +
            "1. Refuse untrusted QR device links.\n" +
            "2. Verify friend out-of-band via call.\n" +
            "3. Enforce cloud 2FA password.",
            15, TextAlignmentOptions.TopLeft, new Color(0.90f, 0.94f, 1.0f));
        SetRectTransform(threatBody.gameObject, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.85f), Vector2.zero, Vector2.zero);

        // =========================================================================
        // Phase 2: Authentic Seamless Curved Telegram Desktop Window
        // =========================================================================
        CurvedSections teleSec = CreateCurvedSectionRoots("TelegramChatScreen", rootPanel.transform);
        GameObject telegramScreen = teleSec.root;
        telegramScreen.SetActive(false);

        // --- Telegram Unified Header Bar running across Left, Center, Right ---
        // Left Header: Telegram icon and title
        GameObject teleLeftHeader = CreateUIObject("TeleLeftHeader", teleSec.left.transform);
        SetRectTransform(teleLeftHeader, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image tlhImg = teleLeftHeader.AddComponent<Image>();
        tlhImg.color = new Color(0.14f, 0.48f, 0.78f);
        TextMeshProUGUI teleAppTitle = CreateTMPText("TeleAppTitle", teleLeftHeader.transform, "<b>Telegram Desktop</b>", 16, TextAlignmentOptions.MidlineLeft, Color.white);
        SetRectTransform(teleAppTitle.gameObject, new Vector2(0.06f, 0f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero);

        // Center Header: Continuous blue header
        GameObject teleCenterHeader = CreateUIObject("TeleCenterHeader", teleSec.center.transform);
        SetRectTransform(teleCenterHeader, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image tchImg = teleCenterHeader.AddComponent<Image>();
        tchImg.color = new Color(0.14f, 0.48f, 0.78f);

        // Right Header: Continuous blue header with window controls
        GameObject teleRightHeader = CreateUIObject("TeleRightHeader", teleSec.right.transform);
        SetRectTransform(teleRightHeader, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image trhImg = teleRightHeader.AddComponent<Image>();
        trhImg.color = new Color(0.14f, 0.48f, 0.78f);
        TextMeshProUGUI teleWinControls = CreateTMPText("TeleWinControls", teleRightHeader.transform, "-  #  X", 16, TextAlignmentOptions.MidlineRight, Color.white);
        SetRectTransform(teleWinControls.gameObject, new Vector2(0.50f, 0f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero);

        // --- Telegram Left Section (448px, +13.5 deg): Chat List Sidebar ---
        GameObject teleLeftBody = CreateUIObject("TeleLeftBody", teleSec.left.transform);
        SetRectTransform(teleLeftBody, Vector2.zero, new Vector2(1f, 0.93f), Vector2.zero, Vector2.zero);
        Image tlbImg = teleLeftBody.AddComponent<Image>();
        tlbImg.color = Color.white;

        GameObject searchPill = CreatePill("SearchBar", teleLeftBody.transform, new Color(0.92f, 0.94f, 0.96f), -2f);
        SetRectTransform(searchPill, new Vector2(0.04f, 0.91f), new Vector2(0.96f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI searchTxt = CreateTMPText("SearchTxt", searchPill.transform, "Search chats...", 14, TextAlignmentOptions.MidlineLeft, new Color(0.50f, 0.55f, 0.60f));
        SetRectTransform(searchTxt.gameObject, new Vector2(0.06f, 0f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero);

        CreateChatListItem("ChatItem_Alerts", teleLeftBody.transform,
            new Vector2(0.03f, 0.77f), new Vector2(0.97f, 0.89f),
            "C", new Color(0.20f, 0.60f, 0.85f), "CADT Security Alerts", "Password rotation notice...", "10:30", false);

        CreateChatListItem("ChatItem_Group", teleLeftBody.transform,
            new Vector2(0.03f, 0.64f), new Vector2(0.97f, 0.76f),
            "N", new Color(0.40f, 0.75f, 0.40f), "NextGen Study Group", "Meeting at 3PM in Lab 2...", "Yesterday", false);

        GameObject sopheaItem = CreateChatListItem("ChatItem_Sophea", teleLeftBody.transform,
            new Vector2(0.03f, 0.51f), new Vector2(0.97f, 0.63f),
            "S", new Color(0.85f, 0.45f, 0.20f), "Sophea (Close Friend)", "Hey! Can you please help me...", "Just now", true);
        Button sopheaBtn = sopheaItem.GetComponent<Button>();

        // --- Telegram Center Section (704px, 0 deg): Active Chat Pane ---
        GameObject teleCenterBody = CreateUIObject("TeleCenterBody", teleSec.center.transform);
        SetRectTransform(teleCenterBody, Vector2.zero, new Vector2(1f, 0.93f), Vector2.zero, Vector2.zero);
        Image tcbImg = teleCenterBody.AddComponent<Image>();
        tcbImg.color = new Color(0.90f, 0.92f, 0.95f);

        // Chat Header on Center
        GameObject centerChatHeader = CreateUIObject("CenterChatHeader", teleCenterBody.transform);
        SetRectTransform(centerChatHeader, new Vector2(0f, 0.90f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image cchImg = centerChatHeader.AddComponent<Image>();
        cchImg.color = Color.white;

        TextMeshProUGUI friendNameText = CreateTMPText("FriendNameText", centerChatHeader.transform,
            "<b>Sophea (Close Friend)</b>  --  <color=#00C853>Online</color>", 17, TextAlignmentOptions.MidlineLeft, new Color(0.12f, 0.12f, 0.12f));
        SetRectTransform(friendNameText.gameObject, new Vector2(0.04f, 0f), new Vector2(0.90f, 1f), Vector2.zero, Vector2.zero);

        GameObject activeChatPanel = CreateUIObject("ActiveChatPanel", teleCenterBody.transform);
        SetRectTransform(activeChatPanel, Vector2.zero, new Vector2(1f, 0.90f), Vector2.zero, Vector2.zero);

        GameObject emptyChatPanel = CreateUIObject("EmptyChatPanel", teleCenterBody.transform);
        SetRectTransform(emptyChatPanel, Vector2.zero, new Vector2(1f, 0.90f), Vector2.zero, Vector2.zero);
        emptyChatPanel.SetActive(false);

        // Date divider
        GameObject datePill = CreatePill("DatePill", activeChatPanel.transform, new Color(0.80f, 0.85f, 0.90f), -2f);
        SetRectTransform(datePill, new Vector2(0.40f, 0.88f), new Vector2(0.60f, 0.94f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI dateTxt = CreateTMPText("DateTxt", datePill.transform, "Today", 13, TextAlignmentOptions.Center, new Color(0.30f, 0.35f, 0.40f));
        SetRectTransform(dateTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Sophea Message Bubble
        GameObject msgBubble = CreateGlassCard("MsgBubble", activeChatPanel.transform, Color.white, -5f);
        SetRectTransform(msgBubble, new Vector2(0.04f, 0.20f), new Vector2(0.96f, 0.85f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI msgSender = CreateTMPText("Sender", msgBubble.transform, "<b>Sophea</b>  --  12:42 PM", 15, TextAlignmentOptions.TopLeft, new Color(0.14f, 0.48f, 0.78f));
        SetRectTransform(msgSender.gameObject, new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI messagePreviewText = CreateTMPText("MessagePreviewText", msgBubble.transform,
            "Hey! Can you please help me fill out a quick survey for our NextGen project?\n" +
            "We urgently need 15 more responses before midnight for our project submission! It only takes 1 minute:\n\n" +
            "Link: <color=#0288D1><u>http://nextgen-survey-portal2026.net/survey/sophea</u></color>",
            16, TextAlignmentOptions.TopLeft, new Color(0.12f, 0.12f, 0.12f));
        SetRectTransform(messagePreviewText.gameObject, new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);

        // Embedded Link Preview Card
        GameObject linkCard = CreateGlassCard("LinkCard", msgBubble.transform, new Color(0.93f, 0.96f, 1.0f), -8f);
        SetRectTransform(linkCard, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.50f), Vector2.zero, Vector2.zero);

        GameObject linkBanner = CreatePill("LinkBanner", linkCard.transform, new Color(0.14f, 0.48f, 0.78f), -2f);
        SetRectTransform(linkBanner, new Vector2(0.03f, 0.72f), new Vector2(0.97f, 0.94f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI linkBannerTxt = CreateTMPText("LinkBannerTxt", linkBanner.transform, "<b>NEXTGEN PROJECT 2026 - SURVEY PORTAL</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(linkBannerTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI linkDesc = CreateTMPText("LinkDesc", linkCard.transform,
            "Official CADT Year 3 Telecom & Networking Student Survey Portal\n" +
            "Topic: Distributed Systems & Network Architecture\n" +
            "<color=#D32F2F><b>Host:</b> http://nextgen-survey-portal2026.net/survey/sophea</color>",
            14, TextAlignmentOptions.TopLeft, new Color(0.20f, 0.25f, 0.30f));
        SetRectTransform(linkDesc.gameObject, new Vector2(0.04f, 0.22f), new Vector2(0.68f, 0.68f), Vector2.zero, Vector2.zero);

        GameObject openLinkBtn = CreateButton("OpenLinkBtn", linkCard.transform, "<b>[ OPEN SURVEY LINK ]</b>", new Color(0.12f, 0.55f, 0.90f), 15, -12f);
        SetRectTransform(openLinkBtn, new Vector2(0.70f, 0.22f), new Vector2(0.97f, 0.65f), Vector2.zero, Vector2.zero);

        // Message input bar
        GameObject inputPill = CreatePill("InputPill", activeChatPanel.transform, Color.white, -3f);
        SetRectTransform(inputPill, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.12f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI inputTxt = CreateTMPText("InputPlaceholder", inputPill.transform, "Write a message...", 15, TextAlignmentOptions.MidlineLeft, new Color(0.55f, 0.60f, 0.65f));
        SetRectTransform(inputTxt.gameObject, new Vector2(0.04f, 0f), new Vector2(0.90f, 1f), Vector2.zero, Vector2.zero);

        // --- Telegram Right Section (448px, -13.5 deg): Contact Details & Call Verify ---
        GameObject teleRightBody = CreateUIObject("TeleRightBody", teleSec.right.transform);
        SetRectTransform(teleRightBody, Vector2.zero, new Vector2(1f, 0.93f), Vector2.zero, Vector2.zero);
        Image trbImg = teleRightBody.AddComponent<Image>();
        trbImg.color = new Color(0.94f, 0.96f, 0.98f);

        GameObject contactCard = CreateGlassCard("ContactCard", teleRightBody.transform, Color.white, -4f);
        SetRectTransform(contactCard, new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero);

        GameObject profileAv = CreatePill("ProfAvatar", contactCard.transform, new Color(0.85f, 0.45f, 0.20f), -2f);
        SetRectTransform(profileAv, new Vector2(0.35f, 0.68f), new Vector2(0.65f, 0.95f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI profAvTxt = CreateTMPText("ProfChar", profileAv.transform, "<b>S</b>", 28, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(profAvTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI profName = CreateTMPText("ProfName", contactCard.transform, "<b>Sophea</b>\n<size=14><color=#546E7A>@sophea_cadt</color></size>", 18, TextAlignmentOptions.Center, new Color(0.12f, 0.12f, 0.12f));
        SetRectTransform(profName.gameObject, new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.66f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI profBio = CreateTMPText("ProfBio", contactCard.transform, "CADT Year 3 Telecom & Networking\nNextGen DMIL Team Member", 14, TextAlignmentOptions.Center, new Color(0.45f, 0.50f, 0.55f));
        SetRectTransform(profBio.gameObject, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.38f), Vector2.zero, Vector2.zero);

        // Call verification card
        GameObject callCard = CreateGlassCard("CallCard", teleRightBody.transform, Color.white, -4f);
        SetRectTransform(callCard, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.32f), Vector2.zero, Vector2.zero);

        GameObject callFriendBtn = CreateButton("CallFriendBtn", callCard.transform, "<b>[ CALL SOPHEA (VERIFY) ]</b>", new Color(0.18f, 0.65f, 0.38f), 15, -6f);
        SetRectTransform(callFriendBtn, new Vector2(0.06f, 0.20f), new Vector2(0.94f, 0.80f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI callFriendBtnTxt = callFriendBtn.GetComponentInChildren<TextMeshProUGUI>();

        AudioWaveformVisualizer waveVisualizer = teleRightBody.AddComponent<AudioWaveformVisualizer>();

        // =========================================================================
        // Phase 3: Authentic Seamless Curved Chrome Browser & Tactical Console
        // =========================================================================
        CurvedSections webSec = CreateCurvedSectionRoots("FakeWebpageScreen", rootPanel.transform);
        GameObject fakeWebpageScreen = webSec.root;
        fakeWebpageScreen.SetActive(false);

        // --- Chrome Dark Tab Bar across Left, Center, Right ---
        GameObject chromeTabLeft = CreateUIObject("ChromeTabLeft", webSec.left.transform);
        SetRectTransform(chromeTabLeft, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image ctlImg = chromeTabLeft.AddComponent<Image>();
        ctlImg.color = new Color(0.10f, 0.11f, 0.13f);

        GameObject activeTabObj = CreateGlassCard("ActiveTab", chromeTabLeft.transform, new Color(0.18f, 0.20f, 0.24f), -2f);
        SetRectTransform(activeTabObj, new Vector2(0.04f, 0.05f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI tabTitle = CreateTMPText("TabTitle", activeTabObj.transform, "<b>NextGen Survey 2026  X</b>", 14, TextAlignmentOptions.MidlineLeft, Color.white);
        SetRectTransform(tabTitle.gameObject, new Vector2(0.08f, 0f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero);

        GameObject chromeTabCenter = CreateUIObject("ChromeTabCenter", webSec.center.transform);
        SetRectTransform(chromeTabCenter, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image ctcImg = chromeTabCenter.AddComponent<Image>();
        ctcImg.color = new Color(0.10f, 0.11f, 0.13f);

        GameObject chromeTabRight = CreateUIObject("ChromeTabRight", webSec.right.transform);
        SetRectTransform(chromeTabRight, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image ctrImg = chromeTabRight.AddComponent<Image>();
        ctrImg.color = new Color(0.10f, 0.11f, 0.13f);
        TextMeshProUGUI chromeWinControls = CreateTMPText("ChromeWinControls", chromeTabRight.transform, "-  #  X", 16, TextAlignmentOptions.MidlineRight, Color.white);
        SetRectTransform(chromeWinControls.gameObject, new Vector2(0.50f, 0f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero);

        // --- Chrome Omnibox Bar across Left and Center ---
        GameObject omniboxLeft = CreateUIObject("OmniboxLeft", webSec.left.transform);
        SetRectTransform(omniboxLeft, new Vector2(0f, 0.86f), new Vector2(1f, 0.93f), Vector2.zero, Vector2.zero);
        Image oblImg = omniboxLeft.AddComponent<Image>();
        oblImg.color = new Color(0.16f, 0.17f, 0.20f);

        GameObject httpWarningBadge = CreatePill("HttpWarningBadge", omniboxLeft.transform, new Color(0.85f, 0.25f, 0.15f), -2f);
        SetRectTransform(httpWarningBadge, new Vector2(0.04f, 0.15f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI httpWarnTxt = CreateTMPText("WarnTxt", httpWarningBadge.transform, "<b>[ ! Not secure ]</b>", 13, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(httpWarnTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject omniboxCenter = CreateUIObject("OmniboxCenter", webSec.center.transform);
        SetRectTransform(omniboxCenter, new Vector2(0f, 0.86f), new Vector2(1f, 0.93f), Vector2.zero, Vector2.zero);
        Image obcImg = omniboxCenter.AddComponent<Image>();
        obcImg.color = new Color(0.16f, 0.17f, 0.20f);

        TextMeshProUGUI urlAddressBarText = CreateTMPText("UrlAddressBarText", omniboxCenter.transform,
            "<b>http://nextgen-survey-portal2026.net/survey/sophea</b>", 15, TextAlignmentOptions.MidlineLeft, new Color(0.90f, 0.95f, 1.0f));
        SetRectTransform(urlAddressBarText.gameObject, new Vector2(0.04f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);

        GameObject omniboxRight = CreateUIObject("OmniboxRight", webSec.right.transform);
        SetRectTransform(omniboxRight, new Vector2(0f, 0.86f), new Vector2(1f, 0.93f), Vector2.zero, Vector2.zero);
        Image obrImg = omniboxRight.AddComponent<Image>();
        obrImg.color = new Color(0.16f, 0.17f, 0.20f);

        // --- Web Body Left Section (448px, +13.5 deg): Webpage Info & Red Flag Triggers ---
        GameObject webLeftBody = CreateGlassCard("WebLeftBody", webSec.left.transform, new Color(0.08f, 0.10f, 0.14f, 0.98f), -5f);
        SetRectTransform(webLeftBody, Vector2.zero, new Vector2(1f, 0.86f), Vector2.zero, Vector2.zero);

        GameObject surveyBanner = CreatePill("SurveyBanner", webLeftBody.transform, new Color(0.12f, 0.35f, 0.65f), -4f);
        SetRectTransform(surveyBanner, new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI bannerTitle = CreateTMPText("BannerTitle", surveyBanner.transform, "<b>NEXTGEN SURVEY PORTAL</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(bannerTitle.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject officialWarningBanner = CreateGlassCard("OfficialWarningBanner", webLeftBody.transform, new Color(0.45f, 0.15f, 0.10f, 0.95f), -6f);
        SetRectTransform(officialWarningBanner, new Vector2(0.04f, 0.66f), new Vector2(0.96f, 0.85f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI warnBannerTxt = CreateTMPText("WarnBannerTxt", officialWarningBanner.transform,
            "<b>SECURITY NOTICE:</b> Do NOT scan unfamiliar QR codes or approve desktop pairing from external sites!",
            13, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.85f));
        SetRectTransform(warnBannerTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject inspectUrlBtn = CreateButton("InspectUrlBtn", webLeftBody.transform, "<b>[FLAG 1] Check Domain</b>", new Color(0.18f, 0.28f, 0.42f), 14, -10f);
        SetRectTransform(inspectUrlBtn, new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.50f), Vector2.zero, Vector2.zero);

        GameObject inspectHttpBtn = CreateButton("InspectHttpBtn", webLeftBody.transform, "<b>[FLAG 2] Check HTTP</b>", new Color(0.18f, 0.28f, 0.42f), 14, -10f);
        SetRectTransform(inspectHttpBtn, new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.30f), Vector2.zero, Vector2.zero);

        // --- Web Body Center Section (704px, 0 deg): QR Code Pairing Panel ---
        GameObject webCenterBody = CreateGlassCard("WebCenterBody", webSec.center.transform, new Color(0.08f, 0.10f, 0.14f, 0.98f), -5f);
        SetRectTransform(webCenterBody, Vector2.zero, new Vector2(1f, 0.86f), Vector2.zero, Vector2.zero);

        GameObject qrPromptPanel = CreateGlassCard("QRPromptPanel", webCenterBody.transform, new Color(0.12f, 0.15f, 0.22f, 0.95f), -8f);
        SetRectTransform(qrPromptPanel, new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);

        GameObject qrImageObj = CreateUIObject("QRImage", qrPromptPanel.transform);
        SetRectTransform(qrImageObj, new Vector2(0.06f, 0.08f), new Vector2(0.44f, 0.92f), Vector2.zero, Vector2.zero);
        Image qrImg = qrImageObj.AddComponent<Image>();
        qrImg.sprite = qrCodeSprite;
        qrImg.color = Color.white;

        TextMeshProUGUI qrInstruction = CreateTMPText("QRInstruction", qrPromptPanel.transform,
            "<size=17><color=#80D8FF><b>Scan with Telegram Mobile to Submit</b></color></size>\n\n" +
            "1. Open <b>Telegram</b> on your phone.\n" +
            "2. Go to <b>Settings > Devices > Link Desktop Device</b>.\n" +
            "3. Point camera at this QR code to verify student credentials.\n\n" +
            "<color=#FFAB91><i>WARNING: Scanning links your account to attacker's server!</i></color>",
            14, TextAlignmentOptions.TopLeft, Color.white);
        SetRectTransform(qrInstruction.gameObject, new Vector2(0.48f, 0.06f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);

        GameObject inspectBannerBtn = CreateButton("InspectBannerBtn", webCenterBody.transform, "<b>[FLAG 3] Note Warning</b>", new Color(0.18f, 0.28f, 0.42f), 14, -10f);
        SetRectTransform(inspectBannerBtn, new Vector2(0.06f, 0.05f), new Vector2(0.48f, 0.17f), Vector2.zero, Vector2.zero);

        GameObject inspectDeviceLinkBtn = CreateButton("InspectDeviceLinkBtn", webCenterBody.transform, "<b>[FLAG 4] Flag Device Link</b>", new Color(0.18f, 0.28f, 0.42f), 14, -10f);
        SetRectTransform(inspectDeviceLinkBtn, new Vector2(0.52f, 0.05f), new Vector2(0.94f, 0.17f), Vector2.zero, Vector2.zero);

        // --- Web Body Right Section (448px, -13.5 deg): Tactical Defense Console ---
        GameObject webRightBody = CreateGlassCard("WebRightBody", webSec.right.transform, new Color(0.07f, 0.10f, 0.16f, 0.98f), -5f);
        SetRectTransform(webRightBody, Vector2.zero, new Vector2(1f, 0.86f), Vector2.zero, Vector2.zero);

        GameObject tacticalPill = CreatePill("TacticalPill", webRightBody.transform, new Color(0.14f, 0.50f, 0.85f, 0.85f), -4f);
        SetRectTransform(tacticalPill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI tacticalPillTxt = CreateTMPText("TacticalPillTxt", tacticalPill.transform, "<b>TACTICAL CONSOLE</b>", 14, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(tacticalPillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject enable2FABtn = CreateButton("Enable2FABtn", webRightBody.transform, "<b>[SHIELD] Enable 2FA Cloud Key</b>", new Color(0.12f, 0.45f, 0.75f), 14, -15f);
        SetRectTransform(enable2FABtn, new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        GameObject notifCard = CreateGlassCard("TelemetryCard", webRightBody.transform, new Color(0.05f, 0.08f, 0.14f, 0.95f), -6f);
        SetRectTransform(notifCard, new Vector2(0.06f, 0.35f), new Vector2(0.94f, 0.71f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI redFlagNotificationText = CreateTMPText("RedFlagNotificationText", notifCard.transform,
            "<color=#80D8FF><b>SYSTEM TELEMETRY:</b></color>\n" +
            "• Red Flags Found: 0/4\n" +
            "• 2FA Cloud Password: Off\n" +
            "• QR Pairing State: Pending\n\n" +
            "<color=#FFE082>Examine URL and red flags before deciding.</color>",
            13, TextAlignmentOptions.TopLeft, Color.white);
        SetRectTransform(redFlagNotificationText.gameObject, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);

        GameObject scanQRBtn = CreateButton("ScanQRBtn", webRightBody.transform, "<b>[ SCAN QR (AUTHORIZE) ]</b>", new Color(0.75f, 0.22f, 0.18f), 14, -20f);
        SetRectTransform(scanQRBtn, new Vector2(0.06f, 0.19f), new Vector2(0.94f, 0.31f), Vector2.zero, Vector2.zero);

        GameObject refuseQRBtn = CreateButton("RefuseQRBtn", webRightBody.transform, "<b>[ REFUSE QR & SECURE ]</b>", new Color(0.18f, 0.65f, 0.38f), 14, -20f);
        SetRectTransform(refuseQRBtn, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.17f), Vector2.zero, Vector2.zero);

        // =========================================================================
        // Phase 4: Mission Consequence Screen (Curved)
        // =========================================================================
        CurvedSections conSec = CreateCurvedSectionRoots("ConsequencePanel", rootPanel.transform);
        GameObject consequencePanel = conSec.root;
        consequencePanel.SetActive(false);

        // Left Section: Attack Methodology
        GameObject conLeftCard = CreateGlassCard("ConLeftCard", conSec.left.transform, new Color(0.08f, 0.12f, 0.20f, 0.96f), -5f);
        SetRectTransform(conLeftCard, new Vector2(0.02f, 0.02f), new Vector2(1.0f, 0.98f), Vector2.zero, Vector2.zero);

        GameObject methodPill = CreatePill("MethodPill", conLeftCard.transform, new Color(0.85f, 0.40f, 0.15f, 0.85f), -4f);
        SetRectTransform(methodPill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI methodPillTxt = CreateTMPText("MethodPillTxt", methodPill.transform, "<b>ATTACK METHODOLOGY</b>", 16, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(methodPillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI methodBody = CreateTMPText("MethodBody", conLeftCard.transform,
            "• <b>Reverse-Proxy Relay:</b> The survey portal acts as a live middleman between you and Telegram servers.\n\n" +
            "• <b>QR Session Hijack:</b> The QR code was an authorization token for attacker's desktop client.\n\n" +
            "• <b>Account Cloning:</b> Scanning it authorizes the attacker to download full chat history and impersonate you.",
            15, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.92f, 0.98f));
        SetRectTransform(methodBody.gameObject, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        // Center Section: Hero Outcome
        GameObject conHeroCard = CreateGlassCard("ConHeroCard", conSec.center.transform, new Color(0.08f, 0.13f, 0.24f, 0.97f), -5f);
        SetRectTransform(conHeroCard, new Vector2(0.0f, 0.02f), new Vector2(1.0f, 0.98f), Vector2.zero, Vector2.zero);

        GameObject outcomePill = CreatePill("OutcomePill", conHeroCard.transform, new Color(0.14f, 0.48f, 0.80f, 0.85f), -4f);
        SetRectTransform(outcomePill, new Vector2(0.15f, 0.88f), new Vector2(0.85f, 0.96f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI outcomePillTxt = CreateTMPText("OutcomePillTxt", outcomePill.transform, "<b>MISSION OUTCOME</b>", 16, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(outcomePillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI conTitle = CreateTMPText("ConsequenceTitleText", conHeroCard.transform, "<b>[DEFENSE SUCCESSFUL]</b>", 32, TextAlignmentOptions.Center, new Color(0.2f, 0.9f, 0.4f));
        SetRectTransform(conTitle.gameObject, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI conBody = CreateTMPText("ConsequenceBodyText", conHeroCard.transform,
            "You successfully recognized the spoofed domain, unencrypted protocol, and deceptive QR pairing attempt!\n\n" +
            "By refusing to scan the untrusted QR code and enabling Two-Step Verification, you completely thwarted the reverse-proxy attack.",
            18, TextAlignmentOptions.TopLeft, Color.white);
        SetRectTransform(conBody.gameObject, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.68f), Vector2.zero, Vector2.zero);

        // Right Section: Recovery Protocol
        GameObject conRightCard = CreateGlassCard("ConRightCard", conSec.right.transform, new Color(0.08f, 0.12f, 0.20f, 0.96f), -5f);
        SetRectTransform(conRightCard, new Vector2(0.0f, 0.02f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);

        GameObject recovPill = CreatePill("RecovPill", conRightCard.transform, new Color(0.14f, 0.50f, 0.85f, 0.85f), -4f);
        SetRectTransform(recovPill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI recovPillTxt = CreateTMPText("RecovPillTxt", recovPill.transform, "<b>RECOVERY PROTOCOL</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(recovPillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI recovBody = CreateTMPText("RecovBody", conRightCard.transform,
            "• <b>Terminate Sessions:</b>\nSettings > Devices > Terminate All Other Sessions.\n\n" +
            "• <b>Enable Cloud 2FA:</b>\nSettings > Privacy > Two-Step Verification.\n\n" +
            "• <b>Alert Contacts:</b>\nWarn peers out-of-band so they do not click forwarded links.",
            15, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.92f, 0.98f));
        SetRectTransform(recovBody.gameObject, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        // =========================================================================
        // Phase 5: Educational Debrief & Scorecard (Curved)
        // =========================================================================
        CurvedSections debSec = CreateCurvedSectionRoots("DebriefPanel", rootPanel.transform);
        GameObject debriefPanel = debSec.root;
        debriefPanel.SetActive(false);

        // Left Section: Scorecard & Controls
        GameObject debLeftCard = CreateGlassCard("DebLeftCard", debSec.left.transform, new Color(0.08f, 0.12f, 0.20f, 0.96f), -5f);
        SetRectTransform(debLeftCard, new Vector2(0.02f, 0.02f), new Vector2(1.0f, 0.98f), Vector2.zero, Vector2.zero);

        GameObject scorePill = CreatePill("ScorePill", debLeftCard.transform, new Color(0.14f, 0.48f, 0.80f, 0.85f), -4f);
        SetRectTransform(scorePill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI scorePillTxt = CreateTMPText("ScorePillTxt", scorePill.transform, "<b>MISSION SCORECARD</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(scorePillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI xpEarnedText = CreateTMPText("EarnedXPText", debLeftCard.transform, "<b><color=#00E676>+200 XP</color></b>", 38, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(xpEarnedText.gameObject, new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.88f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI redFlagsText = CreateTMPText("RedFlagsFoundText", debLeftCard.transform, "Red Flags Found: 4/4\nStatus: DEFENDED", 16, TextAlignmentOptions.Center, new Color(0.85f, 0.92f, 1.0f));
        SetRectTransform(redFlagsText.gameObject, new Vector2(0.06f, 0.58f), new Vector2(0.94f, 0.72f), Vector2.zero, Vector2.zero);

        GameObject nextCardBtn = CreateButton("NextCardBtn", debLeftCard.transform, "<b>[ NEXT LESSON CARD ]</b>", new Color(0.14f, 0.48f, 0.78f), 15, -10f);
        SetRectTransform(nextCardBtn, new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.52f), Vector2.zero, Vector2.zero);

        GameObject restartBtn = CreateButton("RestartBtn", debLeftCard.transform, "<b>[ REPLAY SCENARIO ]</b>", new Color(0.20f, 0.60f, 0.35f), 15, -10f);
        SetRectTransform(restartBtn, new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.37f), Vector2.zero, Vector2.zero);

        GameObject menuBtn = CreateButton("MainMenuBtn", debLeftCard.transform, "<b>[ MAIN MENU ]</b>", new Color(0.18f, 0.28f, 0.42f), 15, -10f);
        SetRectTransform(menuBtn, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.22f), Vector2.zero, Vector2.zero);

        // Center Section: Lesson Cards Carousel
        GameObject debCenterCard = CreateGlassCard("DebCenterCard", debSec.center.transform, new Color(0.08f, 0.13f, 0.24f, 0.97f), -5f);
        SetRectTransform(debCenterCard, new Vector2(0.0f, 0.02f), new Vector2(1.0f, 0.98f), Vector2.zero, Vector2.zero);

        // Card 1: Weaponized Trust
        GameObject card1 = CreateUIObject("Card1_WeaponizedTrust", debCenterCard.transform);
        SetRectTransform(card1, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject c1Pill = CreatePill("C1Pill", card1.transform, new Color(0.85f, 0.45f, 0.15f, 0.85f), -4f);
        SetRectTransform(c1Pill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI c1PillTxt = CreateTMPText("C1PillTxt", c1Pill.transform, "<b>LESSON 1: WEAPONIZED TRUST</b>", 16, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(c1PillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI c1Body = CreateTMPText("C1Body", card1.transform,
            "<size=22><color=#80D8FF><b>Why Attackers Target Your Friends' Accounts</b></color></size>\n\n" +
            "Attackers rarely send phishing links from unfamiliar strangers when targeting students or corporate teams. Instead, they first compromise one member of the group, then immediately abuse existing relationship trust to distribute lures.\n\n" +
            "• <b>Key Defense:</b> If a friend asks for urgent votes, survey fills, or verification codes, <b>always verify out-of-band</b> (e.g. voice call or face-to-face) before taking action.",
            17, TextAlignmentOptions.TopLeft, Color.white);
        SetRectTransform(c1Body.gameObject, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        // Card 2: Link Device Purpose
        GameObject card2 = CreateUIObject("Card2_LinkDevicePurpose", debCenterCard.transform);
        SetRectTransform(card2, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        card2.SetActive(false);

        GameObject c2Pill = CreatePill("C2Pill", card2.transform, new Color(0.14f, 0.48f, 0.80f, 0.85f), -4f);
        SetRectTransform(c2Pill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI c2PillTxt = CreateTMPText("C2PillTxt", c2Pill.transform, "<b>LESSON 2: LINK DEVICE DECEPTION</b>", 16, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(c2PillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI c2Body = CreateTMPText("C2Body", card2.transform,
            "<size=22><color=#80D8FF><b>How QR Login Tokens Work</b></color></size>\n\n" +
            "The 'Link Desktop Device' feature in Telegram, WhatsApp, and Signal is exclusively designed to authorize full desktop access to your messaging account.\n\n" +
            "• <b>The Scam:</b> Phishing portals disguise session-pairing QR codes as 'contest vote verifications' or 'identity checkmarks'.\n\n" +
            "• <b>Crucial Rule:</b> <b>Never</b> use 'Link Device' on a web portal or third-party screen!",
            17, TextAlignmentOptions.TopLeft, Color.white);
        SetRectTransform(c2Body.gameObject, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        // Card 3: Two-Step Verification
        GameObject card3 = CreateUIObject("Card3_TwoStepVerification", debCenterCard.transform);
        SetRectTransform(card3, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        card3.SetActive(false);

        GameObject c3Pill = CreatePill("C3Pill", card3.transform, new Color(0.18f, 0.65f, 0.38f, 0.85f), -4f);
        SetRectTransform(c3Pill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI c3PillTxt = CreateTMPText("C3PillTxt", c3Pill.transform, "<b>LESSON 3: TWO-STEP VERIFICATION (2FA)</b>", 16, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(c3PillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI c3Body = CreateTMPText("C3Body", card3.transform,
            "<size=22><color=#80D8FF><b>The Ultimate Defense Against QR Hijacking</b></color></size>\n\n" +
            "Even if an attacker tricks you into scanning a malicious QR code, enabling <b>Two-Step Verification (Cloud Password)</b> stops them in their tracks.\n\n" +
            "• <b>How it protects:</b> The reverse proxy cannot complete device pairing without your secret secondary cloud password.\n\n" +
            "• <b>Action:</b> Telegram > Settings > Privacy & Security > Two-Step Verification.",
            17, TextAlignmentOptions.TopLeft, Color.white);
        SetRectTransform(c3Body.gameObject, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        // Right Section: Summary Key Takeaway
        GameObject debRightCard = CreateGlassCard("DebRightCard", debSec.right.transform, new Color(0.08f, 0.12f, 0.20f, 0.96f), -5f);
        SetRectTransform(debRightCard, new Vector2(0.0f, 0.02f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);

        GameObject takePill = CreatePill("TakePill", debRightCard.transform, new Color(0.14f, 0.50f, 0.85f, 0.85f), -4f);
        SetRectTransform(takePill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI takePillTxt = CreateTMPText("TakePillTxt", takePill.transform, "<b>KEY TAKEAWAY</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(takePillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI takeBody = CreateTMPText("TakeBody", debRightCard.transform,
            "<b>Summary Checklist:</b>\n\n" +
            "1. Friend asks for urgent survey / vote link? → <b>Call them first!</b>\n\n" +
            "2. Website has <i>http://</i> instead of <i>https://</i>? → <b>Insecure!</b>\n\n" +
            "3. Webpage asks you to 'Link Device' with QR? → <b>Hijack attempt!</b>\n\n" +
            "4. Cloud 2FA enabled? → <b>Attack completely blocked!</b>",
            15, TextAlignmentOptions.TopLeft, new Color(0.90f, 0.95f, 1.0f));
        SetRectTransform(takeBody.gameObject, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        // =========================================================================
        // 9. Wire All Serialized Properties onto Scenario3_FakeLinkManager
        // =========================================================================
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("currentPhase").enumValueIndex = 0;
        so.FindProperty("immersionNotificationPrompt").objectReferenceValue = immersionPanel;
        so.FindProperty("skipImmersionButton").objectReferenceValue = skipImmersionBtn.GetComponent<Button>();

        so.FindProperty("telegramChatScreen").objectReferenceValue = telegramScreen;
        so.FindProperty("emptyChatStatePanel").objectReferenceValue = emptyChatPanel;
        so.FindProperty("activeConversationPanel").objectReferenceValue = activeChatPanel;
        so.FindProperty("sopheaChatItemButton").objectReferenceValue = sopheaBtn;
        so.FindProperty("friendNameText").objectReferenceValue = friendNameText;
        so.FindProperty("messagePreviewText").objectReferenceValue = messagePreviewText;
        so.FindProperty("openLinkButton").objectReferenceValue = openLinkBtn.GetComponent<Button>();
        so.FindProperty("callFriendButton").objectReferenceValue = callFriendBtn.GetComponent<Button>();
        so.FindProperty("callFriendButtonText").objectReferenceValue = callFriendBtnTxt;
        so.FindProperty("callWaveformVisualizer").objectReferenceValue = waveVisualizer;

        so.FindProperty("fakeWebpageScreen").objectReferenceValue = fakeWebpageScreen;
        so.FindProperty("urlAddressBarText").objectReferenceValue = urlAddressBarText;
        so.FindProperty("httpWarningBadge").objectReferenceValue = httpWarningBadge;
        so.FindProperty("telegramOfficialWarningBanner").objectReferenceValue = officialWarningBanner;
        so.FindProperty("qrCodePromptPanel").objectReferenceValue = qrPromptPanel;
        so.FindProperty("scanQRButton").objectReferenceValue = scanQRBtn.GetComponent<Button>();
        so.FindProperty("refuseQRButton").objectReferenceValue = refuseQRBtn.GetComponent<Button>();
        so.FindProperty("inspectUrlButton").objectReferenceValue = inspectUrlBtn.GetComponent<Button>();
        so.FindProperty("inspectHttpButton").objectReferenceValue = inspectHttpBtn.GetComponent<Button>();
        so.FindProperty("inspectBannerButton").objectReferenceValue = inspectBannerBtn.GetComponent<Button>();
        so.FindProperty("inspectDeviceLinkButton").objectReferenceValue = inspectDeviceLinkBtn.GetComponent<Button>();
        so.FindProperty("enable2FAButton").objectReferenceValue = enable2FABtn.GetComponent<Button>();
        so.FindProperty("redFlagNotificationText").objectReferenceValue = redFlagNotificationText;

        so.FindProperty("consequencePanel").objectReferenceValue = consequencePanel;
        so.FindProperty("consequenceTitleText").objectReferenceValue = conTitle;
        so.FindProperty("consequenceBodyText").objectReferenceValue = conBody;
        so.FindProperty("consequenceStatusIcon").objectReferenceValue = outcomePill.GetComponent<Image>();

        so.FindProperty("debriefPanel").objectReferenceValue = debriefPanel;
        so.FindProperty("cardWeaponizedTrust").objectReferenceValue = card1;
        so.FindProperty("cardLinkDevicePurpose").objectReferenceValue = card2;
        so.FindProperty("cardTwoStepVerification").objectReferenceValue = card3;
        so.FindProperty("earnedXPText").objectReferenceValue = xpEarnedText;
        so.FindProperty("redFlagsFoundText").objectReferenceValue = redFlagsText;
        so.FindProperty("debriefNextCardButton").objectReferenceValue = nextCardBtn.GetComponent<Button>();
        so.FindProperty("restartButton").objectReferenceValue = restartBtn.GetComponent<Button>();
        so.FindProperty("mainMenuButton").objectReferenceValue = menuBtn.GetComponent<Button>();

        so.ApplyModifiedProperties();

        // 9b. Wire Scenario Audio Sources & Clips onto Scenario3 Manager
        SetupScenarioAudio(manager, scenarioMgrObj);

        // 10. Save Prefab
        if (!Directory.Exists("Assets/Scenarios/Scenario3/Prefabs")) Directory.CreateDirectory("Assets/Scenarios/Scenario3/Prefabs");
        string prefabPath = "Assets/Scenarios/Scenario3/Prefabs/Scenario3_Canvas.prefab";
        PrefabUtility.SaveAsPrefabAsset(canvasObj, prefabPath);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[Scenario3Builder] Seamless Curved Holographic Screen built successfully!");
    }

    private static CurvedSections CreateCurvedSectionRoots(string phaseName, Transform parent)
    {
        GameObject phaseRoot = CreateUIObject(phaseName, parent);
        SetRectTransform(phaseRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Exact Trigonometric Seam Math:
        // Total arc width = 1600px, divided into Center (704px, 0 deg) and Left/Right (448px, +/-13.5 deg)
        // Center section ends at X = +/-352. Left section inner edge is at X = -352. Seam gap = 0.0 mm.

        // Center Section: width 704, height 900 at (0, 0, 0)
        GameObject center = CreateUIObject("CenterSection", phaseRoot.transform);
        RectTransform rtCenter = center.GetComponent<RectTransform>();
        rtCenter.sizeDelta = new Vector2(704f, 900f);
        rtCenter.anchoredPosition3D = Vector3.zero;
        rtCenter.localRotation = Quaternion.identity;

        // Exact Trigonometric Seam Math (18.0 deg inward curve, 0.0 mm seam gap):
        // Left Section: width 448, height 900 at (-565.037f, 0, -69.220f), rot Y = -18.0 deg
        GameObject left = CreateUIObject("LeftSection", phaseRoot.transform);
        RectTransform rtLeft = left.GetComponent<RectTransform>();
        rtLeft.sizeDelta = new Vector2(448f, 900f);
        rtLeft.anchoredPosition3D = new Vector3(-565.037f, 0f, -69.220f);
        rtLeft.localRotation = Quaternion.Euler(0f, -18.0f, 0f);

        // Right Section: width 448, height 900 at (+565.037f, 0, -69.220f), rot Y = +18.0 deg
        GameObject right = CreateUIObject("RightSection", phaseRoot.transform);
        RectTransform rtRight = right.GetComponent<RectTransform>();
        rtRight.sizeDelta = new Vector2(448f, 900f);
        rtRight.anchoredPosition3D = new Vector3(565.037f, 0f, -69.220f);
        rtRight.localRotation = Quaternion.Euler(0f, 18.0f, 0f);

        CurvedSections cs;
        cs.root = phaseRoot;
        cs.left = left;
        cs.center = center;
        cs.right = right;
        return cs;
    }

    private static void SetupVRAudioFeedback(GameObject coreManagers)
    {
        VRInteractionHandler vrHandler = coreManagers.GetComponent<VRInteractionHandler>();
        if (vrHandler == null) vrHandler = coreManagers.AddComponent<VRInteractionHandler>();

        AudioSource vrAudio = coreManagers.GetComponent<AudioSource>();
        if (vrAudio == null) vrAudio = coreManagers.AddComponent<AudioSource>();
        vrAudio.playOnAwake = false;
        vrAudio.spatialBlend = 0f;
        vrAudio.volume = 0.85f;

        AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/VRTemplateAssets/Audio/Button_22_click.wav");
        AudioClip alertClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Security_Warning.wav");
        AudioClip victoryClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Success_Fanfare.wav");

        SerializedObject so = new SerializedObject(vrHandler);
        so.FindProperty("audioSource").objectReferenceValue = vrAudio;
        if (clickClip != null) so.FindProperty("clickSound").objectReferenceValue = clickClip;
        if (alertClip != null) so.FindProperty("alertSound").objectReferenceValue = alertClip;
        if (victoryClip != null) so.FindProperty("successSound").objectReferenceValue = victoryClip;
        so.ApplyModifiedProperties();
    }

    private static void SetupScenarioAudio(Scenario3_FakeLinkManager manager, GameObject managerObj)
    {
        var allSrcs = managerObj.GetComponents<AudioSource>();
        AudioSource sfxSrc = allSrcs.Length > 0 ? allSrcs[0] : managerObj.AddComponent<AudioSource>();
        sfxSrc.playOnAwake = false;
        sfxSrc.loop = false;
        sfxSrc.spatialBlend = 0f;
        sfxSrc.volume = 0.85f;

        for (int i = 1; i < allSrcs.Length; i++)
        {
            if (allSrcs[i] != null)
            {
                allSrcs[i].Stop();
                allSrcs[i].clip = null;
                allSrcs[i].playOnAwake = false;
                allSrcs[i].loop = false;
                DestroyImmediate(allSrcs[i]);
            }
        }

        AudioClip phoneBuzz   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Phone_Notification.wav");
        AudioClip linkClick   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/VRTemplateAssets/Audio/Button_22_click.wav");
        AudioClip hackAlarm   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Security_Warning.wav");
        AudioClip victory     = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Success_Fanfare.wav");

        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("sfxAudioSource").objectReferenceValue     = sfxSrc;
        so.FindProperty("ambientAudioSource").objectReferenceValue = null;
        if (phoneBuzz != null) so.FindProperty("phoneBuzzClip").objectReferenceValue    = phoneBuzz;
        if (linkClick != null) so.FindProperty("linkClickClip").objectReferenceValue    = linkClick;
        if (hackAlarm != null) so.FindProperty("hackAlarmClip").objectReferenceValue    = hackAlarm;
        if (victory   != null) so.FindProperty("victoryFanfareClip").objectReferenceValue = victory;
        so.ApplyModifiedProperties();
    }

    private static void BuildEnvironmentAndCurvedChassis()
    {
        GameObject oldStudy = GameObject.Find("StudyRoom_Environment");
        if (oldStudy != null) DestroyImmediate(oldStudy);

        GameObject envObj = GameObject.Find("Scenario3_Environment");
        if (envObj == null)
        {
            envObj = new GameObject("Scenario3_Environment");
            Undo.RegisterCreatedObjectUndo(envObj, "Create Scenario 3 Environment");
        }

        // Clean up duplicate SafetySubFloors if re-running builder
        for (int i = envObj.transform.childCount - 1; i >= 0; i--)
        {
            var child = envObj.transform.GetChild(i);
            if (child.name == "SafetySubFloor")
            {
                DestroyImmediate(child.gameObject);
            }
        }

        Material matFloor = GetOrCreateMaterial("Mat_DarkFloor", new Color(0.22f, 0.24f, 0.28f), 0.45f);

        // Safety Ground
        CreateBox("SafetySubFloor", envObj.transform, new Vector3(0, -0.20f, 0.2f), new Vector3(60.0f, 0.1f, 60.0f), matFloor, true);

        // Modular Facility
        GameObject levelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scenarios/Scenario3/Prefabs/BarkingDog_Level.prefab");
        GameObject levelObj = envObj.transform.Find("BarkingDog_ModularFacility")?.gameObject;
        if (levelObj == null && levelPrefab != null)
        {
            levelObj = (GameObject)PrefabUtility.InstantiatePrefab(levelPrefab, envObj.transform);
            levelObj.name = "BarkingDog_ModularFacility";
            levelObj.transform.position = Vector3.zero;
            levelObj.transform.rotation = Quaternion.identity;
        }

        if (levelObj != null)
        {
            Transform r3 = levelObj.transform.Find("Room_Big_Part_03");
            if (r3 != null)
            {
                for (int i = 0; i < r3.childCount; i++)
                {
                    Transform c = r3.GetChild(i);
                    if (c.name == "Floor_01")
                    {
                        if (c.localPosition.y < -0.5f)
                        {
                            c.localPosition = new Vector3(c.localPosition.x, 0f, c.localPosition.z);
                        }
                        // Vertical pit wall panels rotated ~90 deg around X or Z
                        if (Mathf.Abs(c.localEulerAngles.x - 90f) < 5f || Mathf.Abs(c.localEulerAngles.z - 90f) < 5f ||
                            Mathf.Abs(c.localEulerAngles.x - 270f) < 5f || Mathf.Abs(c.localEulerAngles.z - 270f) < 5f)
                        {
                            c.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        // HoloWorkstation root
        GameObject wsRoot = GameObject.Find("HoloWorkstation");
        if (wsRoot == null)
        {
            wsRoot = new GameObject("HoloWorkstation");
            Undo.RegisterCreatedObjectUndo(wsRoot, "Create HoloWorkstation");
        }
        wsRoot.transform.position = WorkstationAnchor;
        wsRoot.transform.rotation = Quaternion.Euler(0f, WorkstationYaw, 0f);

        // Soft Cyan Screen BackGlow Light
        Transform oldBackGlow = wsRoot.transform.Find("HoloScreen_BackGlow");
        if (oldBackGlow != null) DestroyImmediate(oldBackGlow.gameObject);

        GameObject backGlowObj = new GameObject("HoloScreen_BackGlow");
        backGlowObj.transform.SetParent(wsRoot.transform, false);
        backGlowObj.transform.localPosition = new Vector3(0, WorkstationHeight, WorkstationDistance + 0.15f);
        Light backGlow = backGlowObj.AddComponent<Light>();
        backGlow.type = LightType.Point;
        backGlow.color = new Color(0.15f, 0.65f, 1.0f);
        backGlow.range = 4.0f;
        backGlow.intensity = 1.2f;
        backGlow.shadows = LightShadows.None;

        // Build continuous curved cyber chassis with real transparent glass and glowing neon rails
        BuildContinuousCurvedChassis(wsRoot, WorkstationDistance, 68.6f, 0.957f, WorkstationHeight, 0.015f);
    }

    private static void BuildContinuousCurvedChassis(GameObject wsRoot, float radius, float angleSpanDeg, float height, float yCenter, float zOffset)
    {
        Transform oldVisor = wsRoot.transform.Find("Continuous_Curved_Chassis");
        if (oldVisor != null) DestroyImmediate(oldVisor.gameObject);

        GameObject chassisObj = new GameObject("Continuous_Curved_Chassis");
        chassisObj.transform.SetParent(wsRoot.transform, false);
        chassisObj.transform.localPosition = Vector3.zero;
        chassisObj.transform.localRotation = Quaternion.identity;

        MeshFilter mf = chassisObj.AddComponent<MeshFilter>();
        MeshRenderer mr = chassisObj.AddComponent<MeshRenderer>();

        // Proper URP Transparent Cyber Glass material (translucent obsidian with subtle cyan tint)
        Material matGlass = GetOrCreateTransparentGlassMaterial("Mat_HoloVisorGlass", new Color(0.08f, 0.16f, 0.24f, 0.35f), 0.95f);
        mr.material = matGlass;

        int segments = 48;
        int vertexCount = (segments + 1) * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[segments * 12];

        float halfAngle = (angleSpanDeg * 0.5f) * Mathf.Deg2Rad;
        float yBottom = yCenter - height * 0.5f;
        float yTop = yCenter + height * 0.5f;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float theta = Mathf.Lerp(-halfAngle, halfAngle, t);
            float x = radius * Mathf.Sin(theta);
            float z = radius * Mathf.Cos(theta) + zOffset;

            int vBottom = i * 2;
            int vTop = i * 2 + 1;

            vertices[vBottom] = new Vector3(x, yBottom, z);
            vertices[vTop] = new Vector3(x, yTop, z);

            uvs[vBottom] = new Vector2(t, 0f);
            uvs[vTop] = new Vector2(t, 1f);

            if (i < segments)
            {
                int triIndex = i * 12;
                int vb = i * 2;
                int vt = i * 2 + 1;
                int vnb = i * 2 + 2;
                int vnt = i * 2 + 3;

                // Front face
                triangles[triIndex]     = vb;
                triangles[triIndex + 1] = vt;
                triangles[triIndex + 2] = vnb;
                triangles[triIndex + 3] = vnb;
                triangles[triIndex + 4] = vt;
                triangles[triIndex + 5] = vnt;

                // Back face
                triangles[triIndex + 6]  = vnb;
                triangles[triIndex + 7]  = vt;
                triangles[triIndex + 8]  = vb;
                triangles[triIndex + 9]  = vnt;
                triangles[triIndex + 10] = vt;
                triangles[triIndex + 11] = vnb;
            }
        }

        Mesh visorMesh = new Mesh { name = "CurvedChassis_Mesh" };
        visorMesh.vertices = vertices;
        visorMesh.uv = uvs;
        visorMesh.triangles = triangles;
        visorMesh.RecalculateNormals();
        visorMesh.RecalculateBounds();
        mf.sharedMesh = visorMesh;

        // Top and Bottom sleek glowing cyber rails
        Material matNeon = GetOrCreateMaterial("Mat_NeonCyan", new Color(0.15f, 0.85f, 1.0f), 0.1f, true);
        CreateCurvedRail("CyberRail_Top", chassisObj.transform, radius, yTop + 0.005f, angleSpanDeg, 0.012f, matNeon);
        CreateCurvedRail("CyberRail_Bottom", chassisObj.transform, radius, yBottom - 0.005f, angleSpanDeg, 0.012f, matNeon);
    }

    private static Material GetOrCreateTransparentGlassMaterial(string name, Color color, float smoothness)
    {
        string path = "Assets/_Project/Materials/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            if (!Directory.Exists("Assets/_Project/Materials")) Directory.CreateDirectory("Assets/_Project/Materials");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

        // Configure URP Lit as REAL TRANSPARENT
        mat.SetFloat("_Surface", 1.0f); // 1 = Transparent
        mat.SetFloat("_Blend", 0.0f);   // 0 = Alpha blend
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void CreateCurvedRail(string name, Transform parent, float radius, float yPos, float angleSpanDeg, float thickness, Material mat)
    {
        GameObject railObj = new GameObject(name);
        railObj.transform.SetParent(parent, false);

        MeshFilter mf = railObj.AddComponent<MeshFilter>();
        MeshRenderer mr = railObj.AddComponent<MeshRenderer>();
        mr.material = mat;

        int segments = 32;
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        int[] triangles = new int[segments * 6];

        float halfAngle = (angleSpanDeg * 0.5f) * Mathf.Deg2Rad;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float theta = Mathf.Lerp(-halfAngle, halfAngle, t);
            float x = radius * Mathf.Sin(theta);
            float z = radius * Mathf.Cos(theta);

            vertices[i * 2] = new Vector3(x, yPos - thickness * 0.5f, z);
            vertices[i * 2 + 1] = new Vector3(x, yPos + thickness * 0.5f, z);

            if (i < segments)
            {
                int ti = i * 6;
                int vi = i * 2;
                triangles[ti] = vi;
                triangles[ti + 1] = vi + 1;
                triangles[ti + 2] = vi + 2;
                triangles[ti + 3] = vi + 2;
                triangles[ti + 4] = vi + 1;
                triangles[ti + 5] = vi + 3;
            }
        }

        Mesh mesh = new Mesh { name = name + "_Mesh" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
    }

    private static GameObject CreateChatListItem(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        string avatarChar, Color avatarColor, string chatTitle, string snippet, string timeStr, bool hasUnread)
    {
        GameObject itemObj = CreateGlassCard(name, parent, hasUnread ? new Color(0.92f, 0.96f, 1.0f) : Color.white, -4f);
        SetRectTransform(itemObj, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

        Button btn = itemObj.AddComponent<Button>();
        btn.targetGraphic = itemObj.GetComponent<Image>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.88f, 0.93f, 1.0f);
        cb.pressedColor = new Color(0.72f, 0.85f, 0.98f);
        btn.colors = cb;

        GameObject avObj = CreatePill("Avatar", itemObj.transform, avatarColor, -2f);
        SetRectTransform(avObj, new Vector2(0.04f, 0.15f), new Vector2(0.20f, 0.85f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI avChar = CreateTMPText("Char", avObj.transform, $"<b>{avatarChar}</b>", 16, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(avChar.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI titleTxt = CreateTMPText("Title", itemObj.transform, $"<b>{chatTitle}</b>", 15, TextAlignmentOptions.TopLeft, new Color(0.12f, 0.12f, 0.12f));
        SetRectTransform(titleTxt.gameObject, new Vector2(0.24f, 0.52f), new Vector2(0.75f, 0.95f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI snipTxt = CreateTMPText("Snippet", itemObj.transform, snippet, 13, TextAlignmentOptions.TopLeft, new Color(0.50f, 0.55f, 0.60f));
        SetRectTransform(snipTxt.gameObject, new Vector2(0.24f, 0.10f), new Vector2(0.85f, 0.50f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI timeTxt = CreateTMPText("Time", itemObj.transform, timeStr, 12, TextAlignmentOptions.TopRight, new Color(0.55f, 0.60f, 0.65f));
        SetRectTransform(timeTxt.gameObject, new Vector2(0.75f, 0.55f), new Vector2(0.96f, 0.95f), Vector2.zero, Vector2.zero);

        if (hasUnread)
        {
            GameObject badge = CreatePill("UnreadBadge", itemObj.transform, new Color(0.14f, 0.50f, 0.80f), -3f);
            SetRectTransform(badge, new Vector2(0.85f, 0.15f), new Vector2(0.96f, 0.45f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI badgeTxt = CreateTMPText("1", badge.transform, "<b>1</b>", 12, TextAlignmentOptions.Center, Color.white);
            SetRectTransform(badgeTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        return itemObj;
    }

    private static void CleanConflictingObjects()
    {
        string[] oldNames = {
            "Desktop_Monitor_Frame",
            "Workstation_Desk",
            "Continuous_Curved_Visor",
            "Floating_Cyber_Tray",
            "CurvedVisor_Backing",
            "Curved_Cyber_Rails",
            "Continuous_Curved_Chassis"
        };
        foreach (string name in oldNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null) DestroyImmediate(obj);
        }

        GameObject ws = GameObject.Find("HoloWorkstation");
        if (ws != null)
        {
            for (int i = ws.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = ws.transform.GetChild(i);
                if (child.name != "HoloScreen_BackGlow")
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }

    private static void UpgradeModularKitMaterialsToURP()
    {
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null) return;

        string matFolder = "Assets/Barking_Dog/3D Free Modular Kit/Meshes/Materials";
        if (!AssetDatabase.IsValidFolder(matFolder)) return;

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { matFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                if (mat.name.ToLower().Contains("glass"))
                {
                    mat.shader = urpLitShader;
                    mat.SetFloat("_Surface", 1.0f);
                    mat.SetFloat("_Blend", 0.0f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = 3000;
                    mat.SetColor("_BaseColor", new Color(0.15f, 0.35f, 0.55f, 0.25f));
                    mat.SetFloat("_Smoothness", 0.95f);
                    mat.SetFloat("_Metallic", 0.1f);
                    EditorUtility.SetDirty(mat);
                }
                else if (mat.shader != urpLitShader)
                {
                    Color existingColor = mat.HasProperty("_Color") ? mat.color : Color.white;
                    Texture mainTex = mat.mainTexture;
                    mat.shader = urpLitShader;
                    mat.SetColor("_BaseColor", existingColor);
                    if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                    EditorUtility.SetDirty(mat);
                }
            }
        }
    }

    private static void SetupRoomLighting()
    {
        Light[] allLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in allLights)
        {
            if (l.type == LightType.Directional)
            {
                l.color = new Color(0.95f, 0.98f, 1.0f);
                l.intensity = 1.0f;
                l.shadows = LightShadows.Soft;
            }
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.35f, 0.40f, 0.48f);
    }

    private static void RestoreControllersOnXROrigin()
    {
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin == null) return;

        Transform camOffset = xrOrigin.transform.Find("Camera Offset");
        if (camOffset == null) return;

        Transform leftController = camOffset.Find("Left Controller");
        if (leftController != null)
        {
            leftController.gameObject.SetActive(true);
            var renderers = leftController.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) r.enabled = true;
        }

        Transform rightController = camOffset.Find("Right Controller");
        if (rightController != null)
        {
            rightController.gameObject.SetActive(true);
            var renderers = rightController.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) r.enabled = true;
        }
    }

    private static void ResetPlayerSpawnPose()
    {
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin == null) return;

        xrOrigin.transform.position = WorkstationAnchor;
        xrOrigin.transform.rotation = Quaternion.Euler(0f, WorkstationYaw, 0f);

        var originComp = xrOrigin.GetComponent<Unity.XR.CoreUtils.XROrigin>();
        if (originComp != null)
        {
            originComp.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Device;
            originComp.CameraYOffset = WorkstationHeight;
        }

        Transform camOffset = xrOrigin.transform.Find("Camera Offset");
        if (camOffset != null)
        {
            camOffset.localPosition = new Vector3(0f, WorkstationHeight, 0f);
            camOffset.localRotation = Quaternion.identity;

            Transform mainCam = camOffset.Find("Main Camera");
            if (mainCam != null)
            {
                mainCam.localPosition = Vector3.zero;
                mainCam.localRotation = Quaternion.identity;
            }
        }
    }

    private static Sprite CreateGlassCardSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color border = new Color(0.20f, 0.85f, 1.0f, 0.85f);
        Color fill = new Color(0.08f, 0.14f, 0.24f, 0.55f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < 2 || x >= size - 2 || y < 2 || y >= size - 2;
                tex.SetPixel(x, y, isBorder ? border : fill);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
    }

    private static Sprite CreateGlowingButtonSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color fill = Color.white;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, fill);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(6, 6, 6, 6));
    }

    private static Sprite CreatePillSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
    }

    private static Sprite CreateQRTexture()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color black = Color.black;
        Color white = Color.white;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, white);

        DrawQRFinderPattern(tex, 16, 16, 48);
        DrawQRFinderPattern(tex, size - 64, 16, 48);
        DrawQRFinderPattern(tex, 16, size - 64, 48);

        System.Random rnd = new System.Random(2026);
        for (int y = 20; y < size - 20; y += 8)
        {
            for (int x = 20; x < size - 20; x += 8)
            {
                bool inFinder = (x < 72 && y < 72) || (x > size - 72 && y < 72) || (x < 72 && y > size - 72);
                if (!inFinder && rnd.NextDouble() > 0.45)
                {
                    for (int dy = 0; dy < 8; dy++)
                        for (int dx = 0; dx < 8; dx++)
                            tex.SetPixel(x + dx, y + dy, black);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static void DrawQRFinderPattern(Texture2D tex, int startX, int startY, int size)
    {
        for (int y = startY; y < startY + size; y++)
            for (int x = startX; x < startX + size; x++)
                tex.SetPixel(x, y, Color.black);

        int innerOffset = size / 7;
        int innerSize = size - 2 * innerOffset;
        for (int y = startY + innerOffset; y < startY + innerOffset + innerSize; y++)
            for (int x = startX + innerOffset; x < startX + innerOffset + innerSize; x++)
                tex.SetPixel(x, y, Color.white);

        int centerOffset = size * 2 / 7;
        int centerSize = size - 2 * centerOffset;
        for (int y = startY + centerOffset; y < startY + centerOffset + centerSize; y++)
            for (int x = startX + centerOffset; x < startX + centerOffset + centerSize; x++)
                tex.SetPixel(x, y, Color.black);
    }

    private static Sprite CreateScannerReticleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.clear);

        Color cyan = new Color(0.15f, 0.85f, 1.0f);
        int thick = 3;
        int arm = 14;

        for (int t = 0; t < thick; t++)
        {
            for (int i = 0; i < arm; i++)
            {
                tex.SetPixel(i, t, cyan);
                tex.SetPixel(t, i, cyan);
                tex.SetPixel(size - 1 - i, t, cyan);
                tex.SetPixel(size - 1 - t, i, cyan);
                tex.SetPixel(i, size - 1 - t, cyan);
                tex.SetPixel(t, size - 1 - i, cyan);
                tex.SetPixel(size - 1 - i, size - 1 - t, cyan);
                tex.SetPixel(size - 1 - t, size - 1 - i, cyan);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static Material GetOrCreateMaterial(string name, Color color, float smoothness, bool isEmissive = false)
    {
        string path = "Assets/_Project/Materials/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            if (!Directory.Exists("Assets/_Project/Materials")) Directory.CreateDirectory("Assets/_Project/Materials");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (isEmissive)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 2.0f);
            }
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        }
        return mat;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private static void SetRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static TextMeshProUGUI CreateTMPText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions align, Color color)
    {
        GameObject obj = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) tmp.font = defaultFont;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private static GameObject CreateGlassCard(string name, Transform parent, Color color, float zOffset = 0f)
    {
        GameObject card = CreateUIObject(name, parent);
        Image img = card.AddComponent<Image>();
        img.sprite = glassCardSprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        if (zOffset != 0f)
        {
            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchoredPosition3D = new Vector3(rt.anchoredPosition.x, rt.anchoredPosition.y, zOffset);
        }
        return card;
    }

    private static GameObject CreatePill(string name, Transform parent, Color color, float zOffset = 0f)
    {
        GameObject pill = CreateUIObject(name, parent);
        Image img = pill.AddComponent<Image>();
        img.sprite = pillSprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        if (zOffset != 0f)
        {
            RectTransform rt = pill.GetComponent<RectTransform>();
            rt.anchoredPosition3D = new Vector3(rt.anchoredPosition.x, rt.anchoredPosition.y, zOffset);
        }
        return pill;
    }

    private static GameObject CreateButton(string name, Transform parent, string text, Color btnColor, float fontSize, float zOffset = 0f)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        Image img = btnObj.AddComponent<Image>();
        img.sprite = glowingBtnSprite;
        img.type = Image.Type.Sliced;
        img.color = btnColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        btn.colors = cb;

        TextMeshProUGUI txt = CreateTMPText("BtnText", btnObj.transform, text, fontSize, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(txt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        if (zOffset != 0f)
        {
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition3D = new Vector3(rt.anchoredPosition.x, rt.anchoredPosition.y, zOffset);
        }
        return btnObj;
    }

    private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool keepCollider = false)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPos;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().material = mat;
        if (!keepCollider)
        {
            Collider col = box.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }
        return box;
    }
}
#endif
