using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialState
{
    Intro,
    Countdown,
    Practice,
    Success,
    LoadGame,
    Failed
}

[DefaultExecutionOrder(-200)]
public class TutorialFlowManager : MonoBehaviour
{
    [Header("Flow")]
    public string targetSceneName = "Game";
    public float introDuration = 1.1f;
    public float stepIntroDuration = 1.15f;
    public float countdownStepDuration = 0.55f;
    public float successDelay = 0.65f;

    [Header("Tutorial rhythm")]
    public float tutorialBpm = 126f;
    public int beatsBetweenObstacles = 2;
    public int beatObstacleCount = 16;
    public int firstObstacleBeat = 6;
    public float playerMeetX = -4.5f;
    public float backgroundMoveSpeed = 8f;
    public GameObject backgroundPrefab;

    [Header("References")]
    public TutorialUIController uiController;
    public RhythmManager rhythmManager;
    public GameManager gameManager;
    public TutorialBeatSpawner beatSpawner;

    private TutorialState state;
    private int currentStepIndex;
    private int currentProgress;
    private int currentFailures;
    private int[] currentTargetBeats = new int[0];
    private TutorialActionType[] currentTargetActions = new TutorialActionType[0];
    private BackgroundTranform[] backgroundControllers;
    private PlayerController playerController;
    private Rigidbody2D playerRigidbody;
    private AudioSource musicSource;
    private GameObject gameOverObject;
    private bool successStarted;
    private RhythmTimingResult lastRhythmResult = RhythmTimingResult.None;
    private float lastRhythmInputTime = -10f;
    private Vector3 playerStartPosition;
    private float playerStartGravityScale = 1f;
    private float playerGroundOffset = 0.6f;
    private bool playerRuntimeStateCaptured;
    private float tutorialClockStartTime;
    private Coroutine activeStepRoutine;
    private Coroutine activeCompletionRoutine;
    private Coroutine activeRestartRoutine;

    private TutorialStep[] steps;

    void Awake()
    {
        state = TutorialState.Intro;
        BuildSteps();
        CacheRuntimeObjects();
        ConfigureTutorialScene();
        SetGameplayEnabled(false);
    }

    void Start()
    {
        CacheRuntimeObjects();
        ConfigureTutorialScene();
        EnsureSpawner();
        EnsureUi();
        SubscribeEvents();
        StartCoroutine(TutorialRoutine());
    }

