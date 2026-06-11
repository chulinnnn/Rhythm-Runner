using UnityEngine;

// Data model for VerticalRunner mode, tutorial steps, timing, scoring, and fallback visuals.
// VerticalRunner 的模式、教程步骤、时序、分数和兜底视觉数据模型。
//
// Inspector fields are configuration defaults; scene templates and UI hierarchy own authored art/layout.
// Inspector 字段是配置默认值；场景模板和 UI Hierarchy 拥有已编辑美术/布局。

public enum VerticalRunnerMode
{
    Tutorial,
    Game
}

public enum VerticalTutorialStepType
{
    BeatJump,
    LandOnMushroom,
    CollectCoin,
    AvoidObstacle,
    LongJump,
    FinalMiniRun
}

[System.Serializable]
public class VerticalRunnerSettings
{
    [Header("Timing")]
    public float bpm = 126f;
    public float firstBeatOffset = 0f;
    public float songDurationSeconds = 75f;
    [Tooltip("Jump beat slot interval. 2 = jump every 2 beats. Also affects route platform spacing on rebuild.")]
    public int beatsPerPlatform = 2;
    [Tooltip("Within a valid beat slot: fraction from beat start (0-1) that counts as Perfect. Smaller = harder.")]
    [Range(0.05f, 0.95f)]
    public float perfectBeatFraction = 0.45f;
    [Tooltip("Within a valid beat slot: fraction from beat start (0-1) that still counts as Good. Above this = Miss.")]
    [Range(0.1f, 1f)]
    public float goodBeatFraction = 1f;
    [Tooltip("When enabled, only Perfect counts as success; Good is treated as Miss for score and feedback.")]
    public bool requirePerfectForSuccess = false;
    public float actionWindowBeats = 0.32f;
    public int countdownBeats = 8;

    [Header("Route")]
    public int startBeat = 2;
    public float platformSpacingY = 1.55f;
    public float longJumpSpacingY = 2.25f;
    public float laneWidth = 2.45f;
    public int prebuildBeats = 96;

    [Header("Rules")]
    public float jumpDurationBeats = 0.86f;
    public float playerRecoverDelay = 0.45f;
    public float coinCollectRadius = 0.86f;
    public float dangerBranchLaneOffset = 1.35f;

    [Header("Score")]
    public int jumpScore = 10;
    public int bananaScore = 50;
    public int parrotScore = 30;
    public int comboBonusStep = 1;
    public int finishScore = 100;

    [Header("Sprites")]
    public Sprite mushroomSprite;
    public Sprite coinSprite;
    public Sprite obstacleSprite;
    public Sprite backgroundSprite;

    [Header("Fallback colors")]
    public Color backgroundColor = new Color(0.08f, 0.18f, 0.28f, 1f);
    public Color platformColor = new Color(0.35f, 0.82f, 0.45f, 1f);
    public Color strongPlatformColor = new Color(0.58f, 0.42f, 0.95f, 1f);
    public Color coinColor = new Color(1f, 0.84f, 0.16f, 1f);
    public Color obstacleColor = new Color(1f, 0.24f, 0.22f, 1f);
    public Color playerColor = new Color(0.25f, 0.82f, 1f, 1f);
}
