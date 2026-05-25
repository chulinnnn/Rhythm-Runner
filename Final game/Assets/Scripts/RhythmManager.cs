using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum RhythmTimingResult
{
    None,
    Perfect,
    Good,
    Miss
}

public class RhythmManager : MonoBehaviour
{
    public static RhythmManager Instance { get; private set; }
    public event System.Action<RhythmTimingResult, string> InputReported;

    [Header("Music timing")]
    public AudioSource musicSource;
    public string fallbackMusicObjectName = "126bpm";
    public float bpm = 126f;
    public float firstBeatOffset = 0f;
    public bool useLevelTimeWhenMusicMissing = false;
    public float levelTimeFallbackStart = 0f;

    [Header("Timing windows")]
    public float perfectWindow = 0.08f;
    public float goodWindow = 0.15f;

    [Header("Feedback")]
    public Text feedbackText;
    public float feedbackDuration = 0.45f;
    public bool createFeedbackTextIfMissing = true;

    [Header("Score reward")]
    public bool awardRhythmScore = true;
    public int perfectBonus = 2;
    public int goodBonus = 1;

    [Header("Beat visualization")]
    public string visualizationObjectName = "visualization";
    public string secondVisualizationObjectName = "visual2";
    public string correctObjectName = "correct";
    public string goodObjectName = "good";
    public string missObjectName = "miss";
    public float visualizationBpm = 126f;
    public float visualizationPulseScale = 1.35f;
    public float visualizationPulseDuration = 0.12f;
    public float resultVisualDuration = 0.35f;
    public Color visualizationBaseColor = Color.white;
    public Color visualizationBeatColor = new Color(1f, 0.85f, 0.15f);

    [Header("Rhythm visual controls")]
    public bool visualizationEnabled = true;
    public bool syncVisualizationToMusic = true;
    public bool createVisualizationUiIfMissing = true;
    public string toggleButtonText = "Rhythm Visual";
    public bool timingOffsetDebugTextEnabled = false;

    [Header("Result colors")]
    public Color perfectColor = new Color(1f, 0.85f, 0.15f);
    public Color goodColor = new Color(0.25f, 0.9f, 1f);
    public Color missColor = new Color(1f, 0.25f, 0.2f);
    public Color noneColor = Color.gray;

    [Header("Result animation")]
    public float resultTextScale = 1.25f;
    public float resultTextReturnSpeed = 8f;
    public float missHideBeatDuration = 0.16f;

    [Header("Generated UI layout")]
    public Vector2 generatedRingAnchor = new Vector2(0.5f, 0.5f);
    public Vector2 generatedRingPosition = new Vector2(0f, 120f);
    public Vector2 generatedRingSize = new Vector2(128f, 128f);
    public Vector2 generatedFeedbackPosition = new Vector2(0f, 220f);
    public Vector2 generatedResultPosition = new Vector2(0f, -20f);
    public Vector2 generatedTogglePosition = new Vector2(-120f, -48f);

    private float feedbackTimer;
    private GameObject visualizationObject;
    private GameObject secondVisualizationObject;
    private Transform visualizationTransform;
    private Transform secondVisualizationTransform;
    private GameObject correctObject;
    private GameObject goodObject;
    private GameObject missObject;
    private Vector3 visualizationBaseScale;
    private Vector3 secondVisualizationBaseScale;
    private Vector3 feedbackBaseScale = Vector3.one;
    private Image visualizationImage;
    private Image secondVisualizationImage;
    private SpriteRenderer visualizationSpriteRenderer;
    private SpriteRenderer secondVisualizationSpriteRenderer;
    private Renderer visualizationRenderer;
    private Renderer secondVisualizationRenderer;
    private Text correctText;
    private Text goodText;
    private Text missText;
    private Text toggleButtonLabel;
    private Text timingDebugText;
    private Button toggleButton;
    private float visualizationPulseTimer;
    private int lastVisualizationBeat = -1;
    private bool showSecondVisualization;
    private float resultVisualTimer;
    private float missBeatHideTimer;

    private float BeatInterval
    {
        get { return 60f / Mathf.Max(1f, bpm); }
    }

    private float VisualizationBeatInterval
    {
        get { return 60f / Mathf.Max(1f, syncVisualizationToMusic ? bpm : visualizationBpm); }
    }

