using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace VRSetup
{
    /// <summary>
    /// Utility script that configures stereoscopic VR camera settings
    /// and builds glowing laser beam visuals for both VR controllers.
    /// </summary>
    [DisallowMultipleComponent]
    public class VRLaserSetupUtility : MonoBehaviour
    {
        [Header("Laser Visual Settings")]
        [SerializeField] private Color laserColor = new Color(0f, 1f, 1f, 0.9f); // Cyan
        [SerializeField] private float startWidth = 0.006f;
        [SerializeField] private float endWidth = 0.002f;

        private void Start()
        {
            ApplyCameraAndLaserSettings();
        }

        public void ApplyCameraAndLaserSettings()
        {
            // 1. Configure Main Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.stereoTargetEye = StereoTargetEyeMask.Both;
                mainCam.nearClipPlane = 0.01f;
                mainCam.farClipPlane = 1000f;
                mainCam.tag = "MainCamera";
            }

            // 2. Configure Laser Lines on Left & Right Controllers
            SetupControllerLaser(GameObject.Find("Left Controller"));
            SetupControllerLaser(GameObject.Find("Right Controller"));
        }

        private void SetupControllerLaser(GameObject controllerObj)
        {
            if (controllerObj == null) return;

            // Find or get LineRenderer and XRInteractorLineVisual
            LineRenderer lineRenderer = controllerObj.GetComponentInChildren<LineRenderer>(true);
            XRInteractorLineVisual lineVisual = controllerObj.GetComponentInChildren<XRInteractorLineVisual>(true);

            if (lineRenderer != null)
            {
                lineRenderer.startWidth = startWidth;
                lineRenderer.endWidth = endWidth;
                lineRenderer.useWorldSpace = true;
                lineRenderer.enabled = true;

                // Create glowing unlit material if missing
                if (lineRenderer.sharedMaterial == null || lineRenderer.sharedMaterial.name.Contains("Default"))
                {
                    Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
                    if (unlitShader != null)
                    {
                        Material mat = new Material(unlitShader);
                        mat.color = laserColor;
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", laserColor);
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", laserColor * 2f);
                        }
                        lineRenderer.material = mat;
                    }
                }
            }

            if (lineVisual != null)
            {
                lineVisual.enabled = true;
                
                // Build glowing gradient
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(laserColor, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 1f) }
                );

                lineVisual.validColorGradient = grad;
                lineVisual.invalidColorGradient = grad;
            }
        }
    }
}
