using UnityEngine;

// Scene-owned template registry for VerticalRunner generated world objects.
// VerticalRunner 生成世界对象的场景模板注册表。
//
// Designers assign player/platform/banana/parrot/finish visuals here; runtime clones them without restyling templates.
// 设计者在这里挂玩家、平台、香蕉、鹦鹉和终点视觉；runtime 克隆它们但不重设模板样式。

public class VerticalRunnerTemplates : MonoBehaviour
{
    public GameObject playerTemplate;
    public GameObject platformTemplate;
    public GameObject longPlatformTemplate;
    public GameObject coinTemplate;
    public GameObject obstacleTemplate;
    public GameObject finishTemplate;
    public GameObject worldLabelTemplate;
    public Transform runtimeRoot;

    public Transform RuntimeRoot
    {
        get { return runtimeRoot != null ? runtimeRoot : transform; }
    }

    public GameObject PlatformTemplateFor(bool longJump)
    {
        if (longJump && longPlatformTemplate != null)
        {
            return longPlatformTemplate;
        }

        return platformTemplate;
    }
}