    void Awake()
    {
        Instance = this;
        ResolveMusicSource();
        EnsureFeedbackText();
        ResolveVisualization();
        EnsureVisualizationUi();
        ApplyVisualizationEnabledState();
    }

    void Update()
    {
        UpdateFeedbackText();
        UpdateResultVisuals();
        UpdateVisualization();
        UpdateTimingDebugText();
    }

    public RhythmTimingResult JudgeInput()
    {
        if ((musicSource == null || !musicSource.isPlaying) && !useLevelTimeWhenMusicMissing)
        {
            return RhythmTimingResult.None;
        }

        float songTime = GetSongTime();
        if (songTime < 0f)
        {
            return RhythmTimingResult.Miss;
        }

        float beatPosition = songTime / BeatInterval;
        float nearestBeat = Mathf.Round(beatPosition);
        float timeToNearestBeat = Mathf.Abs(songTime - nearestBeat * BeatInterval);

        if (timeToNearestBeat <= perfectWindow)
        {
            return RhythmTimingResult.Perfect;
        }

        if (timeToNearestBeat <= goodWindow)
        {
            return RhythmTimingResult.Good;
        }

        return RhythmTimingResult.Miss;
    }

    public RhythmTimingResult ReportInput(string actionName)
    {
        RhythmTimingResult result = JudgeInput();
        ShowFeedback(result, actionName);
        ShowResultVisual(result);
        AwardScore(result);
        if (InputReported != null)
        {
            InputReported(result, actionName);
        }
        return result;
    }

    public float GetAdjustedSongTime()
    {
        return GetSongTime();
    }

    public void SetVisualizationEnabled(bool enabled)
    {
        visualizationEnabled = enabled;
        ApplyVisualizationEnabledState();
    }

    public void ToggleVisualizationEnabled()
    {
        SetVisualizationEnabled(!visualizationEnabled);
    }

    public void SetVisualizationEnabledToggle()
    {
        ToggleVisualizationEnabled();
    }

    private float GetSongTime()
    {
        if (musicSource == null)
        {
            return useLevelTimeWhenMusicMissing ? Time.timeSinceLevelLoad - levelTimeFallbackStart - firstBeatOffset : 0f;
        }

        if (!musicSource.isPlaying && useLevelTimeWhenMusicMissing)
        {
            return Time.timeSinceLevelLoad - levelTimeFallbackStart - firstBeatOffset;
        }

        return musicSource.time - firstBeatOffset;
    }

    private float GetVisualizationTime()
    {
        if (syncVisualizationToMusic && musicSource != null && musicSource.isPlaying)
        {
            return GetSongTime();
        }

        return Time.time;
    }

    private void AwardScore(RhythmTimingResult result)
    {
        if (!awardRhythmScore || GameManager.Instance == null)
        {
            return;
        }

        if (result == RhythmTimingResult.Perfect)
        {
            GameManager.Instance.UpdateBonus(perfectBonus);
        }
        else if (result == RhythmTimingResult.Good)
        {
            GameManager.Instance.UpdateBonus(goodBonus);
        }
    }

    private void ShowFeedback(RhythmTimingResult result, string actionName)
    {
        if (!visualizationEnabled || feedbackText == null)
        {
            return;
        }

        feedbackText.text = GetFeedbackLabel(result, actionName);
        feedbackText.color = GetResultColor(result);
        feedbackText.transform.localScale = feedbackBaseScale * resultTextScale;
        feedbackTimer = feedbackDuration;
    }

    private string GetFeedbackLabel(RhythmTimingResult result, string actionName)
    {
        string label;
        if (result == RhythmTimingResult.Perfect)
        {
            label = "PERFECT";
        }
        else if (result == RhythmTimingResult.Good)
        {
            label = "GOOD";
        }
        else if (result == RhythmTimingResult.Miss)
        {
            label = "MISS";
        }
        else
        {
            label = "NO MUSIC";
        }

        if (!string.IsNullOrEmpty(actionName))
        {
            label += " " + actionName.ToUpper();
        }

        return label;
    }

    private Color GetResultColor(RhythmTimingResult result)
    {
        if (result == RhythmTimingResult.Perfect)
        {
            return perfectColor;
        }

        if (result == RhythmTimingResult.Good)
        {
            return goodColor;
        }

        if (result == RhythmTimingResult.Miss)
        {
            return missColor;
        }

        return noneColor;
    }

