using UnityEngine;

// Hierarchy-owned feedback style config for AdvancedRunnerConfig/Feedback.
// AdvancedRunnerConfig/Feedback 下由 Hierarchy 管理的反馈样式配置。
//
// Designers can tune labels, colors, font, and pulse strength here. Runtime
// copies the values into settings, then uses them for dynamic feedback messages.
// 设计时可以在这里调整提示文字、颜色、字体和缩放强度。运行时只读取并复制到 settings，
// 再用于动态反馈显示。
public class AdvancedRunnerFeedbackConfig : MonoBehaviour
{
    public AdvancedFeedbackStyle feedback = new AdvancedFeedbackStyle();
}
