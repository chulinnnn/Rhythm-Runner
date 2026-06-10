using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneHierarchyBaker
{
    private const string StartScenePath = "Assets/Scenes/Start.unity";
    private const string OceanRhythmScenePath = "Assets/Scenes/OceanRhythm.unity";

    [MenuItem("Tools/Rhythm Runner/Rebuild Start + OceanRhythm Hierarchies")]
    public static void RebuildStartAndOceanRhythm()
    {
        string originalScenePath = SceneManager.GetActiveScene().path;

        RebuildStartScene();
        RebuildOceanRhythmScene();

        if (!string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath);
        }

        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Rhythm Runner/Rebuild Start Hierarchy")]
    public static void RebuildStartScene()
    {
        Scene scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
        StartMenuController controller = Object.FindObjectOfType<StartMenuController>();
        if (controller == null)
        {
            GameObject obj = new GameObject("StartMenuController");
            controller = obj.AddComponent<StartMenuController>();
        }

        controller.RebuildEditModeHierarchy();
        GameObject canvas = GameObject.Find("StartMenuCanvas");
        Transform root = canvas != null ? canvas.transform.Find("Root") : null;
        AllSceneHierarchyBaker.EnsureStartMusicDecorations(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/Rhythm Runner/Rebuild OceanRhythm Hierarchy")]
    public static void RebuildOceanRhythmScene()
    {
        Scene scene = EditorSceneManager.OpenScene(OceanRhythmScenePath, OpenSceneMode.Single);
        OceanRhythmManager manager = Object.FindObjectOfType<OceanRhythmManager>();
        if (manager == null)
        {
            GameObject obj = new GameObject("OceanRhythmManager");
            manager = obj.AddComponent<OceanRhythmManager>();
        }

        manager.RebuildEditModeHierarchy();
        GameObject canvas = GameObject.Find("OceanRhythmCanvas");
        Transform root = canvas != null ? canvas.transform.Find("OceanRoot") : null;
        if (root != null)
        {
            AllSceneHierarchyBaker.EnsureOceanBeatCardContract(root);
            AllSceneHierarchyBaker.EnsureOceanBucketAlbumContract(root);
        }
        AllSceneHierarchyBaker.EnsureOceanBucketAlbumConfig();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
