using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VerticalRunnerUI : MonoBehaviour
{
    private VerticalRunnerManager manager;
    private Canvas canvas;
    private Font font;
    private Text titleText;
    private Text subtitleText;
    private Text feedbackText;
    private Text objectiveText;
    private Text objectiveProgressText;
    private Text objectiveRuleText;
    private Text heartsText;
    private Text coinsText;
    private Text comboText;
    private Text progressText;
    private Image progressFill;
    private Transform beatLaneRoot;
    private GameObject resultOverlay;
    private GameObject tutorialBriefingOverlay;
    private GameObject gameRulesOverlay;
    private Text resultTitleText;
    private Text resultStatsText;
    private readonly List<Image> beatDots = new List<Image>();
    private Sprite circleSprite;

    public void Build(VerticalRunnerManager manager, Sprite circleSprite)
    {
        this.manager = manager;
        this.circleSprite = circleSprite;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        EnsureEventSystem();
        GameObject existing = GameObject.Find("VerticalRunnerCanvas");
        if (existing != null)
        {
            Destroy(existing);
        }

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
    }

    public void ShowTutorialBriefing()
    {
        tutorialBriefingOverlay.SetActive(true);
    }

    public void HideTutorialBriefing()
    {
        tutorialBriefingOverlay.SetActive(false);
    }

    public void ShowGameRules(bool fromTutorial)
    {
        gameRulesOverlay.SetActive(true);
        Transform card = gameRulesOverlay.transform.Find("Card");
        Text title = card.Find("Title").GetComponent<Text>();
        Text body = card.Find("Body").GetComponent<Text>();
        title.text = fromTutorial ? "You are ready for the real run" : "Game Rules";
        body.text =
            "Goal: climb to the finish.\n" +
            "Win: reach the end or survive the song.\n" +
            "Lose: hearts reach 0.\n\n" +
            "Space: jump to the next mushroom on yellow.\n" +
            "Left/Right or A/D: choose SAFE at DANGER forks.\n" +
            "Down/S: catch a nearby coin on yellow.\n" +
            "Perfect and Good jumps build combo.";
    }

    public void HideGameRules()
    {
        gameRulesOverlay.SetActive(false);
    }

    public void ShowTutorialStep(string title, string instruction, string objective, string successRule, string failureRule, int current, int required, int index, int count)
    {
        titleText.text = "Tutorial  " + index + " / " + count + "  " + title;
        subtitleText.text = instruction;
        feedbackText.text = title.Contains("Coin") ? "Down/S on yellow near coin" : title.Contains("Obstacle") ? "Left/Right on yellow" : "Press Space on yellow";
        feedbackText.color = new Color(1f, 0.94f, 0.68f);
        UpdateTutorialObjective(objective, successRule, failureRule, current, required);
    }

    public void ShowGameIntro()
    {
        titleText.text = "Vertical Rhythm Runner";
        subtitleText.text = "Space on the yellow beat. Climb the mushrooms.";
        feedbackText.text = "Ready";
        feedbackText.color = new Color(1f, 0.94f, 0.68f);
        objectiveText.text = "Goal: climb, collect, avoid red obstacles";
        objectiveProgressText.text = "Reach the finish";
        objectiveRuleText.text = "Space jumps. Left/Right dodges. Down/S catches coins.";
    }

    public void UpdateTutorialObjective(string objective, string successRule, string failureRule, int current, int required)
    {
        objectiveText.text = "Goal: " + objective;
        objectiveProgressText.text = "Progress: " + current + " / " + required;
        objectiveRuleText.text = "Success: " + successRule + "\nWatch out: " + failureRule;
    }

    public void UpdateStats(int hearts, int coins, int combo, int maxCombo, float progress01)
    {
        heartsText.text = "Hearts " + hearts;
        coinsText.text = "Coins " + coins;
        comboText.text = "Combo " + combo + "  Best " + maxCombo;
        progressFill.fillAmount = Mathf.Clamp01(progress01);
        progressText.text = Mathf.RoundToInt(progress01 * 100f) + "%";
    }

    public void UpdateBeatLane(float beatPosition)
    {
        int beatInBar = Mathf.FloorToInt(beatPosition) % 4;
        float frac = beatPosition - Mathf.Floor(beatPosition);
        for (int i = 0; i < beatDots.Count; i++)
        {
            bool active = i == beatInBar;
            beatDots[i].color = active ? new Color(1f, 0.86f, 0.18f, 1f) : new Color(0.25f, 0.72f, 1f, 0.55f);
            beatDots[i].transform.localScale = active ? Vector3.one * Mathf.Lerp(1.2f, 0.92f, frac) : Vector3.one;
        }
    }

    public void ShowFeedback(string label, string detail, Color color)
    {
        feedbackText.text = string.IsNullOrEmpty(detail) ? label : label + "  " + detail;
        feedbackText.color = color;
        feedbackText.transform.localScale = Vector3.one * 1.15f;
    }

    public void ShowResult(bool completed, int coins, int perfect, int good, int miss, int maxCombo)
    {
        resultOverlay.SetActive(true);
        resultTitleText.text = completed ? "Run Complete" : "Try Again";
        int total = Mathf.Max(1, perfect + good + miss);
        int accuracy = Mathf.RoundToInt(((perfect + good) / (float)total) * 100f);
        string grade = accuracy >= 90 ? "A" : accuracy >= 75 ? "B" : accuracy >= 60 ? "C" : "Keep Trying";
        resultStatsText.text =
            "Grade: " + grade +
            "\nAccuracy: " + accuracy + "%" +
            "\nPerfect: " + perfect +
            "\nGood: " + good +
            "\nMiss: " + miss +
            "\nCoins: " + coins +
            "\nMax Combo: " + maxCombo;
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

        titleText = CreateText(top.transform, "Title", "Vertical Rhythm Runner", new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(620f, 42f), 32, FontStyle.Bold, Color.white);
        subtitleText = CreateText(top.transform, "Subtitle", "Space on yellow", new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(720f, 30f), 20, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        heartsText = CreateText(top.transform, "Hearts", "Hearts 3", new Vector2(0.1f, 0.5f), Vector2.zero, new Vector2(180f, 38f), 22, FontStyle.Bold, Color.white);
        coinsText = CreateText(top.transform, "Coins", "Coins 0", new Vector2(0.88f, 0.68f), Vector2.zero, new Vector2(170f, 34f), 21, FontStyle.Bold, Color.white);
        comboText = CreateText(top.transform, "Combo", "Combo 0", new Vector2(0.88f, 0.28f), Vector2.zero, new Vector2(190f, 30f), 18, FontStyle.Bold, new Color(0.78f, 0.95f, 1f));

        GameObject objective = CreatePanel(parent, "ObjectivePanel", new Color(0.02f, 0.08f, 0.13f, 0.68f));
        RectTransform objectiveRect = objective.GetComponent<RectTransform>();
        objectiveRect.anchorMin = new Vector2(0f, 0.5f);
        objectiveRect.anchorMax = new Vector2(0f, 0.5f);
        objectiveRect.anchoredPosition = new Vector2(158f, 18f);
        objectiveRect.sizeDelta = new Vector2(290f, 210f);
        objectiveText = CreateText(objective.transform, "Objective", "Goal", new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(250f, 62f), 20, FontStyle.Bold, Color.white);
        objectiveProgressText = CreateText(objective.transform, "Progress", "Progress", new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(250f, 44f), 22, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        objectiveRuleText = CreateText(objective.transform, "Rules", "Success", new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(250f, 78f), 17, FontStyle.Normal, new Color(0.78f, 0.95f, 1f));

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

        resultTitleText = CreateText(card.transform, "Title", "Run Complete", new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(540f, 58f), 40, FontStyle.Bold, Color.white);
        resultStatsText = CreateText(card.transform, "Stats", "", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(500f, 220f), 24, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        CreateButton(card.transform, "Retry", "Retry", new Vector2(0.35f, 0.15f), new Vector2(0f, 0f), new Vector2(160f, 54f), delegate { SceneManager.LoadScene(SceneManager.GetActiveScene().name); });
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

        CreateText(card.transform, "Title", "This is Tutorial", new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(660f, 58f), 42, FontStyle.Bold, Color.white);
        CreateText(card.transform, "Body",
            "You will learn 6 skills before the real run.\n\n" +
            "Finish all 6 lessons to unlock Game.\n" +
            "Mistakes restart only this lesson.\n" +
            "You do not need to press every beat.\n\n" +
            "Space: jump when the beat is yellow.\n" +
            "Left/Right or A/D: choose SAFE at DANGER.\n" +
            "Down/S: catch a nearby coin on yellow.\n" +
            "Blue means wait. Yellow means jump.",
            new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(650f, 260f), 27, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        CreateButton(card.transform, "StartTutorialButton", "Start Tutorial", new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(240f, 62f), delegate
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

        CreateText(card.transform, "Title", "Game Rules", new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(660f, 58f), 42, FontStyle.Bold, Color.white);
        CreateText(card.transform, "Body", "", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(650f, 260f), 27, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        CreateButton(card.transform, "StartGameButton", "Start Game", new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(220f, 62f), delegate
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
