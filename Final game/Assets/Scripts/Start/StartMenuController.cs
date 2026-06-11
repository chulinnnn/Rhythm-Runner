using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-150)]
public class StartMenuController : MonoBehaviour
{
    private const string MasterVolumeKey = "StartMenu_MasterVolume";

    private static bool registered;

    [Header("Scene routes")]
    public string littleRhythmSceneName = "OceanRhythm";
    public string runnerSceneName = "VerticalRunner";
    public string advancedRunnerSceneName = "AdvancedRunner";
    public string worldMusicSceneName = "WorldMusicExplorer";

    [Header("Runtime scene policy")]
    public RuntimeScenePolicy scenePolicy = RuntimeScenePolicy.Defaults();

    private Canvas canvas;
    private GameObject aboutPanel;
    private GameObject settingsPanel;
    private GameObject recordsPanel;

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
        EnsureEventSystem();
        if (!scenePolicy.useExistingSceneObjects || scenePolicy.rebuildUiOnPlay)
        {
            HideLegacyStartCanvases();
        }

        GameObject existing = GameObject.Find("StartMenuCanvas");
        if (existing != null && scenePolicy.useExistingSceneObjects && !scenePolicy.rebuildUiOnPlay)
        {
            if (BindExistingMenu(existing))
            {
                return;
            }

            Debug.LogWarning("StartMenuController: Existing StartMenuCanvas is missing required children. Keeping it untouched.");
            return;
        }

        if (existing != null && scenePolicy.rebuildUiOnPlay)
        {
            DestroyObject(existing);
        }
        else if (existing != null)
        {
            return;
        }

