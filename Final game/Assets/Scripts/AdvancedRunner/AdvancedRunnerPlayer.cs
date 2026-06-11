using UnityEngine;

// Unity attaches this MonoBehaviour by filename. Runtime implementation lives in AdvancedRunner.cs.
// Unity 会按文件名挂载这个 MonoBehaviour；真正的运行逻辑写在 AdvancedRunner.cs 里。
//
// Player visual/execution component for AdvancedRunner. It applies lane changes,
// jump/slide poses, miss feedback movement, and resets; gameplay timing stays in
// AdvancedRunnerManager.
// AdvancedRunner 的玩家表现/执行组件：处理换道、跳跃/下滑姿态、失误反馈移动和重置；
// 节奏判定仍由 AdvancedRunnerManager 统一负责。
public partial class AdvancedRunnerPlayer : MonoBehaviour
{
}
