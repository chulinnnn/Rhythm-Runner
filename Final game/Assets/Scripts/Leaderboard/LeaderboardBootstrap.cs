using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LeaderboardBootstrap
{
    private static bool isRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneCallback()
    {
        if (isRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        isRegistered = true;
        TrySetupLeaderboard(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySetupLeaderboard(scene);
    }

    private static void TrySetupLeaderboard(Scene scene)
    {
        if (!IsStartScene(scene.name))
        {
            return;
        }

        CleanupExistingLeaderboardUI();
        CreateLeaderboardUI();
    }

    private static bool IsStartScene(string sceneName)
    {
        return sceneName.Contains("Start");
    }

    private static void CleanupExistingLeaderboardUI()
    {
        LeaderboardUI[] existing = Object.FindObjectsOfType<LeaderboardUI>();
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null)
            {
                Object.Destroy(existing[i].gameObject);
            }
        }

        GameObject leftoverPanel = GameObject.Find("LeaderboardPanel");
        if (leftoverPanel != null)
        {
            Object.Destroy(leftoverPanel);
        }

        GameObject leftoverCanvas = GameObject.Find("LeaderboardCanvas");
        if (leftoverCanvas != null)
        {
            Object.Destroy(leftoverCanvas);
        }
    }

    private static void CreateLeaderboardUI()
    {
        if (FindPhbtn() == null)
        {
            return;
        }

        GameObject system = new GameObject("LeaderboardSystem");
        system.AddComponent<LeaderboardUI>();
    }

    public static GameObject FindPhbtn()
    {
        GameObject active = GameObject.Find("phbtn");
        if (active != null)
        {
            return active;
        }

        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.name == "phbtn" && buttons[i].gameObject.scene.isLoaded)
            {
                return buttons[i].gameObject;
            }
        }

        return null;
    }
}
