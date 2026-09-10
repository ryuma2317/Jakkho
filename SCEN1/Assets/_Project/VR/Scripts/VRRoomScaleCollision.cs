using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CADT.VR.Interaction
{
    /// <summary>
    /// Prevents VR players from clipping their headset through walls or physical furniture.
    /// Fades the display to black when physical head movement penetrates solid geometry.
    /// </summary>
    [DisallowMultipleComponent]
    public class VRRoomScaleCollision : MonoBehaviour
    {
        [Header("--- Headset & Collision ---")]
        [SerializeField] private Transform headCameraTransform;
        [SerializeField] private float collisionRadius = 0.12f;
        [SerializeField] private LayerMask obstacleLayers;

        [Header("--- Visual Fade ---")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private float fadeSpeed = 8f;

        private bool isPenetrating = false;
        private float currentFade = 0f;

        private void Awake()
        {
            if (headCameraTransform == null && Camera.main != null)
            {
                headCameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (headCameraTransform == null) return;

            bool hit = Physics.CheckSphere(headCameraTransform.position, collisionRadius, obstacleLayers);
            isPenetrating = hit;

            float targetFade = isPenetrating ? 1f : 0f;
            currentFade = Mathf.MoveTowards(currentFade, targetFade, Time.deltaTime * fadeSpeed);

            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = currentFade;
                fadeImage.color = c;
                fadeImage.gameObject.SetActive(currentFade > 0.01f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (headCameraTransform != null)
            {
                Gizmos.color = isPenetrating ? Color.red : Color.green;
                Gizmos.DrawWireSphere(headCameraTransform.position, collisionRadius);
            }
        }
    }
}
