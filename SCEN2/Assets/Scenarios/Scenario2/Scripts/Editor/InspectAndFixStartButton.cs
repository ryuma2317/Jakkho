using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using Kannko.Scenario;

namespace Kannko.Editor
{
    public static class InspectAndFixStartButton
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";

        [MenuItem("VR Prototype/Inspect & Fix Start Button", priority = 5)]
        public static void InspectAndFix()
        {
            Debug.Log("=================================================================");
            Debug.Log("[Inspect & Fix Start Button] Diagnostic and repair starting...");
            Debug.Log("=================================================================");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[Inspect & Fix] Scene file not found: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Unparent XR Device Simulator to ROOT level to fix DontDestroyOnLoad exception
            GameObject[] allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allGameObjects)
            {
                if (go.name == "XR Device Simulator")
                {
                    if (go.transform.parent != null)
                    {
                        go.transform.SetParent(null);
                        Debug.Log("[Inspect & Fix] Unparented 'XR Device Simulator' to scene ROOT.");
                    }
                }
            }

            // 2. Ensure EventSystem has XRUIInputModule & InputSystemUIInputModule
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                eventSystem = esGo.AddComponent<EventSystem>();
                esGo.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
                Debug.Log("[Inspect & Fix] Created missing EventSystem with XRUIInputModule.");
            }
            else
            {
                var xrModule = eventSystem.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
                if (xrModule == null)
                {
                    eventSystem.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
                    Debug.Log("[Inspect & Fix] Added XRUIInputModule to existing EventSystem.");
                }

                var inputSystemModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (inputSystemModule == null)
                {
                    eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    Debug.Log("[Inspect & Fix] Added InputSystemUIInputModule to EventSystem for mouse/keyboard inputs.");
                }
            }

            // 3. Find Scenario2IntroController & Canvas
            Scenario2IntroController introController = Object.FindFirstObjectByType<Scenario2IntroController>(FindObjectsInactive.Include);
            if (introController == null)
            {
                Debug.LogError("[Inspect & Fix] Scenario2IntroController not found in scene!");
            }
            else
            {
                introController.gameObject.SetActive(true);
                Debug.Log($"[Inspect & Fix] Found Scenario2IntroController on '{introController.name}'. Set active.");
            }

            Canvas introCanvas = introController != null ? introController.GetComponentInParent<Canvas>(true) : null;
            if (introCanvas == null)
            {
                GameObject panelGo = GameObject.Find("Scenario2IntroPanel");
                if (panelGo != null) introCanvas = panelGo.GetComponentInParent<Canvas>(true);
            }

            if (introCanvas != null)
            {
                introCanvas.gameObject.SetActive(true);
                Debug.Log($"[Inspect & Fix] Found Intro Canvas: '{introCanvas.name}', RenderMode: {introCanvas.renderMode}");

                Camera mainCam = Camera.main;
                if (introCanvas.renderMode == RenderMode.WorldSpace)
                {
                    if (mainCam != null)
                    {
                        introCanvas.worldCamera = mainCam;
                    }
                }

                // Add GraphicRaycaster for Mouse Clicks
                var graphicRaycaster = introCanvas.GetComponent<GraphicRaycaster>();
                if (graphicRaycaster == null)
                {
                    introCanvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log("[Inspect & Fix] Added GraphicRaycaster to Intro Canvas.");
                }

                // Add TrackedDeviceGraphicRaycaster for VR Controller Rays
                var trackedRaycaster = introCanvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
                if (trackedRaycaster == null)
                {
                    introCanvas.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
                    Debug.Log("[Inspect & Fix] Added TrackedDeviceGraphicRaycaster to Intro Canvas.");
                }

                CanvasGroup cg = introCanvas.GetComponent<CanvasGroup>();
                if (cg == null) cg = introCanvas.gameObject.AddComponent<CanvasGroup>();
                cg.interactable = true;
                cg.blocksRaycasts = true;
                cg.alpha = 1f;
            }

            // 4. Find and Configure Start Button
            Button startBtn = null;
            if (introController != null)
            {
                startBtn = introController.GetComponentInChildren<Button>(true);
            }
            if (startBtn == null)
            {
                GameObject btnGo = GameObject.Find("Scenario2StartButton");
                if (btnGo == null) btnGo = GameObject.Find("StartButton");
                if (btnGo != null) startBtn = btnGo.GetComponent<Button>();
            }

            if (startBtn != null)
            {
                startBtn.gameObject.SetActive(true);
                startBtn.interactable = true;
                Debug.Log($"[Inspect & Fix] Configured Start Button on '{startBtn.gameObject.name}'");

                // Ensure targetGraphic and all images on button have raycastTarget = true
                var images = startBtn.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    img.raycastTarget = true;
                }

                if (introController != null)
                {
                    // Wire serialized field
                    SerializedObject controllerSo = new SerializedObject(introController);
                    SerializedProperty startBtnProp = controllerSo.FindProperty("m_StartButton");
                    if (startBtnProp != null)
                    {
                        startBtnProp.objectReferenceValue = startBtn;
                        controllerSo.ApplyModifiedProperties();
                    }

                    // Add persistent onClick listener to OnStartButtonClicked
                    bool hasListener = false;
                    for (int i = 0; i < startBtn.onClick.GetPersistentEventCount(); i++)
                    {
                        Object targetObj = startBtn.onClick.GetPersistentTarget(i);
                        string method = startBtn.onClick.GetPersistentMethodName(i);
                        if (targetObj == introController && method == "OnStartButtonClicked")
                        {
                            hasListener = true;
                            break;
                        }
                    }

                    if (!hasListener)
                    {
                        UnityEditor.Events.UnityEventTools.AddPersistentListener(startBtn.onClick, introController.OnStartButtonClicked);
                        Debug.Log("[Inspect & Fix] Successfully added persistent OnStartButtonClicked listener to Start Button onClick!");
                    }
                }
            }
            else
            {
                Debug.LogError("[Inspect & Fix] Start Button component COULD NOT BE FOUND in scene!");
            }

            // 5. Ensure XR Ray Interactors have enableUIInteraction = true
            var rays = Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ray in rays)
            {
                ray.enableUIInteraction = true;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("=================================================================");
            Debug.Log("[Inspect & Fix Start Button] Complete and scene saved!");
            Debug.Log("=================================================================");
        }
    }
}
