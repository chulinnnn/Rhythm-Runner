using UnityEngine;
using UnityEngine.UI;

// Legacy guided-animal presenter kept for Ocean hierarchy compatibility.
// 为兼容 Ocean 层级保留的旧引导动物展示组件。
//
// It swaps runtime animal sprites/animation frames but should not be used to restyle the main scene UI.
// 它只切换运行时动物图和动画帧，不应用来重设主场景 UI 样式。

public class OceanAnimalController : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image animalImage;
    private OceanSpriteAnimator spriteAnimator;
    private Text fallbackLabel;
    private Vector2 basePosition;
    private float bounceScale = 1f;
    private bool captured;

    public void Build(Sprite fallbackSprite, Font font)
    {
        rectTransform = GetComponent<RectTransform>();
        basePosition = rectTransform.anchoredPosition;

        animalImage = gameObject.AddComponent<Image>();
        animalImage.sprite = fallbackSprite;
        animalImage.color = new Color(1f, 0.72f, 0.2f);
        animalImage.preserveAspect = true;
        spriteAnimator = gameObject.AddComponent<OceanSpriteAnimator>();
        spriteAnimator.enabled = false;

        GameObject labelObject = new GameObject("AnimalLabel", typeof(RectTransform));
        labelObject.transform.SetParent(transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(220f, 90f);

        fallbackLabel = labelObject.AddComponent<Text>();
        fallbackLabel.font = font;
        fallbackLabel.fontSize = 30;
        fallbackLabel.fontStyle = FontStyle.Bold;
        fallbackLabel.color = new Color(0.02f, 0.16f, 0.24f);
        fallbackLabel.alignment = TextAnchor.MiddleCenter;
        fallbackLabel.resizeTextForBestFit = true;
        fallbackLabel.resizeTextMinSize = 16;
        fallbackLabel.resizeTextMaxSize = 30;
    }

    public void SetAnimal(string displayName, Sprite sprite, Color fallbackColor)
    {
        SetAnimal(displayName, sprite, null, fallbackColor);
    }

    public void SetAnimal(string displayName, Sprite sprite, Sprite[] animationFrames, Color fallbackColor)
    {
        captured = false;
        bounceScale = 1f;
        rectTransform.localScale = Vector3.one;
        rectTransform.anchoredPosition = basePosition;
        bool hasVisual = sprite != null || HasAnimationFrames(animationFrames);
        animalImage.color = hasVisual ? Color.white : fallbackColor;
        if (HasAnimationFrames(animationFrames))
        {
            spriteAnimator.SetFrames(animationFrames, 6f);
            fallbackLabel.text = "";
        }
        else if (sprite != null)
        {
            spriteAnimator.StopOn(sprite);
            fallbackLabel.text = "";
        }
        else
        {
            spriteAnimator.StopOn(animalImage.sprite);
            fallbackLabel.text = displayName;
        }
    }

    private bool HasAnimationFrames(Sprite[] animationFrames)
    {
        if (animationFrames == null)
        {
            return false;
        }

        for (int i = 0; i < animationFrames.Length; i++)
        {
            if (animationFrames[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    public void Bounce(float accentedScale)
    {
        bounceScale = Mathf.Max(bounceScale, accentedScale);
    }

    public void Capture()
    {
        captured = true;
        bounceScale = 1.28f;
    }

    private void Update()
    {
        if (rectTransform == null)
        {
            return;
        }

        float bob = Mathf.Sin(Time.time * 2.4f) * 10f;
        Vector2 targetPosition = basePosition + new Vector2(0f, captured ? 48f + bob : bob);
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, Time.deltaTime * 5f);

        bounceScale = Mathf.Lerp(bounceScale, captured ? 1.2f : 1f, Time.deltaTime * 7f);
        rectTransform.localScale = Vector3.one * bounceScale;

        Color color = animalImage.color;
        color.a = captured ? Mathf.Lerp(color.a, 0.62f, Time.deltaTime * 3f) : Mathf.Lerp(color.a, 1f, Time.deltaTime * 5f);
        animalImage.color = color;
    }
}