        Debug.LogWarning("StartMenuController: StartMenuCanvas is missing. Add it to the scene hierarchy before play.");
        return;

    }

    private bool BindExistingMenu(GameObject existing)
    {
        canvas = existing.GetComponent<Canvas>();
        if (canvas == null)
        {
            return false;
        }

        aboutPanel = FindChild(existing.transform, "Root/AboutPanel");
        settingsPanel = FindChild(existing.transform, "Root/SettingsPanel");
        recordsPanel = FindChild(existing.transform, "Root/RecordsPanel");
        if (aboutPanel == null || settingsPanel == null || recordsPanel == null)
        {
            return false;
        }

        BindButton(existing.transform, "Root/ModeRow/LittleRhythmOceanCard", delegate { LoadScene(littleRhythmSceneName); });
        BindButton(existing.transform, "Root/ModeRow/RhythmRunnerCard", delegate { LoadScene(runnerSceneName); });
        BindButton(existing.transform, "Root/ModeRow/AdvancedRunnerCard", delegate { LoadScene(advancedRunnerSceneName); });
        BindButton(existing.transform, "Root/ModeRow/WorldMusicExplorerCard", delegate { LoadScene(worldMusicSceneName); });
        BindButton(existing.transform, "Root/UtilityBar/AboutButton", delegate { ShowOnly(aboutPanel); });
        BindButton(existing.transform, "Root/UtilityBar/SettingsButton", delegate { ShowOnly(settingsPanel); });
        BindButton(existing.transform, "Root/UtilityBar/RecordsButton", delegate { RefreshRecords(); ShowOnly(recordsPanel); });
        BindButton(existing.transform, "Root/UtilityBar/ExitButton", QuitGame);
        BindButton(existing.transform, "Root/AboutPanel/Card/CloseButton", delegate { aboutPanel.SetActive(false); });
        BindButton(existing.transform, "Root/SettingsPanel/Card/CloseButton", delegate { settingsPanel.SetActive(false); });
        BindButton(existing.transform, "Root/RecordsPanel/Card/CloseButton", delegate { recordsPanel.SetActive(false); });
        BindSlider(existing.transform, "Root/SettingsPanel/Card/SettingsContent/MasterVolumeRow/Slider", MasterVolumeKey, delegate(float value)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
            AudioListener.volume = value;
            PlayerPrefs.Save();
        });
        EnsureMusicDecorations(existing.transform);

        aboutPanel.SetActive(false);
        settingsPanel.SetActive(false);
        recordsPanel.SetActive(false);
        return true;
    }

    private void EnsureMusicDecorations(Transform menuRoot)
    {
        Transform root = menuRoot.Find("Root");
        if (root == null)
        {
            return;
        }

        Transform music = root.Find("music");
        if (music == null)
        {
            if (!scenePolicy.autoCreateMissingObjects)
            {
                return;
            }

            GameObject obj = new GameObject("music", typeof(RectTransform));
            obj.transform.SetParent(root, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            obj.transform.SetSiblingIndex(0);
            music = obj.transform;
        }

        if (music.GetComponent<StartMenuMusicVisualizer>() == null)
        {
            music.gameObject.AddComponent<StartMenuMusicVisualizer>();
        }

        Transform coverStage = root.Find("MusicCoverStage");
        if (coverStage != null)
        {
            StartMenuCoverStageAnimator animator = coverStage.GetComponent<StartMenuCoverStageAnimator>();
            if (animator == null)
            {
                animator = coverStage.gameObject.AddComponent<StartMenuCoverStageAnimator>();
            }

            CanvasGroup group = coverStage.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = coverStage.gameObject.AddComponent<CanvasGroup>();
            }
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }

    private GameObject FindChild(Transform root, string path)
    {
        Transform child = root.Find(path);
        return child != null ? child.gameObject : null;
    }

    private void BindButton(Transform root, string path, UnityEngine.Events.UnityAction onClick)
    {
        Transform child = root.Find(path);
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

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
    }

    private void BindSlider(Transform root, string path, string key, UnityEngine.Events.UnityAction<float> onChanged)
    {
        Transform child = root.Find(path);
        Slider slider = child != null ? child.GetComponent<Slider>() : null;
        if (slider == null)
        {
            return;
        }

        slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(key, slider.value));
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(onChanged);
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

    private void RefreshRecords()
    {
        if (recordsPanel == null)
        {
            return;
        }

        Transform content = recordsPanel.transform.Find("Card/RecordsContent");
        if (content == null)
        {
            content = recordsPanel.transform.Find("RecordsContent");
        }
        if (content == null)
        {
            return;
        }

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform child = content.GetChild(i);
            if (child.name.StartsWith("RuntimeRecord_"))
            {
                DestroyObject(child.gameObject);
            }
        }

        CreateRecordSection(content, "Rhythm Runner", LeaderboardManager.GetScores(LeaderboardMode.Easy));
        CreateRecordSection(content, "Advanced Runner", LeaderboardManager.GetScores(LeaderboardMode.Hard));
    }

    private void CreateRecordSection(Transform parent, string title, List<int> scores)
    {
        CreateRecordRow(parent, "SectionTemplate", title + "Title", title);
        if (scores.Count == 0)
        {
            CreateRecordRow(parent, "RecordItemTemplate", title + "Empty", "No record yet");
            return;
        }

        for (int i = 0; i < scores.Count; i++)
        {
            CreateRecordRow(parent, "RecordItemTemplate", title + "Row" + i, (i + 1) + ". " + scores[i] + " m");
        }
    }

    private void CreateRecordRow(Transform parent, string templateName, string nameSuffix, string value)
    {
        Transform template = parent.Find(templateName);
        if (template == null)
        {
            Debug.LogWarning("StartMenuController: Missing " + templateName + " under RecordsContent; using text-only fallback.");
            GameObject fallback = new GameObject("RuntimeRecord_" + nameSuffix, typeof(RectTransform));
            fallback.transform.SetParent(parent, false);
            Text fallbackText = fallback.AddComponent<Text>();
            fallbackText.text = value;
            fallbackText.raycastTarget = false;
            return;
        }

        GameObject row = Instantiate(template.gameObject, parent);
        row.name = "RuntimeRecord_" + nameSuffix;
        row.SetActive(true);
        SetRecordText(row.transform, value);
    }

    private void SetRecordText(Transform row, string value)
    {
        Text text = null;
        Transform valueTransform = row.Find("Value");
        if (valueTransform != null)
        {
            text = valueTransform.GetComponent<Text>();
        }
        if (text == null)
        {
            text = row.GetComponentInChildren<Text>(true);
        }
        if (text != null)
        {
            text.text = value;
            text.raycastTarget = false;
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

#if UNITY_EDITOR
    [ContextMenu("Rebuild Edit Mode Hierarchy")]
    public void RebuildEditModeHierarchy()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        ApplySavedSettings();
        BuildMenu();
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void DestroyObject(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
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
