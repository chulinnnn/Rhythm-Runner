using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    private const int requiredHits = 6;

    private RectTransform rectTransform;
    private Image animalImage;
    private Text nameText;
    private Text meterText;
    private Text remainingText;
    private readonly List<Image> captureBubbles = new List<Image>();
    private Vector2 basePosition;
    private Vector2 swimOffset;
    private float swimSeed;
    private bool isSelected;
    private bool isHovered;
    private float pulseScale = 1f;
    private float shakeTimer;

    public void Build(OceanLesson lesson, Sprite animalSprite, Sprite bubbleSprite, Font font, Color fallbackColor, Vector2 startPosition)
    {
        Build(lesson, animalSprite, bubbleSprite, font, fallbackColor, startPosition, lesson.animalKey);
    }

    public void Build(OceanLesson lesson, Sprite animalSprite, Sprite bubbleSprite, Font font, Color fallbackColor, Vector2 startPosition, string instanceId)
    {
        Lesson = lesson;
        InstanceId = instanceId;
        rectTransform = GetComponent<RectTransform>();
        basePosition = startPosition;
        rectTransform.anchoredPosition = startPosition;
        swimSeed = Random.Range(0f, 100f);

        animalImage = gameObject.AddComponent<Image>();
        animalImage.sprite = animalSprite != null ? animalSprite : bubbleSprite;
        animalImage.color = fallbackColor;
        animalImage.preserveAspect = true;

        string displayName = IsMystery ? "?" : lesson.animalName;
        nameText = CreateText("Name", displayName, font, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(160f, 40f), 22, FontStyle.Bold, new Color(0.02f, 0.16f, 0.24f));
        nameText.raycastTarget = false;

        meterText = CreateText("Meter", IsMystery ? "?" : lesson.meterLabel, font, new Vector2(0.5f, 1f), new Vector2(0f, 36f), new Vector2(110f, 34f), 24, FontStyle.Bold, Color.white);
        meterText.raycastTarget = false;

        remainingText = CreateText("Remaining", RemainingHitsText, font, new Vector2(0.5f, 0f), new Vector2(0f, -32f), new Vector2(130f, 30f), 20, FontStyle.Bold, Color.white);
        remainingText.raycastTarget = false;

        CreateCaptureBubbles(bubbleSprite);
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (IsMystery && selected)
        {
            nameText.text = Lesson.animalName;
            meterText.text = Lesson.meterLabel;
        }

        if (meterText != null)
        {
            meterText.gameObject.SetActive(selected || isHovered);
        }
        if (remainingText != null)
        {
            remainingText.gameObject.SetActive(selected || CaptureProgress > 0);
        }
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
        if (meterText != null)
        {
            meterText.gameObject.SetActive(isSelected || isHovered);
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

    public void PlayRescue()
    {
        IsCaptured = true;
        pulseScale = 1.45f;
        SetSelected(false);
    }

    public void ResetCatch()
    {
        CaptureProgress = 0;
        IsCaptured = false;
        shakeTimer = 0f;
        pulseScale = 1f;
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
            remainingText.gameObject.SetActive(false);
        }
        SetSelected(false);
    }

    private void Update()
    {
        if (rectTransform == null)
        {
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

        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, target, Time.deltaTime * 2.8f);

        float targetScale = IsCaptured ? 1.24f : (isSelected ? 1.16f : (isHovered ? 1.08f : 1f));
        pulseScale = Mathf.Lerp(pulseScale, targetScale, Time.deltaTime * 7f);
        rectTransform.localScale = Vector3.one * pulseScale;

        if (animalImage != null)
        {
            Color color = animalImage.color;
            color.a = IsCaptured ? Mathf.Lerp(color.a, 0.45f, Time.deltaTime * 2.4f) : Mathf.Lerp(color.a, 1f, Time.deltaTime * 4f);
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
            LayoutElement element = bubble.AddComponent<LayoutElement>();
            element.minWidth = 18f;
            element.minHeight = 18f;
            captureBubbles.Add(image);
        }
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
            remainingText.text = RemainingHitsText;
            remainingText.gameObject.SetActive(true);
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
