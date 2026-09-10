using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CADT.VR.UI
{
    /// <summary>
    /// Animates dynamic audio waveform bars on holographic UI displays
    /// during phone calls, dialogue speech, or alarm siren broadcasts.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioWaveformVisualizer : MonoBehaviour
    {
        [Header("--- Waveform Configuration ---")]
        [SerializeField] private List<RectTransform> barTransforms = new List<RectTransform>();
        [SerializeField] private float minHeight = 6f;
        [SerializeField] private float maxHeight = 48f;
        [SerializeField] private float animationSpeed = 14f;
        [SerializeField] private bool isActive = false;

        private float[] targetHeights;

        private void Awake()
        {
            if (barTransforms.Count == 0)
            {
                foreach (RectTransform child in transform)
                {
                    barTransforms.Add(child);
                }
            }

            targetHeights = new float[barTransforms.Count];
        }

        private void Update()
        {
            if (!isActive)
            {
                for (int i = 0; i < barTransforms.Count; i++)
                {
                    SetBarHeight(barTransforms[i], Mathf.Lerp(barTransforms[i].sizeDelta.y, minHeight, Time.deltaTime * animationSpeed));
                }
                return;
            }

            for (int i = 0; i < barTransforms.Count; i++)
            {
                if (Mathf.Abs(barTransforms[i].sizeDelta.y - targetHeights[i]) < 2f || Random.value < 0.2f)
                {
                    targetHeights[i] = Random.Range(minHeight, maxHeight);
                }

                float current = Mathf.Lerp(barTransforms[i].sizeDelta.y, targetHeights[i], Time.deltaTime * animationSpeed);
                SetBarHeight(barTransforms[i], current);
            }
        }

        public void SetActive(bool active)
        {
            isActive = active;
        }

        private void SetBarHeight(RectTransform rt, float height)
        {
            if (rt == null) return;
            Vector2 size = rt.sizeDelta;
            size.y = height;
            rt.sizeDelta = size;
        }
    }
}
