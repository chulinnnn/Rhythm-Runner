using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-150)]
public class StartMenuController : MonoBehaviour
{
    private const string MasterVolumeKey = "StartMenu_MasterVolume";
    private const string BeatAssistKey = "StartMenu_BeatAssist";
    private const string VisualAssistKey = "StartMenu_VisualAssist";

    private static bool registered;

    [Header("Scene routes")]
    public string littleRhythmSceneName = "OceanRhythm";
    public string runnerSceneName = "Tutorial";
    public string advancedRunnerSceneName = "AdvancedTutorial";

    private Canvas canvas;
    private GameObject aboutPanel;
    private GameObject settingsPanel;
    private GameObject recordsPanel;
    private Font uiFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (registered)
        {
            return;
        }

        registered = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    private static void EnsureForScene(Scene scene)
    {
        if (scene.name != "Start")
        {
            return;
        }

        if (FindObjectOfType<StartMenuController>() != null)
        {
            return;
        }

        GameObject obj = new GameObject("StartMenuController");
        obj.AddComponent<StartMenuController>();
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "Start")
        {
            return;
        }

        ApplySavedSettings();
        BuildMenu();
    }

    private void BuildMenu()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        EnsureEventSystem();
        HideLegacyStartCanvases();

        GameObject existing = GameObject.Find("StartMenuCanvas");
        if (existing != null)
        {
            Destroy(existing);
        }

        GameObject canvasObj = new GameObject("StartMenuCanvas", typeof(RectTransform));
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject root = CreateRect(canvasObj.transform, "Root", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.34f, 0.52f, 1f);

        CreateWaterBands(root.transform);
        CreateHeader(root.transform);
        CreateModeButtons(root.transform);
        CreateUtilityBar(root.transform);
        aboutPanel = CreateAboutPanel(root.transform);
        settingsPanel = CreateSettingsPanel(root.transform);
        recordsPanel = CreateRecordsPanel(root.transform);

        aboutPanel.SetActive(false);
        settingsPanel.SetActive(false);
        recordsPanel.SetActive(false);
    }

    private void HideLegacyStartCanvases()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas legacy = canvases[i];
            if (legacy == null || legacy.gameObject.scene.name != "Start")
            {
                continue;
            }

            string canvasName = legacy.gameObject.name;
            if (canvasName == "StartMenuCanvas" || canvasName == "LeaderboardCanvas" || canvasName == "SceneTransitionCanvas")
            {
                continue;
            }

            legacy.gameObject.SetActive(false);
        }
    }

    private void CreateWaterBands(Transform parent)
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject band = CreateRect(parent, "WaterBand_" + i, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 54f + i * 82f), new Vector2(1360f, 72f)).gameObject;
            Image image = band.AddComponent<Image>();
            image.color = i % 2 == 0
                ? new Color(0.2f, 0.77f, 0.91f, 0.22f)
                : new Color(0.0f, 0.18f, 0.32f, 0.18f);
        }
    }

    private void CreateHeader(Transform parent)
    {
        Text title = CreateText(parent, "Title", "Beat Bunny", new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(840f, 82f), 58, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        title.resizeTextForBestFit = true;
        title.resizeTextMinSize = 32;
        title.resizeTextMaxSize = 58;

        CreateText(parent, "Subtitle", "Choose a rhythm path", new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(840f, 40f), 26, FontStyle.Normal, new Color(0.95f, 1f, 0.82f), TextAnchor.MiddleCenter);
    }

    private void CreateModeButtons(Transform parent)
    {
        GameObject row = CreateRect(parent, "ModeRow", new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(1040f, 290f)).gameObject;
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 22;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        CreateModeCard(row.transform, "Little Rhythm Ocean", "Under 5", "Move the net. Tap with the bright bubble.", new Color(0.12f, 0.68f, 0.85f), delegate { LoadScene(littleRhythmSceneName); });
        CreateModeCard(row.transform, "Rhythm Runner", "Age 5-10", "Learn the beat, then run the level.", new Color(1f, 0.64f, 0.2f), delegate { LoadScene(runnerSceneName); });
        CreateModeCard(row.transform, "Advanced Runner", "Challenge", "A faster route for practiced players.", new Color(0.42f, 0.78f, 0.34f), delegate { LoadScene(advancedRunnerSceneName); });
    }

    private void CreateModeCard(Transform parent, string title, string label, string body, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject card = CreatePanel(parent, title.Replace(" ", "") + "Card", new Color(1f, 1f, 1f, 0.94f));
        LayoutElement layout = card.AddComponent<LayoutElement>();
        layout.minWidth = 300f;
        layout.flexibleWidth = 1f;
        layout.minHeight = 270f;

        VerticalLayoutGroup group = card.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(18, 18, 18, 18);
        group.spacing = 10;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        GameObject badge = CreatePanel(card.transform, "Badge", color);
        LayoutElement badgeLayout = badge.AddComponent<LayoutElement>();
        badgeLayout.minHeight = 64f;

        Text badgeText = CreateText(badge.transform, "Text", label, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 60f), 28, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        badgeText.font = uiFont;
        badgeText.text = label;
        badgeText.fontSize = 28;
        badgeText.fontStyle = FontStyle.Bold;
        badgeText.alignment = TextAnchor.MiddleCenter;
        badgeText.color = Color.white;
        badgeText.raycastTarget = false;

        CreateFlowText(card.transform, "Title", title, 30, FontStyle.Bold, new Color(0.05f, 0.12f, 0.18f), TextAnchor.MiddleCenter, 48f);
        CreateFlowText(card.transform, "Body", body, 21, FontStyle.Normal, new Color(0.16f, 0.23f, 0.28f), TextAnchor.MiddleCenter, 72f);

        Button button = card.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();
        button.onClick.AddListener(onClick);
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.9f, 0.96f, 1f, 1f);
        button.colors = colors;
    }

    private void CreateUtilityBar(Transform parent)
    {
        GameObject bar = CreateRect(parent, "UtilityBar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(760f, 58f)).gameObject;
        HorizontalLayoutGroup layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;

        CreateSmallButton(bar.transform, "About", delegate { ShowOnly(aboutPanel); });
        CreateSmallButton(bar.transform, "Settings", delegate { ShowOnly(settingsPanel); });
        CreateSmallButton(bar.transform, "Records", delegate { RefreshRecords(); ShowOnly(recordsPanel); });
        CreateSmallButton(bar.transform, "Exit", QuitGame);
    }

    private GameObject CreateAboutPanel(Transform parent)
    {
        GameObject panel = CreateOverlay(parent, "AboutPanel", "About");
        CreateText(panel.transform, "Body",
            "Beat Bunny helps children feel rhythm through play.\n\nLittle Rhythm Ocean is for children under 5. It opens straight into play: help the child click a fish, then click TAP or press Space when the bright bubble lights up. There is no losing and no lesson screen to read.\n\nRhythm Runner is for children 5-10: learn the beat in Tutorial, then use rhythm to survive the run.",
            new Vector2(0.5f, 0.52f), new Vector2(0f, 0f), new Vector2(760f, 300f), 27, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);
        return panel;
    }

    private GameObject CreateSettingsPanel(Transform parent)
    {
        GameObject panel = CreateOverlay(parent, "SettingsPanel", "Settings");
        GameObject content = CreateRect(panel.transform, "SettingsContent", new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(760f, 330f)).gameObject;
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 18;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateSliderSetting(content.transform, "Master Volume", MasterVolumeKey, PlayerPrefs.GetFloat(MasterVolumeKey, 0.85f), delegate(float value)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
            AudioListener.volume = value;
            PlayerPrefs.Save();
        });

        CreateToggleSetting(content.transform, "Beat Visual Assist", BeatAssistKey, PlayerPrefs.GetInt(BeatAssistKey, 1) == 1);
        CreateSliderSetting(content.transform, "Visual Assist Strength", VisualAssistKey, PlayerPrefs.GetFloat(VisualAssistKey, 0.85f), delegate(float value)
        {
            PlayerPrefs.SetFloat(VisualAssistKey, value);
            PlayerPrefs.Save();
        });

        return panel;
    }

    private GameObject CreateRecordsPanel(Transform parent)
    {
        GameObject panel = CreateOverlay(parent, "RecordsPanel", "Records");
        CreateRect(panel.transform, "RecordsContent", new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(760f, 330f));
        return panel;
    }

    private void RefreshRecords()
    {
        if (recordsPanel == null)
        {
            return;
        }

        Transform content = recordsPanel.transform.Find("RecordsContent");
        if (content == null)
        {
            return;
        }

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        VerticalLayoutGroup layout = content.gameObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateRecordSection(content, "Rhythm Runner", LeaderboardManager.GetScores(LeaderboardMode.Easy));
        CreateRecordSection(content, "Advanced Runner", LeaderboardManager.GetScores(LeaderboardMode.Hard));
    }

    private void CreateRecordSection(Transform parent, string title, List<int> scores)
    {
        CreateFlowText(parent, title + "Title", title, 27, FontStyle.Bold, new Color(1f, 0.9f, 0.52f), TextAnchor.MiddleLeft, 38f);
        if (scores.Count == 0)
        {
            CreateFlowText(parent, title + "Empty", "No record yet", 23, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft, 34f);
            return;
        }

        for (int i = 0; i < scores.Count; i++)
        {
            CreateFlowText(parent, title + "Row" + i, (i + 1) + ". " + scores[i] + " m", 23, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft, 32f);
        }
    }

    private GameObject CreateOverlay(Transform parent, string name, string title)
    {
        GameObject overlay = CreateRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image shade = overlay.AddComponent<Image>();
        shade.color = new Color(0.01f, 0.05f, 0.08f, 0.82f);

        GameObject card = CreatePanel(overlay.transform, "Card", new Color(0.05f, 0.29f, 0.42f, 0.98f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(900f, 520f);

        CreateText(card.transform, "Title", title, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(700f, 60f), 42, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        CreateSmallButton(card.transform, "Close", delegate { overlay.SetActive(false); }, new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(190f, 50f));
        return overlay;
    }

    private void CreateSliderSetting(Transform parent, string label, string key, float initialValue, UnityEngine.Events.UnityAction<float> onChanged)
    {
        GameObject row = CreateRect(parent, label.Replace(" ", "") + "Row", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(760f, 66f)).gameObject;
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        CreateFlowText(row.transform, "Label", label, 24, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 56f, 260f);

        GameObject sliderObj = CreateRect(row.transform, "Slider", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(420f, 40f)).gameObject;
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(initialValue);
        slider.onValueChanged.AddListener(onChanged);

        Image bg = sliderObj.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.18f);
        slider.targetGraphic = bg;

        RectTransform fillArea = CreateRect(sliderObj.transform, "FillArea", Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        RectTransform fill = CreateRect(fillArea, "Fill", Vector2.zero, new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = new Color(1f, 0.75f, 0.2f, 1f);
        slider.fillRect = fill;

        RectTransform handle = CreateRect(sliderObj.transform, "Handle", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(32f, 48f));
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = Color.white;
        slider.handleRect = handle;

        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetFloat(key, initialValue);
        }
    }

    private void CreateToggleSetting(Transform parent, string label, string key, bool initialValue)
    {
        GameObject row = CreateRect(parent, label.Replace(" ", "") + "Row", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(760f, 62f)).gameObject;
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        CreateFlowText(row.transform, "Label", label, 24, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 56f, 520f);

        GameObject toggleObj = CreatePanel(row.transform, "Toggle", initialValue ? new Color(0.32f, 0.82f, 0.38f) : new Color(0.55f, 0.58f, 0.62f));
        LayoutElement layoutElement = toggleObj.AddComponent<LayoutElement>();
        layoutElement.minWidth = 92f;
        layoutElement.minHeight = 46f;

        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.isOn = initialValue;
        toggle.onValueChanged.AddListener(delegate(bool enabled)
        {
            PlayerPrefs.SetInt(key, enabled ? 1 : 0);
            PlayerPrefs.Save();
            toggleObj.GetComponent<Image>().color = enabled ? new Color(0.32f, 0.82f, 0.38f) : new Color(0.55f, 0.58f, 0.62f);
        });

        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetInt(key, initialValue ? 1 : 0);
        }
    }

    private void ShowOnly(GameObject panel)
    {
        if (aboutPanel != null)
        {
            aboutPanel.SetActive(false);
        }
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        if (recordsPanel != null)
        {
            recordsPanel.SetActive(false);
        }
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        SceneTransitionManager.LoadScene(sceneName);
    }

    private void ApplySavedSettings()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(MasterVolumeKey, 0.85f);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CreateSmallButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        CreateSmallButton(parent, label, onClick, Vector2.zero, Vector2.zero, new Vector2(170f, 50f));
    }

    private void CreateSmallButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject obj = CreatePanel(parent, label + "Button", new Color(1f, 1f, 1f, 0.92f));
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (anchor != Vector2.zero || position != Vector2.zero)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.minWidth = size.x;
        layout.minHeight = size.y;

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        button.onClick.AddListener(onClick);

        Text text = CreateText(obj.transform, "Text", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 22, FontStyle.Bold, new Color(0.05f, 0.12f, 0.18f), TextAnchor.MiddleCenter);
        text.raycastTarget = false;
    }

    private void CreateFlowText(Transform parent, string name, string text, int size, FontStyle style, Color color, TextAnchor anchor, float height, float width = 0f)
    {
        GameObject obj = CreateRect(parent, name, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(width, height)).gameObject;
        Text label = obj.AddComponent<Text>();
        label.font = uiFont;
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = anchor;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 14;
        label.resizeTextMaxSize = size;

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.minHeight = height;
        if (width > 0f)
        {
            layout.minWidth = width;
        }
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
        return text;
    }

    private GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return obj;
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

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
