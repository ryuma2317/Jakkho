using UnityEngine;
using UnityEditor;

namespace Kannko.Editor
{
    public static class InspectLeartesAssets
    {
        public static void Inspect()
        {
            string[] prefabs = new string[]
            {
                "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/WallsAndFloors_SM_Wall_Windows_3m_9.prefab",
                "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/Walls_SM_Wall_3x4m.prefab",
                "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/SM_FloorCeiling_3x3m.prefab",
                "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/CeilingProps_SM_AC_Box_16.prefab",
                "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/SM_CeilingLamp_14.prefab",
                "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/WallProps_SM_Chalkboard_6.prefab",
                "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/WallProps_SM_WallPainting_10.prefab",
                "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/SM_PlantPot_74.prefab"
            };

            foreach (var p in prefabs)
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go == null)
                {
                    Debug.LogWarning($"[INSPECT_LEARTES] Missing: {p}");
                    continue;
                }
                Renderer r = go.GetComponentInChildren<Renderer>();
                string matInfo = "No Renderer";
                if (r != null && r.sharedMaterial != null)
                {
                    matInfo = $"Mat: {r.sharedMaterial.name}, Shader: {r.sharedMaterial.shader.name}";
                }
                Debug.Log($"[INSPECT_LEARTES] {go.name} -> {matInfo}");
            }
        }
    }
}
