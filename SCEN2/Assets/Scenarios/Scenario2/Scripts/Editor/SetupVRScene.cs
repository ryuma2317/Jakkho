using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
#if UNITY_EDITOR
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Management;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace Kannko.Editor
{
    [InitializeOnLoad]
    public static class SetupVRScene
    {
        static SetupVRScene()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists("Assets/Scenarios/Scenario2/Scenes/Scenario2.unity"))
                {
                    BuildVRScene();
                }
            };
        }

        [MenuItem("VR Prototype/Setup Seated Coffee Shop Scene", priority = 1)]
        public static void BuildVRScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[VR Setup] Cannot build scene during Play Mode.");
                return;
            }
            Debug.Log("[VR Setup] Starting VR Environment & Teleportation Setup...");

            // 1. Ensure XR Management & OpenXR are enabled
            ConfigureXRSettings();

            // 2. Ensure Directories
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            // 3. Create or get standard materials
            Material floorMat = GetOrCreateMaterial("Assets/Materials/M_Floor.mat", new Color(0.18f, 0.20f, 0.23f));
            Material tableMat = GetOrCreateMaterial("Assets/Materials/M_WoodTable.mat", new Color(0.72f, 0.53f, 0.35f));
            Material chairMat = GetOrCreateMaterial("Assets/Materials/M_Chair.mat", new Color(0.28f, 0.32f, 0.36f));
            Material mugMat = GetOrCreateMaterial("Assets/Materials/M_CoffeeMug.mat", new Color(0.95f, 0.95f, 0.92f));
            Material controllerMat = GetOrCreateMaterial("Assets/Materials/M_Controller.mat", new Color(0.15f, 0.6f, 0.85f, 0.8f));
            Material counterMat = GetOrCreateMaterial("Assets/Materials/M_Counter.mat", new Color(0.45f, 0.32f, 0.22f));
            Material marbleMat = GetOrCreateMaterial("Assets/Materials/M_Marble.mat", new Color(0.92f, 0.92f, 0.90f));
            Material metalMat = GetOrCreateMaterial("Assets/Materials/M_MetalDark.mat", new Color(0.25f, 0.27f, 0.30f));
            Material telePadMat = GetOrCreateMaterial("Assets/Materials/M_TeleportPad.mat", new Color(0.18f, 0.65f, 0.75f, 0.5f));
            Material telePadSeatedMat = GetOrCreateMaterial("Assets/Materials/M_TeleportPadSeated.mat", new Color(0.85f, 0.55f, 0.2f, 0.6f));
            Material cardMat = GetOrCreateMaterial("Assets/Materials/M_InstructionCard.mat", new Color(0.96f, 0.94f, 0.88f));
            Material cupPaperMat = GetOrCreateMaterial("Assets/Materials/M_CoffeeCup_Paper.mat", new Color(0.92f, 0.88f, 0.82f));
            Material cupSleeveMat = GetOrCreateMaterial("Assets/Materials/M_CoffeeCup_Sleeve.mat", new Color(0.68f, 0.48f, 0.32f));
            Material cupLidMat = GetOrCreateMaterial("Assets/Materials/M_CoffeeCup_Lid.mat", new Color(0.18f, 0.20f, 0.22f));

            // 4. Create New Scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 5. Lighting & Ambient
            GameObject dirLightGo = new GameObject("Directional Light");
            Light dirLight = dirLightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1.0f, 0.96f, 0.88f); // Warm sunlight
            dirLight.intensity = 1.15f;
            dirLight.shadows = LightShadows.Soft;
            dirLightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.40f, 0.44f);

            // 6. Test Floor (Solid ground, no teleportation component directly on main floor to enforce safe zones)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2f, 1f, 2f); // 20m x 20m floor
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // 7. Seated Environment Blockout (Table, Chair & Instruction Stand)
            GameObject envParent = new GameObject("Environment");

            // Coffee Table (Standard Height = 0.75m)
            GameObject tableParent = new GameObject("CoffeeShop_Table");
            tableParent.transform.SetParent(envParent.transform, false);
            tableParent.transform.position = new Vector3(0f, 0f, 0.65f); // 0.65m in front of seated player

            // Table top
            GameObject tableTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableTop.name = "TableTop";
            tableTop.transform.SetParent(tableParent.transform, false);
            tableTop.transform.localPosition = new Vector3(0f, 0.725f, 0f);
            tableTop.transform.localScale = new Vector3(0.9f, 0.05f, 0.6f);
            tableTop.GetComponent<Renderer>().sharedMaterial = tableMat;

            // Table legs
            CreateLeg(tableParent.transform, new Vector3(-0.4f, 0.35f, -0.25f), new Vector3(0.04f, 0.7f, 0.04f), tableMat);
            CreateLeg(tableParent.transform, new Vector3(0.4f, 0.35f, -0.25f), new Vector3(0.04f, 0.7f, 0.04f), tableMat);
            CreateLeg(tableParent.transform, new Vector3(-0.4f, 0.35f, 0.25f), new Vector3(0.04f, 0.7f, 0.04f), tableMat);
            CreateLeg(tableParent.transform, new Vector3(0.4f, 0.35f, 0.25f), new Vector3(0.04f, 0.7f, 0.04f), tableMat);

            // Tabletop Instruction Stand
            GameObject cardStand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cardStand.name = "Instruction_Stand";
            cardStand.transform.SetParent(tableParent.transform, false);
            cardStand.transform.localPosition = new Vector3(-0.25f, 0.82f, 0.05f);
            cardStand.transform.localScale = new Vector3(0.22f, 0.15f, 0.015f);
            cardStand.transform.localRotation = Quaternion.Euler(-15f, 15f, 0f);
            cardStand.GetComponent<Renderer>().sharedMaterial = cardMat;

            GameObject textGo = new GameObject("Instruction_Text");
            textGo.transform.SetParent(cardStand.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            textGo.transform.localRotation = Quaternion.identity;
            TextMesh textMesh = textGo.AddComponent<TextMesh>();
            textMesh.text = "☕ 24/7 BEACHSIDE CAFE\n\n• Point / Reach to Grab Coffee Cup\n• Hold Trigger/Grip to Move Cup\n• Release to set down on Table\n• Aim Ray at floor pads to Teleport";
            textMesh.fontSize = 22;
            textMesh.characterSize = 0.0065f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = new Color(0.15f, 0.15f, 0.18f);

            // Seated Chair (Seat height = 0.45m)
            GameObject chairParent = new GameObject("CoffeeShop_Chair");
            chairParent.transform.SetParent(envParent.transform, false);
            chairParent.transform.position = Vector3.zero;

            // Chair Seat
            GameObject chairSeat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chairSeat.name = "ChairSeat";
            chairSeat.transform.SetParent(chairParent.transform, false);
            chairSeat.transform.localPosition = new Vector3(0f, 0.425f, 0f);
            chairSeat.transform.localScale = new Vector3(0.45f, 0.05f, 0.45f);
            chairSeat.GetComponent<Renderer>().sharedMaterial = chairMat;

            // Chair Backrest
            GameObject chairBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chairBack.name = "ChairBackrest";
            chairBack.transform.SetParent(chairParent.transform, false);
            chairBack.transform.localPosition = new Vector3(0f, 0.7f, -0.2f);
            chairBack.transform.localScale = new Vector3(0.45f, 0.5f, 0.04f);
            chairBack.GetComponent<Renderer>().sharedMaterial = chairMat;

            // Chair legs
            CreateLeg(chairParent.transform, new Vector3(-0.2f, 0.2f, -0.2f), new Vector3(0.03f, 0.4f, 0.03f), chairMat);
            CreateLeg(chairParent.transform, new Vector3(0.2f, 0.2f, -0.2f), new Vector3(0.03f, 0.4f, 0.03f), chairMat);
            CreateLeg(chairParent.transform, new Vector3(-0.2f, 0.2f, 0.2f), new Vector3(0.03f, 0.4f, 0.03f), chairMat);
            CreateLeg(chairParent.transform, new Vector3(0.2f, 0.2f, 0.2f), new Vector3(0.03f, 0.4f, 0.03f), chairMat);

            // Café Order Counter Area Blockout
            GameObject counterParent = new GameObject("Cafe_OrderCounter");
            counterParent.transform.SetParent(envParent.transform, false);
            counterParent.transform.position = new Vector3(2.5f, 0f, 2.0f);

            // Counter Base
            GameObject counterBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counterBase.name = "CounterBase";
            counterBase.transform.SetParent(counterParent.transform, false);
            counterBase.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            counterBase.transform.localScale = new Vector3(0.8f, 0.9f, 2.4f);
            counterBase.GetComponent<Renderer>().sharedMaterial = counterMat;

            // Counter Top
            GameObject counterTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counterTop.name = "CounterTop";
            counterTop.transform.SetParent(counterParent.transform, false);
            counterTop.transform.localPosition = new Vector3(0f, 0.925f, 0f);
            counterTop.transform.localScale = new Vector3(0.9f, 0.05f, 2.5f);
            counterTop.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // Espresso Machine Placeholder
            GameObject espressoMachine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            espressoMachine.name = "EspressoMachine_Placeholder";
            espressoMachine.transform.SetParent(counterParent.transform, false);
            espressoMachine.transform.localPosition = new Vector3(0.1f, 1.15f, 0.6f);
            espressoMachine.transform.localScale = new Vector3(0.45f, 0.4f, 0.55f);
            espressoMachine.GetComponent<Renderer>().sharedMaterial = metalMat;

            // Register / iPad Placeholder
            GameObject register = GameObject.CreatePrimitive(PrimitiveType.Cube);
            register.name = "Register_Placeholder";
            register.transform.SetParent(counterParent.transform, false);
            register.transform.localPosition = new Vector3(-0.05f, 1.02f, -0.4f);
            register.transform.localScale = new Vector3(0.25f, 0.15f, 0.25f);
            register.transform.localRotation = Quaternion.Euler(20f, -25f, 0f);
            register.GetComponent<Renderer>().sharedMaterial = metalMat;

            // Beachside View / Window Lounge Area Blockout
            GameObject loungeParent = new GameObject("Beachside_Lounge");
            loungeParent.transform.SetParent(envParent.transform, false);
            loungeParent.transform.position = new Vector3(-2.8f, 0f, 2.5f);

            // Modern Railing / Glass Partition
            GameObject railing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railing.name = "OceanView_Railing";
            railing.transform.SetParent(loungeParent.transform, false);
            railing.transform.localPosition = new Vector3(-0.8f, 0.55f, 0f);
            railing.transform.localScale = new Vector3(0.06f, 1.1f, 3.2f);
            railing.GetComponent<Renderer>().sharedMaterial = metalMat;

            // Bistro Standing Table
            GameObject bistroTable = new GameObject("Bistro_Table");
            bistroTable.transform.SetParent(loungeParent.transform, false);
            bistroTable.transform.localPosition = new Vector3(0.2f, 0f, 0.7f);

            GameObject bistroTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bistroTop.name = "BistroTop";
            bistroTop.transform.SetParent(bistroTable.transform, false);
            bistroTop.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            bistroTop.transform.localScale = new Vector3(0.6f, 0.03f, 0.6f);
            bistroTop.GetComponent<Renderer>().sharedMaterial = tableMat;

            CreateLeg(bistroTable.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.08f, 1.0f, 0.08f), metalMat);

            // 8. XR Interaction Manager
            GameObject interactionManagerGo = new GameObject("XR Interaction Manager");
            XRInteractionManager interactionManager = interactionManagerGo.AddComponent<XRInteractionManager>();

            // 8b. Interactive Takeaway Coffee Cup (Modular Grabbable Object)
            GameObject cupRoot = new GameObject("CoffeeCup_Takeaway");
            cupRoot.transform.SetParent(envParent.transform, false);
            cupRoot.transform.position = new Vector3(0.04f, 0.75f, 0.58f); // Resting on coffee table top (y=0.75m) in front of seated player

            // Physics Components on Root
            CapsuleCollider cupCollider = cupRoot.AddComponent<CapsuleCollider>();
            cupCollider.center = new Vector3(0f, 0.065f, 0f);
            cupCollider.radius = 0.042f;
            cupCollider.height = 0.13f;
            cupCollider.direction = 1; // Y-axis

            Rigidbody cupRb = cupRoot.AddComponent<Rigidbody>();
            cupRb.mass = 0.25f;
            cupRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            cupRb.interpolation = RigidbodyInterpolation.Interpolate;
            cupRb.linearDamping = 0.5f;
            cupRb.angularDamping = 1.0f;
            cupRb.centerOfMass = new Vector3(0f, -0.02f, 0f); // Low center of gravity for stable tabletop placement

            // Attach Point for comfortable hand alignment in VR
            GameObject attachPoint = new GameObject("Attach_Point");
            attachPoint.transform.SetParent(cupRoot.transform, false);
            attachPoint.transform.localPosition = new Vector3(0f, 0.055f, 0f); // Grip around the sleeve
            attachPoint.transform.localRotation = Quaternion.identity;

            // Standard XR Grab Interactable
            XRGrabInteractable cupGrab = cupRoot.AddComponent<XRGrabInteractable>();
            cupGrab.interactionManager = interactionManager;
            cupGrab.attachTransform = attachPoint.transform;
            cupGrab.throwOnDetach = true;
            cupGrab.smoothPosition = true;
            cupGrab.smoothRotation = true;

            // Modular Visuals Hierarchy (Easily replaceable with custom 3D mesh later)
            GameObject visualsParent = new GameObject("Visuals");
            visualsParent.transform.SetParent(cupRoot.transform, false);

            // Cup Paper Body
            GameObject cupBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cupBody.name = "Cup_Body";
            cupBody.transform.SetParent(visualsParent.transform, false);
            cupBody.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            cupBody.transform.localScale = new Vector3(0.076f, 0.055f, 0.076f);
            if (cupBody.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(cupBody.GetComponent<Collider>());
            cupBody.GetComponent<Renderer>().sharedMaterial = cupPaperMat;

            // Cardboard Sleeve
            GameObject cupSleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cupSleeve.name = "Cup_Sleeve";
            cupSleeve.transform.SetParent(visualsParent.transform, false);
            cupSleeve.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            cupSleeve.transform.localScale = new Vector3(0.082f, 0.028f, 0.082f);
            if (cupSleeve.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(cupSleeve.GetComponent<Collider>());
            cupSleeve.GetComponent<Renderer>().sharedMaterial = cupSleeveMat;

            // Plastic Lid Base
            GameObject cupLid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cupLid.name = "Cup_Lid";
            cupLid.transform.SetParent(visualsParent.transform, false);
            cupLid.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            cupLid.transform.localScale = new Vector3(0.085f, 0.008f, 0.085f);
            if (cupLid.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(cupLid.GetComponent<Collider>());
            cupLid.GetComponent<Renderer>().sharedMaterial = cupLidMat;

            // Raised Lid Top
            GameObject cupLidTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cupLidTop.name = "Cup_Lid_Top";
            cupLidTop.transform.SetParent(visualsParent.transform, false);
            cupLidTop.transform.localPosition = new Vector3(0f, 0.126f, 0f);
            cupLidTop.transform.localScale = new Vector3(0.075f, 0.005f, 0.075f);
            if (cupLidTop.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(cupLidTop.GetComponent<Collider>());
            cupLidTop.GetComponent<Renderer>().sharedMaterial = cupLidMat;

            // Sip opening detail
            GameObject cupSpout = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cupSpout.name = "Cup_Spout_Detail";
            cupSpout.transform.SetParent(visualsParent.transform, false);
            cupSpout.transform.localPosition = new Vector3(0f, 0.13f, 0.026f);
            cupSpout.transform.localScale = new Vector3(0.02f, 0.004f, 0.012f);
            if (cupSpout.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(cupSpout.GetComponent<Collider>());
            cupSpout.GetComponent<Renderer>().sharedMaterial = metalMat;

            // 9. Teleport Destinations
            GameObject teleDestParent = new GameObject("Teleport_Destinations");

            // Destination 1: Starting Seated Chair Anchor
            GameObject seatedAnchorGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seatedAnchorGo.name = "TeleportAnchor_SeatedChair";
            seatedAnchorGo.transform.SetParent(teleDestParent.transform, false);
            seatedAnchorGo.transform.position = new Vector3(0f, 0.006f, 0f);
            seatedAnchorGo.transform.localScale = new Vector3(0.9f, 0.005f, 0.9f);
            seatedAnchorGo.transform.rotation = Quaternion.identity; // Facing forward (+Z) toward the table
            seatedAnchorGo.GetComponent<Renderer>().sharedMaterial = telePadSeatedMat;

            TeleportationAnchor seatedAnchor = seatedAnchorGo.AddComponent<TeleportationAnchor>();
            seatedAnchor.interactionManager = interactionManager;
            seatedAnchor.teleportAnchorTransform = seatedAnchorGo.transform;
            seatedAnchor.matchOrientation = MatchOrientation.TargetUpAndForward;
            seatedAnchor.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnSelectEntered;

            // Destination 2: Café Counter Open Area
            GameObject counterAreaGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            counterAreaGo.name = "TeleportArea_CafeCounter";
            counterAreaGo.transform.SetParent(teleDestParent.transform, false);
            counterAreaGo.transform.position = new Vector3(1.4f, 0.006f, 2.0f);
            counterAreaGo.transform.localScale = new Vector3(1.3f, 0.005f, 1.3f);
            counterAreaGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // Facing counter
            counterAreaGo.GetComponent<Renderer>().sharedMaterial = telePadMat;

            TeleportationArea counterArea = counterAreaGo.AddComponent<TeleportationArea>();
            counterArea.interactionManager = interactionManager;
            counterArea.matchOrientation = MatchOrientation.WorldSpaceUp;
            counterArea.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnSelectEntered;

            // Destination 3: Beachside View Open Area
            GameObject viewAreaGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            viewAreaGo.name = "TeleportArea_BeachsideView";
            viewAreaGo.transform.SetParent(teleDestParent.transform, false);
            viewAreaGo.transform.position = new Vector3(-2.0f, 0.006f, 2.2f);
            viewAreaGo.transform.localScale = new Vector3(1.3f, 0.005f, 1.3f);
            viewAreaGo.transform.rotation = Quaternion.Euler(0f, -60f, 0f); // Facing ocean view
            viewAreaGo.GetComponent<Renderer>().sharedMaterial = telePadMat;

            TeleportationArea viewArea = viewAreaGo.AddComponent<TeleportationArea>();
            viewArea.interactionManager = interactionManager;
            viewArea.matchOrientation = MatchOrientation.WorldSpaceUp;
            viewArea.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnSelectEntered;

            // 10. XR Origin (VR Player) - Seated Position with Locomotion & Teleportation
            GameObject xrOriginGo = new GameObject("XR Origin (VR)");
            xrOriginGo.transform.position = Vector3.zero; // Seated at chair (0, 0, 0)
            xrOriginGo.transform.rotation = Quaternion.identity; // Facing table (+Z)

            XROrigin xrOrigin = xrOriginGo.AddComponent<XROrigin>();
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            xrOrigin.CameraYOffset = 1.18f; // Realistic seated eye height

            // Locomotion System on XR Origin
            XRBodyTransformer bodyTransformer = xrOriginGo.AddComponent<XRBodyTransformer>();
            bodyTransformer.xrOrigin = xrOrigin;

            LocomotionMediator mediator = xrOriginGo.AddComponent<LocomotionMediator>();

            TeleportationProvider teleportProvider = xrOriginGo.AddComponent<TeleportationProvider>();
            teleportProvider.mediator = mediator;

            // Temporary Desktop Testing Controller (WASD + Mouse Look in Editor)
            xrOriginGo.AddComponent<XRDesktopTestingController>();

            // Link providers to interactables
            seatedAnchor.teleportationProvider = teleportProvider;
            counterArea.teleportationProvider = teleportProvider;
            viewArea.teleportationProvider = teleportProvider;

            // Camera Offset
            GameObject cameraOffsetGo = new GameObject("Camera Offset");
            cameraOffsetGo.transform.SetParent(xrOriginGo.transform, false);
            cameraOffsetGo.transform.localPosition = new Vector3(0f, 1.18f, 0f);
            cameraOffsetGo.transform.localRotation = Quaternion.identity;
            xrOrigin.CameraFloorOffsetObject = cameraOffsetGo;

            // Main Camera (Head Tracking)
            GameObject mainCameraGo = new GameObject("Main Camera");
            mainCameraGo.transform.SetParent(cameraOffsetGo.transform, false);
            mainCameraGo.transform.localPosition = Vector3.zero;
            mainCameraGo.transform.localRotation = Quaternion.Euler(6f, 0f, 0f); // Gentle 6-degree forward-downward tilt
            mainCameraGo.tag = "MainCamera";
            Camera cam = mainCameraGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 500f;
            cam.fieldOfView = 65f;
            mainCameraGo.AddComponent<AudioListener>();

            TrackedPoseDriver headPoseDriver = mainCameraGo.AddComponent<TrackedPoseDriver>();
            headPoseDriver.positionInput = new UnityEngine.InputSystem.InputActionProperty(
                new UnityEngine.InputSystem.InputAction("HeadPosition", binding: "<XRHMD>/centerEyePosition"));
            headPoseDriver.rotationInput = new UnityEngine.InputSystem.InputActionProperty(
                new UnityEngine.InputSystem.InputAction("HeadRotation", binding: "<XRHMD>/centerEyeRotation"));
            headPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;

            xrOrigin.Camera = cam;

            // Left Controller (Pose + Ray Interactor for Teleportation & UI)
            GameObject leftControllerGo = new GameObject("Left Controller");
            leftControllerGo.transform.SetParent(cameraOffsetGo.transform, false);
            leftControllerGo.transform.localPosition = new Vector3(-0.22f, -0.40f, 0.40f);

            TrackedPoseDriver leftPoseDriver = leftControllerGo.AddComponent<TrackedPoseDriver>();
            leftPoseDriver.positionInput = new UnityEngine.InputSystem.InputActionProperty(
                new UnityEngine.InputSystem.InputAction("LeftPosition", binding: "<XRController>{LeftHand}/devicePosition"));
            leftPoseDriver.rotationInput = new UnityEngine.InputSystem.InputActionProperty(
                new UnityEngine.InputSystem.InputAction("LeftRotation", binding: "<XRController>{LeftHand}/deviceRotation"));
            leftPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;

            XRRayInteractor leftRay = leftControllerGo.AddComponent<XRRayInteractor>();
            leftRay.interactionManager = interactionManager;
            leftRay.lineType = XRRayInteractor.LineType.ProjectileCurve;
            leftRay.velocity = 12f;
            leftRay.maxRaycastDistance = 15f;
            leftRay.enableUIInteraction = true;
            leftRay.allowHoveredActivate = false;
            leftRay.selectInput = new XRInputButtonReader("Select")
            {
                inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction,
                inputActionPerformed = new InputAction("LeftSelect", type: InputActionType.Button, binding: "<XRController>{LeftHand}/triggerPressed")
            };

            leftControllerGo.AddComponent<XRInteractorLineVisual>();

            // Left Direct Interactor (Direct Hand Grab with Grip)
            GameObject leftDirectGo = new GameObject("Direct Interactor");
            leftDirectGo.transform.SetParent(leftControllerGo.transform, false);
            SphereCollider leftDirectCol = leftDirectGo.AddComponent<SphereCollider>();
            leftDirectCol.radius = 0.08f;
            leftDirectCol.isTrigger = true;
            XRDirectInteractor leftDirect = leftDirectGo.AddComponent<XRDirectInteractor>();
            leftDirect.interactionManager = interactionManager;
            leftDirect.selectInput = new XRInputButtonReader("Select")
            {
                inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction,
                inputActionPerformed = new InputAction("LeftDirectSelect", type: InputActionType.Button, binding: "<XRController>{LeftHand}/gripPressed")
            };

            GameObject leftVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftVisual.name = "ControllerVisual_L";
            leftVisual.transform.SetParent(leftControllerGo.transform, false);
            leftVisual.transform.localScale = new Vector3(0.06f, 0.06f, 0.08f);
            if (leftVisual.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(leftVisual.GetComponent<Collider>());
            leftVisual.GetComponent<Renderer>().sharedMaterial = controllerMat;

            // Right Controller (Pose + Ray Interactor for Teleportation & UI + Direct Interactor for Grab)
            GameObject rightControllerGo = new GameObject("Right Controller");
            rightControllerGo.transform.SetParent(cameraOffsetGo.transform, false);
            rightControllerGo.transform.localPosition = new Vector3(0.22f, -0.40f, 0.40f);

            TrackedPoseDriver rightPoseDriver = rightControllerGo.AddComponent<TrackedPoseDriver>();
            rightPoseDriver.positionInput = new UnityEngine.InputSystem.InputActionProperty(
                new UnityEngine.InputSystem.InputAction("RightPosition", binding: "<XRController>{RightHand}/devicePosition"));
            rightPoseDriver.rotationInput = new UnityEngine.InputSystem.InputActionProperty(
                new UnityEngine.InputSystem.InputAction("RightRotation", binding: "<XRController>{RightHand}/deviceRotation"));
            rightPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;

            XRRayInteractor rightRay = rightControllerGo.AddComponent<XRRayInteractor>();
            rightRay.interactionManager = interactionManager;
            rightRay.lineType = XRRayInteractor.LineType.ProjectileCurve;
            rightRay.velocity = 12f;
            rightRay.maxRaycastDistance = 15f;
            rightRay.enableUIInteraction = true;
            rightRay.allowHoveredActivate = false;
            rightRay.selectInput = new XRInputButtonReader("Select")
            {
                inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction,
                inputActionPerformed = new InputAction("RightSelect", type: InputActionType.Button, binding: "<XRController>{RightHand}/triggerPressed")
            };

            rightControllerGo.AddComponent<XRInteractorLineVisual>();

            // Right Direct Interactor (Direct Hand Grab with Grip)
            GameObject rightDirectGo = new GameObject("Direct Interactor");
            rightDirectGo.transform.SetParent(rightControllerGo.transform, false);
            SphereCollider rightDirectCol = rightDirectGo.AddComponent<SphereCollider>();
            rightDirectCol.radius = 0.08f;
            rightDirectCol.isTrigger = true;
            XRDirectInteractor rightDirect = rightDirectGo.AddComponent<XRDirectInteractor>();
            rightDirect.interactionManager = interactionManager;
            rightDirect.selectInput = new XRInputButtonReader("Select")
            {
                inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction,
                inputActionPerformed = new InputAction("RightDirectSelect", type: InputActionType.Button, binding: "<XRController>{RightHand}/gripPressed")
            };

            GameObject rightVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightVisual.name = "ControllerVisual_R";
            rightVisual.transform.SetParent(rightControllerGo.transform, false);
            rightVisual.transform.localScale = new Vector3(0.06f, 0.06f, 0.08f);
            if (rightVisual.GetComponent<Collider>() != null) UnityEngine.Object.DestroyImmediate(rightVisual.GetComponent<Collider>());
            rightVisual.GetComponent<Renderer>().sharedMaterial = controllerMat;

            // 11. Save Scene & Build Settings
            string scenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[VR Setup] Scene created and saved to: {scenePath}");

            EditorBuildSettingsScene[] originalScenes = EditorBuildSettings.scenes;
            bool alreadyInBuild = false;
            foreach (var s in originalScenes)
            {
                if (s.path == scenePath) { alreadyInBuild = true; break; }
            }
            if (!alreadyInBuild)
            {
                var newScenes = new EditorBuildSettingsScene[originalScenes.Length + 1];
                originalScenes.CopyTo(newScenes, 0);
                newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = newScenes;
                Debug.Log("[VR Setup] Scene added to EditorBuildSettings.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VR Setup] Teleportation & Scene Setup Completed Successfully!");
        }

        private static void CreateLeg(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = "Leg";
            leg.transform.SetParent(parent, false);
            leg.transform.localPosition = localPos;
            leg.transform.localScale = scale;
            leg.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static Material GetOrCreateMaterial(string assetPath, Color color)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                Shader standardShader = Shader.Find("Standard");
                if (standardShader == null) standardShader = Shader.Find("Universal Render Pipeline/Lit");
                if (standardShader == null) standardShader = Shader.Find("Diffuse");
                
                mat = new Material(standardShader);
                mat.color = color;
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            return mat;
        }

        private static void ConfigureXRSettings()
        {
            try
            {
                XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
                EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettings);
                if (buildTargetSettings == null)
                {
                    buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
                }

                BuildTargetGroup targetGroup = BuildTargetGroup.Standalone;
                XRGeneralSettings settings = buildTargetSettings.SettingsForBuildTarget(targetGroup);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                    buildTargetSettings.SetSettingsForBuildTarget(targetGroup, settings);
                }

                if (settings.AssignedSettings == null)
                {
                    XRManagerSettings managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
                    settings.AssignedSettings = managerSettings;
                }

                XRPackageMetadataStore.AssignLoader(settings.AssignedSettings, "UnityEngine.XR.OpenXR.OpenXRLoader", targetGroup);
                Debug.Log("[VR Setup] OpenXR Loader assigned for Standalone build target.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VR Setup] Note during XR configuration: {ex.Message}");
            }
        }
    }
}