    private void UpdateFeedbackText()
    {
        if (feedbackText == null)
        {
            return;
        }

        feedbackText.transform.localScale = Vector3.Lerp(
            feedbackText.transform.localScale,
            feedbackBaseScale,
            resultTextReturnSpeed * Time.deltaTime);

        if (feedbackTimer <= 0f)
        {
            return;
        }

        feedbackTimer -= Time.deltaTime;
        if (feedbackTimer <= 0f)
        {
            feedbackText.text = "";
        }
    }

    private void ResolveVisualization()
    {
        if (visualizationTransform != null)
        {
            return;
        }

        visualizationObject = FindSceneObjectByName(visualizationObjectName);
        if (visualizationObject == null)
        {
            return;
        }

        visualizationObject.SetActive(true);
        visualizationTransform = visualizationObject.transform;
        visualizationBaseScale = visualizationTransform.localScale;
        visualizationImage = visualizationObject.GetComponent<Image>();
        visualizationSpriteRenderer = visualizationObject.GetComponent<SpriteRenderer>();
        visualizationRenderer = visualizationObject.GetComponent<Renderer>();

        secondVisualizationObject = FindChildObjectByName(visualizationTransform, secondVisualizationObjectName);
        if (secondVisualizationObject == null)
        {
            secondVisualizationObject = FindSceneObjectByName(secondVisualizationObjectName);
        }

        if (secondVisualizationObject != null)
        {
            secondVisualizationObject.SetActive(true);
            secondVisualizationTransform = secondVisualizationObject.transform;
            secondVisualizationBaseScale = secondVisualizationTransform.localScale;
            secondVisualizationImage = secondVisualizationObject.GetComponent<Image>();
            secondVisualizationSpriteRenderer = secondVisualizationObject.GetComponent<SpriteRenderer>();
            secondVisualizationRenderer = secondVisualizationObject.GetComponent<Renderer>();
        }

        correctObject = FindSceneObjectByName(correctObjectName);
        goodObject = FindSceneObjectByName(goodObjectName);
        missObject = FindSceneObjectByName(missObjectName);
        correctText = ResolveResultText(correctObject, "PERFECT");
        goodText = ResolveResultText(goodObject, "GOOD");
        missText = ResolveResultText(missObject, "MISS");

        SetVisualizationColor(visualizationBaseColor);
        SetSecondVisualizationColor(visualizationBaseColor);
        HideResultObjects();
    }

    private Text ResolveResultText(GameObject target, string defaultText)
    {
        if (target == null)
        {
            return null;
        }

        Text text = target.GetComponent<Text>();
        if (text != null)
        {
            text.text = defaultText;
            text.fontStyle = FontStyle.Bold;
            return text;
        }

        text = target.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.text = defaultText;
            text.fontStyle = FontStyle.Bold;
        }

