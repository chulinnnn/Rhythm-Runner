using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Runtime fish/animal view used inside the Free Pond layer.
// Free Pond 层中的运行时鱼/动物视图。
//
// OceanRhythmUIController creates these instances from lesson data; scene-authored overlays and cards remain separate.
// OceanRhythmUIController 会根据 lesson 数据创建这些实例；场景编辑的弹窗和卡片与这里分离。

public class OceanPondAnimal : MonoBehaviour
{
    public OceanLesson Lesson { get; private set; }
    public OceanFishType FishType { get { return Lesson != null ? Lesson.fishType : OceanFishType.Fish; } }
    public bool IsMystery { get { return FishType == OceanFishType.Mystery; } }
    public string InstanceId { get; private set; }
    public int CaptureProgress { get; private set; }
    public int RequiredHits { get { return requiredHits; } }
    public bool IsCaptured { get; private set; }
    public float CaptureRatio { get { return requiredHits <= 0 ? 0f : Mathf.Clamp01((float)CaptureProgress / requiredHits); } }
    public Vector2 AnchoredPosition { get { return rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero; } }
    public string RemainingHitsText
    {
        get
        {
            int remaining = Mathf.Max(0, requiredHits - CaptureProgress);
            return remaining == 0 ? "Ready!" : remaining + " more";
        }
    }

    public int requiredHits = 3;

    private RectTransform rectTransform;
    private Image animalImage;
    private Text nameText;
    private Text meterText;
    private Text remainingText;
    private Sprite captureBubbleSprite;
    private readonly List<Image> captureBubbles = new List<Image>();
    private Vector2 basePosition;
    private Vector2 swimOffset;
    private float swimSeed;
    private bool isSelected;
    private bool isHovered;
    private bool flyingToBucket;
    private float pulseScale = 1f;
    private float shakeTimer;
    private float glowTimer;
    private float flyTimer;
    private Vector2 flyStartPosition;
    private Vector2 flyTargetPosition;

    public void Build(OceanLesson lesson, Sprite animalSprite, Sprite bubbleSprite, Font font, Color fallbackColor, Vector2 startPosition)
    {
        Build(lesson, animalSprite, bubbleSprite, font, fallbackColor, startPosition, lesson.animalKey);
    }

    public void Build(OceanLesson lesson, Sprite animalSprite, Sprite bubbleSprite, Font font, Color fallbackColor, Vector2 startPosition, string instanceId)
    {
        InstanceId = instanceId;
        captureBubbleSprite = bubbleSprite;
        rectTransform = GetComponent<RectTransform>();
        basePosition = startPosition;
        rectTransform.anchoredPosition = startPosition;
        swimSeed = Random.Range(0f, 100f);

        animalImage = gameObject.AddComponent<Image>();
        animalImage.sprite = animalSprite != null ? animalSprite : bubbleSprite;
        animalImage.color = animalSprite != null ? Color.white : fallbackColor;
        animalImage.preserveAspect = true;
        animalImage.raycastTarget = false;

        string displayName = IsMystery ? "?" : lesson.animalName;
        nameText = CreateText("Name", displayName, font, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(160f, 40f), 22, FontStyle.Bold, new Color(0.02f, 0.16f, 0.24f));
        nameText.raycastTarget = false;
        nameText.gameObject.SetActive(false);

        meterText = CreateText("Meter", IsMystery ? "?" : "Tap!", font, new Vector2(0.5f, 1f), new Vector2(0f, 36f), new Vector2(110f, 34f), 24, FontStyle.Bold, Color.white);
        meterText.raycastTarget = false;
        meterText.gameObject.SetActive(false);

        remainingText = CreateText("Remaining", "", font, new Vector2(0.5f, 0f), new Vector2(0f, -32f), new Vector2(130f, 30f), 20, FontStyle.Bold, Color.white);
        remainingText.raycastTarget = false;
        remainingText.gameObject.SetActive(false);

        SetLessonInternal(lesson, bubbleSprite);
        SetSelected(false);
    }