    void Update()
    {
        if (state != TutorialState.Practice)
        {
            return;
        }

        TutorialStep step = steps[currentStepIndex];
        float timeToBeat = GetTimeToNextTargetBeat();
        bool inWindow = IsInsideCurrentTargetBeat();
        if (uiController != null)
        {
            uiController.ShowBeatLane(GetCurrentInputHint(step), timeToBeat, inWindow);
        }

        CheckCurrentObjectiveResult();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private IEnumerator TutorialRoutine()
    {
        state = TutorialState.Intro;
        if (uiController != null)
        {
            uiController.ShowIntro();
        }
        yield return new WaitForSecondsRealtime(introDuration);

        StartMusic();
        BeginStep(0);
    }

    private void BeginStep(int stepIndex)
    {
        if (activeStepRoutine != null)
        {
            StopCoroutine(activeStepRoutine);
        }

        if (activeCompletionRoutine != null)
        {
            StopCoroutine(activeCompletionRoutine);
            activeCompletionRoutine = null;
        }

        if (activeRestartRoutine != null)
        {
            StopCoroutine(activeRestartRoutine);
            activeRestartRoutine = null;
        }

        activeStepRoutine = StartCoroutine(StartStepRoutine(stepIndex));
    }

    private IEnumerator StartStepRoutine(int stepIndex)
    {
        currentStepIndex = Mathf.Clamp(stepIndex, 0, steps.Length - 1);
        currentProgress = 0;
        currentFailures = 0;
        state = TutorialState.Countdown;
        TutorialStep step = steps[currentStepIndex];

        ResetPlayerForStep();
        ResetRhythmPowerups();
        SetGameplayEnabled(false);
        if (beatSpawner != null)
        {
            beatSpawner.ClearAll();
        }
        currentTargetBeats = new int[0];
        currentTargetActions = new TutorialActionType[0];

        if (uiController != null)
        {
            uiController.ShowStepIntro(step.title, step.instruction, step.inputHint, currentProgress, step.requiredSuccesses);
        }
        yield return new WaitForSecondsRealtime(stepIntroDuration);

        string[] countdown = { "3", "2", "1", "Go" };
        for (int i = 0; i < countdown.Length; i++)
        {
            if (uiController != null)
            {
                uiController.ShowCountdown(countdown[i]);
            }
            yield return new WaitForSecondsRealtime(countdownStepDuration);
        }

        PrepareStepTargets(step);
        SpawnCurrentStepObjects(step);
        BackgroundTranform.EnsureForwardSegmentExists();
        state = TutorialState.Practice;
        if (uiController != null)
        {
            uiController.ShowStepProgress(step.title, currentProgress, step.requiredSuccesses);
        }
        ShowCurrentTargetArrow();
        SetGameplayEnabled(true);
        activeStepRoutine = null;
    }

    private void PrepareStepTargets(TutorialStep step)
    {
        int baseBeat = Mathf.Max(firstObstacleBeat, Mathf.CeilToInt(GetCurrentBeat()) + 4);
        if (baseBeat % 2 != 0)
        {
            baseBeat++;
        }

        currentTargetBeats = new int[step.requiredSuccesses];
        currentTargetActions = new TutorialActionType[step.requiredSuccesses];

        for (int i = 0; i < step.requiredSuccesses; i++)
        {
            currentTargetBeats[i] = baseBeat + i * step.beatSpacing;
            currentTargetActions[i] = step.GetActionForIndex(i);
        }
    }

    private void SpawnCurrentStepObjects(TutorialStep step)
    {
        for (int i = 0; i < currentTargetBeats.Length; i++)
        {
            TutorialActionType action = currentTargetActions[i];
            if (action == TutorialActionType.None)
            {
                continue;
            }

            if (beatSpawner != null)
            {
                beatSpawner.Spawn(action, currentTargetBeats[i], step.key);
            }
        }
    }

    private void OnRhythmInputReported(string actionName, RhythmTimingResult result)
    {
        if (state != TutorialState.Practice)
        {
            return;
        }

        lastRhythmResult = result;
        lastRhythmInputTime = Time.time;

        if (uiController != null)
        {
            if (IsTimingSuccess(result))
            {
                uiController.ShowInputResult(result, currentProgress, steps[currentStepIndex].requiredSuccesses);
            }
            else if (result == RhythmTimingResult.Miss)
            {
                uiController.ShowStatusHint("OFF BEAT", GetOffBeatHint(GetCurrentExpectedAction()), Color.gray);
            }
        }
    }

    public void ReportRhythmPickup(TutorialActionType pickupType, int beatIndex, string displayName, int scoreValue, GameObject pickupObject)
    {
        if (state != TutorialState.Practice)
        {
            DestroyPickupObject(pickupObject);
            return;
        }

        TutorialActionType expectedAction = GetCurrentExpectedAction();
        if (expectedAction != pickupType)
        {
            DestroyPickupObject(pickupObject);
            HandleStepMistake("WRONG ITEM", "Follow the arrow. This beat wants " + GetCurrentInputHint(steps[currentStepIndex]) + ".");
            return;
        }

        if (!IsCurrentTargetBeatIndex(beatIndex))
        {
            DestroyPickupObject(pickupObject);
            HandleStepMistake("WRONG BEAT", "This item belongs to another marker. Stay with the highlighted arrow.");
            return;
        }

        DestroyPickupObject(pickupObject);
        if (scoreValue > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.UpdateBonus(scoreValue);
        }

        if (pickupType == TutorialActionType.BeatBoost)
        {
            AdvanceProgress(lastRhythmResult);
            return;
        }

        if (pickupType == TutorialActionType.PulseMagnet)
        {
            AdvanceProgress(lastRhythmResult);
            return;
        }

        AdvanceProgress(lastRhythmResult);
    }

    private void DestroyPickupObject(GameObject pickupObject)
    {
        if (pickupObject != null)
        {
            Destroy(pickupObject);
        }
    }

    private void ResetRhythmPowerups()
    {
    }

    private void CheckCurrentObjectiveResult()
    {
        TutorialActionType expectedAction = GetCurrentExpectedAction();
        if (expectedAction != TutorialActionType.Jump && expectedAction != TutorialActionType.Slide)
        {
            CheckCurrentPickupMiss();
            return;
        }

        TutorialSpawnedObject target = GetCurrentTargetSpawnedObject();
        if (target == null || playerController == null)
        {
            return;
        }

        float playerX = playerController.transform.position.x;
        float targetRightEdge = GetObjectRightEdge(target.gameObject);
        if (targetRightEdge < playerX - 0.25f)
        {
            AdvanceProgress(lastRhythmResult);
        }
    }

    private TutorialSpawnedObject GetCurrentTargetSpawnedObject()
    {
        if (currentTargetBeats == null || currentProgress < 0 || currentProgress >= currentTargetBeats.Length)
        {
            return null;
        }

        TutorialActionType expectedAction = GetCurrentExpectedAction();
        TutorialSpawnedObject[] spawnedObjects = FindObjectsOfType<TutorialSpawnedObject>();
        for (int i = 0; i < spawnedObjects.Length; i++)
        {
            if (spawnedObjects[i] == null)
            {
                continue;
            }

            if (spawnedObjects[i].beatIndex == currentTargetBeats[currentProgress] && spawnedObjects[i].actionType == expectedAction)
            {
                return spawnedObjects[i];
            }
        }

        return null;
    }

    private void CheckCurrentPickupMiss()
    {
        TutorialActionType expectedAction = GetCurrentExpectedAction();
        if (!IsPickupAction(expectedAction))
        {
            return;
        }

        TutorialSpawnedObject target = GetCurrentTargetSpawnedObject();
        if (target == null || playerController == null)
        {
            return;
        }

        float playerX = playerController.transform.position.x;
        float targetRightEdge = GetObjectRightEdge(target.gameObject);
        if (targetRightEdge < playerX - 0.45f)
        {
            HandleStepMistake("MISSED ITEM", "You passed the marker without collecting it. Use the yellow flash to line up the pickup.");
        }
    }

    private float GetObjectRightEdge(GameObject obj)
    {
        if (obj == null)
        {
            return float.PositiveInfinity;
        }

        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>(true);
        if (colliders != null && colliders.Length > 0)
        {
            float maxX = float.NegativeInfinity;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    maxX = Mathf.Max(maxX, colliders[i].bounds.max.x);
                }
            }

            if (!float.IsNegativeInfinity(maxX))
            {
                return maxX;
            }
        }

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            float maxX = float.NegativeInfinity;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    maxX = Mathf.Max(maxX, renderers[i].bounds.max.x);
                }
            }

            if (!float.IsNegativeInfinity(maxX))
            {
                return maxX;
            }
        }

        return obj.transform.position.x;
    }

    private bool IsPickupAction(TutorialActionType action)
    {
        return action == TutorialActionType.RhythmCoin
            || action == TutorialActionType.BeatBoost
            || action == TutorialActionType.PulseMagnet;
    }

    private void AdvanceProgress(RhythmTimingResult result)
    {
        TutorialStep step = steps[currentStepIndex];
        TutorialActionType completedAction = GetCurrentExpectedAction();
        currentProgress++;
        currentFailures = 0;
        if (uiController != null)
        {
            ShowObjectiveClearFeedback(completedAction);
            uiController.ShowStepProgress(step.title, currentProgress, step.requiredSuccesses);
        }
        ShowCurrentTargetArrow();

        if (currentProgress >= step.requiredSuccesses)
        {
            if (activeCompletionRoutine == null)
            {
                activeCompletionRoutine = StartCoroutine(CompleteStepRoutine());
            }
        }
    }

    private void ShowObjectiveClearFeedback(TutorialActionType completedAction)
    {
        if (uiController == null)
        {
            return;
        }

        bool rhythmSuccess = IsTimingSuccess(lastRhythmResult) && Time.time - lastRhythmInputTime <= 0.9f;
        string objective = GetObjectiveClearName(completedAction);
        if (rhythmSuccess)
        {
            uiController.ShowInputResult(lastRhythmResult, currentProgress, steps[currentStepIndex].requiredSuccesses);
            uiController.ShowStatusHint(objective, "Cleared with the beat. Keep using the yellow flash.", GetResultFeedbackColor(lastRhythmResult));
        }
        else
        {
            uiController.ShowStatusHint(objective, "Cleared, but your rhythm was late or early. Try to move on yellow.", new Color(1f, 0.72f, 0.18f));
        }
    }

    private string GetObjectiveClearName(TutorialActionType completedAction)
    {
        if (completedAction == TutorialActionType.Jump)
        {
            return "BARRIER CLEAR";
        }

        if (completedAction == TutorialActionType.Slide)
        {
            return "GATE CLEAR";
        }

        if (completedAction == TutorialActionType.RhythmCoin)
        {
            return "COIN";
        }

        if (completedAction == TutorialActionType.BeatBoost)
        {
            return "BOOST";
        }

        if (completedAction == TutorialActionType.PulseMagnet)
        {
            return "MAGNET";
        }

        return "CLEAR";
    }

    private string GetOffBeatHint(TutorialActionType expectedAction)
    {
        if (expectedAction == TutorialActionType.Jump)
        {
            return "Jump when the lane is yellow, then clear the barrier.";
        }

        if (expectedAction == TutorialActionType.Slide)
        {
            return "Press Down when the lane is yellow, then pass the gate.";
        }

        if (IsPickupAction(expectedAction))
        {
            return "Use the yellow flash to line up the pickup path.";
        }

        return "Blue means wait. Yellow means go.";
    }

    private Color GetResultFeedbackColor(RhythmTimingResult result)
    {
        if (result == RhythmTimingResult.Perfect)
        {
            return new Color(1f, 0.78f, 0.18f);
        }

        if (result == RhythmTimingResult.Good)
        {
            return new Color(0.18f, 0.86f, 1f);
        }

        return new Color(1f, 0.72f, 0.18f);
    }

    private IEnumerator CompleteStepRoutine()
    {
        SetGameplayEnabled(false);
        if (beatSpawner != null)
        {
            beatSpawner.ClearAll();
        }
        yield return new WaitForSecondsRealtime(0.55f);

        if (currentStepIndex >= steps.Length - 1)
        {
            StartCoroutine(SuccessRoutine());
        }
        else
        {
            activeCompletionRoutine = null;
            BeginStep(currentStepIndex + 1);
            yield break;
        }

        activeCompletionRoutine = null;
    }

    private void HandleStepMistake(string label, string hint)
    {
        TutorialStep step = steps[currentStepIndex];
        state = TutorialState.Failed;
        currentProgress = 0;
        currentFailures++;
        lastRhythmResult = RhythmTimingResult.None;
        lastRhythmInputTime = -10f;
        ResetRhythmPowerups();
        SetGameplayEnabled(false);
        if (beatSpawner != null)
        {
            beatSpawner.ClearAll();
        }
        if (uiController != null)
        {
            uiController.ShowFailureHint(label, hint, currentFailures);
            uiController.ShowStepProgress(step.title, currentProgress, step.requiredSuccesses);
        }

        if (activeRestartRoutine == null)
        {
            activeRestartRoutine = StartCoroutine(RestartCurrentStepAfterHint());
        }
    }

    private IEnumerator RestartCurrentStepAfterHint()
    {
        yield return new WaitForSecondsRealtime(0.9f);
        activeRestartRoutine = null;
        BeginStep(currentStepIndex);
    }

    private IEnumerator SuccessRoutine()
    {
        if (successStarted)
        {
            yield break;
        }

        successStarted = true;
        state = TutorialState.Success;
        SetGameplayEnabled(false);
        if (beatSpawner != null)
        {
            beatSpawner.ClearAll();
        }
        if (uiController != null)
        {
            uiController.ShowSuccess();
        }
        yield return new WaitForSecondsRealtime(successDelay);

        state = TutorialState.LoadGame;
        SceneTransitionManager.LoadScene(targetSceneName);
    }

    private void OnGameOverStarted()
    {
        if (state == TutorialState.Success || state == TutorialState.LoadGame || state == TutorialState.Failed)
        {
            if (gameManager != null)
            {
                gameManager.RecoverFromTutorialRetry();
            }
            SetGameplayEnabled(false);
            return;
        }

        if (state != TutorialState.Practice)
        {
            if (gameManager != null)
            {
                gameManager.RecoverFromTutorialRetry();
            }
            SetGameplayEnabled(false);
            return;
        }

        if (gameOverObject != null)
        {
            gameOverObject.SetActive(false);
        }
        if (gameManager != null)
        {
            gameManager.RecoverFromTutorialRetry();
        }
        HandleStepMistake("TRY AGAIN", "You hit the obstacle. Watch the marker and clear it to pass.");
    }

    private void CacheRuntimeObjects()
    {
        if (rhythmManager == null)
        {
            rhythmManager = RhythmManager.Instance != null ? RhythmManager.Instance : FindObjectOfType<RhythmManager>();
        }

        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>();
        }

        backgroundControllers = FindObjectsOfType<BackgroundTranform>();
        playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerRigidbody = playerController.GetComponent<Rigidbody2D>();
            if (!playerRuntimeStateCaptured)
            {
                playerStartPosition = playerController.transform.position;
                if (playerRigidbody != null)
                {
                    playerStartGravityScale = Mathf.Max(0.01f, playerRigidbody.gravityScale);
                }

                Collider2D playerCollider = playerController.GetComponent<Collider2D>();
                float floorY = GetCurrentFloorTopY(playerController.transform.position.x);
                if (playerCollider != null)
                {
                    playerGroundOffset = Mathf.Max(0.05f, playerController.transform.position.y - playerCollider.bounds.min.y);
                }
                if (!float.IsNaN(floorY))
                {
                    playerStartPosition.y = floorY + playerGroundOffset;
                }

                playerRuntimeStateCaptured = true;
            }
        }

        if (rhythmManager != null && rhythmManager.musicSource != null)
        {
            musicSource = rhythmManager.musicSource;
        }
        else
        {
            musicSource = FindObjectOfType<AudioSource>();
        }

        if (gameOverObject == null)
        {
            gameOverObject = GameObject.Find("GameOver");
        }
    }

    private void EnsureSpawner()
    {
        if (beatSpawner == null)
        {
            beatSpawner = FindObjectOfType<TutorialBeatSpawner>();
        }

        if (beatSpawner == null)
        {
            GameObject obj = new GameObject("TutorialBeatSpawner");
            beatSpawner = obj.AddComponent<TutorialBeatSpawner>();
        }

        if (beatSpawner != null)
        {
            beatSpawner.Configure(tutorialBpm, backgroundMoveSpeed, playerMeetX);
            beatSpawner.RemoveStaticTutorialObjects();
        }
    }

    private void EnsureUi()
    {
        if (uiController == null)
        {
            uiController = FindObjectOfType<TutorialUIController>();
        }

        if (uiController == null)
        {
            GameObject obj = new GameObject("TutorialUIController");
            uiController = obj.AddComponent<TutorialUIController>();
        }

        if (uiController != null)
        {
            uiController.BindRhythmManager(rhythmManager);
            int initialRequired = steps != null && steps.Length > 0 ? steps[0].requiredSuccesses : 4;
            uiController.Configure(initialRequired, tutorialBpm, Mathf.Max(1, beatsBetweenObstacles), firstObstacleBeat);
            uiController.EnsureUi();
        }
    }

    private void SubscribeEvents()
    {
        if (playerController != null)
        {
            playerController.RhythmInputReported -= OnRhythmInputReported;
            playerController.RhythmInputReported += OnRhythmInputReported;
        }

        if (gameManager != null)
        {
            gameManager.GameOverStarted -= OnGameOverStarted;
            gameManager.GameOverStarted += OnGameOverStarted;
        }
    }

    private void UnsubscribeEvents()
    {
        if (playerController != null)
        {
            playerController.RhythmInputReported -= OnRhythmInputReported;
        }

        if (gameManager != null)
        {
            gameManager.GameOverStarted -= OnGameOverStarted;
        }
    }

    private void ConfigureTutorialScene()
    {
        if (gameManager != null)
        {
            gameManager.saveScoreOnGameOver = false;
        }

        SceneDifficultySettings settings = SceneDifficultySettings.Instance;
        if (settings != null)
        {
            settings.backgroundMoveSpeed = backgroundMoveSpeed;
            settings.extraSpeedMultiplier = 1f;
            settings.autoSpawnEnemies = false;
            settings.spawnEnemiesOnBeat = false;
            settings.spawnGapsOnBeat = false;
            settings.obstacleBpm = tutorialBpm;
            settings.beatObstacleCount = beatObstacleCount;
            settings.beatsBetweenObstacles = Mathf.Max(1, beatsBetweenObstacles);
            settings.firstObstacleBeat = firstObstacleBeat;
            settings.playerMeetX = playerMeetX;

            if (backgroundPrefab != null)
            {
                settings.mapPrefabs = new[] { backgroundPrefab };
            }
        }

        BackgroundTranform[] backgrounds = FindObjectsOfType<BackgroundTranform>(true);
        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i] == null)
            {
                continue;
            }

            backgrounds[i].spawnBarriersOnStart = false;
            backgrounds[i].disableStaticBarrierChildren = true;
            backgrounds[i].moveSpeed = backgroundMoveSpeed;
            if (backgroundPrefab != null)
            {
                backgrounds[i].mapPrefabs = new[] { backgroundPrefab };
            }
        }

        if (rhythmManager != null)
        {
            rhythmManager.bpm = tutorialBpm;
            rhythmManager.visualizationBpm = tutorialBpm;
            rhythmManager.useLevelTimeWhenMusicMissing = true;
            rhythmManager.levelTimeFallbackStart = tutorialClockStartTime;
            rhythmManager.syncVisualizationToMusic = true;
            rhythmManager.SetVisualizationEnabled(true);
        }
    }

    private void SetGameplayEnabled(bool enabled)
    {
        backgroundControllers = FindObjectsOfType<BackgroundTranform>();

        if (enabled)
        {
            BackgroundTranform.EnsureForwardSegmentExists();
        }

        for (int i = 0; i < backgroundControllers.Length; i++)
        {
            if (backgroundControllers[i] != null)
            {
                backgroundControllers[i].enabled = enabled;
            }
        }

        if (playerController != null)
        {
            playerController.enabled = enabled;
        }

        SetPlayerPhysicsPaused(!enabled);
    }

    private void SetPlayerPhysicsPaused(bool paused)
    {
        if (playerRigidbody == null)
        {
            return;
        }

        playerRigidbody.velocity = Vector2.zero;
        playerRigidbody.angularVelocity = 0f;
        playerRigidbody.gravityScale = paused ? 0f : playerStartGravityScale;
        playerRigidbody.isKinematic = paused;

        if (paused)
        {
            playerRigidbody.Sleep();
        }
        else
        {
            playerRigidbody.WakeUp();
        }
    }

    private void ResetPlayerForStep()
    {
        Vector3 resetPosition = playerStartPosition;
        float floorY = GetCurrentFloorTopY(resetPosition.x);
        if (!float.IsNaN(floorY))
        {
            resetPosition.y = floorY + playerGroundOffset;
        }

        if (playerController != null)
        {
            playerController.ResetForTutorial(resetPosition);
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.gravityScale = playerStartGravityScale;
            SetPlayerPhysicsPaused(true);
        }
    }

    private float GetCurrentFloorTopY(float sampleX)
    {
        GameObject[] floors = GameObject.FindGameObjectsWithTag("Floor");
        if (floors == null || floors.Length == 0)
        {
            return float.NaN;
        }

        float bestLocalTop = float.NegativeInfinity;
        float bestTop = float.NegativeInfinity;
        for (int i = 0; i < floors.Length; i++)
        {
            if (floors[i] == null || !floors[i].activeInHierarchy)
            {
                continue;
            }

            Collider2D collider = floors[i].GetComponent<Collider2D>();
            if (collider == null)
            {
                continue;
            }

            Bounds bounds = collider.bounds;
            if (sampleX >= bounds.min.x - 0.2f && sampleX <= bounds.max.x + 0.2f)
            {
                bestLocalTop = Mathf.Max(bestLocalTop, bounds.max.y);
            }

            bestTop = Mathf.Max(bestTop, bounds.max.y);
        }

        if (!float.IsNegativeInfinity(bestLocalTop))
        {
            return bestLocalTop;
        }

        return float.IsNegativeInfinity(bestTop) ? float.NaN : bestTop;
    }

    private void StartMusic()
    {
        tutorialClockStartTime = Time.timeSinceLevelLoad;
        if (rhythmManager != null)
        {
            rhythmManager.levelTimeFallbackStart = tutorialClockStartTime;
        }

        if (musicSource == null)
        {
            Debug.LogWarning("TutorialFlowManager: No music source found. Tutorial will use level time for beat timing.");
            return;
        }

        musicSource.Stop();
        musicSource.time = 0f;
        musicSource.Play();
    }

    private TutorialActionType GetCurrentExpectedAction()
    {
        if (currentTargetActions == null || currentProgress < 0 || currentProgress >= currentTargetActions.Length)
        {
            return TutorialActionType.None;
        }

        return currentTargetActions[currentProgress];
    }

    private bool IsExpectedInputAction(string actionName, TutorialActionType expectedAction)
    {
        if (expectedAction == TutorialActionType.None)
        {
            return true;
        }

        if (expectedAction == TutorialActionType.Jump)
        {
            return actionName == "Jump";
        }

        if (expectedAction == TutorialActionType.Slide)
        {
            return actionName == "Slide";
        }

        return false;
    }

    private string GetCurrentInputHint(TutorialStep step)
    {
        TutorialActionType action = GetCurrentExpectedAction();
        if (action == TutorialActionType.Jump)
        {
            return "JUMP";
        }

        if (action == TutorialActionType.Slide)
        {
            return "DOWN";
        }

        if (action == TutorialActionType.RhythmCoin)
        {
            return "COIN";
        }

        if (action == TutorialActionType.BeatBoost)
        {
            return "BOOST";
        }

        if (action == TutorialActionType.PulseMagnet)
        {
            return "MAGNET";
        }

        return step.inputHint;
    }

    private void ShowCurrentTargetArrow()
    {
        if (uiController == null || currentTargetBeats == null || currentProgress >= currentTargetBeats.Length)
        {
            if (uiController != null)
            {
                uiController.ShowTargetArrow(null, "");
            }
            return;
        }

        TutorialSpawnedObject[] spawnedObjects = FindObjectsOfType<TutorialSpawnedObject>();
        for (int i = 0; i < spawnedObjects.Length; i++)
        {
            if (spawnedObjects[i] == null)
            {
                continue;
            }

            if (spawnedObjects[i].beatIndex == currentTargetBeats[currentProgress] && spawnedObjects[i].actionType == GetCurrentExpectedAction())
            {
                uiController.ShowTargetArrow(spawnedObjects[i].transform, GetCurrentInputHint(steps[currentStepIndex]));
                return;
            }
        }

        uiController.ShowTargetArrow(null, GetCurrentInputHint(steps[currentStepIndex]));
    }

    private bool IsTimingSuccess(RhythmTimingResult result)
    {
        return result == RhythmTimingResult.Perfect || result == RhythmTimingResult.Good;
    }

    private bool IsInsideCurrentTargetBeat()
    {
        if (currentTargetBeats == null || currentProgress < 0 || currentProgress >= currentTargetBeats.Length)
        {
            return currentTargetBeats == null || currentTargetBeats.Length == 0;
        }

        float beat = GetCurrentBeat();
        float distance = Mathf.Abs(beat - currentTargetBeats[currentProgress]);
        return distance <= 0.35f;
    }

    private bool IsCurrentTargetBeatIndex(int beatIndex)
    {
        if (currentTargetBeats == null || currentProgress < 0 || currentProgress >= currentTargetBeats.Length)
        {
            return false;
        }

        return currentTargetBeats[currentProgress] == beatIndex;
    }

    private float GetTimeToNextTargetBeat()
    {
        float songTime = GetTutorialSongTime();
        float beatInterval = 60f / Mathf.Max(1f, tutorialBpm);
        if (currentTargetBeats == null || currentProgress < 0 || currentProgress >= currentTargetBeats.Length)
        {
            return 0f;
        }

        float nextBeat = currentTargetBeats[currentProgress];
        return nextBeat * beatInterval - songTime;
    }

    private float GetCurrentBeat()
    {
        float songTime = GetTutorialSongTime();
        return songTime / (60f / Mathf.Max(1f, tutorialBpm));
    }

    private float GetTutorialSongTime()
    {
        if (rhythmManager != null && rhythmManager.musicSource != null && rhythmManager.musicSource.isPlaying)
        {
            return rhythmManager.GetAdjustedSongTime();
        }

        return Mathf.Max(0f, Time.timeSinceLevelLoad - tutorialClockStartTime);
    }

    private void BuildSteps()
    {
        steps = new[]
        {
            new TutorialStep("JumpBarrier", "Jump Barrier", "Use the yellow beat flash to jump, then clear the barrier.", "JUMP", TutorialActionType.Jump, 3, 2),
            new TutorialStep("SlideGate", "Slide Gate", "Use the yellow beat flash to slide, then pass under the gate.", "DOWN", TutorialActionType.Slide, 3, 2),
            new TutorialStep("RhythmCoin", "Rhythm Coin", "Use the beat to line up the path, then collect the coin.", "COIN", TutorialActionType.RhythmCoin, 3, 2),
            new TutorialStep("BeatBoost", "Beat Boost", "Collect the Boost item on the rhythm path.", "BOOST", TutorialActionType.BeatBoost, 1, 2),
            new TutorialStep("PulseMagnet", "Pulse Magnet", "Collect the Magnet item on the rhythm path.", "MAGNET", TutorialActionType.PulseMagnet, 1, 2),
            new TutorialStep("FinalCombo", "Final Combo", "Clear barriers and collect items. The beat is your guide.", "MIX", TutorialActionType.Jump, 6, 6, new[] {
                TutorialActionType.Jump,
                TutorialActionType.Slide,
                TutorialActionType.RhythmCoin,
                TutorialActionType.BeatBoost,
                TutorialActionType.PulseMagnet,
                TutorialActionType.Jump
            })
        };
    }

    private class TutorialStep
    {
        public readonly string key;
        public readonly string title;
        public readonly string instruction;
        public readonly string inputHint;
        public readonly TutorialActionType primaryAction;
        public readonly int requiredSuccesses;
        public readonly int beatSpacing;
        private readonly TutorialActionType[] actionPattern;

        public TutorialStep(string key, string title, string instruction, string inputHint, TutorialActionType primaryAction, int requiredSuccesses, int beatSpacing, TutorialActionType[] actionPattern = null)
        {
            this.key = key;
            this.title = title;
            this.instruction = instruction;
            this.inputHint = inputHint;
            this.primaryAction = primaryAction;
            this.requiredSuccesses = requiredSuccesses;
            this.beatSpacing = beatSpacing;
            this.actionPattern = actionPattern;
        }

        public TutorialActionType GetActionForIndex(int index)
        {
            if (actionPattern != null && actionPattern.Length > 0)
            {
                return actionPattern[index % actionPattern.Length];
            }

            return primaryAction;
        }

        public bool ContainsAction(TutorialActionType actionType)
        {
            if (actionPattern == null)
            {
                return primaryAction == actionType;
            }

            for (int i = 0; i < actionPattern.Length; i++)
            {
                if (actionPattern[i] == actionType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
