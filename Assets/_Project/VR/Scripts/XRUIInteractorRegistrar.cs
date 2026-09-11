using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VRSetup
{
    /// <summary>
    /// Safety net for a known XRI edge case: when a ray interactor is disabled and
    /// re-enabled (for example when the input modality toggles controller objects),
    /// its registration with the XRUIInputModule can get stuck inactive, which makes
    /// the laser render but never hover or click UI. This re-registers every UI
    /// interactor under this rig shortly after startup so beams always work.
    /// </summary>
    [DisallowMultipleComponent]
    public class XRUIInteractorRegistrar : MonoBehaviour
    {
        [SerializeField] private float[] reRegisterTimes = { 0.25f, 1f, 2.5f, 5f };

        private void Start()
        {
            StartCoroutine(ReRegisterRoutine());
        }

        private IEnumerator ReRegisterRoutine()
        {
            for (int i = 0; i < reRegisterTimes.Length; i++)
            {
                yield return new WaitForSeconds(reRegisterTimes[i]);
                ReRegisterNow();
            }
        }

        public void ReRegisterNow()
        {
            var module = Object.FindFirstObjectByType<XRUIInputModule>();
            if (module == null) return;

            var behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IUIInteractor uiInteractor)
                    module.RegisterInteractor(uiInteractor);
            }
        }
    }
}