        return text;
    }

    private void UpdateVisualization()
    {
        if (!visualizationEnabled)
        {
            return;
        }

        if (visualizationTransform == null)
        {
            ResolveVisualization();
            if (visualizationTransform == null)
            {
                return;
            }
        }

        if (missBeatHideTimer > 0f)
        {
            missBeatHideTimer -= Time.deltaTime;
            SetBeatVisualsVisible(false, false);
            return;
        }

        if (resultVisualTimer > 0f)
        {
            SetBeatVisualsVisible(false, false);
            return;
        }

        float songTime = GetVisualizationTime();
        if (syncVisualizationToMusic && (musicSource == null || !musicSource.isPlaying || songTime < 0f))
        {
            SetBeatVisualsVisible(true, secondVisualizationTransform != null);
            return;
        }

        float interval = VisualizationBeatInterval;
        int currentBeat = Mathf.FloorToInt(songTime / interval);
        if (currentBeat != lastVisualizationBeat)
        {
            lastVisualizationBeat = currentBeat;
            visualizationPulseTimer = visualizationPulseDuration;
            showSecondVisualization = !showSecondVisualization;
        }

        if (visualizationPulseTimer > 0f)
        {
            visualizationPulseTimer -= Time.deltaTime;
        }

        float pulse = Mathf.Clamp01(visualizationPulseTimer / Mathf.Max(0.01f, visualizationPulseDuration));
        float scale = Mathf.Lerp(1f, visualizationPulseScale, pulse);
        Color color = Color.Lerp(visualizationBaseColor, visualizationBeatColor, pulse);

        SetBeatVisualsVisible(!showSecondVisualization, showSecondVisualization);
        if (showSecondVisualization && secondVisualizationTransform != null)
        {
            secondVisualizationTransform.localScale = secondVisualizationBaseScale * scale;
            SetSecondVisualizationColor(color);
        }
        else
        {
            visualizationTransform.localScale = visualizationBaseScale * scale;
            SetVisualizationColor(color);
        }
    }

    private void ShowResultVisual(RhythmTimingResult result)
    {
        if (!visualizationEnabled)
        {
            return;
        }

        if (correctObject == null || missObject == null)
        {
            ResolveVisualization();
        }

        bool isPerfect = result == RhythmTimingResult.Perfect;
        bool isGood = result == RhythmTimingResult.Good;
        bool isMiss = result == RhythmTimingResult.Miss;

        if (!isPerfect && !isGood && !isMiss)
        {
            return;
        }

        resultVisualTimer = resultVisualDuration;
        SetBeatVisualsVisible(false, false);

        if (correctObject != null)
        {
            correctObject.SetActive(isPerfect);
            SetResultVisual(correctObject, correctText, "PERFECT", perfectColor);
        }

        if (goodObject != null)
        {
            goodObject.SetActive(isGood);
            SetResultVisual(goodObject, goodText, "GOOD", goodColor);
        }

        if (missObject != null)
        {
            missObject.SetActive(isMiss);
            SetResultVisual(missObject, missText, "MISS", missColor);
        }

        if (isMiss)
        {
            missBeatHideTimer = missHideBeatDuration;
        }
    }

    private void SetResultVisual(GameObject target, Text text, string label, Color color)
    {
        SetResultText(text, label, color);
        SetObjectColor(target, color);
    }

    private void SetResultText(Text text, string label, Color color)
    {
        if (text == null)
        {
            return;
        }

        text.text = label;
        text.color = color;
        text.transform.localScale = Vector3.one * resultTextScale;
    }

    private void SetObjectColor(GameObject target, Color color)
    {
        if (target == null)
        {
            return;
        }

        Image[] images = target.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            images[i].color = color;
        }

        SpriteRenderer[] spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].color = color;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material != null)
            {
                renderers[i].material.color = color;
            }
        }
    }

    private void UpdateResultVisuals()
    {
        if (resultVisualTimer <= 0f)
        {
            return;
        }

        resultVisualTimer -= Time.deltaTime;
        if (correctText != null)
        {
            correctText.transform.localScale = Vector3.Lerp(correctText.transform.localScale, Vector3.one, resultTextReturnSpeed * Time.deltaTime);
        }

        if (goodText != null)
        {
            goodText.transform.localScale = Vector3.Lerp(goodText.transform.localScale, Vector3.one, resultTextReturnSpeed * Time.deltaTime);
        }

        if (missText != null)
        {
            missText.transform.localScale = Vector3.Lerp(missText.transform.localScale, Vector3.one, resultTextReturnSpeed * Time.deltaTime);
        }

        if (resultVisualTimer <= 0f)
        {
            HideResultObjects();
        }
    }

    private void HideResultObjects()
    {
        if (correctObject != null)
        {
            correctObject.SetActive(false);
        }

        if (goodObject != null)
        {
            goodObject.SetActive(false);
        }

        if (missObject != null)
        {
            missObject.SetActive(false);
        }
    }

    private void ApplyVisualizationEnabledState()
    {
        if (!visualizationEnabled)
        {
            SetBeatVisualsVisible(false, false);
            HideResultObjects();
            if (feedbackText != null)
            {
                feedbackText.text = "";
            }
        }

        if (toggleButtonLabel != null)
        {
            toggleButtonLabel.text = visualizationEnabled ? toggleButtonText + ": ON" : toggleButtonText + ": OFF";
        }
    }

    private void SetBeatVisualsVisible(bool showFirst, bool showSecond)
    {
        if (!visualizationEnabled)
        {
            showFirst = false;
            showSecond = false;
        }

        if (visualizationObject != null && !visualizationObject.activeSelf)
        {
            visualizationObject.SetActive(true);
        }

        if (secondVisualizationObject != null && !secondVisualizationObject.activeSelf)
        {
            secondVisualizationObject.SetActive(true);
        }

        SetSelfVisualVisible(visualizationImage, visualizationSpriteRenderer, visualizationRenderer, showFirst);
        SetSelfVisualVisible(secondVisualizationImage, secondVisualizationSpriteRenderer, secondVisualizationRenderer, showSecond);
    }

    private void SetSelfVisualVisible(
        Image image,
        SpriteRenderer spriteRenderer,
        Renderer renderer,
        bool visible)
    {
        if (image != null)
        {
            image.enabled = visible;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }

        if (renderer != null)
        {
            renderer.enabled = visible;
        }
    }

    private void SetVisualizationColor(Color color)
    {
        if (visualizationImage != null)
        {
            visualizationImage.color = color;
        }

        if (visualizationSpriteRenderer != null)
        {
            visualizationSpriteRenderer.color = color;
        }

        if (visualizationRenderer != null && visualizationRenderer.material != null)
        {
            visualizationRenderer.material.color = color;
        }
    }

    private void SetSecondVisualizationColor(Color color)
    {
        if (secondVisualizationImage != null)
        {
            secondVisualizationImage.color = color;
        }

        if (secondVisualizationSpriteRenderer != null)
        {
            secondVisualizationSpriteRenderer.color = color;
        }

        if (secondVisualizationRenderer != null && secondVisualizationRenderer.material != null)
        {
            secondVisualizationRenderer.material.color = color;
        }
    }

    private void EnsureVisualizationUi()
    {
        if (!createVisualizationUiIfMissing)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("RhythmCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (visualizationObject == null)
        {
            visualizationObject = CreateRingImage(canvas.transform, visualizationObjectName, generatedRingPosition, generatedRingSize, visualizationBaseColor);
            visualizationTransform = visualizationObject.transform;
            visualizationBaseScale = visualizationTransform.localScale;
            visualizationImage = visualizationObject.GetComponent<Image>();
        }

        if (secondVisualizationObject == null)
        {
            secondVisualizationObject = CreateRingImage(canvas.transform, secondVisualizationObjectName, generatedRingPosition, generatedRingSize * 0.72f, visualizationBaseColor);
            secondVisualizationTransform = secondVisualizationObject.transform;
            secondVisualizationBaseScale = secondVisualizationTransform.localScale;
            secondVisualizationImage = secondVisualizationObject.GetComponent<Image>();
        }

        if (correctObject == null)
        {
            correctText = CreateResultText(canvas.transform, correctObjectName, "PERFECT", perfectColor);
            correctObject = correctText.gameObject;
        }

        if (goodObject == null)
        {
            goodText = CreateResultText(canvas.transform, goodObjectName, "GOOD", goodColor);
            goodObject = goodText.gameObject;
        }

        if (missObject == null)
        {
            missText = CreateResultText(canvas.transform, missObjectName, "MISS", missColor);
            missObject = missText.gameObject;
        }

        EnsureToggleButton(canvas.transform);
        EnsureEventSystem();
        EnsureTimingDebugText(canvas.transform);
        HideResultObjects();
    }

    private GameObject CreateRingImage(Transform parent, string objectName, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = generatedRingAnchor;
        rect.anchorMax = generatedRingAnchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.sprite = CreateRingSprite();
        image.color = color;
        image.raycastTarget = false;

        return obj;
    }

    private Text CreateResultText(Transform parent, string objectName, string textValue, Color color)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = generatedResultPosition;
        rect.sizeDelta = new Vector2(460f, 72f);

        Text text = obj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 48;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.text = textValue;
        text.color = color;
        return text;
    }

    private void EnsureToggleButton(Transform parent)
    {
        GameObject existing = FindSceneObjectByName("RhythmVisualToggleButton");
        if (existing != null)
        {
            toggleButton = existing.GetComponent<Button>();
            toggleButtonLabel = existing.GetComponentInChildren<Text>(true);
        }

        if (toggleButton == null)
        {
            GameObject obj = new GameObject("RhythmVisualToggleButton", typeof(RectTransform));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.anchoredPosition = generatedTogglePosition;
            rect.sizeDelta = new Vector2(180f, 44f);

            Image image = obj.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);

            toggleButton = obj.AddComponent<Button>();
            toggleButton.onClick.AddListener(ToggleVisualizationEnabled);

            GameObject labelObject = new GameObject("Text", typeof(RectTransform));
            labelObject.transform.SetParent(obj.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            toggleButtonLabel = labelObject.AddComponent<Text>();
            toggleButtonLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            toggleButtonLabel.fontSize = 18;
            toggleButtonLabel.fontStyle = FontStyle.Bold;
            toggleButtonLabel.alignment = TextAnchor.MiddleCenter;
            toggleButtonLabel.color = Color.white;
        }
        else
        {
            toggleButton.onClick.RemoveListener(ToggleVisualizationEnabled);
            toggleButton.onClick.AddListener(ToggleVisualizationEnabled);
        }

        ApplyVisualizationEnabledState();
    }

    private void EnsureTimingDebugText(Transform parent)
    {
        GameObject existing = FindSceneObjectByName("RhythmTimingDebugText");
        if (existing != null)
        {
            timingDebugText = existing.GetComponent<Text>();
            return;
        }

        GameObject obj = new GameObject("RhythmTimingDebugText", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(170f, -48f);
        rect.sizeDelta = new Vector2(320f, 36f);

        timingDebugText = obj.AddComponent<Text>();
        timingDebugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timingDebugText.fontSize = 18;
        timingDebugText.alignment = TextAnchor.MiddleLeft;
        timingDebugText.color = new Color(1f, 1f, 1f, 0.8f);
        timingDebugText.raycastTarget = false;
        timingDebugText.text = "";
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject obj = new GameObject("EventSystem");
        obj.AddComponent<EventSystem>();
        obj.AddComponent<StandaloneInputModule>();
    }

    private void UpdateTimingDebugText()
    {
        if (timingDebugText == null)
        {
            return;
        }

        timingDebugText.enabled = timingOffsetDebugTextEnabled && visualizationEnabled;
        if (!timingDebugText.enabled)
        {
            return;
        }

        float songTime = GetSongTime();
        float beatPosition = songTime / BeatInterval;
        timingDebugText.text = "Beat " + beatPosition.ToString("0.00") + "  Offset " + firstBeatOffset.ToString("0.000");
    }

    private Sprite CreateRingSprite()
    {
        const int size = 96;
        const float outerRadius = 42f;
        const float innerRadius = 28f;

        Texture2D texture = new Texture2D(size, size);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= outerRadius && distance >= innerRadius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            GameObject obj = transforms[i].gameObject;
            if (obj.name == objectName && obj.scene.isLoaded)
            {
                return obj;
            }
        }

        return null;
    }

    private GameObject FindChildObjectByName(Transform parent, string objectName)
    {
        if (parent == null || string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == objectName)
            {
                return children[i].gameObject;
            }
        }

        return null;
    }

    private void ResolveMusicSource()
    {
        if (musicSource != null)
        {
            return;
        }

        GameObject musicObject = GameObject.Find(fallbackMusicObjectName);
        if (musicObject != null)
        {
            musicSource = musicObject.GetComponent<AudioSource>();
        }

        if (musicSource == null)
        {
            musicSource = FindObjectOfType<AudioSource>();
        }

        if (musicSource == null)
        {
            Debug.LogWarning("RhythmManager: No AudioSource found. Assign the music AudioSource in the Inspector.");
        }
    }

    private void EnsureFeedbackText()
    {
        if (feedbackText != null)
        {
            feedbackBaseScale = feedbackText.transform.localScale;
            return;
        }

        if (!createFeedbackTextIfMissing)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("RhythmFeedbackCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject textObject = new GameObject("RhythmFeedbackText", typeof(RectTransform));
        textObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = generatedFeedbackPosition;
        rect.sizeDelta = new Vector2(460f, 96f);

        feedbackText = textObject.AddComponent<Text>();
        feedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        feedbackText.fontSize = 46;
        feedbackText.fontStyle = FontStyle.Bold;
        feedbackText.alignment = TextAnchor.MiddleCenter;
        feedbackText.raycastTarget = false;
        feedbackText.text = "";
        feedbackBaseScale = feedbackText.transform.localScale;
    }
}
