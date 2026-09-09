using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRSetup
{
    /// <summary>
    /// Manages visibility and smooth toggling between the Jakkro Card (and its real-time floor reflection)
    /// and the Scenario Select Card (scenario panel).
    /// </summary>
    [DisallowMultipleComponent]
    public class VRCardNavigationManager : MonoBehaviour
    {
        [Header("Card Panels")]
        [Tooltip("The main Jakkro Card GameObject.")]
        public GameObject jakkroCard;

        [Tooltip("The mirrored real-time floor reflection GameObject.")]
        public GameObject jakkroCardReflection;

        [Tooltip("The Scenario Selection Card GameObject.")]
        public GameObject scenarioCard;

        [Header("Trigger Buttons")]
        [Tooltip("Play / Menu button on the Jakkro card.")]
        public Button menuButton;

        [Tooltip("Back button on the Scenario selection card.")]
        public Button backButton;

        private void Awake()
        {
            AutoAssignReferencesIfMissing();
        }

        private void Start()
        {
            SetupButtonListeners();
            ShowJakkroCard();
        }

        public void AutoAssignReferencesIfMissing()
        {
            // Find Jakkro Card, Reflection, and Scenario Card among children
            foreach (Transform child in transform)
            {
                if (child.name == "JakkroCard_Reflection")
                {
                    jakkroCardReflection = child.gameObject;
                }
                else if (child.name == "ScenarioSelectionScreen")
                {
                    scenarioCard = child.gameObject;
                }
                else if (child.name == "JakkroCard")
                {
                    jakkroCard = child.gameObject;
                }
                else
                {
                    var texts = child.GetComponentsInChildren<TMP_Text>(true);
                    foreach (var t in texts)
                    {
                        string txt = t.text.ToUpper();
                        if (txt.Contains("JAKKHO") || txt.Contains("JAKKRO"))
                        {
                            jakkroCard = child.gameObject;
                            break;
                        }
                        else if (txt.Contains("SCAM TRAINING") || txt.Contains("SENARIO") || txt.Contains("SCENARIO"))
                        {
                            scenarioCard = child.gameObject;
                            break;
                        }
                    }
                }
            }

            // Find Play / Menu button on Jakkro Card
            if (jakkroCard != null)
            {
                foreach (var b in jakkroCard.GetComponentsInChildren<Button>(true))
                {
                    var t = b.GetComponentInChildren<TMP_Text>(true);
                    if (t != null)
                    {
                        string txt = t.text.ToUpper().Trim();
                        if (txt.Contains("PLAY") || txt.Contains("MANU") || txt.Contains("MENU") || txt.Contains("START"))
                        {
                            menuButton = b;
                            break;
                        }
                    }
                }
            }

            // Find Back button on Scenario Card
            if (scenarioCard != null)
            {
                foreach (var b in scenarioCard.GetComponentsInChildren<Button>(true))
                {
                    var t = b.GetComponentInChildren<TMP_Text>(true);
                    if (t != null)
                    {
                        string txt = t.text.ToUpper().Trim();
                        if (txt.Contains("BACK") || txt.Contains("RETURN") || txt.Contains("HOME"))
                        {
                            backButton = b;
                            break;
                        }
                    }
                }
            }
        }

        public void SetupButtonListeners()
        {
            if (menuButton != null)
            {
                menuButton.onClick.RemoveListener(ShowScenarioCard);
                menuButton.onClick.AddListener(ShowScenarioCard);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ShowJakkroCard);
                backButton.onClick.AddListener(ShowJakkroCard);
            }
        }

        /// <summary>
        /// Shows the Jakkro Card and its floor reflection, and hides the Scenario Card.
        /// </summary>
        [ContextMenu("Show Jakkro Card")]
        public void ShowJakkroCard()
        {
            if (jakkroCard != null) jakkroCard.SetActive(true);
            if (jakkroCardReflection != null) jakkroCardReflection.SetActive(true);
            if (scenarioCard != null) scenarioCard.SetActive(false);

            Debug.Log("[VRCardNavigationManager] Active View: JAKKRO CARD with Real-Time Floor Reflection.");
        }

        /// <summary>
        /// Shows the Scenario Card and hides the Jakkro Card & reflection.
        /// </summary>
        [ContextMenu("Show Scenario Card")]
        public void ShowScenarioCard()
        {
            if (jakkroCard != null) jakkroCard.SetActive(false);
            if (jakkroCardReflection != null) jakkroCardReflection.SetActive(false);
            if (scenarioCard != null) scenarioCard.SetActive(true);

            Debug.Log("[VRCardNavigationManager] Active View: SCENARIO SELECTION SCREEN.");
        }
    }
}
