#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Kannko.Laptop;

namespace Kannko.Scenario2.Editor
{
    public class TestWifiConnectionFlow
    {
        private const string SCENE_PATH = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";
        private CaptivePortalController m_Controller;
        private GameObject m_BrowserGo;
        private GameObject m_WorldUIGo;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            if (EditorSceneManager.GetActiveScene().path != SCENE_PATH)
            {
                EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            m_WorldUIGo = GameObject.Find("WorldUI");
            Assert.IsNotNull(m_WorldUIGo, "WorldUI GameObject must exist in Scenario2 scene.");

            var browserTrans = m_WorldUIGo.transform.Find("FloatingLaptopPanel/ScreenContainer/LaptopBrowserPanel");
            Assert.IsNotNull(browserTrans, "LaptopBrowserPanel must exist under FloatingLaptopPanel.");
            m_BrowserGo = browserTrans.gameObject;

            m_Controller = m_BrowserGo.GetComponent<CaptivePortalController>();
            Assert.IsNotNull(m_Controller, "CaptivePortalController must exist on LaptopBrowserPanel.");

            m_Controller.FindReferencesIfNull();
            yield return null;
        }

        [Test]
        public void Test01_ConnectButtonIntegrityAndXRComponents()
        {
            var pageContent = m_BrowserGo.transform.Find("PageContent");
            Assert.IsNotNull(pageContent, "PageContent must exist under LaptopBrowserPanel.");

            var captivePanel = pageContent.Find("CaptivePortalPanel");
            Assert.IsNotNull(captivePanel, "CaptivePortalPanel must exist under PageContent.");

            var connectBtn = captivePanel.Find("ConnectButton");
            Assert.IsNotNull(connectBtn, "ConnectButton must exist under CaptivePortalPanel.");

            var btn = connectBtn.GetComponent<Button>();
            Assert.IsNotNull(btn, "ConnectButton must have a UI Button component.");

            var col = connectBtn.GetComponent<BoxCollider>();
            Assert.IsNotNull(col, "ConnectButton must have a 3D BoxCollider for VR raycast interaction.");
            Assert.IsTrue(col.enabled, "BoxCollider on ConnectButton must be enabled.");

            var xr = connectBtn.GetComponent<XRSimpleInteractable>();
            Assert.IsNotNull(xr, "ConnectButton must have an XRSimpleInteractable component for VR controllers.");
        }

        [Test]
        public void Test02_WifiConnectedSuccessPanelExistsAndConfigured()
        {
            var pageContent = m_BrowserGo.transform.Find("PageContent");
            var successPanel = pageContent.Find("WifiConnectedSuccessPanel");
            Assert.IsNotNull(successPanel, "WifiConnectedSuccessPanel must exist under PageContent.");

            Assert.IsNotNull(m_Controller.WifiConnectedSuccessPanel, "CaptivePortalController.WifiConnectedSuccessPanel reference must be assigned.");
            Assert.AreEqual(successPanel.gameObject, m_Controller.WifiConnectedSuccessPanel);

            // Check key UI content
            var title = successPanel.Find("TitleText");
            Assert.IsNotNull(title, "TitleText must exist in WifiConnectedSuccessPanel.");
            var titleTmp = title.GetComponent<TextMeshProUGUI>();
            Assert.IsTrue(titleTmp.text.Contains("Wi-Fi Connected"), "TitleText must display 'Wi-Fi Connected'.");

            var badge = successPanel.Find("SuccessBadge/Label");
            Assert.IsNotNull(badge, "SuccessBadge/Label must exist in WifiConnectedSuccessPanel.");
            var badgeTmp = badge.GetComponent<TextMeshProUGUI>();
            Assert.IsTrue(badgeTmp.text.Contains("Connected successfully"), "SuccessBadge must contain 'Connected successfully'.");

            var subtitle = successPanel.Find("SubtitleText");
            Assert.IsNotNull(subtitle, "SubtitleText must exist in WifiConnectedSuccessPanel.");
            var subTmp = subtitle.GetComponent<TextMeshProUGUI>();
            Assert.IsTrue(subTmp.text.Contains("connected to Wi-Fi"), "SubtitleText must explain device is connected to Wi-Fi.");

            var details = successPanel.Find("DetailsCard/InfoText");
            Assert.IsNotNull(details, "DetailsCard/InfoText must exist in WifiConnectedSuccessPanel.");
        }

