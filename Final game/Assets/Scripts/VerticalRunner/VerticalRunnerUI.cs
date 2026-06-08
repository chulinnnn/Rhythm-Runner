using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VerticalRunnerUI : MonoBehaviour
{
    private VerticalRunnerManager manager;
    [SerializeField] private Canvas canvas;
    private Font font;
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;
    [SerializeField] private Text feedbackText;
    [SerializeField] private Text objectiveText;
    [SerializeField] private Text objectiveProgressText;
    [SerializeField] private Text objectiveRuleText;
    [SerializeField] private Text missesText;
    [SerializeField] private Text coinsText;
    [SerializeField] private Text comboText;
    [SerializeField] private Text progressText;
    [SerializeField] private Image progressFill;
    [SerializeField] private Image missesIcon;
    [SerializeField] private Transform beatLaneRoot;
    [SerializeField] private Button beatVisualToggleButton;
    [SerializeField] private Text beatVisualToggleLabel;
    [SerializeField] private GameObject resultOverlay;
    [SerializeField] private GameObject tutorialBriefingOverlay;
    [SerializeField] private GameObject gameRulesOverlay;
    [SerializeField] private GameObject tutorialCompleteRulesOverlay;
    [SerializeField] private GameObject gameControls;
    [SerializeField] private Text resultTitleText;
    [SerializeField] private Text resultStatsText;
    [SerializeField] private Image damageFlashImage;
    private readonly List<Image> beatDots = new List<Image>();
    private readonly List<GameObject> tutorialStepImages = new List<GameObject>();
    private GameObject tutorialImagesRoot;
    private Sprite circleSprite;
    private bool beatVisualVisible = true;
    private float lastBeatPosition;
    private int lastJumpStartBeat = 2;
    private int lastBeatsPerPlatform = 2;
    private bool missesValueOnly;
    private bool coinsValueOnly;
    private bool comboValueOnly;
    private Color missesTextDefaultColor = Color.white;
    private Color missesIconDefaultColor = Color.white;
    private Vector3 missesTextDefaultScale = Vector3.one;
    private Vector3 missesIconDefaultScale = Vector3.one;
    private Coroutine missesFlashRoutine;
    private Coroutine damageFlashRoutine;
    private Coroutine scoreFlashRoutine;
    private bool tutorialObjectiveActive;

    public bool IsReady { get; private set; }

    public void Build(VerticalRunnerManager manager, Sprite circleSprite)
    {
        Build(manager, circleSprite, RuntimeScenePolicy.Defaults());
    }

    public void Build(VerticalRunnerManager manager, Sprite circleSprite, RuntimeScenePolicy scenePolicy)
    {
        IsReady = false;
        this.manager = manager;
        this.circleSprite = circleSprite;
        if (scenePolicy == null)
        {
            scenePolicy = RuntimeScenePolicy.Defaults();
        }

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        EnsureEventSystem();
        GameObject existing = canvas != null ? canvas.gameObject : GameObject.Find("VerticalRunnerCanvas");
        if (existing != null && scenePolicy.useExistingSceneObjects && !scenePolicy.rebuildUiOnPlay)
        {
            if (BindExistingCanvas(existing))
            {
                IsReady = true;
                return;
            }

            Debug.LogWarning("VerticalRunnerUI: Existing VerticalRunnerCanvas is missing required children. Keeping it untouched.");
            return;
        }

        if (existing != null && scenePolicy.rebuildUiOnPlay)
        {
            Destroy(existing);
        }
        else if (existing != null)
        {
            return;
        }

        Debug.LogWarning("VerticalRunnerUI: VerticalRunnerCanvas is missing. Add it to the scene hierarchy before play.");
        return;

#pragma warning disable 162
        GameObject canvasObject = new GameObject("VerticalRunnerCanvas", typeof(RectTransform));
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 160;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        CreateHud(canvasObject.transform);
        CreateResultOverlay(canvasObject.transform);
        CreateTutorialBriefingOverlay(canvasObject.transform);
        CreateGameRulesOverlay(canvasObject.transform);
        resultOverlay.SetActive(false);
        tutorialBriefingOverlay.SetActive(false);
        gameRulesOverlay.SetActive(false);
        IsReady = true;
#pragma warning restore 162
    }

    private bool BindExistingCanvas(GameObject existing)
    {
        canvas = existing.GetComponent<Canvas>();
        if (canvas == null)
        {
            return false;
        }

        titleText = titleText != null ? titleText : FindText(existing.transform, "TopHud/Title");
        subtitleText = subtitleText != null ? subtitleText : FindText(existing.transform, "TopHud/Subtitle");
        missesText = ResolveCounterText(existing.transform, "TopHud/Misses", missesText, out missesValueOnly);
        if (missesText == null)
        {
            missesText = ResolveCounterText(existing.transform, "TopHud/Hearts", missesText, out missesValueOnly);
        }
        missesIcon = missesIcon != null ? missesIcon : FindImage(existing.transform, "TopHud/Misses/Icon");
        missesIcon = missesIcon != null ? missesIcon : FindImage(existing.transform, "TopHud/Hearts/Icon");
        coinsText = ResolveCounterText(existing.transform, "TopHud/Coins", coinsText, out coinsValueOnly);
        comboText = ResolveCounterText(existing.transform, "TopHud/Combo", comboText, out comboValueOnly);
        objectiveText = objectiveText != null ? objectiveText : FindText(existing.transform, "ObjectivePanel/Objective");
        objectiveProgressText = objectiveProgressText != null ? objectiveProgressText : FindText(existing.transform, "ObjectivePanel/Progress");
        objectiveRuleText = objectiveRuleText != null ? objectiveRuleText : FindText(existing.transform, "ObjectivePanel/Rules");
        feedbackText = feedbackText != null ? feedbackText : FindText(existing.transform, "BottomHud/Feedback");
        progressFill = progressFill != null ? progressFill : FindImage(existing.transform, "BottomHud/Progress/Fill");
        progressText = progressText != null ? progressText : FindText(existing.transform, "BottomHud/ProgressText");
        beatLaneRoot = beatLaneRoot != null ? beatLaneRoot : FindTransform(existing.transform, "BottomHud/BeatLane");
        beatVisualToggleButton = beatVisualToggleButton != null ? beatVisualToggleButton : FindButton(existing.transform, "BottomHud/BeatVisualToggleButton");
        beatVisualToggleLabel = beatVisualToggleLabel != null ? beatVisualToggleLabel : FindText(existing.transform, "BottomHud/BeatVisualToggleButton/Text");
        resultOverlay = resultOverlay != null ? resultOverlay : FindObject(existing.transform, "VerticalRunnerResult");
        tutorialBriefingOverlay = tutorialBriefingOverlay != null ? tutorialBriefingOverlay : FindObject(existing.transform, "TutorialBriefingOverlay");
        gameRulesOverlay = gameRulesOverlay != null ? gameRulesOverlay : FindObject(existing.transform, "GameRulesOverlay");
        tutorialCompleteRulesOverlay = tutorialCompleteRulesOverlay != null ? tutorialCompleteRulesOverlay : FindObject(existing.transform, "TutorialCompleteRulesOverlay");
        gameControls = gameControls != null ? gameControls : FindObject(existing.transform, "GameControls");
        resultTitleText = resultTitleText != null ? resultTitleText : FindText(existing.transform, "VerticalRunnerResult/Card/Title");
        resultStatsText = resultStatsText != null ? resultStatsText : FindText(existing.transform, "VerticalRunnerResult/Card/Stats");
        damageFlashImage = damageFlashImage != null ? damageFlashImage : FindImage(existing.transform, "DamageFlash");
        EnsureDamageFlash(existing.transform);

        if (titleText == null || subtitleText == null || missesText == null || coinsText == null || comboText == null
            || objectiveText == null || objectiveProgressText == null || objectiveRuleText == null
            || feedbackText == null || progressFill == null || progressText == null || beatLaneRoot == null
            || resultOverlay == null || tutorialBriefingOverlay == null || gameRulesOverlay == null
            || resultTitleText == null || resultStatsText == null)
        {
            return false;
        }

        CacheBeatDots();
        CacheTutorialStepImages(existing.transform);
        ConfigureProgressFill();
        CacheMissDefaults();
        beatVisualVisible = true;
        SetBeatVisualVisible(true);
        if (beatVisualToggleButton != null)
        {
            DisableKeyboardSelection(beatVisualToggleButton);
            beatVisualToggleButton.onClick.RemoveAllListeners();
            beatVisualToggleButton.onClick.AddListener(ToggleBeatVisual);
        }
        BindButton(existing.transform, "VerticalRunnerResult/Card/Retry", delegate
        {
            if (manager != null)
            {
                manager.RestartCurrentRun();
            }
        });
        BindButton(existing.transform, "VerticalRunnerResult/Card/Back", delegate { SceneTransitionManager.LoadScene("Start"); });
        BindButton(existing.transform, "GameControls/RetryButton", delegate
        {
            if (manager != null)
            {
                manager.RestartCurrentRun();
            }
        });
        BindButton(existing.transform, "GameControls/BackButton", delegate { SceneTransitionManager.LoadScene("Start"); });
        BindButton(existing.transform, "TutorialBriefingOverlay/Card/StartTutorialButton", delegate
        {
            if (manager != null)
            {
                manager.BeginTutorialAfterBriefing();
            }
        });
        BindButton(existing.transform, "GameRulesOverlay/Card/StartGameButton", delegate
        {
            if (manager != null)
            {
                manager.ContinueAfterGameRules();
            }
        });
        BindButton(existing.transform, "TutorialCompleteRulesOverlay/Card/StartGameButton", delegate
        {
            if (manager != null)
            {
                manager.ContinueAfterGameRules();
            }
        });

        resultOverlay.SetActive(false);
        tutorialBriefingOverlay.SetActive(false);
        gameRulesOverlay.SetActive(false);
        if (tutorialCompleteRulesOverlay != null)
        {
            tutorialCompleteRulesOverlay.SetActive(false);
        }
        SetGameControlsVisible(false);
        return true;
    }

    private Transform FindTransform(Transform root, string path)
    {
        return root != null ? root.Find(path) : null;
    }

    private GameObject FindObject(Transform root, string path)
    {
        Transform child = FindTransform(root, path);
        return child != null ? child.gameObject : null;
    }

    private Text FindText(Transform root, string path)
    {
        Transform child = FindTransform(root, path);
        return child != null ? child.GetComponent<Text>() : null;
    }

    private Text ResolveCounterText(Transform root, string path, Text serializedFallback, out bool valueOnly)
    {
        Text value = FindText(root, path + "/Value");
        if (value != null)
        {
            valueOnly = true;
            return value;
        }

        valueOnly = false;
        Text legacy = FindText(root, path);
        return legacy != null ? legacy : serializedFallback;
    }

    private Image FindImage(Transform root, string path)
    {
        Transform child = FindTransform(root, path);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private void EnsureDamageFlash(Transform root)
    {
        if (damageFlashImage == null)
        {
            Transform existing = root.Find("DamageFlash");
            if (existing != null)
            {
                damageFlashImage = existing.GetComponent<Image>();
            }
        }

        if (damageFlashImage == null)
        {
            GameObject flashObject = new GameObject("DamageFlash", typeof(RectTransform));
            flashObject.transform.SetParent(root, false);
            RectTransform rect = flashObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            damageFlashImage = flashObject.AddComponent<Image>();
        }

        damageFlashImage.raycastTarget = false;
        damageFlashImage.color = new Color(1f, 0.06f, 0.02f, 0f);
        damageFlashImage.transform.SetAsLastSibling();
    }

    private Button FindButton(Transform root, string path)
    {
        Transform child = FindTransform(root, path);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private void BindButton(Transform root, string path, UnityEngine.Events.UnityAction onClick)
    {
        Transform child = FindTransform(root, path);
        if (child == null)
        {
            return;
        }

        Button button = child.GetComponent<Button>();
        if (button == null)
        {
            button = child.gameObject.AddComponent<Button>();
            Graphic graphic = child.GetComponent<Graphic>();
            if (graphic != null)
            {
                button.targetGraphic = graphic;
            }
        }

        DisableKeyboardSelection(button);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
    }

    private void DisableKeyboardSelection(Button button)
    {
        if (button == null)
        {
            return;
        }

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
    }

    private void ClearUiSelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void ConfigureProgressFill()
    {
        if (progressFill == null)
        {
            return;
        }

        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;
        progressFill.raycastTarget = false;
    }

    private void CacheMissDefaults()
    {
        if (missesText != null)
        {
            missesTextDefaultColor = missesText.color;
            missesTextDefaultScale = missesText.transform.localScale;
        }
        if (missesIcon != null)
        {
            missesIconDefaultColor = missesIcon.color;
            missesIconDefaultColor.a = Mathf.Max(missesIconDefaultColor.a, 1f);
            missesIconDefaultScale = missesIcon.transform.localScale;
        }
    }

    private void CacheBeatDots()
    {
        beatDots.Clear();
        if (beatLaneRoot == null)
        {
            return;
        }

        Image[] images = beatLaneRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].name.StartsWith("BeatDot_"))
            {
                beatDots.Add(images[i]);
            }
        }
    }

    private void CacheTutorialStepImages(Transform root)
    {
        tutorialStepImages.Clear();
        tutorialImagesRoot = FindObject(root, "ObjectivePanel/TutorialImages");
        string[] names =
        {
            "BeatJumpImage",
            "HaystackClimbImage",
            "BananaPickupImage",
            "ParrotDodgeImage",
            "BigHaystackJumpImage",
            "MiniBananaClimbImage"
        };

        for (int i = 0; i < names.Length; i++)
        {
            GameObject imageObject = FindObject(root, "ObjectivePanel/TutorialImages/" + names[i]);
            if (imageObject != null)
            {
                imageObject.SetActive(false);
            }
            tutorialStepImages.Add(imageObject);
        }
    }

    private void ShowTutorialStepImage(int index)
    {
        for (int i = 0; i < tutorialStepImages.Count; i++)
        {
            if (tutorialStepImages[i] != null)
            {
                tutorialStepImages[i].SetActive(i == index);
            }
        }

        if (tutorialImagesRoot != null)
        {
            tutorialImagesRoot.SetActive(index >= 0);
        }
    }

    public void ShowTutorialBriefing()
    {
        SetGameControlsVisible(false);
        if (tutorialBriefingOverlay != null)
        {
            tutorialBriefingOverlay.SetActive(true);
        }
    }

    public void HideTutorialBriefing()
    {
        if (tutorialBriefingOverlay != null)
        {
            tutorialBriefingOverlay.SetActive(false);
        }
    }

    public void ShowGameRules(bool fromTutorial)
    {
        SetGameControlsVisible(false);
        GameObject overlay = fromTutorial && tutorialCompleteRulesOverlay != null ? tutorialCompleteRulesOverlay : gameRulesOverlay;
        if (overlay == null)
        {
            return;
        }

        overlay.SetActive(true);
        Transform card = overlay.transform.Find("Card");
        if (card == null)
        {
            return;
        }

        Transform titleTransform = card.Find("Title");
        Transform bodyTransform = card.Find("Body");
        Text title = titleTransform != null ? titleTransform.GetComponent<Text>() : null;
        Text body = bodyTransform != null ? bodyTransform.GetComponent<Text>() : null;
        if (title != null)
        {
            title.text = fromTutorial ? "Ready" : "Climb Up";
        }
        if (body != null)
        {
            body.text =
                "Space: jump\n" +
                "Down/S: banana\n" +
                "Space + Left/Right: parrot\n" +
                "Misses are counted.";
        }
    }

    public void HideGameRules()
    {
        if (gameRulesOverlay != null)
        {
            gameRulesOverlay.SetActive(false);
        }
        if (tutorialCompleteRulesOverlay != null)
        {
            tutorialCompleteRulesOverlay.SetActive(false);
        }
    }

    public void HideResult()
    {
        if (resultOverlay != null)
        {
            resultOverlay.SetActive(false);
        }
    }

    public void SetGameControlsVisible(bool visible)
    {
        if (gameControls != null)
        {
            gameControls.SetActive(visible);
        }
    }

    public void ShowTutorialStep(string title, string instruction, string objective, string hint, int current, int required, int index, int count)
    {
        titleText.text = index + " / " + count + "  " + title;
        subtitleText.text = instruction;
        feedbackText.text = hint;
        feedbackText.color = new Color(1f, 0.94f, 0.68f);
        tutorialObjectiveActive = true;
        ShowTutorialStepImage(index - 1);
        UpdateTutorialObjective(objective, hint, current, required);
    }

    public void ShowGameIntro()
    {
        titleText.text = "Monkey Climb";
        subtitleText.text = "Space every 2 beats";
        feedbackText.text = "Ready";
        feedbackText.color = new Color(1f, 0.94f, 0.68f);
        objectiveText.text = "Climb up";
        objectiveProgressText.text = BuildProgressDots(0, 5);
        objectiveRuleText.text = "Banana  Parrot";
        tutorialObjectiveActive = false;
        ShowTutorialStepImage(-1);
    }

    public void UpdateTutorialObjective(string objective, string hint, int current, int required)
    {
        objectiveText.text = objective;
        objectiveProgressText.text = BuildProgressDots(current, required);
        objectiveRuleText.text = hint;
    }

    public void UpdateStats(int misses, int score, int bananas, int combo, int maxCombo, float progress01)
    {
        missesText.text = missesValueOnly ? misses.ToString() : "Misses " + misses;
        coinsText.text = coinsValueOnly ? score.ToString() : "Score " + score;
        comboText.text = comboValueOnly ? bananas + " / " + combo + " / " + maxCombo : "Bananas " + bananas + "  Combo " + combo + "  Best " + maxCombo;
        progressFill.fillAmount = Mathf.Clamp01(progress01);
        progressText.text = Mathf.RoundToInt(progress01 * 100f) + "%";
        if (!tutorialObjectiveActive && objectiveProgressText != null)
        {
            objectiveProgressText.text = BuildProgressDots(Mathf.RoundToInt(Mathf.Clamp01(progress01) * 5f), 5);
        }
    }

    private string BuildProgressDots(int current, int required)
    {
        int count = Mathf.Clamp(required, 1, 6);
        int filled = Mathf.Clamp(current, 0, count);
        string dots = "";
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                dots += " ";
            }
            dots += i < filled ? "*" : "o";
        }

        return dots;
    }

    public void UpdateBeatLane(float beatPosition, int jumpStartBeat, int beatsPerPlatform)
    {
        lastBeatPosition = beatPosition;
        lastJumpStartBeat = jumpStartBeat;
        lastBeatsPerPlatform = Mathf.Max(1, beatsPerPlatform);
        if (!beatVisualVisible)
        {
            return;
        }

        ApplyBeatLaneHighlight(beatPosition, lastJumpStartBeat, lastBeatsPerPlatform);
    }

    private void ApplyBeatLaneHighlight(float beatPosition, int jumpStartBeat, int beatsPerPlatform)
    {
        int beatInBar = PositiveModulo(Mathf.FloorToInt(beatPosition), Mathf.Max(1, beatDots.Count));
        float frac = beatPosition - Mathf.Floor(beatPosition);
        for (int i = 0; i < beatDots.Count; i++)
        {
            bool jumpBeat = PositiveModulo(i - jumpStartBeat, beatsPerPlatform) == 0;
            bool active = jumpBeat && i == beatInBar;
            beatDots[i].color = active ? new Color(1f, 0.86f, 0.18f, 1f) : jumpBeat ? new Color(1f, 0.86f, 0.18f, 0.5f) : new Color(0.25f, 0.72f, 1f, 0.18f);
            beatDots[i].transform.localScale = active ? Vector3.one * Mathf.Lerp(1.35f, 0.92f, frac) : jumpBeat ? Vector3.one * 1.05f : Vector3.one * 0.78f;
        }
    }

    private int PositiveModulo(int value, int modulo)
    {
        if (modulo <= 0)
        {
            return 0;
        }

        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    private void ToggleBeatVisual()
    {
        SetBeatVisualVisible(!beatVisualVisible);
        ClearUiSelection();
    }

    private void SetBeatVisualVisible(bool visible)
    {
        beatVisualVisible = visible;
        if (beatLaneRoot != null)
        {
            beatLaneRoot.gameObject.SetActive(visible);
        }
        if (beatVisualToggleLabel != null)
        {
            beatVisualToggleLabel.text = visible ? "Beat: ON" : "Beat: OFF";
        }
        if (visible)
        {
            ApplyBeatLaneHighlight(lastBeatPosition, lastJumpStartBeat, lastBeatsPerPlatform);
        }
    }

    public void ShowFeedback(string label, string detail, Color color)
    {
        feedbackText.text = string.IsNullOrEmpty(detail) ? label : label + "  " + detail;
        feedbackText.color = color;
        feedbackText.transform.localScale = Vector3.one * 1.15f;
    }

    public void ShowMiss(string label, string reason)
    {
        string cause = string.IsNullOrEmpty(label) ? "Miss" : label;
        ShowFeedback("Miss " + cause, reason, new Color(1f, 0.32f, 0.28f));
        if (missesFlashRoutine != null)
        {
            StopCoroutine(missesFlashRoutine);
        }

        missesFlashRoutine = StartCoroutine(FlashMissesRoutine());
        FlashDamage();
    }

    private IEnumerator FlashMissesRoutine()
    {
        Color flashColor = new Color(1f, 0.2f, 0.14f, 1f);
        for (int i = 0; i < 6; i++)
        {
            float t = i < 3 ? (i + 1f) / 3f : (6f - i) / 3f;
            if (missesText != null)
            {
                missesText.color = Color.Lerp(missesTextDefaultColor, flashColor, t);
                missesText.transform.localScale = Vector3.Lerp(missesTextDefaultScale, missesTextDefaultScale * 1.35f, t);
            }
            if (missesIcon != null)
            {
                missesIcon.color = Color.Lerp(missesIconDefaultColor, flashColor, t * 0.75f);
                missesIcon.transform.localScale = Vector3.Lerp(missesIconDefaultScale, missesIconDefaultScale * 1.18f, t);
            }
            yield return new WaitForSecondsRealtime(0.06f);
        }

        if (missesText != null)
        {
            missesText.color = missesTextDefaultColor;
            missesText.transform.localScale = missesTextDefaultScale;
        }
        if (missesIcon != null)
        {
            missesIcon.color = missesIconDefaultColor;
            missesIcon.transform.localScale = missesIconDefaultScale;
        }
        missesFlashRoutine = null;
    }

    public void ShowResult(bool completed, int score, int misses, int bananas, int maxCombo)
    {
        SetGameControlsVisible(false);
        resultOverlay.SetActive(true);
        resultTitleText.text = completed ? "Climbed Up" : "Try Again";
        resultStatsText.text =
            "Score  " + score +
            "\nMisses  " + misses +
            "\nBananas  " + bananas +
            "\nBest Combo  " + maxCombo;
    }

    public void ShowScoreFeedback()
    {
        if (scoreFlashRoutine != null)
        {
            StopCoroutine(scoreFlashRoutine);
        }

        scoreFlashRoutine = StartCoroutine(PulseTransformRoutine(coinsText != null ? coinsText.transform : null, Vector3.one, 1.26f, 0.18f));
    }

    public void ShowJumpFeedback(bool longJump)
    {
        ShowFeedback(longJump ? "Big jump" : "Jump", "Up", new Color(0.27f, 0.95f, 0.54f));
        PulseBeatDots(1.18f);
    }

    public void ShowBananaFeedback()
    {
        ShowFeedback("Banana", "+", new Color(1f, 0.86f, 0.18f));
        StartCoroutine(PulseTransformRoutine(objectiveProgressText != null ? objectiveProgressText.transform : null, Vector3.one, 1.22f, 0.18f));
    }

    public void ShowParrotFeedback()
    {
        ShowFeedback("Parrot", "OK", new Color(0.27f, 0.95f, 0.54f));
        PulseBeatDots(1.16f);
    }

    private void FlashDamage()
    {
        if (damageFlashImage == null)
        {
            return;
        }

        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
        }

        damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        Color flash = new Color(1f, 0.06f, 0.02f, 0.36f);
        for (int i = 0; i < 8; i++)
        {
            float t = i < 2 ? (i + 1f) / 2f : (8f - i) / 6f;
            damageFlashImage.color = Color.Lerp(new Color(1f, 0.06f, 0.02f, 0f), flash, Mathf.Clamp01(t));
            yield return new WaitForSecondsRealtime(0.035f);
        }

        damageFlashImage.color = new Color(1f, 0.06f, 0.02f, 0f);
        damageFlashRoutine = null;
    }

    private void PulseBeatDots(float scale)
    {
        for (int i = 0; i < beatDots.Count; i++)
        {
            if (beatDots[i] != null)
            {
                beatDots[i].transform.localScale = Vector3.one * scale;
            }
        }
    }

    private IEnumerator PulseTransformRoutine(Transform target, Vector3 baseScale, float scale, float seconds)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 original = target.localScale == Vector3.zero ? baseScale : target.localScale;
        target.localScale = original * scale;
        float timer = 0f;
        while (timer < seconds)
        {
            timer += Time.unscaledDeltaTime;
            target.localScale = Vector3.Lerp(target.localScale, original, Mathf.Clamp01(timer / Mathf.Max(0.01f, seconds)));
            yield return null;
        }

        target.localScale = original;
    }

    private void Update()
    {
        if (feedbackText != null)
        {
            feedbackText.transform.localScale = Vector3.Lerp(feedbackText.transform.localScale, Vector3.one, Time.deltaTime * 7f);
        }
    }

    private void CreateHud(Transform parent)
    {
        GameObject top = CreatePanel(parent, "TopHud", new Color(0.02f, 0.08f, 0.13f, 0.78f));
        RectTransform topRect = top.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.5f, 1f);
        topRect.anchorMax = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0f, -62f);
        topRect.sizeDelta = new Vector2(1080f, 92f);

        titleText = CreateText(top.transform, "Title", "Monkey Climb", new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(620f, 42f), 32, FontStyle.Bold, Color.white);
        subtitleText = CreateText(top.transform, "Subtitle", "Jump up", new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(720f, 30f), 20, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        missesText = CreateText(top.transform, "Misses", "Misses 0", new Vector2(0.1f, 0.5f), Vector2.zero, new Vector2(180f, 38f), 22, FontStyle.Bold, Color.white);
        coinsText = CreateText(top.transform, "Coins", "Bananas 0", new Vector2(0.88f, 0.68f), Vector2.zero, new Vector2(170f, 34f), 21, FontStyle.Bold, Color.white);
        comboText = CreateText(top.transform, "Combo", "Combo 0", new Vector2(0.88f, 0.28f), Vector2.zero, new Vector2(190f, 30f), 18, FontStyle.Bold, new Color(0.78f, 0.95f, 1f));

        GameObject objective = CreatePanel(parent, "ObjectivePanel", new Color(0.02f, 0.08f, 0.13f, 0.68f));
        RectTransform objectiveRect = objective.GetComponent<RectTransform>();
        objectiveRect.anchorMin = new Vector2(0f, 0.5f);
        objectiveRect.anchorMax = new Vector2(0f, 0.5f);
        objectiveRect.anchoredPosition = new Vector2(158f, 18f);
        objectiveRect.sizeDelta = new Vector2(290f, 210f);
        objectiveText = CreateText(objective.transform, "Objective", "Climb up", new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(250f, 62f), 20, FontStyle.Bold, Color.white);
        objectiveProgressText = CreateText(objective.transform, "Progress", "o", new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(250f, 44f), 22, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        objectiveRuleText = CreateText(objective.transform, "Rules", "Space", new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(250f, 78f), 17, FontStyle.Normal, new Color(0.78f, 0.95f, 1f));

        GameObject bottom = CreatePanel(parent, "BottomHud", new Color(0.02f, 0.08f, 0.13f, 0.72f));
        RectTransform bottomRect = bottom.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0.5f, 0f);
        bottomRect.anchorMax = new Vector2(0.5f, 0f);
        bottomRect.anchoredPosition = new Vector2(0f, 86f);
        bottomRect.sizeDelta = new Vector2(820f, 130f);

        feedbackText = CreateText(bottom.transform, "Feedback", "Ready", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(620f, 42f), 30, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));

        GameObject lane = CreateRect(bottom.transform, "BeatLane", new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(300f, 44f)).gameObject;
        HorizontalLayoutGroup layout = lane.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14;
        layout.childAlignment = TextAnchor.MiddleCenter;
        beatLaneRoot = lane.transform;
        for (int i = 0; i < 4; i++)
        {
            GameObject dot = CreateRect(beatLaneRoot, "BeatDot_" + i, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(42f, 42f)).gameObject;
            Image image = dot.AddComponent<Image>();
            image.sprite = circleSprite;
            image.color = new Color(0.25f, 0.72f, 1f, 0.55f);
            LayoutElement element = dot.AddComponent<LayoutElement>();
            element.minWidth = 42f;
            element.minHeight = 42f;
            beatDots.Add(image);
        }

        GameObject progress = CreatePanel(bottom.transform, "Progress", new Color(1f, 1f, 1f, 0.18f));
        RectTransform progressRect = progress.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.5f, 0.12f);
        progressRect.anchorMax = new Vector2(0.5f, 0.12f);
        progressRect.sizeDelta = new Vector2(540f, 18f);

        progressFill = CreateRect(progress.transform, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
        progressFill.color = new Color(0.27f, 0.95f, 0.54f, 1f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressText = CreateText(bottom.transform, "ProgressText", "0%", new Vector2(0.84f, 0.12f), Vector2.zero, new Vector2(90f, 26f), 18, FontStyle.Bold, Color.white);
    }

    private void CreateResultOverlay(Transform parent)
    {
        resultOverlay = CreateRect(parent, "VerticalRunnerResult", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image shade = resultOverlay.AddComponent<Image>();
        shade.color = new Color(0.01f, 0.04f, 0.07f, 0.82f);

        GameObject card = CreatePanel(resultOverlay.transform, "Card", new Color(0.04f, 0.28f, 0.38f, 0.98f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(620f, 470f);

        resultTitleText = CreateText(card.transform, "Title", "Climbed Up", new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(540f, 58f), 40, FontStyle.Bold, Color.white);
        resultStatsText = CreateText(card.transform, "Stats", "", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(500f, 220f), 24, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        CreateButton(card.transform, "Retry", "Retry", new Vector2(0.35f, 0.15f), new Vector2(0f, 0f), new Vector2(160f, 54f), delegate
        {
            if (manager != null)
            {
                manager.RestartCurrentRun();
            }
        });
        CreateButton(card.transform, "Back", "Back", new Vector2(0.65f, 0.15f), new Vector2(0f, 0f), new Vector2(160f, 54f), delegate { SceneTransitionManager.LoadScene("Start"); });
    }

    private void CreateTutorialBriefingOverlay(Transform parent)
    {
        tutorialBriefingOverlay = CreateRect(parent, "TutorialBriefingOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image shade = tutorialBriefingOverlay.AddComponent<Image>();
        shade.color = new Color(0.01f, 0.04f, 0.07f, 0.86f);

        GameObject card = CreatePanel(tutorialBriefingOverlay.transform, "Card", new Color(0.04f, 0.28f, 0.38f, 0.98f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(760f, 500f);

        CreateText(card.transform, "Title", "Monkey Climb", new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(660f, 58f), 42, FontStyle.Bold, Color.white);
        CreateText(card.transform, "Body",
            "Jump up.\n" +
            "Grab banana.\n" +
            "Avoid parrot.\n\n" +
            "Space every 2 beats.\n" +
            "Down/S between jumps.\n" +
            "Space + Left/Right between jumps.",
            new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(650f, 260f), 27, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        CreateButton(card.transform, "StartTutorialButton", "Start", new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(240f, 62f), delegate
        {
            if (manager != null)
            {
                manager.BeginTutorialAfterBriefing();
            }
        });
    }

    private void CreateGameRulesOverlay(Transform parent)
    {
        gameRulesOverlay = CreateRect(parent, "GameRulesOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image shade = gameRulesOverlay.AddComponent<Image>();
        shade.color = new Color(0.01f, 0.04f, 0.07f, 0.84f);

        GameObject card = CreatePanel(gameRulesOverlay.transform, "Card", new Color(0.05f, 0.31f, 0.42f, 0.98f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(760f, 500f);

        CreateText(card.transform, "Title", "Climb Up", new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(660f, 58f), 42, FontStyle.Bold, Color.white);
        CreateText(card.transform, "Body", "", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(650f, 260f), 27, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        CreateButton(card.transform, "StartGameButton", "Start", new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(220f, 62f), delegate
        {
            if (manager != null)
            {
                manager.ContinueAfterGameRules();
            }
        });
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = CreatePanel(parent, name, new Color(1f, 1f, 1f, 0.92f));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Button button = obj.AddComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        button.onClick.AddListener(onClick);
        Text text = CreateText(obj.transform, "Text", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 22, FontStyle.Bold, new Color(0.02f, 0.12f, 0.18f));
        text.raycastTarget = false;
        return button;
    }

    private GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return obj;
    }

    private Text CreateText(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        GameObject obj = CreateRect(parent, name, anchor, anchor, position, size).gameObject;
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
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
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
}
