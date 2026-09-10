using UnityEngine;
using UnityEditor;

namespace Kannko.Editor
{
    public static class InspectChairOrientation
    {
        public static void Check()
        {
            GameObject chairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/BoothsChairsTables_SM_Chair_6.prefab");
            GameObject chair = Object.Instantiate(chairPrefab);
            chair.transform.position = Vector3.zero;
            chair.transform.rotation = Quaternion.identity;

            MeshFilter mf = chair.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Mesh m = mf.sharedMesh;
                Vector3[] verts = m.vertices;
                // Find min/max Y to find top of backrest and seat
                float highestY = float.MinValue;
                Vector3 highestVert = Vector3.zero;
                foreach (var v in verts)
                {
                    Vector3 worldV = mf.transform.TransformPoint(v);
                    if (worldV.y > highestY)
                    {
                        highestY = worldV.y;
                        highestVert = worldV;
                    }
                }
                Debug.Log($"[CHAIR_ORIENT] Chair top backrest is at world position: {highestVert}. If Z is negative, backrest is at -Z (meaning chair faces +Z towards table).");
            }
            Object.DestroyImmediate(chair);

            GameObject boothPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/BoothsChairsTables_SM_Booth_Single_4.prefab");
            GameObject booth = Object.Instantiate(boothPrefab);
            booth.transform.position = Vector3.zero;
            booth.transform.rotation = Quaternion.identity;

            MeshFilter bmf = booth.GetComponentInChildren<MeshFilter>();
            if (bmf != null && bmf.sharedMesh != null)
            {
                Mesh m = bmf.sharedMesh;
                Vector3[] verts = m.vertices;
                float highestY = float.MinValue;
                Vector3 highestVert = Vector3.zero;
                foreach (var v in verts)
                {
                    Vector3 worldV = bmf.transform.TransformPoint(v);
                    if (worldV.y > highestY)
                    {
                        highestY = worldV.y;
                        highestVert = worldV;
                    }
                }
                Debug.Log($"[CHAIR_ORIENT] Booth top backrest is at world position: {highestVert}. If Z is negative, backrest is at -Z.");
            }
            Object.DestroyImmediate(booth);
        }
    }
}
