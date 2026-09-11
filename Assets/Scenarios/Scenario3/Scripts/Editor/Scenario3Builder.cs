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
    private static Sprite flatCardSprite;
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

    // True cylindrical display: the flat canvas is rendered to a RenderTexture and shown on
    // a smooth cylinder arc of this radius (world units). Controller rays are unrolled from
    // the same cylinder so interaction matches the curved visuals exactly.
    private const float ScreenCurveRadius = 1.403f;      // 1600px arc @ 68.6 deg -> R = arc / theta
    private const float ScreenArcDeg = 68.6f;            // total arc span (matches chassis backing)
    private const int ScreenUILayer = 8;                 // "ScreenUI" dedicated layer (RT-only render)

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
        defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular SDF.asset");
        if (defaultFont == null)
        {
            defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
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
        flatCardSprite = CreateFlatCardSprite();
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

        // Dedicated layer so the flat canvas is only rendered by the RT camera (the
        // curved display mesh shows the RenderTexture to the player instead).
        // NOTE: SetLayerRecursive runs AFTER all UI children are created (before prefab
        // save) so every UI element ends up on the ScreenUI layer.
        canvasObj.layer = ScreenUILayer;

        // Curved display mesh: a true cylinder arc displaying the canvas RenderTexture.
        // Ray casters unroll hits from the same cylinder (curveRadiusWorld), so
        // interaction matches the curved visuals exactly.
        GameObject displayMesh = BuildCurvedDisplayMesh(canvasObj, canvas);

        // Holographic OS Root Window
        GameObject rootPanel = CreateUIObject("RootBackground", canvasObj.transform);
        SetRectTransform(rootPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // =========================================================================
        // Phase 1: Mission Immersion & Tactical Briefing (Curved 3-Section Surface)
        // =========================================================================
        CurvedSections immSec = CreateCurvedSectionRoots("ImmersionPanel", rootPanel.transform);
        GameObject immersionPanel = immSec.root;

        Color cyberHeader = new Color(0.945f, 0.957f, 0.969f, 1f); // #F1F4F7 light header
        Color inkPrimary = new Color(0.100f, 0.120f, 0.150f);
        Color inkSecondary = new Color(0.380f, 0.420f, 0.480f);
        Color accentBlue = new Color(0.106f, 0.400f, 0.729f);
        Color accentAmber = new Color(0.700f, 0.420f, 0.000f);

        // --- Phase 1 Continuous Top Header (Left, Center, Right) ---
        GameObject immLeftHead = CreateUIObject("ImmLeftHead", immSec.left.transform);
        SetRectTransform(immLeftHead, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image ilh = immLeftHead.AddComponent<Image>();
        ilh.color = cyberHeader;
        TextMeshProUGUI cadtTitle = CreateTMPText("CADTTitle", immLeftHead.transform, "<b>CADT CYBER DEFENSE TERMINAL</b>", 15, TextAlignmentOptions.MidlineLeft, accentBlue);
        SetRectTransform(cadtTitle.gameObject, new Vector2(0.06f, 0f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero);

        GameObject immCenterHead = CreateUIObject("ImmCenterHead", immSec.center.transform);
        SetRectTransform(immCenterHead, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image ich = immCenterHead.AddComponent<Image>();
        ich.color = cyberHeader;
        TextMeshProUGUI subTitle = CreateTMPText("SubTitle", immCenterHead.transform, "WORKSTATION OS v2.6 // INCIDENT DISPATCH", 14, TextAlignmentOptions.Center, inkSecondary);
        SetRectTransform(subTitle.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject immRightHead = CreateUIObject("ImmRightHead", immSec.right.transform);
        SetRectTransform(immRightHead, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image irh = immRightHead.AddComponent<Image>();
        irh.color = cyberHeader;
        TextMeshProUGUI statusTxt = CreateTMPText("StatusTxt", immRightHead.transform, "12:42 PM  <color=#1B8A3E>CONNECTED</color>", 14, TextAlignmentOptions.MidlineRight, inkPrimary);
        SetRectTransform(statusTxt.gameObject, new Vector2(0.05f, 0f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero);

        // --- Phase 1 Left Section: Objectives ---
        GameObject immLeftCard = CreateGlassCard("ImmLeftCard", immSec.left.transform, Color.white, -2f);
        SetRectTransform(immLeftCard, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.91f), Vector2.zero, Vector2.zero);

        GameObject objPill = CreatePill("ObjPill", immLeftCard.transform, new Color(0.106f, 0.400f, 0.729f), -2f);
        SetRectTransform(objPill, new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI objPillTxt = CreateTMPText("ObjPillTxt", objPill.transform, "<b>SITUATION DISPATCH</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(objPillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI objBody = CreateTMPText("ObjBody", immLeftCard.transform,
            "It's a regular Tuesday evening at CADT.\n\n" +
            "You're catching up on coursework at your cyber defense workstation when your Telegram notification lights up.\n\n" +
            "A classmate needs your help — urgently.\n\n" +
            $"<color=#{ColorUtility.ToHtmlStringRGB(accentAmber)}><i>Not everything that looks familiar is safe.\nThink before you act.</i></color>",
            15, TextAlignmentOptions.TopLeft, inkPrimary);
        SetRectTransform(objBody.gameObject, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.85f), Vector2.zero, Vector2.zero);

        // --- Phase 1 Center Section: Hero Briefing ---
        GameObject immHeroCard = CreateGlassCard("ImmHeroCard", immSec.center.transform, Color.white, -2f);
        SetRectTransform(immHeroCard, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.91f), Vector2.zero, Vector2.zero);

        GameObject heroPill = CreatePill("HeroPill", immHeroCard.transform, new Color(0.816f, 0.427f, 0.114f), -2f);
        SetRectTransform(heroPill, new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI heroPillTxt = CreateTMPText("HeroPillTxt", heroPill.transform, "<b>INCIDENT #S3-8820: REVERSE-PROXY PHISHING DISPATCH</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(heroPillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI immersionText = CreateTMPText("ImmersionText", immHeroCard.transform,
            $"<size=24><color=#{ColorUtility.ToHtmlStringRGB(accentBlue)}><b>CADT Cyber Operations // Workstation 03</b></color></size>\n\n" +
            "Shift log: 23:42 ICT. You are logged into your student workstation completing tonight's telecom lab work.\n\n" +
            "Campus networks are active. External messaging relays are synchronized.\n\n" +
            $"<color=#{ColorUtility.ToHtmlStringRGB(accentAmber)}><b>SECURITY ADVISORY:</b></color> University networks report targeted phishing campaigns impersonating trusted students and faculty.\n\n" +
            "Examine all incoming communications carefully. Trust nothing without verification.",
            18, TextAlignmentOptions.TopLeft, inkPrimary);
        SetRectTransform(immersionText.gameObject, new Vector2(0.06f, 0.26f), new Vector2(0.94f, 0.85f), Vector2.zero, Vector2.zero);

        GameObject skipImmersionBtn = CreateButton("SkipImmersionBtn", immHeroCard.transform, "Start Scenario", new Color(0.106f, 0.478f, 0.282f), 18, -15f);
        SetRectTransform(skipImmersionBtn, new Vector2(0.15f, 0.06f), new Vector2(0.85f, 0.20f), Vector2.zero, Vector2.zero);

        // --- Phase 1 Right Section: Threat Advisory ---
        GameObject immRightCard = CreateGlassCard("ImmRightCard", immSec.right.transform, Color.white, -2f);
        SetRectTransform(immRightCard, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.91f), Vector2.zero, Vector2.zero);

        GameObject threatPill = CreatePill("ThreatPill", immRightCard.transform, new Color(0.769f, 0.212f, 0.157f), -2f);
        SetRectTransform(threatPill, new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI threatPillTxt = CreateTMPText("ThreatPillTxt", threatPill.transform, "<b>TACTICAL ADVISORY</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(threatPillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI threatBody = CreateTMPText("ThreatBody", immRightCard.transform,
            "• <b>Threat Posture:</b> ELEVATED\n" +
            "• <b>Vector:</b> Peer Account Impersonation\n" +
            "• <b>Target:</b> Session Credential Theft\n\n" +
            "<b>Investigation Protocol:</b>\n" +
            "1. Scrutinize unusual urgency and links.\n" +
            "2. Audit protocols and web domains.\n" +
            "3. Verify out-of-band when suspicious.\n" +
            "4. Never rush an authorization.",
            15, TextAlignmentOptions.TopLeft, inkPrimary);
        SetRectTransform(threatBody.gameObject, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.85f), Vector2.zero, Vector2.zero);

        // =========================================================================
        // Phase 2: Telegram Desktop — Light Theme (sidebar + full chat window).
        // Call / verification options live OUTSIDE Telegram (physical smartphone).
        // =========================================================================
        CurvedSections teleSec = CreateCurvedSectionRoots("TelegramChatScreen", rootPanel.transform);
        GameObject telegramScreen = teleSec.root;
        telegramScreen.SetActive(false);

        // Chat pane spans the center+right sections (x from -352 to +800)
        RectTransform teleChatRect = teleSec.center.GetComponent<RectTransform>();
        teleChatRect.sizeDelta = new Vector2(1152f, 900f);
        teleChatRect.anchoredPosition3D = new Vector3(224f, 0f, 0f);
        teleChatRect.localRotation = Quaternion.identity;
        UnityEngine.Object.DestroyImmediate(teleSec.right);

        // Official Telegram Desktop Light Theme Colors
        Color teleTitleBarColor = new Color(1.000f, 1.000f, 1.000f, 1f); // #FFFFFF
        Color teleSidebarColor  = new Color(1.000f, 1.000f, 1.000f, 1f); // #FFFFFF
        Color teleChatBgColor   = new Color(0.902f, 0.922f, 0.933f, 1f); // #E6EBEE
        Color teleBubbleColor   = new Color(1.000f, 1.000f, 1.000f, 1f); // incoming bubble white
        Color teleLinkCardColor = new Color(0.969f, 0.973f, 0.976f, 1f); // #F7F8F9
        Color teleAccentBlue    = new Color(0.200f, 0.565f, 0.925f, 1f); // #3390EC
        Color teleSubtleText    = new Color(0.600f, 0.600f, 0.600f, 1f); // #999999
        Color teleTextPrimary   = new Color(0.080f, 0.080f, 0.080f, 1f); // #141414
        Color teleDivider       = new Color(0.878f, 0.878f, 0.878f, 1f); // #E0E0E0

        // --- Left Header: Hamburger & Telegram Title ---
        GameObject teleLeftHeader = CreateUIObject("TeleLeftHeader", teleSec.left.transform);
        SetRectTransform(teleLeftHeader, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image tlhImg = teleLeftHeader.AddComponent<Image>();
        tlhImg.color = teleTitleBarColor;

        GameObject menuPill = CreateUIObject("MenuBtnPill", teleLeftHeader.transform);
        SetRectTransform(menuPill, new Vector2(0.04f, 0.15f), new Vector2(0.14f, 0.85f), Vector2.zero, Vector2.zero);
        Image menuIcon = CreateIcon("MenuIcon", menuPill.transform, ProceduralIconSet.Icon.MenuHamburger, new Color(0.44f, 0.46f, 0.48f), 20f);
        SetRectTransform(menuIcon.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI teleAppTitle = CreateTMPText("TeleAppTitle", teleLeftHeader.transform,
            $"<b><color=#{ColorUtility.ToHtmlStringRGB(teleAccentBlue)}>Telegram</color></b> <size=13><color=#{ColorUtility.ToHtmlStringRGB(teleSubtleText)}>Desktop</color></size>",
            16, TextAlignmentOptions.MidlineLeft, teleTextPrimary);
        SetRectTransform(teleAppTitle.gameObject, new Vector2(0.18f, 0f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero);

        // --- Center Header: Contact Info Bar ---
        GameObject teleCenterHeader = CreateUIObject("TeleCenterHeader", teleSec.center.transform);
        SetRectTransform(teleCenterHeader, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image tchImg = teleCenterHeader.AddComponent<Image>();
        tchImg.color = teleTitleBarColor;

        GameObject headerAvatar = CreateUIObject("HeaderAvatar", teleCenterHeader.transform);
        SetRectTransform(headerAvatar, new Vector2(0.015f, 0.16f), new Vector2(0.065f, 0.84f), Vector2.zero, Vector2.zero);
        Image headerAvatarImg = headerAvatar.AddComponent<Image>();
        headerAvatarImg.sprite = ProceduralIconSet.Get(ProceduralIconSet.Icon.Circle);
        headerAvatarImg.color = new Color(0.94f, 0.49f, 0.23f);
        headerAvatarImg.preserveAspect = true;
        TextMeshProUGUI hAvTxt = CreateTMPText("HAvTxt", headerAvatar.transform, "<b>S</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(hAvTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI friendNameText = CreateTMPText("FriendNameText", teleCenterHeader.transform,
            "<b>Sophea Kem</b>\n<size=12><color=#31A24C>online</color></size>", 16, TextAlignmentOptions.MidlineLeft, teleTextPrimary);
        SetRectTransform(friendNameText.gameObject, new Vector2(0.08f, 0f), new Vector2(0.55f, 1f), Vector2.zero, Vector2.zero);

        Image iconSearchH = CreateIcon("IconSearchH", teleCenterHeader.transform, ProceduralIconSet.Icon.Search, new Color(0.44f, 0.46f, 0.48f), 20f);
        SetRectTransform(iconSearchH.gameObject, new Vector2(0.82f, 0.22f), new Vector2(0.87f, 0.78f), Vector2.zero, Vector2.zero);
        Image iconPhoneH = CreateIcon("IconPhoneH", teleCenterHeader.transform, ProceduralIconSet.Icon.Phone, new Color(0.44f, 0.46f, 0.48f), 20f);
        SetRectTransform(iconPhoneH.gameObject, new Vector2(0.89f, 0.22f), new Vector2(0.94f, 0.78f), Vector2.zero, Vector2.zero);
        Image iconMoreH = CreateIcon("IconMoreH", teleCenterHeader.transform, ProceduralIconSet.Icon.MoreDots, new Color(0.44f, 0.46f, 0.48f), 20f);
        SetRectTransform(iconMoreH.gameObject, new Vector2(0.955f, 0.22f), new Vector2(1.0f, 0.78f), Vector2.zero, Vector2.zero);

        // Header bottom divider
        GameObject teleHeaderDivider = CreateUIObject("HeaderDivider", teleCenterHeader.transform);
        SetRectTransform(teleHeaderDivider, new Vector2(0f, 0f), new Vector2(1f, 0.012f), Vector2.zero, Vector2.zero);
        Image thdImg = teleHeaderDivider.AddComponent<Image>();
        thdImg.color = teleDivider;

        // --- Left Body: Sidebar with Search & Chat List ---
        GameObject teleLeftBody = CreateUIObject("TeleLeftBody", teleSec.left.transform);
        SetRectTransform(teleLeftBody, Vector2.zero, new Vector2(1f, 0.93f), Vector2.zero, Vector2.zero);
        Image tlbImg = teleLeftBody.AddComponent<Image>();
        tlbImg.color = teleSidebarColor;

        GameObject searchPill = CreatePill("SearchBar", teleLeftBody.transform, new Color(0.945f, 0.945f, 0.949f), -2f);
        SetRectTransform(searchPill, new Vector2(0.04f, 0.91f), new Vector2(0.96f, 0.97f), Vector2.zero, Vector2.zero);
        Image iconSearch = CreateIcon("SearchIcon", searchPill.transform, ProceduralIconSet.Icon.Search, teleSubtleText, 15f);
        SetRectTransform(iconSearch.gameObject, new Vector2(0.04f, 0.20f), new Vector2(0.12f, 0.80f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI searchTxt = CreateTMPText("SearchTxt", searchPill.transform, "Search chats...", 14, TextAlignmentOptions.MidlineLeft, teleSubtleText);
        SetRectTransform(searchTxt.gameObject, new Vector2(0.15f, 0f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero);

        CreateChatListItem("ChatItem_Alerts", teleLeftBody.transform,
            new Vector2(0.03f, 0.77f), new Vector2(0.97f, 0.89f),
            "C", new Color(0.11f, 0.44f, 0.72f), "CADT Cybersecurity", "Password rotation policy updated...", "10:30", false, false);

        CreateChatListItem("ChatItem_Group", teleLeftBody.transform,
            new Vector2(0.03f, 0.64f), new Vector2(0.97f, 0.76f),
            "N", new Color(0.15f, 0.68f, 0.38f), "NextGen DMIL Group", "Lab presentation slides draft...", "Yesterday", false, false);

        GameObject sopheaItem = CreateChatListItem("ChatItem_Sophea", teleLeftBody.transform,
            new Vector2(0.03f, 0.51f), new Vector2(0.97f, 0.63f),
            "S", new Color(0.85f, 0.45f, 0.20f), "Sophea Kem", "Hey! Can you please help me...", "Just now", true, true);
        Button sopheaBtn = sopheaItem.GetComponent<Button>();

        // --- Center Body: Active Telegram Chat Pane ---
        GameObject teleCenterBody = CreateUIObject("TeleCenterBody", teleSec.center.transform);
        SetRectTransform(teleCenterBody, Vector2.zero, new Vector2(1f, 0.93f), Vector2.zero, Vector2.zero);
        Image tcbImg = teleCenterBody.AddComponent<Image>();
        tcbImg.color = teleChatBgColor;

        GameObject activeChatPanel = CreateUIObject("ActiveChatPanel", teleCenterBody.transform);
        SetRectTransform(activeChatPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject emptyChatPanel = CreateUIObject("EmptyChatPanel", teleCenterBody.transform);
        SetRectTransform(emptyChatPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        emptyChatPanel.SetActive(false);

        // Date pill
        GameObject datePill = CreatePill("DatePill", activeChatPanel.transform, new Color(0.784f, 0.812f, 0.835f, 0.75f), -2f);
        SetRectTransform(datePill, new Vector2(0.45f, 0.91f), new Vector2(0.55f, 0.96f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI dateTxt = CreateTMPText("DateTxt", datePill.transform, "Today", 12, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(dateTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Typing indicator
        GameObject typingIndicator = CreatePill("TypingIndicator", activeChatPanel.transform, teleBubbleColor, -4f);
        SetRectTransform(typingIndicator, new Vector2(0.03f, 0.60f), new Vector2(0.22f, 0.655f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI typingTxt = CreateTMPText("TypingTxt", typingIndicator.transform, $"<i><color=#{ColorUtility.ToHtmlStringRGB(teleAccentBlue)}>Sophea is typing...</color></i>", 13, TextAlignmentOptions.MidlineLeft, teleTextPrimary);
        SetRectTransform(typingTxt.gameObject, new Vector2(0.08f, 0f), new Vector2(0.96f, 1f), Vector2.zero, Vector2.zero);
        typingIndicator.SetActive(false);

        // Sophea Message Bubble (incoming — left aligned, sits just above the input bar)
        GameObject msgBubble = CreateGlassCard("MsgBubble", activeChatPanel.transform, teleBubbleColor, -5f);
        SetRectTransform(msgBubble, new Vector2(0.03f, 0.16f), new Vector2(0.62f, 0.58f), Vector2.zero, Vector2.zero);
        {
            var bubbleImg = msgBubble.GetComponent<Image>();
            bubbleImg.sprite = flatCardSprite; // borderless like real Telegram bubbles
        }
        msgBubble.SetActive(false);

        TextMeshProUGUI msgSender = CreateTMPText("Sender", msgBubble.transform, $"<b><color=#{ColorUtility.ToHtmlStringRGB(teleAccentBlue)}>Sophea Kem</color></b>", 15, TextAlignmentOptions.TopLeft, teleTextPrimary);
        SetRectTransform(msgSender.gameObject, new Vector2(0.04f, 0.88f), new Vector2(0.70f, 0.96f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI messagePreviewText = CreateTMPText("MessagePreviewText", msgBubble.transform,
            "Hey! Can you please help me fill out a quick survey for our NextGen project?\n" +
            "We urgently need 15 more responses before midnight for our project submission! It only takes 1 minute:",
            15, TextAlignmentOptions.TopLeft, teleTextPrimary);
        SetRectTransform(messagePreviewText.gameObject, new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.86f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI msgTime = CreateTMPText("MsgTime", msgBubble.transform, "12:42 PM", 12, TextAlignmentOptions.MidlineRight, teleSubtleText);
        SetRectTransform(msgTime.gameObject, new Vector2(0.74f, 0.03f), new Vector2(0.90f, 0.10f), Vector2.zero, Vector2.zero);
        Image msgReadTicks = CreateIcon("MsgReadTicks", msgBubble.transform, ProceduralIconSet.Icon.DoubleCheck, new Color(0.384f, 0.627f, 0.247f), 14f);
        SetRectTransform(msgReadTicks.gameObject, new Vector2(0.90f, 0.03f), new Vector2(0.95f, 0.10f), Vector2.zero, Vector2.zero);

        // Telegram Rich Link Card Embed — whole card is clickable to open the link
        GameObject linkCard = CreateGlassCard("LinkCard", msgBubble.transform, teleLinkCardColor, -8f);
        SetRectTransform(linkCard, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.46f), Vector2.zero, Vector2.zero);
        Button linkCardBtn = linkCard.AddComponent<Button>();
        linkCardBtn.targetGraphic = linkCard.GetComponent<Image>();
        ColorBlock linkCb = linkCardBtn.colors;
        linkCb.normalColor = Color.white;
        linkCb.highlightedColor = new Color(0.90f, 0.94f, 0.99f);
        linkCb.pressedColor = new Color(0.82f, 0.89f, 0.97f);
        linkCardBtn.colors = linkCb;
        linkCard.AddComponent<VRUIHoverEffect>();
        GameObject openLinkBtn = linkCard;

        GameObject cyanBar = CreateUIObject("AccentBar", linkCard.transform);
        SetRectTransform(cyanBar, new Vector2(0f, 0.06f), new Vector2(0.012f, 0.94f), Vector2.zero, Vector2.zero);
        Image cbImg = cyanBar.AddComponent<Image>();
        cbImg.color = teleAccentBlue;

        TextMeshProUGUI linkBannerTxt = CreateTMPText("LinkBannerTxt", linkCard.transform,
            "nextgen-survey-portal2026.net", 12, TextAlignmentOptions.TopLeft, teleSubtleText);
        SetRectTransform(linkBannerTxt.gameObject, new Vector2(0.035f, 0.72f), new Vector2(0.96f, 0.90f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI linkTitleTxt = CreateTMPText("LinkTitleTxt", linkCard.transform,
            $"<b><color=#{ColorUtility.ToHtmlStringRGB(teleAccentBlue)}>NextGen Student Survey 2026</color></b>", 14, TextAlignmentOptions.TopLeft, teleTextPrimary);
        SetRectTransform(linkTitleTxt.gameObject, new Vector2(0.035f, 0.46f), new Vector2(0.96f, 0.72f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI linkDesc = CreateTMPText("LinkDesc", linkCard.transform,
            "Official CADT research survey — Distributed Systems & Protocols.\n<color=#EF5350>Insecure HTTP connection</color>",
            12, TextAlignmentOptions.TopLeft, new Color(0.42f, 0.45f, 0.48f));
        SetRectTransform(linkDesc.gameObject, new Vector2(0.035f, 0.08f), new Vector2(0.96f, 0.44f), Vector2.zero, Vector2.zero);

        GameObject linkWarningObj = CreateUIObject("LinkWarningText", activeChatPanel.transform);
        SetRectTransform(linkWarningObj, new Vector2(0.03f, 0.60f), new Vector2(0.62f, 0.66f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI linkWarningTmp = linkWarningObj.AddComponent<TextMeshProUGUI>();
        linkWarningTmp.text = "";
        linkWarningTmp.font = defaultFont;
        linkWarningTmp.fontSize = 12;
        linkWarningTmp.alignment = TextAlignmentOptions.MidlineLeft;
        linkWarningTmp.color = new Color(0.87f, 0.20f, 0.20f);
        linkWarningObj.SetActive(false);

        // Message input bar at bottom
        GameObject inputPill = CreatePill("InputPill", activeChatPanel.transform, teleBubbleColor, -3f);
        SetRectTransform(inputPill, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.12f), Vector2.zero, Vector2.zero);
        Image inputTopLine = CreateUIObject("InputTopLine", inputPill.transform).AddComponent<Image>();
        inputTopLine.color = teleDivider;
        SetRectTransform(inputTopLine.gameObject, new Vector2(0f, 0.98f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image iconAttach = CreateIcon("IconAttach", inputPill.transform, ProceduralIconSet.Icon.Paperclip, teleSubtleText, 16f);
        SetRectTransform(iconAttach.gameObject, new Vector2(0.035f, 0.22f), new Vector2(0.09f, 0.78f), Vector2.zero, Vector2.zero);
        Image iconEmoji = CreateIcon("IconEmoji", inputPill.transform, ProceduralIconSet.Icon.Emoji, teleSubtleText, 18f);
        SetRectTransform(iconEmoji.gameObject, new Vector2(0.82f, 0.22f), new Vector2(0.88f, 0.78f), Vector2.zero, Vector2.zero);
        Image iconMic = CreateIcon("IconMic", inputPill.transform, ProceduralIconSet.Icon.Mic, teleSubtleText, 18f);
        SetRectTransform(iconMic.gameObject, new Vector2(0.91f, 0.22f), new Vector2(0.97f, 0.78f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI inputTxt = CreateTMPText("InputPlaceholder", inputPill.transform, "Write a message...", 14, TextAlignmentOptions.MidlineLeft, teleSubtleText);
        SetRectTransform(inputTxt.gameObject, new Vector2(0.12f, 0f), new Vector2(0.80f, 1f), Vector2.zero, Vector2.zero);

        // NOTE: Calling is intentionally NOT part of the Telegram window — the
        // out-of-band verification happens on the physical smartphone prop.

        // =========================================================================
        // Phase 3: Google Chrome (full-width) — Phishing Webpage + floating
        // Security Tools drawer (toggle button), keeping the page clean.
        // =========================================================================
        CurvedSections webSec = CreateCurvedSectionRoots("FakeWebpageScreen", rootPanel.transform);
        GameObject fakeWebpageScreen = webSec.root;
        fakeWebpageScreen.SetActive(false);

        // Web page spans the ENTIRE window (like a maximized browser).
        RectTransform webFull = webSec.center.GetComponent<RectTransform>();
        webFull.sizeDelta = new Vector2(1600f, 900f);
        webFull.anchoredPosition3D = Vector3.zero;
        webFull.localRotation = Quaternion.identity;
        UnityEngine.Object.DestroyImmediate(webSec.left);
        UnityEngine.Object.DestroyImmediate(webSec.right);

        Color chromeTabStripColor  = new Color(0.871f, 0.882f, 0.902f, 1f); // #DEE1E6
        Color chromeActiveTabColor = new Color(1.000f, 1.000f, 1.000f, 1f); // #FFFFFF
        Color chromeOmniboxCapsule = new Color(0.945f, 0.953f, 0.957f, 1f); // #F1F3F4
        Color chromeSubtleText     = new Color(0.373f, 0.388f, 0.408f, 1f); // #5F6368

        // --- Chrome Tab Bar (full width) ---
        GameObject chromeTabBar = CreateUIObject("ChromeTabBar", webSec.center.transform);
        SetRectTransform(chromeTabBar, new Vector2(0f, 0.93f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        chromeTabBar.AddComponent<Image>().color = chromeTabStripColor;

        // Active tab (left-anchored, fixed width)
        GameObject activeTabObj = CreateGlassCard("ActiveTab", chromeTabBar.transform, chromeActiveTabColor, -2f);
        SetRectTransform(activeTabObj, new Vector2(0.008f, 0.06f), new Vector2(0.155f, 0.98f), Vector2.zero, Vector2.zero);
        Image iconFavicon = CreateIcon("TabFavicon", activeTabObj.transform, ProceduralIconSet.Icon.WarningTriangle, new Color(0.85f, 0.20f, 0.15f), 15f);
        SetRectTransform(iconFavicon.gameObject, new Vector2(0.04f, 0.22f), new Vector2(0.11f, 0.78f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI tabTitle = CreateTMPText("TabTitle", activeTabObj.transform, "NextGen Survey 2026 - CADT Portal", 13, TextAlignmentOptions.MidlineLeft, chromeSubtleText);
        SetRectTransform(tabTitle.gameObject, new Vector2(0.13f, 0f), new Vector2(0.90f, 1f), Vector2.zero, Vector2.zero);
        Image iconTabClose = CreateIcon("TabClose", activeTabObj.transform, ProceduralIconSet.Icon.Close, chromeSubtleText, 12f);
        SetRectTransform(iconTabClose.gameObject, new Vector2(0.92f, 0.28f), new Vector2(0.98f, 0.72f), Vector2.zero, Vector2.zero);

        GameObject newTabBtn = CreatePill("NewTabBtn", chromeTabBar.transform, new Color(0.82f, 0.84f, 0.86f), -2f);
        SetRectTransform(newTabBtn, new Vector2(0.163f, 0.18f), new Vector2(0.178f, 0.82f), Vector2.zero, Vector2.zero);
        Image iconNewTab = CreateIcon("NewTabIcon", newTabBtn.transform, ProceduralIconSet.Icon.Plus, chromeSubtleText, 14f);
        SetRectTransform(iconNewTab.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image iconWinMinC = CreateIcon("IconWinMinC", chromeTabBar.transform, ProceduralIconSet.Icon.Minimize, chromeSubtleText, 15f);
        SetRectTransform(iconWinMinC.gameObject, new Vector2(0.945f, 0.24f), new Vector2(0.960f, 0.76f), Vector2.zero, Vector2.zero);
        Image iconWinMaxC = CreateIcon("IconWinMaxC", chromeTabBar.transform, ProceduralIconSet.Icon.Maximize, chromeSubtleText, 15f);
        SetRectTransform(iconWinMaxC.gameObject, new Vector2(0.965f, 0.24f), new Vector2(0.980f, 0.76f), Vector2.zero, Vector2.zero);
        Image iconWinCloseC = CreateIcon("IconWinCloseC", chromeTabBar.transform, ProceduralIconSet.Icon.Close, chromeSubtleText, 15f);
        SetRectTransform(iconWinCloseC.gameObject, new Vector2(0.985f, 0.24f), new Vector2(1.0f, 0.76f), Vector2.zero, Vector2.zero);

        // --- Chrome Omnibox (full width) ---
        GameObject omniboxBar = CreateUIObject("OmniboxBar", webSec.center.transform);
        SetRectTransform(omniboxBar, new Vector2(0f, 0.85f), new Vector2(1f, 0.93f), Vector2.zero, Vector2.zero);
        omniboxBar.AddComponent<Image>().color = chromeActiveTabColor;

        Image iconNavBack = CreateIcon("NavBack", omniboxBar.transform, ProceduralIconSet.Icon.BackArrow, chromeSubtleText, 17f);
        SetRectTransform(iconNavBack.gameObject, new Vector2(0.012f, 0.22f), new Vector2(0.036f, 0.78f), Vector2.zero, Vector2.zero);
        Image iconNavFwd = CreateIcon("NavForward", omniboxBar.transform, ProceduralIconSet.Icon.ForwardArrow, chromeSubtleText, 17f);
        SetRectTransform(iconNavFwd.gameObject, new Vector2(0.044f, 0.22f), new Vector2(0.068f, 0.78f), Vector2.zero, Vector2.zero);
        Image iconNavReload = CreateIcon("NavReload", omniboxBar.transform, ProceduralIconSet.Icon.Reload, chromeSubtleText, 17f);
        SetRectTransform(iconNavReload.gameObject, new Vector2(0.076f, 0.22f), new Vector2(0.100f, 0.78f), Vector2.zero, Vector2.zero);

        GameObject urlPill = CreateGlassCard("UrlPill", omniboxBar.transform, chromeOmniboxCapsule, -2f);
        SetRectTransform(urlPill, new Vector2(0.105f, 0.10f), new Vector2(0.94f, 0.90f), Vector2.zero, Vector2.zero);

        // Chrome "Not secure" chip (left inside omnibox)
        GameObject httpWarningBadge = CreatePill("HttpWarningBadge", urlPill.transform, new Color(0.85f, 0.20f, 0.15f), -3f);
        SetRectTransform(httpWarningBadge, new Vector2(0.008f, 0.16f), new Vector2(0.115f, 0.84f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI httpWarnTxt = CreateTMPText("WarnTxt", httpWarningBadge.transform, "Not secure", 12, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(httpWarnTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI urlAddressBarText = CreateTMPText("UrlAddressBarText", urlPill.transform,
            "<color=#D93025>http://</color><color=#202124>nextgen-survey-portal2026.net</color><color=#5F6368>/survey/verify-token?id=92841</color>",
            14, TextAlignmentOptions.MidlineLeft, new Color(0.125f, 0.129f, 0.141f));
        SetRectTransform(urlAddressBarText.gameObject, new Vector2(0.13f, 0f), new Vector2(0.93f, 1f), Vector2.zero, Vector2.zero);

        Image iconStar = CreateIcon("IconStar", urlPill.transform, ProceduralIconSet.Icon.Star, chromeSubtleText, 14f);
        SetRectTransform(iconStar.gameObject, new Vector2(0.95f, 0.22f), new Vector2(0.985f, 0.78f), Vector2.zero, Vector2.zero);

        // --- Web Page Body (full width, white) ---
        GameObject webPage = CreateUIObject("WebPage", webSec.center.transform);
        SetRectTransform(webPage, Vector2.zero, new Vector2(1f, 0.85f), Vector2.zero, Vector2.zero);
        webPage.AddComponent<Image>().color = new Color(0.975f, 0.980f, 0.988f);

        // University Navy Header Bar (page top)
        GameObject uniBar = CreatePill("UniBar", webPage.transform, new Color(0.04f, 0.11f, 0.24f), -2f);
        SetRectTransform(uniBar, new Vector2(0f, 0.90f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI uniBarTxt = CreateTMPText("UniBarTxt", uniBar.transform, "<b>CADT RESEARCH PORTAL 2026</b>", 16, TextAlignmentOptions.MidlineLeft, Color.white);
        SetRectTransform(uniBarTxt.gameObject, new Vector2(0.03f, 0f), new Vector2(0.6f, 1f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI uniBarLinks = CreateTMPText("UniBarLinks", uniBar.transform, "Home    Surveys    Results    About", 13, TextAlignmentOptions.MidlineRight, new Color(0.65f, 0.75f, 0.9f));
        SetRectTransform(uniBarLinks.gameObject, new Vector2(0.6f, 0f), new Vector2(0.97f, 1f), Vector2.zero, Vector2.zero);

        // Page hero title
        TextMeshProUGUI pageTitle = CreateTMPText("PageTitle", webPage.transform, "<b>NextGen Student Survey 2026</b>", 30, TextAlignmentOptions.Center, new Color(0.10f, 0.12f, 0.17f));
        SetRectTransform(pageTitle.gameObject, new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI pageSub = CreateTMPText("PageSub", webPage.transform, "Help CADT Student Research — Distributed Systems & Protocols  ·  Project Lead: Sophea Kem (Year 3)", 15, TextAlignmentOptions.Center, new Color(0.42f, 0.46f, 0.52f));
        SetRectTransform(pageSub.gameObject, new Vector2(0.04f, 0.75f), new Vector2(0.96f, 0.80f), Vector2.zero, Vector2.zero);

        // Left content column: survey info + security notice
        GameObject descCard = CreateGlassCard("DescCard", webPage.transform, Color.white, -3f);
        SetRectTransform(descCard, new Vector2(0.06f, 0.28f), new Vector2(0.47f, 0.72f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI surveyInfoText = CreateTMPText("SurveyInfoText", descCard.transform,
            "<size=18><b>Distributed Systems Survey</b></size>\n\n" +
            "This survey collects student feedback on distributed network latency and cloud protocol efficiency for the NextGen capstone project.\n\n" +
            "<b>Who can participate:</b> CADT students, all years.\n\n" +
            "<b>Time required:</b> About 1 minute.\n\n" +
            "<color=#0277BD><b>Responses collected:</b> 85 / 100 verified</color>",
            14, TextAlignmentOptions.TopLeft, new Color(0.12f, 0.15f, 0.20f));
        SetRectTransform(surveyInfoText.gameObject, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero);

        // Official security notice (the clue hidden on the page)
        GameObject officialWarningBanner = CreateGlassCard("OfficialWarningBanner", webPage.transform, new Color(0.992f, 0.945f, 0.941f), -4f);
        SetRectTransform(officialWarningBanner, new Vector2(0.06f, 0.10f), new Vector2(0.47f, 0.25f), Vector2.zero, Vector2.zero);
        GameObject warnHeaderPill = CreatePill("WarnHeaderPill", officialWarningBanner.transform, new Color(0.80f, 0.20f, 0.15f), -2f);
        SetRectTransform(warnHeaderPill, new Vector2(0.03f, 0.68f), new Vector2(0.42f, 0.94f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI warnHeaderTxt = CreateTMPText("WarnHeaderTxt", warnHeaderPill.transform, "<b>SECURITY NOTICE</b>", 12, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(warnHeaderTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TextMeshProUGUI warnBannerTxt = CreateTMPText("WarnBannerTxt", officialWarningBanner.transform,
            "External applications requesting Telegram desktop session pairing should be inspected. Never scan untrusted QR codes to link accounts.",
            12, TextAlignmentOptions.TopLeft, new Color(0.35f, 0.12f, 0.10f));
        SetRectTransform(warnBannerTxt.gameObject, new Vector2(0.045f, 0.10f), new Vector2(0.96f, 0.64f), Vector2.zero, Vector2.zero);

        // Right content column: Telegram QR login card (the bait)
        GameObject qrPromptPanel = CreateGlassCard("QRPromptPanel", webPage.transform, Color.white, -4f);
        SetRectTransform(qrPromptPanel, new Vector2(0.53f, 0.10f), new Vector2(0.94f, 0.72f), Vector2.zero, Vector2.zero);

        GameObject tgHeaderBar = CreatePill("TGHeaderBar", qrPromptPanel.transform, new Color(0.15f, 0.55f, 0.88f), -2f);
        SetRectTransform(tgHeaderBar, new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI tgHeaderTxt = CreateTMPText("TGHeaderTxt", tgHeaderBar.transform, "<b>Verify with Telegram</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(tgHeaderTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI qrLead = CreateTMPText("QrLead", qrPromptPanel.transform, "Scan this QR code to confirm your identity and submit your response.", 13, TextAlignmentOptions.Center, new Color(0.35f, 0.38f, 0.44f));
        SetRectTransform(qrLead.gameObject, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.89f), Vector2.zero, Vector2.zero);

        // QR Code Image (the phone camera scans this)
        GameObject qrImageObj = CreateUIObject("QRBox", qrPromptPanel.transform);
        SetRectTransform(qrImageObj, new Vector2(0.13f, 0.26f), new Vector2(0.47f, 0.79f), Vector2.zero, Vector2.zero);
        Image qrImg = qrImageObj.AddComponent<Image>();
        qrImg.sprite = qrCodeSprite;
        qrImg.color = Color.white;

        TextMeshProUGUI qrInstruction = CreateTMPText("QRInstruction", qrPromptPanel.transform,
            "<size=15><color=#1565C0><b>How to verify</b></color></size>\n\n" +
            "<b>1.</b> Open <b>Telegram</b> on your phone\n\n" +
            "<b>2.</b> Settings → Devices → <b>Link Desktop Device</b>\n\n" +
            "<b>3.</b> Point your camera at this QR code\n\n" +
            "<color=#78909C><i>Session sync is required to record responses.</i></color>",
            12, TextAlignmentOptions.TopLeft, new Color(0.15f, 0.18f, 0.22f));
        SetRectTransform(qrInstruction.gameObject, new Vector2(0.51f, 0.24f), new Vector2(0.95f, 0.80f), Vector2.zero, Vector2.zero);

        // ── Floating Security Tools (toggle button + slide-out drawer) ──────────
        // Backing is created FIRST so it renders BEHIND the drawer content (uGUI
        // sorts by hierarchy order, later siblings draw on top).
        GameObject drawerBacking = CreateGlassCard("DrawerBacking", webPage.transform, new Color(0.88f, 0.90f, 0.94f, 1f), -29f);
        SetRectTransform(drawerBacking, new Vector2(0.631f, 0.025f), new Vector2(0.989f, 0.96f), Vector2.zero, Vector2.zero);
        drawerBacking.SetActive(false);

        GameObject securityDrawer = CreateGlassCard("SecurityDrawer", webPage.transform, Color.white, -30f);
        SetRectTransform(securityDrawer, new Vector2(0.635f, 0.03f), new Vector2(0.985f, 0.955f), Vector2.zero, Vector2.zero);
        securityDrawer.SetActive(false);

        GameObject tacticalPill = CreatePill("TacticalPill", securityDrawer.transform, new Color(0.106f, 0.400f, 0.729f), -4f);
        SetRectTransform(tacticalPill, new Vector2(0.05f, 0.935f), new Vector2(0.95f, 0.985f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI tacticalPillTxt = CreateTMPText("TacticalPillTxt", tacticalPill.transform, "<b>SECURITY INSPECTION TOOLS</b>", 13, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(tacticalPillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Color auditBtnColor = new Color(0.925f, 0.937f, 0.953f);
        GameObject inspectUrlBtn = CreateButtonWithIcon("InspectUrlBtn", securityDrawer.transform, "Audit Domain Integrity", ProceduralIconSet.Icon.Globe, auditBtnColor, 12, -10f, new Color(0.09f, 0.30f, 0.55f), false);
        SetRectTransform(inspectUrlBtn, new Vector2(0.05f, 0.855f), new Vector2(0.95f, 0.925f), Vector2.zero, Vector2.zero);

        GameObject inspectHttpBtn = CreateButtonWithIcon("InspectHttpBtn", securityDrawer.transform, "Audit SSL/TLS Transport", ProceduralIconSet.Icon.LockClosed, auditBtnColor, 12, -10f, new Color(0.09f, 0.30f, 0.55f), false);
        SetRectTransform(inspectHttpBtn, new Vector2(0.05f, 0.775f), new Vector2(0.95f, 0.845f), Vector2.zero, Vector2.zero);

        GameObject inspectBannerBtn = CreateButtonWithIcon("InspectBannerBtn", securityDrawer.transform, "Audit Security Notice", ProceduralIconSet.Icon.WarningTriangle, auditBtnColor, 12, -10f, new Color(0.09f, 0.30f, 0.55f), false);
        SetRectTransform(inspectBannerBtn, new Vector2(0.05f, 0.695f), new Vector2(0.95f, 0.765f), Vector2.zero, Vector2.zero);

        GameObject inspectDeviceLinkBtn = CreateButtonWithIcon("InspectDeviceLinkBtn", securityDrawer.transform, "Audit QR Device Token", ProceduralIconSet.Icon.QrScan, auditBtnColor, 12, -10f, new Color(0.09f, 0.30f, 0.55f), false);
        SetRectTransform(inspectDeviceLinkBtn, new Vector2(0.05f, 0.615f), new Vector2(0.95f, 0.685f), Vector2.zero, Vector2.zero);

        GameObject enable2FABtn = CreateButtonWithIcon("Enable2FABtn", securityDrawer.transform, "Enforce Cloud 2FA Password", ProceduralIconSet.Icon.Shield, new Color(0.106f, 0.400f, 0.729f), 12, -15f, Color.white, true);
        SetRectTransform(enable2FABtn, new Vector2(0.05f, 0.535f), new Vector2(0.95f, 0.605f), Vector2.zero, Vector2.zero);

        // Telemetry Card
        GameObject notifCard = CreateGlassCard("TelemetryCard", securityDrawer.transform, new Color(0.941f, 0.953f, 0.969f, 1f), -6f);
        SetRectTransform(notifCard, new Vector2(0.05f, 0.315f), new Vector2(0.95f, 0.515f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI redFlagNotificationText = CreateTMPText("RedFlagNotificationText", notifCard.transform,
            "<color=#B26A00>Use audit tools above to inspect web telemetry.</color>",
            11, TextAlignmentOptions.TopLeft, new Color(0.13f, 0.15f, 0.18f));
        SetRectTransform(redFlagNotificationText.gameObject, new Vector2(0.06f, 0.55f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI flagHistoryText = CreateTMPText("FlagHistoryText", notifCard.transform,
            "<color=#0277BD><b>TELEMETRY — 0/4 flags</b></color>\n2FA: <color=#C62828>OFF</color>    QR: <color=#B26A00>Pending</color>\n<color=#78909C>No flags found yet.</color>",
            11, TextAlignmentOptions.TopLeft, new Color(0.13f, 0.15f, 0.18f));
        SetRectTransform(flagHistoryText.gameObject, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.50f), Vector2.zero, Vector2.zero);

        // Decision buttons (inside drawer)
        GameObject scanQRBtn = CreateButton("ScanQRBtn", securityDrawer.transform, "Scan QR code", new Color(0.75f, 0.22f, 0.18f), 14, -20f);
        SetRectTransform(scanQRBtn, new Vector2(0.05f, 0.195f), new Vector2(0.95f, 0.285f), Vector2.zero, Vector2.zero);

        GameObject refuseQRBtn = CreateButton("RefuseQRBtn", securityDrawer.transform, "Refuse & secure account", new Color(0.18f, 0.65f, 0.38f), 14, -20f);
        SetRectTransform(refuseQRBtn, new Vector2(0.05f, 0.095f), new Vector2(0.95f, 0.185f), Vector2.zero, Vector2.zero);

        // Floating toggle button (right edge, vertically centered)
        GameObject toolsToggleBtn = CreateRoundButton("ToolsToggleBtn", webPage.transform, new Color(0.106f, 0.400f, 0.729f), ProceduralIconSet.Icon.Shield, false);
        SetRectTransform(toolsToggleBtn, new Vector2(0.972f, 0.47f), new Vector2(1.005f, 0.56f), Vector2.zero, Vector2.zero);
        UIToggleButton toolsToggle = toolsToggleBtn.AddComponent<UIToggleButton>();
        {
            SerializedObject soToggle = new SerializedObject(toolsToggle);
            SerializedProperty targetsProp = soToggle.FindProperty("targets");
            targetsProp.arraySize = 2;
            targetsProp.GetArrayElementAtIndex(0).objectReferenceValue = securityDrawer;
            targetsProp.GetArrayElementAtIndex(1).objectReferenceValue = drawerBacking;
            soToggle.ApplyModifiedProperties();
        }

        // Decision confirmation overlay
        GameObject confirmOverlay = CreateUIObject("DecisionConfirmOverlay", rootPanel.transform);
        SetRectTransform(confirmOverlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image confirmBg = confirmOverlay.AddComponent<Image>();
        confirmBg.color = new Color(0.08f, 0.10f, 0.14f, 0.45f); // soft scrim for light theme
        confirmOverlay.SetActive(false);

        GameObject confirmCard = CreateGlassCard("ConfirmCard", confirmOverlay.transform, Color.white, -10f);
        SetRectTransform(confirmCard, new Vector2(0.28f, 0.30f), new Vector2(0.72f, 0.70f), Vector2.zero, Vector2.zero);

        GameObject confirmPill = CreatePill("ConfirmPill", confirmCard.transform, new Color(0.816f, 0.427f, 0.114f), -4f);
        SetRectTransform(confirmPill, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI confirmPillTxt = CreateTMPText("ConfirmPillTxt", confirmPill.transform, "<b>CONFIRM YOUR DECISION</b>", 17, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(confirmPillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI confirmQuestionText = CreateTMPText("ConfirmQuestionText", confirmCard.transform,
            "<size=20><b>Are you sure?</b></size>\n\nThis action cannot be undone.\nYour decision will determine the outcome.",
            16, TextAlignmentOptions.Center, new Color(0.13f, 0.15f, 0.18f));
        SetRectTransform(confirmQuestionText.gameObject, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.80f), Vector2.zero, Vector2.zero);

        GameObject confirmYesBtn = CreateButton("ConfirmYesBtn", confirmCard.transform, "Yes — Continue", new Color(0.106f, 0.400f, 0.729f), 16, -14f);
        SetRectTransform(confirmYesBtn, new Vector2(0.08f, 0.10f), new Vector2(0.50f, 0.36f), Vector2.zero, Vector2.zero);

        GameObject confirmCancelBtn = CreateButton("ConfirmCancelBtn", confirmCard.transform, "Cancel", new Color(0.545f, 0.580f, 0.635f), 16, -14f);
        SetRectTransform(confirmCancelBtn, new Vector2(0.53f, 0.10f), new Vector2(0.92f, 0.36f), Vector2.zero, Vector2.zero);


        // =========================================================================
        // Phase 4: Mission Consequence Screen (Curved)
        // =========================================================================
        CurvedSections conSec = CreateCurvedSectionRoots("ConsequencePanel", rootPanel.transform);
        GameObject consequencePanel = conSec.root;
        consequencePanel.SetActive(false);

        // Left Section: Attack Methodology
        GameObject conLeftCard = CreateGlassCard("ConLeftCard", conSec.left.transform, Color.white, -5f);
        SetRectTransform(conLeftCard, new Vector2(0.02f, 0.02f), new Vector2(1.0f, 0.98f), Vector2.zero, Vector2.zero);

        GameObject methodPill = CreatePill("MethodPill", conLeftCard.transform, new Color(0.816f, 0.427f, 0.114f), -4f);
        SetRectTransform(methodPill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI methodPillTxt = CreateTMPText("MethodPillTxt", methodPill.transform, "<b>ATTACK METHODOLOGY</b>", 16, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(methodPillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI methodBody = CreateTMPText("MethodBody", conLeftCard.transform,
            "• <b>Reverse-Proxy Relay:</b> The survey portal acts as a live middleman between you and Telegram servers.\n\n" +
            "• <b>QR Session Hijack:</b> The QR code was an authorization token for attacker's desktop client.\n\n" +
            "• <b>Account Cloning:</b> Scanning it authorizes the attacker to download full chat history and impersonate you.",
            15, TextAlignmentOptions.TopLeft, new Color(0.13f, 0.15f, 0.18f));
        SetRectTransform(methodBody.gameObject, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        // Center Section: Hero Outcome
        GameObject conHeroCard = CreateGlassCard("ConHeroCard", conSec.center.transform, Color.white, -5f);
        SetRectTransform(conHeroCard, new Vector2(0.0f, 0.02f), new Vector2(1.0f, 0.98f), Vector2.zero, Vector2.zero);

        GameObject outcomePill = CreatePill("OutcomePill", conHeroCard.transform, new Color(0.106f, 0.400f, 0.729f), -4f);
        SetRectTransform(outcomePill, new Vector2(0.15f, 0.88f), new Vector2(0.85f, 0.96f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI outcomePillTxt = CreateTMPText("OutcomePillTxt", outcomePill.transform, "<b>MISSION OUTCOME</b>", 16, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(outcomePillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI conTitle = CreateTMPText("ConsequenceTitleText", conHeroCard.transform, "<b>Defense Successful</b>", 32, TextAlignmentOptions.Center, new Color(0.106f, 0.478f, 0.282f));
        SetRectTransform(conTitle.gameObject, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI conBody = CreateTMPText("ConsequenceBodyText", conHeroCard.transform,
            "You successfully recognized the spoofed domain, unencrypted protocol, and deceptive QR pairing attempt!\n\n" +
            "By refusing to scan the untrusted QR code and enabling Two-Step Verification, you completely thwarted the reverse-proxy attack.",
            17, TextAlignmentOptions.TopLeft, new Color(0.13f, 0.15f, 0.18f));
        SetRectTransform(conBody.gameObject, new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.68f), Vector2.zero, Vector2.zero);

        // Manual continue button — replaces 4.5s auto-advance
        GameObject continueBtn = CreateButton("ContinueToDebriefBtn", conHeroCard.transform, "Continue to Debrief", new Color(0.14f, 0.48f, 0.78f), 16, -12f);
        SetRectTransform(continueBtn, new Vector2(0.20f, 0.05f), new Vector2(0.80f, 0.18f), Vector2.zero, Vector2.zero);

        // Right Section: Recovery Protocol
        GameObject conRightCard = CreateGlassCard("ConRightCard", conSec.right.transform, Color.white, -5f);
        SetRectTransform(conRightCard, new Vector2(0.0f, 0.02f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);

        GameObject recovPill = CreatePill("RecovPill", conRightCard.transform, new Color(0.106f, 0.400f, 0.729f), -4f);
        SetRectTransform(recovPill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI recovPillTxt = CreateTMPText("RecovPillTxt", recovPill.transform, "<b>RECOVERY PROTOCOL</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(recovPillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI recovBody = CreateTMPText("RecovBody", conRightCard.transform,
            "• <b>Terminate Sessions:</b>\nSettings > Devices > Terminate All Other Sessions.\n\n" +
            "• <b>Enable Cloud 2FA:</b>\nSettings > Privacy > Two-Step Verification.\n\n" +
            "• <b>Alert Contacts:</b>\nWarn peers out-of-band so they do not click forwarded links.",
            15, TextAlignmentOptions.TopLeft, new Color(0.13f, 0.15f, 0.18f));
        SetRectTransform(recovBody.gameObject, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        // =========================================================================
        // Phase 5: Educational Debrief & Scorecard (Curved)
        // =========================================================================
        CurvedSections debSec = CreateCurvedSectionRoots("DebriefPanel", rootPanel.transform);
        GameObject debriefPanel = debSec.root;
        debriefPanel.SetActive(false);

        // Left Section: Scorecard & Controls
        GameObject debLeftCard = CreateGlassCard("DebLeftCard", debSec.left.transform, Color.white, -5f);
        SetRectTransform(debLeftCard, new Vector2(0.02f, 0.02f), new Vector2(1.0f, 0.98f), Vector2.zero, Vector2.zero);

        GameObject scorePill = CreatePill("ScorePill", debLeftCard.transform, new Color(0.106f, 0.400f, 0.729f), -4f);
        SetRectTransform(scorePill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI scorePillTxt = CreateTMPText("ScorePillTxt", scorePill.transform, "<b>MISSION SCORECARD</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(scorePillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI xpEarnedText = CreateTMPText("EarnedXPText", debLeftCard.transform, "<b><color=#1B8A3E>+200 XP</color></b>", 38, TextAlignmentOptions.Center, new Color(0.13f, 0.15f, 0.18f));
        SetRectTransform(xpEarnedText.gameObject, new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.88f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI redFlagsText = CreateTMPText("RedFlagsFoundText", debLeftCard.transform, "Red Flags Found: 4/4\nStatus: DEFENDED", 16, TextAlignmentOptions.Center, new Color(0.13f, 0.15f, 0.18f));
        SetRectTransform(redFlagsText.gameObject, new Vector2(0.06f, 0.58f), new Vector2(0.94f, 0.72f), Vector2.zero, Vector2.zero);

        GameObject nextCardBtn = CreateButton("NextCardBtn", debLeftCard.transform, "Next Lesson Card", new Color(0.106f, 0.400f, 0.729f), 15, -10f);
        SetRectTransform(nextCardBtn, new Vector2(0.06f, 0.43f), new Vector2(0.94f, 0.54f), Vector2.zero, Vector2.zero);

        // Card progress counter — "1 / 3"
        TextMeshProUGUI cardCounterText = CreateTMPText("CardCounterText", debLeftCard.transform, "<b>1 / 3</b>", 16, TextAlignmentOptions.Center, new Color(0.106f, 0.400f, 0.729f));
        SetRectTransform(cardCounterText.gameObject, new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.43f), Vector2.zero, Vector2.zero);

        GameObject restartBtn = CreateButton("RestartBtn", debLeftCard.transform, "Replay Scenario", new Color(0.106f, 0.478f, 0.282f), 15, -10f);
        SetRectTransform(restartBtn, new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.36f), Vector2.zero, Vector2.zero);

        GameObject menuBtn = CreateButton("MainMenuBtn", debLeftCard.transform, "Main Menu", new Color(0.310f, 0.365f, 0.451f), 15, -10f);
        SetRectTransform(menuBtn, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.22f), Vector2.zero, Vector2.zero);

        // Center Section: Lesson Cards Carousel
        GameObject debCenterCard = CreateGlassCard("DebCenterCard", debSec.center.transform, Color.white, -5f);
        SetRectTransform(debCenterCard, new Vector2(0.0f, 0.02f), new Vector2(1.0f, 0.98f), Vector2.zero, Vector2.zero);

        // Card 1: Weaponized Trust
        GameObject card1 = CreateUIObject("Card1_WeaponizedTrust", debCenterCard.transform);
        SetRectTransform(card1, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject c1Pill = CreatePill("C1Pill", card1.transform, new Color(0.816f, 0.427f, 0.114f), -4f);
        SetRectTransform(c1Pill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI c1PillTxt = CreateTMPText("C1PillTxt", c1Pill.transform, "<b>LESSON 1: WEAPONIZED TRUST</b>", 16, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(c1PillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI c1Body = CreateTMPText("C1Body", card1.transform,
            "<size=22><color=#0277BD><b>Why Attackers Target Your Friends' Accounts</b></color></size>\n\n" +
            "Attackers rarely send phishing links from unfamiliar strangers when targeting students or corporate teams. Instead, they first compromise one member of the group, then immediately abuse existing relationship trust to distribute lures.\n\n" +
            "• <b>Key Defense:</b> If a friend asks for urgent votes, survey fills, or verification codes, <b>always verify out-of-band</b> (e.g. voice call or face-to-face) before taking action.",
            17, TextAlignmentOptions.TopLeft, new Color(0.13f, 0.15f, 0.18f));
        SetRectTransform(c1Body.gameObject, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        // Card 2: Link Device Purpose
        GameObject card2 = CreateUIObject("Card2_LinkDevicePurpose", debCenterCard.transform);
        SetRectTransform(card2, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        card2.SetActive(false);

        GameObject c2Pill = CreatePill("C2Pill", card2.transform, new Color(0.106f, 0.400f, 0.729f), -4f);
        SetRectTransform(c2Pill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI c2PillTxt = CreateTMPText("C2PillTxt", c2Pill.transform, "<b>LESSON 2: LINK DEVICE DECEPTION</b>", 16, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(c2PillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI c2Body = CreateTMPText("C2Body", card2.transform,
            "<size=22><color=#0277BD><b>How QR Login Tokens Work</b></color></size>\n\n" +
            "The 'Link Desktop Device' feature in Telegram, WhatsApp, and Signal is exclusively designed to authorize full desktop access to your messaging account.\n\n" +
            "• <b>The Scam:</b> Phishing portals disguise session-pairing QR codes as 'contest vote verifications' or 'identity checkmarks'.\n\n" +
            "• <b>Crucial Rule:</b> <b>Never</b> use 'Link Device' on a web portal or third-party screen!",
            17, TextAlignmentOptions.TopLeft, new Color(0.13f, 0.15f, 0.18f));
        SetRectTransform(c2Body.gameObject, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        // Card 3: Two-Step Verification
        GameObject card3 = CreateUIObject("Card3_TwoStepVerification", debCenterCard.transform);
        SetRectTransform(card3, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        card3.SetActive(false);

        GameObject c3Pill = CreatePill("C3Pill", card3.transform, new Color(0.106f, 0.478f, 0.282f), -4f);
        SetRectTransform(c3Pill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI c3PillTxt = CreateTMPText("C3PillTxt", c3Pill.transform, "<b>LESSON 3: TWO-STEP VERIFICATION (2FA)</b>", 16, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(c3PillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI c3Body = CreateTMPText("C3Body", card3.transform,
            "<size=22><color=#0277BD><b>The Ultimate Defense Against QR Hijacking</b></color></size>\n\n" +
            "Even if an attacker tricks you into scanning a malicious QR code, enabling <b>Two-Step Verification (Cloud Password)</b> stops them in their tracks.\n\n" +
            "• <b>How it protects:</b> The reverse proxy cannot complete device pairing without your secret secondary cloud password.\n\n" +
            "• <b>Action:</b> Telegram > Settings > Privacy & Security > Two-Step Verification.",
            17, TextAlignmentOptions.TopLeft, new Color(0.13f, 0.15f, 0.18f));
        SetRectTransform(c3Body.gameObject, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        // Right Section: Summary Key Takeaway
        GameObject debRightCard = CreateGlassCard("DebRightCard", debSec.right.transform, Color.white, -5f);
        SetRectTransform(debRightCard, new Vector2(0.0f, 0.02f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);

        GameObject takePill = CreatePill("TakePill", debRightCard.transform, new Color(0.106f, 0.400f, 0.729f), -4f);
        SetRectTransform(takePill, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI takePillTxt = CreateTMPText("TakePillTxt", takePill.transform, "<b>KEY TAKEAWAY</b>", 15, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(takePillTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI takeBody = CreateTMPText("TakeBody", debRightCard.transform,
            "<b>Summary Checklist:</b>\n\n" +
            "1. Friend asks for urgent survey / vote link? → <b>Call them first!</b>\n\n" +
            "2. Website has <i>http://</i> instead of <i>https://</i>? → <b>Insecure!</b>\n\n" +
            "3. Webpage asks you to 'Link Device' with QR? → <b>Hijack attempt!</b>\n\n" +
            "4. Cloud 2FA enabled? → <b>Attack completely blocked!</b>",
            15, TextAlignmentOptions.TopLeft, new Color(0.13f, 0.15f, 0.18f));
        SetRectTransform(takeBody.gameObject, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        // =========================================================================
        // 9. Wire All Serialized Properties onto Scenario3_FakeLinkManager
        // =========================================================================
        // 8b. Physical smartphone prop (out-of-band call + QR camera scan)
        GameObject envRoot = GameObject.Find("Scenario3_Environment");
        GameObject smartphone = BuildPhysicalSmartphone(envRoot != null ? envRoot : canvasObj);

        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("currentPhase").enumValueIndex = 0;
        so.FindProperty("immersionNotificationPrompt").objectReferenceValue = immersionPanel;
        so.FindProperty("skipImmersionButton").objectReferenceValue = skipImmersionBtn.GetComponent<Button>();

        so.FindProperty("telegramChatScreen").objectReferenceValue = telegramScreen;
        so.FindProperty("emptyChatStatePanel").objectReferenceValue = emptyChatPanel;
        so.FindProperty("activeConversationPanel").objectReferenceValue = activeChatPanel;
        so.FindProperty("sopheaChatItemButton").objectReferenceValue = sopheaBtn;
        so.FindProperty("friendNameText").objectReferenceValue = friendNameText;
        so.FindProperty("typingIndicator").objectReferenceValue = typingIndicator;
        so.FindProperty("messageBubble").objectReferenceValue = msgBubble;
        so.FindProperty("messagePreviewText").objectReferenceValue = messagePreviewText;
        so.FindProperty("openLinkButton").objectReferenceValue = openLinkBtn.GetComponent<Button>();
        so.FindProperty("refuseLinkButton").objectReferenceValue = null; // refusal is a browser-phase decision now
        so.FindProperty("linkWarningText").objectReferenceValue = linkWarningTmp;
        so.FindProperty("callFriendButton").objectReferenceValue = null;      // calling happens on the physical phone
        so.FindProperty("callFriendButtonText").objectReferenceValue = null;
        so.FindProperty("callWaveformVisualizer").objectReferenceValue = null;
        so.FindProperty("physicalSmartphone").objectReferenceValue = smartphone.GetComponent<VRInteractiveSmartphone>();
        so.FindProperty("smartphoneObject").objectReferenceValue = smartphone;

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
        so.FindProperty("flagHistoryText").objectReferenceValue = flagHistoryText;
        so.FindProperty("decisionConfirmOverlay").objectReferenceValue = confirmOverlay;
        so.FindProperty("confirmQuestionText").objectReferenceValue = confirmQuestionText;
        so.FindProperty("confirmYesButton").objectReferenceValue = confirmYesBtn.GetComponent<Button>();
        so.FindProperty("confirmCancelButton").objectReferenceValue = confirmCancelBtn.GetComponent<Button>();

        so.FindProperty("consequencePanel").objectReferenceValue = consequencePanel;
        so.FindProperty("consequenceTitleText").objectReferenceValue = conTitle;
        so.FindProperty("consequenceBodyText").objectReferenceValue = conBody;
        so.FindProperty("consequenceStatusIcon").objectReferenceValue = outcomePill.GetComponent<Image>();
        so.FindProperty("continueToDebriefButton").objectReferenceValue = continueBtn.GetComponent<Button>();

        so.FindProperty("debriefPanel").objectReferenceValue = debriefPanel;
        so.FindProperty("cardWeaponizedTrust").objectReferenceValue = card1;
        so.FindProperty("cardLinkDevicePurpose").objectReferenceValue = card2;
        so.FindProperty("cardTwoStepVerification").objectReferenceValue = card3;
        so.FindProperty("earnedXPText").objectReferenceValue = xpEarnedText;
        so.FindProperty("redFlagsFoundText").objectReferenceValue = redFlagsText;
        so.FindProperty("cardCounterText").objectReferenceValue = cardCounterText;
        so.FindProperty("debriefNextCardButton").objectReferenceValue = nextCardBtn.GetComponent<Button>();
        so.FindProperty("restartButton").objectReferenceValue = restartBtn.GetComponent<Button>();
        so.FindProperty("mainMenuButton").objectReferenceValue = menuBtn.GetComponent<Button>();

        so.ApplyModifiedProperties();

        // 9b. Wire Scenario Audio Sources & Clips onto Scenario3 Manager
        SetupScenarioAudio(manager, scenarioMgrObj);

        // 9c. Move every UI element to the ScreenUI layer (RT-only render). The display
        // mesh stays on Ignore Raycast (2) so it renders for the player but never
        // blocks UI raycasts (physics rays skip layer 2).
        SetLayerRecursive(canvasObj.transform, ScreenUILayer);
        GameObject displayObj = canvasObj.transform.Find("HoloScreen_Display")?.gameObject;
        if (displayObj != null) SetLayerRecursive(displayObj.transform, 2);

        // 9d. Exclude the ScreenUI layer from the player camera(s) so the flat canvas is
        // never rendered directly — only the curved display mesh shows it.
        Camera[] allCams = GameObject.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera cam in allCams)
        {
            if (cam.name == "ScreenRTCamera" || cam.targetTexture != null) continue;
            cam.cullingMask &= ~(1 << ScreenUILayer);
            Debug.Log($"[Scenario3Builder] Excluded ScreenUI layer from camera '{cam.name}'.");
        }

        // 9e. CRITICAL: the controller/hand Near-Far casters must include the ScreenUI
        // layer in their raycast mask, otherwise UI raycasts skip the big screen canvas
        // (its default mask is Default + UI + a reserved bit, excluding layer 8).
        var uiCasters = GameObject.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.Casters.CurveInteractionCaster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var caster in uiCasters)
        {
            SerializedObject casterSO = new SerializedObject(caster);
            SerializedProperty maskProp = casterSO.FindProperty("m_RaycastMask");
            if (maskProp == null) continue;
            int mask = maskProp.intValue;
            if ((mask & (1 << ScreenUILayer)) == 0)
            {
                maskProp.intValue = mask | (1 << ScreenUILayer);
                casterSO.ApplyModifiedProperties();
                Debug.Log($"[Scenario3Builder] Added ScreenUI layer to raycast mask of '{caster.transform.parent?.name}/{caster.name}'.");
            }
        }

        // 10. Save Prefab
        if (!Directory.Exists("Assets/Scenarios/Scenario3/Prefabs")) Directory.CreateDirectory("Assets/Scenarios/Scenario3/Prefabs");
        string prefabPath = "Assets/Scenarios/Scenario3/Prefabs/Scenario3_Canvas.prefab";
        PrefabUtility.SaveAsPrefabAsset(canvasObj, prefabPath);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[Scenario3Builder] Seamless Curved Holographic Screen built successfully!");
    }

    /// <summary>
    /// Builds the smooth cylindrical display mesh that shows the canvas RenderTexture.
    /// The mesh is a child of the canvas (inheriting its pose + scale), spans the full
    /// 1600px canvas width as a true arc of radius <see cref="ScreenCurveRadius"/>, and
    /// is on the DEFAULT layer so only the player camera renders it (the flat canvas
    /// itself is on the ScreenUI layer, seen only by the RT camera).
    /// </summary>
    private static GameObject BuildCurvedDisplayMesh(GameObject canvasObj, Canvas canvas)
    {
        GameObject meshObj = new GameObject("HoloScreen_Display");
        meshObj.transform.SetParent(canvasObj.transform, false);
        meshObj.transform.localPosition = Vector3.zero;
        meshObj.transform.localRotation = Quaternion.identity;
        // IMPORTANT: put the physical display mesh on the Ignore Raycast layer. The mesh
        // bows toward the viewer at the edges; a physical collider there would eat
        // controller raycast hits before they reach the flat UI canvas behind it,
        // breaking button clicks (simulator treats mesh hits as object, not World UI).
        meshObj.layer = 2; // Ignore Raycast

        MeshFilter mf = meshObj.AddComponent<MeshFilter>();
        MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();
        // the canvas pivot. A vertex at canvas x maps to angle theta = x / R.
        float scale = WorkstationCanvasScale;
        float R = ScreenCurveRadius / scale;               // radius in px (1336.19)
        float thetaMax = 800f / R;                          // half arc for 800px half-width (0.599 rad = 34.3°)

        int segments = 64;
        int verticalSegments = 8;
        int rings = verticalSegments + 1; // top / bottom + interior
        int vertexCount = (segments + 1) * rings;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[segments * verticalSegments * 6];

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float theta = Mathf.Lerp(-thetaMax, thetaMax, t);
            float x = R * Mathf.Sin(theta);
            float z = R * (Mathf.Cos(theta) - 1f); // axis at z = -R, so z=0 at center, bows toward viewer (-z)

            // Canvas local space: x in [-800,800], y in [-450,450], z toward viewer is -z.
            // The canvas front faces the player along -Z (uGUI world-space convention with
            // yaw 270: the readable side faces the player).
            for (int j = 0; j < rings; j++)
            {
                float v = (float)j / verticalSegments;
                float y = Mathf.Lerp(-450f, 450f, v);
                int vi = i * rings + j;
                vertices[vi] = new Vector3(x, y, z);
                uvs[vi] = new Vector2(t, v);
            }

            if (i < segments)
            {
                for (int j = 0; j < verticalSegments; j++)
                {
                    int ti = (i * verticalSegments + j) * 6;
                    int v00 = i * rings + j;
                    int v01 = i * rings + j + 1;
                    int v10 = (i + 1) * rings + j;
                    int v11 = (i + 1) * rings + j + 1;
                    triangles[ti] = v00;
                    triangles[ti + 1] = v01;
                    triangles[ti + 2] = v10;
                    triangles[ti + 3] = v10;
                    triangles[ti + 4] = v01;
                    triangles[ti + 5] = v11;
                }
            }
        }

        Mesh mesh = new Mesh { name = "HoloScreen_Display_Mesh" };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;

        // Physical surface so controller rays/laser lines land on the curved screen
        // (the XRUIInputModule raycasts the canvas analytically; the collider is for
        // line visuals and 3D occlusion only).
        MeshCollider mc = meshObj.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;

        // Unlit screen material; the RenderTexture is assigned at runtime by
        // CurvedScreenRenderer (URP/Unlit uses _BaseMap).
        string matPath = "Assets/_Project/Materials/Mat_HoloScreen.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) unlit = Shader.Find("Unlit/Texture");
            mat = new Material(unlit);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetFloat("_Cull", 0f); // double-sided — the player sees the front (-z) face
        mr.sharedMaterial = mat;

        CurvedScreenRenderer renderer = meshObj.AddComponent<CurvedScreenRenderer>();
        var so = new SerializedObject(renderer);
        so.FindProperty("targetCanvas").objectReferenceValue = canvas;
        so.FindProperty("curveRadiusWorld").floatValue = ScreenCurveRadius;
        so.ApplyModifiedProperties();

        return meshObj;
    }

    /// <summary>
    /// Builds the physical, grabbable smartphone prop on a side desk next to the
    /// player. It handles the out-of-band call verification (rings, hold-to-ear
    /// answer) and the QR camera scan, keeping these interactions OUT of the
    /// Telegram window on the big screen.
    /// </summary>
    private static GameObject BuildPhysicalSmartphone(GameObject sceneRoot)
    {
        // ── Side table (proper elevated platform with legs) ──────────────────────
        GameObject oldDesk = GameObject.Find("Phone_SideTable");
        if (oldDesk != null) UnityEngine.Object.DestroyImmediate(oldDesk);
        GameObject oldDeskLegacy = GameObject.Find("Phone_SideDesk");
        if (oldDeskLegacy != null) UnityEngine.Object.DestroyImmediate(oldDeskLegacy);
        GameObject oldPhone = GameObject.Find("Physical_Smartphone");
        if (oldPhone != null) UnityEngine.Object.DestroyImmediate(oldPhone);

        Vector3 right = Quaternion.Euler(0f, WorkstationYaw, 0f) * Vector3.right; // player right = +Z (yaw 270)
        Vector3 tableAnchor = WorkstationAnchor + right * 0.60f; // floor position under the table
        Quaternion tableRot = Quaternion.Euler(0f, WorkstationYaw, 0f);

        const float tableHeight = 0.92f;   // comfortable standing desk height
        const float tableTopThick = 0.04f;
        const float tableWidth = 0.52f;    // along player's right axis
        const float tableDepth = 0.40f;    // along view direction

        Material matDesk = GetOrCreateMaterial("Mat_PhoneTable", new Color(0.30f, 0.33f, 0.38f), 0.35f);
        Material matLeg = GetOrCreateMaterial("Mat_PhoneTableLeg", new Color(0.20f, 0.22f, 0.26f), 0.30f);

        GameObject tableRoot = new GameObject("Phone_SideTable");
        tableRoot.transform.SetParent(sceneRoot.transform, false);
        tableRoot.transform.SetPositionAndRotation(tableAnchor, tableRot);

        // Table top
        GameObject top = CreateBox("TableTop", tableRoot.transform, new Vector3(0f, tableHeight - tableTopThick * 0.5f, 0f), new Vector3(tableWidth, tableTopThick, tableDepth), matDesk, true);

        // Four legs
        float legHeight = tableHeight - tableTopThick;
        float lx = tableWidth * 0.5f - 0.035f;
        float lz = tableDepth * 0.5f - 0.035f;
        CreateBox("Leg_FL", tableRoot.transform, new Vector3(-lx, legHeight * 0.5f, -lz), new Vector3(0.045f, legHeight, 0.045f), matLeg, true);
        CreateBox("Leg_FR", tableRoot.transform, new Vector3(lx, legHeight * 0.5f, -lz), new Vector3(0.045f, legHeight, 0.045f), matLeg, true);
        CreateBox("Leg_BL", tableRoot.transform, new Vector3(-lx, legHeight * 0.5f, lz), new Vector3(0.045f, legHeight, 0.045f), matLeg, true);
        CreateBox("Leg_BR", tableRoot.transform, new Vector3(lx, legHeight * 0.5f, lz), new Vector3(0.045f, legHeight, 0.045f), matLeg, true);

        // ── Phone body ───────────────────────────────────────────────────────────
        Material matPhone = GetOrCreateMaterial("Mat_PhoneBody", new Color(0.10f, 0.11f, 0.13f), 0.6f);
        Material matScreenOff = GetOrCreateMaterial("Mat_PhoneScreenOff", new Color(0.02f, 0.03f, 0.05f), 0.8f);

        GameObject phoneRoot = new GameObject("Physical_Smartphone");
        phoneRoot.transform.SetParent(sceneRoot.transform, false);
        phoneRoot.transform.position = tableRoot.transform.TransformPoint(new Vector3(0.03f, tableHeight + 0.012f, 0.05f));
        phoneRoot.transform.rotation = Quaternion.Euler(0f, WorkstationYaw + 12f, 0f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(phoneRoot.transform, false);
        body.transform.localScale = new Vector3(0.078f, 0.0095f, 0.165f);
        body.GetComponent<Renderer>().material = matPhone;

        // Grab components
        Rigidbody rb = phoneRoot.AddComponent<Rigidbody>();
        rb.mass = 0.18f;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab = phoneRoot.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;
        grab.trackPosition = true;
        grab.trackRotation = true;

        // Screen glass
        GameObject screenGlass = GameObject.CreatePrimitive(PrimitiveType.Quad);
        screenGlass.name = "ScreenGlass";
        screenGlass.transform.SetParent(phoneRoot.transform, false);
        screenGlass.transform.localPosition = new Vector3(0f, 0.0049f, 0f);
        screenGlass.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        screenGlass.transform.localScale = new Vector3(0.071f, 0.152f, 1f);
        screenGlass.GetComponent<Renderer>().material = matScreenOff;
        UnityEngine.Object.DestroyImmediate(screenGlass.GetComponent<Collider>());

        // ── Screen canvas (light phone UI) ───────────────────────────────────────
        GameObject canvasObj = new GameObject("PhoneScreenCanvas");
        canvasObj.transform.SetParent(phoneRoot.transform, false);
        canvasObj.transform.localPosition = new Vector3(0f, 0.0052f, 0f);
        canvasObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        canvasObj.transform.localScale = Vector3.one * 0.0002f;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(360f, 760f);
        canvasObj.AddComponent<GraphicRaycaster>();
        Type trackedRaycasterType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (trackedRaycasterType != null) canvasObj.AddComponent(trackedRaycasterType);

        Color phoneBg = new Color(0.937f, 0.957f, 0.976f);      // #EFF4F9
        Color phoneCard = Color.white;
        Color phoneInk = new Color(0.10f, 0.12f, 0.16f);
        Color phoneSubtle = new Color(0.45f, 0.50f, 0.56f);
        Color phoneAccent = new Color(0.200f, 0.565f, 0.925f);
        Color answerGreen = new Color(0.180f, 0.655f, 0.345f);
        Color declineRed = new Color(0.851f, 0.267f, 0.216f);

        // ── Panel 0: Lock screen ────────────────────────────────────────────────
        GameObject lockPanel = CreateUIObject("LockScreenPanel", canvasObj.transform);
        SetRectTransform(lockPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        lockPanel.AddComponent<Image>().color = new Color(0.851f, 0.918f, 0.980f);

        TextMeshProUGUI clockText = CreateTMPText("ClockText", lockPanel.transform, "8:42", 84, TextAlignmentOptions.Center, phoneInk);
        SetRectTransform(clockText.gameObject, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.88f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI dateText = CreateTMPText("DateText", lockPanel.transform, "Tuesday, 10 September", 22, TextAlignmentOptions.Center, phoneSubtle);
        SetRectTransform(dateText.gameObject, new Vector2(0.05f, 0.66f), new Vector2(0.95f, 0.72f), Vector2.zero, Vector2.zero);

        GameObject notifCard = CreateGlassCard("NotifCard", lockPanel.transform, phoneCard, -2f);
        SetRectTransform(notifCard, new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.58f), Vector2.zero, Vector2.zero);
        Image notifIcon = CreateIcon("NotifIcon", notifCard.transform, ProceduralIconSet.Icon.SendPlane, phoneAccent, 30f);
        SetRectTransform(notifIcon.gameObject, new Vector2(0.05f, 0.58f), new Vector2(0.16f, 0.88f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI notifName = CreateTMPText("NotifName", notifCard.transform, "<b>Telegram</b>", 20, TextAlignmentOptions.MidlineLeft, phoneInk);
        SetRectTransform(notifName.gameObject, new Vector2(0.19f, 0.60f), new Vector2(0.72f, 0.90f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI notifTime = CreateTMPText("NotifTime", notifCard.transform, "now", 16, TextAlignmentOptions.MidlineRight, phoneSubtle);
        SetRectTransform(notifTime.gameObject, new Vector2(0.72f, 0.60f), new Vector2(0.95f, 0.90f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI notifBody = CreateTMPText("NotifBody", notifCard.transform, "Sophea Kem: Hey! Can you please help me with our NextGen survey...", 16, TextAlignmentOptions.TopLeft, phoneSubtle);
        SetRectTransform(notifBody.gameObject, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.56f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI lockHint = CreateTMPText("LockHint", lockPanel.transform, "Pick up the phone to answer calls", 17, TextAlignmentOptions.Center, phoneSubtle);
        SetRectTransform(lockHint.gameObject, new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.33f), Vector2.zero, Vector2.zero);

        // ── Panel 1a: Call contact (player-initiated out-of-band verification) ──
        GameObject readyToCallPanel = CreateUIObject("ReadyToCallPanel", canvasObj.transform);
        SetRectTransform(readyToCallPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        readyToCallPanel.AddComponent<Image>().color = new Color(0.910f, 0.945f, 0.980f);
        readyToCallPanel.SetActive(false);

        TextMeshProUGUI readyTitle = CreateTMPText("ReadyTitle", readyToCallPanel.transform, "<b>Verify this request</b>", 24, TextAlignmentOptions.Center, phoneInk);
        SetRectTransform(readyTitle.gameObject, new Vector2(0.08f, 0.83f), new Vector2(0.92f, 0.90f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI readySub = CreateTMPText("ReadySub", readyToCallPanel.transform, "Urgent requests from friends should be confirmed\nby a voice call before you act.", 15, TextAlignmentOptions.Center, phoneSubtle);
        SetRectTransform(readySub.gameObject, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.82f), Vector2.zero, Vector2.zero);

        GameObject readyAvatar = CreateUIObject("ReadyAvatar", readyToCallPanel.transform);
        SetRectTransform(readyAvatar, new Vector2(0.34f, 0.52f), new Vector2(0.66f, 0.68f), Vector2.zero, Vector2.zero);
        Image readyAvatarImg = readyAvatar.AddComponent<Image>();
        readyAvatarImg.sprite = ProceduralIconSet.Get(ProceduralIconSet.Icon.Circle);
        readyAvatarImg.color = new Color(0.94f, 0.49f, 0.23f);
        readyAvatarImg.preserveAspect = true;
        TextMeshProUGUI readyAvatarTxt = CreateTMPText("ReadyAvatarTxt", readyAvatar.transform, "<b>S</b>", 40, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(readyAvatarTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI readyName = CreateTMPText("ReadyName", readyToCallPanel.transform, "<b>Sophea Kem</b>\n<size=14>CADT · NextGen DMIL</size>", 22, TextAlignmentOptions.Center, phoneInk);
        SetRectTransform(readyName.gameObject, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.51f), Vector2.zero, Vector2.zero);

        GameObject callSopheaBtn = CreateButton("CallSopheaBtn", readyToCallPanel.transform, "Call Sophea", answerGreen, 20);
        SetRectTransform(callSopheaBtn, new Vector2(0.12f, 0.24f), new Vector2(0.88f, 0.34f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI readyHint = CreateTMPText("ReadyHint", readyToCallPanel.transform, "Hold the phone to your ear after calling", 14, TextAlignmentOptions.Center, phoneSubtle);
        SetRectTransform(readyHint.gameObject, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.21f), Vector2.zero, Vector2.zero);
        Image callIconBig = CreateIcon("CallIconBig", readyToCallPanel.transform, ProceduralIconSet.Icon.Phone, new Color(0.180f, 0.655f, 0.345f), 52f);
        SetRectTransform(callIconBig.gameObject, new Vector2(0.44f, 0.06f), new Vector2(0.56f, 0.14f), Vector2.zero, Vector2.zero);

        // ── Panel 1: Incoming call ──────────────────────────────────────────────
        GameObject incomingPanel = CreateUIObject("IncomingCallPanel", canvasObj.transform);
        SetRectTransform(incomingPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        incomingPanel.AddComponent<Image>().color = new Color(0.910f, 0.945f, 0.980f);
        incomingPanel.SetActive(false);

        GameObject callAvatar = CreateUIObject("CallAvatar", incomingPanel.transform);
        SetRectTransform(callAvatar, new Vector2(0.33f, 0.60f), new Vector2(0.67f, 0.76f), Vector2.zero, Vector2.zero);
        Image callAvatarImg = callAvatar.AddComponent<Image>();
        callAvatarImg.sprite = ProceduralIconSet.Get(ProceduralIconSet.Icon.Circle);
        callAvatarImg.color = new Color(0.94f, 0.49f, 0.23f);
        callAvatarImg.preserveAspect = true;
        TextMeshProUGUI callAvatarTxt = CreateTMPText("CallAvatarTxt", callAvatar.transform, "<b>S</b>", 44, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(callAvatarTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI callerName = CreateTMPText("CallerName", incomingPanel.transform, "<b>Sophea Kem</b>", 30, TextAlignmentOptions.Center, phoneInk);
        SetRectTransform(callerName.gameObject, new Vector2(0.08f, 0.50f), new Vector2(0.92f, 0.58f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI callStatusText = CreateTMPText("CallStatusText", incomingPanel.transform, "Incoming Telegram Voice Call...", 19, TextAlignmentOptions.Center, phoneSubtle);
        SetRectTransform(callStatusText.gameObject, new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.50f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI callHintText = CreateTMPText("CallHintText", incomingPanel.transform, "Hold the phone to your ear to answer", 16, TextAlignmentOptions.Center, phoneSubtle);
        SetRectTransform(callHintText.gameObject, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.34f), Vector2.zero, Vector2.zero);

        GameObject declineBtn = CreateRoundButton("DeclineBtn", incomingPanel.transform, declineRed, ProceduralIconSet.Icon.Phone, true);
        SetRectTransform(declineBtn, new Vector2(0.16f, 0.10f), new Vector2(0.36f, 0.24f), Vector2.zero, Vector2.zero);
        GameObject answerBtn = CreateRoundButton("AnswerBtn", incomingPanel.transform, answerGreen, ProceduralIconSet.Icon.Phone, false);
        SetRectTransform(answerBtn, new Vector2(0.64f, 0.10f), new Vector2(0.84f, 0.24f), Vector2.zero, Vector2.zero);

        // ── Panel 2: In call ────────────────────────────────────────────────────
        GameObject inCallPanel = CreateUIObject("InCallPanel", canvasObj.transform);
        SetRectTransform(inCallPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        inCallPanel.AddComponent<Image>().color = new Color(0.878f, 0.925f, 0.965f);
        inCallPanel.SetActive(false);

        GameObject inCallAvatar = CreateUIObject("InCallAvatar", inCallPanel.transform);
        SetRectTransform(inCallAvatar, new Vector2(0.36f, 0.68f), new Vector2(0.64f, 0.80f), Vector2.zero, Vector2.zero);
        Image inCallAvatarImg = inCallAvatar.AddComponent<Image>();
        inCallAvatarImg.sprite = ProceduralIconSet.Get(ProceduralIconSet.Icon.Circle);
        inCallAvatarImg.color = new Color(0.94f, 0.49f, 0.23f);
        inCallAvatarImg.preserveAspect = true;
        TextMeshProUGUI inCallAvatarTxt = CreateTMPText("InCallAvatarTxt", inCallAvatar.transform, "<b>S</b>", 36, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(inCallAvatarTxt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI inCallName = CreateTMPText("InCallName", inCallPanel.transform, "<b>Sophea Kem</b>", 26, TextAlignmentOptions.Center, phoneInk);
        SetRectTransform(inCallName.gameObject, new Vector2(0.08f, 0.61f), new Vector2(0.92f, 0.67f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI inCallStatus = CreateTMPText("InCallStatus", inCallPanel.transform, "connected", 17, TextAlignmentOptions.Center, new Color(0.180f, 0.655f, 0.345f));
        SetRectTransform(inCallStatus.gameObject, new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.60f), Vector2.zero, Vector2.zero);

        GameObject dialogueCard = CreateGlassCard("DialogueCard", inCallPanel.transform, phoneCard, -2f);
        SetRectTransform(dialogueCard, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.53f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI inCallDialogueText = CreateTMPText("InCallDialogueText", dialogueCard.transform, "", 17, TextAlignmentOptions.TopLeft, phoneInk);
        SetRectTransform(inCallDialogueText.gameObject, new Vector2(0.07f, 0.10f), new Vector2(0.93f, 0.90f), Vector2.zero, Vector2.zero);

        GameObject endCallBtn = CreateRoundButton("EndCallBtn", inCallPanel.transform, declineRed, ProceduralIconSet.Icon.Phone, true);
        SetRectTransform(endCallBtn, new Vector2(0.40f, 0.10f), new Vector2(0.60f, 0.22f), Vector2.zero, Vector2.zero);

        // ── Panel 3: QR scanner ─────────────────────────────────────────────────
        GameObject scannerPanel = CreateUIObject("QRScannerPanel", canvasObj.transform);
        SetRectTransform(scannerPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        scannerPanel.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.10f);
        scannerPanel.SetActive(false);

        GameObject reticleObj = CreateUIObject("Reticle", scannerPanel.transform);
        SetRectTransform(reticleObj, new Vector2(0.22f, 0.42f), new Vector2(0.78f, 0.82f), Vector2.zero, Vector2.zero);
        Image reticleImg = reticleObj.AddComponent<Image>();
        reticleImg.sprite = scannerReticleSprite;
        reticleImg.color = new Color(0.2f, 0.8f, 1f, 0.5f);
        reticleImg.preserveAspect = true;
        TextMeshProUGUI scannerHint = CreateTMPText("ScannerHint", scannerPanel.transform, "Point the phone — or simply look\nat the QR code — to scan", 18, TextAlignmentOptions.Center, new Color(0.75f, 0.85f, 0.95f));
        SetRectTransform(scannerHint.gameObject, new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.38f), Vector2.zero, Vector2.zero);

        // ── Panel 4: QR authorization prompt ────────────────────────────────────
        GameObject qrPromptPanel = CreateUIObject("QRPromptPanel", canvasObj.transform);
        SetRectTransform(qrPromptPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        qrPromptPanel.AddComponent<Image>().color = phoneBg;
        qrPromptPanel.SetActive(false);

        TextMeshProUGUI promptWarningText = CreateTMPText("PromptWarningText", qrPromptPanel.transform, "", 16, TextAlignmentOptions.TopLeft, phoneInk);
        SetRectTransform(promptWarningText.gameObject, new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);

        GameObject authorizeBtn = CreateButton("AuthorizeBtn", qrPromptPanel.transform, "Authorize", declineRed, 20);
        SetRectTransform(authorizeBtn, new Vector2(0.08f, 0.10f), new Vector2(0.48f, 0.24f), Vector2.zero, Vector2.zero);
        GameObject rejectLoginBtn = CreateButton("RejectLoginBtn", qrPromptPanel.transform, "Reject", answerGreen, 20);
        SetRectTransform(rejectLoginBtn, new Vector2(0.52f, 0.10f), new Vector2(0.92f, 0.24f), Vector2.zero, Vector2.zero);

        // ── Panel 5: Result ─────────────────────────────────────────────────────
        GameObject resultPanel = CreateUIObject("ResultPanel", canvasObj.transform);
        SetRectTransform(resultPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        resultPanel.AddComponent<Image>().color = phoneBg;
        resultPanel.SetActive(false);

        Image resultIcon = CreateIcon("ResultIcon", resultPanel.transform, ProceduralIconSet.Icon.Shield, phoneAccent, 80f);
        SetRectTransform(resultIcon.gameObject, new Vector2(0.40f, 0.66f), new Vector2(0.60f, 0.80f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI resultTitleText = CreateTMPText("ResultTitleText", resultPanel.transform, "", 26, TextAlignmentOptions.Center, phoneInk);
        SetRectTransform(resultTitleText.gameObject, new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.64f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI resultBodyText = CreateTMPText("ResultBodyText", resultPanel.transform, "", 18, TextAlignmentOptions.Top, phoneSubtle);
        SetRectTransform(resultBodyText.gameObject, new Vector2(0.08f, 0.26f), new Vector2(0.92f, 0.50f), Vector2.zero, Vector2.zero);

        // ── Component wiring ────────────────────────────────────────────────────
        AudioSource phoneAudio = phoneRoot.AddComponent<AudioSource>();
        phoneAudio.playOnAwake = false;
        phoneAudio.spatialBlend = 1f;
        phoneAudio.minDistance = 0.15f;
        phoneAudio.maxDistance = 4f;
        phoneAudio.rolloffMode = AudioRolloffMode.Linear;

        VRInteractiveSmartphone phone = phoneRoot.AddComponent<VRInteractiveSmartphone>();
        SerializedObject so = new SerializedObject(phone);
        so.FindProperty("lockScreenPanel").objectReferenceValue = lockPanel;
        so.FindProperty("readyToCallPanel").objectReferenceValue = readyToCallPanel;
        so.FindProperty("incomingCallPanel").objectReferenceValue = incomingPanel;
        so.FindProperty("inCallPanel").objectReferenceValue = inCallPanel;
        so.FindProperty("qrScannerPanel").objectReferenceValue = scannerPanel;
        so.FindProperty("qrPromptPanel").objectReferenceValue = qrPromptPanel;
        so.FindProperty("resultPanel").objectReferenceValue = resultPanel;
        so.FindProperty("clockText").objectReferenceValue = clockText;
        so.FindProperty("callStatusText").objectReferenceValue = callStatusText;
        so.FindProperty("inCallDialogueText").objectReferenceValue = inCallDialogueText;
        so.FindProperty("promptWarningText").objectReferenceValue = promptWarningText;
        so.FindProperty("resultTitleText").objectReferenceValue = resultTitleText;
        so.FindProperty("resultBodyText").objectReferenceValue = resultBodyText;
        so.FindProperty("scannerReticleImage").objectReferenceValue = reticleImg;
        so.FindProperty("answerCallButton").objectReferenceValue = answerBtn.GetComponent<Button>();
        so.FindProperty("rejectCallButton").objectReferenceValue = declineBtn.GetComponent<Button>();
        so.FindProperty("endCallButton").objectReferenceValue = endCallBtn.GetComponent<Button>();
        so.FindProperty("callSopheaButton").objectReferenceValue = callSopheaBtn.GetComponent<Button>();
        so.FindProperty("notificationClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Phone_Notification.wav");
        so.FindProperty("authorizeLoginButton").objectReferenceValue = authorizeBtn.GetComponent<Button>();
        so.FindProperty("rejectLoginButton").objectReferenceValue = rejectLoginBtn.GetComponent<Button>();
        so.FindProperty("phoneAudioSource").objectReferenceValue = phoneAudio;
        so.FindProperty("ringtoneClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Phone_Ringtone.wav");
        so.FindProperty("scanBeepClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/VRTemplateAssets/Audio/Button_22_click.wav");
        so.FindProperty("successClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Success_Fanfare.wav");
        so.FindProperty("alertClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Security_Warning.wav");
        so.FindProperty("maxScanDistance").floatValue = 4f;
        so.FindProperty("scanAimAngleThreshold").floatValue = 70f;
        so.ApplyModifiedProperties();

        // ── Floating phone screen (pops out vertically when the phone is active) ──
        // Anchor sits to the player's right at eye level, tilted to face them, like
        // a floating vertical phone screen next to the workstation monitor.
        Vector3 floatRight = Quaternion.Euler(0f, WorkstationYaw, 0f) * Vector3.right;
        Vector3 floatForward = Quaternion.Euler(0f, WorkstationYaw, 0f) * Vector3.forward;

        GameObject oldFloat = GameObject.Find("Phone_FloatAnchor");
        if (oldFloat != null) UnityEngine.Object.DestroyImmediate(oldFloat);

        GameObject floatAnchor = new GameObject("Phone_FloatAnchor");
        floatAnchor.transform.SetParent(sceneRoot.transform, false);
        Vector3 anchorPos = WorkstationAnchor + floatRight * 0.42f + floatForward * 0.30f + new Vector3(0f, 1.24f, 0f);
        floatAnchor.transform.position = anchorPos;
        // Face the player (canvas readable side is -Z, so +Z faces away from the player)
        Vector3 toPlayer = (WorkstationAnchor + new Vector3(0f, 1.30f, 0f)) - anchorPos;
        floatAnchor.transform.rotation = Quaternion.LookRotation(-toPlayer.normalized, Vector3.up);

        // Backing slab (phone-like dark frame, slightly larger than the 360x760 canvas)
        Material matFloatBacking = GetOrCreateMaterial("Mat_PhoneFloatBacking", new Color(0.07f, 0.08f, 0.10f), 0.6f);
        GameObject backing = CreateBox("FloatBacking", floatAnchor.transform, new Vector3(0f, 0f, 0.012f), new Vector3(0.335f, 0.700f, 0.018f), matFloatBacking, false);
        backing.SetActive(false);

        PhoneScreenFloating floating = phoneRoot.AddComponent<PhoneScreenFloating>();
        SerializedObject soFloat = new SerializedObject(floating);
        soFloat.FindProperty("phone").objectReferenceValue = phone;
        soFloat.FindProperty("phoneCanvas").objectReferenceValue = canvasRect;
        soFloat.FindProperty("floatingAnchor").objectReferenceValue = floatAnchor.transform;
        soFloat.FindProperty("floatingBacking").objectReferenceValue = backing;
        soFloat.FindProperty("floatingScale").floatValue = 0.0009f;
        soFloat.ApplyModifiedProperties();

        return phoneRoot;
    }

    /// <summary>Circular phone button with a centered icon.</summary>
    private static GameObject CreateRoundButton(string name, Transform parent, Color color, ProceduralIconSet.Icon icon, bool flipIcon)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        Image img = btnObj.AddComponent<Image>();
        img.sprite = ProceduralIconSet.Get(ProceduralIconSet.Icon.Circle);
        img.color = color;
        img.preserveAspect = true;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        btn.colors = cb;
        btn.targetGraphic = img;

        Image iconImg = CreateIcon("Icon", btnObj.transform, icon, Color.white, 30f);
        SetRectTransform(iconImg.gameObject, new Vector2(0.28f, 0.28f), new Vector2(0.72f, 0.72f), Vector2.zero, Vector2.zero);
        if (flipIcon)
            iconImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 135f); // declined-call phone tilt

        return btnObj;
    }

    private static void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
        {
            SetLayerRecursive(t.GetChild(i), layer);
        }
    }

    private static CurvedSections CreateCurvedSectionRoots(string phaseName, Transform parent)
    {
        GameObject phaseRoot = CreateUIObject(phaseName, parent);
        SetRectTransform(phaseRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // The canvas is FLAT (1600x900). Curvature is applied only by the curved display
        // mesh + RenderTexture pipeline (see BuildCurvedDisplayMesh), so the three logical
        // columns simply sit side by side with zero seam gap and no per-section angles.
        // Center column: 704px wide, centered. Side columns: 448px each.
        float centerHalf = 352f;   // 704 / 2
        float sideHalf = 224f;     // 448 / 2
        float sideCenter = centerHalf + sideHalf; // 576

        GameObject center = CreateUIObject("CenterSection", phaseRoot.transform);
        RectTransform rtCenter = center.GetComponent<RectTransform>();
        rtCenter.sizeDelta = new Vector2(704f, 900f);
        rtCenter.anchoredPosition3D = Vector3.zero;
        rtCenter.localRotation = Quaternion.identity;

        GameObject left = CreateUIObject("LeftSection", phaseRoot.transform);
        RectTransform rtLeft = left.GetComponent<RectTransform>();
        rtLeft.sizeDelta = new Vector2(448f, 900f);
        rtLeft.anchoredPosition3D = new Vector3(-sideCenter, 0f, 0f);
        rtLeft.localRotation = Quaternion.identity;

        GameObject right = CreateUIObject("RightSection", phaseRoot.transform);
        RectTransform rtRight = right.GetComponent<RectTransform>();
        rtRight.sizeDelta = new Vector2(448f, 900f);
        rtRight.anchoredPosition3D = new Vector3(sideCenter, 0f, 0f);
        rtRight.localRotation = Quaternion.identity;

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

        AudioClip hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/VRTemplateAssets/Audio/Button_14_hover.wav");
        AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/VRTemplateAssets/Audio/Button_22_click.wav");
        AudioClip alertClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Security_Warning.wav");
        AudioClip victoryClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Success_Fanfare.wav");

        SerializedObject so = new SerializedObject(vrHandler);
        so.FindProperty("audioSource").objectReferenceValue = vrAudio;
        if (hoverClip != null) so.FindProperty("hoverSound").objectReferenceValue = hoverClip;
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
        string avatarChar, Color avatarColor, string chatTitle, string snippet, string timeStr, bool hasUnread, bool isActive = false)
    {
        // Telegram Desktop light theme: selected chat = blue fill, others white
        Color bgColor = isActive ? new Color(0.200f, 0.565f, 0.925f) : new Color(1f, 1f, 1f, 0f);
        GameObject itemObj = CreateGlassCard(name, parent, bgColor, -4f);
        SetRectTransform(itemObj, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

        Button btn = itemObj.AddComponent<Button>();
        btn.targetGraphic = itemObj.GetComponent<Image>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.94f, 0.96f, 0.98f);
        cb.pressedColor = new Color(0.88f, 0.92f, 0.96f);
        btn.colors = cb;
        itemObj.AddComponent<VRUIHoverEffect>();

        GameObject avObj = CreateUIObject("Avatar", itemObj.transform);
        SetRectTransform(avObj, new Vector2(0.04f, 0.15f), new Vector2(0.20f, 0.85f), Vector2.zero, Vector2.zero);
        Image avImg = avObj.AddComponent<Image>();
        avImg.sprite = ProceduralIconSet.Get(ProceduralIconSet.Icon.Circle);
        avImg.color = avatarColor;
        avImg.preserveAspect = true;
        TextMeshProUGUI avChar = CreateTMPText("Char", avObj.transform, $"<b>{avatarChar}</b>", 16, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(avChar.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Color titleColor = isActive ? Color.white : new Color(0.08f, 0.08f, 0.08f);
        Color snippetColor = isActive ? new Color(1f, 1f, 1f, 0.85f) : new Color(0.60f, 0.60f, 0.60f);
        Color timeColor = isActive ? new Color(1f, 1f, 1f, 0.85f) : new Color(0.60f, 0.60f, 0.60f);
        Color unreadBg = isActive ? Color.white : new Color(0.200f, 0.565f, 0.925f);
        Color unreadTxtColor = isActive ? new Color(0.200f, 0.565f, 0.925f) : Color.white;

        TextMeshProUGUI titleTxt = CreateTMPText("Title", itemObj.transform, $"<b>{chatTitle}</b>", 15, TextAlignmentOptions.TopLeft, titleColor);
        SetRectTransform(titleTxt.gameObject, new Vector2(0.24f, 0.52f), new Vector2(0.75f, 0.95f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI snipTxt = CreateTMPText("Snippet", itemObj.transform, snippet, 13, TextAlignmentOptions.TopLeft, snippetColor);
        SetRectTransform(snipTxt.gameObject, new Vector2(0.24f, 0.10f), new Vector2(0.85f, 0.50f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI timeTxt = CreateTMPText("Time", itemObj.transform, timeStr, 12, TextAlignmentOptions.TopRight, timeColor);
        SetRectTransform(timeTxt.gameObject, new Vector2(0.75f, 0.55f), new Vector2(0.96f, 0.95f), Vector2.zero, Vector2.zero);

        if (hasUnread)
        {
            GameObject badge = CreatePill("UnreadBadge", itemObj.transform, unreadBg, -3f);
            SetRectTransform(badge, new Vector2(0.85f, 0.15f), new Vector2(0.96f, 0.45f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI badgeTxt = CreateTMPText("1", badge.transform, "<b>1</b>", 12, TextAlignmentOptions.Center, unreadTxtColor);
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

    /// <summary>Borderless rounded card — used for message bubbles (no outline).</summary>
    private static Sprite CreateFlatCardSprite()
    {
        int size = 64;
        int radius = 14;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = 0f, dy = 0f;
                if (x < radius) dx = radius - x;
                else if (x >= size - radius) dx = x - (size - radius - 1);
                if (y < radius) dy = radius - y;
                else if (y >= size - radius) dy = y - (size - radius - 1);
                bool outside = (dx * dx + dy * dy) > (radius * radius);
                tex.SetPixel(x, y, outside ? Color.clear : Color.white);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private static Sprite CreateGlassCardSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        // Light theme card: white fill with a soft gray border (tint multiplies this).
        Color border = new Color(0.855f, 0.878f, 0.906f, 1f);
        Color fill = new Color(1f, 1f, 1f, 1f);

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

    private static Image CreateIcon(string name, Transform parent, ProceduralIconSet.Icon icon, Color color, float size)
    {
        GameObject obj = CreateUIObject(name, parent);
        Image img = obj.AddComponent<Image>();
        img.sprite = ProceduralIconSet.Get(icon);
        img.type = Image.Type.Simple;
        img.color = color;
        img.preserveAspect = true;
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        return img;
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

        // Add tactile VR hover & haptic feedback to all buttons
        btnObj.AddComponent<VRUIHoverEffect>();

        TextMeshProUGUI txt = CreateTMPText("BtnText", btnObj.transform, text, fontSize, TextAlignmentOptions.Center, Color.white);
        SetRectTransform(txt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        if (zOffset != 0f)
        {
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition3D = new Vector3(rt.anchoredPosition.x, rt.anchoredPosition.y, zOffset);
        }
        return btnObj;
    }

    /// <summary>Button with a vector icon at the left and a left-aligned label.</summary>
    private static GameObject CreateButtonWithIcon(string name, Transform parent, string text, ProceduralIconSet.Icon icon, Color btnColor, float fontSize, float zOffset, Color accentColor, bool whiteText)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        Image img = btnObj.AddComponent<Image>();
        img.sprite = glowingBtnSprite;
        img.type = Image.Type.Sliced;
        img.color = btnColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = cb;
        btnObj.AddComponent<VRUIHoverEffect>();

        Image iconImg = CreateIcon("BtnIcon", btnObj.transform, icon, accentColor, fontSize + 3f);
        SetRectTransform(iconImg.gameObject, new Vector2(0.05f, 0.20f), new Vector2(0.13f, 0.80f), Vector2.zero, Vector2.zero);

        Color textColor = whiteText ? Color.white : new Color(0.13f, 0.15f, 0.18f);
        TextMeshProUGUI txt = CreateTMPText("BtnText", btnObj.transform, text, fontSize, TextAlignmentOptions.MidlineLeft, textColor);
        SetRectTransform(txt.gameObject, new Vector2(0.17f, 0f), new Vector2(0.97f, 1f), Vector2.zero, Vector2.zero);

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
