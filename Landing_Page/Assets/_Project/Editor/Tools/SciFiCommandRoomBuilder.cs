using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.EventSystems;
using UnityEditor.SceneManagement;
using System.IO;

public class SciFiCommandRoomBuilder : Editor
{
    // -----------------------------------------------------------------------
    // PUBLIC: Called by MainMenuBuilder & SceneRestorer.
    // Destroys any previous rooms then builds the new one.
    // -----------------------------------------------------------------------
    public static void BuildRoomGeometryOnly()
    {
        string[] oldNames = {
            "VR_RoomEnvironment","VR_SciFiRoom","RoomEnvironment",
            "SciFiRoom","Room","VR_SciFiCommandRoom","XR Origin","XR_Origin"
        };
        foreach (var n in oldNames)
        {
            var old = GameObject.Find(n);
            if (old != null) DestroyImmediate(old);
        }
        BuildRoomGeometry();
    }

    [MenuItem("Tools/Build 3D Sci-Fi VR Command Room")]
    public static void BuildCommandRoom()
    {
        EnsureEventSystem();

        if (GameObject.Find("GameManager") == null)
            new GameObject("GameManager").AddComponent<GameManager>();

        BuildRoomGeometryOnly();
        ScenarioSelectBuilder.BuildScenarioSelectScreen();
        EnsureMenuManager();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<b>[SciFiCommandRoomBuilder]</b> Futuristic Circular Sci-Fi Command Room built!");
    }

