using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AllSceneHierarchyBaker
{
    [MenuItem("Tools/Rhythm Runner/Rebuild All Active Scene Hierarchies")]
    public static void RebuildAllActiveSceneHierarchies()
    {
        string originalScenePath = SceneManager.GetActiveScene().path;

        RebuildStartScene();
        RebuildOceanScene();
        RebuildVerticalScene("Assets/Scenes/VerticalRunner.unity", VerticalRunnerMode.Tutorial);
        RebuildAdvancedScene("Assets/Scenes/AdvancedRunner.unity", AdvancedRunnerMode.Game);
        RebuildWorldMusicExplorerScene();
        EnsureBuildSettingsScene("Assets/Scenes/WorldMusicExplorer.unity");

        if (!string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath);
        }

        AssetDatabase.SaveAssets();
    }

    public static void RebuildForBatchmode()
    {
        RebuildAllActiveSceneHierarchies();
    }

    [MenuItem("Tools/Rhythm Runner/Apply Vertical Icon UI")]
    public static void ApplyVerticalIconUi()
    {
        string originalScenePath = SceneManager.GetActiveScene().path;

        ApplyVerticalIconUiContract("Assets/Scenes/VerticalRunner.unity");

        if (!string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath);
        }

        AssetDatabase.SaveAssets();
    }

    public static void ApplyVerticalIconUiForBatchmode()
    {
        ApplyVerticalIconUi();
    }

    [MenuItem("Tools/Rhythm Runner/Polish Vertical UI Feedback")]
    public static void PolishVerticalUiFeedback()
    {
        string originalScenePath = SceneManager.GetActiveScene().path;

        ApplyVerticalUiPolishContract("Assets/Scenes/VerticalRunner.unity");

        if (!string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath);
        }

        AssetDatabase.SaveAssets();
    }

    public static void PolishVerticalUiFeedbackForBatchmode()
    {
        PolishVerticalUiFeedback();
    }

    [MenuItem("Tools/Rhythm Runner/Apply Advanced Editable UI")]
    public static void ApplyAdvancedEditableUi()
    {
        string originalScenePath = SceneManager.GetActiveScene().path;

        ApplyAdvancedEditableUiContract("Assets/Scenes/AdvancedRunner.unity");

        if (!string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath);
        }

        AssetDatabase.SaveAssets();
    }

    public static void ApplyAdvancedEditableUiForBatchmode()
    {
        ApplyAdvancedEditableUi();
    }

    private static void ApplyAdvancedEditableUiContract(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        AdvancedRunnerManager manager = Object.FindObjectOfType<AdvancedRunnerManager>(true);
        if (manager == null)
        {
            manager = new GameObject("AdvancedRunnerManager").AddComponent<AdvancedRunnerManager>();
        }

        RemoveDuplicateAdvancedManagers(manager);
        manager.mode = AdvancedRunnerMode.Game;
        ConfigureAdvancedManagerDefaults(manager);
        ConfigurePolicy(manager.scenePolicy, "AdvancedRunnerRuntime", true);
        manager.scenePolicy.preserveExistingImageOverrides = true;

        EnsureAdvancedConfigContract(manager);
        EnsureRoot("AdvancedRunnerRuntime");
        EnsureAdvancedCanvas();
        Save(scene);
    }

    private static void ApplyVerticalIconUiContract(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject canvas = GameObject.Find("VerticalRunnerCanvas");
        if (canvas == null)
        {
            Debug.LogWarning("AllSceneHierarchyBaker: VerticalRunnerCanvas not found in " + scenePath + "; skipping vertical icon UI contract.");
            return;
        }

        Transform top = canvas.transform.Find("TopHud");
        if (top != null)
        {
            EnsureHudCounter(top, "Misses", "0", new Vector2(0.1f, 0.5f), new Vector2(180f, 38f));
            EnsureHudCounter(top, "Coins", "0", new Vector2(0.88f, 0.68f), new Vector2(170f, 34f));
            EnsureHudCounter(top, "Combo", "0/0", new Vector2(0.88f, 0.28f), new Vector2(190f, 30f));
        }
        else
        {
            Debug.LogWarning("AllSceneHierarchyBaker: TopHud not found in " + scenePath + "; HUD icon counters were not added.");
        }

        Transform objective = canvas.transform.Find("ObjectivePanel");
        if (objective != null)
        {
            EnsureTutorialImages(objective);
        }
        else
        {
            Debug.LogWarning("AllSceneHierarchyBaker: ObjectivePanel not found in " + scenePath + "; tutorial image slots were not added.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Save(scene);
    }

    private static void ApplyVerticalUiPolishContract(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject canvas = GameObject.Find("VerticalRunnerCanvas");
        if (canvas == null)
        {
            Debug.LogWarning("AllSceneHierarchyBaker: VerticalRunnerCanvas not found in " + scenePath + "; skipping vertical UI polish.");
            return;
        }

        PolishCounter(canvas.transform, "TopHud/Misses");
        PolishCounter(canvas.transform, "TopHud/Coins");
        PolishCounter(canvas.transform, "TopHud/Combo");
        PolishObjectivePanel(canvas.transform);
        PolishBeatToggle(canvas.transform);
        PolishProgressFill(canvas.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        Save(scene);
    }

    private static void PolishCounter(Transform canvas, string path)
    {
        Transform counter = canvas.Find(path);
        if (counter == null)
        {
            return;
        }

        Transform iconTransform = counter.Find("Icon");
        if (iconTransform != null)
        {
            Image icon = iconTransform.GetComponent<Image>();
            if (icon != null)
            {
                Color color = icon.color;
                color.a = 1f;
                icon.color = color;
                icon.raycastTarget = false;
            }
        }

        Transform valueTransform = counter.Find("Value");
        if (valueTransform != null)
        {
            Text value = valueTransform.GetComponent<Text>();
            if (value != null)
            {
                value.raycastTarget = false;
                value.resizeTextForBestFit = true;
                value.horizontalOverflow = HorizontalWrapMode.Overflow;
                value.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }
    }

    private static void PolishObjectivePanel(Transform canvas)
    {
        Transform objective = canvas.Find("ObjectivePanel");
        if (objective == null)
        {
            return;
        }

        SetRect(objective, new Vector2(0f, 0.5f), new Vector2(170f, 18f), new Vector2(330f, 230f));

        Text objectiveText = SetTextRect(objective, "Objective", new Vector2(0.5f, 0.77f), Vector2.zero, new Vector2(286f, 52f), 22, FontStyle.Bold, Color.white);
        Text progressText = SetTextRect(objective, "Progress", new Vector2(0.5f, 0.51f), Vector2.zero, new Vector2(220f, 46f), 30, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        if (progressText != null)
        {
            progressText.text = progressText.text.Replace("Progress: ", "");
        }

        Text rulesText = SetTextRect(objective, "Rules", new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(288f, 92f), 18, FontStyle.Normal, new Color(0.83f, 0.98f, 1f));
        if (rulesText != null)
        {
            rulesText.text = rulesText.text.Replace("Watch out: ", "");
        }

        Transform images = objective.Find("TutorialImages");
        if (images != null)
        {
            SetRect(images, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(286f, 136f));
        }
    }

    private static Text SetTextRect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            return null;
        }

        SetRect(child, anchor, position, size);
        Text text = child.GetComponent<Text>();
        if (text == null)
        {
            return null;
        }

        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(Transform transform, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rect = transform.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void PolishBeatToggle(Transform canvas)
    {
        Transform toggleTransform = canvas.Find("BottomHud/BeatVisualToggleButton");
        if (toggleTransform == null)
        {
            return;
        }

        Button button = toggleTransform.GetComponent<Button>();
        if (button != null)
        {
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }

        Text label = toggleTransform.Find("Text") != null ? toggleTransform.Find("Text").GetComponent<Text>() : null;
        if (label != null)
        {
            label.text = "Beat: ON";
            label.resizeTextForBestFit = true;
            label.raycastTarget = false;
        }
    }

    private static void PolishProgressFill(Transform canvas)
    {
        Transform fillTransform = canvas.Find("BottomHud/Progress/Fill");
        if (fillTransform == null)
        {
            return;
        }

        Image fill = fillTransform.GetComponent<Image>();
        if (fill == null)
        {
            fill = fillTransform.gameObject.AddComponent<Image>();
        }

        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        fill.raycastTarget = false;
    }

    private static void RebuildStartScene()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Start.unity", OpenSceneMode.Single);
        RemoveLegacyBunnyObjects();

        StartMenuController controller = Object.FindObjectOfType<StartMenuController>(true);
        if (controller == null)
        {
            controller = new GameObject("StartMenuController").AddComponent<StartMenuController>();
        }
        controller.littleRhythmSceneName = "OceanRhythm";
        controller.runnerSceneName = "VerticalRunner";
        controller.advancedRunnerSceneName = "AdvancedRunner";
        controller.worldMusicSceneName = "WorldMusicExplorer";
        ConfigurePolicy(controller.scenePolicy, "StartMenuRuntime", false);

        GameObject canvas = EnsureCanvasRoot("StartMenuCanvas", 120);
        GameObject root = EnsureFullscreenRect(canvas.transform, "Root");
        EnsureImage(root, new Color(0.05f, 0.34f, 0.52f, 1f));
        EnsureStartMusicDecorations(root.transform);
        EnsureText(root.transform, "Title", "Beat Bunny", new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(840f, 82f), 58, FontStyle.Bold, Color.white);

        GameObject modeRow = EnsureRect(root.transform, "ModeRow", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(1360f, 290f));
        RemoveHorizontalLayout(modeRow);
        if (EnsureMissingModeCard(modeRow.transform, "LittleRhythmOceanCard", "Rhythm Ocean", "Under 5", "Move the net. Tap with the bright bubble.", new Color(0.12f, 0.68f, 0.85f)))
        {
            SetModeCardDefaultPosition(modeRow.transform, "LittleRhythmOceanCard", new Vector2(-510f, 0f));
        }
        if (EnsureMissingModeCard(modeRow.transform, "RhythmRunnerCard", "Jumping follow the rhythm", "Age 5-10", "Bounce upward with rhythm.", new Color(1f, 0.64f, 0.2f)))
        {
            SetModeCardDefaultPosition(modeRow.transform, "RhythmRunnerCard", new Vector2(-170f, 0f));
        }
        if (EnsureMissingModeCard(modeRow.transform, "AdvancedRunnerCard", "Advanced Runner", "Challenge", "Read lane and action together.", new Color(0.42f, 0.78f, 0.34f)))
        {
            SetModeCardDefaultPosition(modeRow.transform, "AdvancedRunnerCard", new Vector2(170f, 0f));
        }
        if (EnsureMissingModeCard(modeRow.transform, "WorldMusicExplorerCard", "123", "123", "123", new Color(0.78f, 0.42f, 0.88f)))
        {
            SetModeCardDefaultPosition(modeRow.transform, "WorldMusicExplorerCard", new Vector2(510f, 0f));
        }

        GameObject utility = EnsureRect(root.transform, "UtilityBar", new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(760f, 58f));
        HorizontalLayoutGroup utilityLayout = EnsureLayout(utility, 20f, TextAnchor.MiddleCenter);
        utilityLayout.childControlWidth = false;
        utilityLayout.childControlHeight = false;
        EnsureButton(utility.transform, "AboutButton", "About", Vector2.zero, Vector2.zero, new Vector2(170f, 50f));
        EnsureButton(utility.transform, "SettingsButton", "Settings", Vector2.zero, Vector2.zero, new Vector2(170f, 50f));
        EnsureButton(utility.transform, "RecordsButton", "Records", Vector2.zero, Vector2.zero, new Vector2(170f, 50f));
        EnsureButton(utility.transform, "ExitButton", "Exit", Vector2.zero, Vector2.zero, new Vector2(170f, 50f));

        EnsureStartPanel(root.transform, "AboutPanel", "About", "Beat Bunny helps children feel rhythm through play.");
        EnsureSettingsPanel(root.transform);
        EnsureStartPanel(root.transform, "RecordsPanel", "Records", "");
        EnsureCamera();
        EnsureEventSystem();
        Save(scene);
    }

    [MenuItem("Tools/Rhythm Runner/Fix Start Records and Settings Panels")]
    public static void FixStartRecordsAndSettingsPanels()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Start.unity", OpenSceneMode.Single);
        Transform root = GameObject.Find("StartMenuCanvas/Root")?.transform;
        if (root == null)
        {
            Debug.LogWarning("AllSceneHierarchyBaker: StartMenuCanvas/Root not found.");
            return;
        }

        Transform settingsCard = root.Find("SettingsPanel/Card");
        if (settingsCard != null)
        {
            EnsureSettingsPanelContent(settingsCard);
        }

        Transform recordsCard = root.Find("RecordsPanel/Card");
        if (recordsCard != null)
        {
            EnsureRecordsPanelContent(recordsCard);
        }

        Save(scene);
    }

    public static void FixStartRecordsAndSettingsPanelsForBatchmode()
    {
        FixStartRecordsAndSettingsPanels();
    }

    [MenuItem("Tools/Rhythm Runner/Rebuild World Music Explorer Hierarchy")]
    public static void RebuildWorldMusicExplorerScene()
    {
        const string scenePath = "Assets/Scenes/WorldMusicExplorer.unity";
        Scene scene = System.IO.File.Exists(scenePath)
            ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        WorldMusicExplorerController controller = Object.FindObjectOfType<WorldMusicExplorerController>(true);
        if (controller == null)
        {
            controller = new GameObject("WorldMusicExplorerController").AddComponent<WorldMusicExplorerController>();
        }

        controller.explorerSceneName = "WorldMusicExplorer";
        controller.startSceneName = "Start";

        GameObject canvas = EnsureCanvasRoot("WorldMusicExplorerCanvas", 130);
        GameObject root = EnsureFullscreenRect(canvas.transform, "Root");
        EnsureMissingImage(root, new Color(0.07f, 0.08f, 0.15f, 1f));
        EnsureOptionalButton(root.transform, "BackButton", "Back", new Vector2(0f, 1f), new Vector2(72f, -46f), new Vector2(120f, 48f));
        EnsureMissingText(root.transform, "NowPlaying", "123", new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(820f, 86f), 32, FontStyle.Bold, Color.white);
        EnsureMissingText(root.transform, "HintText", "Press any key to explore music.", new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(760f, 48f), 22, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));

        GameObject content = EnsureCanvasRoot("WorldMusicExplorerContent", 125);
        GameObject items = EnsureMissingRect(content.transform, "Items", Vector2.zero, Vector2.zero, Vector2.zero);
        EnsureWorldMusicItem(items.transform, "Item_RegionName");

        EnsureCamera();
        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        EnsureBuildSettingsScene(scenePath);
        AssetDatabase.SaveAssets();
    }

    public static void EnsureStartMusicDecorations(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Transform existing = root.Find("music");
        bool created = existing == null;
        GameObject music = EnsureMissingRect(root, "music", Vector2.zero, Vector2.zero, Vector2.zero);
        if (created)
        {
            RectTransform rect = music.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            music.transform.SetSiblingIndex(0);
        }

        if (music.GetComponent<StartMenuMusicVisualizer>() == null)
        {
            music.AddComponent<StartMenuMusicVisualizer>();
        }

        EnsureMissingRect(music.transform, "Templates", Vector2.zero, Vector2.zero, Vector2.zero);
        EnsureMissingRect(music.transform, "StaffLines", Vector2.zero, Vector2.zero, Vector2.zero);
        EnsureMissingRect(music.transform, "Runtime", Vector2.zero, Vector2.zero, Vector2.zero);
    }

    public static void EnsureStartWorldMusicExplorerEntry(Transform root, StartMenuController controller)
    {
        if (controller != null)
        {
            controller.worldMusicSceneName = "WorldMusicExplorer";
        }

        if (root == null)
        {
            return;
        }

        GameObject modeRow = EnsureMissingRect(root, "ModeRow", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(1360f, 290f));
        RemoveHorizontalLayout(modeRow);
        if (EnsureMissingModeCard(modeRow.transform, "WorldMusicExplorerCard", "123", "123", "123", new Color(0.78f, 0.42f, 0.88f)))
        {
            SetModeCardDefaultPosition(modeRow.transform, "WorldMusicExplorerCard", new Vector2(510f, 0f));
        }
    }

    private static void RebuildOceanScene()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/OceanRhythm.unity", OpenSceneMode.Single);
        RemoveLegacyBunnyObjects();

        OceanRhythmManager manager = Object.FindObjectOfType<OceanRhythmManager>(true);
        if (manager == null)
        {
            manager = new GameObject("OceanRhythmManager").AddComponent<OceanRhythmManager>();
        }
        ConfigurePolicy(manager.scenePolicy, "OceanRhythmRuntime", false);
        manager.scenePolicy.preserveExistingImageOverrides = true;

        GameObject canvas = EnsureCanvasRoot("OceanRhythmCanvas", 100);
        GameObject root = EnsureFullscreenRect(canvas.transform, "OceanRoot");
        EnsureImage(root, new Color(0.03f, 0.43f, 0.66f, 1f));
        EnsureOceanBucketAlbumConfig();
        EnsureOceanContract(root.transform);
        EnsureCamera();
        EnsureEventSystem();
        Save(scene);
    }

    private static void RebuildVerticalScene(string scenePath, VerticalRunnerMode mode)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        RemoveLegacyBunnyObjects();

        VerticalRunnerManager manager = Object.FindObjectOfType<VerticalRunnerManager>(true);
        if (manager == null)
        {
            manager = new GameObject("VerticalRunnerManager").AddComponent<VerticalRunnerManager>();
        }
        manager.mode = mode;
        ConfigurePolicy(manager.scenePolicy, "VerticalRunnerRuntime", true);
        manager.scenePolicy.preserveExistingImageOverrides = true;

        if (manager.GetComponent<VerticalBeatSpawner>() == null)
        {
            manager.gameObject.AddComponent<VerticalBeatSpawner>();
        }
        if (manager.GetComponent<VerticalRunnerUI>() == null)
        {
            manager.gameObject.AddComponent<VerticalRunnerUI>();
        }
        if (manager.GetComponent<VerticalRunnerCamera>() == null)
        {
            manager.gameObject.AddComponent<VerticalRunnerCamera>();
        }

        GameObject runtime = EnsureRoot("VerticalRunnerRuntime");
        EnsureVerticalTemplates(runtime.transform);
        EnsureVerticalScrollingBackground();
        EnsureVerticalCanvas();
        EnsureCamera();
        EnsureEventSystem();
        Save(scene);
    }

    private static void RebuildAdvancedScene(string scenePath, AdvancedRunnerMode mode)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        RemoveLegacyBunnyObjects();

        AdvancedRunnerManager manager = Object.FindObjectOfType<AdvancedRunnerManager>(true);
        if (manager == null)
        {
            manager = new GameObject("AdvancedRunnerManager").AddComponent<AdvancedRunnerManager>();
        }
        RemoveDuplicateAdvancedManagers(manager);
        manager.mode = mode;
        ConfigureAdvancedManagerDefaults(manager);
        ConfigurePolicy(manager.scenePolicy, "AdvancedRunnerRuntime", true);
        manager.scenePolicy.preserveExistingImageOverrides = true;

        EnsureAdvancedConfigContract(manager);
        EnsureRoot("AdvancedRunnerRuntime");
        EnsureAdvancedCanvas();
        EnsureCamera();
        EnsureEventSystem();
        Save(scene);
    }

    private static void RemoveDuplicateAdvancedManagers(AdvancedRunnerManager keep)
    {
        AdvancedRunnerManager[] managers = Object.FindObjectsOfType<AdvancedRunnerManager>(true);
        for (int i = managers.Length - 1; i >= 0; i--)
        {
            if (managers[i] != null && managers[i] != keep)
            {
                Object.DestroyImmediate(managers[i].gameObject);
            }
        }
    }

    private static void ConfigurePolicy(RuntimeScenePolicy policy, string runtimeRootName, bool overrideCamera)
    {
        policy.useExistingSceneObjects = true;
        policy.autoCreateMissingObjects = true;
        policy.overrideCameraTransform = overrideCamera;
        policy.rebuildUiOnPlay = false;
        policy.runtimeGeneratedRootName = runtimeRootName;
    }

    private static void ConfigureAdvancedManagerDefaults(AdvancedRunnerManager manager)
    {
        if (manager == null)
        {
            return;
        }

        if (manager.settings == null)
        {
            manager.settings = new AdvancedRunnerSettings();
        }

        if (manager.settings.startBeat < 0)
        {
            manager.settings.startBeat = 4;
        }
        if (manager.settings.countdownBeats < 0)
        {
            manager.settings.countdownBeats = 0;
        }
        if (manager.settings.beatsPerAction < 1)
        {
            manager.settings.beatsPerAction = 1;
        }
        if (manager.settings.sceneBpm <= 0f)
        {
            manager.settings.sceneBpm = 126f;
        }
        if (manager.settings.tutorialBpm <= 0f)
        {
            manager.settings.tutorialBpm = 126f;
        }
        if (manager.settings.gameBpm <= 0f)
        {
            manager.settings.gameBpm = 126f;
        }
        if (manager.settings.feedback == null)
        {
            manager.settings.feedback = new AdvancedFeedbackStyle();
        }
        if (manager.settings.feedback.fontSize <= 0)
        {
            manager.settings.feedback.fontSize = 32;
        }
        if (manager.settings.feedback.pulseScale < 1f)
        {
            manager.settings.feedback.pulseScale = 1.16f;
        }
    }

    private static void EnsureAdvancedConfigContract(AdvancedRunnerManager manager)
    {
        GameObject configRoot = EnsureRoot("AdvancedRunnerConfig");
        AdvancedRunnerFeedbackConfig feedback = EnsurePlainChild(configRoot.transform, "Feedback").GetComponent<AdvancedRunnerFeedbackConfig>();
        if (feedback == null)
        {
            feedback = configRoot.transform.Find("Feedback").gameObject.AddComponent<AdvancedRunnerFeedbackConfig>();
        }
        if (feedback.feedback == null)
        {
            feedback.feedback = manager != null && manager.settings != null ? manager.settings.feedback : new AdvancedFeedbackStyle();
        }
        if (feedback.feedback.fontSize <= 0)
        {
            feedback.feedback.fontSize = 32;
        }
        if (feedback.feedback.pulseScale < 1f)
        {
            feedback.feedback.pulseScale = 1.16f;
        }

        GameObject musicRoot = EnsurePlainChild(configRoot.transform, "Music");
        EnsureAdvancedMusicConfig(musicRoot.transform, "Scene", manager != null && manager.settings != null ? manager.settings.sceneBgm : null, manager != null && manager.settings != null ? manager.settings.sceneBpm : 126f);
        EnsureAdvancedMusicConfig(musicRoot.transform, "Tutorial", manager != null && manager.settings != null ? manager.settings.tutorialBgm : null, manager != null && manager.settings != null ? manager.settings.tutorialBpm : 126f);
        EnsureAdvancedMusicConfig(musicRoot.transform, "Game", manager != null && manager.settings != null ? manager.settings.gameBgm : null, manager != null && manager.settings != null ? manager.settings.gameBpm : 126f);
    }

    private static void EnsureAdvancedMusicConfig(Transform parent, string name, AudioClip fallbackClip, float fallbackBpm)
    {
        GameObject obj = EnsurePlainChild(parent, name);
        AdvancedRunnerMusicConfig config = obj.GetComponent<AdvancedRunnerMusicConfig>();
        if (config == null)
        {
            config = obj.AddComponent<AdvancedRunnerMusicConfig>();
        }
        if (config.bgm == null)
        {
            config.bgm = fallbackClip;
        }
        if (config.bpm <= 0f)
        {
            config.bpm = fallbackBpm > 0f ? fallbackBpm : 126f;
        }
    }

    private static void RemoveLegacyBunnyObjects()
    {
        string[] legacyNames = { "Player", "GameManager", "TutorialFlowManager", "TutorialUIController", "TutorialBeatSpawner", "SceneDifficultySettings", "Background1", "Floor", "BarriersPoint1", "BarriersPoint2", "Barrier" };
        List<GameObject> destroyList = new List<GameObject>();
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

        for (int r = 0; r < roots.Length; r++)
        {
            CollectLegacyBunnyObjects(roots[r], legacyNames, destroyList);
        }

        for (int i = destroyList.Count - 1; i >= 0; i--)
        {
            if (destroyList[i] != null)
            {
                Object.DestroyImmediate(destroyList[i]);
            }
        }
    }

    private static void CollectLegacyBunnyObjects(GameObject obj, string[] legacyNames, List<GameObject> destroyList)
    {
        if (obj == null || destroyList.Contains(obj))
        {
            return;
        }

        if (PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.MissingAsset)
        {
            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            destroyList.Add(prefabRoot != null ? prefabRoot : obj);
            return;
        }

        for (int i = 0; i < legacyNames.Length; i++)
        {
            if (obj.name == legacyNames[i] || obj.name.StartsWith(legacyNames[i] + " "))
            {
                destroyList.Add(obj);
                return;
            }
        }

        Transform transform = obj.transform;
        for (int c = 0; c < transform.childCount; c++)
        {
            CollectLegacyBunnyObjects(transform.GetChild(c).gameObject, legacyNames, destroyList);
        }
    }

    private static void EnsureOceanContract(Transform root)
    {
        GameObject top = EnsurePanel(root, "TopBar", new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(980f, 92f), new Color(0.02f, 0.22f, 0.32f, 0.72f));
        EnsureText(top.transform, "LearningMode", "Ocean Rhythm", new Vector2(0.12f, 0.62f), Vector2.zero, new Vector2(190f, 28f), 18, FontStyle.Bold, Color.white);
        EnsureText(top.transform, "AnimalTitle", "Rhythm Ocean", new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(430f, 38f), 30, FontStyle.Bold, Color.white);
        EnsureText(top.transform, "Meter", "Listen and tap", new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(520f, 28f), 18, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        EnsureText(top.transform, "LessonCounter", "1 / 4", new Vector2(0.88f, 0.5f), Vector2.zero, new Vector2(160f, 34f), 20, FontStyle.Bold, Color.white);

        EnsureRect(root, "OceanAnimal", new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(280f, 220f));
        EnsureRect(root, "FreePondLayer", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(960f, 430f));
        GameObject bottom = EnsurePanel(root, "BottomHud", new Vector2(0.5f, 0f), new Vector2(0f, 88f), new Vector2(920f, 146f), new Color(0.02f, 0.22f, 0.32f, 0.72f));
        EnsureText(bottom.transform, "Instruction", "Tap on the bright beat.", new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(700f, 30f), 22, FontStyle.Bold, Color.white);
        EnsureText(bottom.transform, "Feedback", "Ready", new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(700f, 34f), 26, FontStyle.Bold, new Color(1f, 0.86f, 0.18f));
        EnsureText(bottom.transform, "LessonGoal", "Goal", new Vector2(0.18f, 0.25f), Vector2.zero, new Vector2(230f, 28f), 18, FontStyle.Bold, Color.white);
        EnsureText(bottom.transform, "ProgressHelp", "Progress", new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(230f, 28f), 18, FontStyle.Bold, Color.white);
        EnsureText(bottom.transform, "ProgressText", "0%", new Vector2(0.82f, 0.25f), Vector2.zero, new Vector2(120f, 28f), 18, FontStyle.Bold, Color.white);
        GameObject progress = EnsurePanel(bottom.transform, "ProgressBar", new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(520f, 16f), new Color(1f, 1f, 1f, 0.18f));
        Image fill = EnsureImage(EnsureRect(progress.transform, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 16f)), new Color(0.27f, 0.95f, 0.54f, 1f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        EnsureRect(bottom.transform, "BeatBubbleRow", new Vector2(0.5f, 0.39f), Vector2.zero, new Vector2(360f, 42f));
        EnsureRect(bottom.transform, "LessonTargetBubbleRow", new Vector2(0.5f, 0.02f), Vector2.zero, new Vector2(360f, 28f));

        EnsureButton(root, "ParentHelpButton", "?", new Vector2(0f, 1f), new Vector2(46f, -46f), new Vector2(58f, 58f));
        EnsureButton(root, "TapButton", "TAP", new Vector2(0.5f, 0f), new Vector2(0f, 214f), new Vector2(180f, 74f));
        EnsureButton(root, "BackButton", "<", new Vector2(0f, 1f), new Vector2(46f, -112f), new Vector2(58f, 58f));
        EnsureButton(root, "RetryButton", "R", new Vector2(0f, 1f), new Vector2(112f, -112f), new Vector2(58f, 58f));
        EnsureButton(root, "PauseButton", "||", new Vector2(0f, 1f), new Vector2(178f, -112f), new Vector2(58f, 58f));
        EnsureButton(root, "CatchBucketButton", "Bucket", new Vector2(1f, 1f), new Vector2(-92f, -54f), new Vector2(150f, 68f));
        EnsureButton(root, "SingingShellButton", "Listen", new Vector2(1f, 1f), new Vector2(-106f, -130f), new Vector2(180f, 62f));

        EnsureOceanOverlay(root, "CompleteOverlay", false);
        EnsureOceanOverlay(root, "PondCompleteOverlay", true);
        EnsureOceanBeatCardContract(root);
        EnsureOceanOverlay(root, "ParentHelpOverlay", true);
        EnsureOceanBucketAlbumContract(root);
        EnsureSoundMatch(root);
        EnsureText(root, "RewardToast", "", new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(620f, 54f), 30, FontStyle.Bold, new Color(1f, 0.86f, 0.18f));
    }

    public static void EnsureOceanBucketAlbumConfig()
    {
        GameObject configRoot = EnsureRoot("OceanRhythmConfig");
        GameObject assetsObject = EnsurePlainChild(configRoot.transform, "BucketAlbumAssets");
        OceanBucketAlbumAssets assets = assetsObject.GetComponent<OceanBucketAlbumAssets>();
        if (assets == null)
        {
            assets = assetsObject.AddComponent<OceanBucketAlbumAssets>();
        }
        if (assets.decorationIcons == null || assets.decorationIcons.Length == 0)
        {
            OceanDecorationReward[] rewards = OceanBucketInventory.GetAllDecorations();
            assets.decorationIcons = new OceanDecorationSpriteBinding[rewards.Length];
            for (int i = 0; i < rewards.Length; i++)
            {
                assets.decorationIcons[i] = new OceanDecorationSpriteBinding { reward = rewards[i] };
            }
        }
        EditorUtility.SetDirty(assetsObject);
        EditorUtility.SetDirty(assets);
    }

    public static void EnsureOceanBeatCardContract(Transform root)
    {
        if (root == null)
        {
            return;
        }

        GameObject overlay = EnsureFullscreenPanel(root, "BeatCardOverlay", new Color(0f, 0f, 0f, 0.72f));
        GameObject cards = EnsureMissingRect(overlay.transform, "Cards", Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform cardsRect = cards.GetComponent<RectTransform>();
        if (cardsRect != null)
        {
            cardsRect.anchorMin = Vector2.zero;
            cardsRect.anchorMax = Vector2.one;
            cardsRect.anchoredPosition = Vector2.zero;
            cardsRect.sizeDelta = Vector2.zero;
        }

        EnsureOceanBeatCard(cards.transform, "IntroCard", "Little Rhythm Ocean", "Feel 2/4, 3/4, 4/4, and 6/8", "Pick a sea friend and listen first. Then tap with the bright bubbles to feel the beat and bring the fish home.");
        EnsureOceanBeatCard(cards.transform, "FourFourCard", "4/4 Walking Beat", "STRONG  soft  soft  soft", "A walking beat feels steady: one strong step, then three soft steps.");
        EnsureOceanBeatCard(cards.transform, "ThreeFourCard", "3/4 Sway Beat", "STRONG  soft  soft", "A sway beat rocks like a boat: strong, soft, soft.");
        EnsureOceanBeatCard(cards.transform, "TwoFourCard", "2/4 March Beat", "STRONG  soft", "A march beat steps left and right: strong, soft.");
        EnsureOceanBeatCard(cards.transform, "SixEightCard", "6/8 Wave Beat", "STRONG  soft  soft   STRONG  soft  soft", "A wave beat rolls in two groups: strong soft soft, strong soft soft.");

        for (int i = 0; i < cards.transform.childCount; i++)
        {
            cards.transform.GetChild(i).gameObject.SetActive(false);
        }
        overlay.SetActive(false);
    }

    private static void EnsureOceanBeatCard(Transform parent, string name, string title, string pattern, string body)
    {
        GameObject card = EnsureMissingRect(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 420f));
        Image cardImage = card.GetComponent<Image>();
        if (cardImage == null)
        {
            cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.05f, 0.36f, 0.46f, 0.98f);
        }
        cardImage.raycastTarget = false;

        EnsureMissingText(card.transform, "Title", title, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(680f, 62f), 42, FontStyle.Bold, Color.white);
        EnsureMissingText(card.transform, "Pattern", pattern, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(690f, 58f), 30, FontStyle.Bold, new Color(1f, 0.86f, 0.18f));
        EnsureMissingText(card.transform, "Body", body, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(680f, 88f), 26, FontStyle.Normal, Color.white);
        EnsureOptionalButton(card.transform, "CloseButton", "Close", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(220f, 64f));
    }

    private static void EnsureOceanOverlay(Transform root, string name, bool withBack)
    {
        GameObject overlay = EnsureFullscreenPanel(root, name, new Color(0f, 0f, 0f, 0.72f));
        GameObject card = EnsurePanel(overlay.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 420f), new Color(0.05f, 0.36f, 0.46f, 0.98f));
        EnsureText(card.transform, "Title", name, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(580f, 54f), 36, FontStyle.Bold, Color.white);
        EnsureText(card.transform, "Message", "", new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(580f, 64f), 30, FontStyle.Bold, Color.white);
        EnsureText(card.transform, "Detail", "", new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(580f, 64f), 22, FontStyle.Normal, new Color(1f, 0.94f, 0.68f));
        EnsureButton(card.transform, "CloseButton", "Close", new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(180f, 54f));
        if (withBack)
        {
            EnsureButton(card.transform, "BackButton", "Back", new Vector2(0.66f, 0.14f), Vector2.zero, new Vector2(180f, 54f));
            EnsureButton(card.transform, "PlayAgainButton", "Play Again", new Vector2(0.34f, 0.14f), Vector2.zero, new Vector2(190f, 54f));
            EnsureButton(card.transform, "BackToStartButton", "Back", new Vector2(0.66f, 0.14f), Vector2.zero, new Vector2(180f, 54f));
        }
        overlay.SetActive(false);
    }

    public static void EnsureOceanBucketAlbumContract(Transform root)
    {
        if (root == null)
        {
            return;
        }

        EnsureOceanBucketAlbumConfig();
        GameObject overlay = EnsureFullscreenMissingPanel(root, "BucketAlbumOverlay", new Color(0f, 0f, 0f, 0.72f));
        GameObject card = EnsureMissingPanel(overlay.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080f, 620f), new Color(0.05f, 0.36f, 0.46f, 0.98f));

        GameObject header = EnsureMissingRect(card.transform, "Header", new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(980f, 70f));
        EnsureMissingText(header.transform, "Title", "My Rhythm Bucket", new Vector2(0.34f, 0.5f), Vector2.zero, new Vector2(470f, 54f), 42, FontStyle.Bold, Color.white);
        EnsureBucketCounter(header.transform, "Shells", "Shells", "0", new Vector2(0.68f, 0.5f));
        EnsureBucketCounter(header.transform, "Pearls", "Pearls", "0", new Vector2(0.83f, 0.5f));
        EnsureOptionalButton(header.transform, "CloseButton", "Close", new Vector2(0.96f, 0.5f), Vector2.zero, new Vector2(120f, 48f));

        GameObject preview = EnsureMissingPanel(card.transform, "BucketPreview", new Vector2(0.18f, 0.49f), Vector2.zero, new Vector2(330f, 410f), new Color(1f, 1f, 1f, 0f));
        EnsureMissingImage(EnsureMissingRect(preview.transform, "BucketImage", new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(250f, 250f)), new Color(1f, 1f, 1f, 0.18f));
        EnsureBucketSlot(preview.transform, "TopSlot", new Vector2(0.5f, 0.84f));
        EnsureBucketSlot(preview.transform, "LeftSlot", new Vector2(0.24f, 0.5f));
        EnsureBucketSlot(preview.transform, "RightSlot", new Vector2(0.76f, 0.5f));
        EnsureBucketSlot(preview.transform, "FrontSlot", new Vector2(0.5f, 0.42f));
        EnsureBucketSlot(preview.transform, "CharmSlot", new Vector2(0.5f, 0.14f));

        GameObject collection = EnsureMissingPanel(card.transform, "DecorationCollection", new Vector2(0.5f, 0.49f), Vector2.zero, new Vector2(340f, 410f), new Color(1f, 1f, 1f, 0.08f));
        EnsureMissingText(collection.transform, "Title", "My Decorations", new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(290f, 34f), 25, FontStyle.Bold, Color.white);
        GameObject grid = EnsureMissingRect(collection.transform, "Grid", new Vector2(0.5f, 0.44f), Vector2.zero, new Vector2(286f, 320f));
        GridLayoutGroup gridLayout = grid.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(132f, 94f);
            gridLayout.spacing = new Vector2(12f, 12f);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
        }
        EnsureDecorationTemplate(grid.transform, "UnlockedItemTemplate", new Color(1f, 1f, 1f, 0.92f));
        EnsureDecorationTemplate(grid.transform, "LockedItemTemplate", new Color(0.18f, 0.28f, 0.34f, 0.92f));
        EnsureOptionalButton(collection.transform, "PrevPageButton", "<", new Vector2(0.08f, 0.5f), Vector2.zero, new Vector2(56f, 72f));
        EnsureOptionalButton(collection.transform, "NextPageButton", ">", new Vector2(0.92f, 0.5f), Vector2.zero, new Vector2(56f, 72f));
        EnsureMissingText(collection.transform, "PageText", "1 / 1", new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(120f, 30f), 16, FontStyle.Bold, Color.white);

        EnsureDecorationDetail(card.transform);
        EnsureOptionalButton(card.transform, "CloseButton", "Close", new Vector2(0.5f, 0.075f), Vector2.zero, new Vector2(200f, 56f));
        overlay.SetActive(false);
    }

    private static void EnsureSoundMatch(Transform root)
    {
        GameObject overlay = EnsureFullscreenPanel(root, "SoundMatchOverlay", new Color(0f, 0f, 0f, 0.72f));
        GameObject card = EnsurePanel(overlay.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 520f), new Color(0.06f, 0.38f, 0.55f, 0.98f));
        EnsureText(card.transform, "Title", "Listen to the shell", new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(720f, 46f), 36, FontStyle.Bold, Color.white);
        EnsureText(card.transform, "Body", "Which friend sings this beat?", new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(720f, 44f), 26, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        EnsureRect(card.transform, "SoundBubbles", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(560f, 62f));
        EnsureRect(card.transform, "Options", new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(620f, 130f));
        EnsureText(card.transform, "Result", "Listen...", new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(700f, 44f), 25, FontStyle.Bold, Color.white);
        EnsureText(card.transform, "Pearls", "0 music pearls", new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(260f, 30f), 19, FontStyle.Bold, new Color(0.78f, 0.95f, 1f));
        EnsureButton(card.transform, "ReplayButton", "Replay", new Vector2(0.16f, 0.08f), Vector2.zero, new Vector2(150f, 48f));
        EnsureButton(card.transform, "CloseButton", "Close", new Vector2(0.84f, 0.08f), Vector2.zero, new Vector2(150f, 48f));
        overlay.SetActive(false);
    }

    private static void EnsureVerticalTemplates(Transform runtimeRoot)
    {
        GameObject templatesObject = EnsureRoot("VerticalRunnerTemplates");
        VerticalRunnerTemplates templates = templatesObject.GetComponent<VerticalRunnerTemplates>();
        if (templates == null)
        {
            templates = templatesObject.AddComponent<VerticalRunnerTemplates>();
        }
        templates.runtimeRoot = runtimeRoot;
        templates.playerTemplate = EnsureSpriteTemplate(templatesObject.transform, "PlayerTemplate", new Vector3(0.48f, 0.48f, 1f), new Color(0.25f, 0.82f, 1f, 1f), 5, true);
        if (templates.playerTemplate.GetComponent<VerticalRunnerPlayer>() == null) templates.playerTemplate.AddComponent<VerticalRunnerPlayer>();
        templates.platformTemplate = EnsureSpriteTemplate(templatesObject.transform, "PlatformTemplate", new Vector3(1.3f, 0.36f, 1f), new Color(0.35f, 0.82f, 0.45f, 1f), 0, false);
        templates.longPlatformTemplate = EnsureSpriteTemplate(templatesObject.transform, "LongPlatformTemplate", new Vector3(1.55f, 0.42f, 1f), new Color(0.58f, 0.42f, 0.95f, 1f), 0, false);
        templates.coinTemplate = EnsureSpriteTemplate(templatesObject.transform, "CoinTemplate", new Vector3(0.32f, 0.32f, 1f), new Color(1f, 0.84f, 0.16f, 1f), 2, true);
        templates.obstacleTemplate = EnsureSpriteTemplate(templatesObject.transform, "ObstacleTemplate", new Vector3(0.52f, 0.52f, 1f), new Color(1f, 0.24f, 0.22f, 1f), 2, true);
        templates.finishTemplate = EnsureSpriteTemplate(templatesObject.transform, "FinishTemplate", new Vector3(2.2f, 0.22f, 1f), new Color(1f, 0.86f, 0.18f, 1f), 1, false);
        templates.worldLabelTemplate = EnsureTextTemplate(templatesObject.transform, "WorldLabelTemplate");
        SetChildrenInactive(templatesObject.transform);
        EditorUtility.SetDirty(templates);
    }

    private static void EnsureVerticalScrollingBackground()
    {
        GameObject backgroundObject = GameObject.Find("vertical");
        bool createdObject = false;
        if (backgroundObject == null)
        {
            backgroundObject = new GameObject("vertical");
            createdObject = true;
        }

        Sprite defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Background/vertical.png");
        SpriteRenderer renderer = backgroundObject.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = backgroundObject.AddComponent<SpriteRenderer>();
        }
        if (renderer.sprite == null)
        {
            renderer.sprite = defaultSprite;
        }
        if (createdObject)
        {
            renderer.sortingOrder = -50;
        }

        VerticalScrollingBackground background = backgroundObject.GetComponent<VerticalScrollingBackground>();
        bool createdComponent = false;
        if (background == null)
        {
            background = backgroundObject.AddComponent<VerticalScrollingBackground>();
            createdComponent = true;
        }
        if (background.backgroundSprite == null)
        {
            background.backgroundSprite = renderer.sprite != null ? renderer.sprite : defaultSprite;
        }
        if (createdComponent)
        {
            background.sortingOrder = -50;
            background.tileCount = 4;
            background.fitWidthToCamera = true;
            background.widthPadding = 0.35f;
            background.verticalOffset = 0f;
            background.cameraParallax = 0.2f;
            background.autoScrollSpeed = 0f;
        }
        background.UpdateTiles();
        EditorUtility.SetDirty(backgroundObject);
        EditorUtility.SetDirty(background);
    }

    private static void EnsureVerticalCanvas()
    {
        GameObject canvas = EnsureCanvasRoot("VerticalRunnerCanvas", 160);
        CreateVerticalHud(canvas.transform);
        CreateVerticalOverlay(canvas.transform, "VerticalRunnerResult", "Climbed Up", "Retry", "Back");
        CreateVerticalOverlay(canvas.transform, "TutorialBriefingOverlay", "Monkey Climb", "StartTutorialButton", null,
            "Jump up.\nGrab banana.\nAvoid parrot.\n\nSpace every 2 beats.\nDown/S between jumps.\nSpace + Left/Right between jumps.");
        CreateVerticalOverlay(canvas.transform, "GameRulesOverlay", "Climb Up", "StartGameButton", null,
            "Space: jump\nDown/S: banana\nSpace + Left/Right: parrot\nMisses are counted.");
        CreateVerticalOverlay(canvas.transform, "TutorialCompleteRulesOverlay", "Ready", "StartGameButton", null,
            "Space: jump\nDown/S: banana\nSpace + Left/Right: parrot\nMisses are counted.");
        EnsureVerticalGameControls(canvas.transform);
    }

    private static void CreateVerticalHud(Transform root)
    {
        GameObject top = EnsurePanel(root, "TopHud", new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(1080f, 92f), new Color(0.02f, 0.08f, 0.13f, 0.78f));
        EnsureText(top.transform, "Title", "Monkey Climb", new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(620f, 42f), 32, FontStyle.Bold, Color.white);
        EnsureText(top.transform, "Subtitle", "Jump up", new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(720f, 30f), 20, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        EnsureHudCounter(top.transform, "Misses", "0", new Vector2(0.1f, 0.5f), new Vector2(180f, 38f));
        EnsureHudCounter(top.transform, "Coins", "0", new Vector2(0.88f, 0.68f), new Vector2(170f, 34f));
        EnsureHudCounter(top.transform, "Combo", "0/0", new Vector2(0.88f, 0.28f), new Vector2(190f, 30f));
        GameObject objective = EnsurePanel(root, "ObjectivePanel", new Vector2(0f, 0.5f), new Vector2(170f, 18f), new Vector2(330f, 230f), new Color(0.02f, 0.08f, 0.13f, 0.68f));
        EnsureText(objective.transform, "Objective", "Climb up", new Vector2(0.5f, 0.77f), Vector2.zero, new Vector2(286f, 52f), 22, FontStyle.Bold, Color.white);
        EnsureText(objective.transform, "Progress", "o", new Vector2(0.5f, 0.51f), Vector2.zero, new Vector2(220f, 46f), 30, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        EnsureText(objective.transform, "Rules", "Space", new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(288f, 92f), 18, FontStyle.Normal, new Color(0.83f, 0.98f, 1f));
        EnsureTutorialImages(objective.transform);
        GameObject bottom = EnsurePanel(root, "BottomHud", new Vector2(0.5f, 0f), new Vector2(0f, 86f), new Vector2(820f, 130f), new Color(0.02f, 0.08f, 0.13f, 0.72f));
        EnsureText(bottom.transform, "Feedback", "Ready", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(620f, 42f), 30, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        GameObject beatLane = EnsureRect(bottom.transform, "BeatLane", new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(300f, 44f));
        EnsureBeatDots(beatLane.transform);
        EnsureVerticalControlRhythmPrompt(bottom.transform);
        EnsureOptionalButton(bottom.transform, "BeatVisualToggleButton", "Beat: ON", new Vector2(0.18f, 0.38f), Vector2.zero, new Vector2(126f, 40f));
        GameObject progress = EnsurePanel(bottom.transform, "Progress", new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(540f, 18f), new Color(1f, 1f, 1f, 0.18f));
        Image fill = EnsureImage(EnsureRect(progress.transform, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 18f)), new Color(0.27f, 0.95f, 0.54f, 1f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        fill.raycastTarget = false;
        EnsureText(bottom.transform, "ProgressText", "0%", new Vector2(0.84f, 0.12f), Vector2.zero, new Vector2(90f, 26f), 18, FontStyle.Bold, Color.white);
    }

    private static void EnsureVerticalGameControls(Transform root)
    {
        GameObject controls = EnsureRect(root, "GameControls", new Vector2(1f, 1f), new Vector2(-142f, -128f), new Vector2(260f, 58f));
        EnsureButton(controls.transform, "RetryButton", "Retry", new Vector2(0.28f, 0.5f), Vector2.zero, new Vector2(116f, 48f));
        EnsureButton(controls.transform, "BackButton", "Back", new Vector2(0.74f, 0.5f), Vector2.zero, new Vector2(116f, 48f));
        controls.SetActive(false);
    }

    private static void EnsureHudCounter(Transform parent, string name, string value, Vector2 anchor, Vector2 size)
    {
        GameObject counter = EnsureMissingRect(parent, name, anchor, Vector2.zero, size);
        Text legacyText = counter.GetComponent<Text>();
        if (legacyText != null)
        {
            legacyText.text = "";
            legacyText.raycastTarget = false;
        }

        GameObject icon = EnsureMissingRect(counter.transform, "Icon", new Vector2(0.18f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
        Image iconImage = icon.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = icon.AddComponent<Image>();
        }
        Color iconColor = iconImage.color;
        iconColor.a = 1f;
        iconImage.color = iconColor;
        iconImage.raycastTarget = false;

        Text valueText = EnsureMissingText(counter.transform, "Value", value, new Vector2(0.66f, 0.5f), Vector2.zero, new Vector2(size.x * 0.58f, size.y), 21, FontStyle.Bold, Color.white);
        valueText.raycastTarget = false;
    }

    private static void EnsureTutorialImages(Transform objective)
    {
        GameObject root = EnsureMissingRect(objective, "TutorialImages", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 110f));
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
            GameObject slot = EnsureMissingRect(root.transform, names[i], new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 110f));
            Image image = slot.GetComponent<Image>();
            if (image == null)
            {
                image = slot.AddComponent<Image>();
                image.color = Color.white;
            }
            image.raycastTarget = false;
            slot.SetActive(false);
        }
    }

    private static void CreateVerticalOverlay(Transform root, string name, string title, string primaryButton, string secondaryButton, string bodyText = "")
    {
        GameObject overlay = EnsureFullscreenPanel(root, name, new Color(0.01f, 0.04f, 0.07f, 0.84f));
        GameObject card = EnsurePanel(overlay.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 500f), new Color(0.05f, 0.31f, 0.42f, 0.98f));
        EnsureText(card.transform, "Title", title, new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(660f, 58f), 42, FontStyle.Bold, Color.white);
        EnsureText(card.transform, "Stats", "", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(650f, 220f), 24, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        EnsureText(card.transform, "Body", bodyText, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(650f, 260f), 27, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        string primaryLabel = primaryButton == "StartGameButton" || primaryButton == "StartTutorialButton" ? "Start" : primaryButton.Replace("Button", "");
        EnsureButton(card.transform, primaryButton, primaryLabel, new Vector2(secondaryButton == null ? 0.5f : 0.35f, 0.14f), Vector2.zero, new Vector2(220f, 62f));
        if (!string.IsNullOrEmpty(secondaryButton))
        {
            EnsureButton(card.transform, secondaryButton, secondaryButton, new Vector2(0.65f, 0.14f), Vector2.zero, new Vector2(160f, 54f));
        }
        overlay.SetActive(false);
    }

    private static void EnsureAdvancedCanvas()
    {
        EnsureAdvancedWorldBackground();
        EnsureAdvancedWorldContract();
        GameObject canvas = EnsureCanvasRoot("AdvancedRunnerCanvas", 180);
        HideChildIfPresent(canvas.transform, "bg");
        GameObject top = EnsureAdvancedVisibleImagePanel(canvas.transform, "TopHud", new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(1080f, 86f));
        EnsureEditableText(top.transform, "Title", "Advanced Runner", new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(520f, 38f), 30, FontStyle.Bold, Color.white);
        EnsureEditableText(top.transform, "Hint", "Watch the picture cue, then tap on beat.", new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(620f, 30f), 19, FontStyle.Bold, new Color(0.72f, 0.92f, 1f));
        GameObject oldStats = EnsureRect(top.transform, "Stats", new Vector2(0.88f, 0.5f), Vector2.zero, new Vector2(250f, 42f));
        oldStats.SetActive(false);
        HideChildIfPresent(top.transform, "Hearts");
        EnsureAdvancedHudCounter(top.transform, "Score", "SCORE", new Vector2(0.18f, 0.5f), new Vector2(0f, 0f));
        EnsureAdvancedHudCounter(top.transform, "Combo", "COMBO", new Vector2(0.82f, 0.5f), new Vector2(0f, 0f));
        HideChildIfPresent(top.transform, "Best");
        EnsureRect(canvas.transform, "ActionLane", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 420f));
        EnsureRect(canvas.transform, "TargetLane", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 420f));
        GameObject objective = EnsureAdvancedTransparentPanel(canvas.transform, "ObjectivePanel", new Vector2(0f, 0.5f), new Vector2(160f, 20f), new Vector2(300f, 220f));
        HideChildIfPresent(objective.transform, "Objective");
        HideChildIfPresent(objective.transform, "Progress");
        EnsureAdvancedImageSlot(objective.transform, "LessonImage", new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(230f, 112f));
        GameObject lessonProgress = EnsureAdvancedTransparentPanel(objective.transform, "LessonProgress", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(230f, 52f));
        EnsureAdvancedImageSlot(lessonProgress.transform, "Icon", new Vector2(0.16f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
        EnsureAdvancedImageSlot(lessonProgress.transform, "LabelImage", new Vector2(0.42f, 0.5f), Vector2.zero, new Vector2(78f, 34f));
        EnsureEditableText(lessonProgress.transform, "Label", "GO", new Vector2(0.42f, 0.5f), Vector2.zero, new Vector2(78f, 34f), 16, FontStyle.Bold, Color.white);
        EnsureEditableText(lessonProgress.transform, "Value", "1/5  0/3", new Vector2(0.78f, 0.5f), Vector2.zero, new Vector2(82f, 34f), 18, FontStyle.Bold, new Color(1f, 0.86f, 0.18f));
        GameObject bottom = EnsureAdvancedVisibleImagePanel(canvas.transform, "BottomHud", new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(860f, 128f));
        EnsureEditableText(bottom.transform, "Feedback", "Ready", new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(760f, 46f), 32, FontStyle.Bold, new Color(1f, 0.86f, 0.18f));
        GameObject beat = EnsureAdvancedTransparentPanel(bottom.transform, "Beat", new Vector2(0.88f, 0.26f), Vector2.zero, new Vector2(138f, 38f));
        Text oldBeatText = beat.GetComponent<Text>();
        if (oldBeatText != null)
        {
            oldBeatText.text = "";
            oldBeatText.raycastTarget = false;
        }
        EnsureAdvancedImageSlot(beat.transform, "Icon", new Vector2(0.18f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
        EnsureEditableText(beat.transform, "Label", "BEAT", new Vector2(0.48f, 0.5f), Vector2.zero, new Vector2(58f, 26f), 13, FontStyle.Bold, Color.white);
        EnsureEditableText(beat.transform, "Value", "0.00", new Vector2(0.82f, 0.5f), Vector2.zero, new Vector2(42f, 26f), 14, FontStyle.Bold, new Color(1f, 0.86f, 0.18f));
        EnsureAdvancedBeatVisuals(beat.transform);
        EnsureAdvancedControlRhythmPrompt(bottom.transform);
        GameObject progress = EnsureAdvancedTransparentPanel(bottom.transform, "Progress", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(680f, 18f));
        Image fill = EnsureImage(EnsureRect(progress.transform, "Fill", Vector2.zero, Vector2.zero, new Vector2(680f, 18f)), new Color(0.2f, 0.9f, 1f, 1f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        EnsureAdvancedBriefingOverlay(canvas.transform, "TutorialOverlay", "Advanced Tutorial", "Watch the pictures.\nTap the right move on the beat.", "Start Game");
        EnsureAdvancedBriefingOverlay(canvas.transform, "GameRulesOverlay", "Advanced Runner", "Follow the picture cue.\nKeep the beat and stay in lane.", "Start Run");
        EnsureAdvancedResultOverlay(canvas.transform);
    }

    private static void EnsureAdvancedWorldBackground()
    {
        GameObject backgroundObject = GameObject.Find("AdvancedRunnerBackground");
        if (backgroundObject == null)
        {
            backgroundObject = new GameObject("AdvancedRunnerBackground");
        }

        SpriteRenderer renderer = backgroundObject.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = backgroundObject.AddComponent<SpriteRenderer>();
        }

        if (renderer.sprite == null)
        {
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/catsSee/BD.jpg");
        }

        renderer.color = Color.white;
        renderer.sortingOrder = -50;
        backgroundObject.transform.position = new Vector3(0f, 0f, 8f);
        backgroundObject.transform.localRotation = Quaternion.identity;
        backgroundObject.transform.localScale = new Vector3(13.7f, 7.7f, 1f);
        EditorUtility.SetDirty(backgroundObject);
    }

    private static void EnsureAdvancedWorldContract()
    {
        GameObject world = EnsureRoot("AdvancedRunnerWorld");
        GameObject anchors = EnsurePlainChild(world.transform, "LaneAnchors");
        EnsurePlainChild(anchors.transform, "Lane_0").transform.position = new Vector3(-1.65f, -2.85f, 0f);
        EnsurePlainChild(anchors.transform, "Lane_1").transform.position = new Vector3(0f, -2.85f, 0f);
        EnsurePlainChild(anchors.transform, "Lane_2").transform.position = new Vector3(1.65f, -2.85f, 0f);

        GameObject guide = EnsurePlainChild(world.transform, "AdvancedWorldGuide");
        for (int lane = 0; lane < 3; lane++)
        {
            GameObject line = EnsurePlainChild(guide.transform, "AdvancedLane_" + lane);
            if (line.transform.localScale == Vector3.one)
            {
                line.transform.localScale = new Vector3(0.05f, 7.6f, 1f);
            }
            if (line.transform.position == Vector3.zero)
            {
                line.transform.position = new Vector3((lane - 1) * 1.65f, 0.55f, 0f);
            }
            EnsureWorldSprite(line, Color.white, -2, false);
        }

        GameObject judgement = EnsurePlainChild(guide.transform, "AdvancedJudgementLine");
        if (judgement.transform.localScale == Vector3.one)
        {
            judgement.transform.localScale = new Vector3(6.4f, 0.08f, 1f);
        }
        if (judgement.transform.position == Vector3.zero)
        {
            judgement.transform.position = new Vector3(0f, -2.85f, 0f);
        }
        EnsureWorldSprite(judgement, new Color(1f, 0.86f, 0.18f, 0.78f), -1, true);

        GameObject player = EnsurePlainChild(world.transform, "AdvancedRunnerPlayer");
        if (player.GetComponent<AdvancedRunnerPlayer>() == null)
        {
            player.AddComponent<AdvancedRunnerPlayer>();
        }
        EnsureWorldSprite(player, new Color(0.18f, 0.92f, 1f, 1f), 6, false);
        EnsureAdvancedBackdrop(player.transform, new Vector3(8f, 8f, 1f), 5);

        EnsurePlainChild(world.transform, "AdvancedTargets");
        GameObject templates = EnsurePlainChild(world.transform, "AdvancedTargetTemplates");
        EnsureAdvancedTargetTemplate(templates.transform, "Jump", "JUMP", new Vector3(0.78f, 0.52f, 1f), Color.white, new Vector3(16f, 16f, 1f));
        EnsureAdvancedTargetTemplate(templates.transform, "Slide", "DOWN", new Vector3(0.78f, 0.52f, 1f), Color.white, new Vector3(16f, 16f, 1f));
        EnsureAdvancedTargetTemplate(templates.transform, "LaneLeft", "LEFT", new Vector3(0.86f, 0.46f, 1f), Color.white, new Vector3(18f, 5f, 1f));
        EnsureAdvancedTargetTemplate(templates.transform, "LaneRight", "RIGHT", new Vector3(0.86f, 0.46f, 1f), Color.white, new Vector3(18f, 5f, 1f));
        EnsureAdvancedTargetTemplate(templates.transform, "Coin", "COIN", new Vector3(0.44f, 0.44f, 1f), Color.white, new Vector3(8f, 12f, 1f));
    }

    private static void EnsureAdvancedTargetTemplate(Transform parent, string name, string label, Vector3 scale, Color color, Vector3 backdropScale)
    {
        GameObject template = EnsurePlainChild(parent, name);
        if (template.transform.localScale == Vector3.one)
        {
            template.transform.localScale = scale;
        }
        EnsureWorldSprite(template, color, 3, false);
        EnsureAdvancedBackdrop(template.transform, backdropScale, 2);
        GameObject labelObject = EnsurePlainChild(template.transform, "Label");
        if (labelObject.transform.localPosition == Vector3.zero)
        {
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        }
        TextMesh text = labelObject.GetComponent<TextMesh>();
        if (text == null)
        {
            text = labelObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 42;
            text.characterSize = 0.055f;
            text.color = Color.white;
        }
        else if (string.IsNullOrEmpty(text.text) || text.text == "UP" || text.text == "MIX")
        {
            text.text = label;
        }
        MeshRenderer textRenderer = labelObject.GetComponent<MeshRenderer>();
        if (textRenderer != null && textRenderer.sortingOrder == 0)
        {
            textRenderer.sortingOrder = 5;
        }
        template.SetActive(false);
    }

    private static void EnsureAdvancedBackdrop(Transform parent, Vector3 scale, int sortingOrder)
    {
        Transform child = parent.Find("Backdrop");
        bool created = child == null;
        GameObject backdrop = created ? new GameObject("Backdrop") : child.gameObject;
        if (created)
        {
            backdrop.transform.SetParent(parent, false);
            backdrop.transform.localPosition = Vector3.zero;
            backdrop.transform.localRotation = Quaternion.identity;
            backdrop.transform.localScale = scale;
        }

        SpriteRenderer renderer = backdrop.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = backdrop.AddComponent<SpriteRenderer>();
            renderer.color = new Color(1f, 1f, 1f, 0.92f);
            renderer.sortingOrder = sortingOrder;
        }

        if (renderer.sprite == null)
        {
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Spritesheet/square.png");
        }
        if (renderer.sortingOrder == 0)
        {
            renderer.sortingOrder = sortingOrder;
        }
    }

    private static SpriteRenderer EnsureWorldSprite(GameObject obj, Color color, int sortingOrder, bool forceColor)
    {
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = obj.AddComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }
        else
        {
            if (forceColor)
            {
                renderer.color = color;
            }
            if (renderer.sortingOrder == 0)
            {
                renderer.sortingOrder = sortingOrder;
            }
        }
        return renderer;
    }

    private static void EnsureAdvancedBriefingOverlay(Transform root, string name, string title, string body, string buttonLabel)
    {
        GameObject overlay = EnsureAdvancedFullscreenTransparentPanel(root, name);
        GameObject card = EnsureAdvancedCardPanel(overlay.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 500f));
        HideChildIfPresent(card.transform, "Stats");
        HideChildIfPresent(card.transform, "Retry");
        HideChildIfPresent(card.transform, "Back");
        EnsureAdvancedImageSlot(card.transform, "HeroImage", new Vector2(0.3f, 0.54f), Vector2.zero, new Vector2(260f, 250f));
        EnsureEditableText(card.transform, "Title", title, new Vector2(0.66f, 0.78f), Vector2.zero, new Vector2(300f, 62f), 34, FontStyle.Bold, Color.white);
        EnsureEditableText(card.transform, "Body", body, new Vector2(0.66f, 0.55f), Vector2.zero, new Vector2(300f, 116f), 22, FontStyle.Bold, new Color(0.8f, 0.95f, 1f));
        GameObject slots = EnsureRect(card.transform, "ControlImageSlots", new Vector2(0.66f, 0.34f), Vector2.zero, new Vector2(300f, 84f));
        EnsureAdvancedImageSlot(slots.transform, "Slot_Jump", new Vector2(0.2f, 0.5f), Vector2.zero, new Vector2(58f, 58f));
        EnsureAdvancedImageSlot(slots.transform, "Slot_Slide", new Vector2(0.4f, 0.5f), Vector2.zero, new Vector2(58f, 58f));
        EnsureAdvancedImageSlot(slots.transform, "Slot_LeftRight", new Vector2(0.6f, 0.5f), Vector2.zero, new Vector2(58f, 58f));
        EnsureAdvancedImageSlot(slots.transform, "Slot_Beat", new Vector2(0.8f, 0.5f), Vector2.zero, new Vector2(58f, 58f));
        EnsureAdvancedButton(card.transform, "StartButton", buttonLabel, new Vector2(0.66f, 0.13f), Vector2.zero, new Vector2(240f, 62f));
        overlay.SetActive(false);
    }

    private static void EnsureAdvancedResultOverlay(Transform root)
    {
        GameObject overlay = EnsureAdvancedFullscreenTransparentPanel(root, "AdvancedRunnerResult");
        GameObject card = EnsureAdvancedCardPanel(overlay.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 500f));
        EnsureAdvancedImageSlot(card.transform, "HeroImage", new Vector2(0.28f, 0.55f), Vector2.zero, new Vector2(240f, 240f));
        EnsureEditableText(card.transform, "Title", "Result", new Vector2(0.66f, 0.82f), Vector2.zero, new Vector2(320f, 52f), 34, FontStyle.Bold, Color.white);
        GameObject oldStats = EnsureRect(card.transform, "Stats", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(520f, 210f));
        oldStats.SetActive(false);
        HideChildIfPresent(card.transform, "StartButton");
        EnsureAdvancedResultRow(card.transform, "Status", "STATUS", "READY", new Vector2(0.66f, 0.67f));
        EnsureAdvancedResultRow(card.transform, "Score", "SCORE", "0", new Vector2(0.66f, 0.55f));
        EnsureAdvancedResultRow(card.transform, "Perfect", "WOW", "0", new Vector2(0.66f, 0.43f));
        EnsureAdvancedResultRow(card.transform, "Good", "GOOD", "0", new Vector2(0.66f, 0.31f));
        EnsureAdvancedResultRow(card.transform, "Miss", "MISS", "0", new Vector2(0.66f, 0.19f));
        EnsureAdvancedResultRow(card.transform, "MaxCombo", "COMBO", "0", new Vector2(0.66f, 0.07f));
        EnsureAdvancedButton(card.transform, "Retry", "Retry", new Vector2(0.36f, 0.13f), Vector2.zero, new Vector2(150f, 52f));
        EnsureAdvancedButton(card.transform, "Back", "Back", new Vector2(0.18f, 0.13f), Vector2.zero, new Vector2(130f, 52f));
        overlay.SetActive(false);
    }

    private static void EnsureAdvancedHudCounter(Transform parent, string name, string label, Vector2 anchor, Vector2 position)
    {
        GameObject counter = EnsureAdvancedTransparentPanel(parent, name, anchor, position, new Vector2(132f, 50f));
        EnsureAdvancedImageSlot(counter.transform, "Icon", new Vector2(0.13f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
        EnsureAdvancedImageSlot(counter.transform, "LabelImage", new Vector2(0.43f, 0.5f), Vector2.zero, new Vector2(58f, 30f));
        EnsureEditableText(counter.transform, "Label", label, new Vector2(0.43f, 0.5f), Vector2.zero, new Vector2(58f, 30f), 13, FontStyle.Bold, Color.white);
        EnsureEditableText(counter.transform, "Value", "0", new Vector2(0.79f, 0.5f), Vector2.zero, new Vector2(44f, 34f), 18, FontStyle.Bold, new Color(1f, 0.86f, 0.18f));
    }

    private static void EnsureAdvancedResultRow(Transform parent, string name, string label, string value, Vector2 anchor)
    {
        GameObject row = EnsureAdvancedTransparentPanel(parent, name, anchor, Vector2.zero, new Vector2(330f, 46f));
        EnsureAdvancedImageSlot(row.transform, "Icon", new Vector2(0.09f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
        EnsureAdvancedImageSlot(row.transform, "LabelImage", new Vector2(0.36f, 0.5f), Vector2.zero, new Vector2(126f, 30f));
        EnsureEditableText(row.transform, "Label", label, new Vector2(0.36f, 0.5f), Vector2.zero, new Vector2(126f, 30f), 15, FontStyle.Bold, Color.white);
        EnsureEditableText(row.transform, "Value", value, new Vector2(0.78f, 0.5f), Vector2.zero, new Vector2(96f, 34f), 20, FontStyle.Bold, new Color(1f, 0.86f, 0.18f));
    }

    private static GameObject EnsureAdvancedFullscreenTransparentPanel(Transform parent, string name)
    {
        GameObject obj = EnsureFullscreenRect(parent, name);
        MakeAdvancedTransparentPanel(obj);
        return obj;
    }

    private static GameObject EnsureAdvancedTransparentPanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject obj = EnsureRect(parent, name, anchor, position, size);
        MakeAdvancedTransparentPanel(obj);
        return obj;
    }

    private static GameObject EnsureAdvancedCardPanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        return EnsureAdvancedVisibleImagePanel(parent, name, anchor, position, size);
    }

    private static GameObject EnsureAdvancedVisibleImagePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject obj = EnsureRect(parent, name, anchor, position, size);
        Image image = obj.GetComponent<Image>();
        if (image == null)
        {
            image = obj.AddComponent<Image>();
        }
        image.color = Color.white;
        image.raycastTarget = false;
        return obj;
    }

    private static void MakeAdvancedTransparentPanel(GameObject obj)
    {
        Image image = obj.GetComponent<Image>();
        if (image == null)
        {
            image = obj.AddComponent<Image>();
        }
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = false;
    }

    private static Image EnsureAdvancedImageSlot(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject obj = EnsureRect(parent, name, anchor, position, size);
        Image image = obj.GetComponent<Image>();
        if (image == null)
        {
            image = obj.AddComponent<Image>();
        }

        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static void EnsureAdvancedBeatVisuals(Transform beat)
    {
        bool pulseCreated;
        Image pulse = EnsureAdvancedBeatImageSlot(beat, "Pulse", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92f, 92f), out pulseCreated);
        if (pulseCreated)
        {
            pulse.color = new Color(1f, 1f, 1f, 0.18f);
        }

        for (int i = 0; i < 4; i++)
        {
            bool dotCreated;
            Image dot = EnsureAdvancedBeatImageSlot(beat, "BeatDot_" + i, new Vector2(0.5f, 0.5f), new Vector2(-54f + i * 36f, -34f), new Vector2(24f, 24f), out dotCreated);
            if (dotCreated)
            {
                dot.color = new Color(1f, 1f, 1f, i == 0 ? 0.92f : 0.42f);
            }
        }
    }

    private static Image EnsureAdvancedBeatImageSlot(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, out bool createdImage)
    {
        GameObject obj = EnsureRect(parent, name, anchor, position, size);
        Image image = obj.GetComponent<Image>();
        createdImage = image == null;
        if (image == null)
        {
            image = obj.AddComponent<Image>();
            image.color = Color.white;
        }

        image.raycastTarget = false;
        return image;
    }

    private static Text EnsureEditableText(Transform parent, string name, string defaultValue, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        GameObject obj = EnsureRect(parent, name, anchor, position, size);
        Text text = obj.GetComponent<Text>();
        if (text == null)
        {
            text = obj.AddComponent<Text>();
            text.text = defaultValue;
        }
        else if (string.IsNullOrEmpty(text.text))
        {
            text.text = defaultValue;
        }

        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 8;
        text.resizeTextMaxSize = fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static Button EnsureAdvancedButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject obj = EnsurePanel(parent, name, anchor, position, size, new Color(1f, 1f, 1f, 0.92f));
        Button button = obj.GetComponent<Button>();
        if (button == null)
        {
            button = obj.AddComponent<Button>();
        }

        button.targetGraphic = obj.GetComponent<Image>();
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        EnsureEditableText(obj.transform, "Text", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 20, FontStyle.Bold, new Color(0.02f, 0.12f, 0.18f));
        return button;
    }

    private static void HideChildIfPresent(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            child.gameObject.SetActive(false);
        }
    }

    private static void EnsureModeCard(Transform parent, string name, string title, string badge, string body, Color color)
    {
        GameObject card = EnsurePanel(parent, name, Vector2.zero, Vector2.zero, new Vector2(320f, 250f), new Color(1f, 1f, 1f, 0.94f));
        LayoutElement element = card.GetComponent<LayoutElement>() ?? card.AddComponent<LayoutElement>();
        element.preferredWidth = 320f;
        element.preferredHeight = 250f;
        GameObject badgeObj = EnsurePanel(card.transform, "Badge", new Vector2(0.5f, 0.83f), Vector2.zero, new Vector2(260f, 54f), color);
        EnsureText(badgeObj.transform, "Text", badge, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 44f), 24, FontStyle.Bold, Color.white);
        EnsureText(card.transform, "Title", title, new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(280f, 54f), 28, FontStyle.Bold, new Color(0.05f, 0.12f, 0.18f));
        EnsureText(card.transform, "Body", body, new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(280f, 80f), 20, FontStyle.Normal, new Color(0.16f, 0.23f, 0.28f));
        if (card.GetComponent<Button>() == null) card.AddComponent<Button>().targetGraphic = card.GetComponent<Image>();
    }

    private static bool EnsureMissingModeCard(Transform parent, string name, string title, string badge, string body, Color color)
    {
        Transform existing = parent.Find(name);
        if (existing == null)
        {
            EnsureModeCard(parent, name, title, badge, body, color);
            return true;
        }

        Button button = existing.GetComponent<Button>();
        if (button == null)
        {
            button = existing.gameObject.AddComponent<Button>();
            Graphic graphic = existing.GetComponent<Graphic>();
            if (graphic != null)
            {
                button.targetGraphic = graphic;
            }
        }
        return false;
    }

    private static void SetModeCardDefaultPosition(Transform parent, string name, Vector2 position)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            return;
        }

        RectTransform rect = child.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchoredPosition = position;
    }

    private static void RemoveHorizontalLayout(GameObject obj)
    {
        HorizontalLayoutGroup layout = obj.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            return;
        }

        Object.DestroyImmediate(layout);
    }

    private static void EnsureWorldMusicItem(Transform parent, string name)
    {
        GameObject item = EnsureMissingRect(parent, name, Vector2.zero, Vector2.zero, Vector2.zero);
        GameObject music = EnsureMissingRect(item.transform, "Music", Vector2.zero, Vector2.zero, Vector2.zero);
        AudioSource source = music.GetComponent<AudioSource>();
        if (source == null)
        {
            source = music.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
        }

        GameObject visualRoot = EnsureMissingRect(item.transform, "VisualRoot", Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform visualRect = visualRoot.GetComponent<RectTransform>();
        if (visualRect != null)
        {
            visualRect.anchorMin = Vector2.zero;
            visualRect.anchorMax = Vector2.one;
            visualRect.anchoredPosition = Vector2.zero;
            visualRect.sizeDelta = Vector2.zero;
        }
        CanvasGroup group = visualRoot.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = visualRoot.AddComponent<CanvasGroup>();
            group.alpha = 1f;
        }
        group.interactable = false;
        group.blocksRaycasts = false;

        EnsureWorldMusicVisualLayer(visualRoot.transform, "Background", new Vector2(0.5f, 0.5f), new Vector2(1200f, 650f), new Color(0.18f, 0.22f, 0.36f, 1f));
        EnsureWorldMusicVisualLayer(visualRoot.transform, "Pattern", new Vector2(0.5f, 0.5f), new Vector2(920f, 420f), new Color(1f, 1f, 1f, 0.16f));
        EnsureWorldMusicVisualLayer(visualRoot.transform, "Landmark", new Vector2(0.5f, 0.46f), new Vector2(520f, 300f), new Color(1f, 1f, 1f, 0.38f));
        EnsureWorldMusicVisualLayer(visualRoot.transform, "Instrument", new Vector2(0.5f, 0.32f), new Vector2(240f, 180f), new Color(1f, 0.94f, 0.68f, 0.55f));
        EnsureWorldMusicVisualLayer(visualRoot.transform, "Foreground", new Vector2(0.5f, 0.12f), new Vector2(1280f, 120f), new Color(0f, 0f, 0f, 0.18f));

        EnsureMissingText(item.transform, "Label", "123", new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(720f, 52f), 28, FontStyle.Bold, Color.white);
        EnsureMissingText(item.transform, "Description", "123", new Vector2(0.5f, 0.69f), Vector2.zero, new Vector2(720f, 48f), 20, FontStyle.Normal, new Color(1f, 0.94f, 0.68f));
    }

    private static void EnsureWorldMusicVisualLayer(Transform parent, string name, Vector2 anchor, Vector2 size, Color color)
    {
        GameObject layer = EnsureMissingRect(parent, name, anchor, Vector2.zero, size);
        Image image = EnsureMissingImage(layer, color);
        image.raycastTarget = false;
    }

    private static void EnsureCoverImageSlot(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        bool created = parent.Find(name) == null;
        GameObject slot = EnsureMissingRect(parent, name, anchor, position, size);
        if (created)
        {
            Image image = EnsureMissingImage(slot, color);
            image.raycastTarget = false;
            CanvasGroup group = slot.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = slot.AddComponent<CanvasGroup>();
            }
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        else
        {
            Image image = slot.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }
            CanvasGroup group = slot.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }
    }

    private static void DisableCoverStageRaycasts(Transform root)
    {
        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            images[i].raycastTarget = false;
        }

        CanvasGroup[] groups = root.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            groups[i].interactable = false;
            groups[i].blocksRaycasts = false;
        }
    }

    private static void EnsureStartPanel(Transform root, string name, string title, string body)
    {
        bool overlayCreated = root.Find(name) == null;
        GameObject overlay = EnsureFullscreenMissingPanel(root, name, new Color(0f, 0f, 0f, 0.55f));
        bool cardCreated = overlay.transform.Find("Card") == null;
        GameObject card = EnsureMissingPanel(overlay.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 480f), new Color(0.05f, 0.29f, 0.42f, 0.98f));
        EnsureMissingText(card.transform, "Title", title, new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(700f, 60f), 42, FontStyle.Bold, Color.white);
        if (!string.IsNullOrEmpty(body))
        {
            EnsureMissingText(card.transform, "Body", body, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(700f, 230f), 24, FontStyle.Normal, Color.white);
        }

        if (name == "RecordsPanel")
        {
            EnsureRecordsPanelContent(card.transform);
        }
        else if (name != "SettingsPanel")
        {
            EnsureMissingRect(card.transform, "Content", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(760f, 330f));
        }

        EnsureOptionalButton(card.transform, "CloseButton", "Close", new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(190f, 50f));
        if (overlayCreated || cardCreated)
        {
            overlay.SetActive(false);
        }
    }

    private static void EnsureSettingsPanel(Transform root)
    {
        EnsureStartPanel(root, "SettingsPanel", "Settings", "");
        Transform card = root.Find("SettingsPanel/Card");
        if (card == null)
        {
            return;
        }

        EnsureSettingsPanelContent(card);
    }

    private static void EnsureSettingsPanelContent(Transform card)
    {
        GameObject content = EnsureMissingRect(card, "SettingsContent", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(760f, 330f));
        EnsureMissingVerticalLayout(content, 8f);
        ReparentNamedChild(content.transform, "MasterVolumeRow");
        EnsureSettingRow(content.transform, "MasterVolumeRow", "Master Volume", true);
        EnsureSettingRow(content.transform, "MusicVolumeRow", "Music Volume", true);
        EnsureBeatPromptsRow(content.transform);
        ReconfigureSettingsSliders(content.transform);
        StartMenuAudioSettings.PrepareSettingsContent(content.transform);
    }

    private static void ReconfigureSettingsSliders(Transform content)
    {
        foreach (string rowName in new[] { "MasterVolumeRow", "MusicVolumeRow" })
        {
            Transform row = content.Find(rowName);
            if (row == null)
            {
                continue;
            }

            Transform sliderTransform = row.Find("Slider");
            Slider slider = sliderTransform != null ? sliderTransform.GetComponent<Slider>() : null;
            if (slider == null)
            {
                continue;
            }

            Transform fillArea = sliderTransform.Find("FillArea");
            Transform fill = fillArea != null ? fillArea.Find("Fill") : null;
            Transform handle = sliderTransform.Find("Handle");
            ConfigureSettingsSliderHierarchy(slider, fillArea, fill, handle);
        }
    }

    private static void EnsureBeatPromptsRow(Transform parent)
    {
        GameObject row = EnsureMissingRect(parent, "BeatPromptsRow", Vector2.zero, Vector2.zero, new Vector2(760f, 66f));
        EnsureMissingText(row.transform, "Label", "Show beat prompts", new Vector2(0.18f, 0.5f), Vector2.zero, new Vector2(260f, 56f), 22, FontStyle.Bold, Color.white);
        GameObject toggle = EnsureMissingRect(row.transform, "Toggle", new Vector2(0.68f, 0.5f), Vector2.zero, new Vector2(180f, 52f));
        GameObject hitArea = EnsureMissingRect(toggle.transform, "HitArea", Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform hitRect = hitArea.GetComponent<RectTransform>();
        hitRect.anchorMin = Vector2.zero;
        hitRect.anchorMax = Vector2.one;
        hitRect.anchoredPosition = Vector2.zero;
        hitRect.sizeDelta = Vector2.zero;
        if (hitArea.GetComponent<Button>() == null)
        {
            hitArea.AddComponent<Button>();
        }
        Image hitImage = hitArea.GetComponent<Image>();
        if (hitImage == null)
        {
            hitImage = hitArea.AddComponent<Image>();
        }
        hitImage.color = new Color(1f, 1f, 1f, 0.01f);
        hitImage.raycastTarget = true;

        GameObject onState = EnsureMissingPanel(toggle.transform, "OnState", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 44f), new Color(0.2f, 0.75f, 0.35f, 1f));
        EnsureMissingText(onState.transform, "Label", "ON", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72f, 36f), 20, FontStyle.Bold, Color.white);
        GameObject offState = EnsureMissingPanel(toggle.transform, "OffState", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 44f), new Color(0.45f, 0.45f, 0.45f, 1f));
        EnsureMissingText(offState.transform, "Label", "OFF", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72f, 36f), 20, FontStyle.Bold, Color.white);
        foreach (Graphic graphic in toggle.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.gameObject != hitArea)
            {
                graphic.raycastTarget = false;
            }
        }
    }

    private static void ReparentNamedChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return;
        }

        Transform searchRoot = parent.parent != null ? parent.parent : parent;
        foreach (Transform t in searchRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName)
            {
                t.SetParent(parent, false);
                return;
            }
        }
    }

    private static void EnsureMissingVerticalLayout(GameObject obj, float spacing)
    {
        if (obj == null)
        {
            return;
        }

        VerticalLayoutGroup layout = obj.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = obj.AddComponent<VerticalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = spacing;
        layout.padding = new RectOffset(8, 8, 8, 8);
    }

    private static void EnsureSettingRow(Transform parent, string name, string label, bool slider)
    {
        GameObject row = EnsureMissingRect(parent, name, Vector2.zero, Vector2.zero, new Vector2(760f, 66f));
        EnsureMissingText(row.transform, "Label", label, new Vector2(0.18f, 0.5f), Vector2.zero, new Vector2(260f, 56f), 22, FontStyle.Bold, Color.white);
        if (slider)
        {
            GameObject sliderObj = EnsureMissingPanel(row.transform, "Slider", new Vector2(0.68f, 0.5f), Vector2.zero, new Vector2(420f, 40f), new Color(1f, 1f, 1f, 0.25f));
            Slider sliderComponent = sliderObj.GetComponent<Slider>();
            if (sliderComponent == null)
            {
                sliderComponent = sliderObj.AddComponent<Slider>();
            }

            GameObject fillArea = EnsureMissingRect(sliderObj.transform, "FillArea", Vector2.zero, Vector2.zero, new Vector2(400f, 30f));
            GameObject fill = EnsureMissingPanel(fillArea.transform, "Fill", Vector2.zero, Vector2.zero, new Vector2(400f, 30f), new Color(1f, 1f, 1f, 0.55f));
            GameObject handle = EnsureMissingRect(sliderObj.transform, "Handle", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(32f, 48f));
            EnsureMissingImage(handle, Color.white);
            ConfigureSettingsSliderHierarchy(sliderComponent, fillArea.transform, fill.transform, handle.transform);
        }
    }

    private static void ConfigureSettingsSliderHierarchy(Slider slider, Transform fillArea, Transform fill, Transform handle)
    {
        if (slider == null)
        {
            return;
        }

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
        }

        if (handle != null)
        {
            slider.handleRect = handle as RectTransform;
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

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = Mathf.Max(slider.value, 0.01f);
        if (slider.value <= 0f)
        {
            slider.value = 0.85f;
        }
    }

    private static void EnsureRecordsPanelContent(Transform card)
    {
        GameObject content = EnsureMissingRect(card, "RecordsContent", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(760f, 330f));
        EnsureMissingVerticalLayout(content, 6f);
        EnsureMissingText(
            content.transform,
            "IntroText",
            "Best scores from Rhythm Runner and Advanced Runner. Play a run to add a row.",
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(720f, 48f),
            20,
            FontStyle.Normal,
            new Color(0.92f, 0.96f, 1f, 1f));
        EnsureStartRecordTemplates(content.transform);
    }

    private static void EnsureStartRecordTemplates(Transform content)
    {
        GameObject section = EnsureMissingRect(content, "SectionTemplate", Vector2.zero, Vector2.zero, new Vector2(760f, 38f));
        EnsureMissingText(section.transform, "Value", "Section", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 34f), 27, FontStyle.Bold, new Color(1f, 0.9f, 0.52f));
        section.SetActive(false);

        GameObject item = EnsureMissingRect(content, "RecordItemTemplate", Vector2.zero, Vector2.zero, new Vector2(760f, 34f));
        EnsureMissingText(item.transform, "Value", "0", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 30f), 23, FontStyle.Normal, Color.white);
        item.SetActive(false);
    }

    private static GameObject EnsureCanvasRoot(string name, int sortingOrder)
    {
        GameObject canvasObject = GameObject.Find(name);
        if (canvasObject == null)
        {
            canvasObject = new GameObject(name, typeof(RectTransform));
        }
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        if (canvasObject.GetComponent<GraphicRaycaster>() == null) canvasObject.AddComponent<GraphicRaycaster>();
        return canvasObject;
    }

    private static GameObject EnsureRoot(string name)
    {
        GameObject obj = GameObject.Find(name);
        return obj != null ? obj : new GameObject(name);
    }

    private static GameObject EnsureFullscreenRect(Transform parent, string name)
    {
        GameObject obj = EnsureRect(parent, name, Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return obj;
    }

    private static GameObject EnsureFullscreenPanel(Transform parent, string name, Color color)
    {
        GameObject obj = EnsureFullscreenRect(parent, name);
        EnsureImage(obj, color);
        return obj;
    }

    private static GameObject EnsureFullscreenMissingPanel(Transform parent, string name, Color color)
    {
        GameObject obj = EnsureMissingRect(parent, name, Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null && rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.zero && rect.sizeDelta == Vector2.zero)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }
        EnsureMissingImage(obj, color);
        return obj;
    }

    private static GameObject EnsurePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = EnsureRect(parent, name, anchor, position, size);
        EnsureImage(obj, color);
        return obj;
    }

    private static GameObject EnsureMissingPanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = EnsureMissingRect(parent, name, anchor, position, size);
        EnsureMissingImage(obj, color);
        return obj;
    }

    private static GameObject EnsureRect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Transform child = parent.Find(name);
        GameObject obj = child != null ? child.gameObject : new GameObject(name, typeof(RectTransform));
        if (child == null) obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogWarning("AllSceneHierarchyBaker: '" + name + "' exists without RectTransform. Leaving transform unchanged; recreate it manually if UI editing behaves incorrectly.");
            return obj;
        }
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return obj;
    }

    private static Text EnsureText(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        GameObject obj = EnsureRect(parent, name, anchor, position, size);
        Text text = obj.GetComponent<Text>();
        if (text == null)
        {
            text = obj.AddComponent<Text>();
        }
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject EnsureMissingRect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return obj;
    }

    private static Text EnsureMissingText(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        GameObject obj = EnsureMissingRect(parent, name, anchor, position, size);
        Text text = obj.GetComponent<Text>();
        if (text != null)
        {
            return text;
        }

        text = obj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static Text EnsureBucketCounter(Transform parent, string name, string label, string value, Vector2 anchor)
    {
        GameObject counter = EnsureMissingRect(parent, name, anchor, Vector2.zero, new Vector2(142f, 48f));
        EnsureMissingText(counter.transform, "Label", label, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(132f, 20f), 15, FontStyle.Bold, new Color(0.78f, 0.95f, 1f));
        return EnsureMissingText(counter.transform, "Value", value, new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(132f, 24f), 22, FontStyle.Bold, Color.white);
    }

    private static void EnsureBucketSlot(Transform parent, string name, Vector2 anchor)
    {
        GameObject slot = EnsureMissingPanel(parent, name, anchor, Vector2.zero, new Vector2(82f, 82f), new Color(0.78f, 0.95f, 1f, 0.16f));
        if (slot.GetComponent<OceanBucketSlot>() == null)
        {
            slot.AddComponent<OceanBucketSlot>();
        }
        EnsureMissingImage(EnsureMissingRect(slot.transform, "Highlight", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(82f, 82f)), new Color(1f, 0.86f, 0.18f, 0.62f)).raycastTarget = false;
        Transform highlight = slot.transform.Find("Highlight");
        if (highlight != null)
        {
            highlight.gameObject.SetActive(false);
        }
        EnsureMissingImage(EnsureMissingRect(slot.transform, "Decoration", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50f, 50f)), Color.white).raycastTarget = false;
        EnsureMissingText(slot.transform, "Label", SlotDefaultLabel(name), new Vector2(0.5f, 0f), new Vector2(0f, -20f), new Vector2(110f, 24f), 13, FontStyle.Bold, new Color(0.78f, 0.95f, 1f, 0.76f));
    }

    private static string SlotDefaultLabel(string name)
    {
        if (name == "TopSlot") return "Top";
        if (name == "LeftSlot") return "Left";
        if (name == "RightSlot") return "Right";
        if (name == "CharmSlot") return "Charm";
        return "Front";
    }

    private static void EnsureDecorationTemplate(Transform parent, string name, Color background)
    {
        GameObject item = EnsureMissingPanel(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(132f, 94f), background);
        if (item.GetComponent<Button>() == null)
        {
            Button button = item.AddComponent<Button>();
            button.targetGraphic = item.GetComponent<Image>();
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }
        EnsureMissingImage(EnsureMissingRect(item.transform, "SelectedBadge", new Vector2(0.14f, 0.82f), Vector2.zero, new Vector2(24f, 24f)), new Color(1f, 0.86f, 0.18f, 0.95f));
        EnsureMissingImage(EnsureMissingRect(item.transform, "EquippedBadge", new Vector2(0.86f, 0.82f), Vector2.zero, new Vector2(24f, 24f)), new Color(0.27f, 0.95f, 0.54f, 0.95f));
        EnsureMissingImage(EnsureMissingRect(item.transform, "LockBadge", new Vector2(0.86f, 0.82f), Vector2.zero, new Vector2(24f, 24f)), new Color(1f, 0.94f, 0.68f, 0.95f));
        Image icon = EnsureMissingImage(EnsureMissingRect(item.transform, "Icon", new Vector2(0.26f, 0.54f), Vector2.zero, new Vector2(48f, 48f)), Color.white);
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        EnsureMissingText(item.transform, "Name", "Decoration", new Vector2(0.66f, 0.68f), Vector2.zero, new Vector2(78f, 24f), 14, FontStyle.Bold, new Color(0.02f, 0.15f, 0.22f));
        EnsureMissingText(item.transform, "Status", "Use", new Vector2(0.66f, 0.42f), Vector2.zero, new Vector2(78f, 22f), 12, FontStyle.Bold, new Color(0.12f, 0.28f, 0.48f));
        GameObject progress = EnsureMissingPanel(item.transform, "Progress", new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(104f, 12f), new Color(1f, 1f, 1f, 0.16f));
        Image fill = EnsureMissingImage(EnsureMissingRect(progress.transform, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 12f)), new Color(0.27f, 0.95f, 0.54f, 1f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        EnsureMissingText(progress.transform, "Value", "", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 18f), 10, FontStyle.Bold, Color.white);
        EnsureMissingText(item.transform, "Requirement", "", new Vector2(0.5f, 0.02f), Vector2.zero, new Vector2(116f, 18f), 10, FontStyle.Bold, Color.white);
        item.SetActive(false);
    }

    private static void EnsureDecorationDetail(Transform parent)
    {
        GameObject detail = EnsureMissingPanel(parent, "DecorationDetail", new Vector2(0.82f, 0.49f), Vector2.zero, new Vector2(280f, 410f), new Color(1f, 1f, 1f, 0.08f));
        Image icon = EnsureMissingImage(EnsureMissingRect(detail.transform, "Icon", new Vector2(0.5f, 0.8f), Vector2.zero, new Vector2(82f, 82f)), Color.white);
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        EnsureMissingText(detail.transform, "Name", "Decoration", new Vector2(0.5f, 0.63f), Vector2.zero, new Vector2(240f, 34f), 25, FontStyle.Bold, Color.white);
        EnsureMissingText(detail.transform, "Status", "Tap a decoration", new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(240f, 28f), 18, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        GameObject progress = EnsureMissingPanel(detail.transform, "Progress", new Vector2(0.5f, 0.43f), Vector2.zero, new Vector2(210f, 20f), new Color(1f, 1f, 1f, 0.16f));
        Image fill = EnsureMissingImage(EnsureMissingRect(progress.transform, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210f, 20f)), new Color(0.27f, 0.95f, 0.54f, 1f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        EnsureMissingText(progress.transform, "Value", "", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210f, 22f), 14, FontStyle.Bold, Color.white);
        GameObject requirement = EnsureMissingRect(detail.transform, "Requirement", new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(230f, 58f));
        EnsureMissingImage(EnsureMissingRect(requirement.transform, "Icon", new Vector2(0.16f, 0.5f), Vector2.zero, new Vector2(34f, 34f)), Color.white).raycastTarget = false;
        EnsureMissingText(requirement.transform, "Text", "Choose a decoration", new Vector2(0.62f, 0.5f), Vector2.zero, new Vector2(170f, 50f), 16, FontStyle.Bold, Color.white);
        EnsureOptionalButton(detail.transform, "ActionButton", "Use", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(180f, 54f));
        EnsureMissingText(detail.transform, "Hint", "Tap a decoration to see how to unlock it.", new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(240f, 42f), 15, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
    }

    private static Button EnsureButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject obj = EnsurePanel(parent, name, anchor, position, size, new Color(1f, 1f, 1f, 0.92f));
        Button button = obj.GetComponent<Button>();
        if (button == null)
        {
            button = obj.AddComponent<Button>();
        }
        button.targetGraphic = obj.GetComponent<Image>();
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        EnsureText(obj.transform, "Text", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 20, FontStyle.Bold, new Color(0.02f, 0.12f, 0.18f));
        return button;
    }

    private static Button EnsureOptionalButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Button existingButton = existing.GetComponent<Button>();
            if (existingButton == null)
            {
                existingButton = existing.gameObject.AddComponent<Button>();
                Graphic graphic = existing.GetComponent<Graphic>();
                if (graphic != null)
                {
                    existingButton.targetGraphic = graphic;
                }
            }
            Navigation navigation = existingButton.navigation;
            navigation.mode = Navigation.Mode.None;
            existingButton.navigation = navigation;
            return existingButton;
        }

        return EnsureButton(parent, name, label, anchor, position, size);
    }

    private static void EnsureBeatDots(Transform beatLane)
    {
        for (int i = 0; i < 4; i++)
        {
            string name = "BeatDot_" + i;
            Transform existing = beatLane.Find(name);
            GameObject dot;
            if (existing != null)
            {
                dot = existing.gameObject;
            }
            else
            {
                dot = new GameObject(name, typeof(RectTransform));
                dot.transform.SetParent(beatLane, false);
                RectTransform rect = dot.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(-63f + i * 42f, 0f);
                rect.sizeDelta = new Vector2(32f, 32f);
            }

            if (dot.GetComponent<CanvasRenderer>() == null)
            {
                dot.AddComponent<CanvasRenderer>();
            }
            if (dot.GetComponent<Image>() == null)
            {
                Image image = dot.AddComponent<Image>();
                image.color = new Color(0.25f, 0.72f, 1f, 0.55f);
            }
        }
    }

    private static void EnsureVerticalControlRhythmPrompt(Transform bottom)
    {
        GameObject prompt = EnsureMissingRect(bottom, "ControlRhythmPrompt", new Vector2(0.5f, 0.5f), new Vector2(260f, 0f), new Vector2(420f, 130f));
        CanvasGroup group = prompt.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = prompt.AddComponent<CanvasGroup>();
            group.alpha = 0f;
        }
        group.interactable = false;
        group.blocksRaycasts = false;

        EnsureVerticalPromptColumn(prompt.transform, "SpaceColumn", "SpaceUp", "SpaceDown", new Vector2(0.125f, 0.5f));
        EnsureVerticalPromptColumn(prompt.transform, "DownColumn", "DownUp", "DownDown", new Vector2(0.375f, 0.5f));
        EnsureVerticalPromptColumn(prompt.transform, "LeftColumn", "LeftUp", "LeftDown", new Vector2(0.625f, 0.5f));
        EnsureVerticalPromptColumn(prompt.transform, "RightColumn", "RightUp", "RightDown", new Vector2(0.875f, 0.5f));
    }

    private static void EnsureAdvancedControlRhythmPrompt(Transform bottom)
    {
        GameObject prompt = EnsureMissingRect(bottom, "ControlRhythmPrompt", new Vector2(0.5f, 0.5f), new Vector2(260f, 0f), new Vector2(420f, 130f));
        CanvasGroup group = prompt.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = prompt.AddComponent<CanvasGroup>();
            group.alpha = 0f;
        }
        group.interactable = false;
        group.blocksRaycasts = false;

        EnsureVerticalPromptColumn(prompt.transform, "SpaceColumn", "SpaceUp", "SpaceDown", new Vector2(0.125f, 0.5f));
        EnsureVerticalPromptColumn(prompt.transform, "DownColumn", "DownUp", "DownDown", new Vector2(0.375f, 0.5f));
        EnsureVerticalPromptColumn(prompt.transform, "LeftColumn", "LeftUp", "LeftDown", new Vector2(0.625f, 0.5f));
        EnsureVerticalPromptColumn(prompt.transform, "RightColumn", "RightUp", "RightDown", new Vector2(0.875f, 0.5f));
    }

    private static void EnsureVerticalPromptColumn(Transform parent, string columnName, string upName, string downName, Vector2 anchor)
    {
        GameObject column = EnsureMissingRect(parent, columnName, anchor, Vector2.zero, new Vector2(86f, 96f));
        GameObject key = EnsureMissingRect(column.transform, "Key", new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(78f, 52f));
        GameObject hand = EnsureMissingRect(column.transform, "Hand", new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(54f, 42f));
        EnsureVerticalPromptImageSlot(key.transform, upName);
        EnsureVerticalPromptImageSlot(key.transform, downName);
        EnsureVerticalPromptImageSlot(hand.transform, "HandUp");
        EnsureVerticalPromptImageSlot(hand.transform, "HandDown");
    }

    private static void EnsureVerticalPromptImageSlot(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject slot = existing != null ? existing.gameObject : EnsureMissingRect(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform rect = slot.GetComponent<RectTransform>();
        if (existing == null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        Image image = slot.GetComponent<Image>();
        if (image == null)
        {
            image = slot.AddComponent<Image>();
            image.color = Color.white;
            image.preserveAspect = true;
        }
        image.raycastTarget = false;
        if (existing == null)
        {
            slot.SetActive(false);
        }
    }

    private static Image EnsureImage(GameObject obj, Color color)
    {
        Image image = obj.GetComponent<Image>();
        if (image == null)
        {
            image = obj.AddComponent<Image>();
        }
        image.color = color;
        return image;
    }

    private static Image EnsureMissingImage(GameObject obj, Color color)
    {
        Image image = obj.GetComponent<Image>();
        if (image == null)
        {
            image = obj.AddComponent<Image>();
            image.color = color;
        }
        return image;
    }

    private static HorizontalLayoutGroup EnsureLayout(GameObject obj, float spacing, TextAnchor alignment)
    {
        HorizontalLayoutGroup layout = obj.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = obj.AddComponent<HorizontalLayoutGroup>();
        }
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        return layout;
    }

    private static GameObject EnsureSpriteTemplate(Transform parent, string name, Vector3 scale, Color color, int sortingOrder, bool trigger)
    {
        Transform existing = parent.Find(name);
        bool created = existing == null;
        GameObject obj = EnsurePlainChild(parent, name);
        if (created)
        {
            obj.transform.localScale = scale;
        }
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = obj.AddComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }
        Collider2D collider = obj.GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = obj.AddComponent<BoxCollider2D>();
            collider.isTrigger = trigger;
        }
        obj.SetActive(false);
        return obj;
    }

    private static GameObject EnsureTextTemplate(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        bool created = existing == null;
        GameObject obj = EnsurePlainChild(parent, name);
        TextMesh text = obj.GetComponent<TextMesh>();
        if (text == null)
        {
            text = obj.AddComponent<TextMesh>();
            created = true;
        }
        if (created)
        {
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 42;
            text.characterSize = 0.045f;
            text.color = Color.white;
        }
        obj.SetActive(false);
        return obj;
    }

    private static GameObject EnsurePlainChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        GameObject obj = child != null ? child.gameObject : new GameObject(name);
        if (child == null) obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void SetChildrenInactive(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            parent.GetChild(i).gameObject.SetActive(false);
        }
    }

    private static void EnsureCamera()
    {
        Camera camera = Object.FindObjectOfType<Camera>();
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }
        camera.orthographic = true;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) return;
        GameObject obj = new GameObject("EventSystem");
        obj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        obj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    private static void Save(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureBuildSettingsScene(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == scenePath)
            {
                if (!scenes[i].enabled)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes.ToArray();
                }
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
