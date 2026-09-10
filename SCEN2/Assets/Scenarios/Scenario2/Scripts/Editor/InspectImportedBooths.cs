using UnityEngine;
using UnityEditor;

namespace Kannko.Editor
{
    public static class InspectImportedBooths
    {
        public static void Inspect()
        {
            string[] prefabPaths = new string[]
            {
                "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/BoothsChairsTables_SM_Table_8.prefab",
                "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/BoothsChairsTables_SM_Chair_6.prefab",
                "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/BoothsChairsTables_SM_Booth_Single_4.prefab"
            };

            foreach (string path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogError($"[BOOTH_INSPECT] Could not load: {path}");
                    continue;
                }

                GameObject instance = Object.Instantiate(prefab);
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.identity;

                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                Bounds totalBounds = new Bounds(Vector3.zero, Vector3.zero);
                bool hasBounds = false;

                foreach (Renderer r in renderers)
                {
                    if (r is MeshRenderer mr)
                    {
                        if (!hasBounds)
                        {
                            totalBounds = mr.bounds;
                            hasBounds = true;
                        }
                        else
                        {
                            totalBounds.Encapsulate(mr.bounds);
                        }

                        Debug.Log($"[BOOTH_INSPECT] {prefab.name} -> Child: {mr.name}, Mat: {(mr.sharedMaterial != null ? mr.sharedMaterial.name : "null")}, Shader: {(mr.sharedMaterial != null && mr.sharedMaterial.shader != null ? mr.sharedMaterial.shader.name : "null")}");
                    }
                }

                Collider[] colliders = instance.GetComponentsInChildren<Collider>();
                Debug.Log($"[BOOTH_INSPECT] {prefab.name} TOTAL BOUNDS: Center={totalBounds.center}, Size={totalBounds.size}, Min={totalBounds.min}, Max={totalBounds.max}, CollidersCount={colliders.Length}");

                Object.DestroyImmediate(instance);
            }
        }
    }
}
