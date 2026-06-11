using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// EditMode scene contract tests for the current Unity project.
// 当前 Unity 项目的 EditMode 场景契约测试。
//
// These tests open saved scenes, check required hierarchy paths, and verify
// a few non-interactive UI prompt rules.
// They do not press gameplay keys, modify scenes, or depend on Play Mode timing.
// 这些测试会打开已保存场景，检查必要的 Hierarchy 路径，并验证少量
// “不挡点击”的 UI 提示规则。它们不会模拟玩法按键，不会修改场景，
// 也不依赖 Play Mode 的节奏时间。
//在不进入游戏运行的情况下，自动检查几个核心场景的关键层级和基础 UI 结构有没有被误删、误改或变成会挡点击的状态
public class SceneContractEditModeTests
{
    private const string StartScenePath = "Assets/Scenes/Start.unity";
    private const string OceanScenePath = "Assets/Scenes/OceanRhythm.unity";
    private const string VerticalScenePath = "Assets/Scenes/VerticalRunner.unity";
    private const string AdvancedScenePath = "Assets/Scenes/AdvancedRunner.unity";
    private const string WorldScenePath = "Assets/Scenes/WorldMusicExplorer.unity";

    // Verifies that all player-facing scenes are present in Build Settings.
    // 是否是5个scene
    [Test]
    public void BuildSettingsContainPlayableScenes()
    {
        string[] enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        Assert.Contains(StartScenePath, enabledScenes);
        Assert.Contains(OceanScenePath, enabledScenes);
        Assert.Contains(VerticalScenePath, enabledScenes);
        Assert.Contains(AdvancedScenePath, enabledScenes);
        Assert.Contains(WorldScenePath, enabledScenes);
    }

    // Verifies that the Start scene still contains the four mode cards and the
    // hierarchy-owned music decoration layer used by the menu.
    // 验证 Start 场景仍保留四张 mode 卡片，以及菜单使用的
    // hierarchy-owned music 装饰层。
    [Test]
    public void StartSceneHasFourModeCardsAndMusicLayer()
    {
        OpenScene(StartScenePath);

        AssertPath("StartMenuCanvas/Root");
        AssertPath("StartMenuCanvas/Root/music");
        AssertPath("StartMenuCanvas/Root/ModeRow/LittleRhythmOceanCard");
        AssertPath("StartMenuCanvas/Root/ModeRow/RhythmRunnerCard");
        AssertPath("StartMenuCanvas/Root/ModeRow/AdvancedRunnerCard");
        AssertPath("StartMenuCanvas/Root/ModeRow/WorldMusicExplorerCard");
        AssertPath("StartMenuCanvas/Root/SettingsPanel");
        AssertPath("StartMenuCanvas/Root/RecordsPanel");
        AssertPath("StartMenuCanvas/Root/AboutPanel");
    }

    // Verifies that OceanRhythm still keeps the key editable game UI roots,
    // including restored result overlays, beat cards, and bucket album paging.
    // 验证 OceanRhythm 仍保留关键的可编辑游戏 UI 根节点，
    // 包括恢复后的结果界面、节拍卡片和 bucket album 翻页控件。
    [Test]
    public void OceanRhythmSceneKeepsEditableGameUiRoots()
    {
        OpenScene(OceanScenePath);

        AssertPath("OceanRhythmCanvas/OceanRoot");
        AssertPath("OceanRhythmCanvas/OceanRoot/OceanAnimal");
        AssertPath("OceanRhythmCanvas/OceanRoot/CompleteOverlay");
        AssertPath("OceanRhythmCanvas/OceanRoot/PondCompleteOverlay");
        AssertPath("OceanRhythmCanvas/OceanRoot/BeatCardOverlay/Cards/IntroCard");
        AssertPath("OceanRhythmCanvas/OceanRoot/BeatCardOverlay/Cards/FourFourCard");
        AssertPath("OceanRhythmCanvas/OceanRoot/BeatCardOverlay/Cards/ThreeFourCard");
        AssertPath("OceanRhythmCanvas/OceanRoot/BeatCardOverlay/Cards/TwoFourCard");
        AssertPath("OceanRhythmCanvas/OceanRoot/BeatCardOverlay/Cards/SixEightCard");
        AssertPath("OceanRhythmCanvas/OceanRoot/BucketAlbumOverlay/Card/DecorationCollection/PrevPageButton");
        AssertPath("OceanRhythmCanvas/OceanRoot/BucketAlbumOverlay/Card/DecorationCollection/NextPageButton");
        AssertPath("OceanRhythmCanvas/OceanRoot/BucketAlbumOverlay/Card/DecorationCollection/PageText");
    }

