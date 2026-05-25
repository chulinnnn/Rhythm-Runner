using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OceanRhythmUIController : MonoBehaviour
{
    private OceanRhythmManager manager;
    private Canvas canvas;
    private Font uiFont;
    private Text titleText;
    private Text subtitleText;
    private Text instructionText;
    private Text feedbackText;
    private Text progressText;
    private Text lessonCounterText;
    private Image progressFill;
    private Transform beatBubbleRoot;
    private OceanAnimalController animalController;
    private GameObject completeOverlay;
    private readonly List<Image> beatBubbles = new List<Image>();
    private Sprite circleSprite;

    public void Build(OceanRhythmManager owner)
    {
        manager = owner;
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        circleSprite = CreateCircleSprite("OceanCircleSprite", 96, Color.white);
        EnsureEventSystem();

        GameObject existing = GameObject.Find("OceanRhythmCanvas");
        if (existing != null)
        {
            Destroy(existing);
        }

        GameObject canvasObject = new GameObject("OceanRhythmCanvas", typeof(RectTransform));
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject root = CreateRect(canvasObject.transform, "OceanRoot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image background = root.AddComponent<Image>();
        background.color = new Color(0.03f, 0.43f, 0.66f, 1f);
        Sprite bgSprite = manager != null ? manager.GetWaterBackgroundSprite() : null;
        if (bgSprite != null)
        {
            background.sprite = bgSprite;
            background.preserveAspect = false;
        }

        WaterRippleController rippleController = root.AddComponent<WaterRippleController>();
        rippleController.Initialize(circleSprite);

        CreateAnimatedWater(root.transform);
        CreateTopBar(root.transform);
        CreateCenterStage(root.transform);
        CreateBottomHud(root.transform);
        CreateCompleteOverlay(root.transform);
        CreateNavigation(root.transform);

        completeOverlay.SetActive(false);
    }

    public void ShowLesson(OceanLesson lesson, int lessonNumber, int lessonCount, int currentProgress)
    {
        if (lesson == null)
        {
            return;
        }

        titleText.text = lesson.animalName;
        subtitleText.text = lesson.meterLabel + "  " + Mathf.RoundToInt(lesson.bpm) + " BPM";
        instructionText.text = lesson.instruction + "\nTap Space on the bright bubble.";
        feedbackText.text = "Listen first";
        feedbackText.color = new Color(1f, 0.95f, 0.68f);
        lessonCounterText.text = lessonNumber + " / " + lessonCount;

        BuildBeatBubbles(lesson.beatsPerBar);
        SetProgress(currentProgress, lesson.requiredHits);

        if (animalController != null)
        {
            animalController.SetAnimal(lesson.animalName, manager != null ? manager.GetSpriteForLesson(lesson) : null, ColorForAnimal(lesson.animalKey));
        }

        completeOverlay.SetActive(false);
    }

    public void PulseBeat(int beatInBar, bool accented)
    {
        for (int i = 0; i < beatBubbles.Count; i++)
        {
            Image bubble = beatBubbles[i];
            if (bubble == null)
            {
                continue;
            }

            if (i == beatInBar)
            {
                bubble.color = accented ? new Color(1f, 0.86f, 0.18f, 1f) : new Color(0.25f, 0.74f, 1f, 1f);
                bubble.transform.localScale = accented ? Vector3.one * 1.22f : Vector3.one * 1.12f;
            }
            else
            {
                bubble.color = new Color(0.78f, 0.95f, 1f, 0.46f);
                bubble.transform.localScale = Vector3.one;
            }
        }

        if (animalController != null)
        {
            animalController.Bounce(accentedScale: accented ? 1.08f : 1.04f);
        }
    }

    public void ShowInputResult(OceanRhythmHitResult result, float timingError, int currentProgress, int requiredHits)
    {
        string text;
        Color color;
        if (result == OceanRhythmHitResult.Perfect)
        {
            text = "Perfect";
            color = new Color(1f, 0.86f, 0.18f);
        }
        else if (result == OceanRhythmHitResult.Good)
        {
            text = "Good";
            color = new Color(0.27f, 0.95f, 0.54f);
        }
        else if (result == OceanRhythmHitResult.Near)
        {
            text = "Almost";
            color = new Color(0.46f, 0.82f, 1f);
        }
        else
        {
            text = "Wait for the bright bubble";
            color = new Color(1f, 0.46f, 0.42f);
        }

        feedbackText.text = text;
        feedbackText.color = color;
        feedbackText.transform.localScale = Vector3.one * 1.1f;
        SetProgress(currentProgress, requiredHits);
    }

    public void ShowLessonComplete(OceanLesson lesson, bool finalLesson)
    {
        if (animalController != null)
        {
            animalController.Capture();
        }

        completeOverlay.SetActive(true);
        Text message = completeOverlay.transform.Find("Card/Message").GetComponent<Text>();
        message.text = finalLesson ? "Ocean rhythm complete!" : lesson.animalName + " rescued!";

        Text detail = completeOverlay.transform.Find("Card/Detail").GetComponent<Text>();
        detail.text = finalLesson ? "Great listening. Returning to Start." : "Next rhythm friend is coming.";
    }

    private void SetProgress(int currentProgress, int requiredHits)
    {
        float ratio = requiredHits <= 0 ? 0f : Mathf.Clamp01((float)currentProgress / requiredHits);
        if (progressFill != null)
        {
            progressFill.fillAmount = ratio;
        }
        if (progressText != null)
        {
            progressText.text = currentProgress + " / " + requiredHits;
        }
    }

    private void BuildBeatBubbles(int count)
    {
        if (beatBubbleRoot == null)
        {
            return;
        }

        for (int i = beatBubbleRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(beatBubbleRoot.GetChild(i).gameObject);
        }
        beatBubbles.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject bubble = CreateRect(beatBubbleRoot, "BeatBubble_" + (i + 1), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(58f, 58f)).gameObject;
            Image image = bubble.AddComponent<Image>();
            image.sprite = circleSprite;
            image.color = new Color(0.78f, 0.95f, 1f, 0.46f);
            image.preserveAspect = true;

            Text number = CreateText(bubble.transform, "Number", (i + 1).ToString(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50f, 50f), 23, FontStyle.Bold, new Color(0.02f, 0.16f, 0.24f), TextAnchor.MiddleCenter);
            number.raycastTarget = false;

            LayoutElement layout = bubble.AddComponent<LayoutElement>();
            layout.minWidth = 58f;
            layout.minHeight = 58f;
            beatBubbles.Add(image);
        }
    }

    private void CreateAnimatedWater(Transform parent)
    {
        for (int i = 0; i < 6; i++)
        {
            GameObject band = CreateRect(parent, "SoftWave_" + i, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f + i * 92f), new Vector2(1480f, 58f)).gameObject;
            Image image = band.AddComponent<Image>();
            image.color = i % 2 == 0 ? new Color(0.24f, 0.86f, 0.95f, 0.14f) : new Color(1f, 1f, 1f, 0.08f);
        }
    }

    private void CreateTopBar(Transform parent)
    {
        GameObject top = CreatePanel(parent, "TopBar", new Color(0.02f, 0.12f, 0.2f, 0.74f));
        RectTransform topRect = top.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.5f, 1f);
        topRect.anchorMax = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0f, -58f);
        topRect.sizeDelta = new Vector2(1080f, 84f);

        titleText = CreateText(top.transform, "AnimalTitle", "Little Fish", new Vector2(0.5f, 0.62f), new Vector2(0f, 0f), new Vector2(620f, 44f), 34, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        subtitleText = CreateText(top.transform, "Meter", "4/4  76 BPM", new Vector2(0.5f, 0.2f), new Vector2(0f, 0f), new Vector2(480f, 30f), 22, FontStyle.Bold, new Color(1f, 0.86f, 0.24f), TextAnchor.MiddleCenter);
        lessonCounterText = CreateText(top.transform, "LessonCounter", "1 / 4", new Vector2(1f, 0.5f), new Vector2(-66f, 0f), new Vector2(120f, 40f), 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
    }

    private void CreateCenterStage(Transform parent)
    {
        GameObject animal = CreateRect(parent, "OceanAnimal", new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(0f, 4f), new Vector2(270f, 230f)).gameObject;
        animalController = animal.AddComponent<OceanAnimalController>();
        animalController.Build(circleSprite, uiFont);
    }

    private void CreateBottomHud(Transform parent)
    {
        GameObject card = CreatePanel(parent, "BottomHud", new Color(0.02f, 0.12f, 0.2f, 0.76f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0f);
        cardRect.anchorMax = new Vector2(0.5f, 0f);
        cardRect.anchoredPosition = new Vector2(0f, 118f);
        cardRect.sizeDelta = new Vector2(940f, 180f);

        instructionText = CreateText(card.transform, "Instruction", "", new Vector2(0.5f, 0.76f), Vector2.zero, new Vector2(850f, 58f), 25, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);

        GameObject beatRow = CreateRect(card.transform, "BeatBubbleRow", new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(560f, 62f)).gameObject;
        HorizontalLayoutGroup beatLayout = beatRow.AddComponent<HorizontalLayoutGroup>();
        beatLayout.spacing = 14;
        beatLayout.childAlignment = TextAnchor.MiddleCenter;
        beatLayout.childControlWidth = true;
        beatLayout.childControlHeight = true;
        beatLayout.childForceExpandWidth = false;
        beatBubbleRoot = beatRow.transform;

        feedbackText = CreateText(card.transform, "Feedback", "Listen first", new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(430f, 42f), 30, FontStyle.Bold, new Color(1f, 0.95f, 0.68f), TextAnchor.MiddleCenter);

        GameObject progress = CreatePanel(card.transform, "ProgressBar", new Color(1f, 1f, 1f, 0.2f));
        RectTransform progressRect = progress.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.5f, 0.05f);
        progressRect.anchorMax = new Vector2(0.5f, 0.05f);
        progressRect.anchoredPosition = Vector2.zero;
        progressRect.sizeDelta = new Vector2(640f, 22f);

        GameObject fill = CreateRect(progress.transform, "Fill", new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero).gameObject;
        progressFill = fill.AddComponent<Image>();
        progressFill.color = new Color(0.27f, 0.95f, 0.54f, 1f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;

        progressText = CreateText(card.transform, "ProgressText", "0 / 12", new Vector2(0.5f, 0.05f), new Vector2(0f, 28f), new Vector2(180f, 28f), 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
    }

    private void CreateCompleteOverlay(Transform parent)
    {
        completeOverlay = CreateRect(parent, "CompleteOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image shade = completeOverlay.AddComponent<Image>();
        shade.color = new Color(0.01f, 0.07f, 0.1f, 0.72f);

        GameObject card = CreatePanel(completeOverlay.transform, "Card", new Color(0.06f, 0.42f, 0.5f, 0.96f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(660f, 260f);

        CreateText(card.transform, "Message", "Rescued!", new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(580f, 70f), 42, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        CreateText(card.transform, "Detail", "Next rhythm friend is coming.", new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(580f, 54f), 26, FontStyle.Normal, new Color(1f, 0.94f, 0.68f), TextAnchor.MiddleCenter);
    }

    private void CreateNavigation(Transform parent)
    {
        CreateButton(parent, "BackButton", "Back", new Vector2(0f, 1f), new Vector2(82f, -56f), new Vector2(126f, 48f), delegate
        {
            if (manager != null)
            {
                manager.ReturnToStart();
            }
        });

        CreateButton(parent, "RetryButton", "Retry", new Vector2(1f, 1f), new Vector2(-86f, -56f), new Vector2(126f, 48f), delegate
        {
            if (manager != null)
            {
                manager.RestartCurrentLesson();
            }
        });
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = CreatePanel(parent, name, new Color(1f, 1f, 1f, 0.9f));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        button.onClick.AddListener(onClick);

        Text text = CreateText(obj.transform, "Text", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 22, FontStyle.Bold, new Color(0.02f, 0.15f, 0.22f), TextAnchor.MiddleCenter);
        text.raycastTarget = false;
        return button;
    }

    private Color ColorForAnimal(string animalKey)
    {
        if (animalKey == "Fish")
        {
            return new Color(1f, 0.72f, 0.2f);
        }
        if (animalKey == "Octopus")
        {
            return new Color(0.95f, 0.38f, 0.76f);
        }
        if (animalKey == "Turtle")
        {
            return new Color(0.34f, 0.82f, 0.45f);
        }
        return new Color(0.48f, 0.78f, 1f);
    }

    private GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return obj;
    }

    private Text CreateText(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color, TextAnchor alignment)
    {
        GameObject obj = CreateRect(parent, name, anchor, anchor, position, size).gameObject;
        Text text = obj.AddComponent<Text>();
        text.font = uiFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 14;
        text.resizeTextMaxSize = fontSize;
        return text;
    }

    private RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private Sprite CreateCircleSprite(string spriteName, int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.47f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? color : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
