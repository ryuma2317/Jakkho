using System;
using CADT.VR.Cybersecurity;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CADT.VR.Editor
{
    public static class CyberVRSceneBuilder
    {
        [MenuItem("CADT/Build Simulation UI & Scene", false, 1)]
        public static void BuildCompleteSimulationScene()
        {
            Debug.Log("<color=#00F0FF><b>[CADT VR]</b> Constructing Immersive CADT Office Room & Workstation UI with Authentic Telegram Desktop...</color>");

            EnsureEventSystem();
            Camera mainCam = EnsureCameraPosition();
            Material holoMaterial = CreateOrGetHoloMaterial();

            // 1. Root Environment Holder
            GameObject envRoot = GetOrCreateRoot("CADT_Office_Environment");

            // 2. Build Complete Enclosed Office Room
            BuildOfficeRoom(envRoot);

            // 3. Desk, Chair, and Dual Monitor Setup
            CreateDeskAndMonitors(envRoot, out GameObject leftMonitorScreen, out GameObject rightMonitorScreen);

            // 4. Interactive Grabbable Props on Desk (USB Drive, Security Keycard, Coffee Mug)
            CreateInteractiveDeskProps(envRoot);

            // 5. Monitor 1 UI (Left Screen - Windows 11 Desktop)
            GameObject mon1Canvas = CreateMonitor1UI(leftMonitorScreen, mainCam);

            // 6. Monitor 2 UI (Right Screen - Pixel-Perfect Authentic Telegram Interface)
            GameObject mon2Canvas = CreateMonitor2TelegramUI(rightMonitorScreen, mainCam);

            // 7. Diegetic Spatial Hologram Dashboard
            GameObject dashboardObj = CreateHolographicDashboard(envRoot, holoMaterial, mainCam);

            // 8. Audio Generation & Assignment
            AudioClip successSfx = GenerateToneClip("SFX_Success_Chime", 880f, 0.4f, true);
            AudioClip failSfx = GenerateToneClip("SFX_Breach_Alarm", 220f, 0.5f, false);
            AudioClip clickSfx = GenerateToneClip("SFX_UI_Click", 1200f, 0.05f, false);
            AudioClip inspectSfx = GenerateToneClip("SFX_Inspect_Beep", 660f, 0.15f, true);

            // 9. Wire Dependencies on VRBranchManager and MonitorFileInspector
            WireDependencies(dashboardObj, mon1Canvas, mon2Canvas, successSfx, failSfx, clickSfx, inspectSfx);

            EditorUtility.SetDirty(envRoot);
            Debug.Log("<color=#10B981><b>[CADT VR]</b> Complete Office Room & Authentic Telegram UI generated successfully!</color>");
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }
        }

        private static Camera EnsureCameraPosition()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            }

            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0f, 1.15f, -0.05f);
                mainCam.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
                mainCam.fieldOfView = 65f;
                mainCam.nearClipPlane = 0.03f;
            }
            return mainCam;
        }

        private static Material CreateOrGetHoloMaterial()
        {
            Shader holoShader = Shader.Find("CADT/VR/HolographicSpatialUI");
            if (holoShader == null) holoShader = Shader.Find("UI/Default");

            Material mat = new Material(holoShader);
            mat.name = "M_Holographic_Spatial_UI";
            return mat;
        }

        private static GameObject GetOrCreateRoot(string name)
        {
            GameObject root = GameObject.Find(name);
            if (root != null)
            {
                Undo.DestroyObjectImmediate(root);
            }
            root = new GameObject(name);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(root, "Create " + name);
            return root;
        }

        private static void BuildOfficeRoom(GameObject parent)
        {
            GameObject room = new GameObject("Room_Architecture");
            room.transform.SetParent(parent.transform, false);

            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Office_Floor";
            floor.transform.SetParent(room.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.05f, 1f);
            floor.transform.localScale = new Vector3(6f, 0.1f, 6f);
            SetObjectColor(floor, new Color(0.12f, 0.14f, 0.17f));

            // Ceiling
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Office_Ceiling";
            ceiling.transform.SetParent(room.transform, false);
            ceiling.transform.localPosition = new Vector3(0f, 2.8f, 1f);
            ceiling.transform.localScale = new Vector3(6f, 0.1f, 6f);
            SetObjectColor(ceiling, new Color(0.16f, 0.18f, 0.22f));

            // Left Wall & Accent Panel
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "Wall_Left";
            leftWall.transform.SetParent(room.transform, false);
            leftWall.transform.localPosition = new Vector3(-3f, 1.4f, 1f);
            leftWall.transform.localScale = new Vector3(0.1f, 2.8f, 6f);
            SetObjectColor(leftWall, new Color(0.20f, 0.23f, 0.27f));

            GameObject leftAccent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftAccent.name = "Left_Wall_Acoustic_Panel";
            leftAccent.transform.SetParent(room.transform, false);
            leftAccent.transform.localPosition = new Vector3(-2.92f, 1.4f, 1f);
            leftAccent.transform.localScale = new Vector3(0.04f, 2.2f, 3.5f);
            SetObjectColor(leftAccent, new Color(0.32f, 0.20f, 0.12f));

            // Right Wall
            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "Wall_Right";
            rightWall.transform.SetParent(room.transform, false);
            rightWall.transform.localPosition = new Vector3(3f, 1.4f, 1f);
            rightWall.transform.localScale = new Vector3(0.1f, 2.8f, 6f);
            SetObjectColor(rightWall, new Color(0.20f, 0.23f, 0.27f));

            // Back Wall Window Frame & Glass
            GameObject backWallFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWallFrame.name = "Window_Mullion_Top";
            backWallFrame.transform.SetParent(room.transform, false);
            backWallFrame.transform.localPosition = new Vector3(0f, 2.6f, 3.8f);
            backWallFrame.transform.localScale = new Vector3(6f, 0.4f, 0.1f);
            SetObjectColor(backWallFrame, new Color(0.08f, 0.10f, 0.13f));

            GameObject backWallBottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWallBottom.name = "Window_Sill_Bottom";
            backWallBottom.transform.SetParent(room.transform, false);
            backWallBottom.transform.localPosition = new Vector3(0f, 0.3f, 3.8f);
            backWallBottom.transform.localScale = new Vector3(6f, 0.6f, 0.1f);
            SetObjectColor(backWallBottom, new Color(0.08f, 0.10f, 0.13f));

            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "CADT_Campus_Glass_Window";
            glass.transform.SetParent(room.transform, false);
            glass.transform.localPosition = new Vector3(0f, 1.5f, 3.85f);
            glass.transform.localScale = new Vector3(5.8f, 1.8f, 0.02f);
            SetObjectColor(glass, new Color(0.4f, 0.6f, 0.75f, 0.35f), true);

            // Campus Skyline Backdrop
            GameObject campusBackdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            campusBackdrop.name = "CADT_Campus_Skyline_Backdrop";
            campusBackdrop.transform.SetParent(room.transform, false);
            campusBackdrop.transform.localPosition = new Vector3(0f, 1.5f, 4.8f);
            campusBackdrop.transform.localScale = new Vector3(10f, 4.5f, 1f);
            SetObjectColor(campusBackdrop, new Color(0.55f, 0.72f, 0.90f));

            // Rear Wall
            GameObject rearWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearWall.name = "Wall_Rear";
            rearWall.transform.SetParent(room.transform, false);
            rearWall.transform.localPosition = new Vector3(0f, 1.4f, -2f);
            rearWall.transform.localScale = new Vector3(6f, 2.8f, 0.1f);
            SetObjectColor(rearWall, new Color(0.14f, 0.16f, 0.20f));

            // Point Light
            GameObject ceilingLightObj = new GameObject("Ceiling_Recessed_Light");
            ceilingLightObj.transform.SetParent(room.transform, false);
            ceilingLightObj.transform.localPosition = new Vector3(0f, 2.6f, 0.6f);
            Light ceilingLight = ceilingLightObj.AddComponent<Light>();
            ceilingLight.type = LightType.Point;
            ceilingLight.range = 8f;
            ceilingLight.intensity = 2.2f;
            ceilingLight.color = new Color(0.96f, 0.97f, 1.0f);
        }

        private static void CreateDeskAndMonitors(GameObject parent, out GameObject leftScreen, out GameObject rightScreen)
        {
            GameObject workstation = new GameObject("Workstation_Setup");
            workstation.transform.SetParent(parent.transform, false);

            // Desk Surface
            GameObject tableTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableTop.name = "Desk_Surface";
            tableTop.transform.SetParent(workstation.transform, false);
            tableTop.transform.localPosition = new Vector3(0f, 0.72f, 0.65f);
            tableTop.transform.localScale = new Vector3(1.7f, 0.04f, 0.85f);
            SetObjectColor(tableTop, new Color(0.14f, 0.16f, 0.19f));

            // Desk Legs
            CreateDeskLeg("Desk_Leg_FL", workstation.transform, new Vector3(-0.8f, 0.35f, 0.3f));
            CreateDeskLeg("Desk_Leg_FR", workstation.transform, new Vector3(0.8f, 0.35f, 0.3f));
            CreateDeskLeg("Desk_Leg_BL", workstation.transform, new Vector3(-0.8f, 0.35f, 1.0f));
            CreateDeskLeg("Desk_Leg_BR", workstation.transform, new Vector3(0.8f, 0.35f, 1.0f));

            // Mousepad, Keyboard, Mouse
            GameObject mousepad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mousepad.name = "Desk_Mousepad";
            mousepad.transform.SetParent(workstation.transform, false);
            mousepad.transform.localPosition = new Vector3(0f, 0.742f, 0.45f);
            mousepad.transform.localScale = new Vector3(0.9f, 0.005f, 0.38f);
            SetObjectColor(mousepad, new Color(0.06f, 0.08f, 0.12f));

            GameObject keyboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keyboard.name = "Mechanical_Keyboard";
            keyboard.transform.SetParent(workstation.transform, false);
            keyboard.transform.localPosition = new Vector3(-0.08f, 0.75f, 0.45f);
            keyboard.transform.localScale = new Vector3(0.42f, 0.015f, 0.14f);
            SetObjectColor(keyboard, new Color(0.18f, 0.20f, 0.24f));

            GameObject mouse = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mouse.name = "Optical_Mouse";
            mouse.transform.SetParent(workstation.transform, false);
            mouse.transform.localPosition = new Vector3(0.28f, 0.75f, 0.45f);
            mouse.transform.localScale = new Vector3(0.07f, 0.02f, 0.12f);
            SetObjectColor(mouse, new Color(0.18f, 0.20f, 0.24f));

            // Left Monitor Stand & Frame
            GameObject leftMonitor = new GameObject("Left_Monitor_Setup");
            leftMonitor.transform.SetParent(workstation.transform, false);
            leftMonitor.transform.localPosition = new Vector3(-0.38f, 0.98f, 0.78f);
            leftMonitor.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);

            GameObject leftFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftFrame.name = "Monitor_Frame_Left";
            leftFrame.transform.SetParent(leftMonitor.transform, false);
            leftFrame.transform.localScale = new Vector3(0.72f, 0.44f, 0.025f);
            SetObjectColor(leftFrame, new Color(0.04f, 0.04f, 0.06f));

            leftScreen = new GameObject("Left_Monitor_Screen_Canvas");
            leftScreen.transform.SetParent(leftMonitor.transform, false);
            leftScreen.transform.localPosition = new Vector3(0f, 0f, -0.018f);

            // Right Monitor Stand & Frame
            GameObject rightMonitor = new GameObject("Right_Monitor_Setup");
            rightMonitor.transform.SetParent(workstation.transform, false);
            rightMonitor.transform.localPosition = new Vector3(0.38f, 0.98f, 0.78f);
            rightMonitor.transform.localRotation = Quaternion.Euler(0f, -15f, 0f);

            GameObject rightFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightFrame.name = "Monitor_Frame_Right";
            rightFrame.transform.SetParent(rightMonitor.transform, false);
            rightFrame.transform.localScale = new Vector3(0.72f, 0.44f, 0.025f);
            SetObjectColor(rightFrame, new Color(0.04f, 0.04f, 0.06f));

            rightScreen = new GameObject("Right_Monitor_Screen_Canvas");
            rightScreen.transform.SetParent(rightMonitor.transform, false);
            rightScreen.transform.localPosition = new Vector3(0f, 0f, -0.018f);
        }

        private static void CreateInteractiveDeskProps(GameObject parent)
        {
            GameObject propsGroup = new GameObject("Interactive_Desk_Props");
            propsGroup.transform.SetParent(parent.transform, false);

            // 1. Suspicious USB Drive on Desk (Grabbable)
            GameObject usb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            usb.name = "Suspicious_USB_Drive";
            usb.transform.SetParent(propsGroup.transform, false);
            usb.transform.localPosition = new Vector3(-0.35f, 0.76f, 0.45f);
            usb.transform.localScale = new Vector3(0.025f, 0.012f, 0.06f);
            SetObjectColor(usb, new Color(0.85f, 0.2f, 0.2f));
            usb.AddComponent<VRGrabbableObject>();

            // 2. Security Keycard / Badge (Grabbable)
            GameObject badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = "Security_Access_Card";
            badge.transform.SetParent(propsGroup.transform, false);
            badge.transform.localPosition = new Vector3(0.42f, 0.75f, 0.42f);
            badge.transform.localScale = new Vector3(0.06f, 0.004f, 0.09f);
            SetObjectColor(badge, new Color(0.0f, 0.94f, 1.0f));
            badge.AddComponent<VRGrabbableObject>();

            // 3. Ceramic Coffee Mug (Grabbable)
            GameObject mug = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mug.name = "CADT_Coffee_Mug";
            mug.transform.SetParent(propsGroup.transform, false);
            mug.transform.localPosition = new Vector3(-0.55f, 0.80f, 0.55f);
            mug.transform.localScale = new Vector3(0.08f, 0.06f, 0.08f);
            SetObjectColor(mug, new Color(0.9f, 0.92f, 0.95f));
            mug.AddComponent<VRGrabbableObject>();
        }

        private static void CreateDeskLeg(string name, Transform parent, Vector3 localPos)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = name;
            leg.transform.SetParent(parent, false);
            leg.transform.localPosition = localPos;
            leg.transform.localScale = new Vector3(0.05f, 0.35f, 0.05f);
            SetObjectColor(leg, new Color(0.1f, 0.1f, 0.12f));
        }

        private static GameObject CreateMonitor1UI(GameObject screenAnchor, Camera cam)
        {
            Canvas canvas = screenAnchor.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            screenAnchor.AddComponent<GraphicRaycaster>();
            AddTrackedDeviceGraphicRaycaster(screenAnchor);

            RectTransform rect = screenAnchor.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1920f, 1080f);
            rect.localScale = new Vector3(0.00036f, 0.00036f, 0.00036f);

            // Wallpaper Background
            GameObject bg = CreateUIPanel("Win11_Wallpaper", screenAnchor.transform, new Vector2(1920f, 1080f), new Color(0.04f, 0.08f, 0.18f, 1f));

            // Taskbar
            GameObject taskbar = CreateUIPanel("Taskbar", bg.transform, new Vector2(1920f, 65f), new Color(0.08f, 0.11f, 0.18f, 0.95f));
            SetAnchors(taskbar.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 32.5f));

            // Suspicious File Icon on Desktop
            GameObject fileButtonObj = CreateUIPanel("File_Document_Invoice", bg.transform, new Vector2(140f, 160f), new Color(0.12f, 0.18f, 0.28f, 0.85f));
            SetAnchors(fileButtonObj.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(130f, -150f));
            Button fileBtn = fileButtonObj.AddComponent<Button>();

            // Icon Image / Badge
            GameObject iconBadge = CreateUIPanel("PDF_Badge", fileButtonObj.transform, new Vector2(75f, 85f), new Color(0.85f, 0.2f, 0.2f, 1f));
            SetAnchors(iconBadge.GetComponent<RectTransform>(), new Vector2(0.5f, 0.7f), Vector2.zero);
            CreateTMPText("Badge_Text", iconBadge.transform, "PDF", 26, TextAlignmentOptions.Center, Color.white);

            // Icon Name Label
            GameObject labelObj = CreateTMPText("Label_TMP", fileButtonObj.transform, "document_invoice.pdf", 18, TextAlignmentOptions.Center, Color.white);
            SetAnchors(labelObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.15f), Vector2.zero);

            // File Properties Modal Window
            GameObject propModal = CreateUIPanel("File_Properties_Modal", bg.transform, new Vector2(620f, 720f), new Color(0.10f, 0.12f, 0.16f, 0.98f));
            propModal.transform.localPosition = new Vector3(60f, 20f, -2f);
            Outline modalOutline = propModal.AddComponent<Outline>();
            modalOutline.effectColor = new Color(0.0f, 0.94f, 1.0f, 0.8f);
            modalOutline.effectDistance = new Vector2(2f, -2f);

            // Modal Header
            GameObject modalHeader = CreateUIPanel("Header_Bar", propModal.transform, new Vector2(620f, 50f), new Color(0.15f, 0.18f, 0.24f, 1f));
            SetAnchors(modalHeader.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -25f));
            CreateTMPText("Modal_Title", modalHeader.transform, "document_invoice.pdf.exe Properties", 20, TextAlignmentOptions.Left, Color.white, new Vector2(580f, 40f));

            // Content Container
            GameObject content = new GameObject("Content_Container", typeof(RectTransform));
            content.transform.SetParent(propModal.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(580f, 620f);
            contentRect.anchoredPosition = new Vector2(0f, -40f);

            // File Name Box
            GameObject fnBox = CreateUIPanel("FileName_Box", content.transform, new Vector2(560f, 55f), new Color(0.06f, 0.08f, 0.12f, 1f));
            SetAnchors(fnBox.GetComponent<RectTransform>(), new Vector2(0.5f, 0.9f), Vector2.zero);
            CreateTMPText("FileName_TMP", fnBox.transform, "document_invoice.pdf<color=#EF4444><b>.exe</b></color>", 24, TextAlignmentOptions.Left, Color.white, new Vector2(520f, 45f));

            // Extension Warning Highlight Box
            GameObject extHighlight = CreateUIPanel("Extension_Warning_Box", fnBox.transform, new Vector2(110f, 45f), new Color(0.94f, 0.27f, 0.27f, 0.4f));
            SetAnchors(extHighlight.GetComponent<RectTransform>(), new Vector2(0.86f, 0.5f), Vector2.zero);
            Outline extOutline = extHighlight.AddComponent<Outline>();
            extOutline.effectColor = new Color(0.94f, 0.27f, 0.27f, 0.95f);

            // File Type Metadata
            GameObject ftObj = CreateTMPText("FileType_TMP", content.transform,
                "<b>Type of file:</b> <color=#EF4444>Application (.exe)</color>\n" +
                "<b>Description:</b> Disguised Binary Executable\n" +
                "<b>Size:</b> 2.45 MB (2,572,110 bytes)\n" +
                "<b>Location:</b> C:\\Users\\CADT\\Downloads\n" +
                "<b>Digital Signature:</b> <color=#EF4444>Unsigned / Invalid</color>\n" +
                "<b>Security:</b> This file came from another computer and is flagged.",
                20, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.88f, 0.92f), new Vector2(560f, 260f));
            SetAnchors(ftObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.52f), Vector2.zero);

            // Close Button
            GameObject closeBtnObj = CreateUIPanel("Close_Button", propModal.transform, new Vector2(120f, 42f), new Color(0.2f, 0.25f, 0.35f, 1f));
            SetAnchors(closeBtnObj.GetComponent<RectTransform>(), new Vector2(0.86f, 0.07f), Vector2.zero);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            CreateTMPText("Close_Text", closeBtnObj.transform, "OK", 20, TextAlignmentOptions.Center, Color.white);

            // Overlays
            GameObject infectedOverlay = CreateUIPanel("Infection_Alert_Overlay", bg.transform, new Vector2(1920f, 1080f), new Color(0.6f, 0.05f, 0.05f, 0.95f));
            CreateTMPText("Infected_Title", infectedOverlay.transform, "[!] SYSTEM COMPROMISED\n<size=36>Command & Control Payload Executed</size>", 64, TextAlignmentOptions.Center, Color.white);
            infectedOverlay.SetActive(false);

            GameObject securedOverlay = CreateUIPanel("Secured_Overlay", bg.transform, new Vector2(1920f, 1080f), new Color(0.04f, 0.35f, 0.2f, 0.95f));
            CreateTMPText("Secured_Title", securedOverlay.transform, "[OK] THREAT NEUTRALIZED\n<size=36>Payload Quarantined & Reported to SOC</size>", 64, TextAlignmentOptions.Center, Color.white);
            securedOverlay.SetActive(false);

            // Attach Inspector Script
            MonitorFileInspector inspector = screenAnchor.AddComponent<MonitorFileInspector>();
            fileBtn.onClick.AddListener(inspector.ToggleFileProperties);
            closeBtn.onClick.AddListener(inspector.ToggleFileProperties);

            return screenAnchor;
        }

        private static GameObject CreateMonitor2TelegramUI(GameObject screenAnchor, Camera cam)
        {
            Canvas canvas = screenAnchor.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            screenAnchor.AddComponent<GraphicRaycaster>();
            AddTrackedDeviceGraphicRaycaster(screenAnchor);

            RectTransform rect = screenAnchor.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1920f, 1080f);
            rect.localScale = new Vector3(0.00036f, 0.00036f, 0.00036f);

            // 1. Telegram Main Container (Authentic Light Theme)
            GameObject bg = CreateUIPanel("Telegram_Client_Root", screenAnchor.transform, new Vector2(1920f, 1080f), new Color(0.96f, 0.96f, 0.97f, 1f));

            // ----------------------------------------------------
            // 2. LEFT SIDEBAR: WHITE BACKGROUND + CONTACT LIST
            // ----------------------------------------------------
            GameObject sidebar = CreateUIPanel("Chat_Sidebar", bg.transform, new Vector2(620f, 1080f), Color.white);
            SetAnchors(sidebar.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(310f, 0f));

            // Top Search Bar in Sidebar
            GameObject topBar = CreateUIPanel("Top_Search_Bar", sidebar.transform, new Vector2(620f, 65f), Color.white);
            SetAnchors(topBar.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -32.5f));

            // Menu Icon (Hamburger)
            GameObject menuIcon = CreateUIPanel("Menu_Icon", topBar.transform, new Vector2(32f, 24f), new Color(0.55f, 0.6f, 0.65f));
            SetAnchors(menuIcon.GetComponent<RectTransform>(), new Vector2(0.06f, 0.5f), Vector2.zero);

            // Search Box Input Pill
            GameObject searchBox = CreateUIPanel("Search_Box", topBar.transform, new Vector2(500f, 44f), new Color(0.94f, 0.95f, 0.96f, 1f));
            SetAnchors(searchBox.GetComponent<RectTransform>(), new Vector2(0.56f, 0.5f), Vector2.zero);
            CreateTMPText("Search_Placeholder", searchBox.transform, "Search", 18, TextAlignmentOptions.Left, new Color(0.62f, 0.66f, 0.72f), new Vector2(460f, 36f));

            // Contact Items (Matching reference screenshot exactly)
            float startY = 960f;
            float stepY = 100f;

            // 1. Death Star 3.0 (Group)
            CreateAuthenticTelegramContact(sidebar.transform, "Contact_DeathStar",
                "Death Star 3.0", true, "DS", new Color(0.2f, 0.2f, 0.25f),
                "<color=#2481CC>Darth:</color> I'll trust you. For now.", "7:15 PM", false, startY - (stepY * 0));

            // 2. Eva Summer (SELECTED - Vibrant Blue)
            CreateAuthenticTelegramContact(sidebar.transform, "Contact_EvaSummer",
                "Eva Summer", false, "ES", new Color(0.95f, 0.45f, 0.35f), // Coral Red avatar
                "Reminds me of a Chinese prove...", "11:28 PM", true, startY - (stepY * 1));

            // 3. Lena Oxton
            CreateAuthenticTelegramContact(sidebar.transform, "Contact_Lena",
                "Lena Oxton", false, "LO", new Color(0.92f, 0.7f, 0.2f),
                "<color=#2481CC>Sticker</color>", "9:17 PM", false, startY - (stepY * 2), true);

            // 4. Mom
            CreateAuthenticTelegramContact(sidebar.transform, "Contact_Mom",
                "Mom", false, "M", new Color(0.85f, 0.65f, 0.5f),
                "Don't forget your blaster and helmet", "8:02 PM", false, startY - (stepY * 3));

            // 5. Pandas HQ (Group)
            CreateAuthenticTelegramContact(sidebar.transform, "Contact_Pandas",
                "Pandas HQ", true, "PH", new Color(0.2f, 0.75f, 0.4f),
                "<color=#2481CC>Eva: Photo</color>", "1:14 AM", false, startY - (stepY * 4));

            // 6. Old Pirates (Group)
            CreateAuthenticTelegramContact(sidebar.transform, "Contact_Pirates",
                "Old Pirates", true, "OP", new Color(0.18f, 0.5f, 0.75f),
                "<color=#2481CC>Jack:</color> Yo-ho-ho, all aboard!", "0:02 AM", false, startY - (stepY * 5));

            // 7. Max Bright
            CreateAuthenticTelegramContact(sidebar.transform, "Contact_MaxBright",
                "Max Bright", false, "MB", new Color(0.35f, 0.65f, 0.95f), // Sky blue avatar
                "Coffee time?", "Mon", false, startY - (stepY * 6), true);

            // 8. Lee
            CreateAuthenticTelegramContact(sidebar.transform, "Contact_Lee",
                "Lee", false, "L", new Color(0.3f, 0.3f, 0.35f),
                "We can call it Galaxy Star 7 ;)", "Mon", false, startY - (stepY * 7));

            // 9. Alexandra Z
            CreateAuthenticTelegramContact(sidebar.transform, "Contact_Alexandra",
                "Alexandra Z", false, "AZ", new Color(0.75f, 0.35f, 0.75f),
                "<color=#2481CC>Workout_Shedule.pdf</color>", "Mon", false, startY - (stepY * 8));

            // ----------------------------------------------------
            // 3. RIGHT CHAT AREA (Nature Floral Wallpaper + Message Bubbles)
            // ----------------------------------------------------
            GameObject chatArea = CreateUIPanel("Main_Chat_Area", bg.transform, new Vector2(1300f, 1080f), new Color(0.68f, 0.78f, 0.68f, 1f)); // Nature soft green background
            SetAnchors(chatArea.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-650f, 0f));

            // Nature Chat Wallpaper Gradient (Simulating chamomile sunset wallpaper)
            GameObject wallpaperTop = CreateUIPanel("Wallpaper_Sky", chatArea.transform, new Vector2(1300f, 540f), new Color(0.78f, 0.88f, 0.82f, 1f));
            SetAnchors(wallpaperTop.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -270f));

            // Chat Header Bar (Pure White)
            GameObject chatHeader = CreateUIPanel("Chat_Header", chatArea.transform, new Vector2(1300f, 65f), Color.white);
            SetAnchors(chatHeader.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -32.5f));

            // Header Contact Name & Status
            CreateTMPText("Header_Name", chatHeader.transform, "<b>Eva Summer</b>\n<size=15><color=#2481CC>online</color></size>", 19, TextAlignmentOptions.Left, Color.black, new Vector2(600f, 50f));
            SetAnchors(chatHeader.transform.Find("Header_Name").GetComponent<RectTransform>(), new Vector2(0.25f, 0.5f), Vector2.zero);

            // Header Action Icons
            CreateTMPText("Header_Icons", chatHeader.transform, "[Search]   [Menu]", 17, TextAlignmentOptions.Right, new Color(0.55f, 0.6f, 0.65f), new Vector2(200f, 30f));
            SetAnchors(chatHeader.transform.Find("Header_Icons").GetComponent<RectTransform>(), new Vector2(0.90f, 0.5f), Vector2.zero);

            // ----------------------------------------------------
            // 4. AUTHENTIC MESSAGE THREAD (Incoming White / Outgoing Green)
            // ----------------------------------------------------
            // 1. Incoming Photo Card (Sunrise Landscape)
            GameObject photoBubble = CreateUIPanel("Photo_Message_Bubble", chatArea.transform, new Vector2(560f, 180f), Color.white);
            SetAnchors(photoBubble.GetComponent<RectTransform>(), new Vector2(0.24f, 0.83f), Vector2.zero);
            
            // Photo Image Box
            GameObject photoImg = CreateUIPanel("Landscape_Photo", photoBubble.transform, new Vector2(540f, 120f), new Color(0.92f, 0.65f, 0.45f));
            SetAnchors(photoImg.GetComponent<RectTransform>(), new Vector2(0.5f, 0.58f), Vector2.zero);
            CreateTMPText("SunText", photoImg.transform, "CADT Campus Sunrise Panorama", 16, TextAlignmentOptions.Center, Color.white);

            CreateTMPText("Photo_Caption", photoBubble.transform, "I finally visited Earth.. The nature here is fantastic!  <size=14><color=#8E8E93>11:23 PM</color></size>", 16, TextAlignmentOptions.Left, new Color(0.1f, 0.1f, 0.12f), new Vector2(530f, 35f));
            SetAnchors(photoBubble.transform.Find("Photo_Caption").GetComponent<RectTransform>(), new Vector2(0.5f, 0.12f), Vector2.zero);

            // 2. Outgoing Voice Note Bubble (Green #EEFFDE)
            GameObject voiceBubble = CreateUIPanel("Voice_Message_Bubble", chatArea.transform, new Vector2(400f, 75f), new Color(0.93f, 1f, 0.87f, 1f));
            SetAnchors(voiceBubble.GetComponent<RectTransform>(), new Vector2(0.82f, 0.70f), Vector2.zero);

            GameObject playBtn = CreateUIPanel("Play_Circle", voiceBubble.transform, new Vector2(48f, 48f), new Color(0.31f, 0.68f, 0.31f, 1f));
            SetAnchors(playBtn.GetComponent<RectTransform>(), new Vector2(0.1f, 0.5f), Vector2.zero);
            CreateTMPText("Play_Icon", playBtn.transform, ">", 22, TextAlignmentOptions.Center, Color.white);

            CreateTMPText("Voice_Wave", voiceBubble.transform, "||||||||||||||||||||||||||||||||||||||||\n00:07                          <size=14><color=#4FAE4E>11:24 PM [read]</color></size>", 15, TextAlignmentOptions.Left, new Color(0.31f, 0.68f, 0.31f), new Vector2(300f, 50f));
            SetAnchors(voiceBubble.transform.Find("Voice_Wave").GetComponent<RectTransform>(), new Vector2(0.58f, 0.5f), Vector2.zero);

            // 3. INCOMING MALICIOUS ATTACHMENT CARD (Matches PDF file bubble in screenshot)
            GameObject attachBubble = CreateUIPanel("Chat_Message_Bubble", chatArea.transform, new Vector2(480f, 95f), Color.white);
            SetAnchors(attachBubble.GetComponent<RectTransform>(), new Vector2(0.21f, 0.58f), Vector2.zero);

            // Circular Blue Download Icon (Arrow Down)
            GameObject dlCircle = CreateUIPanel("DL_Circle", attachBubble.transform, new Vector2(58f, 58f), new Color(0.2f, 0.56f, 0.93f, 1f));
            SetAnchors(dlCircle.GetComponent<RectTransform>(), new Vector2(0.1f, 0.5f), Vector2.zero);
            CreateTMPText("Arrow_Down", dlCircle.transform, "↓", 28, TextAlignmentOptions.Center, Color.white);
            Button dlBtn = dlCircle.AddComponent<Button>();

            // File Name & Size
            CreateTMPText("Attach_Text", attachBubble.transform, "<b>Staff_Bonus_List_2026.rar</b>\n<size=15><color=#8E8E93>4.2 MB</color></size>                                        <size=14><color=#8E8E93>11:27 PM</color></size>", 18, TextAlignmentOptions.Left, Color.black, new Vector2(380f, 65f));
            SetAnchors(attachBubble.transform.Find("Attach_Text").GetComponent<RectTransform>(), new Vector2(0.56f, 0.5f), Vector2.zero);

            // 4. Outgoing Long Quote Bubble (Green)
            GameObject quoteBubble = CreateUIPanel("Outgoing_Long_Msg", chatArea.transform, new Vector2(580f, 130f), new Color(0.93f, 1f, 0.87f, 1f));
            SetAnchors(quoteBubble.GetComponent<RectTransform>(), new Vector2(0.75f, 0.44f), Vector2.zero);
            CreateTMPText("Quote_Text", quoteBubble.transform,
                "Twenty years from now you will be more disappointed by the things that you didn't do than by the ones you did do, so throw off the bowlines, sail away from safe harbor, catch the trade winds in your sails.\n                                                         <size=14><color=#4FAE4E>11:28 PM [read]</color></size>",
                16, TextAlignmentOptions.TopLeft, new Color(0.1f, 0.1f, 0.12f), new Vector2(550f, 115f));

            // Outgoing Short Note
            GameObject shortNote = CreateUIPanel("Short_Note", chatArea.transform, new Vector2(340f, 48f), new Color(0.93f, 1f, 0.87f, 1f));
            SetAnchors(shortNote.GetComponent<RectTransform>(), new Vector2(0.85f, 0.32f), Vector2.zero);
            CreateTMPText("SN_Text", shortNote.transform, "Mark Twain said that [!]     <size=14><color=#4FAE4E>11:28 PM [read]</color></size>", 16, TextAlignmentOptions.Left, new Color(0.1f, 0.1f, 0.12f), new Vector2(310f, 35f));

            // 5. Incoming Quoted Reply Bubble (White with Blue Line)
            GameObject replyBubble = CreateUIPanel("Reply_Bubble", chatArea.transform, new Vector2(560f, 150f), Color.white);
            SetAnchors(replyBubble.GetComponent<RectTransform>(), new Vector2(0.24f, 0.19f), Vector2.zero);

            // Blue Quote Bar
            GameObject quoteBar = CreateUIPanel("Quote_Bar", replyBubble.transform, new Vector2(4f, 45f), new Color(0.2f, 0.56f, 0.93f, 1f));
            SetAnchors(quoteBar.GetComponent<RectTransform>(), new Vector2(0.04f, 0.72f), Vector2.zero);

            CreateTMPText("Quoted_Part", replyBubble.transform, "<color=#2481CC><b>Mary Sue</b></color>\n<size=14><color=#8E8E93>Twenty years from now you will be more disappointed by t...</color></size>", 15, TextAlignmentOptions.Left, Color.black, new Vector2(490f, 45f));
            SetAnchors(replyBubble.transform.Find("Quoted_Part").GetComponent<RectTransform>(), new Vector2(0.52f, 0.72f), Vector2.zero);

            CreateTMPText("Reply_Body", replyBubble.transform, "Reminds me of a Chinese proverb: the best time to plant a tree was 20 years ago. The second best time is now.   <size=14><color=#8E8E93>11:28 PM</color></size>", 16, TextAlignmentOptions.TopLeft, new Color(0.1f, 0.1f, 0.12f), new Vector2(510f, 65f));
            SetAnchors(replyBubble.transform.Find("Reply_Body").GetComponent<RectTransform>(), new Vector2(0.5f, 0.28f), Vector2.zero);

            // ----------------------------------------------------
            // 5. BOTTOM INPUT BAR (Pure White with Icons)
            // ----------------------------------------------------
            GameObject inputBar = CreateUIPanel("Message_Input_Bar", chatArea.transform, new Vector2(1300f, 65f), Color.white);
            SetAnchors(inputBar.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 32.5f));

            // Paperclip Icon
            CreateTMPText("Clip_Icon", inputBar.transform, "[Attach]", 18, TextAlignmentOptions.Center, new Color(0.55f, 0.6f, 0.65f), new Vector2(80f, 40f));
            SetAnchors(inputBar.transform.Find("Clip_Icon").GetComponent<RectTransform>(), new Vector2(0.04f, 0.5f), Vector2.zero);

            // Placeholder Text
            CreateTMPText("Input_Placeholder", inputBar.transform, "Write a message...", 18, TextAlignmentOptions.Left, new Color(0.62f, 0.66f, 0.72f), new Vector2(900f, 40f));
            SetAnchors(inputBar.transform.Find("Input_Placeholder").GetComponent<RectTransform>(), new Vector2(0.44f, 0.5f), Vector2.zero);

            // Emoji & Mic Icons
            CreateTMPText("Right_Icons", inputBar.transform, "[Emoji]    [Mic]", 18, TextAlignmentOptions.Right, new Color(0.55f, 0.6f, 0.65f), new Vector2(160f, 40f));
            SetAnchors(inputBar.transform.Find("Right_Icons").GetComponent<RectTransform>(), new Vector2(0.92f, 0.5f), Vector2.zero);

            // ----------------------------------------------------
            // 6. SCENARIO OVERLAYS (Quarantine & Compromised)
            // ----------------------------------------------------
            GameObject quarantineBanner = CreateUIPanel("Quarantine_Banner", bg.transform, new Vector2(1300f, 110f), new Color(0.06f, 0.4f, 0.25f, 0.96f));
            SetAnchors(quarantineBanner.GetComponent<RectTransform>(), new Vector2(0.66f, 0.12f), Vector2.zero);
            CreateTMPText("Q_Text", quarantineBanner.transform, "[LOCKED] SENDER BLOCKED & ARCHIVE QUARANTINED BY CADT SOC", 28, TextAlignmentOptions.Center, Color.white);
            quarantineBanner.SetActive(false);

            GameObject compBanner = CreateUIPanel("Compromised_Banner", bg.transform, new Vector2(1300f, 110f), new Color(0.65f, 0.1f, 0.1f, 0.96f));
            SetAnchors(compBanner.GetComponent<RectTransform>(), new Vector2(0.66f, 0.12f), Vector2.zero);
            CreateTMPText("C_Text", compBanner.transform, "[!] DATA EXFILTRATION IN PROGRESS: CREDENTIALS STOLEN", 28, TextAlignmentOptions.Center, Color.white);
            compBanner.SetActive(false);

            return screenAnchor;
        }

        private static void CreateAuthenticTelegramContact(Transform parent, string name, string contactName, bool isGroup, string avatarInitials, Color avatarColor, string snippet, string time, bool isSelected, float posY, bool hasCheckmark = false)
        {
            GameObject contact = CreateUIPanel(name, parent, new Vector2(600f, 85f), isSelected ? new Color(0.2f, 0.56f, 0.93f, 1f) : Color.white);
            SetAnchors(contact.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, posY));

            // Avatar Circle
            GameObject avatar = CreateUIPanel("Avatar", contact.transform, new Vector2(56f, 56f), avatarColor);
            SetAnchors(avatar.GetComponent<RectTransform>(), new Vector2(0.07f, 0.5f), Vector2.zero);
            if (!string.IsNullOrEmpty(avatarInitials))
            {
                CreateTMPText("Initials", avatar.transform, avatarInitials, 22, TextAlignmentOptions.Center, Color.white);
            }

            // Name
            CreateTMPText("Contact_Name", contact.transform, (isGroup ? "[Grp] " : "") + "<b>" + contactName + "</b>", 18, TextAlignmentOptions.Left, isSelected ? Color.white : new Color(0.12f, 0.12f, 0.14f), new Vector2(360f, 26f));
            SetAnchors(contact.transform.Find("Contact_Name").GetComponent<RectTransform>(), new Vector2(0.44f, 0.68f), Vector2.zero);

            // Message Snippet
            CreateTMPText("Snippet", contact.transform, snippet, 16, TextAlignmentOptions.Left, isSelected ? new Color(0.9f, 0.95f, 1f) : new Color(0.55f, 0.58f, 0.64f), new Vector2(360f, 24f));
            SetAnchors(contact.transform.Find("Snippet").GetComponent<RectTransform>(), new Vector2(0.44f, 0.32f), Vector2.zero);

            // Time & Read Status
            string timeStr = hasCheckmark ? "<color=#4FAE4E>[read]</color> " + time : time;
            CreateTMPText("Time", contact.transform, timeStr, 15, TextAlignmentOptions.Right, isSelected ? new Color(0.9f, 0.95f, 1f) : new Color(0.55f, 0.58f, 0.64f), new Vector2(120f, 24f));
            SetAnchors(contact.transform.Find("Time").GetComponent<RectTransform>(), new Vector2(0.86f, 0.68f), Vector2.zero);
        }

        private static GameObject CreateHolographicDashboard(GameObject parent, Material holoMat, Camera cam)
        {
            GameObject dashObj = new GameObject("Diegetic_Spatial_Dashboard");
            dashObj.transform.SetParent(parent.transform, false);
            dashObj.transform.localPosition = new Vector3(0f, 1.18f, 0.52f);
            dashObj.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);

            Canvas canvas = dashObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            dashObj.AddComponent<GraphicRaycaster>();
            AddTrackedDeviceGraphicRaycaster(dashObj);

            RectTransform rect = dashObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(960f, 440f);
            rect.localScale = new Vector3(0.00045f, 0.00045f, 0.00045f);

            // Audio Source
            AudioSource audioSource = dashObj.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.playOnAwake = false;

            // Background Holographic Panel
            GameObject bgPanel = CreateUIPanel("Holographic_Backdrop", dashObj.transform, new Vector2(960f, 440f), new Color(0.06f, 0.09f, 0.16f, 0.65f));
            Image bgImage = bgPanel.GetComponent<Image>();
            if (holoMat != null) bgImage.material = holoMat;

            Outline outline = bgPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.0f, 0.94f, 1.0f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            // Top Header
            GameObject headerObj = CreateTMPText("Header_TMP", dashObj.transform, "PHASE 2: Spear-Phishing & Extension Spoofing Detection", 24, TextAlignmentOptions.Center, new Color(0.0f, 0.94f, 1.0f), new Vector2(920f, 45f));
            SetAnchors(headerObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.88f), Vector2.zero);

            // Status Feedback Bar
            GameObject statusObj = CreateTMPText("Status_Feedback_TMP", dashObj.transform, "<color=#94A3B8>STATUS:</color> Inspect suspicious files on desktop monitors before making a triage decision.", 18, TextAlignmentOptions.Center, Color.white, new Vector2(880f, 40f));
            SetAnchors(statusObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.74f), Vector2.zero);

            // Timeline Slider
            GameObject sliderObj = CreateSlider("Timeline_Slider", dashObj.transform, new Vector2(840f, 20f));
            SetAnchors(sliderObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.62f), Vector2.zero);

            // Button Container
            GameObject btnContainer = new GameObject("Branch_Buttons_Container", typeof(RectTransform));
            btnContainer.transform.SetParent(dashObj.transform, false);
            RectTransform btnRect = btnContainer.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(880f, 160f);
            btnRect.anchoredPosition = new Vector2(0f, -70f);

            // Branch A Button (Fail / Threat Path)
            GameObject branchA = CreateUIPanel("Branch_A_Button", btnContainer.transform, new Vector2(400f, 140f), new Color(0.85f, 0.47f, 0.02f, 0.85f));
            SetAnchors(branchA.GetComponent<RectTransform>(), new Vector2(0.26f, 0.5f), Vector2.zero);
            Button btnA = branchA.AddComponent<Button>();
            Outline outlineA = branchA.AddComponent<Outline>();
            outlineA.effectColor = new Color(0.94f, 0.27f, 0.27f, 0.9f);
            CreateTMPText("BtnA_Text", branchA.transform, "<b>[BRANCH A]</b>\nExecute File / Dismiss Alert\n\n<size=14><color=#FECACA>Assume trusted document • Open payload</color></size>", 17, TextAlignmentOptions.Center, Color.white, new Vector2(360f, 110f));
            SpatialUIHoverEffect hoverA = branchA.AddComponent<SpatialUIHoverEffect>();
            SetHoverEffectField(hoverA, "theme", SpatialUIHoverEffect.UIThemeType.BranchA_FailThreat);

            // Branch B Button (Success / Triage Path)
            GameObject branchB = CreateUIPanel("Branch_B_Button", btnContainer.transform, new Vector2(400f, 140f), new Color(0.0f, 0.75f, 0.85f, 0.85f));
            SetAnchors(branchB.GetComponent<RectTransform>(), new Vector2(0.74f, 0.5f), Vector2.zero);
            Button btnB = branchB.AddComponent<Button>();
            Outline outlineB = branchB.AddComponent<Outline>();
            outlineB.effectColor = new Color(0.06f, 0.72f, 0.51f, 0.9f);
            CreateTMPText("BtnB_Text", branchB.transform, "<b>[BRANCH B]</b>\nQuarantine & Report Threat\n\n<size=14><color=#A7F3D0>Flag disguised .exe • Report to SOC</color></size>", 17, TextAlignmentOptions.Center, Color.white, new Vector2(360f, 110f));
            SpatialUIHoverEffect hoverB = branchB.AddComponent<SpatialUIHoverEffect>();
            SetHoverEffectField(hoverB, "theme", SpatialUIHoverEffect.UIThemeType.BranchB_SuccessTriage);

            // Attach Manager Script
            VRBranchManager manager = dashObj.AddComponent<VRBranchManager>();

            return dashObj;
        }

        private static void WireDependencies(GameObject dashObj, GameObject mon1, GameObject mon2, AudioClip successClip, AudioClip failClip, AudioClip clickClip, AudioClip inspectClip)
        {
            VRBranchManager manager = dashObj.GetComponent<VRBranchManager>();
            SerializedObject so = new SerializedObject(manager);

            so.FindProperty("headerText").objectReferenceValue = dashObj.transform.Find("Header_TMP")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("statusFeedbackText").objectReferenceValue = dashObj.transform.Find("Status_Feedback_TMP")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("timelineSlider").objectReferenceValue = dashObj.transform.Find("Timeline_Slider")?.GetComponent<Slider>();

            Transform btnContainer = dashObj.transform.Find("Branch_Buttons_Container");
            so.FindProperty("branchAButton").objectReferenceValue = btnContainer?.Find("Branch_A_Button")?.GetComponent<Button>();
            so.FindProperty("branchBButton").objectReferenceValue = btnContainer?.Find("Branch_B_Button")?.GetComponent<Button>();

            // Monitor 1 References
            so.FindProperty("monitor1RootCanvas").objectReferenceValue = mon1;
            Transform win11Bg = mon1.transform.Find("Win11_Wallpaper");
            so.FindProperty("monitor1FilePropertiesWindow").objectReferenceValue = win11Bg?.Find("File_Properties_Modal")?.gameObject;
            so.FindProperty("monitor1InfectionAlertOverlay").objectReferenceValue = win11Bg?.Find("Infection_Alert_Overlay")?.gameObject;
            so.FindProperty("monitor1SecuredOverlay").objectReferenceValue = win11Bg?.Find("Secured_Overlay")?.gameObject;

            // Monitor 2 References
            so.FindProperty("monitor2RootCanvas").objectReferenceValue = mon2;
            Transform tgBg = mon2.transform.Find("Telegram_Client_Root");
            Transform chatArea = tgBg?.Find("Main_Chat_Area");
            so.FindProperty("monitor2ChatThread").objectReferenceValue = chatArea?.Find("Chat_Message_Bubble")?.gameObject;
            so.FindProperty("monitor2QuarantineBanner").objectReferenceValue = tgBg?.Find("Quarantine_Banner")?.gameObject;
            so.FindProperty("monitor2CompromisedBanner").objectReferenceValue = tgBg?.Find("Compromised_Banner")?.gameObject;

            // Audio clips
            so.FindProperty("spatialAudioSource").objectReferenceValue = dashObj.GetComponent<AudioSource>();
            so.FindProperty("successAudioClip").objectReferenceValue = successClip;
            so.FindProperty("failureAudioClip").objectReferenceValue = failClip;
            so.FindProperty("inspectAudioClip").objectReferenceValue = inspectClip;

            so.ApplyModifiedProperties();

            // Wire Monitor 1 Inspector
            MonitorFileInspector inspector = mon1.GetComponent<MonitorFileInspector>();
            if (inspector != null)
            {
                SerializedObject soInsp = new SerializedObject(inspector);
                soInsp.FindProperty("branchManager").objectReferenceValue = manager;
                Transform modal = win11Bg?.Find("File_Properties_Modal");
                soInsp.FindProperty("filePropertiesModal").objectReferenceValue = modal?.gameObject;
                Transform content = modal?.Find("Content_Container");
                soInsp.FindProperty("fileNameText").objectReferenceValue = content?.Find("FileName_Box/FileName_TMP")?.GetComponent<TextMeshProUGUI>();
                soInsp.FindProperty("fileTypeText").objectReferenceValue = content?.Find("FileType_TMP")?.GetComponent<TextMeshProUGUI>();
                soInsp.FindProperty("extensionWarningHighlight").objectReferenceValue = content?.Find("FileName_Box/Extension_Warning_Box")?.gameObject;
                soInsp.FindProperty("threatDiscoveredSound").objectReferenceValue = inspectClip;
                soInsp.FindProperty("openPropertiesSound").objectReferenceValue = clickClip;
                soInsp.FindProperty("closePropertiesSound").objectReferenceValue = clickClip;
                soInsp.ApplyModifiedProperties();
            }
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
            slider.value = 0.35f;

            // Background
            GameObject bg = CreateUIPanel("Background", sliderObj.transform, size, new Color(0.08f, 0.12f, 0.18f, 0.9f));
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Fill Area
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

        private static void SetHoverEffectField(SpatialUIHoverEffect effect, string fieldName, SpatialUIHoverEffect.UIThemeType theme)
        {
            SerializedObject so = new SerializedObject(effect);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.enumValueIndex = (int)theme;
                so.ApplyModifiedProperties();
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
