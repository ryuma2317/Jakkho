#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Kannko.Laptop;

namespace Kannko.Scenario2.Editor
{
    public class TestExitAndDecisionFlow
    {
        private const string SCENE_PATH = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";
        private GameObject m_WorldUIGo;
        private GameObject m_FloatingLaptopGo;
        private GameObject m_BrowserGo;
        private JakkoFreeWifiBrowserController m_BrowserCtrl;
        private GameObject m_DecisionGo;
        private StopPlayingDecisionController m_DecisionCtrl;
        private GameObject m_ExitGo;
        private Scenario2ExitController m_ExitCtrl;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            if (EditorSceneManager.GetActiveScene().path != SCENE_PATH)
            {
                EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            m_WorldUIGo = GameObject.Find("WorldUI");
            Assert.IsNotNull(m_WorldUIGo, "WorldUI must exist in Scenario2 scene.");

            var laptopTrans = m_WorldUIGo.transform.Find("FloatingLaptopPanel");
            Assert.IsNotNull(laptopTrans, "FloatingLaptopPanel must exist under WorldUI.");
            m_FloatingLaptopGo = laptopTrans.gameObject;

            var browserTrans = laptopTrans.Find("ScreenContainer/LaptopBrowserPanel");
            Assert.IsNotNull(browserTrans, "LaptopBrowserPanel must exist under ScreenContainer.");
            m_BrowserGo = browserTrans.gameObject;
            m_BrowserCtrl = m_BrowserGo.GetComponent<JakkoFreeWifiBrowserController>();
            Assert.IsNotNull(m_BrowserCtrl, "JakkoFreeWifiBrowserController must exist on LaptopBrowserPanel.");

            var decisionTrans = m_WorldUIGo.transform.Find("StopPlayingDecisionPanel");
            Assert.IsNotNull(decisionTrans, "StopPlayingDecisionPanel must exist directly under WorldUI.");
            m_DecisionGo = decisionTrans.gameObject;
            m_DecisionCtrl = m_DecisionGo.GetComponent<StopPlayingDecisionController>();
            Assert.IsNotNull(m_DecisionCtrl, "StopPlayingDecisionController must exist on StopPlayingDecisionPanel.");

            var exitTrans = m_WorldUIGo.transform.Find("Scenario2ExitConfirmationPanel");
            Assert.IsNotNull(exitTrans, "Scenario2ExitConfirmationPanel must exist under WorldUI.");
            m_ExitGo = exitTrans.gameObject;
            m_ExitCtrl = m_ExitGo.GetComponent<Scenario2ExitController>();
            Assert.IsNotNull(m_ExitCtrl, "Scenario2ExitController must exist on Scenario2ExitConfirmationPanel.");

            m_BrowserCtrl.FindReferencesIfNull();
            m_DecisionCtrl.FindReferencesIfNull();
            m_ExitCtrl.FindReferencesIfNull();

            yield return null;
        }

        [Test]
        public void Test01_StopPlayingDecisionPanel_IsDirectChildOfWorldUI_NotLaptop()
        {
            Assert.AreEqual("WorldUI", m_DecisionGo.transform.parent.name,
                "StopPlayingDecisionPanel must be a direct child of WorldUI.");

            Assert.IsFalse(m_DecisionGo.transform.IsChildOf(m_FloatingLaptopGo.transform),
                "StopPlayingDecisionPanel must NOT be a child of FloatingLaptopPanel.");
        }

        [Test]
        public void Test02_StopPlayingDecisionPanel_HasWorldSpaceCanvasAndXRRaycaster()
        {
            var canvas = m_DecisionGo.GetComponent<Canvas>();
            Assert.IsNotNull(canvas, "StopPlayingDecisionPanel must have a Canvas component.");
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode, "Canvas render mode must be WorldSpace.");
            Assert.AreEqual(24, canvas.sortingOrder, "Canvas sorting order should be 24.");

            var scaler = m_DecisionGo.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler, "StopPlayingDecisionPanel must have a CanvasScaler.");
            Assert.AreEqual(10f, scaler.dynamicPixelsPerUnit, "dynamicPixelsPerUnit must be 10.");

            var gr = m_DecisionGo.GetComponent<GraphicRaycaster>();
            Assert.IsNotNull(gr, "StopPlayingDecisionPanel must have a GraphicRaycaster.");

