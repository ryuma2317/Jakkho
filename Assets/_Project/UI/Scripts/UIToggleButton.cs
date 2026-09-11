using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggles target GameObject active states when this button is clicked.
/// Used for floating tool panels (e.g. the browser Security Tools drawer).
/// </summary>
[RequireComponent(typeof(Button))]
public class UIToggleButton : MonoBehaviour
{
    [SerializeField] private GameObject[] targets;

    public GameObject[] Targets
    {
        get => targets;
        set => targets = value;
    }

    private void Awake()
    {
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(Toggle);
    }

    public void Toggle()
    {
        if (targets == null) return;
        foreach (var target in targets)
        {
            if (target != null)
                target.SetActive(!target.activeSelf);
        }
    }
}