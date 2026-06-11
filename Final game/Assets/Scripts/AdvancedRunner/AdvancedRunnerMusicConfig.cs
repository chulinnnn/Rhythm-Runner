using UnityEngine;

// Hierarchy-owned music slot for AdvancedRunnerConfig/Music/*.
// AdvancedRunnerConfig/Music/* 下由 Hierarchy 管理的音乐槽位。
//
// Designers assign the AudioClip and BPM here. AdvancedRunnerManager reads these
// values when a stage starts; runtime does not overwrite the Inspector data.
// 设计时在这里挂 AudioClip 和 BPM。AdvancedRunnerManager 会在对应阶段开始时读取；
// 运行时不会覆盖 Inspector 中配置好的音乐数据。
public class AdvancedRunnerMusicConfig : MonoBehaviour
{
    public AudioClip bgm;
    public float bpm = 126f;
}
