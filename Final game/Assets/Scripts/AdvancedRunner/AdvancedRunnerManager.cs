using UnityEngine;

// Unity attaches this MonoBehaviour by filename. Runtime implementation lives in AdvancedRunner.cs.
// Unity 会按文件名挂载这个 MonoBehaviour；真正的运行逻辑写在 AdvancedRunner.cs 里。
//
// Core scene owner for AdvancedRunner. It boots the scene, chooses tutorial/game
// flow, builds the target chart, owns the beat clock, and judges player actions.
// AdvancedRunner 的场景主控：负责启动场景、教程/正式游戏流程、生成目标谱面、
// 管理节拍时钟，并处理玩家输入判定。
public partial class AdvancedRunnerManager : MonoBehaviour
{
}