    // Verifies that VerticalRunner's four-column rhythm prompt exists and that
    // its images remain non-raycast so it cannot block gameplay UI clicks.
    // 验证 VerticalRunner 的四列节奏提示存在，并且这些图片不接收 raycast，
    // 不会挡住游戏界面的点击。
    [Test]
    public void VerticalRunnerControlPromptColumnsExistAndDoNotBlockRaycasts()
    {
        OpenScene(VerticalScenePath);

        AssertControlPromptColumns("VerticalRunnerCanvas/BottomHud/ControlRhythmPrompt");
    }

    // Verifies that AdvancedRunner's four-column rhythm prompt exists and that
    // its images remain non-raycast so it cannot block gameplay UI clicks.
    // 验证 AdvancedRunner 的四列节奏提示存在，并且这些图片不接收 raycast，
    // 不会挡住游戏界面的点击。
    [Test]
    public void AdvancedRunnerControlPromptColumnsExistAndDoNotBlockRaycasts()
    {
        OpenScene(AdvancedScenePath);

        AssertControlPromptColumns("AdvancedRunnerCanvas/BottomHud/ControlRhythmPrompt");
    }

    // Verifies that WorldMusicExplorer keeps its editable scene contract:
    // root canvas, back button, now playing area, hint text, and items root.
    // 验证 WorldMusicExplorer 保留可编辑的场景契约：
    // 根画布、返回按钮、当前播放区、提示文字和 items 根节点。
    [Test]
    public void WorldMusicExplorerSceneHasEditableContentContract()
    {
        OpenScene(WorldScenePath);

        AssertPath("WorldMusicExplorerCanvas/Root");
        AssertPath("WorldMusicExplorerCanvas/Root/BackButton");
        AssertPath("WorldMusicExplorerCanvas/Root/NowPlaying");
        AssertPath("WorldMusicExplorerCanvas/Root/HintText");
        AssertPath("WorldMusicExplorerContent/Items");
    }

    // Opens a saved scene in EditMode before hierarchy assertions run.
    // 在 EditMode 中打开已保存场景，供后续层级断言使用。
    private static void OpenScene(string scenePath)
    {
        Assert.IsTrue(System.IO.File.Exists(scenePath), "Missing scene file: " + scenePath);
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
    }

    // Asserts that a hierarchy path exists in the active scene and returns the
    // matching GameObject for follow-up checks.
    // 断言当前活动场景中存在指定 hierarchy 路径，并返回对应的
    // GameObject 供后续检查使用。
    private static GameObject AssertPath(string path)
    {
        GameObject obj = FindSceneObjectByPath(path);
        Assert.IsNotNull(obj, "Missing required hierarchy path: " + path);
        return obj;
    }

    // Resolves a slash-separated hierarchy path by walking loaded scene roots
    // and direct child names, including inactive objects.
    // 通过遍历已加载场景的根对象和直接子节点名字，解析斜杠分隔的
    // hierarchy 路径；inactive 对象也可以被找到。
    private static GameObject FindSceneObjectByPath(string path)
    {
        string[] parts = path.Split('/');
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name != parts[0])
            {
                continue;
            }

            Transform current = root.transform;
            for (int i = 1; i < parts.Length && current != null; i++)
            {
                current = FindDirectChild(current, parts[i]);
            }

            if (current != null)
            {
                return current.gameObject;
            }
        }

        return null;
    }

    // Finds a direct child with the requested name under one parent transform.
    // 在指定父 Transform 下查找名字匹配的直接子节点。
    private static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    // Verifies the shared contract for a four-column rhythm prompt: the root
    // must not block raycasts, each column must contain Key and Hand branches,
    // and every prompt image must be non-raycast.
    // 验证四列节奏提示的共享契约：根节点不能阻挡 raycast，
    // 每一列都必须包含 Key 和 Hand 分支，且所有提示图片都不能接收 raycast。
    private static void AssertControlPromptColumns(string promptRootPath)
    {
        CanvasGroup group = AssertPath(promptRootPath).GetComponent<CanvasGroup>();
        Assert.IsNotNull(group, "ControlRhythmPrompt should have a CanvasGroup.");
        Assert.IsFalse(group.blocksRaycasts, "ControlRhythmPrompt must not block UI clicks.");

        string[] columns = { "SpaceColumn", "DownColumn", "LeftColumn", "RightColumn" };
        foreach (string column in columns)
        {
            AssertPath(promptRootPath + "/" + column + "/Key");
            AssertPath(promptRootPath + "/" + column + "/Hand");

            Image[] images = AssertPath(promptRootPath + "/" + column).GetComponentsInChildren<Image>(true);
            Assert.IsNotEmpty(images, "Prompt column should contain editable Image slots: " + column);

            foreach (Image image in images)
            {
                Assert.IsFalse(image.raycastTarget, image.name + " should not block pointer clicks.");
            }
        }
    }
}
