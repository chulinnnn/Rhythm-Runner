using UnityEngine;

// Unity attaches this MonoBehaviour by filename. Runtime implementation lives in AdvancedRunner.cs.
// Unity 会按文件名挂载这个 MonoBehaviour；真正的运行逻辑写在 AdvancedRunner.cs 里。
//
// Hierarchy UI binder for AdvancedRunnerCanvas. Runtime may update dynamic text,
// progress, beat highlights, prompt active states, and button listeners, but the
// scene owns static layout, sprites, colors, fonts, and panel styling.
// AdvancedRunnerCanvas 的 Hierarchy UI 绑定器：运行时只更新动态文本、进度、
// 节拍高亮、提示图标显隐状态和按钮事件；静态布局、图片、颜色、字体和面板样式归场景控制。
public partial class AdvancedRunnerUI : MonoBehaviour
{
}
