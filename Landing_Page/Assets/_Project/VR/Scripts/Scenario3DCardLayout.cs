using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRSetup
{
    /// <summary>
    /// Arranges scenario cards in a 3D Cover Flow / fanned-out perspective layout.
    /// The center active card is large, straight-on (rotY=0), brought forward, and fully bright.
    /// Side cards are pushed back in Z, scaled down, rotated on Y, and dimmed.
    /// Supports clicking side cards or using arrow buttons to cycle between scenarios.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class Scenario3DCardLayout : MonoBehaviour
    {
        [Header("Card Configuration")]
        [Tooltip("The list of scenario card RectTransforms to lay out.")]
        public List<RectTransform> cards = new List<RectTransform>();

        [Tooltip("Currently focused / selected card index.")]
        [SerializeField] public int selectedIndex = 1;

        [Header("3D Layout Settings")]
        [Tooltip("Scale of the active center card.")]
        public float centerScale = 1.06f;

        [Tooltip("Scale of the side cards.")]
        public float sideScale = 0.82f;

        [Tooltip("Horizontal spacing between cards.")]
        public float horizontalSpacing = 370f;

        [Tooltip("Z-depth of the center card (negative moves closer to camera).")]
        public float centerZ = -25f;

        [Tooltip("Z-depth pushback for side cards.")]
        public float sideZ = 90f;

        [Tooltip("Y-axis rotation angle in degrees for side cards (fanned out).")]
        public float sideRotationY = 24f;

        [Tooltip("Vertical Y offset for all cards.")]
        public float cardCenterY = -30f;

        [Header("Dimming & Opacity")]
        [Range(0.1f, 1f)]
        [Tooltip("Alpha / brightness for the active center card.")]
        public float centerAlpha = 1.0f;

        [Range(0.1f, 1f)]
        [Tooltip("Alpha / brightness for dimmed side cards.")]
        public float sideAlpha = 0.45f;

        [Header("Animation")]
        [Tooltip("Transition smoothing speed.")]
        public float transitionSpeed = 10f;

        [Header("Navigation Buttons (Optional)")]
        public Button prevButton;
        public Button nextButton;

        private void Awake()
        {
            FindCardsIfEmpty();
        }

        private void Start()
        {
            FindCardsIfEmpty();
            SetupCardClickListeners();
            SetupNavButtons();
            UpdateLayoutImmediate();
        }

        private void OnEnable()
        {
            FindCardsIfEmpty();
            UpdateLayoutImmediate();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cards == null || cards.Count == 0)
            {
                FindCardsIfEmpty();
            }
            UpdateLayoutImmediate();
        }
#endif

        private void Update()
        {
            if (cards == null || cards.Count == 0) return;

            if (Application.isPlaying)
            {
                AnimateLayout(Time.deltaTime);
            }
            else
            {
                UpdateLayoutImmediate();
            }
        }

        public void FindCardsIfEmpty()
        {
            if (cards == null) cards = new List<RectTransform>();

            cards.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("Card_") || child.name.Contains("ScenarioCard") || child.GetComponent<ScenarioCardUI>() != null)
                {
                    var rt = child.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        cards.Add(rt);
                        EnsureCanvasGroup(rt);
                    }
                }
            }

            if (cards.Count > 0)
            {
                selectedIndex = Mathf.Clamp(selectedIndex, 0, cards.Count - 1);
            }
        }

        private void EnsureCanvasGroup(RectTransform rt)
        {
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = rt.gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void SetupCardClickListeners()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                int index = i;
                var card = cards[i];
                if (card == null) continue;

                var cardBtn = card.GetComponent<Button>();
                if (cardBtn == null)
                {
                    cardBtn = card.gameObject.AddComponent<Button>();
                    cardBtn.transition = Selectable.Transition.None;
                }

                cardBtn.onClick.RemoveAllListeners();
                cardBtn.onClick.AddListener(() => OnCardClicked(index));

                var col = card.GetComponent<BoxCollider>();
                if (col == null)
                {
                    col = card.gameObject.AddComponent<BoxCollider>();
                    col.size = new Vector3(card.sizeDelta.x, card.sizeDelta.y, 15f);
                    col.center = Vector3.zero;
                }
            }
        }

        public void SetupNavButtons()
        {
            if (prevButton != null)
            {
                prevButton.onClick.RemoveListener(SelectPreviousCard);
                prevButton.onClick.AddListener(SelectPreviousCard);
            }
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(SelectNextCard);
                nextButton.onClick.AddListener(SelectNextCard);
            }
        }

        public void OnCardClicked(int index)
        {
            if (index != selectedIndex)
            {
                SetSelectedIndex(index);
            }
        }

        public void SetSelectedIndex(int index)
        {
            if (cards == null || cards.Count == 0) return;
            selectedIndex = Mathf.Clamp(index, 0, cards.Count - 1);
        }

        public void SelectPreviousCard()
        {
            if (cards == null || cards.Count == 0) return;
            int newIdx = selectedIndex - 1;
            if (newIdx < 0) newIdx = cards.Count - 1;
            SetSelectedIndex(newIdx);
        }

        public void SelectNextCard()
        {
            if (cards == null || cards.Count == 0) return;
            int newIdx = (selectedIndex + 1) % cards.Count;
            SetSelectedIndex(newIdx);
        }

        public void UpdateLayoutImmediate()
        {
            if (cards == null || cards.Count == 0) return;

            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null) continue;

                GetTargetTransform(i, out Vector3 targetPos, out Quaternion targetRot, out Vector3 targetScale, out float targetAlpha);

                card.anchoredPosition3D = targetPos;
                card.localRotation = targetRot;
                card.localScale = targetScale;

                var cg = card.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = targetAlpha;
                    bool isCenter = (i == selectedIndex);
                    cg.interactable = true;
                    cg.blocksRaycasts = true;

                    var playBtn = card.Find("PlayScenarioButton")?.GetComponent<Button>();
                    if (playBtn != null)
                    {
                        playBtn.interactable = isCenter;
                    }
                }
            }
        }

        private void AnimateLayout(float deltaTime)
        {
            float t = Mathf.Clamp01(deltaTime * transitionSpeed);

            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null) continue;

                GetTargetTransform(i, out Vector3 targetPos, out Quaternion targetRot, out Vector3 targetScale, out float targetAlpha);

                card.anchoredPosition3D = Vector3.Lerp(card.anchoredPosition3D, targetPos, t);
                card.localRotation = Quaternion.Slerp(card.localRotation, targetRot, t);
                card.localScale = Vector3.Lerp(card.localScale, targetScale, t);

                var cg = card.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = Mathf.Lerp(cg.alpha, targetAlpha, t);
                    bool isCenter = (i == selectedIndex);
                    var playBtn = card.Find("PlayScenarioButton")?.GetComponent<Button>();
                    if (playBtn != null)
                    {
                        playBtn.interactable = isCenter;
                    }
                }
            }
        }

        private void GetTargetTransform(int index, out Vector3 pos, out Quaternion rot, out Vector3 scale, out float alpha)
        {
            int offset = index - selectedIndex;

            if (offset == 0)
            {
                pos = new Vector3(0f, cardCenterY, centerZ);
                rot = Quaternion.identity;
                scale = Vector3.one * centerScale;
                alpha = centerAlpha;
            }
            else
            {
                float sign = Mathf.Sign(offset);
                float absOffset = Mathf.Abs(offset);

                float posX = sign * (horizontalSpacing + (absOffset - 1) * (horizontalSpacing * 0.9f));
                float posZ = sideZ + (absOffset - 1) * 40f;
                // Left card (sign < 0) angles right (+Y), Right card (sign > 0) angles left (-Y)
                float rotY = -sign * sideRotationY;

                pos = new Vector3(posX, cardCenterY, posZ);
                rot = Quaternion.Euler(0f, rotY, 0f);
                scale = Vector3.one * Mathf.Max(0.5f, sideScale - (absOffset - 1) * 0.1f);
                alpha = Mathf.Max(0.2f, sideAlpha - (absOffset - 1) * 0.15f);
            }
        }
    }
}
