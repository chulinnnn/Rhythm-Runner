using UnityEngine;
using UnityEngine.UI;

// Runtime cursor/net feedback for Free Pond capture progress.
// Free Pond 中用于显示捕捉进度的运行时网兜光标。
//
// This object is generated/controlled by UI code; it does not own static scene layout.
// 该对象由 UI 代码生成和控制，不负责静态场景布局。

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
        image = gameObject.GetComponent<Image>();
        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
        }
        image.sprite = netSprite != null ? netSprite : fallbackSprite;
        image.color = new Color(1f, 1f, 1f, 0.96f);
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

        float size = Mathf.Lerp(300f, 350f, captureRatio);
        rectTransform.sizeDelta = Vector2.one * size;
        pulseScale = Mathf.Lerp(pulseScale, 1f, Time.deltaTime * 9f);
        rectTransform.localScale = Vector3.one * pulseScale;

        if (image != null)
        {
            image.color = Color.Lerp(new Color(1f, 1f, 1f, 0.92f), new Color(1f, 0.86f, 0.18f, 1f), captureRatio);
        }
    }
}
