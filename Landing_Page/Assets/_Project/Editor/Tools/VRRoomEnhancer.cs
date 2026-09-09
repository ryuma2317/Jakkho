using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace VRSetup.Editor
{
    public class VRRoomEnhancer : UnityEditor.Editor
    {
        [MenuItem("Tools/Build Enhanced VR Room")]
        public static void BuildEnhancedRoom()
        {
            // 1. Clean up legacy loose wall & floor fragments
            string[] legacyObjects = {
                "WallWindow4m", "Wall4m", "Wall1m", "Wall4m (1)", "WallWindowTall4m", 
                "Floor2m", "RugRectangleMedium", "VR_EnhancedRoomEnvironment", "VR_SciFiCommandRoom"
            };

            foreach (var name in legacyObjects)
            {
                var go = GameObject.Find(name);
                if (go != null) DestroyImmediate(go);
            }

            // 2. Create Root Environment Object
            var root = new GameObject("VR_EnhancedRoomEnvironment");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;

            // Load or Create Materials
            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
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

            const float roomWidth  = 18.0f; // X
            const float roomLength = 22.0f; // Z
            const float wallH      = 7.0f;  // Y
            const float centerZ    = 2.5f;

            // ================================================================
            //  A. ENHANCED FLOOR SYSTEM
            // ================================================================
            var mainFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainFloor.name = "MainFloor";
            mainFloor.transform.SetParent(root.transform, false);
            mainFloor.transform.localPosition = new Vector3(0f, -0.25f, centerZ);
            mainFloor.transform.localScale    = new Vector3(roomWidth + 2f, 0.5f, roomLength + 2f);
            mainFloor.GetComponent<Renderer>().sharedMaterial = floorBaseMat;
            EnsureCollider<BoxCollider>(mainFloor);

            // Elevated Center Platform
            const float platformR = 6.5f;
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "CentralPlatform";
            platform.transform.SetParent(root.transform, false);
            platform.transform.localPosition = new Vector3(0f, 0.01f, centerZ);
            platform.transform.localScale    = new Vector3(platformR * 2f, 0.02f, platformR * 2f);
            platform.GetComponent<Renderer>().sharedMaterial = darkMetalMat;
            EnsureCollider<MeshCollider>(platform);

            // Glowing Amber Trim Rings on Floor
            var trimRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trimRing.name = "PlatformTrimRing";
            trimRing.transform.SetParent(root.transform, false);
            trimRing.transform.localPosition = new Vector3(0f, 0.025f, centerZ);
            trimRing.transform.localScale    = new Vector3(platformR * 2f + 0.3f, 0.015f, platformR * 2f + 0.3f);
            trimRing.GetComponent<Renderer>().sharedMaterial = amberTrimMat;

            var innerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerRing.name = "PlatformInnerRing";
            innerRing.transform.SetParent(root.transform, false);
            innerRing.transform.localPosition = new Vector3(0f, 0.028f, centerZ);
            innerRing.transform.localScale    = new Vector3(platformR * 2f - 0.5f, 0.016f, platformR * 2f - 0.5f);
            innerRing.GetComponent<Renderer>().sharedMaterial = amberMat;

            // ================================================================
            //  B. ENHANCED ROOF / CEILING SYSTEM
            // ================================================================
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "MainCeiling";
            ceiling.transform.SetParent(root.transform, false);
            ceiling.transform.localPosition = new Vector3(0f, wallH + 0.25f, centerZ);
            ceiling.transform.localScale    = new Vector3(roomWidth + 2f, 0.5f, roomLength + 2f);
            ceiling.GetComponent<Renderer>().sharedMaterial = ceilingMat;
            EnsureCollider<BoxCollider>(ceiling);

            // Concentric Glowing Dome Rings on Ceiling
            float[] domeRadii = { 7.0f, 5.2f, 3.6f, 2.0f, 0.8f };
            float ringThick = 0.06f;
            for (int i = 0; i < domeRadii.Length; i++)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = $"CeilingRing_{i}";
                ring.transform.SetParent(root.transform, false);
                ring.transform.localPosition = new Vector3(0f, wallH - (i * 0.08f), centerZ);
                ring.transform.localScale    = new Vector3(domeRadii[i] * 2f, ringThick, domeRadii[i] * 2f);
                ring.GetComponent<Renderer>().sharedMaterial = purpleRingMat;
            }

            // ================================================================
            //  C. ENHANCED 4-SIDED WALL ENCLOSURE
            // ================================================================
            // 1. Back Wall (North: +Z)
            CreateWall(root.transform, "Wall_North_Back", new Vector3(0f, wallH * 0.5f, centerZ + roomLength * 0.5f), new Vector3(roomWidth, wallH, 0.6f), backWallMat);

            // 2. Front Wall (South: -Z)
            CreateWall(root.transform, "Wall_South_Front", new Vector3(0f, wallH * 0.5f, centerZ - roomLength * 0.5f), new Vector3(roomWidth, wallH, 0.6f), backWallMat);

            // 3. Left Wall (West: -X)
            CreateWall(root.transform, "Wall_West_Left", new Vector3(-roomWidth * 0.5f, wallH * 0.5f, centerZ), new Vector3(0.6f, wallH, roomLength), darkMetalMat);

            // 4. Right Wall (East: +X)
            CreateWall(root.transform, "Wall_East_Right", new Vector3(roomWidth * 0.5f, wallH * 0.5f, centerZ), new Vector3(0.6f, wallH, roomLength), darkMetalMat);

            // Glowing Wall Trim Strips
            CreateTrimStrip(root.transform, "Trim_North", new Vector3(0f, wallH * 0.5f, centerZ + roomLength * 0.5f - 0.32f), new Vector3(roomWidth - 2f, 0.12f, 0.06f), blueLEDMat);
            CreateTrimStrip(root.transform, "Trim_South", new Vector3(0f, wallH * 0.5f, centerZ - roomLength * 0.5f + 0.32f), new Vector3(roomWidth - 2f, 0.12f, 0.06f), blueLEDMat);
            CreateTrimStrip(root.transform, "Trim_West",  new Vector3(-roomWidth * 0.5f + 0.32f, wallH * 0.5f, centerZ), new Vector3(0.06f, 0.12f, roomLength - 2f), amberMat);
            CreateTrimStrip(root.transform, "Trim_East",  new Vector3( roomWidth * 0.5f - 0.32f, wallH * 0.5f, centerZ), new Vector3(0.06f, 0.12f, roomLength - 2f), amberMat);

            // ================================================================
            //  D. SCI-FI SERVER RACKS & CONSOLES
            // ================================================================
            BuildServerRack(root.transform, "ServerRack_Left",  new Vector3(-5.5f, 0f, centerZ + 7.5f), rackMat, rackFrameMat, blueLEDMat, amberMat);
            BuildServerRack(root.transform, "ServerRack_Right", new Vector3( 5.5f, 0f, centerZ + 7.5f), rackMat, rackFrameMat, blueLEDMat, amberMat);

            // ================================================================
            //  E. LIGHTING SETUP
            // ================================================================
            // Ambient Directional Light
            var dirGO = GameObject.Find("Directional Light") ?? new GameObject("Directional Light");
            var dirL  = dirGO.GetComponent<Light>() ?? dirGO.AddComponent<Light>();
            dirL.type = LightType.Directional;
            dirL.color     = new Color(0.70f, 0.80f, 1.0f);
            dirL.intensity = 0.65f;
            dirGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Point and Spot Lights
            CreatePointLight(root.transform, "Light_FloorGlow", new Vector3(0f, 0.5f, centerZ), new Color(0.4f, 0.75f, 1.0f), 3.5f, 12f);
            CreatePointLight(root.transform, "Light_CeilingDome", new Vector3(0f, wallH - 0.8f, centerZ), new Color(0.55f, 0.40f, 1.0f), 4.0f, 16f);
            CreatePointLight(root.transform, "Light_AmberAccent", new Vector3(0f, 0.2f, centerZ), new Color(1.0f, 0.6f, 0.15f), 2.5f, 9f);

            // 3. Mark Scene Dirty & Save
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("<b>[VRRoomEnhancer]</b> Fully enclosed spacious room (Floor, Roof/Ceiling, and 4 Walls) successfully built and saved!");
        }

        private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = pos;
            wall.transform.localScale    = scale;
            wall.GetComponent<Renderer>().sharedMaterial = mat;
            EnsureCollider<BoxCollider>(wall);
        }

        private static void CreateTrimStrip(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = name;
            trim.transform.SetParent(parent, false);
            trim.transform.localPosition = pos;
            trim.transform.localScale    = scale;
            trim.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreatePointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
        }

        private static void BuildServerRack(Transform parent, string name, Vector3 pos, Material body, Material frame, Material blueLED, Material amberLED)
        {
            var rack = new GameObject(name);
            rack.transform.SetParent(parent, false);
            rack.transform.localPosition = pos;

            var casing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            casing.name = "Casing";
            casing.transform.SetParent(rack.transform, false);
            casing.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            casing.transform.localScale = new Vector3(1.0f, 4.0f, 0.8f);
            casing.GetComponent<Renderer>().sharedMaterial = body;
            EnsureCollider<BoxCollider>(casing);

            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "LEDStrip";
            trim.transform.SetParent(rack.transform, false);
            trim.transform.localPosition = new Vector3(0f, 2.0f, -0.42f);
            trim.transform.localScale = new Vector3(0.85f, 3.8f, 0.05f);
            trim.GetComponent<Renderer>().sharedMaterial = blueLED;
        }

        private static void EnsureCollider<T>(GameObject go) where T : Collider
        {
            if (go.GetComponent<Collider>() == null)
            {
                go.AddComponent<T>();
            }
        }

        private static Material GetOrCreateMat(Shader shader, string name, Color color, float smoothness, float metallic)
        {
            string dir = "Assets/Materials/SciFiCommandRoom";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = $"{dir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateMat(Shader shader, string name, Color color, float emissionIntensity)
        {
            string dir = "Assets/Materials/SciFiCommandRoom";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = $"{dir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emissionIntensity);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
