using UnityEngine;
using UnityEditor;

namespace Kannko.Editor
{
    public static class InspectStarterRig
    {
        public static void Inspect()
        {
            string rigPath = "Assets/Samples/XR Interaction Toolkit/3.0.7/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
            GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            if (rigPrefab == null)
            {
                Debug.LogError($"[STARTER_RIG] Rig prefab not found: {rigPath}");
                return;
            }

            GameObject instance = Object.Instantiate(rigPrefab);
            PrintHierarchy(instance.transform, "");
            Object.DestroyImmediate(instance);
        }

        private static void PrintHierarchy(Transform t, string indent)
        {
            Component[] comps = t.GetComponents<Component>();
            string compNames = "";
            foreach (var c in comps)
            {
                if (c == null || c is Transform) continue;
                compNames += $" [{c.GetType().Name}]";
            }

            Debug.Log($"[STARTER_RIG] {indent}{t.name}{compNames}");

            for (int i = 0; i < t.childCount; i++)
            {
                PrintHierarchy(t.GetChild(i), indent + "  ");
            }
        }
    }
}