    public void AssignLesson(OceanLesson lesson)
    {
        SetLessonInternal(lesson, captureBubbleSprite);
        if (IsMystery && nameText != null)
        {
            nameText.text = "?";
        }
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (IsMystery && selected)
        {
            nameText.text = Lesson.animalName;
            meterText.text = "Tap!";
        }

        if (meterText != null)
        {
            meterText.gameObject.SetActive(false);
        }
        if (nameText != null)
        {
            nameText.gameObject.SetActive(false);
        }
        HideRemainingText();
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
        if (meterText != null)
        {
            meterText.gameObject.SetActive(false);
        }
    }

    public void AddCaptureProgress(OceanRhythmHitResult result)
    {
        if (IsCaptured)
        {
            return;
        }

        CaptureProgress = Mathf.Clamp(CaptureProgress + 1, 0, requiredHits);
        pulseScale = result == OceanRhythmHitResult.Perfect ? 1.28f : 1.18f;
        RefreshCaptureBubbles(result);

        if (CaptureProgress >= requiredHits)
        {
            IsCaptured = true;
        }
    }

    public void ShowRhythmHint(OceanRhythmHitResult result)
    {
        if (IsCaptured)
        {
            return;
        }

        shakeTimer = 0.28f;
        RefreshCaptureBubbles(result);
    }

    public void HighlightFromSoundMatch()
    {
        if (IsCaptured)
        {
            return;
        }

        glowTimer = 3.5f;
        pulseScale = 1.32f;
        SetHovered(true);
    }

    public void PlayRescue(Vector2 bucketPosition)
    {
        IsCaptured = true;
        flyingToBucket = true;
        flyTimer = 0f;
        transform.SetAsLastSibling();
        flyStartPosition = rectTransform != null ? rectTransform.anchoredPosition : AnchoredPosition;
        flyTargetPosition = bucketPosition;
        pulseScale = 1.45f;
        SetSelected(false);
    }

    public void ResetCatch()
    {
        CaptureProgress = 0;
        IsCaptured = false;
        flyingToBucket = false;
        flyTimer = 0f;
        shakeTimer = 0f;
        pulseScale = 1f;
        transform.SetAsLastSibling();
        rectTransform.anchoredPosition = basePosition;
        if (animalImage != null)
        {
            Color color = animalImage.color;
            color.a = 1f;
            animalImage.color = color;
        }

        RefreshCaptureBubbles(OceanRhythmHitResult.Near);
        if (remainingText != null)
        {
            HideRemainingText();
        }
        SetSelected(false);
    }

    private void Update()
    {
        if (rectTransform == null)
        {
            return;
        }

        if (flyingToBucket)
        {
            flyTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(flyTimer / 0.8f);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            Vector2 arc = Vector2.up * Mathf.Sin(progress * Mathf.PI) * 150f;
            rectTransform.anchoredPosition = Vector2.Lerp(flyStartPosition, flyTargetPosition, eased) + arc;
            pulseScale = Mathf.Lerp(pulseScale, 0.38f, Time.deltaTime * 7f);
            rectTransform.localScale = Vector3.one * pulseScale;
            if (animalImage != null)
            {
                Color color = animalImage.color;
                color.a = Mathf.Lerp(1f, 0.15f, progress);
                animalImage.color = color;
            }
            return;
        }

        float t = Time.time + swimSeed;
        swimOffset = new Vector2(Mathf.Sin(t * 0.62f) * 36f, Mathf.Cos(t * 0.48f) * 22f);
        Vector2 target = basePosition + swimOffset;
        if (IsCaptured)
        {
            target += new Vector2(0f, 86f);
        }

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            target += new Vector2(Mathf.Sin(Time.time * 38f) * 7f, 0f);
        }

