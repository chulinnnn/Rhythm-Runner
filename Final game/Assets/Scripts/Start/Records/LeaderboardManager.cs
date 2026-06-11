using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// EN: Score storage only. Start RecordsPanel owns presentation through hierarchy templates.
// ZH: 这里只负责分数存取；Start 的 RecordsPanel 通过 Hierarchy 模板负责显示样式。
public enum LeaderboardMode
{
    Easy,
    Hard
}

public static class LeaderboardManager
{
    private const int MaxEntries = 4;
    private const string EasyKeyPrefix = "Leaderboard_Easy_";
    private const string HardKeyPrefix = "Leaderboard_Hard_";
    private const string EasyCountKey = "Leaderboard_Easy_Count";
    private const string HardCountKey = "Leaderboard_Hard_Count";

    public static LeaderboardMode GetModeFromActiveScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Contains("AdvancedRunner"))
        {
            return LeaderboardMode.Hard;
        }
        return LeaderboardMode.Easy;
    }

    public static void SaveScore(LeaderboardMode mode, int distanceMeters)
    {
        if (distanceMeters <= 0)
        {
            return;
        }

        List<int> scores = GetScores(mode);
        scores.Add(distanceMeters);
        scores.Sort((a, b) => b.CompareTo(a));

        if (scores.Count > MaxEntries)
        {
            scores.RemoveRange(MaxEntries, scores.Count - MaxEntries);
        }

        string prefix = GetKeyPrefix(mode);
        string countKey = GetCountKey(mode);
        PlayerPrefs.SetInt(countKey, scores.Count);
        for (int i = 0; i < scores.Count; i++)
        {
            PlayerPrefs.SetInt(prefix + i, scores[i]);
        }
        PlayerPrefs.Save();
    }

    public static List<int> GetScores(LeaderboardMode mode)
    {
        List<int> scores = new List<int>();
        string prefix = GetKeyPrefix(mode);
        int count = PlayerPrefs.GetInt(GetCountKey(mode), 0);
        for (int i = 0; i < count && i < MaxEntries; i++)
        {
            scores.Add(PlayerPrefs.GetInt(prefix + i, 0));
        }
        scores.Sort((a, b) => b.CompareTo(a));
        return scores;
    }

    private static string GetKeyPrefix(LeaderboardMode mode)
    {
        return mode == LeaderboardMode.Easy ? EasyKeyPrefix : HardKeyPrefix;
    }

    private static string GetCountKey(LeaderboardMode mode)
    {
        return mode == LeaderboardMode.Easy ? EasyCountKey : HardCountKey;
    }
}
