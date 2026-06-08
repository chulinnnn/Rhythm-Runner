using UnityEngine;

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