        if (glowTimer > 0f)
        {
            glowTimer -= Time.deltaTime;
        }

        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, target, Time.deltaTime * 2.8f);

        float targetScale = IsCaptured ? 1.24f : (isSelected ? 1.16f : (isHovered ? 1.08f : 1f));
        pulseScale = Mathf.Lerp(pulseScale, targetScale, Time.deltaTime * 7f);
        rectTransform.localScale = Vector3.one * pulseScale;

        if (animalImage != null)
        {
            Color color = animalImage.color;
            color.a = IsCaptured ? Mathf.Lerp(color.a, 0.45f, Time.deltaTime * 2.4f) : Mathf.Lerp(color.a, 1f, Time.deltaTime * 4f);
            if (glowTimer > 0f)
            {
                color = Color.Lerp(color, new Color(1f, 0.9f, 0.22f, 1f), 0.35f + Mathf.Sin(Time.time * 8f) * 0.12f);
            }
            animalImage.color = color;
        }
    }

    private void CreateCaptureBubbles(Sprite bubbleSprite)
    {
        GameObject row = new GameObject("CaptureBubbles", typeof(RectTransform));
        row.transform.SetParent(transform, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0f);
        rowRect.anchorMax = new Vector2(0.5f, 0f);
        rowRect.anchoredPosition = new Vector2(0f, -2f);
        rowRect.sizeDelta = new Vector2(150f, 24f);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;

        for (int i = 0; i < requiredHits; i++)
        {
            GameObject bubble = new GameObject("Bubble_" + i, typeof(RectTransform));
            bubble.transform.SetParent(row.transform, false);
            Image image = bubble.AddComponent<Image>();
            image.sprite = bubbleSprite;
            image.color = new Color(0.78f, 0.95f, 1f, 0.28f);
            image.preserveAspect = true;
            image.raycastTarget = false;
            LayoutElement element = bubble.AddComponent<LayoutElement>();
            element.minWidth = 18f;
            element.minHeight = 18f;
            captureBubbles.Add(image);
        }
    }

    private void SetLessonInternal(OceanLesson lesson, Sprite bubbleSprite)
    {
        Lesson = lesson;
        int newRequiredHits = Mathf.Max(1, lesson != null ? lesson.requiredHits : requiredHits);
        if (captureBubbles.Count > 0 && newRequiredHits != requiredHits)
        {
            Transform existing = transform.Find("CaptureBubbles");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }
            captureBubbles.Clear();
        }

        requiredHits = newRequiredHits;
        if (captureBubbles.Count == 0)
        {
            CreateCaptureBubbles(bubbleSprite);
        }

        if (nameText != null)
        {
            nameText.text = IsMystery ? "?" : (Lesson != null ? Lesson.animalName : "");
        }
        if (meterText != null)
        {
            meterText.text = IsMystery ? "?" : "Tap!";
        }

        RefreshCaptureBubbles(OceanRhythmHitResult.Near);
    }

    private void RefreshCaptureBubbles(OceanRhythmHitResult result)
    {
        for (int i = 0; i < captureBubbles.Count; i++)
        {
            Image bubble = captureBubbles[i];
            if (bubble == null)
            {
                continue;
            }

            if (i < CaptureProgress)
            {
                bubble.color = result == OceanRhythmHitResult.Perfect
                    ? new Color(1f, 0.86f, 0.18f, 1f)
                    : new Color(0.27f, 0.95f, 0.54f, 1f);
            }
            else
            {
                bubble.color = result == OceanRhythmHitResult.Miss
                    ? new Color(1f, 0.46f, 0.42f, 0.45f)
                    : new Color(0.78f, 0.95f, 1f, 0.28f);
            }
        }

        if (remainingText != null)
        {
            HideRemainingText();
        }
    }

    private void HideRemainingText()
    {
        if (remainingText != null)
        {
            remainingText.text = "";
            remainingText.gameObject.SetActive(false);
        }
    }

    private Text CreateText(string name, string value, Font font, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(transform, false);
        RectTransform textRect = obj.GetComponent<RectTransform>();
        textRect.anchorMin = anchor;
        textRect.anchorMax = anchor;
        textRect.anchoredPosition = position;
        textRect.sizeDelta = size;

        Text text = obj.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = fontSize;
        return text;
    }
}