        [Test]
        public void Test03_ShowWifiConnectedSuccessTransitionsCleanly()
        {
            m_Controller.ShowCaptivePortal("Google");
            Assert.IsTrue(m_Controller.transform.Find("PageContent/CaptivePortalPanel").gameObject.activeSelf);
            Assert.IsFalse(m_Controller.WifiConnectedSuccessPanel.activeSelf);

            m_Controller.ShowWifiConnectedSuccess();

            Assert.IsFalse(m_Controller.transform.Find("PageContent/CaptivePortalPanel").gameObject.activeSelf, "CaptivePortalPanel should be inactive on success.");
            Assert.IsTrue(m_Controller.WifiConnectedSuccessPanel.activeSelf, "WifiConnectedSuccessPanel should be active.");

            var url = m_BrowserGo.transform.Find("BrowserHeader/NavBar/AddressBar/UrlText").GetComponent<TextMeshProUGUI>();
            Assert.AreEqual("https://wifi.jakkocoffee.com/connected", url.text, "Browser URL bar should reflect connected state.");

            var tab = m_BrowserGo.transform.Find("BrowserHeader/TabBar/ActiveTab/TabTitle").GetComponent<TextMeshProUGUI>();
            Assert.AreEqual("Wi-Fi Connected", tab.text, "Browser Tab Title should reflect connected state.");

            // Reset
            m_Controller.ResetToInitialPortal();
            Assert.IsFalse(m_Controller.WifiConnectedSuccessPanel.activeSelf, "WifiConnectedSuccessPanel should be inactive after reset.");
        }

        [Test]
        public void Test04_ShowAttackRevealPreservesExistingWorldSpaceMechanism()
        {
            m_Controller.ShowWifiConnectedSuccess();
            Assert.IsTrue(m_Controller.WifiConnectedSuccessPanel.activeSelf);

            m_Controller.ShowAttackReveal();

            Assert.IsFalse(m_Controller.WifiConnectedSuccessPanel.activeSelf, "WifiConnectedSuccessPanel must be deactivated when Reveal warning activates.");

            var floatingLaptop = m_WorldUIGo.transform.Find("FloatingLaptopPanel");
            Assert.IsFalse(floatingLaptop.gameObject.activeSelf, "FloatingLaptopPanel must be hidden during Reveal presentation.");

            var revealSpace = GameObject.Find("Scenario2RevealWorldSpace") ?? m_WorldUIGo.transform.Find("Scenario2RevealWorldSpace")?.gameObject;
            Assert.IsNotNull(revealSpace, "Scenario2RevealWorldSpace must exist.");
            Assert.IsTrue(revealSpace.activeSelf, "Scenario2RevealWorldSpace must be active during Reveal presentation.");

            // Reset
            m_Controller.ResetToInitialPortal();
            floatingLaptop.gameObject.SetActive(true);
        }

        [Test]
        public void Test05_VRRaycastDispatchHitTriggersConnect()
        {
            m_Controller.ShowCaptivePortal("Google");
            var connectBtn = m_BrowserGo.transform.Find("PageContent/CaptivePortalPanel/ConnectButton").gameObject;

            bool handled = m_Controller.DispatchHit(connectBtn);
            Assert.IsTrue(handled, "DispatchHit on ConnectButton must return true.");

            // Reset
            m_Controller.ResetToInitialPortal();
        }

        [Test]
        public void Test06_AttackReveal_DoesNotContainWarningSymbolAndMatchesEditMode()
        {
            m_Controller.ShowAttackReveal();

            var warningTitleTrans = GameObject.Find("WarningTitle")?.GetComponent<TextMeshProUGUI>();
            if (warningTitleTrans != null)
            {
                Assert.IsFalse(warningTitleTrans.text.Contains("⚠"), "WarningTitle text must not contain Unicode warning symbol '⚠'.");
                Assert.IsFalse(warningTitleTrans.text.Contains("\u26A0"), "WarningTitle text must not contain Unicode character \\u26A0.");
                Assert.IsTrue(warningTitleTrans.text.Contains("WARNING"), "WarningTitle text should contain 'WARNING'.");
            }

            var tabTitleTrans = m_BrowserGo.transform.Find("BrowserHeader/TabBar/ActiveTab/TabTitle")?.GetComponent<TextMeshProUGUI>();
            if (tabTitleTrans != null)
            {
                Assert.IsFalse(tabTitleTrans.text.Contains("⚠"), "TabTitle text must not contain Unicode warning symbol '⚠'.");
                Assert.IsFalse(tabTitleTrans.text.Contains("\u26A0"), "TabTitle text must not contain Unicode character \\u26A0.");
            }

            m_Controller.ResetToInitialPortal();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (m_Controller != null)
            {
                m_Controller.ResetToInitialPortal();
            }

            var floatingLaptop = m_WorldUIGo?.transform.Find("FloatingLaptopPanel");
            if (floatingLaptop != null)
            {
                floatingLaptop.gameObject.SetActive(true);
            }

            var revealSpace = GameObject.Find("Scenario2RevealWorldSpace") ?? m_WorldUIGo?.transform.Find("Scenario2RevealWorldSpace")?.gameObject;
            if (revealSpace != null)
            {
                revealSpace.SetActive(false);
            }

            yield return null;
        }
    }
}
#endif
