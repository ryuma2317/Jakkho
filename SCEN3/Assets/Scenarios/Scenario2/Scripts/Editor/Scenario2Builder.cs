using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor builder for Scenario 2: Evil Twin at the Cafe.
/// Constructs the complete Scenario2 scene hierarchy including:
///   - XR Origin (XR Rig) with camera and controllers
///   - Coffee shop / cafe environment
///   - Curved holographic workstation (Wi-Fi selector + browser UI)
///   - All 5 phase UI panels
///   - Audio sources and clips
///   - Wires all serialized fields on Scenario2_EvilTwinManager
///
/// Usage: Menu → Jakkho → Build Scenario 2 Scene & Environment
/// </summary>
public class Scenario2Builder : MonoBehaviour
{
    // ═══════════════════════════════════════════════
    //  Constants — Match the Scenario 3 standard
    // ═══════════════════════════════════════════════
    private const float WorkstationHeight = 1.10f;
    private const float WorkstationDistance = 1.45f;
    private const float WorkstationCanvasScale = 0.00105f;
    private const float WorkstationYaw = 270f;

    // Workstation anchor position (cafe room)
    private static readonly Vector3 WorkstationAnchor = new Vector3(-10.0f, 0f, 4.0f);

    // Curved section geometry
    private const float CenterWidth = 704f;
    private const float SideWidth = 448f;
    private const float CanvasHeight = 900f;
    private const float CurveAngle = 18f;

    [MenuItem("Jakkho/Build Scenario 2 Scene & Environment", priority = 5)]
    public static void BuildScenario2()
    {
        Debug.Log("[Scenario2Builder] ═══ Starting Scenario 2 Build ═══");

        // TODO: Implement full scene construction following Scenario3Builder pattern
        //
        // Required steps:
        // 1. Create new scene → save as Assets/Scenarios/Scenario2/Scenes/Scenario2.unity
        // 2. Build XR Origin (XR Rig)
        // 3. Build cafe environment (tables, chairs, counter, ambient lighting)
        // 4. Build curved holographic workstation with Wi-Fi + browser UI
        // 5. Create Phase 1 UI — Immersion panel (cafe settling)
        // 6. Create Phase 2 UI — Wi-Fi network list with evil twin
        // 7. Create Phase 3 UI — Browser with SSL stripping, certificate errors
        // 8. Create Phase 4 UI — Consequence panel
        // 9. Create Phase 5 UI — Debrief cards (Evil Twin, SSL Importance, VPN Protection)
        // 10. Setup audio sources and clips
        // 11. Add Scenario2_EvilTwinManager component
        // 12. Wire all serialized fields via SerializedObject
        // 13. Save scene and prefab

        Debug.LogWarning("[Scenario2Builder] Builder is a stub — implement following Scenario3Builder.cs pattern");
        EditorUtility.DisplayDialog(
            "Scenario 2 Builder",
            "This builder is a template stub.\n\n" +
            "Implement the full build pipeline following the pattern in Scenario3Builder.cs.\n\n" +
            "See PROJECT_STRUCTURE.md for architecture guidelines.",
            "OK"
        );
    }
}
