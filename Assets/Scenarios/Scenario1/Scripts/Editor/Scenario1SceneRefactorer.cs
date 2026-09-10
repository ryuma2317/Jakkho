using System;
using CADT.VR.Cybersecurity;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CADT.VR.Editor
{
    public static class Scenario1SceneRefactorer
    {
        [MenuItem("CADT/Refactor Scenario 1 Workstation", false, 2)]
        public static void RefactorScenario1Workstation()
        {
            Debug.Log("<color=#00F0FF><b>[CADT VR]</b> Refactoring Workstation to Single Centered Telegram Monitor & Phase 3 Dilemma...</color>");

            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = UnityEngine.Object.FindFirstObjectByType<Camera>();

            GameObject envRoot = GameObject.Find("CADT_Office_Environment");
            if (envRoot == null)
            {
                Debug.LogError("CADT_Office_Environment root not found in scene!");
                return;
            }

            Transform workstation = envRoot.transform.Find("Workstation_Setup");
            if (workstation == null)
            {
                Debug.LogError("Workstation_Setup not found!");
                return;
            }

            // ----------------------------------------------------
            // 1. ENVIRONMENT LAYOUT: DESTROY LEFT MONITOR
            // ----------------------------------------------------
            Transform leftMon = workstation.Find("Left_Monitor_Setup");
            if (leftMon != null)
            {
                Undo.DestroyObjectImmediate(leftMon.gameObject);
                Debug.Log("<color=#EF4444>[CADT VR] Left Red Screen Monitor removed.</color>");
            }

            // ----------------------------------------------------
            // 2. REPOSITION & CENTER PRIMARY MONITOR
            // ----------------------------------------------------
            Transform rightMon = workstation.Find("Right_Monitor_Setup");
            GameObject centerMonObj;
            if (rightMon != null)
            {
                centerMonObj = rightMon.gameObject;
                centerMonObj.name = "Primary_Monitor_Setup";
            }
            else
            {
                centerMonObj = workstation.Find("Primary_Monitor_Setup")?.gameObject;
                if (centerMonObj == null)
                {
                    centerMonObj = new GameObject("Primary_Monitor_Setup");
                    centerMonObj.transform.SetParent(workstation, false);
                }
            }

            centerMonObj.transform.localPosition = new Vector3(0f, 0.98f, 0.78f);
            centerMonObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            Transform frame = centerMonObj.transform.Find("Monitor_Frame_Right");
            if (frame == null) frame = centerMonObj.transform.Find("Monitor_Frame_Center");
            if (frame != null)
            {
                frame.name = "Monitor_Frame_Center";
                frame.localScale = new Vector3(0.72f, 0.44f, 0.025f);
            }

            // Monitor Stand Base
            Transform stand = centerMonObj.transform.Find("Monitor_Stand_Base");
            if (stand == null)
            {
                GameObject standObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                standObj.name = "Monitor_Stand_Base";
                standObj.transform.SetParent(centerMonObj.transform, false);
                standObj.transform.localPosition = new Vector3(0f, -0.22f, 0.02f);
                standObj.transform.localScale = new Vector3(0.24f, 0.015f, 0.18f);
                SetObjectColor(standObj, new Color(0.08f, 0.08f, 0.1f));
            }

            // ----------------------------------------------------
            // 3. REPOSITION DESK PROPS AROUND CENTERED MONITOR
            // ----------------------------------------------------
            Transform mousepad = workstation.Find("Desk_Mousepad");
            if (mousepad != null)
            {
                mousepad.localPosition = new Vector3(0f, 0.742f, 0.45f);
                mousepad.localScale = new Vector3(0.85f, 0.005f, 0.36f);
            }

            Transform keyboard = workstation.Find("Mechanical_Keyboard");
            if (keyboard != null)
            {
                keyboard.localPosition = new Vector3(-0.04f, 0.75f, 0.45f);
                keyboard.localScale = new Vector3(0.42f, 0.015f, 0.14f);
            }

            Transform mouse = workstation.Find("Optical_Mouse");
            if (mouse != null)
            {
                mouse.localPosition = new Vector3(0.30f, 0.75f, 0.45f);
                mouse.localScale = new Vector3(0.07f, 0.02f, 0.12f);
            }

            Transform propsGroup = envRoot.transform.Find("Interactive_Desk_Props");
            if (propsGroup != null)
            {
                Transform mug = propsGroup.Find("CADT_Coffee_Mug");
                if (mug != null) mug.localPosition = new Vector3(-0.46f, 0.80f, 0.55f);

                Transform card = propsGroup.Find("Security_Access_Card");
                if (card != null) card.localPosition = new Vector3(0.44f, 0.75f, 0.45f);

                Transform usb = propsGroup.Find("Suspicious_USB_Drive");
                if (usb != null) usb.localPosition = new Vector3(-0.35f, 0.75f, 0.38f);
            }

            // Desk Phone (Ext 4023)
            GameObject deskPhone = workstation.Find("Desk_Phone")?.gameObject;
            if (deskPhone == null)
            {
                deskPhone = new GameObject("Desk_Phone");
                deskPhone.transform.SetParent(workstation, false);
            }
            deskPhone.transform.localPosition = new Vector3(-0.48f, 0.76f, 0.42f);
            deskPhone.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);

            // Phone Base
            Transform phoneBase = deskPhone.transform.Find("Phone_Base");
            if (phoneBase == null)
            {
                GameObject pb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pb.name = "Phone_Base";
                pb.transform.SetParent(deskPhone.transform, false);
                pb.transform.localPosition = Vector3.zero;
                pb.transform.localScale = new Vector3(0.14f, 0.035f, 0.16f);
                SetObjectColor(pb, new Color(0.12f, 0.14f, 0.18f));
            }

            // Phone Handset
            Transform phoneHandset = deskPhone.transform.Find("Phone_Handset");
            if (phoneHandset == null)
            {
                GameObject ph = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ph.name = "Phone_Handset";
                ph.transform.SetParent(deskPhone.transform, false);
                ph.transform.localPosition = new Vector3(-0.04f, 0.03f, 0f);
                ph.transform.localScale = new Vector3(0.04f, 0.025f, 0.18f);
                SetObjectColor(ph, new Color(0.18f, 0.20f, 0.25f));
            }

            // Phone Display Screen
            Transform phoneDisplay = deskPhone.transform.Find("Phone_Display");
            if (phoneDisplay == null)
            {
                GameObject pd = GameObject.CreatePrimitive(PrimitiveType.Quad);
                pd.name = "Phone_Display";
                pd.transform.SetParent(deskPhone.transform, false);
                pd.transform.localPosition = new Vector3(0.03f, 0.02f, 0.04f);
                pd.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                pd.transform.localScale = new Vector3(0.06f, 0.04f, 1f);
                SetObjectColor(pd, new Color(0.0f, 0.75f, 0.85f));
            }

            // ----------------------------------------------------
            // 4. REBUILD / UPDATE TELEGRAM MONITOR UI
            // ----------------------------------------------------
            Transform screenCanvasTrans = centerMonObj.transform.Find("Right_Monitor_Screen_Canvas");
            if (screenCanvasTrans == null) screenCanvasTrans = centerMonObj.transform.Find("Monitor_Screen_Canvas");

            GameObject screenCanvasObj = screenCanvasTrans != null ? screenCanvasTrans.gameObject : new GameObject("Monitor_Screen_Canvas");
            screenCanvasObj.name = "Monitor_Screen_Canvas";
            screenCanvasObj.transform.SetParent(centerMonObj.transform, false);
            screenCanvasObj.transform.localPosition = new Vector3(0f, 0f, -0.018f);
            screenCanvasObj.transform.localRotation = Quaternion.identity;

            Canvas canvas = screenCanvasObj.GetComponent<Canvas>() ?? screenCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCam;
            if (screenCanvasObj.GetComponent<GraphicRaycaster>() == null) screenCanvasObj.AddComponent<GraphicRaycaster>();
            AddTrackedDeviceGraphicRaycaster(screenCanvasObj);

            RectTransform canvasRect = screenCanvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            canvasRect.localScale = new Vector3(0.00036f, 0.00036f, 0.00036f);

            // Build or refresh Telegram UI structure
            GameObject sovannToast;
            GameObject propertiesModal;
            GameObject adobeModal;
            GameObject cmdOverlay;
            GameObject qBanner;
            GameObject compBanner;
            GameObject wormBanner;

            BuildTelegramInterface(screenCanvasObj.transform, out sovannToast, out propertiesModal, out adobeModal, out cmdOverlay, out qBanner, out compBanner, out wormBanner);

            // ----------------------------------------------------
            // 5. 3D HOLOGRAPHIC ATTACKER C2 DASHBOARD
            // ----------------------------------------------------
            Transform existingC2 = envRoot.transform.Find("C2_Attacker_Hologram");
            if (existingC2 != null) Undo.DestroyObjectImmediate(existingC2.gameObject);

            GameObject c2HoloObj = new GameObject("C2_Attacker_Hologram", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            c2HoloObj.transform.SetParent(envRoot.transform, false);
            c2HoloObj.transform.localPosition = new Vector3(0f, 1.55f, 0.70f);
            c2HoloObj.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);

            Canvas c2Canvas = c2HoloObj.GetComponent<Canvas>();
            c2Canvas.renderMode = RenderMode.WorldSpace;
            c2Canvas.worldCamera = mainCam;
            AddTrackedDeviceGraphicRaycaster(c2HoloObj);
            AddTrackedDeviceGraphicRaycaster(c2HoloObj);

            RectTransform c2Rect = c2HoloObj.GetComponent<RectTransform>();
            c2Rect.sizeDelta = new Vector2(1000f, 380f);
            c2Rect.localScale = new Vector3(0.00045f, 0.00045f, 0.00045f);

            // C2 Background
            Transform c2Bg = c2HoloObj.transform.Find("C2_Bg");
            if (c2Bg != null) Undo.DestroyObjectImmediate(c2Bg.gameObject);

            GameObject c2BgObj = CreateUIPanel("C2_Bg", c2HoloObj.transform, new Vector2(1000f, 380f), new Color(0.12f, 0.02f, 0.02f, 0.90f));
            Outline c2Outline = c2BgObj.AddComponent<Outline>();
            c2Outline.effectColor = new Color(0.94f, 0.27f, 0.27f, 0.95f);
            c2Outline.effectDistance = new Vector2(3f, -3f);

            CreateTMPText("C2_Header", c2BgObj.transform, "<color=#EF4444>[!] ADVERSARY C2 DASHBOARD — LIVE TELEMETRY</color>", 24, TextAlignmentOptions.Left, Color.white, new Vector2(920f, 40f));
            SetAnchors(c2BgObj.transform.Find("C2_Header").GetComponent<RectTransform>(), new Vector2(0.5f, 0.88f), Vector2.zero);

            GameObject c2LogTextObj = CreateTMPText("C2_Log_TMP", c2BgObj.transform, "", 18, TextAlignmentOptions.TopLeft, new Color(0.95f, 0.8f, 0.8f), new Vector2(920f, 270f));
            SetAnchors(c2LogTextObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.42f), Vector2.zero);
            TextMeshProUGUI c2LogTMP = c2LogTextObj.GetComponent<TextMeshProUGUI>();

            c2HoloObj.SetActive(false);

            // ----------------------------------------------------
            // 6. VR GREEN SHIELD EFFECT
            // ----------------------------------------------------
            GameObject shieldObj = envRoot.transform.Find("VR_Green_Shield_FX")?.gameObject;
            if (shieldObj == null)
            {
                shieldObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shieldObj.name = "VR_Green_Shield_FX";
                shieldObj.transform.SetParent(envRoot.transform, false);
                shieldObj.transform.localPosition = new Vector3(0f, 0.95f, 0.65f);
                shieldObj.transform.localScale = new Vector3(1.8f, 1.2f, 1.4f);
                SetObjectColor(shieldObj, new Color(0.06f, 0.85f, 0.55f, 0.18f), true);
                Collider col = shieldObj.GetComponent<Collider>();
                if (col != null) Undo.DestroyObjectImmediate(col);
            }
            shieldObj.SetActive(false);

            // ----------------------------------------------------
            // 7. REDESIGN FLOATING 4-CHOICE DASHBOARD
            // ----------------------------------------------------
            Transform existingDash = envRoot.transform.Find("Diegetic_Spatial_Dashboard");
            GameObject dashboardObj;
            if (existingDash != null)
            {
                dashboardObj = existingDash.gameObject;
            }
            else
            {
                dashboardObj = new GameObject("Diegetic_Spatial_Dashboard", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                dashboardObj.transform.SetParent(envRoot.transform, false);
            }
            dashboardObj.transform.localPosition = new Vector3(0f, 1.22f, 0.52f);
            dashboardObj.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);

            Canvas dashCanvas = dashboardObj.GetComponent<Canvas>() ?? dashboardObj.AddComponent<Canvas>();
            dashCanvas.renderMode = RenderMode.WorldSpace;
            dashCanvas.worldCamera = mainCam;
            if (dashboardObj.GetComponent<GraphicRaycaster>() == null) dashboardObj.AddComponent<GraphicRaycaster>();
            AddTrackedDeviceGraphicRaycaster(dashboardObj);

            RectTransform dashRect = dashboardObj.GetComponent<RectTransform>() ?? dashboardObj.AddComponent<RectTransform>();
            dashRect.sizeDelta = new Vector2(1060f, 500f);
            dashRect.localScale = new Vector3(0.00045f, 0.00045f, 0.00045f);

            AudioSource audioSource = dashboardObj.GetComponent<AudioSource>() ?? dashboardObj.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.playOnAwake = false;

            // Clear old children on dashboard
            while (dashboardObj.transform.childCount > 0)
            {
                Undo.DestroyObjectImmediate(dashboardObj.transform.GetChild(0).gameObject);
            }

            // Dashboard Holographic Backdrop
            GameObject dashBg = CreateUIPanel("Holographic_Backdrop", dashboardObj.transform, new Vector2(1060f, 500f), new Color(0.05f, 0.08f, 0.15f, 0.80f));
            Outline dashOutline = dashBg.AddComponent<Outline>();
            dashOutline.effectColor = new Color(0.0f, 0.94f, 1.0f, 0.95f);
            dashOutline.effectDistance = new Vector2(3f, -3f);

            // Title & Subtitle
            GameObject headerObj = CreateTMPText("Header_TMP", dashboardObj.transform, "PHASE 3: INVESTIGATION & DILEMMA", 25, TextAlignmentOptions.Center, new Color(0.0f, 0.94f, 1.0f), new Vector2(1000f, 40f));
            SetAnchors(headerObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.91f), Vector2.zero);

            GameObject subObj = CreateTMPText("Subtitle_TMP", dashboardObj.transform, "A suspicious file was received via Telegram. How do you proceed?", 18, TextAlignmentOptions.Center, new Color(0.85f, 0.90f, 0.95f), new Vector2(1000f, 32f));
            SetAnchors(subObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.82f), Vector2.zero);

            // Status Bar
            GameObject statusObj = CreateTMPText("Status_Feedback_TMP", dashboardObj.transform, "<color=#94A3B8>STATUS:</color> Incoming threat archive detected. Choose your investigation action.", 16, TextAlignmentOptions.Center, Color.white, new Vector2(1000f, 32f));
            SetAnchors(statusObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.73f), Vector2.zero);

            // Timeline Slider
            GameObject sliderObj = CreateSlider("Timeline_Slider", dashboardObj.transform, new Vector2(980f, 16f));
            SetAnchors(sliderObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.65f), Vector2.zero);

            // 4 Decision Buttons (2x2 Grid)
            GameObject btnContainer = new GameObject("Choice_Buttons_Grid", typeof(RectTransform));
            btnContainer.transform.SetParent(dashboardObj.transform, false);
            RectTransform gridRect = btnContainer.GetComponent<RectTransform>();
            gridRect.sizeDelta = new Vector2(1000f, 280f);
            gridRect.anchoredPosition = new Vector2(0f, -85f);

            // Button A (Failure Branch - Red)
            GameObject btnAObj = CreateUIPanel("OptionA_Button", btnContainer.transform, new Vector2(480f, 110f), new Color(0.75f, 0.15f, 0.15f, 0.90f));
            SetAnchors(btnAObj.GetComponent<RectTransform>(), new Vector2(0.25f, 0.72f), Vector2.zero);
            Button btnA = btnAObj.AddComponent<Button>();
            Outline outA = btnAObj.AddComponent<Outline>();
            outA.effectColor = new Color(0.94f, 0.27f, 0.27f, 0.95f);
            CreateTMPText("BtnA_Text", btnAObj.transform, "<b>[OPTION A] Open Archive Directly</b>\n<size=14><color=#FECACA>Execute payload • Ignore double extension warning</color></size>", 16, TextAlignmentOptions.Center, Color.white, new Vector2(450f, 90f));

            // Button B (Inspect Properties - Cyan)
            GameObject btnBObj = CreateUIPanel("OptionB_Button", btnContainer.transform, new Vector2(480f, 110f), new Color(0.08f, 0.45f, 0.65f, 0.90f));
            SetAnchors(btnBObj.GetComponent<RectTransform>(), new Vector2(0.75f, 0.72f), Vector2.zero);
            Button btnB = btnBObj.AddComponent<Button>();
            Outline outB = btnBObj.AddComponent<Outline>();
            outB.effectColor = new Color(0.0f, 0.94f, 1.0f, 0.95f);
            CreateTMPText("BtnB_Text", btnBObj.transform, "<b>[OPTION B] Inspect File Properties</b>\n<size=14><color=#BAE6FD>Check true file extension (.pdf.exe) & AV bypass</color></size>", 16, TextAlignmentOptions.Center, Color.white, new Vector2(450f, 90f));

            // Button C (Call Sovann via Phone - Green)
            GameObject btnCObj = CreateUIPanel("OptionC_Button", btnContainer.transform, new Vector2(480f, 110f), new Color(0.06f, 0.50f, 0.35f, 0.90f));
            SetAnchors(btnCObj.GetComponent<RectTransform>(), new Vector2(0.25f, 0.24f), Vector2.zero);
            Button btnC = btnCObj.AddComponent<Button>();
            Outline outC = btnCObj.AddComponent<Outline>();
            outC.effectColor = new Color(0.16f, 0.85f, 0.55f, 0.95f);
            CreateTMPText("BtnC_Text", btnCObj.transform, "<b>[OPTION C] Call Sovann via Desk Phone</b>\n<size=14><color=#D1FAE5>Ext 4023 • Perform Out-of-Band Verification</color></size>", 16, TextAlignmentOptions.Center, Color.white, new Vector2(450f, 90f));

            // Button D (Quarantine Action - Indigo/Purple)
            GameObject btnDObj = CreateUIPanel("OptionD_Button", btnContainer.transform, new Vector2(480f, 110f), new Color(0.35f, 0.25f, 0.65f, 0.90f));
            SetAnchors(btnDObj.GetComponent<RectTransform>(), new Vector2(0.75f, 0.24f), Vector2.zero);
            Button btnD = btnDObj.AddComponent<Button>();
            Outline outD = btnDObj.AddComponent<Outline>();
            outD.effectColor = new Color(0.65f, 0.55f, 0.95f, 0.95f);
            CreateTMPText("BtnD_Text", btnDObj.transform, "<b>[OPTION D] Submit to IT Quarantine</b>\n<size=14><color=#E0E7FF>Isolate endpoint & dispatch incident ticket to SOC</color></size>", 16, TextAlignmentOptions.Center, Color.white, new Vector2(450f, 90f));

            // XP Success Banner (Overlay on dashboard)
            GameObject xpBannerObj = CreateUIPanel("XP_Banner_Overlay", dashboardObj.transform, new Vector2(980f, 120f), new Color(0.04f, 0.45f, 0.25f, 0.98f));
            SetAnchors(xpBannerObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero);
            Outline xpOutline = xpBannerObj.AddComponent<Outline>();
            xpOutline.effectColor = new Color(0.16f, 0.95f, 0.55f, 1f);
            xpOutline.effectDistance = new Vector2(3f, -3f);
            GameObject xpTextObj = CreateTMPText("XP_Text", xpBannerObj.transform, "+100 XP — THREAT NEUTRALIZED\n<size=18><color=#D1FAE5>Disguised executable payload intercepted before lateral execution.</color></size>", 24, TextAlignmentOptions.Center, Color.white, new Vector2(920f, 90f));
            xpBannerObj.SetActive(false);

            // ----------------------------------------------------
            // 8. WIRE SCENARIO CONTROLLER & AUDIO
            // ----------------------------------------------------
            // Remove old VRBranchManager if present
            VRBranchManager oldMgr = dashboardObj.GetComponent<VRBranchManager>();
            if (oldMgr != null) Undo.DestroyObjectImmediate(oldMgr);

            ScenarioChoiceController controller = dashboardObj.GetComponent<ScenarioChoiceController>() ?? dashboardObj.AddComponent<ScenarioChoiceController>();

            // Generate SFX
            AudioClip chimeSfx = GenerateToneClip("SFX_Telegram_Chime", 920f, 0.35f, true);
            AudioClip alarmSfx = GenerateToneClip("SFX_Breach_Alarm_Loud", 220f, 0.8f, false);
            AudioClip passSfx = GenerateToneClip("SFX_Success_Fanfare", 880f, 0.5f, true);
            AudioClip phoneSfx = GenerateToneClip("SFX_Phone_Ring_Voice", 480f, 0.4f, true);
            AudioClip clickSfx = GenerateToneClip("SFX_UI_Click", 1200f, 0.05f, false);
            AudioClip inspectSfx = GenerateToneClip("SFX_File_Inspect", 680f, 0.15f, true);

            // Lighting
            Light ceilingLight = envRoot.transform.Find("Room_Architecture/Ceiling_Recessed_Light")?.GetComponent<Light>();
            Light dirLight = GameObject.Find("Directional Light")?.GetComponent<Light>();

            // Wire Serialized Object
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("headerTMP").objectReferenceValue = headerObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("subtitleTMP").objectReferenceValue = subObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("statusFeedbackTMP").objectReferenceValue = statusObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("timelineSlider").objectReferenceValue = sliderObj.GetComponent<Slider>();
            so.FindProperty("xpBannerObject").objectReferenceValue = xpBannerObj;
            so.FindProperty("xpBannerTMP").objectReferenceValue = xpTextObj.GetComponent<TextMeshProUGUI>();

            so.FindProperty("optionA_OpenArchiveButton").objectReferenceValue = btnA;
            so.FindProperty("optionB_InspectPropertiesButton").objectReferenceValue = btnB;
            so.FindProperty("optionC_CallPhoneButton").objectReferenceValue = btnC;
            so.FindProperty("optionD_QuarantineButton").objectReferenceValue = btnD;

            so.FindProperty("telegramMonitorRoot").objectReferenceValue = screenCanvasObj;
            so.FindProperty("incomingSovannPopup").objectReferenceValue = sovannToast;
            so.FindProperty("filePropertiesModal").objectReferenceValue = propertiesModal;
            so.FindProperty("adobeErrorModal").objectReferenceValue = adobeModal;
            so.FindProperty("cmdPromptFlashFX").objectReferenceValue = cmdOverlay;
            so.FindProperty("telegramQuarantineBanner").objectReferenceValue = qBanner;
            so.FindProperty("telegramCompromisedBanner").objectReferenceValue = compBanner;
            so.FindProperty("telegramWormSpreadingBanner").objectReferenceValue = wormBanner;

            so.FindProperty("c2HologramDashboard").objectReferenceValue = c2HoloObj;
            so.FindProperty("c2LogTMP").objectReferenceValue = c2LogTMP;

            so.FindProperty("vrGreenShieldFX").objectReferenceValue = shieldObj;
            so.FindProperty("deskPhoneObject").objectReferenceValue = deskPhone;
            so.FindProperty("ceilingLight").objectReferenceValue = ceilingLight;
            so.FindProperty("directionalLight").objectReferenceValue = dirLight;

            so.FindProperty("spatialAudioSource").objectReferenceValue = audioSource;
            so.FindProperty("incomingMessageSFX").objectReferenceValue = chimeSfx;
            so.FindProperty("alarmAudioClip").objectReferenceValue = alarmSfx;
            so.FindProperty("successAudioClip").objectReferenceValue = passSfx;
            so.FindProperty("phoneRingDialogueSFX").objectReferenceValue = phoneSfx;
            so.FindProperty("buttonClickSFX").objectReferenceValue = clickSfx;
            so.FindProperty("inspectSFX").objectReferenceValue = inspectSfx;

            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(envRoot);
            EditorUtility.SetDirty(dashboardObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("<color=#10B981><b>[CADT VR]</b> Scenario 1 Workstation refactored and saved successfully!</color>");
        }

        private static void BuildTelegramInterface(Transform parent, out GameObject sovannToast, out GameObject propModal, out GameObject adobeModal, out GameObject cmdOverlay, out GameObject qBanner, out GameObject compBanner, out GameObject wormBanner)
        {
            // Clear existing Telegram structure to ensure a fresh, pixel-perfect layout
            while (parent.childCount > 0)
            {
                Undo.DestroyObjectImmediate(parent.GetChild(0).gameObject);
            }

            // Main Background
            GameObject bg = CreateUIPanel("Telegram_Client_Root", parent, new Vector2(1920f, 1080f), new Color(0.95f, 0.96f, 0.97f, 1f));

            // Sidebar
            GameObject sidebar = CreateUIPanel("Chat_Sidebar", bg.transform, new Vector2(620f, 1080f), Color.white);
            SetAnchors(sidebar.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(310f, 0f));

            // Top Search Bar
            GameObject topBar = CreateUIPanel("Top_Search_Bar", sidebar.transform, new Vector2(620f, 65f), Color.white);
            SetAnchors(topBar.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -32.5f));
            CreateTMPText("Search_Placeholder", topBar.transform, "Search", 18, TextAlignmentOptions.Left, new Color(0.62f, 0.66f, 0.72f), new Vector2(460f, 36f));
            SetAnchors(topBar.transform.Find("Search_Placeholder").GetComponent<RectTransform>(), new Vector2(0.56f, 0.5f), Vector2.zero);

            // Contacts
            float startY = 960f;
            float stepY = 100f;

            // 1. Sovann (INCOMING / SELECTED - Cyan Blue)
            CreateTelegramContact(sidebar.transform, "Contact_Sovann", "Sovann (Coworker)", false, "SV", new Color(0.15f, 0.65f, 0.85f),
                "Salary_Adjustments_August_2026.rar", "Just now", true, startY - (stepY * 0));

            // 2. Eva Summer
            CreateTelegramContact(sidebar.transform, "Contact_EvaSummer", "Eva Summer", false, "ES", new Color(0.95f, 0.45f, 0.35f),
                "Reminds me of a Chinese proverb...", "11:28 PM", false, startY - (stepY * 1));

            // 3. Lena Oxton
            CreateTelegramContact(sidebar.transform, "Contact_Lena", "Lena Oxton", false, "LO", new Color(0.92f, 0.7f, 0.2f),
                "Coffee at 2pm?", "9:17 PM", false, startY - (stepY * 2));

            // 4. Pandas HQ (Group)
            CreateTelegramContact(sidebar.transform, "Contact_Pandas", "Pandas HQ (Engineering)", true, "PH", new Color(0.2f, 0.75f, 0.4f),
                "Sprint retro scheduled for tomorrow", "1:14 AM", false, startY - (stepY * 3));

            // 5. Max Bright
            CreateTelegramContact(sidebar.transform, "Contact_MaxBright", "Max Bright", false, "MB", new Color(0.35f, 0.65f, 0.95f),
                "Docs updated on Git.", "Mon", false, startY - (stepY * 4));

            // ----------------------------------------------------
            // CHAT AREA
            // ----------------------------------------------------
            GameObject chatArea = CreateUIPanel("Main_Chat_Area", bg.transform, new Vector2(1300f, 1080f), new Color(0.70f, 0.80f, 0.72f, 1f));
            SetAnchors(chatArea.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-650f, 0f));

            // Header Bar
            GameObject chatHeader = CreateUIPanel("Chat_Header", chatArea.transform, new Vector2(1300f, 65f), Color.white);
            SetAnchors(chatHeader.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -32.5f));
            CreateTMPText("Header_Name", chatHeader.transform, "<b>Sovann (Coworker)</b>\n<size=15><color=#2481CC>online • HR & Internal Ops</color></size>", 19, TextAlignmentOptions.Left, Color.black, new Vector2(600f, 50f));
            SetAnchors(chatHeader.transform.Find("Header_Name").GetComponent<RectTransform>(), new Vector2(0.26f, 0.5f), Vector2.zero);

            // Message 1: Sovann Phishing Text Bubble
            GameObject msg1 = CreateUIPanel("Sovann_Text_Bubble", chatArea.transform, new Vector2(720f, 95f), Color.white);
            SetAnchors(msg1.GetComponent<RectTransform>(), new Vector2(0.30f, 0.78f), Vector2.zero);
            CreateTMPText("Msg_Text", msg1.transform, "Hey, HR just sent the updated salary slip for August. Check your email too. Password is: <color=#EF4444><b>salary2026</b></color>  <size=14><color=#8E8E93>11:42 AM</color></size>", 17, TextAlignmentOptions.Left, Color.black, new Vector2(680f, 75f));
            SetAnchors(msg1.transform.Find("Msg_Text").GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero);

            // Message 2: Downloadable RAR Attachment Card
            GameObject msg2 = CreateUIPanel("Sovann_Attachment_Bubble", chatArea.transform, new Vector2(620f, 110f), Color.white);
            SetAnchors(msg2.GetComponent<RectTransform>(), new Vector2(0.26f, 0.60f), Vector2.zero);

            // Download Icon Circle
            GameObject dlCircle = CreateUIPanel("DL_Circle", msg2.transform, new Vector2(64f, 64f), new Color(0.2f, 0.56f, 0.93f, 1f));
            SetAnchors(dlCircle.GetComponent<RectTransform>(), new Vector2(0.08f, 0.5f), Vector2.zero);
            CreateTMPText("Arrow_Down", dlCircle.transform, "↓", 30, TextAlignmentOptions.Center, Color.white);

            // File Name & Details
            CreateTMPText("Attach_Details", msg2.transform, "<b>Salary_Adjustments_August_2026.rar</b>\n<size=15><color=#EF4444><b>Contains: Salary_Slip_August.pdf.exe</b></color></size>\n<size=14><color=#8E8E93>4.8 MB • Encrypted Archive (Password: salary2026) • 11:42 AM</color></size>", 16, TextAlignmentOptions.Left, Color.black, new Vector2(490f, 85f));
            SetAnchors(msg2.transform.Find("Attach_Details").GetComponent<RectTransform>(), new Vector2(0.56f, 0.5f), Vector2.zero);

            // Bottom Input Bar
            GameObject inputBar = CreateUIPanel("Message_Input_Bar", chatArea.transform, new Vector2(1300f, 65f), Color.white);
            SetAnchors(inputBar.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 32.5f));
            CreateTMPText("Input_Placeholder", inputBar.transform, "Write a message...", 18, TextAlignmentOptions.Left, new Color(0.62f, 0.66f, 0.72f), new Vector2(900f, 40f));
            SetAnchors(inputBar.transform.Find("Input_Placeholder").GetComponent<RectTransform>(), new Vector2(0.44f, 0.5f), Vector2.zero);

            // ----------------------------------------------------
            // OVERLAY POP-UPS & MODALS
            // ----------------------------------------------------
            // 1. Incoming Threat Toast (Top Right Notification)
            sovannToast = CreateUIPanel("Incoming_Threat_Toast", bg.transform, new Vector2(460f, 120f), new Color(0.12f, 0.16f, 0.24f, 0.98f));
            SetAnchors(sovannToast.GetComponent<RectTransform>(), new Vector2(0.86f, 0.88f), Vector2.zero);
            Outline toastOutline = sovannToast.AddComponent<Outline>();
            toastOutline.effectColor = new Color(0.0f, 0.94f, 1.0f, 0.95f);
            toastOutline.effectDistance = new Vector2(2f, -2f);
            CreateTMPText("Toast_Text", sovannToast.transform, "<b>Telegram Desktop • Sovann</b>\n<size=15>\"Hey, HR just sent the updated salary slip...\"</size>\n<size=14><color=#38BDF8>Attachment: Salary_Adjustments_August_2026.rar</color></size>", 16, TextAlignmentOptions.Left, Color.white, new Vector2(420f, 95f));
            SetAnchors(sovannToast.transform.Find("Toast_Text").GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero);

            // 2. File Properties Modal (Option B Inspector)
            propModal = CreateUIPanel("File_Properties_Modal", bg.transform, new Vector2(650f, 720f), new Color(0.10f, 0.12f, 0.16f, 0.98f));
            propModal.transform.localPosition = new Vector3(80f, 20f, -2f);
            Outline propOutline = propModal.AddComponent<Outline>();
            propOutline.effectColor = new Color(0.0f, 0.94f, 1.0f, 0.95f);

            GameObject propHeader = CreateUIPanel("Header_Bar", propModal.transform, new Vector2(650f, 50f), new Color(0.15f, 0.18f, 0.24f, 1f));
            SetAnchors(propHeader.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -25f));
            CreateTMPText("Modal_Title", propHeader.transform, "Salary_Slip_August.pdf.exe Properties", 20, TextAlignmentOptions.Left, Color.white, new Vector2(600f, 40f));

            CreateTMPText("Property_Body", propModal.transform,
                "<b>File Name:</b> Salary_Slip_August.pdf<color=#EF4444><b>.exe</b></color>\n" +
                "<b>Type of file:</b> <color=#EF4444><b>Application (.exe)</b></color>\n" +
                "<b>Description:</b> Disguised Malicious Dropper Payload\n" +
                "<b>Archive:</b> Salary_Adjustments_August_2026.rar\n" +
                "<b>Evasion Tactic:</b> <color=#EF4444>Password Protected RAR (salary2026)</color>\n" +
                "<b>Digital Signature:</b> <color=#EF4444>Unsigned / Invalid Certificate</color>\n" +
                "<b>Security Analysis:</b> Double extension spoofing designed to mislead employees into opening binary payload.",
                18, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.88f, 0.92f), new Vector2(580f, 480f));
            SetAnchors(propModal.transform.Find("Property_Body").GetComponent<RectTransform>(), new Vector2(0.5f, 0.48f), Vector2.zero);

            GameObject closeBtnObj = CreateUIPanel("Close_Button", propModal.transform, new Vector2(140f, 44f), new Color(0.2f, 0.25f, 0.35f, 1f));
            SetAnchors(closeBtnObj.GetComponent<RectTransform>(), new Vector2(0.84f, 0.08f), Vector2.zero);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            CreateTMPText("Close_Text", closeBtnObj.transform, "OK", 20, TextAlignmentOptions.Center, Color.white);
            GameObject localPropModal = propModal;
            closeBtn.onClick.AddListener(() => localPropModal.SetActive(false));
            propModal.SetActive(false);

            // 3. Fake Adobe Reader Error Modal (Option A)
            adobeModal = CreateUIPanel("Adobe_Error_Modal", bg.transform, new Vector2(600f, 260f), new Color(0.95f, 0.95f, 0.95f, 1f));
            adobeModal.transform.localPosition = new Vector3(80f, 20f, -3f);
            Outline adobeOutline = adobeModal.AddComponent<Outline>();
            adobeOutline.effectColor = new Color(0.9f, 0.2f, 0.2f, 1f);

            GameObject adobeHeader = CreateUIPanel("Adobe_Header", adobeModal.transform, new Vector2(600f, 40f), new Color(0.85f, 0.15f, 0.15f, 1f));
            SetAnchors(adobeHeader.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -20f));
            CreateTMPText("Adobe_Title", adobeHeader.transform, "Adobe Acrobat Reader DC - Error", 18, TextAlignmentOptions.Left, Color.white, new Vector2(560f, 30f));

            CreateTMPText("Adobe_Body", adobeModal.transform, "<b>Fatal Error 0x80070002:</b>\nFailed to render PDF structure in document 'Salary_Slip_August.pdf'.\n\n<color=#64748B>A background process has encountered an unhandled exception.</color>", 16, TextAlignmentOptions.TopLeft, Color.black, new Vector2(540f, 140f));
            SetAnchors(adobeModal.transform.Find("Adobe_Body").GetComponent<RectTransform>(), new Vector2(0.5f, 0.35f), Vector2.zero);
            adobeModal.SetActive(false);

            // 4. Command Prompt Flash Overlay
            cmdOverlay = CreateUIPanel("Cmd_Flash_Overlay", bg.transform, new Vector2(800f, 450f), new Color(0.02f, 0.02f, 0.02f, 0.96f));
            cmdOverlay.transform.localPosition = new Vector3(0f, 0f, -4f);
            CreateTMPText("Cmd_Text", cmdOverlay.transform, "C:\\Windows\\System32\\cmd.exe\n\nC:\\Users\\CADT> powershell.exe -WindowStyle Hidden -ExecutionPolicy Bypass -enc SQBFAFgA...\n[+] Payload downloaded to %TEMP%\\Salary_Slip_August.pdf.exe\n[+] In-memory process hollowed: PID 4108 (svchost.exe)\n[+] C2 Beacon active: 185.220.101.44:443", 16, TextAlignmentOptions.TopLeft, new Color(0.1f, 0.95f, 0.2f), new Vector2(750f, 380f));
            SetAnchors(cmdOverlay.transform.Find("Cmd_Text").GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero);
            cmdOverlay.SetActive(false);

            // 5. Quarantine Banner
            qBanner = CreateUIPanel("Quarantine_Banner", bg.transform, new Vector2(1300f, 110f), new Color(0.06f, 0.4f, 0.25f, 0.96f));
            SetAnchors(qBanner.GetComponent<RectTransform>(), new Vector2(0.66f, 0.12f), Vector2.zero);
            CreateTMPText("Q_Text", qBanner.transform, "[LOCKED] THREAT QUARANTINED BY CADT SOC • ENDPOINT SECURED", 26, TextAlignmentOptions.Center, Color.white);
            qBanner.SetActive(false);

            // 6. Compromised Banner
            compBanner = CreateUIPanel("Compromised_Banner", bg.transform, new Vector2(1300f, 110f), new Color(0.65f, 0.1f, 0.1f, 0.96f));
            SetAnchors(compBanner.GetComponent<RectTransform>(), new Vector2(0.66f, 0.12f), Vector2.zero);
            CreateTMPText("C_Text", compBanner.transform, "[!] WORKSTATION COMPROMISED: CREDENTIALS HARVESTED BY C2", 26, TextAlignmentOptions.Center, Color.white);
            compBanner.SetActive(false);

            // 7. Worm Spreading Banner
            wormBanner = CreateUIPanel("Worm_Banner", bg.transform, new Vector2(1300f, 80f), new Color(0.85f, 0.35f, 0.05f, 0.96f));
            SetAnchors(wormBanner.GetComponent<RectTransform>(), new Vector2(0.66f, 0.24f), Vector2.zero);
            CreateTMPText("W_Text", wormBanner.transform, "[!] WORM SPREADING: Auto-sent 'Salary_Adjustments_August_2026.rar' to 10 contacts", 22, TextAlignmentOptions.Center, Color.white);
            wormBanner.SetActive(false);
        }

        private static void CreateTelegramContact(Transform parent, string name, string contactName, bool isGroup, string avatarInitials, Color avatarColor, string snippet, string time, bool isSelected, float posY)
        {
            GameObject contact = CreateUIPanel(name, parent, new Vector2(600f, 85f), isSelected ? new Color(0.2f, 0.56f, 0.93f, 1f) : Color.white);
            SetAnchors(contact.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, posY));

            GameObject avatar = CreateUIPanel("Avatar", contact.transform, new Vector2(56f, 56f), avatarColor);
            SetAnchors(avatar.GetComponent<RectTransform>(), new Vector2(0.07f, 0.5f), Vector2.zero);
            CreateTMPText("Initials", avatar.transform, avatarInitials, 22, TextAlignmentOptions.Center, Color.white);

            CreateTMPText("Contact_Name", contact.transform, (isGroup ? "[Grp] " : "") + "<b>" + contactName + "</b>", 18, TextAlignmentOptions.Left, isSelected ? Color.white : new Color(0.12f, 0.12f, 0.14f), new Vector2(360f, 26f));
            SetAnchors(contact.transform.Find("Contact_Name").GetComponent<RectTransform>(), new Vector2(0.44f, 0.68f), Vector2.zero);

            CreateTMPText("Snippet", contact.transform, snippet, 16, TextAlignmentOptions.Left, isSelected ? new Color(0.9f, 0.95f, 1f) : new Color(0.55f, 0.58f, 0.64f), new Vector2(360f, 24f));
            SetAnchors(contact.transform.Find("Snippet").GetComponent<RectTransform>(), new Vector2(0.44f, 0.32f), Vector2.zero);

            CreateTMPText("Time", contact.transform, time, 15, TextAlignmentOptions.Right, isSelected ? new Color(0.9f, 0.95f, 1f) : new Color(0.55f, 0.58f, 0.64f), new Vector2(120f, 24f));
            SetAnchors(contact.transform.Find("Time").GetComponent<RectTransform>(), new Vector2(0.86f, 0.68f), Vector2.zero);
        }

        private static GameObject CreateUIPanel(string name, Transform parent, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            Image img = panel.GetComponent<Image>();
            img.color = color;
            return panel;
        }

        private static GameObject CreateTMPText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions align, Color color, Vector2 size = default)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (size != default) rect.sizeDelta = size;
            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.extraPadding = true;

            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null) tmp.font = font;

            return obj;
        }

        private static GameObject CreateSlider(string name, Transform parent, Vector2 size)
        {
            GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(parent, false);
            RectTransform rect = sliderObj.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.45f;

            GameObject bg = CreateUIPanel("Background", sliderObj.transform, size, new Color(0.08f, 0.12f, 0.18f, 0.9f));
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0f, 0.15f);
            faRect.anchorMax = new Vector2(1f, 0.85f);
            faRect.sizeDelta = Vector2.zero;

            GameObject fill = CreateUIPanel("Fill", fillArea.transform, Vector2.zero, new Color(0.0f, 0.94f, 1.0f, 1f));
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            slider.fillRect = fillRect;
            slider.targetGraphic = fill.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;

            return sliderObj;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchor, Vector2 anchoredPos)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = anchoredPos;
        }

        private static void SetObjectColor(GameObject go, Color col, bool transparent = false)
        {
            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material mat = new Material(shader);
                mat.color = col;
                if (transparent)
                {
                    mat.SetFloat("_Surface", 1);
                    mat.renderQueue = 3000;
                }
                rend.material = mat;
            }
        }

        private static void AddTrackedDeviceGraphicRaycaster(GameObject go)
        {
            Type raycasterType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (raycasterType != null && go.GetComponent(raycasterType) == null)
            {
                go.AddComponent(raycasterType);
            }
        }

        private static AudioClip GenerateToneClip(string clipName, float frequency, float duration, bool harmonic)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Sin(t / duration * Mathf.PI);
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);
                if (harmonic)
                {
                    wave = 0.7f * wave + 0.3f * Mathf.Sin(2f * Mathf.PI * frequency * 1.5f * t);
                }
                samples[i] = wave * envelope * 0.45f;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
