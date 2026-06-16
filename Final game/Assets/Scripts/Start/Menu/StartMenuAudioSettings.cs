using UnityEngine;
using UnityEngine.UI;

// EN: Shared Start menu audio prefs read by Start, runners, and Ocean.
// ZH: Start 菜单音量与节拍提示开关，各场景读取。
public static class StartMenuAudioSettings
{
    public const string MasterVolumeKey = "StartMenu_MasterVolume";
    public const string MusicVolumeKey = "StartMenu_MusicVolume";
    public const string BeatAssistKey = "StartMenu_BeatAssist";

    public static float MasterVolume => PlayerPrefs.GetFloat(MasterVolumeKey, 0.85f);

    public static float MusicVolume => PlayerPrefs.GetFloat(MusicVolumeKey, 0.85f);

    public static bool BeatPromptsEnabled => PlayerPrefs.GetInt(BeatAssistKey, 1) == 1;

    public static void ApplyMasterVolume()
    {
        AudioListener.volume = MasterVolume;
    }

    public static void ApplyMusicVolume(AudioSource source, float sceneDefaultVolume = 0.85f)
    {
        if (source == null)
        {
            return;
        }

        source.volume = sceneDefaultVolume * MusicVolume;
    }

    /// <summary>
    /// Repairs hierarchy-owned settings sliders when Fill/Handle refs are missing from the scene YAML.
    /// </summary>
    public static void ConfigureSettingsSlider(Slider slider, float defaultValue = 0.85f)
    {
        if (slider == null)
        {
            return;
        }

        Transform sliderTransform = slider.transform;
        RectTransform sliderRect = sliderTransform as RectTransform;
        if (sliderRect != null)
        {
            sliderRect.anchorMin = new Vector2(0.35f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.95f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.sizeDelta = new Vector2(0f, 40f);
        }

        Transform fillArea = sliderTransform.Find("FillArea");
        Transform fill = fillArea != null ? fillArea.Find("Fill") : null;
        Transform handle = sliderTransform.Find("Handle");

        if (fillArea != null)
        {
            RectTransform fillAreaRect = fillArea as RectTransform;
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.anchoredPosition = Vector2.zero;
            fillAreaRect.sizeDelta = Vector2.zero;
        }

        if (fill != null)
        {
            RectTransform fillRect = fill as RectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
            slider.fillRect = fillRect;

            Image fillImage = fill.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.raycastTarget = false;
            }
        }

        if (handle != null)
        {
            RectTransform handleRect = handle as RectTransform;
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            if (handleRect.sizeDelta.x < 8f || handleRect.sizeDelta.y < 8f)
            {
                handleRect.sizeDelta = new Vector2(32f, 48f);
            }

            slider.handleRect = handleRect;
            Image handleImage = handle.GetComponent<Image>();
            if (handleImage != null)
            {
                handleImage.raycastTarget = true;
                slider.targetGraphic = handleImage;
            }
        }

        Image backgroundImage = slider.GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = true;
            if (slider.targetGraphic == null)
            {
                slider.targetGraphic = backgroundImage;
            }
        }

        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = true;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        if (slider.value <= 0f)
        {
            slider.SetValueWithoutNotify(defaultValue);
        }
    }

    public static void PrepareSettingsContent(Transform content)
    {
        if (content == null)
        {
            return;
        }

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        float[] rowOffsets = { 44f, -6f, -58f };
        int rowIndex = 0;
        for (int i = 0; i < content.childCount; i++)
        {
            Transform row = content.GetChild(i);
            if (!row.name.EndsWith("Row"))
            {
                continue;
            }

            RectTransform rowRect = row as RectTransform;
            if (rowRect != null)
            {
                rowRect.anchorMin = new Vector2(0.5f, 0.5f);
                rowRect.anchorMax = new Vector2(0.5f, 0.5f);
                rowRect.pivot = new Vector2(0.5f, 0.5f);
                rowRect.sizeDelta = new Vector2(760f, 66f);
                float y = rowIndex < rowOffsets.Length ? rowOffsets[rowIndex] : 0f;
                rowRect.anchoredPosition = new Vector2(0f, y);
            }

            LayoutElement layoutElement = row.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = row.gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.minHeight = 66f;
            layoutElement.preferredHeight = 66f;
            layoutElement.ignoreLayout = true;

            Transform label = row.Find("Label");
            if (label is RectTransform labelRect)
            {
                labelRect.anchorMin = new Vector2(0.05f, 0.5f);
                labelRect.anchorMax = new Vector2(0.32f, 0.5f);
                labelRect.pivot = new Vector2(0f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.sizeDelta = new Vector2(260f, 56f);
                Text labelText = label.GetComponent<Text>();
                if (labelText != null)
                {
                    labelText.raycastTarget = false;
                }
            }

            Slider slider = row.Find("Slider")?.GetComponent<Slider>();
            if (slider != null)
            {
                ConfigureSettingsSlider(slider);
            }

            rowIndex++;
        }

        RectTransform contentRect = content as RectTransform;
        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }
}
