using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Kannko.Editor
{
    public static class CheckCloseButton
    {
        private const string PrefabPath = "Assets/Samples/XR Interaction Toolkit/3.0.7/XR Device Simulator/UI/XR Device Simulator UI.prefab";

        [MenuItem("VR Prototype/Check Close Button", priority = 50)]
        public static void Check()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("Prefab not found!");
                return;
            }

            Transform mainPanel = prefab.transform.Find("XR Device Simulator Main Panel");
            if (mainPanel == null)
            {
                Debug.LogError("XR Device Simulator Main Panel not found on prefab!");
                return;
            }

            Transform titleBar = mainPanel.Find("TitleBar");
            if (titleBar != null)
            {
                Transform closeBtnTrans = titleBar.Find("CloseUIBtn");
                if (closeBtnTrans != null)
                {
                    Button btn = closeBtnTrans.GetComponent<Button>();
                    Debug.Log($"CloseUIBtn found! Button active: {btn != null}, Persistent listeners count: {btn?.onClick.GetPersistentEventCount()}");
                    if (btn != null)
                    {
                        for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
                        {
                            Debug.Log($"Listener {i}: target={btn.onClick.GetPersistentTarget(i)?.name}, method={btn.onClick.GetPersistentMethodName(i)}");
                        }
                    }
                }
                else
                {
                    Debug.LogError("CloseUIBtn transform not found under TitleBar!");
                }
            }
            else
            {
                Debug.LogError("TitleBar transform not found under Main Panel!");
            }
        }
    }
}
