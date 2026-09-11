#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;

/// <summary>
/// Quick switch between the XR Interaction Simulator (desktop testing, no headset)
/// and a real headset (Quest via Link / any OpenXR device) for Standalone play mode.
/// </summary>
public static class XRSimulationToggle
{
    private const string SimulationLoaderPath = "Assets/XR/Loaders/SimulationLoader.asset";

    [MenuItem("JAKKHO VR/Testing - Use Real Headset (disable XR Simulation)", false, 20)]
    public static void UseRealHeadset()
    {
        SetSimulation(false);
    }

    [MenuItem("JAKKHO VR/Testing - Use XR Simulator (enable XR Simulation)", false, 21)]
    public static void UseSimulator()
    {
        SetSimulation(true);
    }

    private static void SetSimulation(bool enabled)
    {
        var perTarget = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (perTarget == null || perTarget.Manager == null)
        {
            Debug.LogWarning("[XRSimulationToggle] Could not access Standalone XR settings.");
            return;
        }

        var simLoader = AssetDatabase.LoadAssetAtPath<XRLoader>(SimulationLoaderPath);
        if (simLoader == null)
        {
            Debug.LogWarning("[XRSimulationToggle] SimulationLoader.asset not found at " + SimulationLoaderPath);
            return;
        }

        bool has = false;
        foreach (var loader in perTarget.Manager.activeLoaders)
        {
            if (loader == simLoader) { has = true; break; }
        }

        if (enabled && !has)
        {
            if (perTarget.Manager.TryAddLoader(simLoader))
                Debug.Log("<color=#00f5d4>[XRSimulationToggle] XR Simulation ENABLED - desktop simulator devices active (no headset).</color>");
        }
        else if (!enabled && has)
        {
            if (perTarget.Manager.TryRemoveLoader(simLoader))
                Debug.Log("<color=#00f5d4>[XRSimulationToggle] XR Simulation DISABLED - a connected headset (OpenXR) will drive instead.</color>");
        }
        else
        {
            Debug.Log("[XRSimulationToggle] Already " + (enabled ? "enabled" : "disabled") + ".");
        }

        EditorUtility.SetDirty(perTarget.Manager);
        AssetDatabase.SaveAssets();
    }
}
#endif
