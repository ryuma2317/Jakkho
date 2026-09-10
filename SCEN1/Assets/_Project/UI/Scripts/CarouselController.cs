using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CADT.VR.UI
{
    /// <summary>
    /// Smooth horizontal carousel controller for VR scenario selection cards.
    /// Provides next/previous navigation, snap-to-center physics, and active item callback.
    /// </summary>
    [DisallowMultipleComponent]
    public class CarouselController : MonoBehaviour
    {
        [Header("--- Carousel Setup ---")]
        [SerializeField] private RectTransform container;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button prevButton;
        [SerializeField] private float itemWidth = 320f;
        [SerializeField] private float spacing = 40f;
        [SerializeField] private float smoothSpeed = 10f;

        [Header("--- Events ---")]
        public UnityEvent<int> onIndexChanged;

        private int currentIndex = 0;
        private int totalItems = 0;
        private float targetX = 0f;

        public int CurrentIndex => currentIndex;

        private void Start()
        {
            if (nextButton != null) nextButton.onClick.AddListener(Next);
            if (prevButton != null) prevButton.onClick.AddListener(Previous);

            if (container != null)
            {
                totalItems = container.childCount;
            }

            UpdateTargetPosition();
        }

        private void Update()
        {
            if (container != null)
            {
                Vector2 currentPos = container.anchoredPosition;
                currentPos.x = Mathf.Lerp(currentPos.x, targetX, Time.deltaTime * smoothSpeed);
                container.anchoredPosition = currentPos;
            }
        }

        public void Next()
        {
            if (currentIndex < totalItems - 1)
            {
                currentIndex++;
                UpdateTargetPosition();
                onIndexChanged?.Invoke(currentIndex);
            }
        }

        public void Previous()
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                UpdateTargetPosition();
                onIndexChanged?.Invoke(currentIndex);
            }
        }

        public void SetIndex(int index)
        {
            currentIndex = Mathf.Clamp(index, 0, Mathf.Max(0, totalItems - 1));
            UpdateTargetPosition();
            onIndexChanged?.Invoke(currentIndex);
        }

        private void UpdateTargetPosition()
        {
            float step = itemWidth + spacing;
            targetX = -(currentIndex * step);
        }
    }
}
