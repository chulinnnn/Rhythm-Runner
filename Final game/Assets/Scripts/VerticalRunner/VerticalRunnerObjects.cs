using UnityEngine;

public enum VerticalBranchChoice
{
    None,
    Left,
    Right
}

public class VerticalRunnerPlatform : MonoBehaviour
{
    public int beatIndex;
    public bool strongBeat;
    public bool longJump;
    public bool isSafePlatform = true;
    public bool isDangerBranchPlatform;
    public VerticalRunnerPlatform defaultNext;
    public VerticalRunnerPlatform leftNext;
    public VerticalRunnerPlatform rightNext;
    public bool requiresDirectionalChoice;
    public int actionBeatIndex = -1;
    public VerticalBranchChoice safeChoice = VerticalBranchChoice.None;
}

public class VerticalRunnerPickup : MonoBehaviour
{
    public int beatIndex;
    public int value = 1;
    public bool collected;
    public bool missed;
}

public class VerticalRunnerObstacle : MonoBehaviour
{
    public int beatIndex;
}

public class VerticalRunnerBeatPulse : MonoBehaviour
{
    public int beatIndex;
    public float pulseWindowBeats = 0.35f;
    public float pulseScale = 1.18f;

    private Vector3 baseScale;

    private void Start()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        RhythmManager rhythm = RhythmManager.Instance;
        if (rhythm == null || rhythm.bpm <= 0f)
        {
            return;
        }

        float beatPosition = rhythm.GetAdjustedSongTime() / (60f / rhythm.bpm);
        float distance = Mathf.Abs(beatPosition - beatIndex);
        float pulse = Mathf.Clamp01(1f - distance / Mathf.Max(0.01f, pulseWindowBeats));
        transform.localScale = baseScale * Mathf.Lerp(1f, pulseScale, pulse);
    }
}
