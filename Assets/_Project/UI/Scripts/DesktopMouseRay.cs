#if UNITY_EDITOR || UNITY_STANDALONE
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Desktop/editor testing helper: draws a laser from the main camera that follows
/// the mouse pointer and lets you hover + click UI (menus, scenario exit panels)
/// exactly like a VR controller ray would. Automatically disabled when a real
/// head-mounted display is active, and excluded from XR device builds.
/// No scene setup needed - it bootstraps itself.
/// </summary>
public class DesktopMouseRay : MonoBehaviour
{
    private LineRenderer line;
    private GameObject dot;
    private static readonly Color RayColor = new Color(0.2f, 0.9f, 1f, 0.95f);

    private readonly List<GraphicRaycaster> raycasters = new List<GraphicRaycaster>();
    private float nextRescan;
    private GameObject hovered;
    private float nextHmdCheck;
    private bool hmdPresent;

    private PointerEventData pointerData;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Object.FindFirstObjectByType<DesktopMouseRay>() != null) return;
        var go = new GameObject("DesktopMouseRay");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<DesktopMouseRay>();
    }

    private void Awake()
    {
        var lineGO = new GameObject("MouseRayLine");
        lineGO.transform.SetParent(transform, false);
        line = lineGO.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = 0.006f;
        line.endWidth = 0.002f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = CreateRayMaterial();
        line.enabled = false;

        dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = "MouseRayDot";
        dot.transform.SetParent(transform, false);
        dot.transform.localScale = Vector3.one * 0.014f;
        Object.Destroy(dot.GetComponent<Collider>());
        dot.GetComponent<Renderer>().material = line.material;
        dot.SetActive(false);
    }

    private Material CreateRayMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", RayColor);
        mat.color = RayColor;
        return mat;
    }

    private void OnDestroy()
    {
        if (line != null) Destroy(line.material);
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextHmdCheck)
        {
            nextHmdCheck = Time.unscaledTime + 1f;
            var devices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.HeadMounted, devices);
            hmdPresent = devices.Count > 0;
        }

        if (hmdPresent || Camera.main == null || Mouse.current == null)
        {
            SetVisible(false);
            return;
        }

        if (Time.unscaledTime >= nextRescan)
        {
            nextRescan = Time.unscaledTime + 1f;
            raycasters.Clear();
            raycasters.AddRange(Object.FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None));
        }

        var mousePos = Mouse.current.position.ReadValue();
        var ray = Camera.main.ScreenPointToRay(mousePos);

        if (EventSystem.current == null)
        {
            SetVisible(false);
            return;
        }

        if (pointerData == null) pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = mousePos;
        pointerData.delta = Vector2.zero;

        Button hoverButton = null;
        Vector3 endPoint = ray.origin + ray.direction * 12f;
        float bestDistance = float.MaxValue;
        var results = new List<RaycastResult>();
        foreach (var rc in raycasters)
        {
            if (rc == null || !rc.isActiveAndEnabled) continue;
            results.Clear();
            rc.Raycast(pointerData, results);
            foreach (var r in results)
            {
                if (r.distance < bestDistance)
                {
                    var btn = r.gameObject.GetComponentInParent<Button>();
                    if (btn != null && btn.isActiveAndEnabled && btn.interactable)
                    {
                        bestDistance = r.distance;
                        hoverButton = btn;
                        endPoint = r.worldPosition;
                    }
                }
            }
        }

        var hoverGO = hoverButton != null ? hoverButton.gameObject : null;
        if (hoverGO != hovered)
        {
            if (hovered != null)
                ExecuteEvents.Execute(hovered, pointerData, ExecuteEvents.pointerExitHandler);
            hovered = hoverGO;
            if (hovered != null)
                ExecuteEvents.Execute(hovered, pointerData, ExecuteEvents.pointerEnterHandler);
        }

        SetVisible(true);
        line.SetPosition(0, ray.origin + ray.direction * 0.12f);
        line.SetPosition(1, endPoint);
        dot.transform.position = endPoint;

        if (Mouse.current.leftButton.wasPressedThisFrame && hoverButton != null)
        {
            ExecuteEvents.Execute(hoverButton.gameObject, pointerData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(hoverButton.gameObject, pointerData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hoverButton.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
            Debug.Log("[DesktopMouseRay] Clicked UI Button: " + hoverButton.gameObject.name);
        }
    }

    private void SetVisible(bool visible)
    {
        if (line != null && line.enabled != visible) line.enabled = visible;
        if (dot != null && dot.activeSelf != visible) dot.SetActive(visible);
        if (!visible && hovered != null)
        {
            if (pointerData != null)
                ExecuteEvents.Execute(hovered, pointerData, ExecuteEvents.pointerExitHandler);
            hovered = null;
        }
    }
}
#endif
