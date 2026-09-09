using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    [SerializeField] private float hoverScaleMultiplier = 1.05f;
    [SerializeField] private float scaleSpeed = 15f;

    [Header("VR 3D Depth Settings")]
    [SerializeField] private float hoverZOffset = -20f; // Shift forward in 3D local space toward VR player

    private Vector3 defaultScale;
    private Vector3 targetScale;
    private float defaultZ;
    private float targetZ;

    private Coroutine scaleCoroutine;

    private void Awake()
    {
        defaultScale = transform.localScale;
        targetScale = defaultScale;
        defaultZ = transform.localPosition.z;
        targetZ = defaultZ;
    }

    private void OnDisable()
    {
        transform.localScale = defaultScale;
        Vector3 pos = transform.localPosition;
        pos.z = defaultZ;
        transform.localPosition = pos;

        targetScale = defaultScale;
        targetZ = defaultZ;
    }

    public void SetHoverScaleMultiplier(float multiplier, float zOffset = -20f)
    {
        hoverScaleMultiplier = multiplier;
        hoverZOffset = zOffset;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (defaultScale == Vector3.zero) defaultScale = transform.localScale;

        defaultZ = transform.localPosition.z;
        targetScale = defaultScale * hoverScaleMultiplier;
        targetZ = defaultZ + hoverZOffset;
        StartScaling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = defaultScale;
        targetZ = defaultZ;
        StartScaling();
    }

    private void StartScaling()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(AnimateScale());
    }

    private IEnumerator AnimateScale()
    {
        while (Vector3.Distance(transform.localScale, targetScale) > 0.001f || Mathf.Abs(transform.localPosition.z - targetZ) > 0.05f)
        {
            float dt = Time.unscaledDeltaTime * scaleSpeed;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, dt);

            Vector3 currentPos = transform.localPosition;
            currentPos.z = Mathf.Lerp(currentPos.z, targetZ, dt);
            transform.localPosition = currentPos;

            yield return null;
        }

        transform.localScale = targetScale;
        Vector3 finalPos = transform.localPosition;
        finalPos.z = targetZ;
        transform.localPosition = finalPos;
    }
}
