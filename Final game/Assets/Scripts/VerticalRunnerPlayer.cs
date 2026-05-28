using UnityEngine;

public class VerticalRunnerPlayer : MonoBehaviour
{
    private VerticalRunnerManager manager;
    private VerticalRunnerSettings settings;
    private VerticalBeatSpawner spawner;
    private Sprite circleSprite;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rigidbody2d;
    private VerticalRunnerPlatform currentPlatform;
    private VerticalRunnerPlatform targetPlatform;
    private VerticalRunnerPickup promptedPickup;
    private Vector3 jumpStart;
    private Vector3 jumpTarget;
    private float jumpTimer;
    private float jumpDuration;
    private bool jumping;
    private bool inputLocked;

    public VerticalRunnerPlatform CurrentPlatform { get { return currentPlatform; } }

    public void Build(VerticalRunnerManager manager, VerticalRunnerSettings settings, VerticalBeatSpawner spawner, Sprite circleSprite)
    {
        this.manager = manager;
        this.settings = settings;
        this.spawner = spawner;
        this.circleSprite = circleSprite;

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = circleSprite;
        spriteRenderer.color = settings.playerColor;
        spriteRenderer.sortingOrder = 5;
        transform.localScale = new Vector3(0.48f, 0.48f, 1f);

        CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.48f;

        rigidbody2d = gameObject.AddComponent<Rigidbody2D>();
        rigidbody2d.bodyType = RigidbodyType2D.Kinematic;
        rigidbody2d.gravityScale = 0f;
        rigidbody2d.freezeRotation = true;

        currentPlatform = spawner.GetPlatformForBeat(settings.startBeat);
        if (currentPlatform != null)
        {
            SnapToPlatform(currentPlatform);
        }
    }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
    }

    public void Tick()
    {
        if (!inputLocked && TryReadDirectionalChoice(out VerticalBranchChoice choice))
        {
            TryDirectionalDodge(choice);
        }
        if (!inputLocked && Input.GetKeyDown(KeyCode.Space))
        {
            TryBeatJump();
        }
        if (!inputLocked && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)))
        {
            TryCollectNearbyCoin();
        }

        if (jumping)
        {
            UpdateJump();
        }
        else if (currentPlatform != null)
        {
            Vector3 target = currentPlatform.transform.position + new Vector3(0f, 0.48f, 0f);
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 7f);
        }

        if (transform.position.y < manager.CameraBottomY - 1.4f)
        {
            manager.TakeDamage("FALL", "Stay with the next mushroom.");
        }

        UpdateCoinPrompt();
    }

    public void RecoverToSafePlatform()
    {
        jumping = false;
        targetPlatform = null;
        promptedPickup = null;
        VerticalRunnerPlatform safe = currentPlatform != null ? currentPlatform : spawner.GetNearestPlatform(transform.position);
        if (safe == null)
        {
            safe = spawner.GetPlatformForBeat(settings.startBeat);
        }
        SnapToPlatform(safe);
    }

    private void TryBeatJump()
    {
        if (currentPlatform != null && currentPlatform.requiresDirectionalChoice)
        {
            manager.ShowDirectionalChoiceHint();
            return;
        }

        RhythmTimingResult result = RhythmManager.Instance != null ? RhythmManager.Instance.ReportInput("Jump") : RhythmTimingResult.None;
        manager.ReportJumpInput(result);

        if (jumping)
        {
            return;
        }

        VerticalRunnerPlatform next = currentPlatform != null && currentPlatform.defaultNext != null
            ? currentPlatform.defaultNext
            : currentPlatform != null ? spawner.GetNextPlatformAfterBeat(currentPlatform.beatIndex) : spawner.GetNearestPlatform(transform.position);
        if (next == null)
        {
            manager.CompleteRun();
            return;
        }

        bool rhythmSuccess = result == RhythmTimingResult.Perfect || result == RhythmTimingResult.Good;
        if (!rhythmSuccess)
        {
            manager.TakeDamage("OFF BEAT", "Jump on yellow to reach the next mushroom.", false);
            return;
        }

        StartJump(next, result);
    }

    private bool TryReadDirectionalChoice(out VerticalBranchChoice choice)
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            choice = VerticalBranchChoice.Left;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            choice = VerticalBranchChoice.Right;
            return true;
        }

        choice = VerticalBranchChoice.None;
        return false;
    }

    private void TryDirectionalDodge(VerticalBranchChoice choice)
    {
        if (jumping || currentPlatform == null || !currentPlatform.requiresDirectionalChoice)
        {
            return;
        }

        RhythmTimingResult result = RhythmManager.Instance != null ? RhythmManager.Instance.ReportInput("Dodge") : RhythmTimingResult.None;
        bool correctDirection = choice == currentPlatform.safeChoice;
        if (!manager.ReportDirectionalDodge(result, correctDirection))
        {
            return;
        }

        VerticalRunnerPlatform next = choice == VerticalBranchChoice.Left ? currentPlatform.leftNext : currentPlatform.rightNext;
        if (next == null)
        {
            manager.TakeDamage("DANGER", "The safe path is missing.");
            return;
        }

        StartJump(next, result, false);
    }

    private void StartJump(VerticalRunnerPlatform next, RhythmTimingResult result, bool showJumpFeedback = true)
    {
        targetPlatform = next;
        jumpStart = transform.position;
        jumpTarget = targetPlatform.transform.position + new Vector3(0f, 0.48f, 0f);
        jumpDuration = (60f / Mathf.Max(1f, settings.bpm)) * settings.jumpDurationBeats;
        if (targetPlatform.longJump)
        {
            jumpDuration *= 1.15f;
        }
        jumpTimer = 0f;
        jumping = true;
        if (showJumpFeedback)
        {
            manager.ShowJumpFeedback(result, targetPlatform.longJump);
        }
    }

    private void TryCollectNearbyCoin()
    {
        VerticalRunnerPickup pickup = spawner.GetNearestCollectibleCoin(transform.position, settings.coinCollectRadius);
        if (pickup == null)
        {
            return;
        }

        RhythmTimingResult result = RhythmManager.Instance != null ? RhythmManager.Instance.ReportInput("Coin") : RhythmTimingResult.None;
        if (!manager.ReportCoinInput(result))
        {
            return;
        }

        CollectPickup(pickup);
    }

    private void CollectPickup(VerticalRunnerPickup pickup)
    {
        if (pickup == null || pickup.collected)
        {
            return;
        }

        pickup.collected = true;
        pickup.gameObject.SetActive(false);
        if (promptedPickup == pickup)
        {
            promptedPickup = null;
        }
        manager.CollectCoin(pickup.value);
    }

    private void UpdateCoinPrompt()
    {
        if (inputLocked)
        {
            promptedPickup = null;
            return;
        }

        VerticalRunnerPickup pickup = spawner.GetNearestCollectibleCoin(transform.position, settings.coinCollectRadius);
        if (pickup != null && pickup != promptedPickup)
        {
            promptedPickup = pickup;
            manager.ShowCoinCollectHint();
        }
        else if (pickup == null)
        {
            promptedPickup = null;
        }
    }

    private void UpdateJump()
    {
        jumpTimer += Time.deltaTime;
        float t = Mathf.Clamp01(jumpTimer / Mathf.Max(0.05f, jumpDuration));
        Vector3 position = Vector3.Lerp(jumpStart, jumpTarget, t);
        float arc = Mathf.Sin(t * Mathf.PI) * (targetPlatform != null && targetPlatform.longJump ? 1.2f : 0.72f);
        transform.position = position + new Vector3(0f, arc, 0f);

        if (t >= 1f)
        {
            jumping = false;
            if (targetPlatform != null)
            {
                currentPlatform = targetPlatform;
                targetPlatform = null;
                manager.ReportPlatformLanded(currentPlatform);
            }
        }
    }

    private void SnapToPlatform(VerticalRunnerPlatform platform)
    {
        currentPlatform = platform;
        targetPlatform = null;
        jumping = false;
        if (platform != null)
        {
            transform.position = platform.transform.position + new Vector3(0f, 0.48f, 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        VerticalRunnerPickup pickup = other.GetComponent<VerticalRunnerPickup>();
        if (pickup != null && !pickup.collected)
        {
            manager.ShowCoinCollectHint();
            promptedPickup = pickup;
            return;
        }

        VerticalRunnerObstacle obstacle = other.GetComponent<VerticalRunnerObstacle>();
        if (obstacle != null)
        {
            manager.TakeDamage("HIT", "Watch the safe side of the beat path.");
        }
    }
}
