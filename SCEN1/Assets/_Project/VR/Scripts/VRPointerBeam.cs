using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CADT.VR
{
    /// <summary>
    /// VR Pointer Beam for Right Hand Controller (and Left Hand).
    /// Tracks and connects the controller tip to World Space UI Canvas hit points, 
    /// moves an end reticle indicator, and triggers UI click events with visual snapping.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    [DisallowMultipleComponent]
    public class VRPointerBeam : MonoBehaviour
    {
        public enum AimMode
        {
            ControllerForward,   // Standard VR: Raycasts along controller tip's forward direction
            FollowMouseCursor,   // Desktop / Simulator: Raycasts from main camera through mouse cursor
            AutoDetect           // Automatically switches to mouse cursor when mouse moves in editor
        }

        [Header("--- 1. Ray Origin & Aiming ---")]
        [Tooltip("Transform representing the controller tip where the beam originates.")]
        [SerializeField] private Transform rayOrigin;

        [Tooltip("Aiming mode for the beam (VR controller forward or mouse cursor for simulator/desktop).")]
        [SerializeField] private AimMode aimMode = AimMode.AutoDetect;

        [Tooltip("Camera used for World Space UI raycasting. Defaults to Camera.main if not assigned.")]
        [SerializeField] private Camera eventCamera;

        [Tooltip("Maximum length of the beam when no target is hit (meters).")]
        [SerializeField] private float maxDistance = 5.0f;

        [Header("--- 2. Visual Connection (LineRenderer) ---")]
        [Tooltip("LineRenderer component used to draw the beam.")]
        [SerializeField] private LineRenderer lineRenderer;

        [Tooltip("Width of the beam at the controller tip.")]
        [SerializeField] private float startWidth = 0.006f;

        [Tooltip("Width of the beam at the hit point/reticle.")]
        [SerializeField] private float endWidth = 0.003f;

        [Tooltip("Default color gradient for the pointer beam.")]
        [SerializeField] private Color defaultColor = new Color(0.0f, 0.85f, 1.0f, 0.75f); // Cyan

        [Tooltip("Color when hovering over interactive UI or colliders.")]
        [SerializeField] private Color hoverColor = new Color(0.15f, 1.0f, 0.55f, 0.95f);  // Green

        [Tooltip("Color pulse when a click/trigger is pressed.")]
        [SerializeField] private Color clickColor = new Color(1.0f, 0.85f, 0.2f, 1.0f);   // Amber Gold

        [Header("--- 3. Hit Indicator (Reticle) ---")]
        [Tooltip("GameObject (small sphere or reticle) that moves to the hit position.")]
        [SerializeField] private GameObject reticleObject;

        [Tooltip("Scale of the reticle indicator.")]
        [SerializeField] private float reticleScale = 0.025f;

        [Tooltip("Whether to show the reticle at max distance when nothing is hit.")]
        [SerializeField] private bool showReticleWhenNoHit = true;

        [Header("--- 4. Detection & Snapping ---")]
        [Tooltip("Raycast against World Space UI Canvases (Buttons, Sliders, Text).")]
        [SerializeField] private bool raycastWorldUI = true;

        [Tooltip("Raycast against 3D Physics colliders (TV display, desk objects, etc.).")]
        [SerializeField] private bool raycastPhysics = true;

        [Tooltip("LayerMask filter for 3D physics raycasting.")]
        [SerializeField] private LayerMask physicsLayers = ~0;

        [Tooltip("Snaps the reticle to the center of the hovered UI element for magnetic aiming.")]
        [SerializeField] private bool snapToUIElements = false;

        [Tooltip("Smoothing speed when snapping to UI elements (0 for instant).")]
        [SerializeField] private float snapSmoothingSpeed = 20f;

#if ENABLE_INPUT_SYSTEM
        [Header("--- 5. Input Action (New Input System) ---")]
        [Tooltip("Input Action for controller trigger click (e.g. <XRController>{RightHand}/triggerPressed).")]
        [SerializeField] private InputActionProperty triggerAction;
#endif

        // Runtime state
        private Vector3 currentHitPoint;
        private Vector3 currentHitNormal = Vector3.forward;
        private GameObject currentHitGameObject;
        private bool isHittingTarget = false;
        private bool isHoveringInteractive = false;
        private float clickPulseTimer = 0f;
        private Vector3 lastMousePos;
        private PointerEventData pointerEventData;
        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
        private Material lineMaterialInstance;
        private Renderer reticleRenderer;

        public Vector3 CurrentHitPoint => currentHitPoint;
        public GameObject CurrentHitObject => currentHitGameObject;
        public bool IsHittingTarget => isHittingTarget;

        private void Awake()
        {
            // Auto-assign rayOrigin if not assigned
            if (rayOrigin == null)
            {
                rayOrigin = transform;
            }

            // Auto-assign or fetch LineRenderer
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    lineRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            SetupLineRenderer();

            // Auto-assign event camera
            if (eventCamera == null)
            {
                eventCamera = Camera.main;
                if (eventCamera == null)
                {
                    eventCamera = FindFirstObjectByType<Camera>();
                }
            }

            // If no reticle is assigned, create a default sphere reticle
            if (reticleObject == null)
            {
                CreateDefaultReticle();
            }
            else
            {
                reticleRenderer = reticleObject.GetComponent<Renderer>();
            }

            lastMousePos = Input.mousePosition;
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            if (triggerAction.action != null)
            {
                triggerAction.action.Enable();
            }
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            if (triggerAction.action != null)
            {
                triggerAction.action.Disable();
            }
#endif
            if (lineRenderer != null) lineRenderer.enabled = false;
            if (reticleObject != null) reticleObject.SetActive(false);
        }

        private void Update()
        {
            UpdatePointerAim();
            UpdateVisuals();
            CheckAndHandleClick();
        }

        /// <summary>
        /// Calculates the ray and performs UI / Physics raycasting.
        /// </summary>
        private void UpdatePointerAim()
        {
            if (rayOrigin == null) rayOrigin = transform;

            Ray ray = CalculateAimRay();

            isHittingTarget = false;
            isHoveringInteractive = false;
            currentHitGameObject = null;
            float closestDistance = maxDistance;
            Vector3 bestHitPoint = ray.origin + ray.direction * maxDistance;
            Vector3 bestHitNormal = -ray.direction;

            // 1. Raycast against World Space UI Canvases
            if (raycastWorldUI)
            {
                if (RaycastWorldSpaceUI(ray, out RaycastResult uiResult, out Vector3 uiHitPoint, out Vector3 uiHitNormal))
                {
                    if (uiResult.distance < closestDistance)
                    {
                        closestDistance = uiResult.distance;
                        bestHitPoint = uiHitPoint;
                        bestHitNormal = uiHitNormal;
                        currentHitGameObject = uiResult.gameObject;
                        isHittingTarget = true;
                        isHoveringInteractive = IsSelectableOrInteractive(currentHitGameObject);

                        // Optional UI magnetic snapping
                        if (snapToUIElements && isHoveringInteractive && uiResult.gameObject.transform is RectTransform rt)
                        {
                            Vector3 centerWorld = rt.TransformPoint(rt.rect.center);
                            bestHitPoint = Vector3.Lerp(bestHitPoint, centerWorld, 0.7f);
                        }
                    }
                }
            }

            // 2. Raycast against 3D Physics Colliders
            if (raycastPhysics)
            {
                if (Physics.Raycast(ray, out RaycastHit physHit, closestDistance, physicsLayers, QueryTriggerInteraction.Ignore))
                {
                    closestDistance = physHit.distance;
                    bestHitPoint = physHit.point;
                    bestHitNormal = physHit.normal;
                    currentHitGameObject = physHit.collider.gameObject;
                    isHittingTarget = true;
                    isHoveringInteractive = IsSelectableOrInteractive(currentHitGameObject);
                }
            }

            // Smooth position update
            if (snapSmoothingSpeed > 0f && Time.deltaTime > 0f)
            {
                currentHitPoint = Vector3.Lerp(currentHitPoint, bestHitPoint, Time.deltaTime * snapSmoothingSpeed);
            }
            else
            {
                currentHitPoint = bestHitPoint;
            }

            currentHitNormal = bestHitNormal;
        }

        /// <summary>
        /// Determines the 3D ray based on AimMode.
        /// </summary>
        private Ray CalculateAimRay()
        {
            Vector3 originPos = rayOrigin.position;
            Vector3 dir = rayOrigin.forward;

            bool useMouse = false;
            if (aimMode == AimMode.FollowMouseCursor)
            {
                useMouse = true;
            }
            else if (aimMode == AimMode.AutoDetect)
            {
                // If mouse position changed significantly in editor/simulator, aim towards mouse
                if (Vector3.Distance(Input.mousePosition, lastMousePos) > 1.0f)
                {
                    useMouse = true;
                    lastMousePos = Input.mousePosition;
                }
            }

            if (useMouse && eventCamera != null)
            {
                Ray mouseRay = eventCamera.ScreenPointToRay(Input.mousePosition);
                // Aim from controller tip towards the mouse ray target in the scene
                if (Physics.Raycast(mouseRay, out RaycastHit mouseHit, maxDistance * 2f, physicsLayers))
                {
                    dir = (mouseHit.point - originPos).normalized;
                }
                else
                {
                    Vector3 mousePlaneTarget = mouseRay.GetPoint(maxDistance);
                    dir = (mousePlaneTarget - originPos).normalized;
                }
            }

            return new Ray(originPos, dir);
        }

        /// <summary>
        /// Performs raycasting against World Space Canvases.
        /// </summary>
        private bool RaycastWorldSpaceUI(Ray ray, out RaycastResult hitResult, out Vector3 worldHitPoint, out Vector3 hitNormal)
        {
            hitResult = default;
            worldHitPoint = Vector3.zero;
            hitNormal = Vector3.forward;

            if (EventSystem.current == null) return false;

            Camera cam = eventCamera != null ? eventCamera : Camera.main;
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            float minDistance = float.MaxValue;
            bool foundHit = false;

            foreach (Canvas canvas in allCanvases)
            {
                if (!canvas.isActiveAndEnabled || canvas.renderMode != RenderMode.WorldSpace)
                    continue;

                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect == null) continue;

                // Test geometric intersection with the Canvas plane
                Plane canvasPlane = new Plane(-canvasRect.forward, canvasRect.position);
                if (canvasPlane.Raycast(ray, out float enter))
                {
                    if (enter > 0f && enter < minDistance && enter <= maxDistance)
                    {
                        Vector3 potentialHit = ray.GetPoint(enter);
                        Vector3 localPt = canvasRect.InverseTransformPoint(potentialHit);

                        // Check if point is within Canvas bounds
                        if (canvasRect.rect.Contains((Vector2)localPt))
                        {
                            // Convert world point to screen point for GraphicRaycaster test
                            Vector2 screenPos = cam != null 
                                ? RectTransformUtility.WorldToScreenPoint(cam, potentialHit)
                                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

                            if (pointerEventData == null)
                            {
                                pointerEventData = new PointerEventData(EventSystem.current);
                            }
                            pointerEventData.position = screenPos;

                            uiRaycastResults.Clear();
                            EventSystem.current.RaycastAll(pointerEventData, uiRaycastResults);

                            // Find the first result belonging to this canvas
                            for (int i = 0; i < uiRaycastResults.Count; i++)
                            {
                                RaycastResult r = uiRaycastResults[i];
                                if (r.isValid && r.gameObject != null && r.gameObject.transform.IsChildOf(canvas.transform))
                                {
                                    minDistance = enter;
                                    hitResult = r;
                                    hitResult.distance = enter;
                                    worldHitPoint = potentialHit;
                                    hitNormal = -canvasRect.forward;
                                    foundHit = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return foundHit;
        }

        /// <summary>
        /// Updates the LineRenderer positions and Reticle position/color.
        /// </summary>
        private void UpdateVisuals()
        {
            if (lineRenderer == null) return;

            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;

            // Point 0: Controller tip
            lineRenderer.SetPosition(0, rayOrigin.position);

            // Point 1: Hit point or max distance
            lineRenderer.SetPosition(1, currentHitPoint);

            // Reticle management
            if (reticleObject != null)
            {
                if (isHittingTarget || showReticleWhenNoHit)
                {
                    reticleObject.SetActive(true);
                    reticleObject.transform.position = currentHitPoint;

                    if (currentHitNormal != Vector3.zero)
                    {
                        reticleObject.transform.rotation = Quaternion.LookRotation(currentHitNormal);
                    }
                }
                else
                {
                    reticleObject.SetActive(false);
                }
            }

            // Determine beam & reticle color
            Color targetColor = defaultColor;
            if (clickPulseTimer > 0f)
            {
                clickPulseTimer -= Time.deltaTime;
                targetColor = clickColor;
            }
            else if (isHoveringInteractive)
            {
                targetColor = hoverColor;
            }

            ApplyColor(targetColor);
        }

        /// <summary>
        /// Checks for VR trigger or mouse click input and invokes the UI click event.
        /// </summary>
        private void CheckAndHandleClick()
        {
            bool clickPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (triggerAction.action != null && triggerAction.action.WasPressedThisFrame())
            {
                clickPressed = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                clickPressed = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0) || Input.GetButtonDown("Fire1"))
            {
                clickPressed = true;
            }
#endif

            if (clickPressed && isHittingTarget && currentHitGameObject != null)
            {
                TriggerUIClick(currentHitGameObject);
                clickPulseTimer = 0.15f; // Visual feedback flash
            }
        }

        /// <summary>
        /// Dispatches UI click events to buttons or click handlers.
        /// </summary>
        private void TriggerUIClick(GameObject target)
        {
            if (target == null) return;

            if (pointerEventData == null)
            {
                pointerEventData = new PointerEventData(EventSystem.current);
            }

            Camera cam = eventCamera != null ? eventCamera : Camera.main;
            pointerEventData.position = cam != null ? RectTransformUtility.WorldToScreenPoint(cam, currentHitPoint) : (Vector2)Input.mousePosition;

            // 1. Send standard PointerDown, PointerUp, and PointerClick
            ExecuteEvents.Execute(target, pointerEventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointerEventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointerEventData, ExecuteEvents.pointerClickHandler);

            // 2. Direct trigger for UGUI Button
            Button button = target.GetComponentInParent<Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }

            // 3. Direct trigger for UGUI Toggle
            Toggle toggle = target.GetComponentInParent<Toggle>();
            if (toggle != null && toggle.interactable)
            {
                toggle.isOn = !toggle.isOn;
            }
        }

        private bool IsSelectableOrInteractive(GameObject go)
        {
            if (go == null) return false;
            return go.GetComponentInParent<Selectable>() != null ||
                   go.GetComponentInParent<IPointerClickHandler>() != null;
        }

        private void SetupLineRenderer()
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;

            // Use an unlit/UI shader for clear visibility in VR
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") 
                         ?? Shader.Find("Sprites/Default") 
                         ?? Shader.Find("Unlit/Color");

            if (lineMaterialInstance == null)
            {
                lineMaterialInstance = new Material(shader);
            }
            lineRenderer.material = lineMaterialInstance;
            ApplyColor(defaultColor);
        }

        private void ApplyColor(Color col)
        {
            if (lineRenderer != null)
            {
                lineRenderer.startColor = col;
                lineRenderer.endColor = new Color(col.r, col.g, col.b, col.a * 0.4f);
            }

            if (reticleRenderer != null && reticleRenderer.material != null)
            {
                reticleRenderer.material.color = col;
            }
        }

        /// <summary>
        /// Creates a clean sphere reticle at runtime if none is provided.
        /// </summary>
        private void CreateDefaultReticle()
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Pointer_Reticle";
            sphere.transform.SetParent(transform, false);
            sphere.transform.localScale = Vector3.one * reticleScale;

            // Destroy collider so it doesn't block raycasts
            Collider col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            reticleRenderer = sphere.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") 
                         ?? Shader.Find("Sprites/Default") 
                         ?? Shader.Find("Unlit/Color");
            reticleRenderer.material = new Material(shader);
            reticleRenderer.material.color = defaultColor;

            reticleObject = sphere;
        }

        [ContextMenu("Setup Reticle & LineRenderer")]
        public void SetupComponentsInspector()
        {
            Awake();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("CADT/Setup Right Controller Pointer Beam", false, 10)]
        public static void SetupRightControllerInScene()
        {
            GameObject rightCtrl = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Right Controller");
            if (rightCtrl == null)
            {
                Debug.LogError("[CADT VR] Could not find 'XR Origin (XR Rig)/Camera Offset/Right Controller' in the scene.");
                return;
            }

            // 1. Create or find Controller Tip
            Transform tip = rightCtrl.transform.Find("Right_Controller_Tip");
            if (tip == null)
            {
                GameObject tipObj = new GameObject("Right_Controller_Tip");
                tipObj.transform.SetParent(rightCtrl.transform, false);
                tipObj.transform.localPosition = new Vector3(0f, 0f, 0.05f);
                tipObj.transform.localRotation = Quaternion.identity;
                tip = tipObj.transform;
            }

            // 2. Add or configure VRPointerBeam component
            VRPointerBeam beam = rightCtrl.GetComponent<VRPointerBeam>();
            if (beam == null)
            {
                beam = UnityEditor.Undo.AddComponent<VRPointerBeam>(rightCtrl);
            }

            // 3. Configure Serialized Properties
            UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(beam);
            so.FindProperty("rayOrigin").objectReferenceValue = tip;
            so.FindProperty("aimMode").enumValueIndex = (int)AimMode.AutoDetect;
            so.FindProperty("maxDistance").floatValue = 5.0f;
            so.FindProperty("raycastWorldUI").boolValue = true;
            so.FindProperty("raycastPhysics").boolValue = true;
            so.FindProperty("snapToUIElements").boolValue = true;

            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = FindFirstObjectByType<Camera>();
            if (mainCam != null)
            {
                so.FindProperty("eventCamera").objectReferenceValue = mainCam;
            }

            so.ApplyModifiedProperties();

            // 4. Create or link default Reticle
            Transform existingReticle = rightCtrl.transform.Find("Pointer_Reticle");
            if (existingReticle == null)
            {
                GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                reticle.name = "Pointer_Reticle";
                reticle.transform.SetParent(rightCtrl.transform, false);
                reticle.transform.localScale = Vector3.one * 0.025f;

                Collider col = reticle.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);

                Renderer rend = reticle.GetComponent<Renderer>();
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") 
                             ?? Shader.Find("Sprites/Default") 
                             ?? Shader.Find("Unlit/Color");
                rend.material = new Material(shader);
                rend.material.color = new Color(0.0f, 0.85f, 1.0f, 0.85f);

                UnityEditor.Undo.RegisterCreatedObjectUndo(reticle, "Create Pointer Reticle");

                so.Update();
                so.FindProperty("reticleObject").objectReferenceValue = reticle;
                so.ApplyModifiedProperties();
            }

            UnityEditor.EditorUtility.SetDirty(rightCtrl);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#10B981><b>[CADT VR]</b> Right Controller Pointer Beam successfully configured with Tip, Reticle, and UI Raycasting!</color>");
        }
#endif
    }
}
