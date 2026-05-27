using UnityEngine;
using UnityEngine.UI;

public class OceanNetCursor : MonoBehaviour
{
    private RectTransform rectTransform;
    private RectTransform parentRect;
    private Image image;
    private float captureRatio;
    private float pulseScale = 1f;

    public void Build(Sprite fallbackSprite, Sprite netSprite)
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = transform.parent as RectTransform;
        image = gameObject.AddComponent<Image>();
        image.sprite = netSprite != null ? netSprite : fallbackSprite;
        image.color = new Color(1f, 1f, 1f, 0.58f);
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    public void SetCaptureRatio(float ratio)
    {
        captureRatio = Mathf.Clamp01(ratio);
    }

    public void Pulse(OceanRhythmHitResult result)
    {
        if (result == OceanRhythmHitResult.Perfect)
        {
            pulseScale = 1.16f;
        }
        else if (result == OceanRhythmHitResult.Good)
        {
            pulseScale = 1.08f;
        }
        else if (result == OceanRhythmHitResult.Miss)
        {
            pulseScale = 0.94f;
        }
    }

    private void Update()
    {
        if (rectTransform == null || parentRect == null)
        {
            return;
        }

        Vector2 localMouse;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, null, out localMouse);
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, localMouse, Time.deltaTime * 14f);

        float size = Mathf.Lerp(190f, 112f, captureRatio);
        rectTransform.sizeDelta = Vector2.one * size;
        pulseScale = Mathf.Lerp(pulseScale, 1f, Time.deltaTime * 9f);
        rectTransform.localScale = Vector3.one * pulseScale;

        if (image != null)
        {
            image.color = Color.Lerp(new Color(1f, 1f, 1f, 0.48f), new Color(1f, 0.86f, 0.18f, 0.72f), captureRatio);
        }
    }
}
