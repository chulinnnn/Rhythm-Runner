using UnityEngine;

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
}
