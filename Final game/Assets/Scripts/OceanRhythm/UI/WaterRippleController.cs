using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Decorative pointer ripple layer for OceanRhythm.
// OceanRhythm 的鼠标/指针水波装饰层。
//
// It creates non-raycast runtime ripples only, so it should never block buttons or overwrite authored UI.
// 它只生成不接收射线的运行时水波，因此不会挡按钮，也不覆盖已编辑 UI。

public class WaterRippleController : MonoBehaviour
{
    public float spawnInterval = 0.045f;
    public float minMoveDistance = 16f;
    public float rippleDuration = 0.55f;
    public Vector2 startSize = new Vector2(26f, 26f);
    public Vector2 endSize = new Vector2(150f, 150f);

    private RectTransform rectTransform;
    private Sprite rippleSprite;
    private Vector2 lastMousePosition;
    private float lastSpawnTime;

    public void Initialize(Sprite sprite)
    {
        rippleSprite = sprite;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        lastMousePosition = Input.mousePosition;
    }

    private void Update()
    {
        Vector2 mousePosition = Input.mousePosition;
        if (Time.unscaledTime - lastSpawnTime < spawnInterval)
        {
            return;
        }

        if (Vector2.Distance(mousePosition, lastMousePosition) < minMoveDistance)
        {
            return;
        }

        lastMousePosition = mousePosition;
        lastSpawnTime = Time.unscaledTime;
        SpawnRipple(mousePosition);
    }

    private void SpawnRipple(Vector2 screenPosition)
    {
        if (rectTransform == null)
        {
            return;
        }

        Vector2 localPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, null, out localPosition);

        GameObject obj = new GameObject("MouseRipple", typeof(RectTransform));
        obj.transform.SetParent(transform, false);
        RectTransform rippleRect = obj.GetComponent<RectTransform>();
        rippleRect.anchorMin = new Vector2(0.5f, 0.5f);
        rippleRect.anchorMax = new Vector2(0.5f, 0.5f);
        rippleRect.anchoredPosition = localPosition;
        rippleRect.sizeDelta = startSize;

        Image image = obj.AddComponent<Image>();
        image.sprite = rippleSprite;
        image.color = new Color(1f, 1f, 1f, 0.38f);
        image.raycastTarget = false;
        image.preserveAspect = true;

        StartCoroutine(AnimateRipple(rippleRect, image));
    }

    private IEnumerator AnimateRipple(RectTransform rippleRect, Image image)
    {
        float timer = 0f;
        while (timer < rippleDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / rippleDuration);
            rippleRect.sizeDelta = Vector2.Lerp(startSize, endSize, t);
            Color color = image.color;
            color.a = Mathf.Lerp(0.38f, 0f, t);
            image.color = color;
            yield return null;
        }

        if (rippleRect != null)
        {
            Destroy(rippleRect.gameObject);
        }
    }
}
