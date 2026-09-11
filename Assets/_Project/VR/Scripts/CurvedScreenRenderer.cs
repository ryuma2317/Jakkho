using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Renders a flat world-space Canvas into a RenderTexture and displays it on this
/// (cylindrically curved) mesh. The matching TrackedDeviceGraphicRaycaster on the
/// canvas gets its curveRadiusWorld set so controller rays are unrolled from the
/// same cylinder, keeping UI interaction aligned with the curved visuals.
///
/// The display mesh must be a child of the canvas (so it inherits the canvas pose
/// and scale) but on a normal layer; the flat canvas itself is moved to a dedicated
/// layer that only the RT camera renders.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class CurvedScreenRenderer : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private float curveRadiusWorld = 1.403f;

    [Header("Render Texture")]
    [SerializeField] private int renderWidth = 2048;
    [SerializeField] private int renderHeight = 1152;
    [SerializeField] private int antiAliasing = 4;
    [SerializeField] private Color clearColor = new Color(0.94f, 0.95f, 0.97f, 1f);

    [Header("Camera")]
    [SerializeField] private float cameraDistance = 0.35f;
    [SerializeField] private float nearClip = 0.01f;
    [SerializeField] private float farClip = 1.5f;

    private RenderTexture rt;
    private Camera rtCamera;

    private void Awake()
    {
        if (targetCanvas == null)
        {
            Debug.LogError("[CurvedScreenRenderer] No target canvas assigned.", this);
            return;
        }

        var canvasRect = targetCanvas.GetComponent<RectTransform>();
        var scale = canvasRect.lossyScale.x;

        rt = new RenderTexture(renderWidth, renderHeight, 24, RenderTextureFormat.ARGB32)
        {
            name = "ScreenRT",
            antiAliasing = antiAliasing,
        };
        rt.Create();

        var mat = GetComponent<MeshRenderer>().sharedMaterial;
        if (mat != null)
            mat.SetTexture("_BaseMap", rt); // URP/Unlit uses _BaseMap, not _MainTex

// RT camera: scene root (no inherited scale!), on the player-facing side of the
        // canvas, looking at it. Culls ONLY the canvas layer.
        var camObj = new GameObject("ScreenRTCamera");
        camObj.transform.position = targetCanvas.transform.position - targetCanvas.transform.forward * cameraDistance;
        camObj.transform.rotation = targetCanvas.transform.rotation;

        rtCamera = camObj.AddComponent<Camera>();
        rtCamera.orthographic = true;
        rtCamera.orthographicSize = 450f * scale; // half canvas height (900px) in world units
        rtCamera.nearClipPlane = nearClip;
        rtCamera.farClipPlane = farClip;
        rtCamera.targetTexture = rt;
        rtCamera.cullingMask = 1 << targetCanvas.gameObject.layer;
        rtCamera.clearFlags = CameraClearFlags.SolidColor;
        rtCamera.backgroundColor = clearColor;

        var raycaster = targetCanvas.GetComponent<TrackedDeviceGraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.curveRadiusWorld = curveRadiusWorld;
        }
        else
        {
            Debug.LogWarning("[CurvedScreenRenderer] Canvas has no TrackedDeviceGraphicRaycaster; curved raycasting disabled.", this);
        }
    }

    private void OnDestroy()
    {
        if (rtCamera != null && rtCamera.gameObject != null)
            Destroy(rtCamera.gameObject);
        if (rt != null)
            rt.Release();
    }
}