    // -----------------------------------------------------------------------
    // MAIN ROOM  —  Futuristic Sci-Fi Circular Interior Demo Lab
    // -----------------------------------------------------------------------
    private static void BuildRoomGeometry()
    {
        // 1. Camera setup
        GameObject camGO = GameObject.FindWithTag("MainCamera") ?? GameObject.Find("Main Camera");
        if (camGO == null)
        {
            camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }
        camGO.transform.position = new Vector3(0f, 1.6f, 0f);
        camGO.transform.rotation = Quaternion.identity;
        var cam = camGO.GetComponent<Camera>();
        if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.06f, 0.05f, 0.12f); }

        var root = new GameObject("VR_SciFiCommandRoom");

        // 2. XR Origin Rig with Locomotion & Projectile (Curved) Ray Interactors
        SetupXROriginRig(root.transform);

        Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // ---- Materials (Saved persistently to avoid turning pink) ----
        var darkMetalMat  = GetOrCreateMat(lit, "DarkMetal",  new Color(0.10f, 0.10f, 0.16f), 0.85f, 0.60f);
        var backWallMat   = GetOrCreateMat(lit, "BackWall",   new Color(0.14f, 0.14f, 0.22f), 0.80f, 0.55f);
        var floorBaseMat  = GetOrCreateMat(lit, "FloorBase",  new Color(0.08f, 0.08f, 0.14f), 0.90f, 0.65f);
        var ceilingMat    = GetOrCreateMat(lit, "Ceiling",    new Color(0.10f, 0.10f, 0.18f), 0.80f, 0.50f);
        var consoleMat    = GetOrCreateMat(lit, "Console",    new Color(0.16f, 0.17f, 0.26f), 0.75f, 0.50f);
        var screenMat     = GetOrCreateMat(lit, "ScreenGlass",new Color(0.04f, 0.05f, 0.10f), 0.98f, 0.80f);
        var rackMat       = GetOrCreateMat(lit, "RackBody",   new Color(0.12f, 0.12f, 0.18f), 0.80f, 0.60f);
        var rackFrameMat  = GetOrCreateMat(lit, "RackFrame",  new Color(0.18f, 0.18f, 0.28f), 0.75f, 0.55f);
        var blueWhiteGlow = GetOrCreateMat(lit, "BlueWhiteGlow", new Color(0.70f, 0.88f, 1.00f), 2.2f);
        var blueLEDMat    = GetOrCreateMat(lit, "BlueLED",       new Color(0.20f, 0.60f, 1.00f), 3.5f);
        var purpleRingMat = GetOrCreateMat(lit, "PurpleCeilingRings", new Color(0.38f, 0.30f, 1.00f), 2.8f);
        var amberMat      = GetOrCreateMat(lit, "AmberSeam",     new Color(1.00f, 0.62f, 0.10f), 2.5f);
        var amberTrimMat  = GetOrCreateMat(lit, "AmberTrim",     new Color(1.00f, 0.55f, 0.08f), 1.8f);

        const float roomR   = 7.5f;
        const float wallH   = 6.0f;
        const float centerZ = 2.8f;

        // ---- A. FLOOR ----
        var outerFloor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        outerFloor.name = "OuterFloor";
        outerFloor.transform.SetParent(root.transform, false);
        outerFloor.transform.localPosition = new Vector3(0f, -0.02f, centerZ);
        outerFloor.transform.localScale    = new Vector3(roomR * 2f, 0.02f, roomR * 2f);
        outerFloor.GetComponent<Renderer>().sharedMaterial = floorBaseMat;

        // Circular floor platform
        const float platformR = 4.8f;
        var platformRoot = new GameObject("CircularFloorPlatform");
        platformRoot.transform.SetParent(root.transform, false);

        var platformBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platformBase.name = "PlatformBase";
        platformBase.transform.SetParent(platformRoot.transform, false);
        platformBase.transform.localPosition = new Vector3(0f, 0.01f, centerZ);
        platformBase.transform.localScale    = new Vector3(platformR * 2f, 0.012f, platformR * 2f);
        platformBase.GetComponent<Renderer>().sharedMaterial = darkMetalMat;

        const int tileN = 4;
        const float tileS = 1.95f, tileGap = 0.065f;
        float gridHalf = tileN * 0.5f * (tileS + tileGap);
        for (int r = 0; r < tileN; r++)
        {
            for (int c = 0; c < tileN; c++)
            {
                float tx = -gridHalf + (c + 0.5f) * (tileS + tileGap);
                float tz = centerZ - gridHalf + (r + 0.5f) * (tileS + tileGap);
                if (new Vector2(tx, tz - centerZ).magnitude > platformR - 0.4f) continue;

                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"GridTile_{r}_{c}";
                tile.transform.SetParent(platformRoot.transform, false);
                tile.transform.localPosition = new Vector3(tx, 0.022f, tz);
                tile.transform.localScale    = new Vector3(tileS, 0.025f, tileS);
                tile.GetComponent<Renderer>().sharedMaterial = blueWhiteGlow;

                var seamX = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seamX.name = $"SeamX_{r}_{c}";
                seamX.transform.SetParent(platformRoot.transform, false);
                seamX.transform.localPosition = new Vector3(tx + (tileS + tileGap) * 0.5f - tileGap * 0.5f, 0.024f, tz);
                seamX.transform.localScale    = new Vector3(tileGap, 0.028f, tileS);
                seamX.GetComponent<Renderer>().sharedMaterial = amberMat;

                var seamZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seamZ.name = $"SeamZ_{r}_{c}";
                seamZ.transform.SetParent(platformRoot.transform, false);
                seamZ.transform.localPosition = new Vector3(tx, 0.024f, tz + (tileS + tileGap) * 0.5f - tileGap * 0.5f);
                seamZ.transform.localScale    = new Vector3(tileS, 0.028f, tileGap);
                seamZ.GetComponent<Renderer>().sharedMaterial = amberMat;
            }
        }

        var amberRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        amberRing.name = "AmberTrimRing";
        amberRing.transform.SetParent(platformRoot.transform, false);
        amberRing.transform.localPosition = new Vector3(0f, 0.030f, centerZ);
        amberRing.transform.localScale    = new Vector3(platformR * 2f + 0.15f, 0.018f, platformR * 2f + 0.15f);
        amberRing.GetComponent<Renderer>().sharedMaterial = amberTrimMat;

        var amberRing2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        amberRing2.name = "AmberTrimRingInner";
        amberRing2.transform.SetParent(platformRoot.transform, false);
        amberRing2.transform.localPosition = new Vector3(0f, 0.033f, centerZ);
        amberRing2.transform.localScale    = new Vector3(platformR * 2f - 0.38f, 0.016f, platformR * 2f - 0.38f);
        amberRing2.GetComponent<Renderer>().sharedMaterial = amberMat;

        // ---- B. CURVED BACK WALL ----
        const int arcN = 10;
        const float arcDeg = 160f;
        const float arcR2 = 6.8f;
        const float arcZ0 = 5.5f;
        for (int i = 0; i < arcN; i++)
        {
            float a0 = ((float)i / arcN - 0.5f) * arcDeg * Mathf.Deg2Rad;
            float a1 = ((float)(i + 1) / arcN - 0.5f) * arcDeg * Mathf.Deg2Rad;
            float aMid = (a0 + a1) * 0.5f;

            float x = Mathf.Sin(aMid) * arcR2;
            float z = arcZ0 + arcR2 - Mathf.Cos(aMid) * arcR2;

            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = $"BackWall_Seg{i}";
            seg.transform.SetParent(root.transform, false);
            seg.transform.localPosition = new Vector3(x, wallH * 0.5f, z);
            seg.transform.localEulerAngles = new Vector3(0f, aMid * Mathf.Rad2Deg, 0f);
            seg.transform.localScale = new Vector3(arcR2 * (a1 - a0) + 0.18f, wallH, 0.50f);
            seg.GetComponent<Renderer>().sharedMaterial = backWallMat;
        }

        Wall(root.transform, "LeftSideWall",  new Vector3(-roomR + 0.25f, wallH * 0.5f, centerZ), new Vector3(0.4f, wallH, roomR * 2.2f), darkMetalMat);
        Wall(root.transform, "RightSideWall", new Vector3( roomR - 0.25f, wallH * 0.5f, centerZ), new Vector3(0.4f, wallH, roomR * 2.2f), darkMetalMat);

        // ---- C. CEILING ----
        var ceilDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ceilDisc.name = "CeilingDisc";
        ceilDisc.transform.SetParent(root.transform, false);
        ceilDisc.transform.localPosition = new Vector3(0f, wallH + 0.18f, centerZ);
        ceilDisc.transform.localScale    = new Vector3(roomR * 2.1f, 0.22f, roomR * 2.1f);
        ceilDisc.GetComponent<Renderer>().sharedMaterial = ceilingMat;

        float[] ringRadii = { 5.8f, 4.4f, 3.2f, 2.0f, 0.9f };
        float[] ringYs = { wallH - 0.02f, wallH + 0.18f, wallH + 0.35f, wallH + 0.48f, wallH + 0.56f };
        float ringThick = 0.055f;
        for (int i = 0; i < ringRadii.Length; i++)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = $"DomeRing_{i}";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, ringYs[i], centerZ);
            ring.transform.localScale    = new Vector3(ringRadii[i] * 2f, ringThick, ringRadii[i] * 2f);
            ring.GetComponent<Renderer>().sharedMaterial = purpleRingMat;

            if (i < ringRadii.Length - 1)
            {
                var acc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                acc.name = $"DomeRingAmberAccent_{i}";
                acc.transform.SetParent(root.transform, false);
                acc.transform.localPosition = new Vector3(0f, ringYs[i] - ringThick * 0.5f, centerZ);
                acc.transform.localScale    = new Vector3(ringRadii[i] * 2f - 0.08f, ringThick * 0.25f, ringRadii[i] * 2f - 0.08f);
                acc.GetComponent<Renderer>().sharedMaterial = amberMat;
            }
        }

        // ---- E. SERVER RACKS ----
        BuildServerRack(root.transform, "ServerRack_Left",  new Vector3(-2.0f, 0f, 5.6f), rackMat, rackFrameMat, blueLEDMat, amberMat);
        BuildServerRack(root.transform, "ServerRack_Right", new Vector3( 2.0f, 0f, 5.6f), rackMat, rackFrameMat, blueLEDMat, amberMat);

        // ---- F. MONITOR WALL ----
        var monGrp = new GameObject("MonitorDisplay_Left");
        monGrp.transform.SetParent(root.transform, false);
        monGrp.transform.localPosition    = new Vector3(-roomR + 0.95f, 2.7f, 2.8f);
        monGrp.transform.localEulerAngles = new Vector3(0f, 16f, 0f);
        Cube(monGrp.transform, "MonitorBezel", Vector3.zero, Vector3.zero, new Vector3(4.40f, 2.70f, 0.10f), darkMetalMat);
        int monRows = 2, monCols = 3;
        for (int r = 0; r < monRows; r++)
        {
            for (int c = 0; c < monCols; c++)
            {
                Cube(monGrp.transform, $"Screen_{r}_{c}", new Vector3((c - 1f) * 1.42f, (r - 0.5f) * 1.20f, -0.058f), Vector3.zero, new Vector3(1.32f, 1.06f, 0.055f), screenMat);
            }
        }
        Cube(monGrp.transform, "MonitorShelf",   new Vector3(0f, -1.58f, -0.04f), Vector3.zero, new Vector3(4.65f, 0.14f, 0.55f), consoleMat);
        Cube(monGrp.transform, "MonitorAmberLED",new Vector3(0f, -1.50f, -0.30f), Vector3.zero, new Vector3(4.50f, 0.045f, 0.06f), amberMat);

        // ---- G. CONSOLE & WALL SIGN ----
        var conBody = Cube(root.transform, "ControlConsolePodium", new Vector3(roomR - 1.70f, 0.56f, 2.10f), new Vector3(0f, -14f, 0f), new Vector3(2.00f, 1.12f, 1.35f), consoleMat);
        Cube(conBody.transform, "ConsoleScreenInset", new Vector3(0f, 0.52f, -0.05f), Vector3.zero, new Vector3(0.88f, 0.055f, 0.82f), screenMat);
        Cube(conBody.transform, "ConsoleBevel", new Vector3(0f, 0.508f, 0f), Vector3.zero, new Vector3(1.04f, 0.042f, 1.04f), darkMetalMat);
        Cube(conBody.transform, "ConsoleAmberStrip", new Vector3(0f, 0.0f, -0.52f), Vector3.zero, new Vector3(0.90f, 0.030f, 0.025f), amberMat);

        var signBoard = Cube(root.transform, "InteractiveSandboxSign", new Vector3(roomR - 1.40f, 3.75f, 1.10f), new Vector3(0f, -14f, 0f), new Vector3(3.20f, 0.48f, 0.12f), consoleMat);
        Cube(signBoard.transform, "SignAmberStrip", new Vector3(0f, -0.55f, 0f), Vector3.zero, new Vector3(1.00f, 0.06f, 0.08f), amberMat);

        var signLabel = new GameObject("SignLabel3D");
        signLabel.transform.SetParent(signBoard.transform, false);
        signLabel.transform.localPosition    = new Vector3(0f, 0f, -0.52f);
        signLabel.transform.localScale       = new Vector3(0.042f, 0.12f, 0.12f);
        var signTMP = signLabel.AddComponent<TextMeshPro>();
        signTMP.text = "Interactive Sandbox  |  Demo Lab";
        signTMP.fontSize  = 8f;
        signTMP.fontStyle = FontStyles.Bold;
        signTMP.alignment = TextAlignmentOptions.Center;
        signTMP.color     = new Color(0.88f, 0.94f, 1.0f);

        // ---- H. CORNER ACCENTS ----
        DiamondAccent(root.transform, "DiamondAccent_BackLeft",  new Vector3(-2.0f, 4.55f, 5.55f), purpleRingMat);
        DiamondAccent(root.transform, "DiamondAccent_BackRight", new Vector3( 2.0f, 4.55f, 5.55f), purpleRingMat);

        // ---- I. LIGHTS ----
        float[] spotAngles = { 0f, 72f, 144f, 216f, 288f };
        foreach (var ang in spotAngles)
        {
            float rad = ang * Mathf.Deg2Rad;
            float sx  = Mathf.Sin(rad) * 4.8f;
            float sz  = centerZ + Mathf.Cos(rad) * 4.8f;
            Spotlight(root.transform, $"CeilSpot_{ang:F0}", new Vector3(sx, wallH - 0.05f, sz), Quaternion.Euler(88f, -ang, 0f), new Color(1.0f, 0.82f, 0.55f), 2.2f, 28f);
        }
        PointLight(root.transform, "PlatformBlueGlow",   new Vector3(0f, 0.40f, centerZ),  new Color(0.55f, 0.82f, 1.0f), 4.5f, 8.5f);
        PointLight(root.transform, "AmberRingGlow",      new Vector3(0f, 0.12f, centerZ),  new Color(1.0f,  0.55f, 0.12f), 3.0f, 6.5f);
        PointLight(root.transform, "DomePurpleAmbient",  new Vector3(0f, wallH - 0.6f, centerZ), new Color(0.38f, 0.28f, 0.90f), 3.5f, 12f);
        PointLight(root.transform, "MonitorBlueFill",    new Vector3(-5f, 2.5f, 2.8f),       new Color(0.30f, 0.55f, 1.0f), 2.0f, 6f);
        PointLight(root.transform, "ConsoleAmberFill",   new Vector3( 5f, 1.5f, 2.0f),       new Color(1.0f,  0.55f, 0.15f), 1.8f, 5f);

        var dirGO = GameObject.Find("Directional Light") ?? new GameObject("Directional Light");
        var dirL  = dirGO.GetComponent<Light>() ?? dirGO.AddComponent<Light>();
        dirL.type = LightType.Directional;
        dirL.color     = new Color(0.60f, 0.70f, 0.95f);
        dirL.intensity = 0.40f;
        dirGO.transform.rotation = Quaternion.Euler(48f, -20f, 0f);
    }

    // ================================================================
    //  XR ORIGIN SETUP WITH LOCOMOTION & PARABOLIC RAY
    // ================================================================
    private static void SetupXROriginRig(Transform parent)
    {
        // Destroy old rigs
        GameObject oldRig = GameObject.Find("XR Origin") ?? GameObject.Find("XR_Origin");
        if (oldRig != null) DestroyImmediate(oldRig);

        GameObject xrOriginGO = new GameObject("XR Origin");
        xrOriginGO.transform.SetParent(parent, false);
        xrOriginGO.transform.localPosition = Vector3.zero;

        // Add Locomotion System components for joystick movement
        System.Type locomotionSystemType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.LocomotionSystem, Unity.XR.Interaction.Toolkit")
            ?? System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionSystem, Unity.XR.Interaction.Toolkit");
        if (locomotionSystemType != null)
        {
            xrOriginGO.AddComponent(locomotionSystemType);
        }

        // Add Continuous Move Provider (Action-based) for standard movement
        System.Type contMoveType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.ActionBasedContinuousMoveProvider, Unity.XR.Interaction.Toolkit")
            ?? System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ActionBasedContinuousMoveProvider, Unity.XR.Interaction.Toolkit");
        if (contMoveType != null)
        {
            xrOriginGO.AddComponent(contMoveType);
        }

        // Add Snap Turn Provider for rotating
        System.Type snapTurnType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.ActionBasedSnapTurnProvider, Unity.XR.Interaction.Toolkit")
            ?? System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.ActionBasedSnapTurnProvider, Unity.XR.Interaction.Toolkit");
        if (snapTurnType != null)
        {
            xrOriginGO.AddComponent(snapTurnType);
        }

        System.Type xrOriginType = System.Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
        if (xrOriginType != null)
        {
            Component xrOrigin = xrOriginGO.AddComponent(xrOriginType);

            GameObject cameraOffsetGO = new GameObject("Camera Offset");
            cameraOffsetGO.transform.SetParent(xrOriginGO.transform, false);

            var offsetField = xrOriginType.GetField("m_CameraOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (offsetField != null) offsetField.SetValue(xrOrigin, cameraOffsetGO.transform);

            // Re-parent Main Camera
            GameObject camGO = GameObject.FindWithTag("MainCamera") ?? GameObject.Find("Main Camera");
            if (camGO != null)
            {
                camGO.transform.SetParent(cameraOffsetGO.transform, false);
                camGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                camGO.transform.localRotation = Quaternion.identity;

                var camField = xrOriginType.GetField("m_Camera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (camField != null) camField.SetValue(xrOrigin, camGO.GetComponent<Camera>());
            }

            SetupXRController(cameraOffsetGO.transform, "LeftHand Controller", true);
            SetupXRController(cameraOffsetGO.transform, "RightHand Controller", false);
        }
    }

    private static void SetupXRController(Transform parent, string name, bool isLeft)
    {
        GameObject controllerGO = new GameObject(name);
        controllerGO.transform.SetParent(parent, false);
        controllerGO.transform.localPosition = isLeft ? new Vector3(-0.25f, 1.3f, 0.2f) : new Vector3(0.25f, 1.3f, 0.2f);
        controllerGO.transform.localRotation = Quaternion.identity;

        System.Type xrControllerType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.ActionBasedController, Unity.XR.Interaction.Toolkit")
            ?? System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRController, Unity.XR.Interaction.Toolkit");
        if (xrControllerType != null)
        {
            controllerGO.AddComponent(xrControllerType);
        }

        System.Type rayInteractorType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor, Unity.XR.Interaction.Toolkit")
            ?? System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRRayInteractor, Unity.XR.Interaction.Toolkit");
        
        Component interactorInstance = null;
        if (rayInteractorType != null)
        {
            interactorInstance = controllerGO.AddComponent(rayInteractorType);

            // Configured ray interactor to use curved projectile visual (parabolic curve matching user drawing)
            var lineTypeProp = rayInteractorType.GetProperty("lineType");
            if (lineTypeProp != null)
            {
                // LineType: 0 = Straight, 1 = ProjectileCurve, 2 = BezierCurve
                lineTypeProp.SetValue(interactorInstance, 1);
            }
        }

        LineRenderer lineRenderer = controllerGO.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.widthMultiplier = 0.015f;
        lineRenderer.startWidth = 0.012f;
        lineRenderer.endWidth = 0.003f;

        Shader lineShader = Shader.Find("Sprites/Default") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
        Material laserMat = new Material(lineShader);
        Color laserColor = new Color(1f, 0.18f, 0.47f, 1f); // Magenta/pink
        laserMat.color = laserColor;
        if (laserMat.HasProperty("_EmissionColor"))
        {
            laserMat.EnableKeyword("_EMISSION");
            laserMat.SetColor("_EmissionColor", laserColor * 3.0f);
        }
        lineRenderer.sharedMaterial = laserMat;

        System.Type lineVisualType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual, Unity.XR.Interaction.Toolkit")
            ?? System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual, Unity.XR.Interaction.Toolkit");
        if (lineVisualType != null)
        {
            controllerGO.AddComponent(lineVisualType);
        }
    }

    // ================================================================
    //  SERVER RACK TOWER
    // ================================================================
    private static void BuildServerRack(Transform parent, string name, Vector3 pos,
        Material body, Material frame, Material blueLED, Material amberLED)
    {
        var rack = new GameObject(name);
        rack.transform.SetParent(parent, false);
        rack.transform.localPosition = pos;

        Cube(rack.transform, "Casing", new Vector3(0f, 1.75f, 0f), Vector3.zero, new Vector3(0.72f, 3.50f, 0.56f), body);
        Cube(rack.transform, "CasingFrame", new Vector3(0f, 1.75f, -0.29f), Vector3.zero, new Vector3(0.68f, 3.46f, 0.04f), frame);

        for (int i = 0; i < 8; i++)
        {
            float y = 0.40f + i * 0.36f;
            Cube(rack.transform, $"Blade_{i}",    new Vector3(0f,   y, -0.30f), Vector3.zero, new Vector3(0.64f, 0.28f, 0.040f), frame);
            Cube(rack.transform, $"BladeLED_{i}", new Vector3(0.24f,y, -0.31f), Vector3.zero, new Vector3(0.036f,0.036f,0.016f), blueLED);
        }

        Cube(rack.transform, "TopAmberBar", new Vector3(0f, 3.55f, -0.28f), Vector3.zero, new Vector3(0.66f, 0.048f, 0.048f), amberLED);
        DiamondAccent(rack.transform, "TopDiamond", new Vector3(0f, 3.80f, -0.28f), blueLED);
    }

    // ================================================================
    //  PERSISTENT MATERIAL GENERATOR (Shader Pink Fix)
    // ================================================================
    private static Material GetOrCreateMat(Shader shader, string name, Color color, float smoothness, float metallic)
    {
        string dir = "Assets/Materials/SciFiCommandRoom";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        string path = $"{dir}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateMat(Shader shader, string name, Color color, float emissionIntensity)
    {
        string dir = "Assets/Materials/SciFiCommandRoom";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        string path = $"{dir}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.color = color;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emissionIntensity);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ================================================================
    //  HELPERS
    // ================================================================
    private static void DiamondAccent(Transform parent, string name, Vector3 pos, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition    = pos;
        go.transform.localEulerAngles = new Vector3(0f, 0f, 45f);
        go.transform.localScale       = new Vector3(0.18f, 0.18f, 0.055f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void Wall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static GameObject Cube(Transform parent, string name, Vector3 lPos, Vector3 lEuler,
        Vector3 lScale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition    = lPos;
        go.transform.localEulerAngles = lEuler;
        go.transform.localScale       = lScale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static void Spotlight(Transform p, string name, Vector3 pos, Quaternion rot,
        Color col, float intensity, float angle)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        go.transform.position = pos;
        go.transform.rotation = rot;
        var l = go.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = col; l.intensity = intensity;
        l.spotAngle = angle; l.range = 9f;
    }

    private static void PointLight(Transform p, string name, Vector3 pos,
        Color col, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = col; l.intensity = intensity; l.range = range;
    }

    private static void EnsureMenuManager()
    {
        if (!GameObject.Find("MenuManager"))
            new GameObject("MenuManager").AddComponent<MenuManager>();
    }

    private static void EnsureEventSystem()
    {
        var es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null) es = new GameObject("EventSystem").AddComponent<EventSystem>();

        var isType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        var legacy  = es.GetComponent<StandaloneInputModule>();
        if (legacy != null) Undo.DestroyObjectImmediate(legacy);
        es = Object.FindFirstObjectByType<EventSystem>();
        if (isType != null) { if (!es.GetComponent(isType)) Undo.AddComponent(es.gameObject, isType); }
        else { if (!es.GetComponent<StandaloneInputModule>()) Undo.AddComponent<StandaloneInputModule>(es.gameObject); }
    }
}