            var tr = m_DecisionGo.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
            Assert.IsNotNull(tr, "StopPlayingDecisionPanel must have a TrackedDeviceGraphicRaycaster for VR rays.");
        }

        [Test]
        public void Test03_StopPlayingDecisionPanel_TransformMatchesExitPanel()
        {
            var exitRt = m_ExitGo.GetComponent<RectTransform>();
            var decisionRt = m_DecisionGo.GetComponent<RectTransform>();

            // Both should be placed at the eye-level position in front of camera
            Assert.AreEqual(exitRt.position.x, decisionRt.position.x, 0.05f, "X position should match ExitPanel.");
            Assert.AreEqual(exitRt.position.y, decisionRt.position.y, 0.05f, "Y position should match ExitPanel.");
            Assert.AreEqual(exitRt.position.z, decisionRt.position.z, 0.05f, "Z position should match ExitPanel.");

            // Both should face 270 degrees towards player
            Assert.AreEqual(270f, decisionRt.eulerAngles.y, 1f, "Rotation should face 270 degrees towards player.");

            // Scale should match
            Assert.AreEqual(0.0008f, decisionRt.localScale.x, 0.0001f, "Scale X must match 0.0008.");
            Assert.AreEqual(0.0008f, decisionRt.localScale.y, 0.0001f, "Scale Y must match 0.0008.");
            Assert.AreEqual(0.0008f, decisionRt.localScale.z, 0.0001f, "Scale Z must match 0.0008.");
        }

        [Test]
        public void Test04_ButtonsHave3DCollidersAndXRInteractables()
        {
            var continueBtn = m_DecisionGo.transform.Find("ActionButtons/ContinueScenarioButton");
            Assert.IsNotNull(continueBtn, "ContinueScenarioButton must exist.");
            var colCont = continueBtn.GetComponent<BoxCollider>();
            Assert.IsNotNull(colCont, "ContinueScenarioButton must have a BoxCollider.");
            Assert.IsTrue(colCont.enabled, "ContinueScenarioButton BoxCollider must be enabled.");
            var xrCont = continueBtn.GetComponent<XRSimpleInteractable>();
            Assert.IsNotNull(xrCont, "ContinueScenarioButton must have an XRSimpleInteractable.");

            var getOutBtn = m_DecisionGo.transform.Find("ActionButtons/GetOutOfCafeButton");
            Assert.IsNotNull(getOutBtn, "GetOutOfCafeButton must exist.");
            var colGetOut = getOutBtn.GetComponent<BoxCollider>();
            Assert.IsNotNull(colGetOut, "GetOutOfCafeButton must have a BoxCollider.");
            Assert.IsTrue(colGetOut.enabled, "GetOutOfCafeButton BoxCollider must be enabled.");
            var xrGetOut = getOutBtn.GetComponent<XRSimpleInteractable>();
            Assert.IsNotNull(xrGetOut, "GetOutOfCafeButton must have an XRSimpleInteractable.");
        }

        [Test]
        public void Test05_BrowserBackClick_ShowsDecisionPanel_HidesLaptop()
        {
            m_BrowserCtrl.OpenBrowser();
            Assert.IsTrue(m_BrowserGo.activeSelf, "Browser must be open.");

            m_BrowserCtrl.OnBackButtonClicked();

            Assert.IsFalse(m_BrowserGo.activeSelf, "Browser must be closed after back click.");
            Assert.IsFalse(m_FloatingLaptopGo.activeSelf, "FloatingLaptopPanel must be hidden while decision is shown.");
            Assert.IsTrue(m_DecisionGo.activeSelf, "StopPlayingDecisionPanel must be active.");
        }

        [Test]
        public void Test06_GetOutOfCafeClick_TransitionsToExitConfirmationPanel()
        {
            // Show decision panel
            m_DecisionCtrl.ShowDecision();
            Assert.IsTrue(m_DecisionGo.activeSelf, "Decision panel should be active.");
            Assert.IsFalse(m_ExitGo.activeSelf, "Exit confirmation should be inactive initially.");

            // Click Get Out of This Cafe
            m_DecisionCtrl.OnGetOutOfCafeClicked();

            Assert.IsFalse(m_DecisionGo.activeSelf, "Decision panel should be hidden after GetOutOfCafe click.");
            Assert.IsTrue(m_ExitGo.activeSelf, "Scenario2ExitConfirmationPanel must be shown.");
            Assert.IsFalse(m_FloatingLaptopGo.activeSelf, "FloatingLaptopPanel must remain hidden during exit confirmation.");
        }

        [Test]
        public void Test07_ContinueScenarioAndBackToScenario_RestoresLaptop()
        {
            // Flow A: Continue from Decision Panel
            m_DecisionCtrl.ShowDecision();
            Assert.IsTrue(m_DecisionGo.activeSelf);
            Assert.IsFalse(m_FloatingLaptopGo.activeSelf);

            m_DecisionCtrl.OnContinueScenarioClicked();
            Assert.IsFalse(m_DecisionGo.activeSelf, "Decision panel should hide on continue.");
            Assert.IsTrue(m_FloatingLaptopGo.activeSelf, "FloatingLaptopPanel should be restored on continue.");

            var wifiPanel = m_FloatingLaptopGo.transform.Find("ScreenContainer/EnlargedWifiPanel");
            Assert.IsTrue(wifiPanel.gameObject.activeSelf, "EnlargedWifiPanel should be visible upon returning.");

            // Flow B: Back from Exit Confirmation
            m_ExitCtrl.ShowExitConfirmation();
            Assert.IsTrue(m_ExitGo.activeSelf);
            Assert.IsFalse(m_FloatingLaptopGo.activeSelf);

            m_ExitCtrl.OnBackToScenarioClicked();
            Assert.IsFalse(m_ExitGo.activeSelf, "Exit panel should hide on back.");
            Assert.IsTrue(m_FloatingLaptopGo.activeSelf, "FloatingLaptopPanel should be restored on back.");
            Assert.IsTrue(wifiPanel.gameObject.activeSelf, "EnlargedWifiPanel should be visible upon returning.");
        }

        [Test]
        public void Test08_VRRaycastDispatchHitOnDecisionButtons()
        {
            m_DecisionCtrl.ShowDecision();
            Assert.IsTrue(m_DecisionGo.activeSelf);

            var continueBtn = m_DecisionGo.transform.Find("ActionButtons/ContinueScenarioButton").gameObject;
            bool handled = m_DecisionCtrl.DispatchHit(continueBtn);
            Assert.IsTrue(handled, "DispatchHit on ContinueScenarioButton must handle click.");
            Assert.IsFalse(m_DecisionGo.activeSelf, "Continue should have closed decision panel.");

            m_DecisionCtrl.ShowDecision();
            var getOutBtn = m_DecisionGo.transform.Find("ActionButtons/GetOutOfCafeButton").gameObject;
            handled = m_DecisionCtrl.DispatchHit(getOutBtn);
            Assert.IsTrue(handled, "DispatchHit on GetOutOfCafeButton must handle click.");
            Assert.IsTrue(m_ExitGo.activeSelf, "GetOutOfCafe should have opened exit panel.");
        }

        [Test]
        public void Test09_BackButton_HasExpandedHitbox_AndNonBlockingContainers()
        {
            var backBtn = m_BrowserGo.transform.Find("BrowserHeader/NavBar/BackButton");
            Assert.IsNotNull(backBtn, "BackButton must exist.");

            var rt = backBtn.GetComponent<RectTransform>();
            Assert.GreaterOrEqual(rt.sizeDelta.x, 60f, "BackButton width must be at least 60px.");
            Assert.GreaterOrEqual(rt.sizeDelta.y, 44f, "BackButton height must be at least 44px.");

            var col = backBtn.GetComponent<BoxCollider>();
            Assert.IsNotNull(col, "BackButton must have a BoxCollider.");
            Assert.IsTrue(col.enabled, "BackButton BoxCollider must be enabled.");
            Assert.GreaterOrEqual(col.size.x, 70f, "BoxCollider width must be at least 70.");
            Assert.GreaterOrEqual(col.size.y, 50f, "BoxCollider height must be at least 50.");
            Assert.Less(col.center.z, 0f, "BoxCollider center.z must be negative to protrude in front of screen.");

            var navBar = m_BrowserGo.transform.Find("BrowserHeader/NavBar");
            var navImg = navBar.GetComponent<Image>();
            if (navImg != null)
            {
                Assert.IsFalse(navImg.raycastTarget, "NavBar must NOT have raycastTarget enabled.");
            }

            var header = m_BrowserGo.transform.Find("BrowserHeader");
            var headerImg = header.GetComponent<Image>();
            if (headerImg != null)
            {
                Assert.IsFalse(headerImg.raycastTarget, "BrowserHeader must NOT have raycastTarget enabled.");
            }
        }

        [Test]
        public void Test10_BackButton_GraphicRaycast_HitsBackButton_NotSwallowed()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            m_BrowserCtrl.OpenBrowser();
            Camera cam = Camera.main;
            cam.Render();

            var backBtn = m_BrowserGo.transform.Find("BrowserHeader/NavBar/BackButton");
            var rt = backBtn.GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 center = (corners[0] + corners[2]) * 0.5f;
            Vector3 screenCenter = cam.WorldToScreenPoint(center);

            var es = UnityEngine.EventSystems.EventSystem.current;
            GameObject tempES = null;
            if (es == null)
            {
                tempES = new GameObject("TempES");
                es = tempES.AddComponent<UnityEngine.EventSystems.EventSystem>();
            }

            var ped = new UnityEngine.EventSystems.PointerEventData(es);
            ped.position = new Vector2(screenCenter.x, screenCenter.y);

            var rc = m_BrowserGo.GetComponentInParent<GraphicRaycaster>();
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            rc.Raycast(ped, results);

            Button hitButton = null;
            for (int i = 0; i < results.Count; i++)
            {
                var b = results[i].gameObject.GetComponentInParent<Button>();
                if (b != null && b.interactable)
                {
                    hitButton = b;
                    break;
                }
            }

            if (tempES != null) Object.DestroyImmediate(tempES);

            Assert.IsNotNull(hitButton, "GraphicRaycast must hit an interactable button.");
            Assert.AreEqual("BackButton", hitButton.name, "Hit button must be BackButton.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (m_DecisionGo != null) m_DecisionGo.SetActive(false);
            if (m_ExitGo != null) m_ExitGo.SetActive(false);
            if (m_BrowserGo != null) m_BrowserGo.SetActive(false);
            if (m_FloatingLaptopGo != null)
            {
                m_FloatingLaptopGo.SetActive(true);
                var wifi = m_FloatingLaptopGo.transform.Find("ScreenContainer/EnlargedWifiPanel");
                if (wifi != null) wifi.gameObject.SetActive(true);
            }
            yield return null;
        }
    }
}
#endif
